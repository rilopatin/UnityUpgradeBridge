using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace UnityUpgradeBridge.Editor
{
    public readonly struct Uub002Finding
    {
        public Uub002Finding(string file, int line)
            : this(file, line, Uub002SourceScanner.Explanation)
        {
        }

        public Uub002Finding(string file, int line, string explanation)
        {
            File = file;
            Line = line;
            Explanation = explanation;
        }

        public string File { get; }
        public int Line { get; }
        public string RuleId => Uub002SourceScanner.RuleId;
        public string Explanation { get; }
    }

    public static class Uub002SourceScanner
    {
        public const string RuleId = "UUB002";
        public const string Explanation =
            "EntityId is transported as a 64-bit JSON number, which can lose precision in JavaScript; transport it as a string instead.";
        public const string ManualReviewExplanation =
            "Needs Manual Review: 64-bit EntityId is stored as numeric instanceId payload. If this crosses JSON/JavaScript, precision can be lost; use a string representation.";

        private static readonly Regex EntityIdDeclaration = new Regex(
            @"^\s*(?:UnityEngine\.)?EntityId\s+(?<name>[A-Za-z_]\w*)\s*=\s*[^;\r\n]+\.GetEntityId\s*\(\s*\)\s*;\s*$",
            RegexOptions.Compiled);

        private static readonly Regex Raw64Declaration = new Regex(
            @"^\s*(?:long|ulong|Int64|UInt64|System\.Int64|System\.UInt64)\s+(?<raw>[A-Za-z_]\w*)\s*=\s*(?<entity>[A-Za-z_]\w*)\s*;\s*$",
            RegexOptions.Compiled);

        private static readonly Regex NumericJsonTransport = new Regex(
            @"^\s*(?:return\s+)?(?:UnityEngine\.)?JsonUtility\.ToJson\s*\(\s*new\s+[A-Za-z_]\w*\s*\{\s*[A-Za-z_]\w*\s*=\s*(?<raw>[A-Za-z_]\w*)\s*\}\s*\)\s*;\s*$",
            RegexOptions.Compiled);

        private static readonly Regex NumericHelperDeclaration = new Regex(
            @"^\s*(?:(?:public|private|protected|internal|static|virtual|override|sealed|new|unsafe|partial)\s+)*(?:long|ulong|Int64|UInt64|System\.Int64|System\.UInt64)\s+(?<helper>[A-Za-z_]\w*)\s*\([^;{}]*\)\s*(?:\{\s*)?$",
            RegexOptions.Compiled);

        private static readonly Regex DirectEntityIdToULongReturn = new Regex(
            @"^\s*return\s+(?:\(\s*(?:long|ulong|Int64|UInt64|System\.Int64|System\.UInt64)\s*\)\s*)?(?:UnityEngine\.)?EntityId\.ToULong\s*\(\s*[^;\r\n]+\.GetEntityId\s*\(\s*\)\s*\)\s*;\s*$",
            RegexOptions.Compiled);

        private static readonly Regex StringObjectDictionaryDeclaration = new Regex(
            @"^\s*(?:var|(?:System\.Collections\.Generic\.)?Dictionary\s*<\s*string\s*,\s*object\s*>)\s+(?<payload>[A-Za-z_]\w*)\s*=\s*new\s+(?:System\.Collections\.Generic\.)?Dictionary\s*<\s*string\s*,\s*object\s*>",
            RegexOptions.Compiled);

        private static readonly Regex InstanceIdPayloadEntry = new Regex(
            @"^\s*\{\s*""instanceId""\s*,\s*(?<helper>[A-Za-z_]\w*)\s*\([^;{}]*\)\s*\}\s*,?\s*$",
            RegexOptions.Compiled);

        public static IReadOnlyList<Uub002Finding> Scan(IEnumerable<string> paths)
        {
            var findings = new List<Uub002Finding>();

            foreach (var path in paths)
            {
                if (!string.Equals(Path.GetExtension(path), ".cs", StringComparison.OrdinalIgnoreCase))
                    continue;

                ScanFile(path, findings);
            }

            return findings;
        }

        private static void ScanFile(string path, ICollection<Uub002Finding> findings)
        {
            var lines = File.ReadAllLines(path);
            var entityIds = new HashSet<string>();
            var raw64Values = new Dictionary<string, int>();
            var reportedValues = new HashSet<string>();
            var lineNumber = 0;

            foreach (var line in lines)
            {
                lineNumber++;

                var entityMatch = EntityIdDeclaration.Match(line);
                if (entityMatch.Success)
                    entityIds.Add(entityMatch.Groups["name"].Value);

                var rawMatch = Raw64Declaration.Match(line);
                if (rawMatch.Success && entityIds.Contains(rawMatch.Groups["entity"].Value))
                    raw64Values[rawMatch.Groups["raw"].Value] = lineNumber;

                var jsonMatch = NumericJsonTransport.Match(line);
                if (!jsonMatch.Success)
                    continue;

                var rawName = jsonMatch.Groups["raw"].Value;
                if (raw64Values.TryGetValue(rawName, out var conversionLine) && reportedValues.Add(rawName))
                    findings.Add(new Uub002Finding(path, conversionLine));
            }

            AddInstanceIdPayloadFindings(path, lines, findings);
        }

        private static void AddInstanceIdPayloadFindings(
            string path,
            string[] lines,
            ICollection<Uub002Finding> findings)
        {
            var helpers = FindNumericEntityIdHelpers(lines);
            if (helpers.Count == 0)
                return;

            var payloadHelpers = FindInstanceIdPayloadHelpers(lines);
            var reportedHelpers = new HashSet<string>();

            foreach (var payloadHelper in payloadHelpers)
            {
                if (!helpers.TryGetValue(payloadHelper.Value, out var conversionLine) ||
                    !reportedHelpers.Add(payloadHelper.Value))
                    continue;

                var explanation = HasProvenJsonSink(lines, payloadHelper.Key)
                    ? Explanation
                    : ManualReviewExplanation;
                findings.Add(new Uub002Finding(path, conversionLine, explanation));
            }
        }

        private static Dictionary<string, int> FindNumericEntityIdHelpers(string[] lines)
        {
            var helpers = new Dictionary<string, int>();

            for (var methodLine = 0; methodLine < lines.Length; methodLine++)
            {
                var declaration = NumericHelperDeclaration.Match(lines[methodLine]);
                if (!declaration.Success)
                    continue;

                var bodyEnd = FindBodyEnd(lines, methodLine, out var bodyStart);
                if (bodyStart < 0)
                    continue;

                for (var lineNumber = bodyStart; lineNumber <= bodyEnd; lineNumber++)
                {
                    if (!DirectEntityIdToULongReturn.IsMatch(lines[lineNumber]))
                        continue;

                    helpers[declaration.Groups["helper"].Value] = lineNumber + 1;
                    break;
                }

                methodLine = bodyEnd;
            }

            return helpers;
        }

        private static Dictionary<string, string> FindInstanceIdPayloadHelpers(string[] lines)
        {
            var payloadHelpers = new Dictionary<string, string>();

            for (var declarationLine = 0; declarationLine < lines.Length; declarationLine++)
            {
                var declaration = StringObjectDictionaryDeclaration.Match(lines[declarationLine]);
                if (!declaration.Success)
                    continue;

                var bodyEnd = FindBodyEnd(lines, declarationLine, out var bodyStart);
                if (bodyStart < 0)
                    continue;

                for (var lineNumber = bodyStart; lineNumber <= bodyEnd; lineNumber++)
                {
                    var entry = InstanceIdPayloadEntry.Match(lines[lineNumber]);
                    if (entry.Success)
                    {
                        payloadHelpers[declaration.Groups["payload"].Value] =
                            entry.Groups["helper"].Value;
                        break;
                    }
                }

                declarationLine = bodyEnd;
            }

            return payloadHelpers;
        }

        private static bool HasProvenJsonSink(string[] lines, string payload)
        {
            var escapedPayload = Regex.Escape(payload);
            var jsonUtility = @"\b(?:UnityEngine\.)?JsonUtility\.ToJson\s*\(\s*" + escapedPayload + @"\b";
            var systemTextJson = @"\b(?:System\.Text\.Json\.)?JsonSerializer\.Serialize\s*\(\s*" + escapedPayload + @"\b";
            var newtonsoftJson = @"\b(?:Newtonsoft\.Json\.)?JsonConvert\.SerializeObject\s*\(\s*" + escapedPayload + @"\b";

            foreach (var line in lines)
            {
                if (Regex.IsMatch(line, jsonUtility) ||
                    Regex.IsMatch(line, systemTextJson) ||
                    Regex.IsMatch(line, newtonsoftJson))
                    return true;
            }

            return false;
        }

        private static int FindBodyEnd(string[] lines, int declarationLine, out int bodyStart)
        {
            bodyStart = -1;
            var braceDepth = 0;

            for (var lineNumber = declarationLine; lineNumber < lines.Length; lineNumber++)
            {
                var openingBraces = CountCharacter(lines[lineNumber], '{');
                var closingBraces = CountCharacter(lines[lineNumber], '}');

                if (bodyStart < 0 && openingBraces == 0)
                    continue;

                if (bodyStart < 0)
                    bodyStart = lineNumber;

                braceDepth += openingBraces - closingBraces;
                if (braceDepth <= 0)
                    return lineNumber;
            }

            return lines.Length - 1;
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
