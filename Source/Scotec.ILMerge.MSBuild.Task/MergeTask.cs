#region MIT License

/*
    MIT License

    Copyright (c) 2016 Emerson Brito
    Copyright (c) 2023 Olaf Meyer

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
 */

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;

namespace Scotec.ILMerge.MsBuild.Task;

// project properties
// http://stackoverflow.com/questions/2772426/how-to-access-the-msbuild-s-properties-list-when-coding-a-custom-task

// dependencies
// http://stackoverflow.com/questions/8849289/get-dependent-assemblies

public sealed class MergeTask : Microsoft.Build.Utilities.Task
{
    #region Constructors


    public MergeTask()
    {
        //Debugger.Launch();
        InputAssemblies = Array.Empty<ITaskItem>();
    }

    #endregion

    #region Public Methods

    public override bool Execute()
    {
        LogInputVariables();

        string exePath;
        var settings = new MergerSettings();

        // try to read configuration if file exists
        if (!ReadConfigFile(out var jsonConfig))
        {
            return false;
        }

        // replace tokens if applicable
        if (!string.IsNullOrWhiteSpace(jsonConfig))
        {
            jsonConfig = ReplaceTokens(jsonConfig);
        }

        // if json config exists, try to deserialize into settings object
        if (!string.IsNullOrWhiteSpace(jsonConfig) && !DeserializeJsonConfig(jsonConfig, out settings))
        {
            return false;
        }

        // create instance if seetings still null which indicates a custom json config was not used
        settings ??= new MergerSettings();

        // apply defaults
        SetDefaults(settings);

        if (settings.General.AlternativeILMergePath.HasValue())
        {
            if (!File.Exists(settings.General.AlternativeILMergePath))
            {
                Log.LogError($"An alternative path for ILMerge.exe was provided but the file was not found: {settings.General.AlternativeILMergePath}");
                return false;
            }

            exePath = settings.General.AlternativeILMergePath;
            Log.LogMessage("Using alternative ILMerge path: {0}", settings.General.AlternativeILMergePath);
        }
        else
        {
            exePath = GetILMergePath();
        }

        if (!exePath.HasValue())
        {
            Log.LogError("ILMerge.exe was no located. Make sure you have the ILMerge nuget package installed. "
                         + "If you defined a custom packages folder in your Nuget.Config it is possible we are having a hard time figuring it out. "
                         + "In this case please use attribute 'AlternativeILMergePath' in the configuration file to indicate the full path for ILMerge.exe.");
            return false;
        }

        // log configuration file used by this task.
        LogConfigFile(settings);

        if (!MergeAssemblies(exePath, settings))
        {
            return false;
        }

        return true;
    }

    #endregion

    #region Public Properties

    public ITaskItem[] InputAssemblies { get; set; }

    public string[] AdditionalLocations { get; set; }

    [Required] public string ConfigurationFilePath { get; set; }

    [Required] public string SolutionDir { get; set; }

    public string SolutionPath { get; set; }

    [Required] public string ProjectDir { get; set; }

    public string ProjectFileName { get; set; }

    public string ProjectPath { get; set; }

    [Required] public string TargetDir { get; set; }

    public string TargetPath { get; set; }

    public string IntermediateOutputPath { get; set; }

    public string TargetFileName { get; set; }

    [Required] public string TargetFrameworkVersion { get; set; }

    public string TargetArchitecture { get; set; }

    public string ILMergeConsolePath { get; set; }

    public string KeyFile { get; set; }

    #endregion

    #region Private Methods

