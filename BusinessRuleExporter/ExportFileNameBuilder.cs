using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BusinessRuleExporter
{
    /// <summary>
    /// Builds the suggested Excel file name "[EnvironmentName]_Business_Rules.xlsx" from the
    /// connected environment. Pure string logic so it can be unit tested without XrmToolBox.
    /// </summary>
    public static class ExportFileNameBuilder
    {
        public const string Suffix = "_Business_Rules.xlsx";
        public const string FallbackName = "Dataverse";

        private const int MaxNameLength = 100;
        private const string SuffixBase = "_Business_Rules";
        private const string Extension = ".xlsx";

        /// <summary>
        /// Returns a safe file name using the first candidate that still has content after sanitizing.
        /// Candidates are tried in order; if none qualifies, "Dataverse_Business_Rules.xlsx" is returned.
        /// </summary>
        public static string Build(params string[] candidateNames)
        {
            if (candidateNames != null)
            {
                foreach (var candidate in candidateNames)
                {
                    var name = Sanitize(candidate);
                    if (name.Length > 0)
                    {
                        return name + Suffix;
                    }
                }
            }

            return FallbackName + Suffix;
        }

        /// <summary>
        /// Makes an environment name safe for use as the base of a Windows file name.
        /// Returns an empty string when nothing usable remains.
        /// </summary>
        public static string Sanitize(string environmentName)
        {
            if (string.IsNullOrWhiteSpace(environmentName))
            {
                return string.Empty;
            }

            var name = environmentName.Trim();

            // Never append the suffix twice if the name already carries it.
            var stripped = true;
            while (stripped)
            {
                stripped = false;
                if (name.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(0, name.Length - Extension.Length).TrimEnd();
                    stripped = true;
                }

                if (name.EndsWith(SuffixBase, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(0, name.Length - SuffixBase.Length).TrimEnd();
                    stripped = true;
                }
            }

            var invalid = new HashSet<char>(Path.GetInvalidFileNameChars());
            foreach (var ch in "\\/:*?\"<>|")
            {
                invalid.Add(ch);
            }

            // Each run of invalid characters or whitespace becomes a single underscore.
            var sb = new StringBuilder(name.Length);
            var pendingSeparator = false;
            foreach (var ch in name)
            {
                if (invalid.Contains(ch) || char.IsWhiteSpace(ch) || char.IsControl(ch))
                {
                    pendingSeparator = true;
                    continue;
                }

                if (pendingSeparator)
                {
                    sb.Append('_');
                    pendingSeparator = false;
                }

                sb.Append(ch);
            }

            if (pendingSeparator)
            {
                sb.Append('_');
            }

            var result = sb.ToString();
            if (result.Length > MaxNameLength)
            {
                result = result.Substring(0, MaxNameLength);
            }

            // Windows does not allow names to end with a space or period; also drop edge underscores
            // left behind by replaced characters.
            return result.Trim('_', '.', ' ');
        }
    }
}
