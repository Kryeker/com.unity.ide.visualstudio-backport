using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using Unity.CodeEditor;

// Advanced filters "addons"
namespace Microsoft.Unity.VisualStudio.Editor
{
	public partial class VisualStudioEditor : IExternalCodeEditor
	{
		private class AssemblyWrapper
		{
			internal string PackageId;
			internal string Path;
			internal string Id;
			internal string DisplayName;
		}

		private class PackageWrapper
		{
			internal string Id;
			internal string DisplayName;
			internal List<AssemblyWrapper> Assemblies;
			internal ProjectGenerationFlag Source;
		}

		private Dictionary<ProjectGenerationFlag, bool> _packageFiltersExpanded = new Dictionary<ProjectGenerationFlag, bool>();
		private Dictionary<string, bool> _assemblyFiltersExpanded = new Dictionary<string, bool>();
		private ProjectGenerationFlag _cachedFlag;
		private Dictionary<string, bool> _packageFilter;
		private Dictionary<string, bool> _assemblyFilter;
		private List<PackageWrapper> _packageAssemblyHierarchy;
		private Dictionary<ProjectGenerationFlag, List<PackageWrapper>> _packageAssemblyHierarchyByGenerationFlag;

		private void EnsureAdvancedFiltersCache(IVisualStudioInstallation installation)
		{
			if (_packageFilter == null || _cachedFlag != installation.ProjectGenerator.AssemblyNameProvider.ProjectGenerationFlag)
				InitializeAdvancedFiltersCache(installation);
		}

		private void InitializeAdvancedFiltersCache(IVisualStudioInstallation installation)
		{
			_cachedFlag = installation.ProjectGenerator.AssemblyNameProvider.ProjectGenerationFlag;

			_packageFilter = CreateFilterDictionary(installation.ProjectGenerator.ExcludedPackages);
			_assemblyFilter = CreateFilterDictionary(installation.ProjectGenerator.ExcludedAssemblies);

			var eligiblePackages = UnityHelper.GetAllPackages()
				.Select(p => new PackageWrapper
				{
					Id = p.name,
					DisplayName = string.IsNullOrWhiteSpace(p.displayName) ? p.name : p.displayName,
					Source = ProjectGenerationFlagFromPackageSource(p.source)
				})
				.OrderBy(ph => ph.DisplayName)
				.ToList();

			var eligibleAssemblies = UnityEditor.Compilation.CompilationPipeline.GetAssemblies()
				.Select(a =>
				{
					var assemblyPath = UnityEditor.Compilation.CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(a.name);
					if (assemblyPath == null)
					{
						// "Assembly-CSharp" and the like... we'll just ignore them
						return null;
						// ... or draw them as disabled:
						//return new AssemblyWrapper { Id = a.name, DisplayName = a.name };
					}

					var asset = AssetDatabase.LoadAssetAtPath<UnityEditorInternal.AssemblyDefinitionAsset>(assemblyPath);

					var assemblyName = assemblyPath != null ? Path.GetFileName(assemblyPath) : a.name;

					var package = installation.ProjectGenerator.AssemblyNameProvider.FindForAssetPath(assemblyPath);
					if (package == null)
						// .asmdef in /Assets, so no package
						return new AssemblyWrapper { Id = assemblyName, Path = assemblyPath, DisplayName = assemblyName };

					// .asmdef within a package
					return new AssemblyWrapper { PackageId = package.name, Id = assemblyName, Path = assemblyPath, DisplayName = assemblyName };
				})
				.Where(ah => ah != null)
				.OrderBy(ah => ah.DisplayName)
				.ToList();

			// Join by package id
			_packageAssemblyHierarchy = eligiblePackages.GroupJoin(eligibleAssemblies.Where(a => a != null && a.PackageId != null),
				p => p.Id, a => a.PackageId,
				(p, aa) => new PackageWrapper { Id = p.Id, DisplayName = p.DisplayName, Assemblies = aa.ToList(), Source = p.Source })
				.Where(p => p.Assemblies.Any())
				.ToList();

			// Prepend "empty package" containing the .asmdefs in Assets folder
			var assetsAssemblies = eligibleAssemblies.Where(a => a != null && a.PackageId == null).ToList();
			if (assetsAssemblies.Count > 0)
				_packageAssemblyHierarchy.Insert(0, new PackageWrapper { Assemblies = assetsAssemblies });

			_packageAssemblyHierarchyByGenerationFlag = _packageAssemblyHierarchy.GroupBy(p => p.Source).ToDictionary(pg => pg.Key, pg => pg.ToList());
		}

		private ProjectGenerationFlag ProjectGenerationFlagFromPackageSource(PackageSource source)
		{
			switch (source)
			{
				case PackageSource.Unknown:
					return ProjectGenerationFlag.None;

				default:
					return (ProjectGenerationFlag)Enum.Parse(typeof(ProjectGenerationFlag), source.ToString());
			}
		}

