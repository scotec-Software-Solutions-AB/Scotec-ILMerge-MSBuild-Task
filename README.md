Configurable ILMerge Task for MSBuild
=====================================


Scotec-ILMerge-MSBuild-Task is a MSBuild task that wraps the ILMerge.exe command-line tool. 

The code is based on the [ILMerge MSBuild task by Emerson Brito](https://github.com/emerbrito/ILMerge-MSBuild-Task) and also supports the new .csproj format.


This is a clone of the [ILMerge-MSBuild-Task by Emerson Brito](https://github.com/emerbrito/ILMerge-MSBuild-Task).

Getting Started
---------------

Use Nuget to add Scotec.ILMerge.MSBuild.Task to your Visual Studio project:

```
Install-Package ILMerge.MSBuild.Task
```

Build your project. The merged assembly will be stored in an ILMerge folder under your project output.
The output directory is configurable.
