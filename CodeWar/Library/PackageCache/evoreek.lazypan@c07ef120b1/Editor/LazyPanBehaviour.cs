using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace LazyPan {
    // 定义一个存储多个字段的类
    [System.Serializable]
    public class MyBehaviourData {
        public string[] Infos;
        public int OperationIndex;

        public MyBehaviourData(string[] infos) {
            this.Infos = infos;
        }
    }

    public class MyBehaviourGenerateData {
        public string[] Infos;
        public int OperationIndex;
        public int BehaviourTypeIndexs;
        public MyBehaviourGenerateData(string[] infos, int behaviourTypeIndexs) {
            this.Infos = infos;
            this.BehaviourTypeIndexs = behaviourTypeIndexs;
        }
    }

    public class LazyPanBehaviour : EditorWindow {
        private bool isFoldoutTool;
        private bool isFoldoutBehaviour;
        private bool isFoldoutData;
        private string[][] BehaviourConfigStr;
        private LazyPanTool _tool;
        private int _index;
        //数据
        private List<MyBehaviourData> behaviourDatas = new List<MyBehaviourData>();
        private List<MyBehaviourGenerateData> behaviourGenerateDatas = new List<MyBehaviourGenerateData>();
        //记录列表
        private ReorderableList reorderableList;
        private ReorderableList generateReorderableList;
        private List<string> operationNameOptions = new List<string>();
        private List<string> generateOperationNameOptions = new List<string>();
        private string[] behaviourTypeNameOptions = new [] {
            "Auto",//自动
            "Event",//事件
            "Trigger",//触发
        };

        //标题表
        private string[] names = new[] {
            "标识：",
            "名字：",
            "描述：",
            "操作",
        };
        
        //标题表
        private string[] generateNames = new[] {
            "行为代码标识：",
            "行为：",
            "行为类型代码标识：",
            "方法代码标识",
            "方法",
            "操作",
        };
        
        //模糊搜索
        private string UserInput;
        private bool isFocused;
        private string placeholder = "模糊搜索请输入关键字";
        private string textName = "模糊搜索控件";
        
        //模糊搜索
        private string UserGenerateInput;
        private bool isGenerateFocused;
        private string generatePlaceholder = "模糊搜索请输入关键字";
        private string generateTextName = "模糊搜索控件";

        public void OnStart(LazyPanTool tool) {
            _tool = tool;
            _tool.InitScroll();

            ReadCSV.Instance.Read("BehaviourConfig", out string content, out string[] lines);
            if (lines != null && lines.Length > 0) {
                BehaviourConfigStr = new string[lines.Length - 3][];
                for (int i = 0; i < lines.Length; i++) {
                    if (i > 2) {
                        string[] lineStr = lines[i].Split(",");
                        BehaviourConfigStr[i - 3] = new string[lineStr.Length];
                        if (lineStr.Length > 0) {
                            for (int j = 0; j < lineStr.Length; j++) {
                                BehaviourConfigStr[i - 3][j] = lineStr[j];
                            }
                        }
                    }
                }
            }
            
            isFoldoutTool = true;
            isFoldoutData = true;
            isFoldoutBehaviour = true;
            
            InitReorderableList("");
            InitGenerateReorderableList("");
        }

        public void OnCustomGUI(float areaX) {
            GUILayout.BeginArea(new Rect(areaX + _tool.scrollOffsetX, 60 + _tool.scrollOffsetY, Screen.width, Screen.height * 10));
            Title();
            AutoTool();
            PreviewBehaviourConfigData();
            ManualGenerateBehaviourTool();
            GUILayout.EndArea();
        }

        /// <summary>
        /// 初始化无序列表
        /// </summary>
        private void InitReorderableList(string fuzzyContent) {
            ReadBehaviourData(fuzzyContent);
            reorderableList = new ReorderableList(behaviourDatas, typeof(MyBehaviourData), true, true, true, true);
            //头部标题
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
            //绘制无序列表
            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                rect.y += 2.5f;
                rect.width -= 2;
                
                //宽度
                float _width = Screen.width - 10f;
                string tmpFlowSign = "";
                
                //遍历标题
                for (int i = 0; i < names.Length; i++) {
                    Rect labelRect = new Rect(rect.x + _width / 4 * i,
                        rect.y,
                        _width / 4,
                        EditorGUIUtility.singleLineHeight);

                    if (i < 3) {
                        string textField = EditorGUI.TextField(labelRect, behaviourDatas[index].Infos[i], EditorStyles.wordWrappedLabel);
                        if (behaviourDatas[index].Infos[i] != textField) {
                            behaviourDatas[index].Infos[i] = textField;
                            reorderableList.onChangedCallback.Invoke(reorderableList);
                        }
                    } else if (i == 3) {
                        RefreshOperationNameOptions();

                        labelRect.height = 18f;
                        int selectIndex = EditorGUI.Popup(labelRect, behaviourDatas[index].OperationIndex, operationNameOptions.ToArray());
                        if (behaviourDatas[index].OperationIndex != selectIndex) {
                            behaviourDatas[index].OperationIndex = 0;
                            string operationName = operationNameOptions[selectIndex];
                            Operation(operationName, behaviourDatas[index].Infos);
                        }
                    }
                }
            };
            reorderableList.onReorderCallback = (ReorderableList list) => {
                UnityEngine.Debug.Log("元素重新排序");
            };
            reorderableList.onAddCallback = (ReorderableList list) => {
                UnityEngine.Debug.Log("元素添加");
                behaviourDatas.Add(new MyBehaviourData(new string[3]));
            };
            reorderableList.onRemoveCallback = (ReorderableList list) => {
                UnityEngine.Debug.Log("元素删除触发");
                // 确保索引有效
                if (list.index >= 0 && list.index < list.list.Count) {
                    list.list.RemoveAt(list.index);  // 移除当前选中的元素
                }
            };
            reorderableList.onChangedCallback = (ReorderableList list) => {
                UnityEngine.Debug.Log("列表发生变化");    
                //csv数据自动更新
                WriteBehaviourData();
            };
        }

        private void InitGenerateReorderableList(string fuzzyContent) {
            ReadBehaviourGenerateData(fuzzyContent);
            generateReorderableList = new ReorderableList(behaviourGenerateDatas, typeof(MyBehaviourGenerateData), true, true, true, true);
            //头部标题
            generateReorderableList.drawHeaderCallback = (Rect rect) => {
                _index = 0;
                for (int i = 0; i < generateNames.Length; i++) {
                    EditorGUI.LabelField(
                        new Rect(rect.x + 15f + Screen.width / 6 * _index++,
                            rect.y,
                            Screen.width / 6,
                            EditorGUIUtility.singleLineHeight),
                        generateNames[i]);
                }
            };
            //绘制无序列表
            generateReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                rect.y += 2.5f;
                rect.width -= 2;
                
                //宽度
                float _width = Screen.width - 10f;
                string tmpFlowSign = "";
                
                //遍历标题
                for (int i = 0; i < generateNames.Length; i++) {
                    Rect labelRect = new Rect(rect.x + _width / 6 * i,
                        rect.y,
                        _width / 6,
                        EditorGUIUtility.singleLineHeight);

                    if (i == 2) {
                        labelRect.height = 18f;
                        int selectIndex = EditorGUI.Popup(labelRect, behaviourGenerateDatas[index].BehaviourTypeIndexs, behaviourTypeNameOptions);
                        if (behaviourGenerateDatas[index].BehaviourTypeIndexs != selectIndex) {
                            behaviourGenerateDatas[index].BehaviourTypeIndexs = selectIndex;
                            string behaviourTypeName = behaviourTypeNameOptions[selectIndex];
                            behaviourGenerateDatas[index].Infos[i] = behaviourTypeName;
                            generateReorderableList.onChangedCallback.Invoke(generateReorderableList);
                        }
                    } else if (i < 5) {
                        string textField = EditorGUI.TextField(labelRect, behaviourGenerateDatas[index].Infos[i], EditorStyles.wordWrappedLabel);
                        if (behaviourGenerateDatas[index].Infos[i] != textField) {
                            behaviourGenerateDatas[index].Infos[i] = textField;
                            generateReorderableList.onChangedCallback.Invoke(generateReorderableList);
                        }
                    } else if (i == 5) {
                        RefreshGenerateOperationNameOptions();

                        labelRect.height = 18f;
                        int selectIndex = EditorGUI.Popup(labelRect, behaviourGenerateDatas[index].OperationIndex, generateOperationNameOptions.ToArray());
                        if (behaviourGenerateDatas[index].OperationIndex != selectIndex) {
                            behaviourGenerateDatas[index].OperationIndex = 0;
                            string operationName = generateOperationNameOptions[selectIndex];
                            GenerateOperation(operationName, behaviourGenerateDatas[index].Infos);
                            Repaint();
                        }
                    }
                }
            };
            generateReorderableList.onReorderCallback = (ReorderableList list) => {
                UnityEngine.Debug.Log("元素重新排序");
            };
            generateReorderableList.onAddCallback = (ReorderableList list) => {
                UnityEngine.Debug.Log("元素添加");
                behaviourGenerateDatas.Add(new MyBehaviourGenerateData(new string[5], -1));
            };
            generateReorderableList.onRemoveCallback = (ReorderableList list) => {
                UnityEngine.Debug.Log("元素删除触发");
                // 确保索引有效
                if (list.index >= 0 && list.index < list.list.Count) {
                    list.list.RemoveAt(list.index);  // 移除当前选中的元素
                }
            };
            generateReorderableList.onChangedCallback = (ReorderableList list) => {
                UnityEngine.Debug.Log("列表发生变化");    
                //csv数据自动更新
                WriteBehaviourGenerateData();
            };
        }

        private void WriteBehaviourGenerateData() {
            ReadCSV.Instance.Read("BehaviourGenerate", out string content, out string[] lines);
            try {
                Queue<MyBehaviourGenerateData> behaviourGenerateDataQue = new Queue<MyBehaviourGenerateData>(behaviourGenerateDatas);
                int newLength = -1;
                for (int i = 0; i < lines.Length; i++) {
                    if (i > 2) {
                        string[] linesStr = lines[i].Split(',');
                        if (behaviourGenerateDataQue.Count == 0) {
                            newLength = i;
                            break;
                        }

                        MyBehaviourGenerateData data = behaviourGenerateDataQue.Dequeue();
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
                    ReadCSV.Instance.Write("BehaviourGenerate", newLines);
                } else {
                    newLines = new string[behaviourGenerateDataQue.Count];
                    int index = 0;
                    while (behaviourGenerateDataQue.Count > 0) {
                        MyBehaviourGenerateData data = behaviourGenerateDataQue.Dequeue();
                        if (data != null) {
                            string[] linesStr = new string[6];
                            for (int j = 0; j < data.Infos.Length; j++) {
                                linesStr[j] = data.Infos[j];
                            }
                
                            newLines[index] = string.Join(",", linesStr);
                            index++;
                        }
                    }
                    ReadCSV.Instance.Write("BehaviourGenerate", lines.Concat(newLines).ToArray());
                }
            } catch {
                UnityEngine.Debug.LogError("录入错误");
            }
        }

        private void ReadBehaviourGenerateData(string fuzzyContent) {
            behaviourGenerateDatas.Clear();
            ReadCSV.Instance.Read("BehaviourGenerate", out string content, out string[] lines);
            if (lines != null && lines.Length > 0) {
                for (int i = 0; i < lines.Length; i++) {
                    if (i > 2) {
                        string[] lineStr = lines[i].Split(",");
                        if (lineStr.Length > 0) {
                            bool hasFuzzyContent = false;
                            for (int j = 0; j <= 2; j++) {
                                if (lineStr[j].Contains(fuzzyContent)) {
                                    hasFuzzyContent = true;
                                    break;
                                }
                            }

                            if (!hasFuzzyContent) {
                                continue;
                            }

                            //实体数据
                            string[] lineInfo = new string[5];
                            for (int j = 0; j <= 4; j++) {
                                lineInfo[j] = lineStr[j];
                            }

                            int index = -1;
                            for (int j = 0; j < behaviourTypeNameOptions.Length; j++) {
                                if (behaviourTypeNameOptions[j] == lineInfo[2]) {
                                    index = j;
                                    break;
                                }
                            }
                            MyBehaviourGenerateData instanceBehaviourData = new MyBehaviourGenerateData(lineInfo, index);
                            behaviourGenerateDatas.Add(instanceBehaviourData);
                        }
                    }
                }
            }
        }

        private void Operation(string operationName, string[] infos) {
            if (operationName == "一键跳转到行为脚本") {
                string[] scriptPaths = AssetDatabase.FindAssets(infos[0] + " t:script");
                if (scriptPaths.Length > 0) {
                    string path = AssetDatabase.GUIDToAssetPath(scriptPaths[0]);
                    AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<MonoScript>(path));
                }
            }
        }
        
        private void GenerateOperation(string operationName, string[] infos) {
            if (operationName == "步骤一：一键生成行为在行为模板目录") {
                Generate.GenerateBehaviourBySign(infos[0], true);
            } else if (operationName == "步骤二：一键移动并覆盖到行为目录") {
                // 查找脚本的路径
                string behaviourName = "Behaviour_" + infos[2] + "_" + infos[0] + "_Template";
                string[] scriptPaths = AssetDatabase.FindAssets(behaviourName + " t:script");
                if (scriptPaths.Length > 0) {
                    // 获取脚本的当前路径
                    string currentPath = AssetDatabase.GUIDToAssetPath(scriptPaths[0]);

                    // 获取目标路径
                    string fileName = Path.GetFileName(currentPath); // 获取文件名
                    string targetDirectory = "Assets/LazyPan/Scripts/GamePlay/Behaviour";
                    string targetPath = Path.Combine(targetDirectory, fileName);

                    // 如果目标目录不存在，则创建该目录
                    if (Directory.Exists(targetDirectory)) {
                        AssetDatabase.MoveAsset(currentPath, targetPath);
                        MoveAndRenameScriptFile(behaviourName);
                        UnityEngine.Debug.Log($"脚本 '{behaviourName}' 已成功移动到 {targetPath}");
                        // 刷新资源数据库
                        AssetDatabase.Refresh();
                    } else {
                        UnityEngine.Debug.Log($"脚本 '{behaviourName}' 移动失败 {targetPath}");
                    }
                } else {
                    UnityEngine.Debug.LogError($"没有找到名为 '{behaviourName}' 的脚本文件");
                }
            } else if (operationName == "步骤三：一键将行为配置到行为配置") {
                //找到是否存在该脚本
                string behaviourName = "Behaviour_" + infos[2] + "_" + infos[0];
                string[] scriptPaths = AssetDatabase.FindAssets(behaviourName + " t:script");
                bool isScriptExit = scriptPaths.Length > 0;
                if (isScriptExit) {
                    //配置中是否已存在该配置
                    bool isExitConfig = false;
                    for (int i = 0; i < BehaviourConfigStr.Length; i++) {
                        if (BehaviourConfigStr[i][0] == behaviourName) {
                            isExitConfig = true;
                            break;
                        }
                    }

                    //配置增加
                    if (!isExitConfig) {
                        MyBehaviourData instanceData = new MyBehaviourData(new string[] {
                            behaviourName,
                            infos[1],
                            infos[1],
                        });
                        behaviourDatas.Add(instanceData);
                        WriteBehaviourData();
                    }
                }
            }
        }

        public void MoveAndRenameScriptFile(string behaviourName) {
            // 查找脚本的路径
            string[] scriptPaths = AssetDatabase.FindAssets(behaviourName + " t:script");

            if (scriptPaths.Length > 0) {
                // 获取脚本的当前路径
                string currentPath = AssetDatabase.GUIDToAssetPath(scriptPaths[0]);

                // 获取文件名
                string fileName = Path.GetFileName(currentPath);

                // 如果文件名以 "_Template" 结尾，去掉后缀
                if (fileName.EndsWith("_Template.cs")) {
                    // 去掉后缀
                    string newFileName = fileName.Substring(0, fileName.Length - "_Template.cs".Length) + ".cs";

                    // 目标目录和目标路径
                    string targetDirectory = "Assets/LazyPan/Scripts/GamePlay/Behaviour";
                    string targetPath = Path.Combine(targetDirectory, newFileName);

                    // 获取目标目录路径
                    string targetDir = Path.GetDirectoryName(targetPath);

                    // 如果目标目录不存在，则创建该目录
                    if (!Directory.Exists(targetDir)) {
                        Directory.CreateDirectory(targetDir); // 创建目录
                        AssetDatabase.Refresh(); // 刷新资源数据库，确保目录被识别
                    }

                    // 移动并重命名脚本文件
                    string moveResult = AssetDatabase.MoveAsset(currentPath, targetPath);
                    if (string.IsNullOrEmpty(moveResult)) // 如果返回为空，表示移动成功
                    {
                        // 重命名脚本文件
                        AssetDatabase.RenameAsset(targetPath, newFileName);

                        // 读取脚本内容
                        string scriptContent = File.ReadAllText(targetPath);

                        // 更新类名（假设类名与文件名一致）
                        string oldClassName = Path.GetFileNameWithoutExtension(fileName);
                        string newClassName = Path.GetFileNameWithoutExtension(newFileName);
                        scriptContent = RenameClassInScript(scriptContent, oldClassName, newClassName);

                        // 更新文件内容
                        File.WriteAllText(targetPath, scriptContent);

                        UnityEngine.Debug.Log($"脚本 '{behaviourName}' 已成功移动并重命名为 {newFileName}");
                        AssetDatabase.Refresh(); // 刷新资源数据库
                    } else {
                        UnityEngine.Debug.LogError($"脚本 '{behaviourName}' 移动失败: {moveResult}");
                    }
                } else {
                    UnityEngine.Debug.LogWarning($"脚本 '{behaviourName}' 不包含 '_Template' 后缀，跳过重命名");
                }
            } else {
                UnityEngine.Debug.LogError($"没有找到名为 '{behaviourName}' 的脚本文件");
            }
        }

        // 使用正则表达式来修改脚本中的类名
        private string RenameClassInScript(string scriptContent, string oldClassName, string newClassName) {
            // 更新类名
            scriptContent = Regex.Replace(scriptContent, $@"\b{oldClassName}\b", newClassName);

            // 可以根据需要进一步修改方法名、字段名等
            scriptContent =
                Regex.Replace(scriptContent, $@"\b{oldClassName}_\w*\b", newClassName); // 这里是一个简单的例子，修改以类名为前缀的方法名

            return scriptContent;
        }
        
        private void RefreshOperationNameOptions() {
            operationNameOptions.Clear();
            operationNameOptions.Add("操作");
            operationNameOptions.Add("一键跳转到行为脚本");
        }
        
        private void RefreshGenerateOperationNameOptions() {
            generateOperationNameOptions.Clear();
            generateOperationNameOptions.Add("操作");
            generateOperationNameOptions.Add("步骤一：一键生成行为在行为模板目录");
            generateOperationNameOptions.Add("步骤二：一键移动并覆盖到行为目录");
            generateOperationNameOptions.Add("步骤三：一键将行为配置到行为配置");
        }
        
        private void WriteBehaviourData() {
            ReadCSV.Instance.Read("BehaviourConfig", out string content, out string[] lines);
            try {
                Queue<MyBehaviourData> behaviourDataQue = new Queue<MyBehaviourData>(behaviourDatas);
                int newLength = -1;
                for (int i = 0; i < lines.Length; i++) {
                    if (i > 2) {
                        string[] linesStr = lines[i].Split(',');
                        if (behaviourDataQue.Count == 0) {
                            newLength = i;
                            break;
                        }

                        MyBehaviourData data = behaviourDataQue.Dequeue();
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
                    ReadCSV.Instance.Write("BehaviourConfig", newLines);
                } else {
                    newLines = new string[behaviourDataQue.Count];
                    int index = 0;
                    while (behaviourDataQue.Count > 0) {
                        MyBehaviourData data = behaviourDataQue.Dequeue();
                        if (data != null) {
                            string[] linesStr = new string[6];
                            for (int j = 0; j < data.Infos.Length; j++) {
                                linesStr[j] = data.Infos[j];
                            }
                
                            newLines[index] = string.Join(",", linesStr);
                            index++;
                        }
                    }
                    ReadCSV.Instance.Write("BehaviourConfig", lines.Concat(newLines).ToArray());
                }
            } catch {
                UnityEngine.Debug.LogError("录入错误");
            }
        }

        private void ReadBehaviourData(string fuzzyContent) {
            behaviourDatas.Clear();
            ReadCSV.Instance.Read("BehaviourConfig", out string content, out string[] lines);
            if (lines != null && lines.Length > 0) {
                for (int i = 0; i < lines.Length; i++) {
                    if (i > 2) {
                        string[] lineStr = lines[i].Split(",");
                        if (lineStr.Length > 0) {
                            bool hasFuzzyContent = false;
                            for (int j = 0; j <= 2; j++) {
                                if (lineStr[j].Contains(fuzzyContent)) {
                                    hasFuzzyContent = true;
                                    break;
                                }
                            }

                            if (!hasFuzzyContent) {
                                continue;
                            }

                            //实体数据
                            string[] lineInfo = new string[3];
                            for (int j = 0; j <= 2; j++) {
                                lineInfo[j] = lineStr[j];
                            }
                            MyBehaviourData instanceBehaviourData = new MyBehaviourData(lineInfo);
                            behaviourDatas.Add(instanceBehaviourData);
                        }
                    }
                }
            }
        }

        private void PreviewBehaviourConfigData() {
            isFoldoutData = EditorGUILayout.Foldout(isFoldoutData, " 预览实体配置数据", true);
            Rect rect = GUILayoutUtility.GetLastRect();
            float height = 0;
            if (isFoldoutData) {
                height += FuzzySearch();
                reorderableList.DoLayoutList();
                height += GUILayoutUtility.GetLastRect().height;
            } else {
                GUILayout.Space(10);
            }
            
            LazyPanTool.DrawBorder(new Rect(rect.x + 2f, rect.y - 2f, rect.width - 2f, rect.height + height + 5f), Color.white);

            GUILayout.Space(10);
        }

        private float GenerateFuzzySearch() {
            GUILayout.BeginHorizontal();

            float inputHeight = 20f;

            // 设置控件名称用于检测焦点
            GUI.SetNextControlName(generateTextName);

            // 判断输入框内容
            string displayText = string.IsNullOrEmpty(UserGenerateInput) && !isGenerateFocused ? generatePlaceholder : UserGenerateInput;

            // 显示输入框
            string newInput = EditorGUILayout.TextField(displayText, GUILayout.Height(inputHeight));
            GUILayout.EndHorizontal(); // 结束水平布局

            // 检测焦点状态
            if (GUI.GetNameOfFocusedControl() == generateTextName) {
                if (!isGenerateFocused) {
                    isGenerateFocused = true; // 聚焦状态
                    if (displayText == generatePlaceholder) {
                        newInput = ""; // 清空提示文本
                    }
                }
            } else {
                if (isGenerateFocused) {
                    isGenerateFocused = false; // 失去焦点
                    if (string.IsNullOrEmpty(newInput)) {
                        newInput = generatePlaceholder; // 恢复提示文本
                    }
                }
            }

            //更新
            if (UserGenerateInput != newInput) {
                UserGenerateInput = newInput;
                string fuzzyContent = UserGenerateInput == generatePlaceholder ? "" : UserGenerateInput;
                InitGenerateReorderableList(fuzzyContent);
            }

            return inputHeight;
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

        private void AutoTool() {
            isFoldoutTool = EditorGUILayout.Foldout(isFoldoutTool, " 自动化工具", true);
            Rect rect = GUILayoutUtility.GetLastRect();
            float height = 0;
            if (isFoldoutTool) {
                GUILayout.Label("");
                height += GUILayoutUtility.GetLastRect().height;
                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                GUIStyle style = LazyPanTool.GetGUISkin("AButtonGUISkin").GetStyle("button");
                if(GUILayout.Button("一键打开行为配置表 Csv", style)) {
                    OpenBehaviourCsv(false);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if(GUILayout.Button("一键打开行为自动脚本生成配置表 Csv", style)) {
                    OpenBehaviourCsv(true);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("一键自动创建且覆盖所有行为模板", style)) {
                    AutoGenerateBehaviourTemplate();
                }
                GUILayout.EndHorizontal();
                
                GUILayout.EndVertical();
                height += GUILayoutUtility.GetLastRect().height;
            } else {
                GUILayout.Space(10);
            }
            
            LazyPanTool.DrawBorder(new Rect(rect.x + 2f, rect.y - 2f, rect.width - 2f, rect.height + height + 5f), Color.white);

            GUILayout.Space(10);
        }

        private void ManualGenerateBehaviourTool() {
            isFoldoutBehaviour = EditorGUILayout.Foldout(isFoldoutBehaviour, " 手动创建行为工具", true);
            Rect rect = GUILayoutUtility.GetLastRect();
            float height = 0;
            if (isFoldoutBehaviour) {
                height += GenerateFuzzySearch();
                generateReorderableList.DoLayoutList();
                height += GUILayoutUtility.GetLastRect().height;
            } else {
                GUILayout.Space(10);
            }
            
            LazyPanTool.DrawBorder(new Rect(rect.x + 2f, rect.y - 2f, rect.width - 2f, rect.height + height + 5f), Color.white);

            GUILayout.Space(10);
        }

        private void AutoGenerateBehaviourTemplate() {
            Generate.GenerateBehaviour(true);
        }

        private void Title() {
            GUILayout.BeginHorizontal();
            GUIStyle style = LazyPanTool.GetGUISkin("LogoGUISkin").GetStyle("label");
            GUILayout.Label("BEHAVIOUR", style);
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            style = LazyPanTool.GetGUISkin("AnnotationGUISkin").GetStyle("label");
            GUILayout.Label("@行为 游戏内实体绑定的业务逻辑", style);
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
        }

        private void OpenBehaviourCsv(bool isGenerate) {
            string fileName = isGenerate ? "BehaviourGenerate" : "BehaviourConfig";
            string filePath = Application.dataPath + $"/StreamingAssets/Csv/{fileName}.csv";
            Process.Start(filePath);
        }
    }
}