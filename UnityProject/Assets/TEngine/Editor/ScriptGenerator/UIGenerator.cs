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

    [MenuItem("TEngine/����UI", priority = 99)]
    static void AddWindow()
    {
        //��������
        Rect wr = new Rect(200, 200, 300, 100);
        UIGenerator window = (UIGenerator)EditorWindow.GetWindowWithRect(typeof(UIGenerator), wr, true, "����UI");
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
        m_UIPath = EditorGUILayout.TextField("����UI·��:", m_UIPath);
        m_UIName = EditorGUILayout.TextField("����UI��:", m_UIName);

        if (GUILayout.Button("����UI", GUILayout.Width(300)))
        {
            if (CheckStringEmpty(m_UIPath) || CheckStringEmpty(m_UIName))
                Debug.LogError("UI·����UI��������Ϊ��");
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
            Debug.Log("UI Prefab�ļ��Ѿ�����");
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
