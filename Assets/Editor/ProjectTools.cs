using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Emberfall.Editor
{
    [InitializeOnLoad]
    public static class ProjectTools
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        static ProjectTools() { EditorApplication.delayCall += InitializeProject; }

        private static void InitializeProject()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || BuildPipeline.isBuildingPlayer) return;
            EnsureSettings();
            if (!Application.isBatchMode && !SessionState.GetBool("Emberfall.WelcomeShown", false))
            {
                SessionState.SetBool("Emberfall.WelcomeShown", true);
                ShowGuide();
            }
        }

        public static void EnsureSettings()
        {
            PlayerSettings.companyName = "EmberfallStudio";
            PlayerSettings.productName = "Emberfall";
            PlayerSettings.bundleVersion = "0.4.0";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(UnityEditor.Build.NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Standard_2_0);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            QualitySettings.shadowDistance = 65;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.antiAliasing = 4;
            QualitySettings.pixelLightCount = 4;
            IncludeRuntimeShaders();
            AppIconSetup.Apply();
        }

        private static void IncludeRuntimeShaders()
        {
            // Procedural materials have no serialized asset references. Explicitly retain their shaders in players.
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (assets.Length == 0) return;
            using var serialized = new SerializedObject(assets[0]);
            SerializedProperty shaders = serialized.FindProperty("m_AlwaysIncludedShaders");
            if (shaders == null) return;
            // IMGUI's text shader belongs to unity default resources, not unity_builtin_extra.
            // Unity retains it automatically; manually including it breaks Unity 6 resource serialization.
            Shader guiText = Shader.Find("GUI/Text Shader");
            for (int i = shaders.arraySize - 1; i >= 0; i--)
            {
                if (guiText == null || shaders.GetArrayElementAtIndex(i).objectReferenceValue != guiText) continue;
                shaders.GetArrayElementAtIndex(i).objectReferenceValue = null;
                shaders.DeleteArrayElementAtIndex(i);
            }
            foreach (string name in new[] { "Standard", "Unlit/Color", "Sprites/Default" })
            {
                Shader shader = Shader.Find(name);
                if (shader == null) continue;
                bool found = false;
                for (int i = 0; i < shaders.arraySize; i++) if (shaders.GetArrayElementAtIndex(i).objectReferenceValue == shader) found = true;
                if (found) continue;
                int index = shaders.arraySize;
                shaders.InsertArrayElementAtIndex(index);
                shaders.GetArrayElementAtIndex(index).objectReferenceValue = shader;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [MenuItem("Emberfall/开始试玩 Play", false, 1)]
        public static void Play()
        {
            if (EditorApplication.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureSettings();
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Emberfall/构建 Windows 游戏 Build", false, 2)]
        public static void BuildWindows()
        {
            EnsureSettings();
            ProgressionValidation.Validate();
            AssetDatabase.SaveAssets();
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                throw new InvalidOperationException("请在 Unity Hub 为此编辑器安装 Windows Build Support (Mono)。");
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string output = Path.Combine(project, "Builds", "Windows", "Emberfall.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var options = new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = output,
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows build failed: " + report.summary.result + " / errors: " + report.summary.totalErrors);
            Debug.Log("Emberfall build ready: " + output);
            if (!Application.isBatchMode) EditorUtility.RevealInFinder(output);
        }

        [MenuItem("Emberfall/打开存档文件夹 Saves", false, 3)]
        public static void OpenSaves() { EditorUtility.RevealInFinder(Application.persistentDataPath); }

        [MenuItem("Emberfall/项目指南 Guide", false, 20)]
        public static void ShowGuide() { EditorWindow.GetWindow<EmberfallGuide>(true, "星烬纪元 · 项目指南", true); }
    }

    public sealed class EmberfallGuide : EditorWindow
    {
        private void OnEnable() { minSize = new Vector2(540, 490); }
        private void OnGUI()
        {
            GUILayout.Space(16);
            GUILayout.Label("星烬纪元 / EMBERFALL", EditorStyles.boldLabel);
            GUILayout.Space(8);
            EditorGUILayout.HelpBox("可玩的 3D 即时战斗 RPG 原型。场景与角色在进入 Play 后自动生成。无需下载美术资源或购买插件。", MessageType.Info);
            GUILayout.Label("剑卫 / 元素师 / 游侠 · 原野 · 三波副本与首领", EditorStyles.wordWrappedLabel);
            GUILayout.Label("每职业 10 个技能：8 主动 + 2 被动。升级获得技能点，原技能可进阶为强化和觉醒形态。", EditorStyles.wordWrappedLabel);
            GUILayout.Space(12);
            GUILayout.Label("WASD 移动 · 鼠标瞄准 · 左键/J 普攻 · 空格闪避", EditorStyles.wordWrappedLabel);
            GUILayout.Label("Z/X/C/V/B、1/2/3/4/5 技能 · Tab / [ / ] 翻页", EditorStyles.wordWrappedLabel);
            GUILayout.Label("I 背包 · K 技能与快捷键配置 · F 药水 · Esc 暂停", EditorStyles.wordWrappedLabel);
            GUILayout.Label("T 进入传送门 / 通关返回 · H 脱战返回营地 · 滚轮缩放", EditorStyles.wordWrappedLabel);
            GUILayout.Space(16);
            if (GUILayout.Button("开始试玩", GUILayout.Height(38))) { Close(); ProjectTools.Play(); }
            if (GUILayout.Button("构建 Windows 64 位游戏", GUILayout.Height(32))) ProjectTools.BuildWindows();
            GUILayout.Space(12);
            EditorGUILayout.HelpBox("自动保存一个角色。创建新角色会覆盖当前角色；标题界面会要求再次确认。首次导入请等待脚本编译完成。", MessageType.None);
            if (GUILayout.Button("打开 README")) Application.OpenURL(Path.GetFullPath("README.md"));
        }
    }
}
