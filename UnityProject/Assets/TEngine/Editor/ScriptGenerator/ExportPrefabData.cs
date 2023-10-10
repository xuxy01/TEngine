using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;

public class ExportPrefabData : ScriptableObject
{
    public string PrefabGUID;
    public string ScriptGUID;
    public List<GameObjectData> DataList = new List<GameObjectData>();

    private Dictionary<string, GameObjectData> ObjDic = null;

    public static string GetDataPath(string GUID)
    {
        string assetPath = AssetDatabase.GUIDToAssetPath(GUID);
        string assetName = Path.GetFileNameWithoutExtension(assetPath);
        string dataPath = string.Format("Assets/TEngine/Editor/ScriptGenerator/Data/{0}.asset", assetName);
        return dataPath;
    }

    public static ExportPrefabData GetData(string GUID)
    {
        string dataPath = GetDataPath(GUID);
        if (File.Exists(dataPath))
        {
            return AssetDatabase.LoadAssetAtPath<ExportPrefabData>(dataPath);
        }
        else
        {
            var instance = ScriptableObject.CreateInstance<ExportPrefabData>();
            instance.PrefabGUID = GUID;
            AssetDatabase.CreateAsset(instance, dataPath);
            return instance;
        }
    }

    public static string GetKey(ulong prefabGUID, ulong objectGUID)
    {
        return string.Format("{0}_{1}", prefabGUID, objectGUID);
    }

    private Dictionary<string, GameObjectData> GetObjDic()
    {
        if(ObjDic == null)
        {
            ObjDic = new Dictionary<string, GameObjectData>();
            foreach(var v in DataList)
            {
                ObjDic.Add(GetKey(v.PrefabGUID, v.ObjectGUID), v);
            }
        }
        return ObjDic;
    }

    private bool IsExist(ulong prefabGUID, ulong objectGUID)
    {
        string key = GetKey(prefabGUID, objectGUID);
        var dic = GetObjDic();
        return dic.ContainsKey(key);
    }

    private GameObjectData GetDataObjectData(ulong prefabGUID, ulong objectGUID, bool create = false)
    {
        string key = GetKey(prefabGUID, objectGUID);
        var dic = GetObjDic();
        if (dic.ContainsKey(key))
        {
            return dic[key];
        }

        if (create)
        {
            var data = new GameObjectData();
            data.PrefabGUID = prefabGUID;
            data.ObjectGUID = objectGUID;
            dic.Add(key, data);
            DataList.Add(data);
            return data;
        }
        return null;
    }

    public bool IsComponentExport(ulong prefabGUID, ulong objectGUID, string componentName)
    {
        var data = GetDataObjectData(prefabGUID, objectGUID);
        if (data == null)
        {
            return false;
        }
        return data.ExistComponent(componentName);
    }

    public void SetComponentExport(ulong prefabGUID, ulong objectGUID, string componet)
    {
        if (IsExist(prefabGUID, objectGUID) || componet != "")
        {
            var data = GetDataObjectData(prefabGUID, objectGUID, true);
            data.SetComponent(componet);
            EditorUtility.SetDirty(this);
            RemoveUnused(data);
        }
    }

    public void SetComponentExport(ulong prefabGUID, ulong objectGUID, string[] componets)
    {
        if(IsExist(prefabGUID, objectGUID) || componets.Length != 0)
        {
            var data = GetDataObjectData(prefabGUID, objectGUID, true);
            data.SetComponents(componets);
            EditorUtility.SetDirty(this);
            RemoveUnused(data);
        }
    }

    public string[] GetComponentExport(ulong prefabGUID, ulong objectGUID)
    {
        var data = GetDataObjectData(prefabGUID, objectGUID);
        if (data == null)
        {
            return null;
        }
        return data.ExportComponents.ToArray();
    }

    public string GetGameObjectAlias(ulong prefabGUID, ulong objectGUID)
    {
        var data = GetDataObjectData(prefabGUID, objectGUID);
        if (data == null)
        {
            return "";
        }
        return data.Alias;
    }

    public void SetGameObjectAlias(ulong prefabGUID, ulong objectGUID, string alias)
    {
        if(IsExist(prefabGUID, objectGUID) || alias != "")
        {
            var data = GetDataObjectData(prefabGUID, objectGUID, true);
            data.Alias = alias;
            EditorUtility.SetDirty(this);
            RemoveUnused(data);
        }
    }

    public UnityEngine.Object GetLuaObject()
    {
        string path = AssetDatabase.GUIDToAssetPath(ScriptGUID);
        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        return obj;
    }

    public void SetLuaObject(UnityEngine.Object luaObj)
    {
        string path = AssetDatabase.GetAssetPath(luaObj);
        ScriptGUID = AssetDatabase.AssetPathToGUID(path);
        EditorUtility.SetDirty(this);
    }

    public void RemoveUnused(GameObjectData data)
    {
        if (data.IsRemovable())
        {
            DataList.Remove(data);
            ObjDic.Remove(data.GetKey());
        }
    }

    public void Clear()
    {
        DataList.Clear();
        ObjDic.Clear();
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }

    public void Save()
    {
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }
}

[Serializable]
public class GameObjectData
{
    public ulong PrefabGUID = 0;
    public ulong ObjectGUID = 0;
    public string Alias = "";
    public List<string> ExportComponents = new List<string>();

    public bool ExistComponent(string component)
    {
        return ExportComponents.Contains(component);
    }

    public void SetComponents(string[] components)
    {
        ExportComponents.Clear();
        ExportComponents.AddRange(components);
    }

    public void SetComponent(string component)
    {
        if(component != "")
        {
            if (ExportComponents.Contains(component))
            {
                ExportComponents.Remove(component);
            }
            else
            {
                ExportComponents.Add(component);
            }
        }

    }

    public bool IsRemovable()
    {
        if (Alias == "" && ExportComponents.Count == 0)
        {
            return true;
        }
        return false;
    }

    public string GetKey()
    {
        return ExportPrefabData.GetKey(PrefabGUID, ObjectGUID);
    }
}
