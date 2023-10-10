using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

using System.Text;
using TEngine;

public class ScriptExportTool
{
    const string EXCHANGE_NAMESPACE = "[EXCHANGE_NAMESPACE]";
    const string EXCHANGE_CLASSNAME = "[EXCHANGE_CLASSNAME]";
    const string EXCHANGE_DECLARE = "[EXCHANGE_DECLARE]";
    const string EXCHANGE_ASSIGNMENT = "[EXCHANGE_ASSIGNMENT]";

    public static void ExportUIScript(string uiPath, string uiName)
    {
        StringBuilder strFile = new StringBuilder();

#if ENABLE_TEXTMESHPRO
        strFile.Append("using TMPro;\n");
#endif

        strFile.Append("using UnityEngine;\n");
        strFile.Append("using UnityEngine.UI;\n");
        strFile.Append("using TEngine;\n\n");
        strFile.Append($"namespace {SettingsUtils.GetUINameSpace()}\n");
        strFile.Append("{\n");
        strFile.Append("\t[Window(UILayer.UI)]\n");
        strFile.Append("\tpublic partial class " + uiName + " : UIWindow\n");
        strFile.Append("\t{\n");

        strFile.Append("\t}\n");
        strFile.Append("}\n");

        string exportFolder = Application.dataPath + "/GameScripts/HotFix/GameLogic/UI/" + uiPath;

        string exportPath = exportFolder + "/" + uiName + ".cs";
        if (!Directory.Exists(exportFolder))
        {
            Directory.CreateDirectory(exportFolder);
        }
        File.WriteAllText(exportPath, strFile.ToString());

        AssetDatabase.Refresh();
    }


    public static void Export(ExportPrefabData data, UnityEditor.SceneManagement.PrefabStage prefab)
    {
        if (data == null)
        {
            Debug.LogError("导出错误");
            return;
        }
        //if (data.ScriptGUID == "")
        //{
        //    EditorUtility.DisplayDialog("提示", "请选择预设对应的Lua文件", "确定");
        //    return;
        //}

        string templateTxt = GetTemplateTxt();
        if (templateTxt == null)
        {
            Debug.LogError("加载模板失败");
            return;
        }

        DoExport(data, prefab, templateTxt);
    }

    static void DoExport(ExportPrefabData data, UnityEditor.SceneManagement.PrefabStage prefab, string templateTxt)
    {
        string prefabPath = AssetDatabase.GUIDToAssetPath(data.PrefabGUID);
        string prefabName = Path.GetFileNameWithoutExtension(prefabPath);

        string scriptPath = "";

        string[] paths = AssetDatabase.FindAssets($"t:Script {prefabName}");
        Debug.Log("paths" + paths.Length);
        foreach (string pathGUID in paths)
        {
            var path = AssetDatabase.GUIDToAssetPath(pathGUID);
            Debug.Log("prefabName:" + path + " " + Path.GetFileNameWithoutExtension(path) + " " + prefabName);
            if (Path.GetFileNameWithoutExtension(path) == prefabName)
            {
                scriptPath = path;
                break;
            }
        }

        if(scriptPath == "")
        {
            Debug.LogError("未找到UI对应的代码文件，请先创建UI代码文件");
            return;
        }

        string folderPath = Path.GetDirectoryName(scriptPath).Replace("\\", "/");

        string declare;
        string assignment;
        GetWidghtAndComponent(data, prefab, out declare, out assignment);

        templateTxt = templateTxt.Replace(EXCHANGE_NAMESPACE, SettingsUtils.GetUINameSpace());
        templateTxt = templateTxt.Replace(EXCHANGE_CLASSNAME, prefabName);
        templateTxt = templateTxt.Replace(EXCHANGE_DECLARE, declare);
        templateTxt = templateTxt.Replace(EXCHANGE_ASSIGNMENT, assignment);

        Debug.Log("templateTxt:" + templateTxt);

        string exportFolder = folderPath + "/AutoGen";
        string exportPath = exportFolder + "/" + prefabName + "_AutoGen.cs";
        if (!Directory.Exists(exportFolder))
        {
            Directory.CreateDirectory(exportFolder);
        }
        File.WriteAllText(exportPath, templateTxt);

        AssetDatabase.Refresh();
    }

    static string GetTemplateTxt()
    {
        var asset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/TEngine/Editor/ScriptGenerator/Template/ScriptTemplate_AutoGen.txt");
        if (asset == null)
        {

            return null;
        }
        return asset.text;
    }

    static Dictionary<string, GameObject> goDic = new Dictionary<string, GameObject>();

    static GameObject GetGameObject(GameObjectData goData, UnityEditor.SceneManagement.PrefabStage prefab)
    {
        if (goDic.Count == 0)
        {
            var arr = prefab.prefabContentsRoot.GetComponentsInChildren<Transform>(true);
            foreach (var v in arr)
            {
                var go = v.gameObject;
                GlobalObjectId globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(go);
                string key1 = ExportPrefabData.GetKey(globalObjectId.targetPrefabId, globalObjectId.targetObjectId);
                goDic.Add(key1, go);
            }
        }

        string key = ExportPrefabData.GetKey(goData.PrefabGUID, goData.ObjectGUID);
        if (goDic.ContainsKey(key))
        {
            return goDic[key];
        }
        return null;
    }

    static void GetWidghtAndComponent(ExportPrefabData data, UnityEditor.SceneManagement.PrefabStage prefab, out string declare, out string assignment)
    {
        goDic.Clear();

        StringBuilder declareStr = new StringBuilder();
        StringBuilder assignmentStr = new StringBuilder();

        foreach (var goData in data.DataList)
        {
            GameObject go = GetGameObject(goData, prefab);
            if (go != null)
            {
                string path = GetFullPath(go.transform, prefab.prefabContentsRoot.transform);
                string goName = goData.Alias == "" ? Path.GetFileNameWithoutExtension(path) : goData.Alias;
                foreach (var v in goData.ExportComponents)
                {
                    var arr = v.Split('.');
                    string componentName = arr[arr.Length - 1];
                    if (componentName == "GameObject")
                    {
                        declareStr.Append(string.Format("\t\tGameObject {0};\n", goName));
                        assignmentStr.Append($"\t\t\t{goName} = FindChild(\"{path}\").gameObject;\n");
                    }
                    else
                    {
                        string name = GetComponentExportName(goName, componentName);

                        declareStr.Append(string.Format("\t\t{0} {1};\n", componentName, name));
                        assignmentStr.Append($"\t\t\t{name} = FindChildComponent<{componentName}>(\"{path}\");\n");
                    }
                }
            }
        }
        declare = declareStr.ToString();
        assignment = assignmentStr.ToString();
    }

    //除非gameObject.name本身已经以componentName结尾 否则以gameObject.name+componentName组合作为名字
    public static string GetComponentExportName(string goName, string componentName)
    {
        if (goName.EndsWith(componentName))
        {
            //goName已经以组件名结尾时 直接使用goName
            return goName;
        }

        return goName + componentName;
    }

    static string GetFullPath(Transform transform, Transform root)
    {
        string path = transform.name;
        Transform t = transform;
        while (t.parent != root)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

}
