using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace LazyPan {
    // 定义一个存储多个字段的类
    [System.Serializable]
    public class MyFlowData {
        public int[] SelectedIndex;//8个选项

        public MyFlowData(int[] value) {
            this.SelectedIndex = value;
        }
    }

    public class LazyPanFlow : EditorWindow {
        private bool isFoldoutTool;
        private bool isFoldoutData;
        
        private LazyPanTool _tool;
        
        //记录列表
        private ReorderableList reorderableList;

        //标题表
        private string[] names = new[] {
            "流程：",
            "生命周期：",
            "阶段：",
            "实体：",
        };

        #region 关联数据

        //流程场景关联数据
        private Dictionary<string, string> linkedFlowDictionary = new Dictionary<string, string>();
        //生命周期关联数据
        private Dictionary<string, string> linkedLifeCycleDictionary = new Dictionary<string, string>() {
            { "创建界面", "open_ui"},
            { "销毁界面", "close_ui"},
            { "创建", "load_entity" },
            { "销毁", "unload_entity" },
        };
        //阶段关联数据
        private Dictionary<string, string> linkedStageDictionary = new Dictionary<string, string>();
        //实体关联数据
        private Dictionary<string, string> linkedEntityDictionary = new Dictionary<string, string>();
        //UI关联数据
        private Dictionary<string, string> linkedUIDictionary = new Dictionary<string, string>();

        #endregion

        #region 选项数据

        private List<string> flowNameOptions = new List<string>();
        private List<string> lifeCycleNameOptions = new List<string>();
        private List<string> stageNameOptions = new List<string>();
        private List<string> entityNameOptions = new List<string>();
        private List<string> uiNameOptions = new List<string>();

        #endregion

        //流程字符串
        private string[][] FlowGenerateStr;
        //数据
        private List<MyFlowData> flowDatas = new List<MyFlowData>();
        private int _index;
        private int _itemIndex;
        
        private Dictionary<string, string> userInputStageDic = new Dictionary<string, string>();
        private string userInputNewStageName;
        private string userInputNewStageCode;

        /// <summary>
        /// 激活
        /// </summary>
        public void OnStart(LazyPanTool tool) {
            _tool = tool;
            _tool.InitScroll();
            ReadCSV.Instance.Read("FlowGenerate", out string content, out string[] lines);
            if (lines != null && lines.Length > 0) {
                FlowGenerateStr = new string[lines.Length - 2][];
                for (int i = 0; i < lines.Length; i++) {
                    if (i > 2) {
                        //遍历第三行到最后一行
                        //遍历每一行数据
                        string[] lineStr = lines[i].Split(",");
                        FlowGenerateStr[i - 3] = new string[lineStr.Length];
                        if (lineStr.Length > 0) {
                            for (int j = 0; j < lineStr.Length; j++) {
                                FlowGenerateStr[i - 3][j] = lineStr[j];
                            }
                        }
                    }
                }
            } 

            isFoldoutTool = true;
            isFoldoutData = true;
            
            InitReorderableList();
        }
        
        /// <summary>
        /// GUI
        /// </summary>
        public void OnCustomGUI(float areaX) {
            GUILayout.BeginArea(new Rect(areaX + _tool.scrollOffsetX, 60 + _tool.scrollOffsetY, Screen.width, Screen.height * 10));
            Title();
            AutoTool();
            PreviewGenerateFlowData();
            GUILayout.EndArea();
        }

        /// <summary>
        /// 初始化无序列表
        /// </summary>
        private void InitReorderableList() {
            userInputNewStageName = null;
            userInputNewStageCode = null;
            userInputStageDic.Clear();
            InitLinkDic();
            InitOptionDic();
            ReadFlowData();
            reorderableList = new ReorderableList(flowDatas, typeof(MyFlowData), true, true, true, true);
            reorderableList.drawHeaderCallback = (Rect rect) => {
                _index = 0;
                for (int i = 0; i < names.Length; i++) {
                    EditorGUI.LabelField(
                        new Rect(rect.x + 15f + Screen.width / 4 * _index++,
                            rect.y,
                            Screen.width / 4,
                            EditorGUIUtility.singleLineHeight),
                        names[i]);
                }
            };
            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                rect.y += 2.5f;
                rect.width -= 2;
                
                //宽度
                float _width = Screen.width - 10f;

                string tmpFlowSign = "";
                
                //遍历标题
                for (int i = 0; i < names.Length; i++) {
                    string[] keys = linkedLifeCycleDictionary.Keys.ToArray();
                    int tmpInd = flowDatas[index].SelectedIndex[3];
                    string lifeCycleSign = "";
                    bool isUI = false;

                    if (tmpInd > -1 && keys.Length > tmpInd) {
                        lifeCycleSign = keys[tmpInd];
                        isUI = lifeCycleSign == "创建界面" || lifeCycleSign == "销毁界面";
                    }

                    List<string> options = null;
                    options = i == 0 ? flowNameOptions : options;
                    options = i == 1 ? lifeCycleNameOptions : options;
                    options = i == 2 ? stageNameOptions : options;
                    options = i == 3 ? (isUI ? uiNameOptions : entityNameOptions) : options;

                    if (options == null) {
                        continue;
                    }

                    Color defaultColor = GUI.color;
                    
                    if (lifeCycleSign.Contains("创建")) {
                        GUI.color = new Color(226f / 255f, 224f / 255f, 212f / 255f);
                    } else if (lifeCycleSign.Contains("销毁")) {
                        GUI.color = new Color(206f / 255f, 190f / 255f, 185f / 255f);
                    } else {
                        GUI.color = new Color(148f / 255f, 168f / 255f, 179f / 255f);
                    }

                    Rect labelRect = new Rect(rect.x + _width / 4 * i,
                        rect.y,
                        _width / 4,
                        EditorGUIUtility.singleLineHeight);

                    //下拉
                    int selectIndex = EditorGUI.Popup(labelRect,
                        flowDatas[index].SelectedIndex[2 * i + 1],
                        options.ToArray());
                    
                    GUI.color = defaultColor;

                    if (flowDatas[index].SelectedIndex[2 * i + 1] != selectIndex) {
                        flowDatas[index].SelectedIndex[2 * i + 1] = selectIndex;
                        reorderableList.onChangedCallback.Invoke(reorderableList);
                    }
                }
            };

            reorderableList.onReorderCallback = (ReorderableList list) => {
                Debug.Log("元素重新排序");
            };
            reorderableList.onAddCallback = (ReorderableList list) => {
                Debug.Log("元素添加");
                int[] data = {
                    -1, -1, -1, -1, -1, -1, -1, -1
                };
                flowDatas.Add(new MyFlowData(data));
            };
            reorderableList.onRemoveCallback = (ReorderableList list) => {
                Debug.Log("元素删除触发");
                // 确保索引有效
                if (list.index >= 0 && list.index < list.list.Count) {
                    list.list.RemoveAt(list.index);  // 移除当前选中的元素
                }
            };
            reorderableList.onChangedCallback = (ReorderableList list) => {
                Debug.Log("列表发生变化");
                //csv数据自动更新
                WriteFlowData();
            };
        }

        /// <summary>
        /// 初始化关联字典
        /// </summary>
        private void InitLinkDic() {
            //流程 - 读取 SceneConfig
            linkedFlowDictionary.Clear();
            ReadCSV.Instance.Read("SceneConfig", out string sceneContent, out string[] sceneLines);
            if (sceneLines != null && sceneLines.Length > 0) {
                for (int i = 0; i < sceneLines.Length; i++) {
                    if (i > 2) {
                        string[] lineStr = sceneLines[i].Split(",");
                        linkedFlowDictionary.Add(lineStr[1], lineStr[0]);
                    }
                }
            }

            //阶段 - 读取 FlowGenerate StageCode
            linkedStageDictionary.Clear();
            ReadCSV.Instance.Read("FlowGenerate", out string flowContent, out string[] flowContents);
            if (flowContents != null && flowContents.Length > 0) {
                for (int i = 0; i < flowContents.Length; i++) {
                    if (i > 2) {
                        string[] lineStr = flowContents[i].Split(",");
                        linkedStageDictionary.TryAdd(lineStr[5], lineStr[4]);
                    }
                }
            }

            //实体 - 读取 ObjConfig
            linkedEntityDictionary.Clear();
            ReadCSV.Instance.Read("ObjConfig", out string objContent, out string[] objContents);
            if (objContents != null && objContents.Length > 0) {
                for (int i = 0; i < objContents.Length; i++) {
                    if (i > 2) {
                        string[] lineStr = objContents[i].Split(",");
                        linkedEntityDictionary.TryAdd(lineStr[3], string.Concat(lineStr[0], '|', lineStr[1]));
                    }
                }
            }
            
            linkedUIDictionary.Clear();
            ReadCSV.Instance.Read("UIConfig", out string uiContent, out string[] uiContents);
            if (uiContents != null && uiContents.Length > 0) {
                for (int i = 0; i < uiContents.Length; i++) {
                    if (i > 2) {
                        string[] lineStr = uiContents[i].Split(",");
                        linkedUIDictionary.TryAdd(lineStr[1], lineStr[0]);
                    }
                }
            }
        }

        /// <summary>
        /// 获取选项字典
        /// </summary>
        private void InitOptionDic() {
            flowNameOptions = new List<string>();
            if (linkedFlowDictionary != null) {
                foreach (var tmpFlow in linkedFlowDictionary) {
                    flowNameOptions.Add(tmpFlow.Key);
                }
            }
            
            lifeCycleNameOptions = new List<string>();
            if (linkedLifeCycleDictionary != null) {
                foreach (var tmpFlow in linkedLifeCycleDictionary) {
                    lifeCycleNameOptions.Add(tmpFlow.Key);
                }
            }
            
            stageNameOptions = new List<string>();
            if (linkedStageDictionary != null) {
                foreach (var tmpFlow in linkedStageDictionary) {
                    stageNameOptions.Add(tmpFlow.Key);
                }
            }
            
            entityNameOptions = new List<string>();
            if (linkedEntityDictionary != null) {
                foreach (var tmpFlow in linkedEntityDictionary) {
                    entityNameOptions.Add(tmpFlow.Key);
                }
            }

            uiNameOptions = new List<string>();
            if (linkedUIDictionary != null) {
                foreach (var tmpFlow in linkedUIDictionary) {
                    uiNameOptions.Add(tmpFlow.Key);
                }
            }
        }

        /// <summary>
        /// 读取流程数据
        /// </summary>
        private void ReadFlowData() {
            flowDatas.Clear();
            ReadCSV.Instance.Read("FlowGenerate", out string content, out string[] lines);
            if (lines != null && lines.Length > 0) {
                for (int i = 0; i < lines.Length; i++) {
                    if (i > 2) {
                        string[] lineStr = lines[i].Split(",");
                        if (lineStr.Length > 0) {
                            int[] selects = new int[8] {
                                -1, -1, -1, -1, -1, -1, -1, -1
                            };
                            for (int j = 0; j < lineStr.Length; j++) {
                                List<string> options = null;
                                options = j == 1 ? flowNameOptions : options;
                                options = j == 3 ? lifeCycleNameOptions : options;
                                options = j == 5 ? stageNameOptions : options;
                                options = j == 7
                                    ? (lineStr[2] == "open_ui" || lineStr[2] == "close_ui"
                                        ? uiNameOptions
                                        : entityNameOptions) : options;
                                if (options != null) {
                                    for (int k = 0; k < options.Count; k++) {
                                        if (options[k] == lineStr[j]) {
                                            selects[j] = k;
                                        }
                                    }
                                }
                            }

                            flowDatas.Add(new MyFlowData(selects));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 写入流程数据
        /// </summary>
        private void WriteFlowData() {
            ReadCSV.Instance.Read("FlowGenerate", out string content, out string[] lines);
            try {
                Queue<MyFlowData> flowDataQue = new Queue<MyFlowData>(flowDatas);
                int newLength = -1;
                for (int i = 0; i < lines.Length; i++) {
                    if (i > 2) {
                        string[] linesStr = lines[i].Split(',');
                        if (flowDataQue.Count == 0) {
                            newLength = i;
                            break;
                        }
                        MyFlowData data = flowDataQue.Dequeue();
                        if (data != null) {
                            Debug.Log("#" + data.SelectedIndex[0] + " " + data.SelectedIndex[1]);
                            string[] flowVal = linkedFlowDictionary.Values.ToArray();
                            int ind = data.SelectedIndex[1];
                            if (ind != -1 && flowVal.Length > ind) {
                                linesStr[0] = flowVal[ind];
                            } else {
                                linesStr[0] = "";
                            }

                            string[] flowKey = linkedFlowDictionary.Keys.ToArray();
                            if (ind != -1 && flowKey.Length > ind) {
                                linesStr[1] = flowKey[ind];
                            } else {
                                linesStr[1] = "";
                            }

                            ind = data.SelectedIndex[3];
                            string[] lifeCycleVal = linkedLifeCycleDictionary.Values.ToArray();
                            if (ind != -1 && lifeCycleVal.Length > ind) {
                                linesStr[2] = lifeCycleVal[ind];
                            } else {
                                linesStr[2] = "";
                            }

                            string[] lifeCycleKey = linkedLifeCycleDictionary.Keys.ToArray();
                            if (ind != -1 && lifeCycleKey.Length > ind) {
                                linesStr[3] = lifeCycleKey[ind];
                            } else {
                                linesStr[3] = "";
                            }

                            ind = data.SelectedIndex[5];
                            string[] stageVal = linkedStageDictionary.Values.ToArray();
                            if (ind != -1 && stageVal.Length > ind) {
                                linesStr[4] = stageVal[ind];
                            } else {
                                linesStr[4] = "";
                            }

                            string[] stageKey = linkedStageDictionary.Keys.ToArray();
                            if (ind != -1 && stageKey.Length > ind) {
                                linesStr[5] = stageKey[ind];
                            } else {
                                linesStr[5] = "";
                            }

                            ind = data.SelectedIndex[7];
                            if (linesStr[3] == "创建界面" || linesStr[3] == "销毁界面") {
                                string[] uiVal = linkedUIDictionary.Values.ToArray();
                                if (ind != -1 && uiVal.Length > ind) {
                                    linesStr[6] = uiVal[ind];
                                } else {
                                    linesStr[6] = "";
                                }

                                string[] uiKey = linkedUIDictionary.Keys.ToArray();
                                if (ind != -1 && uiKey.Length > ind) {
                                    linesStr[7] = uiKey[ind];
                                } else {
                                    linesStr[7] = "";
                                }
                            } else {
                                string[] entityVal = linkedEntityDictionary.Values.ToArray();
                                if (ind != -1 && entityVal.Length > ind) {
                                    linesStr[6] = entityVal[ind];
                                } else {
                                    linesStr[6] = "";
                                }

                                string[] entityKey = linkedEntityDictionary.Keys.ToArray();
                                if (ind != -1 && entityKey.Length > ind) {
                                    linesStr[7] = entityKey[ind];
                                } else {
                                    linesStr[7] = "";
                                }
                            }
                        }

                        lines[i] = string.Join(",", linesStr);
                    }
                }

                string[] newLines;
                if (newLength > -1) {
                    //需要裁剪
                    newLines = new string[newLength];
                    Array.Copy(lines, newLines, newLength);
                    ReadCSV.Instance.Write("FlowGenerate", newLines);
                } else {
                    newLines = new string[flowDataQue.Count];
                    int index = 0;
                    while (flowDataQue.Count > 0) {
                        MyFlowData data = flowDataQue.Dequeue();
                        if (data != null) {
                            Debug.Log("#" + data.SelectedIndex[0] + " " + data.SelectedIndex[1]);
                            string[] flowVal = linkedFlowDictionary.Values.ToArray();
                            string[] linesStr = new string[8];
                            int ind = data.SelectedIndex[1];
                            if (ind != -1 && flowVal.Length > ind) {
                                linesStr[0] = flowVal[ind];
                            } else {
                                linesStr[0] = "";
                            }

                            string[] flowKey = linkedFlowDictionary.Keys.ToArray();
                            if (ind != -1 && flowKey.Length > ind) {
                                linesStr[1] = flowKey[ind];
                            } else {
                                linesStr[1] = "";
                            }

                            ind = data.SelectedIndex[3];
                            string[] lifeCycleVal = linkedLifeCycleDictionary.Values.ToArray();
                            if (ind != -1 && lifeCycleVal.Length > ind) {
                                linesStr[2] = lifeCycleVal[ind];
                            } else {
                                linesStr[2] = "";
                            }

                            string[] lifeCycleKey = linkedLifeCycleDictionary.Keys.ToArray();
                            if (ind != -1 && lifeCycleKey.Length > ind) {
                                linesStr[3] = lifeCycleKey[ind];
                            } else {
                                linesStr[3] = "";
                            }

                            ind = data.SelectedIndex[5];
                            string[] stageVal = linkedStageDictionary.Values.ToArray();
                            if (ind != -1 && stageVal.Length > ind) {
                                linesStr[4] = stageVal[ind];
                            } else {
                                linesStr[4] = "";
                            }

                            string[] stageKey = linkedStageDictionary.Keys.ToArray();
                            if (ind != -1 && stageKey.Length > ind) {
                                linesStr[5] = stageKey[ind];
                            } else {
                                linesStr[5] = "";
                            }

                            ind = data.SelectedIndex[7];
                            if (linesStr[3] == "创建界面" || linesStr[3] == "销毁界面") {
                                string[] uiVal = linkedUIDictionary.Values.ToArray();
                                if (ind != -1 && uiVal.Length > ind) {
                                    linesStr[6] = uiVal[ind];
                                } else {
                                    linesStr[6] = "";
                                }

                                string[] uiKey = linkedUIDictionary.Keys.ToArray();
                                if (ind != -1 && uiKey.Length > ind) {
                                    linesStr[7] = uiKey[ind];
                                } else {
                                    linesStr[7] = "";
                                }
                            } else {
                                string[] entityVal = linkedEntityDictionary.Values.ToArray();
                                if (ind != -1 && entityVal.Length > ind) {
                                    linesStr[6] = entityVal[ind];
                                } else {
                                    linesStr[6] = "";
                                }

                                string[] entityKey = linkedEntityDictionary.Keys.ToArray();
                                if (ind != -1 && entityKey.Length > ind) {
                                    linesStr[7] = entityKey[ind];
                                } else {
                                    linesStr[7] = "";
                                }
                            }

                            newLines[index] = string.Join(",", linesStr);
                            index++;
                        }
                    }
                    ReadCSV.Instance.Write("FlowGenerate", lines.Concat(newLines).ToArray());
                }
            } catch {
                Debug.LogError("录入错误");
            }
        }

        private void PreviewGenerateFlowData() {
            isFoldoutData = EditorGUILayout.Foldout(isFoldoutData, " 预览流程自动化数据", true);
            Rect rect = GUILayoutUtility.GetLastRect();
            float height = 0;
            if (isFoldoutData) {
                ExpandFlowData();
                height += GUILayoutUtility.GetLastRect().height;
                GUILayout.BeginHorizontal();
                float width = Screen.width - 10f;
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("阶段注释:", GUILayout.Width(50));
                userInputNewStageName = EditorGUILayout.TextField(userInputNewStageName, GUILayout.Width(width / 3 - 50));
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("阶段标识:", GUILayout.Width(50));
                userInputNewStageCode = EditorGUILayout.TextField(userInputNewStageCode, GUILayout.Width(width / 3 - 50));
                GUILayout.EndHorizontal();
                
                if (GUILayout.Button("录入新阶段", GUILayout.Width(width / 3))) {
                    if (userInputNewStageName != null
                        && userInputNewStageCode != null
                        && !userInputStageDic
                            .ContainsKey(userInputNewStageName)
                        && userInputNewStageName != "初始化" &&
                        userInputNewStageName != "清除") {
                        userInputStageDic.Add(userInputNewStageName, userInputNewStageCode);
                        linkedStageDictionary.TryAdd(userInputNewStageName, userInputNewStageCode);
                        stageNameOptions.Add(userInputNewStageName);
                        userInputNewStageName = null;
                        userInputNewStageCode = null;
                    }
                }
                GUILayout.EndHorizontal();
                height += GUILayoutUtility.GetLastRect().height;
            } else {
                GUILayout.Space(10);
            }
            LazyPanTool.DrawBorder(new Rect(rect.x + 2f, rect.y - 2f, rect.width - 2f, rect.height + height + 5f), Color.white);
        }

        private void AutoTool() {
            isFoldoutTool = EditorGUILayout.Foldout(isFoldoutTool, " 自动化工具", true);
            Rect rect = GUILayoutUtility.GetLastRect();
            float height = 0;
            if (isFoldoutTool) {
                GUILayout.Label("");
                height += GUILayoutUtility.GetLastRect().height;
                GUILayout.BeginHorizontal();
                GUIStyle style = LazyPanTool.GetGUISkin("AButtonGUISkin").GetStyle("button");
                if(GUILayout.Button("打开流程配置表 Csv", style)) {
                    OpenFlowCsv();
                }
                GUILayout.EndHorizontal();
                height += GUILayoutUtility.GetLastRect().height;
            } else {
                GUILayout.Space(10);
            }

            LazyPanTool.DrawBorder(new Rect(rect.x + 2f, rect.y - 2f, rect.width - 2f, rect.height + height + 5f), Color.white);

            GUILayout.Space(10);
        }

        private void Title() {
            GUILayout.BeginHorizontal();
            GUIStyle style = LazyPanTool.GetGUISkin("LogoGUISkin").GetStyle("label");
            GUILayout.Label("FLOW", style);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            style = LazyPanTool.GetGUISkin("AnnotationGUISkin").GetStyle("label");
            GUILayout.Label("@流程 游戏内主流程", style);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
        }

        private void ExpandFlowData() {
            reorderableList.DoLayoutList();
        }

        private void OpenFlowCsv() {
            string filePath = Application.dataPath + "/StreamingAssets/Csv/FlowGenerate.csv";
            Process.Start(filePath);
        }
    }
}