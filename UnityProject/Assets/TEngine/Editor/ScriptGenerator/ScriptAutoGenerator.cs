using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using System.Reflection;
using System;

namespace TEngine.Editor.UI
{
    [InitializeOnLoad]
    public class ScriptAutoGenerator
    {
        static ScriptAutoGenerator()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyWindowItemGUI;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemGUI;
        }

        static ExportPrefabData exportPrefabData = null;
        static int currentInstanceId = 0;
        static bool isOn = true;
        static int currentPrefabInstanceId = 0;
        static int inputInstanceId = 0;
        static Rect inputRect;
        static void InitData(string GUID)
        {
            if (exportPrefabData == null)
            {
                exportPrefabData = ExportPrefabData.GetData(GUID);
            }
            else
            {
                if (exportPrefabData.PrefabGUID != GUID)
                {
                    exportPrefabData = ExportPrefabData.GetData(GUID);
                }
            }
        }


        static void OnHierarchyWindowItemGUI(int instanceID, Rect selectionRect)
        {
            var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage == null)
            {
                //仅在预设模式下可用
                if (exportPrefabData != null)
                {
                    exportPrefabData.Save();
                    exportPrefabData = null;
                }
                return;
            }

            if (prefabStage.prefabContentsRoot.transform.parent != null && prefabStage.prefabContentsRoot.transform.parent.gameObject.GetInstanceID() == instanceID)
            {
                return;
            }

