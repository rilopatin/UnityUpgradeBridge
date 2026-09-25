using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace UnityUpgradeBridge.Editor
{
    public readonly struct Uub003Finding
    {
        public Uub003Finding(string file, int line)
        {
            File = file;
            Line = line;
        }

        public string File { get; }
        public int Line { get; }
        public string RuleId => Uub003SourceScanner.RuleId;
        public string Explanation => Uub003SourceScanner.Explanation;
    }

    public static class Uub003SourceScanner
    {
        public const string RuleId = "UUB003";
        public const string Explanation =
            "GetHashCode() produces a 32-bit hash, not the original EntityId identity.";

        private static readonly Regex EntityIdHashStoredAsInt = new Regex(
            @"^\s*int\s+[A-Za-z_]\w*\s*=\s*[^;\r\n]+\.GetEntityId\s*\(\s*\)\.GetHashCode\s*\(\s*\)\s*;\s*$",
            RegexOptions.Compiled);

        private static readonly Regex IntReturningMethodDeclaration = new Regex(
            @"^\s*(?:(?:public|private|protected|internal|static|virtual|override|sealed|new|unsafe|async|partial)\s+)*int\s+(?<method>[A-Za-z_]\w*)\s*\([^;{}]*\)\s*(?:\{\s*)?$",
            RegexOptions.Compiled);

        private static readonly Regex DirectEntityIdHashReturn = new Regex(
            @"^\s*return\s+[A-Za-z_]\w*\.GetEntityId\s*\(\s*\)\.GetHashCode\s*\(\s*\)\s*;\s*$",
            RegexOptions.Compiled);

        private static readonly Regex IntDictionaryDeclaration = new Regex(
            @"^\s*(?:(?:public|private|protected|internal|static|readonly|volatile|new)\s+)*(?:System\.Collections\.Generic\.)?Dictionary\s*<\s*int\s*,[^;=]+>\s+(?<dictionary>[A-Za-z_]\w*)\b",
            RegexOptions.Compiled);

        public static IReadOnlyList<Uub003Finding> Scan(IEnumerable<string> paths)
        {
            var findings = new List<Uub003Finding>();

            foreach (var path in paths)
            {
                if (!string.Equals(Path.GetExtension(path), ".cs", StringComparison.OrdinalIgnoreCase))
                    continue;

                var lines = File.ReadAllLines(path);
                var lineNumber = 0;
                foreach (var line in lines)
                {
                    lineNumber++;

                    if (EntityIdHashStoredAsInt.IsMatch(line))
                        findings.Add(new Uub003Finding(path, lineNumber));
                }

                AddDictionaryKeyFindings(path, lines, findings);
            }

            return findings;
        }

        private static void AddDictionaryKeyFindings(
            string path,
            string[] lines,
            ICollection<Uub003Finding> findings)
        {
            var dictionaries = new HashSet<string>();
            foreach (var line in lines)
            {
                var match = IntDictionaryDeclaration.Match(line);
                if (match.Success)
                    dictionaries.Add(match.Groups["dictionary"].Value);
            }

            var helpers = FindDirectHashReturningHelpers(lines);
            if (helpers.Count == 0)
                return;

            var reportedHelpers = new HashSet<string>();

            foreach (var line in lines)
            {
                foreach (var dictionary in dictionaries)
                {
                    foreach (var helper in helpers)
                    {
                        var dictionaryKeyCall = @"^\s*" + Regex.Escape(dictionary) +
                            @"\s*\[\s*" + Regex.Escape(helper.Key) + @"\s*\(";

                        if (!Regex.IsMatch(line, dictionaryKeyCall) ||
                            !reportedHelpers.Add(helper.Key))
                            continue;

                        findings.Add(new Uub003Finding(path, helper.Value));
                    }
                }
            }

            foreach (var line in lines)
            {
                foreach (var helper in helpers)
                {
                    var tryGetValueKeyCall =
                        @"^\s*(?:if\s*\([^;\r\n]*?)?[A-Za-z_]\w*\s*\.\s*TryGetValue\s*\(\s*" +
                        Regex.Escape(helper.Key) + @"\s*\(";

                    if (!Regex.IsMatch(line, tryGetValueKeyCall) ||
                        !reportedHelpers.Add(helper.Key))
                        continue;

                    findings.Add(new Uub003Finding(path, helper.Value));
                }
            }
        }

        private static Dictionary<string, int> FindDirectHashReturningHelpers(string[] lines)
        {
            var helpers = new Dictionary<string, int>();

            for (var methodLine = 0; methodLine < lines.Length; methodLine++)
            {
                var declaration = IntReturningMethodDeclaration.Match(lines[methodLine]);
                if (!declaration.Success)
                    continue;

                var braceDepth = 0;
                var bodyStarted = false;
                var returnLine = 0;

                for (var bodyLine = methodLine; bodyLine < lines.Length; bodyLine++)
                {
                    var line = lines[bodyLine];
                    var openingBraces = CountCharacter(line, '{');
                    var closingBraces = CountCharacter(line, '}');

                    if (!bodyStarted && openingBraces == 0)
                        continue;

                    bodyStarted = true;
                    braceDepth += openingBraces - closingBraces;

                    if (DirectEntityIdHashReturn.IsMatch(line))
                        returnLine = bodyLine + 1;

                    if (braceDepth > 0)
                        continue;

                    methodLine = bodyLine;
                    break;
                }

                if (returnLine > 0)
                    helpers[declaration.Groups["method"].Value] = returnLine;
            }

            return helpers;
        }

        private static int CountCharacter(string value, char character)
        {
            var count = 0;
            foreach (var current in value)
            {
                if (current == character)
                    count++;
            }

            return count;
        }
    }
}
