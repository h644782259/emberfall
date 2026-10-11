using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace Emberfall.Editor
{
    /// <summary>Exports an unsigned Xcode project. Device signing belongs to Xcode on the user's Mac.</summary>
    public static class IOSBuild
    {
        public const string DefaultBundleId = "com.h644782259.emberfall.ios";

        [MenuItem("Emberfall/导出 iPhone Xcode 工程 Export iOS", false, 3)]
        public static void Export()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
                throw new BuildFailedException("缺少当前 Unity 编辑器的 iOS Build Support。请在 Mac 的 Unity Hub 中为项目指定版本添加该模块后重试；此操作不会安装模块或生成可签名 IPA。");

            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string allowedRoot = Path.GetFullPath(Path.Combine(project, "Builds", "iOS"));
            string outputArgument = Argument("-emberfallIosOutput");
            string output = string.IsNullOrWhiteSpace(outputArgument)
                ? Path.Combine(allowedRoot, "Xcode")
                : Path.GetFullPath(Path.IsPathRooted(outputArgument) ? outputArgument : Path.Combine(project, outputArgument));
            string rootPrefix = allowedRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!output.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                throw new BuildFailedException("iOS 输出必须是本项目 Builds/iOS 下的独立子目录。");
            if (Directory.Exists(output) && Directory.GetFileSystemEntries(output).Length > 0)
                throw new BuildFailedException("输出目录已有内容。请使用新的空目录，避免覆盖在 Xcode 中配置过的工程：" + output);

            string bundleId = Argument("-emberfallIosBundleId") ?? DefaultBundleId;
            if (!Regex.IsMatch(bundleId, @"^[A-Za-z][A-Za-z0-9-]*(\.[A-Za-z0-9][A-Za-z0-9-]*)+$"))
                throw new BuildFailedException("Bundle Identifier 必须是合法的反向域名，例如 com.yourname.emberfall。");
            string teamId = Argument("-emberfallIosTeamId") ?? "";
            if (teamId.Length > 0 && !Regex.IsMatch(teamId, "^[A-Z0-9]{10}$"))
                throw new BuildFailedException("Team ID 应为10位大写字母/数字；也可省略并在 Xcode 中选择 Personal Team。");

            ProjectTools.EnsureSettings();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, bundleId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.iOS, ApiCompatibilityLevel.NET_Standard_2_0);
            string sdk = Argument("-emberfallIosSdk") ?? "device";
            if (sdk != "device" && sdk != "simulator")
                throw new BuildFailedException("iOS SDK must be device or simulator.");
            PlayerSettings.SetArchitecture(NamedBuildTarget.iOS, 1);
            PlayerSettings.iOS.sdkVersion = sdk == "simulator"
                ? iOSSdkVersion.SimulatorSDK : iOSSdkVersion.DeviceSDK;
            if (sdk == "simulator")
                {
                var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
                settings.FindProperty("iOSSimulatorArchitecture").intValue = 1; // ARM64 simulator.
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
            ConfigureDeviceFamily();
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.buildNumber = "1";
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS, new[] { GraphicsDeviceType.Metal });

            string previousTeam = PlayerSettings.iOS.appleDeveloperTeamID;
            try
            {
                // No developer identity or signing credential is embedded in source.
                PlayerSettings.iOS.appleDeveloperTeamID = teamId;
                Directory.CreateDirectory(output);
                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/Scenes/Main.unity" },
                    target = BuildTarget.iOS,
                    targetGroup = BuildTargetGroup.iOS,
                    locationPathName = output,
                    options = BuildOptions.None
                });
                string xcodeProject = Path.Combine(output, "Unity-iPhone.xcodeproj", "project.pbxproj");
                if (report == null || report.summary.result != BuildResult.Succeeded || !File.Exists(xcodeProject))
                    throw new BuildFailedException("iOS Xcode 导出未完成；查看本次日志。没有生成已签名 IPA，也未安装到设备。");
                Debug.Log("Emberfall iOS Xcode source exported: " + output + "\n在 Mac 用 Xcode 打开工程，选择开发团队及已连接 iPhone 后 Build & Run。此结果尚未签名或进行真机验证。");
                if (!Application.isBatchMode) EditorUtility.RevealInFinder(output);
            }
            finally
            {
                // Keep a CLI-provided personal team out of saved project settings.
                PlayerSettings.iOS.appleDeveloperTeamID = previousTeam;
            }
        }

        public static void ConfigureDeviceFamily()
        {
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            // Landscape full screen keeps combat controls usable on every iPad ratio.
            PlayerSettings.iOS.requiresFullScreen = true;
        }

        private static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (!string.Equals(args[i], name, StringComparison.Ordinal)) continue;
                if (i + 1 >= args.Length || args[i+1].StartsWith("-", StringComparison.Ordinal))
                    throw new BuildFailedException("缺少参数值：" + name);
                return args[i+1];
            }
            return null;
        }
    }
}
