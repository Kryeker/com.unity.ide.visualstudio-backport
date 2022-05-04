/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Unity Technologies.
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System;

namespace Microsoft.Unity.VisualStudio.Editor
{
	[Flags]
	public enum ProjectGenerationFlag
	{
		None = 0,
		Embedded = 1,
		Local = 2,
		Registry = 4,
		Git = 8,
		BuiltIn = 16,
		Unknown = 32,
		PlayerAssemblies = 64,
#if UNITY_2019_3_OR_NEWER
		LocalTarBall = 128,
#endif
	}
}