    private void LogInputVariables()
    {
        Log.LogMessage($"SolutionDir: {SolutionDir}");
        Log.LogMessage($"SolutionPath: {SolutionPath}");
        Log.LogMessage($"ProjectDir: {ProjectDir}");
        Log.LogMessage($"ProjectFileName: {ProjectFileName}");
        Log.LogMessage($"ProjectPath: {ProjectPath}");
        Log.LogMessage($"TargetDir: {TargetDir}");
        Log.LogMessage($"TargetPath: {TargetPath}");
        Log.LogMessage($"IntermediateOutputPath: {IntermediateOutputPath}");
        Log.LogMessage($"TargetFileName: {TargetFileName}");
        Log.LogMessage($"TargetFrameworkVersion: {TargetFrameworkVersion}");
        Log.LogMessage($"TargetArchitecture: {TargetArchitecture}");
        Log.LogMessage($"ILMergeConsolePath: {ILMergeConsolePath}");
        Log.LogMessage($"KeyFile: {KeyFile}");
        Log.LogMessage($"ConfigurationFilePath: {ConfigurationFilePath}");
    }

    private bool DeserializeJsonConfig(string jsonConfig, out MergerSettings settings)
    {
        settings = null;
        var success = true;

        if (string.IsNullOrWhiteSpace(jsonConfig))
        {
            Log.LogError("Unable to deserialize configuration. Configuration string is null or empty.");
            return false;
        }

        try
        {
            Log.LogMessage("Deserializing configuration.");
            settings = MergerSettings.FromJson(jsonConfig);
            Log.LogMessage("Configuration file deserialized successfully.");
        }
        catch (Exception ex)
        {
            Log.LogError("Error deserializing configuration.");
            Log.LogErrorFromException(ex);
            success = false;
        }

        return success;
    }

    private string ReplaceTokens(string jsonConfig)
    {
        return jsonConfig
               .Replace("$(SolutionDir)", EscapePath(SolutionDir))
               .Replace("$(SolutionPath)", EscapePath(SolutionPath))
               .Replace("$(ProjectDir)", EscapePath(ProjectDir))
               .Replace("$(ProjectFileName)", EscapePath(ProjectFileName))
               .Replace("$(ProjectPath)", EscapePath(ProjectPath))
               .Replace("$(TargetDir)", EscapePath(TargetDir))
               .Replace("$(TargetPath)", EscapePath(TargetPath))
               .Replace("$(IntermediateOutputPath)", EscapePath(IntermediateOutputPath))
               .Replace("$(TargetFileName)", EscapePath(TargetFileName))
               .Replace("$(AssemblyOriginatorKeyFile)", EscapePath(KeyFile));
    }

    private void SetDefaults(MergerSettings settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        settings.General ??= new GeneralSettings();

        if (!settings.General.KeyFile.HasValue() && KeyFile.HasValue())
        {
            settings.General.KeyFile = ToAbsolutePath(KeyFile);
            Log.LogMessage("Applying default value for KeyFile.");
        }

        if (!settings.General.OutputFile.HasValue())
        {
            settings.General.OutputFile = Path.Combine(TargetDir, TargetFileName);
            //settings.General.OutputFile = Path.Combine(this.TargetDir, "ILMerge", this.TargetFileName);
            Log.LogMessage("Applying default value for OutputFile.");
        }

        if (!settings.General.TargetPlatform.HasValue())
        {
            settings.General.TargetPlatform = FrameworkInfo.ToILmergeTargetPlatform(TargetFrameworkVersion, TargetArchitecture, Log);
            Log.LogMessage($"Applying default value for TargetPlatform: {settings.General.TargetPlatform}");
        }

        var targetPath = string.IsNullOrEmpty(IntermediateOutputPath) ? TargetPath : IntermediateOutputPath;


        if (settings.General.InputAssemblies == null || settings.General.InputAssemblies.Count == 0)
        {
            Log.LogMessage("No input assembles were found in configuration.");

            settings.General.InputAssemblies = new List<string>();

            Log.LogMessage($"Adding target assembly: {targetPath}");
            settings.General.InputAssemblies.Add(targetPath);

            // When building projects in old .csproj format the @(ReferenceCopyLocalPaths) macro returns .pdb and .xml files as well.
            // We need to filter out these files. This might happen even with new .csproj format but I never observed this.
            //TODO: Probably we can do this already within the .targets file.
            var inputAssembliesFilter = InputAssemblies.Where(item => Path.GetExtension(item.ItemSpec).ToLower() == ".dll");
            foreach (var item in inputAssembliesFilter)
            {
                Log.LogMessage($"Adding assembly: {item.ItemSpec}");
                settings.General.InputAssemblies.Add(item.ItemSpec);
            }
        }
        else
        {
            foreach (var item in settings.General.InputAssemblies)
            {
                Log.LogMessage($"Config input assembly: {item}");
            }

            Log.LogMessage($"Adding target assembly at position [0]: {targetPath}");
            settings.General.InputAssemblies.Insert(0, targetPath);
        }

        settings.Advanced ??= new AdvancedSettings();

        settings.Advanced.SearchDirectories ??= new List<string>();

        if (!settings.Advanced.SearchDirectories.Contains(TargetDir))
        {
            settings.Advanced.SearchDirectories.Add(TargetDir);
        }

        if (AdditionalLocations != null && AdditionalLocations.Any())
        {
            foreach (var dir in AdditionalLocations)
            {
                if (!settings.Advanced.SearchDirectories.Contains(dir))
                {
                    settings.Advanced.SearchDirectories.Add(dir);
                }
            }
        }
    }