		private Dictionary<string, bool> CreateFilterDictionary(IList<string> excludedPackages)
		{
			return excludedPackages?
				.Where(p => string.IsNullOrWhiteSpace(p) == false)
				.ToDictionary(p => p, _ => false)
				?? new Dictionary<string, bool>();
		}

		private void WriteBackFilters(IVisualStudioInstallation installation)
		{
			installation.ProjectGenerator.ExcludedPackages = _packageFilter
				.Where(kvp => kvp.Value == false)
				.Select(kvp => kvp.Key)
				.ToList();

			installation.ProjectGenerator.ExcludedAssemblies = _assemblyFilter
				.Where(kvp => kvp.Value == false)
				.Select(kvp => kvp.Key)
				.ToList();
		}

		private string FormatAssemblyCount(int includedCount, int count) => $"{includedCount}/{count} assembl{(count == 1 ? "y" : "ies")}";

		private void DrawPackageFilters(ProjectGenerationFlag preference, IVisualStudioInstallation installation, bool isParentEnabled)
		{
			var isDirty = false;

			foreach (var package in _packageAssemblyHierarchy)
			{
				if (package.Source != preference)
					continue;

				var assemblyCount = package.Assemblies.Count;
				var includedAssemblyCount = package.Assemblies.Count(a => installation.ProjectGenerator.ExcludedAssemblies.Contains(a.Id) == false);

				if (_packageFilter.TryGetValue(package.Id, out var wasEnabled) == false)
					_packageFilter.Add(package.Id, wasEnabled = true);

				if (_assemblyFiltersExpanded.TryGetValue(package.Id, out var showAssemblies) == false)
					showAssemblies = false;

				EditorGUILayout.BeginHorizontal();
				var result = DrawFoldoutToggle(new FoldoutToggleOptions
				{
					label = new GUIContent(package.DisplayName),
					isEnabled = wasEnabled,
					drawFoldout = assemblyCount > 0,
					isExpanded = showAssemblies,
					showMixedValue = assemblyCount > includedAssemblyCount,
					drawLabelAsDisabled = isParentEnabled == false || includedAssemblyCount == 0
				});

				if (result.isEnabled != wasEnabled)
				{
					_packageFilter[package.Id] = result.isEnabled;
					isDirty = true;
				}
				_assemblyFiltersExpanded[package.Id] = result.isExpanded;

				EditorGUILayout.EndHorizontal();

				if (result.isExpanded)
				{
					EditorGUI.indentLevel++;
					isDirty = DrawAssemblyFilters(package, result.isEnabled && result.drawLabelAsDisabled == false) || isDirty;
					EditorGUI.indentLevel--;
				}
			}

			if (isDirty)
			{
				WriteBackFilters(installation);
			}
		}

		private void DrawAssetAssemblies(IVisualStudioInstallation installation)
		{
			var assetsPackage = _packageAssemblyHierarchyByGenerationFlag.TryGetValue(ProjectGenerationFlag.None, out var packageWrappers) ? packageWrappers.FirstOrDefault() : null;
			if (assetsPackage == null)
				return;

			var assemblyCount = assetsPackage.Assemblies.Count();
			var includedAssemblyCount = assetsPackage.Assemblies.Count(a => installation.ProjectGenerator.ExcludedAssemblies.Contains(a.Id) == false);

			if (_packageFiltersExpanded.TryGetValue(ProjectGenerationFlag.None, out var isFoldoutExpanded) == false)
				isFoldoutExpanded = false;

			var result = DrawFoldoutToggle(new FoldoutToggleOptions
			{
				isEnabled = true,
				drawFoldout = true,
				isExpanded = isFoldoutExpanded,
				label = new GUIContent("Assemblies from Assets"),
				drawLabelAsDisabled = includedAssemblyCount == 0,
				disableToggle = true,
				showMixedValue = assemblyCount != includedAssemblyCount
			});

			DrawAssemblyCountInfo(assemblyCount, includedAssemblyCount);

			_packageFiltersExpanded[ProjectGenerationFlag.None] = result.isExpanded;

			if (result.isExpanded)
			{
				EditorGUI.indentLevel++;
				var isDirty = DrawAssemblyFilters(assetsPackage, isParentEnabled: true);
				EditorGUI.indentLevel--;

				if (isDirty)
				{
					WriteBackFilters(installation);
				}
			}
		}

		private void DrawAssemblyCountInfo(int assemblyCount, int includedAssemblyCount)
		{
			var rect = GUILayoutUtility.GetLastRect();
			var guiContent = new GUIContent($"{FormatAssemblyCount(includedAssemblyCount, assemblyCount)}");
			rect.xMin += 310;
			EditorGUI.BeginDisabledGroup(includedAssemblyCount == 0);
			EditorGUI.LabelField(rect, guiContent, EditorStyles.miniLabel);
			EditorGUI.EndDisabledGroup();
		}

