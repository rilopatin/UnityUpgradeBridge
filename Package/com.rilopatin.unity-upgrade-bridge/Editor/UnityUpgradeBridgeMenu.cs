using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityUpgradeBridge.Editor
{
    internal static class UnityUpgradeBridgeMenu
    {
        [MenuItem("Tools/Unity Upgrade Bridge/Scan Project")]
        private static void ScanProject()
        {
            var sourceFiles = Directory.GetFiles(
                Application.dataPath,
                "*.cs",
                SearchOption.AllDirectories);

            var findingCount = 0;    

            foreach (var finding in Uub001SourceScanner.Scan(sourceFiles))
            {
                var projectPath = FileUtil.GetProjectRelativePath(finding.File).Replace('\\', '/');
                findingCount++;
                Debug.Log(
                    $"{projectPath}, line {finding.Line}, {finding.RuleId}, {finding.Explanation}");
            }

            foreach (var finding in Uub002SourceScanner.Scan(sourceFiles))
            {
                var projectPath = FileUtil.GetProjectRelativePath(finding.File).Replace('\\', '/');
                findingCount++;
                Debug.Log(
                    $"{projectPath}, line {finding.Line}, {finding.RuleId}, {finding.Explanation}");
            }

            foreach (var finding in Uub003SourceScanner.Scan(sourceFiles))
            {
                var projectPath = FileUtil.GetProjectRelativePath(finding.File).Replace('\\', '/');
                findingCount++;
                Debug.Log(
                    $"{projectPath}, line {finding.Line}, {finding.RuleId}, {finding.Explanation}");
            }
            Debug.Log($"Unity Upgrade Bridge: scan complete — {findingCount} finding(s).");
        }
    }
}
