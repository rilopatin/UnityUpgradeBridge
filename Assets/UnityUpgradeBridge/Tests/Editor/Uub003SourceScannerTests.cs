using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace UnityUpgradeBridge.Editor.Tests
{
    public class Uub003SourceScannerTests
    {
        [Test]
        public void Scan_ReportsEntityIdHashCodeUsedAsIdentity()
        {
            var path = Path.Combine(Application.dataPath, "ValidationCases", "Uub003HashCodeBroken.cs");
            var findings = Uub003SourceScanner.Scan(new[] { path });

            Assert.That(findings, Has.Count.EqualTo(1));
            Assert.That(findings[0].File, Is.EqualTo(path));
            Assert.That(findings[0].RuleId, Is.EqualTo("UUB003"));
        }

        [Test]
        public void Scan_DoesNotReportEntityIdStoredAsEntityId()
        {
            var path = WriteSource("EntityId id = target.GetEntityId();");

            try
            {
                Assert.That(Uub003SourceScanner.Scan(new[] { path }), Is.Empty);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void Scan_DoesNotReportEntityIdHashCodeUsedAsSeed()
        {
            var path = WriteSource(
                "int CreateNewSeed()\n" +
                "{\n" +
                "    return gameObject.GetEntityId().GetHashCode();\n" +
                "}\n");

            try
            {
                Assert.That(Uub003SourceScanner.Scan(new[] { path }), Is.Empty);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void Scan_ReportsEntityIdHashCodeUsedAsDictionaryIdentity()
        {
            var path = WriteSource(
                "private readonly Dictionary<int, Object> objectsById = new Dictionary<int, Object>();\n" +
                "\n" +
                "private int GetObjectId(Object obj)\n" +
                "{\n" +
                "#if UNITY_6000_4_OR_NEWER\n" +
                "    return obj.GetEntityId().GetHashCode();\n" +
                "#else\n" +
                "    return obj.GetInstanceID();\n" +
                "#endif\n" +
                "}\n" +
                "\n" +
                "private void StoreObject(Object obj)\n" +
                "{\n" +
                "    objectsById[GetObjectId(obj)] = obj;\n" +
                "}\n");

            try
            {
                var findings = Uub003SourceScanner.Scan(new[] { path });

                Assert.That(findings, Has.Count.EqualTo(1));
                Assert.That(findings[0].RuleId, Is.EqualTo("UUB003"));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void Scan_ReportsEntityIdHashCodeUsedAsTryGetValueKey()
        {
            var path = WriteSource(
                "private int GetObjectId(Object obj)\n" +
                "{\n" +
                "#if UNITY_6000_4_OR_NEWER\n" +
                "    return obj.GetEntityId().GetHashCode();\n" +
                "#else\n" +
                "    return obj.GetInstanceID();\n" +
                "#endif\n" +
                "}\n" +
                "\n" +
                "public int GetTransformIndex(Transform transform)\n" +
                "{\n" +
                "    if (transform && _exportedTransforms.TryGetValue(GetObjectId(transform), out var index)) return index;\n" +
                "    return -1;\n" +
                "}\n");

            try
            {
                var findings = Uub003SourceScanner.Scan(new[] { path });

                Assert.That(findings, Has.Count.EqualTo(1));
                Assert.That(findings[0].RuleId, Is.EqualTo("UUB003"));
            }
            finally
            {
                File.Delete(path);
            }
        }

        private static string WriteSource(string source)
        {
            var path = Path.Combine(Path.GetTempPath(), $"Uub003-{Guid.NewGuid():N}.cs");
            File.WriteAllText(path, source);
            return path;
        }
    }
}
