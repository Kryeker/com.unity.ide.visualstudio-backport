using System;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;

public class UnityHelper : MonoBehaviour
{
#if UNITY_2021_1_OR_NEWER
	public static IEnumerable<PackageInfo> GetAllPackages() => PackageInfo.GetAllRegisteredPackages();
#else
	private static Func<PackageInfo[]> _getAllPackages;
	public static IEnumerable<PackageInfo> GetAllPackages()
	{
		if (_getAllPackages == null)
		{
			var m = typeof(PackageInfo).GetMethod("GetAll", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.FlattenHierarchy);
			_getAllPackages = (Func<PackageInfo[]>)Delegate.CreateDelegate(typeof(Func<PackageInfo[]>), m);
		}

		return _getAllPackages();
	}
#endif
}
