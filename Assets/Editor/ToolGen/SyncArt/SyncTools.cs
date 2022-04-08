using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public static class SyncTools
{
    public static string FolderChoose(string pathKey)
    {
        var folderPath = EditorUtility.OpenFolderPanel("Find File", Application.dataPath, "");
        Debug.Log(folderPath);
        if (string.IsNullOrEmpty(folderPath)) { return null; }
        PlayerPrefs.SetString(pathKey, folderPath);
        PlayerPrefs.Save();
        return folderPath;
    }
    
    /// <summary>
    /// 0=根目录 1=子目录 2=子目录平铺
    /// </summary>
    public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target, bool overwrite, string[] extension, int subDir, HashSet<string> skip = null)
    {
        if (subDir > 0)
        {
            foreach (var dir in source.GetDirectories())
            {
                if ((skip == null || !skip.Contains(dir.Name)) 
                    && (dir.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden
                    && (dir.Attributes & FileAttributes.System) != FileAttributes.System)
                {
                    if (subDir == 1)
                    {
                        CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name), overwrite, extension, subDir, skip);
                    }
                    else
                    {
                        CopyFilesRecursively(dir, target, overwrite, extension, subDir, skip);
                    }
                }
            }
        }
		
        foreach (var file in source.GetFiles())
        {
            if (extension != null) 
            {
                var enableCopy = false;
                var count = extension.Length;
                for (int i = 0; i < count; i++) 
                {
                    if (string.IsNullOrEmpty(extension[i])) { continue; }
                    if (extension[i].EndsWith(file.Extension, StringComparison.OrdinalIgnoreCase)) 
                    {
                        enableCopy = true;
                        break;
                    }
                }
                if (enableCopy) 
                {
                    file.CopyTo(Path.Combine(target.FullName, file.Name), overwrite);
                }
            }
            else
            {
                file.CopyTo(Path.Combine(target.FullName, file.Name), overwrite);
            }
        }
    }

    public static bool InvokeProcess(string processPath, string args, string workDirectory)
    {
        try
        {
            var info = new ProcessStartInfo(processPath, args);
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            info.ErrorDialog = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.WorkingDirectory = workDirectory;
            info.StandardOutputEncoding = Encoding.UTF8;
            info.StandardErrorEncoding = Encoding.UTF8;
            
            var p = Process.Start(info);
            var output = p.StandardOutput.ReadToEnd();
            var error = p.StandardError.ReadToEnd();
            p.WaitForExit();
            var exitCode = p.ExitCode;
            p.Close();
            if (!string.IsNullOrEmpty(output))
            {
                if (exitCode == 0)
                {
                    Debug.Log(output);
                }
                else
                {
                    Debug.LogError(output);
                }
            }
            if (!string.IsNullOrEmpty(error))
            {
                Debug.Log(args);
                
                if (exitCode == 0)
                {
                    Debug.LogWarning(error);
                }
                else
                {
                    Debug.LogError(error);
                }
            }
            return exitCode == 0;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}