		private bool DrawAssemblyFilters(PackageWrapper package, bool isParentEnabled)
		{
			if (package.Assemblies == null)
				return false;

			var isDirty = false;
			foreach (var assembly in package.Assemblies)
			{
				if (_assemblyFilter.TryGetValue(assembly.Id, out var wasEnabled) == false)
					_assemblyFilter.Add(assembly.Id, wasEnabled = true);

				EditorGUI.BeginDisabledGroup(assembly.Path == null);
				var result = DrawFoldoutToggle(new FoldoutToggleOptions
				{
					label = new GUIContent(assembly.DisplayName),
					isEnabled = wasEnabled,
					drawLabelAsDisabled = isParentEnabled == false
				});
				EditorGUI.EndDisabledGroup();

				if (result.isEnabled != wasEnabled)
				{
					_assemblyFilter[assembly.Id] = result.isEnabled;
					isDirty = true;
				}
			}
			return isDirty;
		}

		private struct FoldoutToggleOptions
		{
			public GUIContent label;
			public bool isEnabled;
			public bool drawFoldout;
			public bool isExpanded;
			public bool showMixedValue;
			public bool drawLabelAsDisabled;
			internal bool disableToggle;
		}

		private static FoldoutToggleOptions DrawFoldoutToggle(FoldoutToggleOptions ftOptions, params GUILayoutOption[] options)
		{
			EditorGUILayout.BeginHorizontal();

			var previousColor = GUI.color;
			var disabledColor = previousColor;
			disabledColor.a *= 0.5f;

			var drawLabelAsDisabled = ftOptions.drawLabelAsDisabled || ftOptions.isEnabled == false;

			Rect labelRect = new Rect();
			if (ftOptions.drawFoldout)
			{
				ftOptions.isExpanded = EditorGUILayout.Foldout(ftOptions.isExpanded, GUIContent.none, toggleOnLabelClick: false);
				labelRect = GUILayoutUtility.GetLastRect();
				labelRect.xMin += 12;

				if(drawLabelAsDisabled)
					GUI.color = disabledColor;

				EditorGUI.LabelField(labelRect, ftOptions.label);
			}
			else
			{
				if (drawLabelAsDisabled)
					GUI.color = disabledColor;

				EditorGUILayout.LabelField(ftOptions.label);
			}
			GUI.color = previousColor;

			var foldoutRect = GUILayoutUtility.GetLastRect();
			var toggleRect = foldoutRect;
			toggleRect.xMin = 300;
			var savedIndentLevel = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			EditorGUI.BeginDisabledGroup(ftOptions.disableToggle);
			// Use change check because of mixed value mode returning unclear value
			EditorGUI.BeginChangeCheck();
			EditorGUI.showMixedValue = ftOptions.isEnabled && ftOptions.showMixedValue;
			EditorGUI.Toggle(toggleRect, GUIContent.none, (bool)ftOptions.isEnabled);
			EditorGUI.showMixedValue = false;
			EditorGUI.EndDisabledGroup();

			if(EditorGUI.EndChangeCheck())
			{
				ftOptions.isEnabled = !ftOptions.isEnabled;
			}
			// If the toggle was not pressed we can check for clicks on the foldout's label
			else if(ftOptions.drawFoldout)
			{
				if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && labelRect.Contains(Event.current.mousePosition))
				{
					ftOptions.isExpanded = !ftOptions.isExpanded;
				}
			}

			EditorGUI.indentLevel = savedIndentLevel;

			//EditorGUILayout.Toggle(GUIContent.none, isEnabled, options);
			//EditorGUILayout.BeginHorizontal();
			//EditorGUILayout.LabelField(label, GUILayout.Width(260 - EditorGUI.indentLevel * 15));
			//EditorGUILayout.Toggle(wasEnabled, GUILayout.Width(32), GUILayout.ExpandWidth(true));
			//EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndHorizontal();

			if (foldoutRect.Contains(Event.current.mousePosition))
			{
				if (Event.current.shift)
					ftOptions.isEnabled = false;
				else if (Event.current.control)
					ftOptions.isEnabled = true;

				EditorGUI.DrawRect(foldoutRect, new Color(0.1f, 0.1f, 0.1f, 0.1f));
			}
			return ftOptions;
		}

		private void DrawResetFiltersButton(IVisualStudioInstallation installation)
		{
			var rect = EditorGUILayout.GetControlRect();
			rect.width = 252;
			rect.x -= 20;
			EditorGUI.BeginDisabledGroup(_packageFilter.Count == 0 && _assemblyFilter.Count == 0);
			if (GUI.Button(rect, "Reset filters"))
			{
				_packageFilter = new Dictionary<string, bool>();
				_assemblyFilter = new Dictionary<string, bool>();
				_packageFiltersExpanded = new Dictionary<ProjectGenerationFlag, bool>();
				WriteBackFilters(installation);
			}
			EditorGUI.EndDisabledGroup();
		}
	}
}
