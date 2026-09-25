using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace UnityUpgradeBridge.Editor.Tests
{
    public class Uub002SourceScannerTests
    {
        [Test]
        public void Scan_ReportsEntityIdTransportedAsNumericJson()
        {
            var path = Path.Combine(Application.dataPath, "ValidationCases", "Uub002JsonNumericBroken.cs");
            var findings = Uub002SourceScanner.Scan(new[] { path });

            Assert.That(findings, Has.Count.EqualTo(1));
            Assert.That(findings[0].File, Is.EqualTo(path));
            Assert.That(findings[0].RuleId, Is.EqualTo("UUB002"));
            Assert.That(findings[0].Explanation, Is.EqualTo(Uub002SourceScanner.Explanation));
        }

        [Test]
        public void Scan_DoesNotReportEntityIdTransportedAsJsonString()
        {
            var path = WriteSource(
                "EntityId entityId = target.GetEntityId();\n" +
                "string serializedEntityId = entityId.ToString();\n" +
                "return JsonUtility.ToJson(new Payload { entityId = serializedEntityId });");

            try
            {
                Assert.That(Uub002SourceScanner.Scan(new[] { path }), Is.Empty);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void Scan_ReportsUnityMcpPluginNumericEntityIdJsonBridge()
        {
            var path = WriteSource(
                "static long GetId(Object obj)\n" +
                "{\n" +
                "    return (long)EntityId.ToULong(obj.GetEntityId());\n" +
                "}\n" +
                "\n" +
                "var result = new Dictionary<string, object>\n" +
                "{\n" +
                "    { \"instanceId\", GetId(obj) }\n" +
                "};\n" +
                "SendJson(result);\n");

            try
            {
                var findings = Uub002SourceScanner.Scan(new[] { path });

                Assert.That(findings, Has.Count.EqualTo(1));
                Assert.That(findings[0].RuleId, Is.EqualTo("UUB002"));
                Assert.That(
                    findings[0].Explanation,
                    Is.EqualTo(Uub002SourceScanner.ManualReviewExplanation));
            }
            finally
            {
                File.Delete(path);
            }
        }

        private static string WriteSource(string source)
        {
            var path = Path.Combine(Path.GetTempPath(), $"Uub002-{Guid.NewGuid():N}.cs");
            File.WriteAllText(path, source);
            return path;
        }
    }
}
