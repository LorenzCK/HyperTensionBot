using System.Text.RegularExpressions;

namespace HyperTensionBot.Server.Bot {
    public static class RegexExtensions {
        public static int GetIntMatch(this Match match, string groupName) {
            return match.GetOptionalIntMatch(groupName) ?? throw new ArgumentException($"Group {groupName} not matched or not convertible to integer");
        }

        public static int? GetOptionalIntMatch(this Match match, string groupName) {
            var g = match.Groups[groupName] ?? throw new ArgumentException($"Group {groupName} not found");
            if (!g.Success) {
                return null;
            }

            if (!int.TryParse(g.ValueSpan, out var result)) {
                return null;
            }

            return result;
        }
    }
}
