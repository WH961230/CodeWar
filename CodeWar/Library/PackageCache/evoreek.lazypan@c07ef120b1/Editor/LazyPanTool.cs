using System;
using System.Collections.Generic;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace LazyPan {
    public class LazyPanTool : EditorWindow {
        public Dictionary<Type, EditorWindow> editorWindows = new Dictionary<Type, EditorWindow>();

        private float areaX = 0;
        private float screenWidth = 1000;
        private float screenHeight = 800;
        private float animToolBarSpeed = 0.001f;
        private bool isAnimToolBar = false;
        public int currentToolBar = 0;
        private int lastToolBar = 0;
        private int previousToolBar = 0;

        private string[] values = {
            "引导首页 GuideHomePage", "流程 Flow", "实体 Entity", "行为 Behaviour", "教程 Tutorial", "支持 Support"
        };
        
        public float scrollOffsetY;
        public float scrollOffsetX;
        private float maxScrollY = 0f;
        private float minScrollY = -99999f;
        private float maxScrollX = 0f;
        private float minScrollX = -99999f;

        [MenuItem("LazyPan/打开引导面板 _F1")]
        public static void OpenGuide() {
            LazyPanTool window = (LazyPanTool)GetWindow(typeof(LazyPanTool), true, "LAZYPANPRO 框架", true);
            window.Show();
        }

        private void OnGUI() {
            ToolBar();
            Jump();
        }

        //标签
        private void ToolBar() {
            Scroll();
            currentToolBar = GUILayout.Toolbar(currentToolBar, values, GUILayout.Height(30));
            try {
                switch (currentToolBar) {
                    case 0:
                        if (editorWindows.TryGetValue(typeof(LazyPanGuide), out EditorWindow ret)) {
                            if (ret is LazyPanGuide ep) {
                                if (lastToolBar != currentToolBar) {
                                    ep.OnStart(this);
                                    lastToolBar = currentToolBar;
                                }

                                ep.OnCustomGUI(areaX);
                            }
                        } else {
                            editorWindows.Add(typeof(LazyPanGuide), CreateInstance<LazyPanGuide>());
                        }

                        break;
                    case 1:
                        if (editorWindows.TryGetValue(typeof(LazyPanFlow), out ret)) {
                            if (ret is LazyPanFlow ep) {
                                if (lastToolBar != currentToolBar) {
                                    ep.OnStart(this);
                                    lastToolBar = currentToolBar;
                                }

                                ep.OnCustomGUI(areaX);
                            }
                        } else {
                            editorWindows.Add(typeof(LazyPanFlow), CreateInstance<LazyPanFlow>());
                        }

                        break;
                    case 2:
                        if (editorWindows.TryGetValue(typeof(LazyPanEntity), out ret)) {
                            if (ret is LazyPanEntity ep) {
                                if (lastToolBar != currentToolBar) {
                                    ep.OnStart(this);
                                    lastToolBar = currentToolBar;
                                }

                                ep.OnCustomGUI(areaX);
                            }
                        } else {
                            editorWindows.Add(typeof(LazyPanEntity), CreateInstance<LazyPanEntity>());
                        }

                        break;
                    case 3:
                        if (editorWindows.TryGetValue(typeof(LazyPanBehaviour), out ret)) {
                            if (ret is LazyPanBehaviour ep) {
                                if (lastToolBar != currentToolBar) {
                                    ep.OnStart(this);
                                    lastToolBar = currentToolBar;
                                }

                                ep.OnCustomGUI(areaX);
                            }
                        } else {
                            editorWindows.Add(typeof(LazyPanBehaviour), CreateInstance<LazyPanBehaviour>());
                        }

                        break;
                    case 4:
                        if (editorWindows.TryGetValue(typeof(LazyPanTutorial), out ret)) {
                            if (ret is LazyPanTutorial ep) {
                                if (lastToolBar != currentToolBar) {
                                    ep.OnStart(this);
                                    lastToolBar = currentToolBar;
                                }

                                ep.OnCustomGUI(areaX);
                            }
                        } else {
                            editorWindows.Add(typeof(LazyPanTutorial), CreateInstance<LazyPanTutorial>());
                        }
                        break;
                    case 5:
                        if (editorWindows.TryGetValue(typeof(LazyPanSupport), out ret)) {
                            if (ret is LazyPanSupport ep) {
                                if (lastToolBar != currentToolBar) {
                                    ep.OnStart(this);
                                    lastToolBar = currentToolBar;
                                }

                                ep.OnCustomGUI(areaX);
                            }
                        } else {
                            editorWindows.Add(typeof(LazyPanSupport), CreateInstance<LazyPanSupport>());
                        }
                        break;
                }
            } catch {
                
            }
        }

        private float progress;
        //跳转动画
        private void Jump() {
            //点击新的标签页
            if (previousToolBar != currentToolBar) {
                areaX = previousToolBar < currentToolBar ? Screen.width : -Screen.width;
                progress = 0;
                previousToolBar = currentToolBar;
            }

            // 动画更新
            if (progress < 1f) {
                progress += Time.deltaTime * animToolBarSpeed; // 更新动画进度
                progress = Mathf.Clamp01(progress); // 确保 progress 在 [0, 1] 范围内

                areaX = Mathf.Lerp(areaX, 0, progress); // 基于起点和终点的平滑插值

                Repaint(); // 强制重绘，刷新动画
            } else {
                areaX = 0; // 确保动画最终位置准确
            }
        }

        public void InitScroll() {
            scrollOffsetX = 0;
            scrollOffsetY = 0;
        }
        
        private void Scroll() {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 2) {
                scrollOffsetX = 0;
                scrollOffsetY = 0;
                return;
            }

            if (e.type == EventType.ScrollWheel) {
                if (e.shift) {
                    scrollOffsetX -= e.delta.y * 10f; // delta.y 为滚动的增量，这里乘以 10f 来控制滚动速度
                    scrollOffsetX = Mathf.Clamp(scrollOffsetX, minScrollX, maxScrollX);
                } else {
                    scrollOffsetY -= e.delta.y * 10f; // delta.y 为滚动的增量，这里乘以 10f 来控制滚动速度
                    scrollOffsetY = Mathf.Clamp(scrollOffsetY, minScrollY, maxScrollY);
                }
                e.Use();
            }
        }

        public static GUISkin GetGUISkin(string guiskinname) {
            return AssetDatabase.LoadAssetAtPath<GUISkin>(
                $"Packages/evoreek.lazypan/Runtime/Bundles/GUISkin/{guiskinname}.guiskin");
        }
        
        //绘制边框
        public static void DrawBorder(Rect rect, Color color) {
            Color borderColor = color;

            // 绘制四条边
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, rect.width, 1), borderColor); // Top
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMax - 1, rect.width, 1), borderColor); // Bottom
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, 1, rect.height), borderColor); // Left
            EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.yMin, 1, rect.height), borderColor); // Right
        }

        //压缩文件
        public static void CompressFile(string sourceFolder, string destinationZipFile) {
            try {
                ZipFile.CreateFromDirectory(sourceFolder, destinationZipFile, CompressionLevel.Optimal, false);
                Debug.Log("Folder compressed successfully.");
            } catch (Exception ex) {
                Debug.LogError($"Failed to compress folder: {ex.Message}");
            }
        }

        //解压文件
        public static void DecompressFile(string sourceZipFile, string destinationFolder) {
            try {
                ZipFile.ExtractToDirectory(sourceZipFile, destinationFolder);
                Debug.Log("Folder decompressed successfully.");
            } catch (Exception ex) {
                Debug.LogError($"Failed to decompress folder: {ex.Message}");
            }
        }
    }
}