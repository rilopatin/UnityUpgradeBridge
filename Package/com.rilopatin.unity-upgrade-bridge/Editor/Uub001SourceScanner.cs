using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace UnityUpgradeBridge.Editor
{
    public readonly struct Uub001Finding
    {
        public Uub001Finding(string file, int line)
        {
            File = file;
            Line = line;
        }

        public string File { get; }
        public int Line { get; }
        public string RuleId => Uub001SourceScanner.RuleId;
        public string Explanation => Uub001SourceScanner.Explanation;
    }

    public static class Uub001SourceScanner
    {
        public const string RuleId = "UUB001";
        public const string Explanation =
            "Unity object ID returned by GetEntityId() is stored in int; store it as EntityId instead.";

        private static readonly Regex DirectEntityIdStoredAsInt = new Regex(
            @"^\s*int\s+[A-Za-z_]\w*\s*=\s*[^;\r\n]+\.GetEntityId\s*\(\s*\)\s*;\s*$",
            RegexOptions.Compiled);

        public static IReadOnlyList<Uub001Finding> Scan(IEnumerable<string> paths)
        {
            var findings = new List<Uub001Finding>();

            foreach (var path in paths)
            {
                if (!string.Equals(Path.GetExtension(path), ".cs", StringComparison.OrdinalIgnoreCase))
                    continue;

                var lineNumber = 0;
                foreach (var line in File.ReadLines(path))
                {
                    lineNumber++;

                    if (!DirectEntityIdStoredAsInt.IsMatch(line))
                        continue;

                    findings.Add(new Uub001Finding(path, lineNumber));
                }
            }

            return findings;
        }
    }
}
