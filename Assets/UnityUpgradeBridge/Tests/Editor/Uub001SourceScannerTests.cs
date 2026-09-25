using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace UnityUpgradeBridge.Editor.Tests
{
    public class Uub001SourceScannerTests
    {
        [Test]
        public void Scan_ReportsDirectEntityIdStoredAsInt()
        {
            var path = WriteSource("int id = target.GetEntityId();");

            try
            {
                var findings = Uub001SourceScanner.Scan(new[] { path });

                Assert.That(findings, Has.Count.EqualTo(1));
                Assert.That(findings[0].File, Is.EqualTo(path));
                Assert.That(findings[0].Line, Is.EqualTo(1));
                Assert.That(findings[0].RuleId, Is.EqualTo("UUB001"));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void Scan_DoesNotReportEntityIdStoredAsEntityId()
        {
            var path = WriteSource("EntityId id = target.GetEntityId();");

            try
            {
                Assert.That(Uub001SourceScanner.Scan(new[] { path }), Is.Empty);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void Scan_ReportsUub001ForBrokenValidationFixture()
        {
            var path = Path.Combine(Application.dataPath, "ValidationCases", "Uub001Broken.cs");
            var findings = Uub001SourceScanner.Scan(new[] { path });

            Assert.That(findings, Has.Count.EqualTo(1));
            Assert.That(findings[0].File, Is.EqualTo(path));
            Assert.That(findings[0].RuleId, Is.EqualTo("UUB001"));
        }

        [Test]
        public void Scan_ReportsUub001ForDictionaryKeyValidationFixture()
        {
            var path = Path.Combine(
                Application.dataPath,
                "ValidationCases",
                "Uub001DictionaryKeyBroken.cs");
            var findings = Uub001SourceScanner.Scan(new[] { path });

            Assert.That(findings, Has.Count.EqualTo(1));
            Assert.That(findings[0].File, Is.EqualTo(path));
            Assert.That(findings[0].RuleId, Is.EqualTo("UUB001"));
        }

        [Test]
        public void Scan_ReportsNeoXiderEntityIdStoredAsDictionaryKeyInt()
        {
            var path = WriteSource(
                "int id = upgrade.GetEntityId();\n" +
                "someDictionary.TryGetValue(id, out var value);\n");

            try
            {
                var findings = Uub001SourceScanner.Scan(new[] { path });

                Assert.That(findings, Has.Count.EqualTo(1));
                Assert.That(findings[0].Line, Is.EqualTo(1));
                Assert.That(findings[0].RuleId, Is.EqualTo("UUB001"));
            }
            finally
            {
                File.Delete(path);
            }
        }

        private static string WriteSource(string source)
        {
            var path = Path.Combine(Path.GetTempPath(), $"Uub001-{Guid.NewGuid():N}.cs");
            File.WriteAllText(path, source);
            return path;
        }
    }
}
