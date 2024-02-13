# com.unity.ide.visualstudio-backport
![image](https://user-images.githubusercontent.com/2897430/166826494-2c838a44-93ec-44fc-8d2d-046adeddb5b3.png)

`com.unity.ide.visualstudio` supports working with Unity 2019.4.21f1 and later. We backported it for earlier versions of Unity.

## Works with
- Unity 2019.2.21f1

## Install

https://github.com/Kryeker/com.unity.ide.visualstudio-backport.git

## License
The original `com.unity.ide.visualstudio` package is licensed under the MIT license.

turns into:

![image](https://user-images.githubusercontent.com/3404365/192978279-5caf95cd-7a1e-4fe0-bc93-8fd05cc89e2d.png)

By that you're able to remove all those projects from your Visual Studio solution you'd never look at anyway or even edit.

## Installation
Here are the easiest ways to install the package, choose either:

### A) Using package manager
Open Package Manager, select "Add package from git URL..." and enter
```
MIT License

Copyright (c) 2019 Unity Technologies
Copyright (c) 2019 Microsoft Corporation. All rights reserved.
Copyright (c) 2021 Cysharp, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## Versioning
Tags and UPM package versions will be postfixed `-apf` from 2.0.22 on.

Adaptions for previous versions are also available, see [tags](https://github.com/krisrok/com.unity.ide.visualstudio/tags).

## Usage

1. Navigate to Edit/Preferences/External Tools.
2. Use the the pre-existing filters to choose between embedded, local packages and so on. (Note this functionality is already provided by Unity's original package.)
3. Now, for finer-grained control you can open the the "Advanced Filters" foldout:
4. Select which packages and assemblies (defined by .asmdefs) you actually want in your solution. Tip: You can also use Ctrl/Shift to add/remove checkmarks in bulk while moving your cursor over them.
5. Click "Regenerate project files" to make sure changes are applied.
6. Open Visual Studio e.g. by opening a script. If the solution is already open Visual Studio should display a dialog to automatically re-open it.

## Note
This is a fork of a mirror package because Unity does not provide public access to the original sources.

[Mirrored from UPM, not affiliated with Unity Technologies.] 📦 Code editor integration for supporting Visual Studio as code editor for unity. Adds support for generating csproj files for intellisense purposes, auto discovery of installations, etc.