            if (prefabStage.prefabContentsRoot.GetInstanceID() == instanceID)
            {
                DrawMenu(prefabStage, selectionRect);
            }
            else if (isOn)
            {
                DrawItem(instanceID, selectionRect);
            }
        }

        static void DrawMenu(UnityEditor.SceneManagement.PrefabStage prefabStage, Rect selectionRect)
        {
            if (prefabStage.prefabContentsRoot.GetInstanceID() == currentPrefabInstanceId)
            {

                Rect rect = new Rect(selectionRect.x + selectionRect.width, selectionRect.y, 20, selectionRect.height);
                isOn = GUI.Toggle(rect, isOn, "");

                if (isOn)
                {
                    InitData(AssetDatabase.AssetPathToGUID(prefabStage.assetPath));
                    //rect = new Rect(selectionRect.x + selectionRect.width - 140, selectionRect.y, 136, selectionRect.height);
                    //var luaObj = EditorGUI.ObjectField(rect, GetLuaObject(), typeof(UnityEngine.TextAsset), false);
                    //SetLuaObject(luaObj);

                    if (selectionRect.Contains(Event.current.mousePosition))
                    {
                        //右键
                        if (Event.current.type == EventType.MouseUp && Event.current.button == 1)
                        {
                            GenericMenu menu = new GenericMenu();
                            menu.AddItem(new GUIContent("生成Script_AutoGen"), false, GenerateAutoScript, prefabStage);
                            menu.AddSeparator("");
                            menu.AddItem(new GUIContent("清除所有组件"), false, ClearAllComponents);
                            menu.ShowAsContext();
                        }
                    }
                }
            }
            else
            {
                currentPrefabInstanceId = prefabStage.prefabContentsRoot.GetInstanceID();
                isOn = false;
            }
        }

        static void GenerateAutoScript(object prefabStage)
        {
            exportPrefabData.Save();
            ScriptExportTool.Export(exportPrefabData, prefabStage as UnityEditor.SceneManagement.PrefabStage);
        }

        static void ClearAllComponents()
        {
            if (EditorUtility.DisplayDialog("警告", "是否清除该预设所有组件导出信息?", "删除", "取消"))
            {
                exportPrefabData.Clear();
            }

        }

        static void DrawItem(int instanceID, Rect selectionRect)
        {
            GameObject target = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            GlobalObjectId globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(target);

            ulong prefabGUID = globalObjectId.targetPrefabId;
            ulong objectGUID = globalObjectId.targetObjectId;

            Rect rect;

            string alias = GetAlias(prefabGUID, objectGUID);
            rect = new Rect(selectionRect.x + selectionRect.width - 20, selectionRect.y, 20, selectionRect.height);
            GUIContent aliasContent = EditorGUIUtility.IconContent(alias == "" ? "sv_icon_dot6_pix16_gizmo" : "sv_icon_dot1_pix16_gizmo");
            aliasContent.tooltip = GetAlias(prefabGUID, objectGUID);
            if (GUI.Button(rect, aliasContent, new GUIStyle("label")))
            {
                //显示输入框
                inputInstanceId = instanceID;
                inputRect = selectionRect;
            }

            string[] selectedComponents = GetComponentsExport(prefabGUID, objectGUID);
            if (selectedComponents != null)
            {
                for (int i = 0; i < selectedComponents.Length; i++)
                {
                    string component = selectedComponents[i];
                    rect = new Rect(selectionRect.x + selectionRect.width - 54 - i * 14, selectionRect.y, 20, selectionRect.height);
                    if (GUI.Button(rect, GetComponentIcon(component), new GUIStyle("label")))
                    {
                        string goName = alias == "" ? target.name : alias;
                        CopyComponentPath(goName, component);
                    }
                }
            }

            if (selectionRect.Contains(Event.current.mousePosition) || currentInstanceId == instanceID)
            {
                currentInstanceId = instanceID;
                rect = new Rect(selectionRect.x + selectionRect.width - 36, selectionRect.y, 20, selectionRect.height);
                if (GUI.Button(rect, EditorGUIUtility.IconContent("d_AnimationWrapModeMenu"), "label"))
                {
                    var components = GetComponents(target);

                    ShowMenu(prefabGUID, objectGUID, components);
                }
            }

            DrawInput(instanceID);
        }

        static void ShowMenu(ulong prefabFileId, ulong fileId, string[] components)
        {
            GenericMenu menu = new GenericMenu();
            for (int i = 0; i < components.Length; i++)
            {
                string component = components[i];
                menu.AddItem(new GUIContent(component), IsComponentSelected(prefabFileId, fileId, component), OnMenuItem, component);
            }

            menu.ShowAsContext();
        }

        static void CopyComponentPath(string goName, string component)
        {
            string[] arr = component.Split('.');
            string componentName = arr[arr.Length - 1];
            string copyTxt;
            if (componentName == "GameObject")
            {
                copyTxt = "self.widgetTable." + goName;
            }
            else
            {
                copyTxt = "self.componentTable." + ScriptExportTool.GetComponentExportName(goName, componentName);
            }
            Debug.Log("复制组件引用成功:" + copyTxt);
            GUIUtility.systemCopyBuffer = copyTxt;
        }

        static void OnMenuItem(object obj)
        {
            string component = obj as string;

            GameObject target = EditorUtility.InstanceIDToObject(currentInstanceId) as GameObject;

            GlobalObjectId globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(target);

            ulong prefabGUID = globalObjectId.targetPrefabId;
            ulong objectGUID = globalObjectId.targetObjectId;
            SetComponentsSelected(prefabGUID, objectGUID, component);
        }

        static void DrawInput(int instanceID)
        {
            if (inputInstanceId != 0 && inputInstanceId == instanceID)
            {
                GameObject target = EditorUtility.InstanceIDToObject(inputInstanceId) as GameObject;

                GlobalObjectId globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(target);

                ulong prefabGUID = globalObjectId.targetPrefabId;
                ulong objectGUID = globalObjectId.targetObjectId;
                string alias = GetAlias(prefabGUID, objectGUID);
                Rect rect = new Rect(inputRect.x + 160, inputRect.y, inputRect.width - 160, inputRect.height);
                string newAlias = EditorGUI.TextField(rect, alias);// GUI.TextField(rect, alias);
                SetAlias(prefabGUID, objectGUID, newAlias);

                if ((Event.current.type == EventType.KeyUp && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)) || (Event.current.type == EventType.MouseUp && Event.current.button == 0 && !rect.Contains(Event.current.mousePosition)))
                {
                    inputInstanceId = 0;
                    EditorWindow.focusedWindow.Repaint();
                }
            }
        }

        static bool IsComponentSelected(ulong prefabFileId, ulong fileId, string component)
        {
            return exportPrefabData.IsComponentExport(prefabFileId, fileId, component);
        }

        static void SetComponentsSelected(ulong prefabFileId, ulong fileId, string component)
        {
            exportPrefabData.SetComponentExport(prefabFileId, fileId, component);
        }

        static string[] GetComponentsExport(ulong prefabFileId, ulong fileId)
        {
            return exportPrefabData.GetComponentExport(prefabFileId, fileId);
        }

        static string[] GetComponents(GameObject go)
        {
            List<string> list = new List<string>();
            list.Add("UnityEngine.GameObject");
            Component[] components = go.GetComponents<Component>();
            foreach (var v in components)
            {

                list.Add(v.GetType().ToString());
            }
            return list.ToArray();
        }

        static GUIContent GetComponentIcon(string component)
        {

            string[] arr = component.Split('.');
            string name = arr[arr.Length - 1];
            GUIContent result;
            if (component.Contains("UnityEngine"))
            {
                result = EditorGUIUtility.IconContent(name + " Icon");
                result.tooltip = name;
            }
            else
            {
                result = EditorGUIUtility.IconContent("cs Script Icon");
                result.tooltip = name;
            }

            return result;
        }

        static UnityEngine.Object GetLuaObject()
        {
            return exportPrefabData.GetLuaObject();
        }

        static void SetLuaObject(UnityEngine.Object luaObj)
        {
            exportPrefabData.SetLuaObject(luaObj);
        }

        static void SetAlias(ulong prefabFileId, ulong fileId, string alias)
        {
            exportPrefabData.SetGameObjectAlias(prefabFileId, fileId, alias);
        }

        static string GetAlias(ulong prefabFileId, ulong fileId)
        {
            return exportPrefabData.GetGameObjectAlias(prefabFileId, fileId);
        }
    }
}
