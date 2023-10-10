using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;

public class UIGenerator : EditorWindow
{
    private string m_UIPath = "";
    private string m_UIName = "";

    [MenuItem("TEngine/创建UI", priority = 99)]
    static void AddWindow()
    {
        //创建窗口
        Rect wr = new Rect(200, 200, 300, 100);
        UIGenerator window = (UIGenerator)EditorWindow.GetWindowWithRect(typeof(UIGenerator), wr, true, "创建UI");
        window.Show();
    }

    bool CheckStringEmpty(string str)
    {
        if (str == null || str.Equals("") || str.Equals(string.Empty))
            return true;

        return false;
    }

    void OnGUI()
    {
        m_UIPath = EditorGUILayout.TextField("输入UI路径:", m_UIPath);
        m_UIName = EditorGUILayout.TextField("输入UI名:", m_UIName);

        if (GUILayout.Button("创建UI", GUILayout.Width(300)))
        {
            if (CheckStringEmpty(m_UIPath) || CheckStringEmpty(m_UIName))
                Debug.LogError("UI路径和UI名都不能为空");
            else
            {
                CreateUIPrefab();
                CreateScript();
                CreateAtlasFolder();
                AssetDatabase.Refresh();
            }
        }
    }

    private void CreateScript()
    {
        ScriptExportTool.ExportUIScript(m_UIPath, m_UIName);
    }

    private void CreateUIPrefab()
    {

        string path = "Assets/AssetRaw/UIRaw/UI/";
        string dirPath = path + "/" + m_UIPath + "/";
        string name = m_UIName + ".prefab";
        string filePath = dirPath + name;
        

        if (File.Exists(filePath))
        {
            Debug.Log("UI Prefab文件已经存在");
            return;
        }
        
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        FileUtil.CopyFileOrDirectory("Assets/AssetRaw/UIRaw/UI/TemplateUI.prefab", filePath);
    }
    private void CreateAtlasFolder()
    {
        string path = Application.dataPath + "/AssetRaw/UIRaw/Atlas/" + m_UIPath  + "/"+ m_UIName;

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

}
