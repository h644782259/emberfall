using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Emberfall.Editor
{
    // A build summary is evidence of that build only. An iOS export is not an IPA
    // and neither source bytes nor Unity's output bytes are installed disk usage.
    public sealed class BuildSizeAudit : IPostprocessBuildWithReport
    {
        [Serializable]
        private sealed class Snapshot
        {
            public string utc, target, buildOptions, metric, unityVersion;
            public long reportedBytes;
            public long previousReportedBytes;
            public long changeBytes;
            public long explicitBudgetBytes;
            public bool comparablePrevious, exceededExplicitBudget;
        }

        public int callbackOrder { get { return 1000; } }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report == null || report.summary.result == BuildResult.Failed || report.summary.result == BuildResult.Cancelled) return;
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string directory = Path.Combine(project, "Builds", "SizeReports");
            Directory.CreateDirectory(directory);
            string target = report.summary.platform.ToString();
            string file = Path.Combine(directory, target + "-latest.json");
            string previousFile = Path.Combine(directory, target + "-previous.json");
            Snapshot previous = null;
            string previousJson = null;
            bool preserveHistory = File.Exists(file);
            try
            {
                if (File.Exists(file) && new FileInfo(file).Length <= 65536)
                {
                    previousJson = File.ReadAllText(file);
                    previous = JsonUtility.FromJson<Snapshot>(previousJson);
                    if (previous != null && (string.IsNullOrEmpty(previous.target) || string.IsNullOrEmpty(previous.metric) ||
                        string.IsNullOrEmpty(previous.unityVersion) || previous.reportedBytes < 0)) previous = null;
                    if (previous != null) preserveHistory = false;
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning("Emberfall 构建体积历史无法读取，将保留文件并记录本次报告：" + error.Message);
            }
            long budget = ExplicitBudgetBytes();
            var current = new Snapshot
            {
                utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                target = target,
                buildOptions = report.summary.options.ToString(),
                metric = report.summary.platform == BuildTarget.iOS
                    ? "Unity Xcode export reported bytes; not signed IPA or installed size"
                    : "Unity build reported bytes; not installer or installed size",
                unityVersion = Application.unityVersion,
                reportedBytes = checked((long)report.summary.totalSize),
                explicitBudgetBytes = budget
            };
            current.comparablePrevious = previous != null && previous.target == current.target &&
                previous.buildOptions == current.buildOptions && previous.metric == current.metric &&
                previous.unityVersion == current.unityVersion && previous.reportedBytes > 0;
            if (current.comparablePrevious)
            {
                current.previousReportedBytes = previous.reportedBytes;
                current.changeBytes = current.reportedBytes - previous.reportedBytes;
            }
            current.exceededExplicitBudget = budget > 0 && current.reportedBytes > budget;
            // Fixed report names, not an accumulating timestamp archive. A
            // corrupt old report is left untouched for inspection instead of deleted.
            string destination = preserveHistory
                ? Path.Combine(directory, target + "-recovered-latest.json") : file;
            if (previous != null && previousJson != null) File.WriteAllText(previousFile, previousJson);
            File.WriteAllText(destination, JsonUtility.ToJson(current, true));
            string message = "Emberfall 构建体积：" + Mib(current.reportedBytes) + " MiB；报告：" + destination;
            if (current.comparablePrevious) message += "；相同构建配置变化 " + Mib(current.changeBytes) + " MiB";
            Debug.Log(message);
            if (current.comparablePrevious && BuildSizePolicy.IsSignificantGrowth(current.previousReportedBytes, current.reportedBytes))
                Debug.LogWarning("Emberfall 构建体积比上次增加超过20%且超过25 MiB，请检查新增资源、字体和调试内容。");
            if (current.exceededExplicitBudget)
                throw new BuildFailedException("Emberfall 构建体积超过显式预算 " + Mib(budget) +
                    " MiB。本次输出已保留供检查，没有删除旧构建或资源。");
        }

        private static string Mib(long bytes)
        { return (bytes / (1024d * 1024d)).ToString("0.00", CultureInfo.InvariantCulture); }

        private static long ExplicitBudgetBytes()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (!string.Equals(arguments[i], "-emberfallBuildBudgetMiB", StringComparison.Ordinal)) continue;
                long bytes;
                if (i + 1 >= arguments.Length || !BuildSizePolicy.TryParseBudget(arguments[i + 1], out bytes))
                    throw new BuildFailedException("-emberfallBuildBudgetMiB 必须是大于0且不超过1048576的MiB数值。");
                return bytes;
            }
            return 0;
        }
    }
}
