using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LazyPan {
    // 定义一个存储多个字段的类
    [System.Serializable]
    public class MyEntityData {
        public string[] Infos;
        public int SceneIndexs;
        public List<string> BindBehaviour;
        public bool[] BehaviourIndexs;
        public int OperationIndex;

        public MyEntityData(string[] infos, int SceneIndexs, List<string> bindBehaviour, bool[] behaviourIndexs) {
            this.Infos = infos;
            this.SceneIndexs = SceneIndexs;
            this.BindBehaviour = bindBehaviour;
            this.BehaviourIndexs = behaviourIndexs;
        }
    }

    public class LazyPanEntity : EditorWindow {
        private string _instanceFlowName;
        private string _instanceTypeName;
        private string _instanceObjName;
        private string _instanceObjChineseName;
        
        private string[][] _entityConfig;//实体配置
        private string[] behaviourNames;//行为名称数组

        private bool[] selectedOptions;

        private int _index;

        private Dictionary<string, string> linkedSceneDictionary = new Dictionary<string, string>();
        private List<string> sceneNameOptions = new List<string>();

        private Dictionary<string, string> linkedBehaviourDictionary = new Dictionary<string, string>();
        private List<string> behaviourNameOptions = new List<string>();

        private List<string> operationNameOptions = new List<string>();

        //标题表
        private string[] names = new[] {
            "标识：",
            "流程：",
            "类型：",
            "名称：",
            "创建绑定的位置信息：",
            "创建绑定行为名字：",
            "操作",
        };

        //数据
        private List<MyEntityData> entityDatas = new List<MyEntityData>();
        //模糊搜索数据
        private List<MyEntityData> fuzzyEntityDatas = new List<MyEntityData>();
        //记录列表
        private ReorderableList reorderableList;

        private bool isFoldoutTool;
        private bool isFoldoutData;
        private bool isFoldoutGenerate;
        
        //模糊搜索
        private string UserInput;
        private bool isFocused;
        private string placeholder = "模糊搜索请输入关键字";
        private string textName = "模糊搜索控件";

        private LazyPanTool _tool;

        public void OnStart(LazyPanTool tool) {
            _tool = tool;
            _tool.InitScroll();

            #region 实体配置

            //实体配置数组
            ReadCSV.Instance.Read("ObjConfig", out string content, out string[] lines);
            if (lines != null && lines.Length > 0) {
                _entityConfig = new string[lines.Length - 3][];
                for (int i = 0; i < lines.Length; i++) {
                    if (i > 2) {
                        string[] lineStr = lines[i].Split(",");
                        _entityConfig[i - 3] = new string[lineStr.Length];
                        if (lineStr.Length > 0) {
                            for (int j = 0; j < lineStr.Length; j++) {
                                _entityConfig[i - 3][j] = lineStr[j];
                            }
                        }
                    }
                }
            }

            #endregion

            #region 行为配置

            //读取行为配置
            ReadCSV.Instance.Read("BehaviourConfig", out content, out lines);
            string[][] _behaviourConfig = new string[lines.Length - 3][];
            if (lines != null && lines.Length > 0) {
                for (int i = 0; i < lines.Length; i++) {
                    if (i > 2) {
                        string[] lineStr = lines[i].Split(",");
                        _behaviourConfig[i - 3] = new string[lineStr.Length];
                        if (lineStr.Length > 0) {
                            for (int j = 0; j < lineStr.Length; j++) {
                                _behaviourConfig[i - 3][j] = lineStr[j];
                            }
                        }
                    }
                }

                //获取行为名称
                behaviourNames = new string[_behaviourConfig.Length];
                for (int i = 0; i < _behaviourConfig.Length; i++) {
                    string[] tmpStr = _behaviourConfig[i];
                    for (int j = 0; j < tmpStr.Length; j++) {
                        if (j == 1) {
                            behaviourNames[i] = tmpStr[j];
                        }
                    }
                }
            }

            #endregion

            isFoldoutTool = true;
            isFoldoutData = true;
            isFoldoutGenerate = true;
            
            InitReorderableList("");
        }

        public void OnCustomGUI(float areaX) {
            GUILayout.BeginArea(new Rect(areaX + _tool.scrollOffsetX, 60 + _tool.scrollOffsetY, Screen.width, Screen.height * 10));
            Title();
            AutoTool();
            PreviewEntityConfigData();
            ManualGeneratePrefabTool();
            GUILayout.EndArea();
        }

        /// <summary>
        /// 初始化无序列表
        /// </summary>
        private void InitReorderableList(string fuzzyContent) {
            InitLinkDic();
            InitOptionDic();
            ReadEntityData(fuzzyContent);
            reorderableList = new ReorderableList(entityDatas, typeof(MyEntityData), true, true, true, true);
            //头部标题
            reorderableList.drawHeaderCallback = (Rect rect) => {
                _index = 0;
                for (int i = 0; i < names.Length; i++) {
                    EditorGUI.LabelField(
                        new Rect(rect.x + 15f + Screen.width / 7 * _index++,
                            rect.y,
                            Screen.width / 7,
                            EditorGUIUtility.singleLineHeight),
                        names[i]);
                }
            };
            //绘制无序列表
            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                rect.y += 2.5f;
                rect.width -= 2;
                
                //宽度
                float _width = Screen.width - 10f;
                string tmpFlowSign = "";
                
                //遍历标题
                for (int i = 0; i < names.Length; i++) {
                    Rect labelRect = new Rect(rect.x + _width / 7 * i,
                        rect.y,
                        _width / 7,
                        EditorGUIUtility.singleLineHeight);
                    if (i == 1) {
                        labelRect.height = 18f;
                        int selectIndex = EditorGUI.Popup(labelRect, entityDatas[index].SceneIndexs, sceneNameOptions.ToArray());
                        if (entityDatas[index].SceneIndexs != selectIndex) {
                            entityDatas[index].SceneIndexs = selectIndex;
                            string sceneName = sceneNameOptions[selectIndex];
                            entityDatas[index].Infos[i] = sceneName;
                            reorderableList.onChangedCallback.Invoke(reorderableList);
                        }
                    } else if (i < 5) {
                        // 设置Label的矩形区域
                        string textField = EditorGUI.TextField(labelRect, entityDatas[index].Infos[i], EditorStyles.wordWrappedLabel);
                        if (entityDatas[index].Infos[i] != textField) {
                            entityDatas[index].Infos[i] = textField;
                            reorderableList.onChangedCallback.Invoke(reorderableList);
                        }
                    } else if (i == 5) {
                        //行为按钮的文本
                        string behaviourBindNames = "";
                        List<string> bindBehaviour = entityDatas[index].BindBehaviour;
                        for (int k = 0 ; k < bindBehaviour.Count ; k++) {
                            behaviourBindNames += bindBehaviour[k];
                            if (k != bindBehaviour.Count - 1) {
                                behaviourBindNames += ",";
                            }
                        }

                        //按钮
                        labelRect.height = 18f;
                        if (GUI.Button(labelRect, behaviourBindNames)) {
                            selectedOptions = entityDatas[index].BehaviourIndexs;
                            GenericMenu menu = new GenericMenu();
                            //遍历所有配置的行为名
                            for (int j = 0; j < behaviourNames.Length; j++) {
                                int tmpIndex = j;
                                string tmpBehaviourName = behaviourNames[j];
                                bool isSelected = entityDatas[index].BindBehaviour.Contains(tmpBehaviourName);
                                selectedOptions[j] = isSelected;
                                menu.AddItem(new GUIContent(tmpBehaviourName), isSelected, () => ToggleLayerSelection(entityDatas[index], tmpIndex, entityDatas[index].Infos[1], entityDatas[index].Infos[0], tmpBehaviourName));
                            }
                            Rect buttonRect = labelRect; // 使用原始的labelRect
                            Vector2 menuPosition = new Vector2(buttonRect.xMin, buttonRect.yMax);
                            menu.DropDown(new Rect(menuPosition, Vector2.zero));
                        }
                    } else if (i == 6) {
                        RefreshOperationNameOptions(IsExitPrefab(entityDatas[index]));

                        labelRect.height = 18f;
                        int selectIndex = EditorGUI.Popup(labelRect, entityDatas[index].OperationIndex, operationNameOptions.ToArray());
                        if (entityDatas[index].OperationIndex != selectIndex) {
                            entityDatas[index].OperationIndex = 0;
                            string operationName = operationNameOptions[selectIndex];
                            Operation(operationName, entityDatas[index].Infos);
                        }
                    }
                }
            };
            reorderableList.onReorderCallback = (ReorderableList list) => {
                Debug.Log("元素重新排序");
            };
            reorderableList.onAddCallback = (ReorderableList list) => {
                Debug.Log("元素添加");
                string[] infos = new string[]{"", "", "", "", "", ""};
                int sceneIndex = -1;
                List<string> bindBehaviour = new List<string>();
                bool[] behaviourName = new bool[behaviourNames.Length];
                entityDatas.Add(new MyEntityData(infos, sceneIndex, bindBehaviour, behaviourName));
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
                WriteEntityData();
            };
        }

        private void Operation(string operationName, string[] infos) {
            GameObject prefab = GetPrefab(infos[1], infos[0], out string flowCode);
            if (operationName == "跳转到预制体") {
                //点击的实体如果在实体配置存在直接跳转 如果没有游戏物体创建
                if (prefab != null) {
                    Selection.activeObject = prefab;
                    EditorGUIUtility.PingObject(prefab);
                }
            } else if (operationName == "创建预制体") {
                _instanceFlowName = flowCode;
                _instanceTypeName = infos[2];
                _instanceObjName = infos[0].Split("_")[2];
                _instanceObjChineseName = infos[3];
                GUI.FocusControl("objChineseName");
            } else if (operationName == "打印绑定数据到日志") {
                DisplayEntityData(prefab, infos);
            }
        }

        private void DisplayEntityData(GameObject prefab, string[] infos) {
            Data data = prefab.GetComponent<Data>();
            // 设置每列的固定宽度
            int signWidth = 30; // Sign列的宽度
            int descriptionWidth = 30; // Description列的宽度
            int valueWidth = 30; // 数据列的宽度（Bool, Float, Int, String, Vector3）
            int titleWidth = 40;

            // 计算分隔符的统一长度
            int totalWidth = signWidth + descriptionWidth + valueWidth + 6; // 6 是额外的空白和间隔

            string log = "";

            // 打印标题
            log += new string('=', titleWidth) + "\n"; // 使用等号生成统一长度的分隔线
            log += $"              {infos[1]} {infos[0]} 数据展示\n";
            log += new string('=', titleWidth) + "\n";
            log += "\n"; // 增加空行

            // 打印 Bool 数据
            log += "\n[ Bools ]\n"; // 在类别前加空行
            log += new string('-', totalWidth / 2) + "\n"; // 使用短横线作为每个数据类别的分隔符
            log += string.Format("{0,-" + signWidth + "} {1,-" + descriptionWidth + "} {2,-" + valueWidth + "}\n",
                "Sign", "Description", "Value");
            log += new string('-', totalWidth / 2) + "\n";
            foreach (var tmp in data.Bools) {
                log += string.Format(
                    "{0,-" + signWidth + "} {1,-" + descriptionWidth + "} {2,-" + valueWidth + "}\n",
                    tmp.Sign, tmp.Description, tmp.Bool);
            }

            log += "\n"; // 在数据后增加空行

            // 打印 Float 数据
            log += "\n[ Floats ]\n"; // 在类别前加空行
            log += new string('-', totalWidth / 2) + "\n";
            log += string.Format("{0,-" + signWidth + "} {1,-" + descriptionWidth + "} {2,-" + valueWidth + "}\n",
                "Sign", "Description", "Value");
            log += new string('-', totalWidth / 2) + "\n";
            foreach (var tmp in data.Floats) {
                log += string.Format(
                    "{0,-" + signWidth + "} {1,-" + descriptionWidth + "} {2,-" + valueWidth + "}\n",
                    tmp.Sign, tmp.Description, tmp.Float);
            }

            log += "\n"; // 在数据后增加空行

            // 打印 Int 数据
            log += "\n[ Integers ]\n"; // 在类别前加空行
            log += new string('-', totalWidth / 2) + "\n";
            log += string.Format("{0,-" + signWidth + "} {1,-" + descriptionWidth + "} {2,-" + valueWidth + "}\n",
                "Sign", "Description", "Value");
            log += new string('-', totalWidth / 2) + "\n";
            foreach (var tmp in data.Ints) {
                log += string.Format(
                    "{0,-" + signWidth + "} {1,-" + descriptionWidth + "} {2,-" + valueWidth + "}\n",
                    tmp.Sign, tmp.Description, tmp.Int);
            }

            log += "\n"; // 在数据后增加空行

            // 打印 String 数据
            log += "\n[ Strings ]\n"; // 在类别前加空行
            log += new string('-', totalWidth / 2) + "\n";
            log += string.Format("{0,-" + signWidth + "} {1,-" + descriptionWidth + "} {2,-" + valueWidth + "}\n",
                "Sign", "Description", "Value");
            log += new string('-', totalWidth / 2) + "\n";
            foreach (var tmp in data.Strings) {
                log += string.Format(
                    "{0,-" + signWidth + "} {1,-" + descriptionWidth + "} {2,-" + valueWidth + "}\n",
                    tmp.Sign, tmp.Description, tmp.String);
            }

            log += "\n"; // 在数据后增加空行

            // 打印 Vector3 数据
            log += "\n[ Vector3s ]\n"; // 在类别前加空行
            log += new string('-', totalWidth / 2) + "\n";
            log += string.Format("{0,-" + signWidth + "} {1,-" + descriptionWidth + "} {2,-" + valueWidth + "}\n",
                "Sign", "Description", "Value");
            log += new string('-', totalWidth / 2) + "\n";
            foreach (var tmp in data.Vector3s) {
                log += string.Format(
                    "{0,-" + signWidth + "} {1,-" + descriptionWidth + "} {2,-" + valueWidth + "}\n",
                    tmp.Sign, tmp.Description, tmp.Vector3);
            }

            // 打印结束标记
            Debug.Log(log);
        }

        private GameObject GetPrefab(string flowName, string entitySign, out string flowCode) {
            GameObject prefab = null;
            if (linkedSceneDictionary.TryGetValue(flowName, out flowCode)) {
                string prefabPath = $"Assets/LazyPan/Bundles/Prefabs/Obj/{flowCode}/{entitySign}.prefab";
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }

            return prefab;
        }

        private void InitLinkDic() {
            //行为 - 读取 BehaviourConfig
            linkedBehaviourDictionary.Clear();
            ReadCSV.Instance.Read("BehaviourConfig", out string objContent, out string[] objContents);
            if (objContents != null && objContents.Length > 0) {
                for (int i = 0; i < objContents.Length; i++) {
                    if (i > 2) {
                        string[] lineStr = objContents[i].Split(",");
                        linkedBehaviourDictionary.TryAdd(lineStr[1], lineStr[0]);
                    }
                }
            }
            
            linkedSceneDictionary.Clear();
            ReadCSV.Instance.Read("SceneConfig", out string sceneContent, out string[] sceneContents);
            if (sceneContents != null && sceneContents.Length > 0) {
                for (int i = 0; i < sceneContents.Length; i++) {
                    if (i > 2) {
                        string[] lineStr = sceneContents[i].Split(",");
                        linkedSceneDictionary.TryAdd(lineStr[1], lineStr[0]);
                    }
                }
            }
        }

        private void InitOptionDic() {
            behaviourNameOptions = new List<string>();
            if (linkedBehaviourDictionary != null) {
                foreach (var tmp in linkedBehaviourDictionary) {
                    behaviourNameOptions.Add(tmp.Key);
                }
            }
            
            sceneNameOptions = new List<string>();
            if (linkedSceneDictionary != null) {
                foreach (var tmp in linkedSceneDictionary) {
                    sceneNameOptions.Add(tmp.Key);
                }
            }
        }

        private void RefreshOperationNameOptions(bool exist) {
            operationNameOptions.Clear();
            operationNameOptions.Add("操作");
            operationNameOptions.Add(exist ? "跳转到预制体" : "创建预制体");
            if (exist) {
                operationNameOptions.Add("打印绑定数据到日志");
            }
        }

        private void ReadEntityData(string fuzzyContent) {
            entityDatas.Clear();
            ReadCSV.Instance.Read("ObjConfig", out string content, out string[] lines);
            if (lines != null && lines.Length > 0) {
                for (int i = 0; i < lines.Length; i++) {
                    if (i > 2) {
                        string[] lineStr = lines[i].Split(",");
                        if (lineStr.Length > 0) {
                            bool hasFuzzyContent = false;
                            for (int j = 0; j <= 5; j++) {
                                if (lineStr[j].Contains(fuzzyContent)) {
                                    hasFuzzyContent = true;
                                    break;
                                }
                            }

                            if (!hasFuzzyContent) {
                                continue;
                            }

                            //实体数据
                            string[] lineInfo = new string[6];
                            for (int j = 0; j <= 5; j++) {
                                lineInfo[j] = lineStr[j];
                            }
                            //场景数据
                            string[] sceneIndex = sceneNameOptions.ToArray();
                            int selectScene = -1;
                            for (int j = 0; j < sceneIndex.Length; j++) {
                                if (sceneIndex[j] == lineInfo[1]) {
                                    selectScene = j;
                                    break;
                                }
                            }
                            //行为数据
                            string behaviourBind = lineStr[5];
                            string[] behaviourBindName = behaviourBind.Split('|');
                            //遍历玩家绑定的行为
                            bool[] selectBehaviour = new bool[behaviourNames.Length];
                            foreach (var tmpBehaviourConfig in behaviourNames) {
                                for (int j = 0; j < behaviourBindName.Length; j++) {
                                    string behaviourName = behaviourBindName[j];
                                    if (tmpBehaviourConfig == behaviourName) {
                                        selectBehaviour[j] = true;
                                    }
                                }
                            }

                            MyEntityData instanceEntityData = new MyEntityData(lineInfo, selectScene,
                                behaviourBindName.ToList(), selectBehaviour);
                            entityDatas.Add(instanceEntityData);
                        }
                    }
                }
            }
        }

        private bool IsExitPrefab(MyEntityData entityData) {
            return GetPrefab(entityData.Infos[1], entityData.Infos[0], out string flowCode) != null;
        }

        private void WriteEntityData() {
            ReadCSV.Instance.Read("ObjConfig", out string content, out string[] lines);
            try {
                Queue<MyEntityData> entityDataQue = new Queue<MyEntityData>(entityDatas);
                int newLength = -1;
                for (int i = 0; i < lines.Length; i++) {
                    if (i > 2) {
                        string[] linesStr = lines[i].Split(',');
                        if (entityDataQue.Count == 0) {
                            newLength = i;
                            break;
                        }

                        MyEntityData data = entityDataQue.Dequeue();
                        if (data != null) {
                            for (int j = 0; j < data.Infos.Length; j++) {
                                linesStr[j] = data.Infos[j];
                            }

                            lines[i] = string.Join(",", linesStr);
                        }
                    }
                }
                
                string[] newLines;
                if (newLength > -1) {
                    //需要裁剪
                    newLines = new string[newLength];
                    Array.Copy(lines, newLines, newLength);
                    ReadCSV.Instance.Write("ObjConfig", newLines);
                } else {
                    newLines = new string[entityDataQue.Count];
                    int index = 0;
                    while (entityDataQue.Count > 0) {
                        MyEntityData data = entityDataQue.Dequeue();
                        if (data != null) {
                            string[] linesStr = new string[6];
                            for (int j = 0; j < data.Infos.Length; j++) {
                                linesStr[j] = data.Infos[j];
                            }
                
                            newLines[index] = string.Join(",", linesStr);
                            index++;
                        }
                    }
                    ReadCSV.Instance.Write("ObjConfig", lines.Concat(newLines).ToArray());
                }
            } catch {
                Debug.LogError("录入错误");
            }
        }

        private void PreviewEntityConfigData() {            
            isFoldoutData = EditorGUILayout.Foldout(isFoldoutData, " 预览实体配置数据", true);
            Rect rect = GUILayoutUtility.GetLastRect();
            float height = 0;
            if (isFoldoutData) {
                height += FuzzySearch();
                reorderableList.DoLayoutList();

                height += GUILayoutUtility.GetLastRect().height;
                GUILayout.Space(10);
            } else {
                GUILayout.Space(10);
            }
            
            LazyPanTool.DrawBorder(new Rect(rect.x + 2f, rect.y - 2f, rect.width - 2f, rect.height + height + 5f), Color.white);

            GUILayout.Space(10);
        }

        private float FuzzySearch() {
            GUILayout.BeginHorizontal();

            float inputHeight = 20f;

            // 设置控件名称用于检测焦点
            GUI.SetNextControlName(textName);

            // 判断输入框内容
            string displayText = string.IsNullOrEmpty(UserInput) && !isFocused ? placeholder : UserInput;

            // 显示输入框
            string newInput = EditorGUILayout.TextField(displayText, GUILayout.Height(inputHeight));
            GUILayout.EndHorizontal(); // 结束水平布局

            // 检测焦点状态
            if (GUI.GetNameOfFocusedControl() == textName) {
                if (!isFocused) {
                    isFocused = true; // 聚焦状态
                    if (displayText == placeholder) {
                        newInput = ""; // 清空提示文本
                    }
                }
            } else {
                if (isFocused) {
                    isFocused = false; // 失去焦点
                    if (string.IsNullOrEmpty(newInput)) {
                        newInput = placeholder; // 恢复提示文本
                    }
                }
            }

            //更新
            if (UserInput != newInput) {
                UserInput = newInput;
                string fuzzyContent = UserInput == placeholder ? "" : UserInput;
                InitReorderableList(fuzzyContent);
            }

            return inputHeight;
        }

        private void ManualGeneratePrefabTool() {
            isFoldoutGenerate = EditorGUILayout.Foldout(isFoldoutGenerate, " 手动创建预制体工具", true);
            Rect rect = GUILayoutUtility.GetLastRect();
            float height = 0;
            if (isFoldoutGenerate) {
                GUILayout.BeginVertical();
                GUILayout.Label("");
                GUILayout.BeginHorizontal();
                _instanceFlowName = EditorGUILayout.TextField("流程标识(创建于文件夹下，必填)", _instanceFlowName, GUILayout.Height(20));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                _instanceTypeName = EditorGUILayout.TextField("类型标识(必填)", _instanceTypeName, GUILayout.Height(20));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                _instanceObjName = EditorGUILayout.TextField("实体标识(必填)", _instanceObjName, GUILayout.Height(20));
                GUILayout.EndHorizontal();
                GUI.SetNextControlName("objChineseName");
                GUILayout.BeginHorizontal();
                _instanceObjChineseName = EditorGUILayout.TextField("实体中文名字(必填)", _instanceObjChineseName, GUILayout.Height(20));
                GUILayout.EndHorizontal();
                GUIStyle style = LazyPanTool.GetGUISkin("AButtonGUISkin").GetStyle("button");
                if(GUILayout.Button("创建实体物体", style)) {
                    InstanceCustomObj();
                }
                if(GUILayout.Button("创建实体物体点位配置", style)) {
                    InstanceCustomLocationSetting();
                }
                GUILayout.EndVertical();
                height += GUILayoutUtility.GetLastRect().height;
            } else {
                GUILayout.Space(10);
            }
            
            LazyPanTool.DrawBorder(new Rect(rect.x + 2f, rect.y - 2f, rect.width - 2f, rect.height + height + 5f), Color.white);

            GUILayout.Space(10);
        }

        private void AutoTool() {
            GUIStyle style = LazyPanTool.GetGUISkin("AButtonGUISkin").GetStyle("button");
            isFoldoutTool = EditorGUILayout.Foldout(isFoldoutTool, " 自动化工具", true);
            Rect rect = GUILayoutUtility.GetLastRect();
            float height = 0;
            if (isFoldoutTool) {
                GUILayout.Label("");
                height += GUILayoutUtility.GetLastRect().height;
                if(GUILayout.Button("打开实体配置表 Csv", style)) {
                    GUILayout.BeginHorizontal();
                    OpenEntityCsv();
                    GUILayout.EndHorizontal();
                }
                height += GUILayoutUtility.GetLastRect().height;
                if(GUILayout.Button("批量输出实体绑定数据日志", style)) {
                    GUILayout.BeginHorizontal();
                    LogAllEntityData();
                    GUILayout.EndHorizontal();
                }
                height += GUILayoutUtility.GetLastRect().height;
            } else {
                GUILayout.Space(10);
            }
            
            LazyPanTool.DrawBorder(new Rect(rect.x + 2f, rect.y - 2f, rect.width - 2f, rect.height + height + 10f), Color.white);

            GUILayout.Space(10);
        }

        private void LogAllEntityData() {
            foreach (var tmpEntity in entityDatas) {
                if (IsExitPrefab(tmpEntity)) {
                    GameObject prefab = GetPrefab(tmpEntity.Infos[1], tmpEntity.Infos[0], out string flowCode);
                    DisplayEntityData(prefab, tmpEntity.Infos);
                }
            }
        }

        private void Title() {
            GUILayout.BeginHorizontal();
            GUIStyle style = LazyPanTool.GetGUISkin("LogoGUISkin").GetStyle("label");
            GUILayout.Label("ENTITY", style);
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            style = LazyPanTool.GetGUISkin("AnnotationGUISkin").GetStyle("label");
            GUILayout.Label("@实体 游戏内最小单位", style);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
        }

        //选中改变状态
        private void ToggleLayerSelection(MyEntityData entityData, int index, string flowSign, string entitySign, string behaviourSign) {
            entityData.BehaviourIndexs[index] = !entityData.BehaviourIndexs[index];
            //如果点击后是增加 则增加行为数据 反之减少 操作是对行为重新录入
            UpdateEntityBehaviourData(entityData.BehaviourIndexs[index], flowSign, entitySign, behaviourSign);
            OnStart(_tool);
        }

        private void UpdateEntityBehaviourData(bool add, string flowSign, string entitySign, string behaviourName) {
            ReadCSV.Instance.Read("ObjConfig", out string content, out string[] lines);
            for (int i = 0; i < lines.Length; i++) {
                if (i > 2) {
                    string[] linesStr = lines[i].Split(',');
                    if (linesStr[0] == entitySign && linesStr[1] == flowSign) {
                        string[] bindBehaviourName = linesStr[5].Split('|');
                        string newBind = "";
                        foreach (var tmpBindStr in bindBehaviourName) {
                            if (add) {
                                newBind += tmpBindStr + "|";
                            } else {
                                if (tmpBindStr != behaviourName) {
                                    newBind += tmpBindStr + "|";
                                }
                            }
                        }

                        if (add) {
                            newBind += behaviourName;
                        }

                        newBind = newBind.TrimEnd('|');
                        newBind = newBind.TrimStart('|');

                        linesStr[5] = newBind;
                
                        // 将更新后的内容重新拼接回 lines 数组
                        lines[i] = string.Join(",", linesStr);
                        break;
                    }
                }
            }

            ReadCSV.Instance.Write("ObjConfig", lines);
        }

        private void ExpandEntityData() {
            bool hasContent = false;
            if (_entityConfig != null && _entityConfig.Length > 0) {
                GUILayout.BeginVertical();
                string entitySign = "";
                int displayCount = 0;
                foreach (var str in _entityConfig) {
                    if (str != null) {
                        if (entitySign != str[1]) {
                            entitySign = str[1];
                            GUILayout.Label("");
                        }

                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();

                        string strContent = "";
                        displayCount = str.Length;
                        for (int i = 0; i < displayCount; i++) {
                            bool isLast = false;
                            strContent = str[i];

                            bool hasPrefab = HasPrefabTips(str);
                            Color fontColor;
                            if (i == 1) {
                                fontColor = Color.cyan;
                            } else if (i == 0) {
                                fontColor = hasPrefab ? Color.green : Color.red;
                            } else {
                                fontColor = Color.green;
                            }

                            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
                            labelStyle.normal.textColor = fontColor; // 设置字体颜色

                            //预制体相关判断
                            if (GUILayout.Button(strContent, isLast ? null : labelStyle,
                                GUILayout.Width(Screen.width / (displayCount + 1) - 10))) {
                                switch (i) {
                                    case 0:
                                        PrefabJudge(hasPrefab, str);
                                        break;
                                    case 5:
                                        BehaviourJudge(str);
                                        break;
                                }
                            }

                            string tooltip = "";
                            // 检测鼠标悬停
                            Rect buttonRect = GUILayoutUtility.GetLastRect();
                            if (buttonRect.Contains(Event.current.mousePosition)) {
                                if (!hasPrefab) {
                                    tooltip = "找不到预制体，请添加: " + str[0];
                                }
                            }

                            // 显示悬浮信息
                            if (!string.IsNullOrEmpty(tooltip)) {
                                Vector2 tooltipPosition =
                                    Event.current.mousePosition + new Vector2(10, 10); // 设置悬浮提示位置
                                GUI.Label(new Rect(tooltipPosition.x, tooltipPosition.y, 250, 20), tooltip);
                            }

                            hasContent = true;
                        }

                        strContent = "行为绑定";
                        SelectBindBehaviour(strContent, displayCount, str[1], str[0], str[5]);

                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();                
                    }
                }
                GUILayout.EndVertical();
            }

            if(!hasContent || _entityConfig == null || _entityConfig.Length == 0) {
                GUILayout.Label("找不到 EntityConfig.csv 的配置数据！\n请检查是否存在路径或者配置内数据是否为空！");
            }
        }

        private void SelectBindBehaviour(string buttonName, int displayCount, string flowSign, string entitySign, string behaviourSigns) {
            // if (GUILayout.Button(buttonName, GUILayout.Width(Screen.width / (displayCount + 1) - 10))) {
            //     GenericMenu menu = new GenericMenu();
            //     List<string> behaviourClips = behaviourSigns.Split('|').ToList();
            //     selectedOptions = new bool[behaviourNames.Length];
            //     for (int i = 0; i < behaviourNames.Length; i++) {
            //         int index = i;
            //         string tmpBehaviourName = behaviourNames[i];
            //         bool isSelected = behaviourClips.Contains(tmpBehaviourName);
            //         selectedOptions[i] = isSelected;
            //         menu.AddItem(new GUIContent(tmpBehaviourName), isSelected, () => ToggleLayerSelection(index, flowSign, entitySign, tmpBehaviourName));
            //     }
            //     menu.ShowAsContext();
            // }
        }

        //绑定行为相关
        private void BehaviourJudge(string[] str) {
            //Color green
            //Color red
            //去 BehaviourConfig 判断是否配置行为 没有的话 点击 CSV 创建？ 有的话跳转到行为预览？
            Debug.Log("行为点击：" + str[5]);
        }

        //预制体相关
        private void PrefabJudge(bool hasPrefab, string[] str) {
            //点击的实体如果在实体配置存在直接跳转 如果没有游戏物体创建
            if (hasPrefab) {
                string path = $"Assets/LazyPan/Bundles/Prefabs/Obj/{str[1]}/{str[0]}.prefab"; // 修改为你的路径
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) {
                    Selection.activeObject = prefab;
                    EditorGUIUtility.PingObject(prefab);
                }
            } else {
                _instanceFlowName = str[1];
                _instanceTypeName = str[2];
                _instanceObjName = str[0].Split("_")[2];
                GUI.FocusControl("objChineseName");
            }
        }

        //是否存在预制体
        private bool HasPrefabTips(string[] str) {
            string prefabPath = $"Assets/LazyPan/Bundles/Prefabs/Obj/{str[1]}/{str[0]}.prefab";
            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;
        }

        private void OpenEntityCsv() {
            string filePath = Application.dataPath + "/StreamingAssets/Csv/ObjConfig.csv";
            Process.Start(filePath);
        }

        private void InstanceCustomObj() {
            if (_instanceObjName == "" || _instanceTypeName == "" || _instanceFlowName == "" || _instanceObjChineseName == "") {
                return;
            }
            string sourcePath = "Packages/evoreek.lazypan/Runtime/Bundles/Prefabs/Obj/Obj_Sample_Sample.prefab"; // 替换为你的预制体源文件路径
            string targetFolderPath = "Assets/LazyPan/Bundles/Prefabs/Obj"; // 替换为你想要拷贝到的目标文件夹路径
            
            // 获取选中的游戏对象
            GameObject selectedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (selectedPrefab != null && PrefabUtility.IsPartOfPrefabAsset(selectedPrefab)) {
                // 确保目标文件夹存在
                if (!Directory.Exists(targetFolderPath)) {
                    Directory.CreateDirectory(targetFolderPath);
                }

                // 获取预制体路径
                string prefabPath = AssetDatabase.GetAssetPath(selectedPrefab);
                
                // 拷贝预制体到目标文件夹
                string targetPath = Path.Combine(targetFolderPath, Path.GetFileName(prefabPath));
                AssetDatabase.CopyAsset(prefabPath, targetPath);
                
                // 刷新AssetDatabase
                AssetDatabase.Refresh();
                
                //修改资源的名字为自定义
                AssetDatabase.RenameAsset(targetPath,
                    string.Concat(_instanceFlowName, _instanceFlowName != null ? "/" : "", "Obj_", _instanceTypeName,
                        "_", _instanceObjName));
                AssetDatabase.Refresh();
            }
        }

        private void InstanceCustomLocationSetting() {
            if (_instanceObjName == "" || _instanceTypeName == "" || _instanceFlowName == "" || _instanceObjChineseName == "") {
                return;
            }

            // 创建实例并赋值
            LocationInformationSetting testAsset = CreateInstance<LocationInformationSetting>();
            testAsset.SettingName = _instanceObjChineseName;
            testAsset.locationInformationDatas = new List<LocationInformationData>();
            testAsset.locationInformationDatas.Add(new LocationInformationData());

            // 替换为你希望保存的目录路径，例如 "Assets/MyFolder/"
            string savePath = "Assets/LazyPan/Bundles/Configs/Setting/LocationInformationSetting/";

            // 确保目标文件夹存在，如果不存在则创建
            if (!AssetDatabase.IsValidFolder(savePath)) {
                AssetDatabase.CreateFolder("Assets", "LazyPan/Bundles/Configs/Setting/LocationInformationSetting");
            }

            // 生成一个唯一的文件名
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(savePath + string.Concat("LocationInf_", _instanceFlowName, "_", _instanceObjName, ".asset"));

            // 将实例保存为.asset文件
            AssetDatabase.CreateAsset(testAsset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}