    private bool MergeAssemblies(string mergerPath, MergerSettings settings)
    {
        var success = true;
        var ilmerge = LoadILMerge(mergerPath);
        var ilmergeType = ilmerge.GetType("ILMerging.ILMerge", true, true);
        if (ilmergeType == null)
        {
            throw new InvalidOperationException("Cannot find 'ILMerging.ILMerge' in executable.");
        }

        Log.LogMessage("Instantianting ILMerge.");
        dynamic merger = Activator.CreateInstance(ilmergeType);

        Log.LogMessage("Setting up ILMerge.");

        merger.AllowMultipleAssemblyLevelAttributes = settings.Advanced.AllowMultipleAssemblyLevelAttributes;
        merger.AllowWildCards = settings.Advanced.AllowWildCards;
        merger.AllowZeroPeKind = settings.Advanced.AllowZeroPeKind;
        merger.AttributeFile = settings.Advanced.AttributeFile;
        merger.Closed = settings.Advanced.Closed;
        merger.CopyAttributes = settings.Advanced.CopyAttributes;
        merger.DebugInfo = settings.Advanced.DebugInfo;
        merger.DelaySign = settings.Advanced.DelaySign;
        merger.FileAlignment = settings.Advanced.FileAlignment > 0 ? settings.Advanced.FileAlignment : 512;
        merger.KeyFile = settings.General.KeyFile;
        merger.Log = settings.Advanced.Log;
        merger.LogFile = settings.Advanced.LogFile;
        merger.OutputFile = settings.General.OutputFile;
        merger.PublicKeyTokens = settings.Advanced.PublicKeyTokens;
        merger.UnionMerge = settings.Advanced.UnionMerge;
        merger.XmlDocumentation = settings.Advanced.XmlDocumentation;

        if (!string.IsNullOrWhiteSpace(settings.Advanced.ExcludeFile))
        {
            merger.ExcludeFile = settings.Advanced.ExcludeFile;
        }

        merger.Internalize = settings.Advanced.Internalize;

        if (settings.Advanced.TargetKind.HasValue())
        {
            merger.TargetKind = Enum.Parse(merger.TargetKind.GetType(), settings.Advanced.TargetKind);
        }

        if (settings.Advanced.Version.HasValue())
        {
            merger.Version = new Version(settings.Advanced.Version);
        }

        if (settings.Advanced.AllowDuplicateType.HasValue())
        {
            if (settings.Advanced.AllowDuplicateType == "*")
            {
                merger.AllowDuplicateType(null);
            }
            else
            {
                foreach (var typeName in settings.Advanced.AllowDuplicateType.Split(','))
                {
                    merger.AllowDuplicateType(typeName);
                }
            }
        }

        var tp = settings.General.TargetPlatform.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);

        merger.SetTargetPlatform(tp[0].Trim(), tp[1].Trim());
        merger.SetInputAssemblies(settings.General.InputAssemblies.ToArray());
        merger.SetSearchDirectories(settings.Advanced.SearchDirectories
                                            .Concat(settings.General.InputAssemblies.Select(item => Path.GetDirectoryName(item))
                                                            .Distinct())
                                            .ToArray());

