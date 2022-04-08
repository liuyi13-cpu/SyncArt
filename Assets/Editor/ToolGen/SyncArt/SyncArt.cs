using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;

public class SyncArt
{
    public struct DataItem
    {
        public string SourcePath;
        public string TargetPath;
        public string[] SuffixArray;
        public int SubDir; // 0=根目录 1=子目录 2=子目录平铺
    }
    private List<DataItem> mItems = new();
    
    private const string ArtResPath = "ArtResPath";
    private static readonly string XmlPath = Path.Combine(Application.dataPath, "Editor/ToolGen/Config/art_table.xml");
    private static readonly string TargetPath = Application.dataPath;
    
    [MenuItem("资源同步工具/同步资源 &A")]
    private static void _SyncRes()
    {
        new SyncArt().OnSyncArt();
    }

    private void OnSyncArt()
    {
        var sourcePath = string.Empty;
        if (SelectPath(ArtResPath, ref sourcePath)) {
            
            // 1.1更新svn
            // ExternalProcessInvoke.InvokeProcess("svn", "update", sourcePath);
            // 1.2更新git
            SyncTools.InvokeProcess("git", "pull", sourcePath);
            
            // 2.解析xml
            LoadXml(XmlPath);
            
            // 3.拷贝资源
            UpdateDir(sourcePath);

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("提示", "同步美术资源成功", "确定");
        }
    }
    
    private bool SelectPath(string key, ref string path)
    {
        var rootPath = PlayerPrefs.GetString(key);
        if (string.IsNullOrEmpty(rootPath)) {
            if (!EditorUtility.DisplayDialog("提示", "请选择美术资源目录", "确定")) {
                return false;
            }
            rootPath = SyncTools.FolderChoose(key);
            if (string.IsNullOrEmpty(rootPath)) {
                EditorUtility.DisplayDialog("提示", "未选择美术资源目录", "确定");
                return false;
            }
        }

        if (!Directory.Exists(rootPath)) {
            PlayerPrefs.SetString(key, null);
            if (!EditorUtility.DisplayDialog("提示", "该路径不存在:" + rootPath + ",请重新选择目录", "确定")) {
                Debug.LogErrorFormat("路径指定错误：{0}", rootPath);
                return SelectPath(key, ref path);
            }
        }

        path = rootPath;
        PlayerPrefs.SetString(key, rootPath);
        return true;
    }

    private void LoadXml(string xmlPath)
    {
        mItems.Clear();
        using var reader = XmlReader.Create(xmlPath);
        reader.ReadToFollowing("item");
        do {
            var item = new DataItem
            {
                SourcePath = reader.GetAttribute("source_path"),
                TargetPath = reader.GetAttribute("target_path")
            };

            var suffix = reader.GetAttribute("suffix");
            if (!string.IsNullOrEmpty(suffix) && !suffix.Contains("*.*"))
            {
                item.SuffixArray = suffix.Split(';');
            }
            item.SubDir = int.Parse(reader.GetAttribute("sub_dir"));
            mItems.Add(item);
        } while (reader.ReadToNextSibling("item"));
    }
    
    private void UpdateDir(string SourcePath)
    {
        foreach (var item in mItems)
        {
            var sourcePath = Path.Combine(SourcePath, item.SourcePath);
            var targetPath = Path.Combine(TargetPath, item.TargetPath);
            /*if (Directory.Exists(targetPath))
            {
                Directory.Delete(targetPath,true);
            }*/
            if (!Directory.Exists(targetPath)) {
                Directory.CreateDirectory(targetPath);
            }
            
            SyncTools.CopyFilesRecursively(new DirectoryInfo(sourcePath), new DirectoryInfo(targetPath), true, item.SuffixArray, item.SubDir);
        }
    }
}
