Configurable ILMerge Task for MSBuild
=====================================


Scotec-ILMerge-MSBuild-Task is a MSBuild task that wraps the ILMerge.exe command-line tool. 
The code is based on the [ILMerge MSBuild task by Emerson Brito](https://github.com/emerbrito/ILMerge-MSBuild-Task) and also supports the new .csproj format.

This build task supports the new .csproj format and is executed before the target directory is created. This means that the target assembly does not have to be created in a separate directory, but can be merged with the referenced assemblies at an early stage. The target directory then only contains the merged assembly. 



Getting Started
---------------

Add the Scotec.ILMerge.MSBuild.Task package to your Visual Studio project:

```powershell
Install-Package Scotec.ILMerge.MSBuild.Task
```

Open project file and edit the Scotec.ILMerge.MSBuild.Task package reference:

```xml
<PackageReference Include="Scotec.ILMerge.MSBuild.Task" Version="0.1.0-dev.61">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>build</IncludeAssets>
</PackageReference>
 ```
 Only build assets should be included.

Build your project. The merged assembly will be stored in your project output directory.