        try
        {
            var outputDir = Path.GetDirectoryName(settings.General.OutputFile);
            if (!Directory.Exists(outputDir))
            {
                Log.LogWarning($"Output directory not found. An attempt to create the directory will be made: {outputDir}");
                Directory.CreateDirectory(outputDir!);
                Log.LogMessage("Output directory created.");
            }

            Log.LogMessage(
                MessageImportance.Normal,
                "Merging {0} assembl{1} to '{2}'.",
                settings.General.InputAssemblies.Count.ToString(),
                settings.General.InputAssemblies.Count != 1 ? "ies" : "y",
                settings.General.OutputFile);

            merger.Merge();

            if (merger.StrongNameLost)
            {
                Log.LogMessage(MessageImportance.High, "StrongNameLost = true");
            }
        }
        catch (Exception exception)
        {
            Log.LogErrorFromException(exception);
            success = false;
        }

        if (settings.Advanced.DeleteCopiesOverwriteTarget)
        {
            DeleteCopiesAndCopyTargetPath(settings);
        }

        return success;
    }

    private void DeleteCopiesAndCopyTargetPath(MergerSettings settings)
    {
        // Delete input assemblies and copy the merged outputfile to the TargetPath

        Log.LogMessage(MessageImportance.Normal, "Deleting Merged Assembl{0}",
            settings.General.InputAssemblies.Count != 1 ? "ies" : "y");

        foreach (var file in settings.General.InputAssemblies)
        {
            try
            {
                // Input can reference dependency project, make sure the assembly is removed from the TargetDir                    
                DeleteFileAndPdb(Path.Combine(TargetDir, Path.GetFileName(file)));
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
            }
        }

        // Copy the output-file to the target-dir
        Log.LogMessage(MessageImportance.Normal, "Overwriting OutputTarget");
        try
        {
            // Remove and copy Target
            DeleteFileAndPdb(TargetPath);

            if (File.Exists(settings.General.OutputFile))
            {
                File.Copy(settings.General.OutputFile, TargetPath);
            }

            // Also copy debug-information
            var outputFilePdb = Path.ChangeExtension(settings.General.OutputFile, "pdb");
            if (File.Exists(outputFilePdb))
            {
                File.Copy(outputFilePdb!, Path.ChangeExtension(TargetPath, "pdb")!);
            }
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex);
        }
    }

    private static void DeleteFileAndPdb(string targetFile)
    {
        if (File.Exists(targetFile))
        {
            File.Delete(targetFile);
        }

        // Also do debug-output, if existing
        var targetFilePdb = Path.ChangeExtension(targetFile, "pdb");
        if (File.Exists(targetFilePdb))
        {
            File.Delete(targetFilePdb);
        }
    }

    private Assembly LoadILMerge(string mergerPath)
    {
        if (string.IsNullOrWhiteSpace(mergerPath))
        {
            throw new ArgumentNullException(nameof(mergerPath));
        }

        Log.LogMessage($"Loading ILMerge from '{mergerPath}'.");

        var ilmerge = Assembly.LoadFrom(mergerPath);

        return ilmerge;
    }

    private bool ReadConfigFile(out string jsonConfig)
    {
        jsonConfig = string.Empty;
        var success = true;

        if (string.IsNullOrWhiteSpace(ConfigurationFilePath))
        {
            Log.LogWarning("Path for configuration file is empty. Default values will be applied.");
        }
        else
        {
            if (!File.Exists(ConfigurationFilePath))
            {
                Log.LogMessage($"Using default configuration. A custom configuration file was not found at: {ConfigurationFilePath}");
            }
            else
            {
                try
                {
                    Log.LogMessage($"Loading configuration from: {ConfigurationFilePath}");
                    jsonConfig = File.ReadAllText(ConfigurationFilePath, Encoding.UTF8);
                    Log.LogMessage("Configuration file loaded successfully.");
                }
                catch (Exception ex)
                {
                    Log.LogError("Error reading configuration file.");
                    Log.LogErrorFromException(ex);
                    success = false;
                }
            }
        }

        return success;
    }

    public string GetILMergePath()
    {
        string exePath;
        var failedPaths = new List<string>();

        // Look at property containing ILMerge's full path. Supported in ILMerge nuget package 3.0.17 and above.
        if (string.IsNullOrWhiteSpace(ILMergeConsolePath))
        {
            Log.LogWarning(
                "Variable $(ILMergeConsolePath) is not available. For a better experience please make sure you are using the latest version of ilmerge's nuget pakcage.");
        }
        else
        {
            exePath = Path.GetFullPath(ILMergeConsolePath);
            Log.LogMessage($"ILMerge.exe found at $(ILMergeConsolePath): {exePath}");
            return exePath;
        }

        // look at same directory as this assembly (task dll);
        if (ExeLocationHelper.TryValidateILMergePath(Path.GetDirectoryName(GetType().Assembly.Location), out exePath))
        {
            Log.LogMessage($"ILMerge.exe found at (task location): {Path.GetDirectoryName(GetType().Assembly.Location)}");
            return exePath;
        }

        var errMsg = $"ILMerge.exe not found at (task location): {Path.GetDirectoryName(GetType().Assembly.Location)}";
        failedPaths.Add(errMsg);
        Log.LogMessage(errMsg);

        // look at target dir;
        if (!string.IsNullOrWhiteSpace(TargetDir))
        {
            if (ExeLocationHelper.TryValidateILMergePath(TargetDir, out exePath))
            {
                Log.LogMessage($"ILMerge.exe found at (target dir): {TargetDir}");
                return exePath;
            }

            errMsg = $"ILMerge.exe not found at (target dir): {TargetDir}";
            failedPaths.Add(errMsg);
            Log.LogMessage(errMsg);
        }

        // look for "packages" folder at the solution root and if one is found, look for ILMerge package folder
        if (!string.IsNullOrWhiteSpace(SolutionDir))
        {
            if (ExeLocationHelper.TryILMergeInSolutionDir(SolutionDir, out exePath))
            {
                Log.LogMessage($"ILMerge.exe found at (solution dir): {SolutionDir}");
                return exePath;
            }

            {
                errMsg = $"ILMerge.exe not found at (solution dir): {SolutionDir}";
                failedPaths.Add(errMsg);
                Log.LogMessage(errMsg);
            }
        }

        // get the location of the this assembly (task dll) and assumes it is under the packages folder.
        // use this information to determine the possible location of the executable.
        if (ExeLocationHelper.TryLocatePackagesFolder(Log, out exePath))
        {
            Log.LogMessage($"ILMerge.exe found at custom package location: {exePath}");
            return exePath;
        }

        {
            foreach (var err in failedPaths)
            {
                Log.LogWarning(err);
            }

            Log.LogWarning("Unable to determine custom package location or, location was determined but an ILMerge package folder was not found.");
        }

        return exePath;
    }

    private string ToAbsolutePath(string relativePath)
    {
        // if path is not rooted assume it is relative.
        // convert relative to absolute using project dir as root.

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentNullException(relativePath);
        }

        if (Path.IsPathRooted(relativePath))
        {
            return relativePath;
        }

        return Path.GetFullPath(Path.Combine(ProjectDir, relativePath));
    }

    private void LogConfigFile(MergerSettings settings)
    {
        var outputPath = Path.Combine(Path.GetDirectoryName(settings.General.OutputFile)!,
            Path.GetFileNameWithoutExtension(settings.General.OutputFile) + ".merge.json");
        var outputDir = Path.GetDirectoryName(outputPath);

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir!);
        }

        Log.LogMessage("Saving current configuration to: {0}", outputPath);
        File.WriteAllText(outputPath, settings.ToJson(), Encoding.UTF8);
    }

    private string EscapePath(string path)
    {
        return Regex.Replace(path ?? "", @"\\", @"\\");
    }

    #endregion
}
