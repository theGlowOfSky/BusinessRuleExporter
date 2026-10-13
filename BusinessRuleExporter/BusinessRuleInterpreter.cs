using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace BusinessRuleExporter
{
    /// <summary>
    /// Best-effort conversion of Dynamics 365 generated business-rule JavaScript
    /// into designer-style IF / THEN / ELSE text. Option-set integers are kept
    /// as numbers because labels are not stored on the workflow record.
    /// </summary>
    public static class BusinessRuleInterpreter
    {
        private static readonly Regex ClientCode = new Regex(
            @"<clientcode>\s*<!\[CDATA\[(.*?)\]\]>\s*</clientcode>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly string[] WordPieces =
        {
            "status", "type", "code", "name", "date", "amount", "number",
            "value", "level", "category", "task", "entry", "order", "price",
            "list", "reason", "description", "quantity", "cost", "actual",
            "participant", "resource", "project", "charge", "model",
            "variation", "calculated", "required", "recommended", "image",
            "file", "attachment", "notification", "button", "duration"
        };

        public static string ExtractJavaScript(string clientData)
        {
            if (string.IsNullOrWhiteSpace(clientData))
            {
                return string.Empty;
            }

            var decoded = System.Net.WebUtility.HtmlDecode(clientData);
            var match = ClientCode.Match(decoded);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            try
            {
                var doc = XDocument.Parse(decoded);
                var node = doc.Descendants().FirstOrDefault(e =>
                    e.Name.LocalName.Equals("clientcode", StringComparison.OrdinalIgnoreCase));
                return node == null ? string.Empty : string.Concat(node.Nodes()).Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        public static Dictionary<string, string> MapVariables(string js)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(js))
            {
                return map;
            }

            foreach (Match m in Regex.Matches(
                js,
                @"\b(v\d+)\s*=(?!=)\s*[^;\n]*?\.attributes\.get\(\s*['""]([^'""]+)['""]\s*\)",
                RegexOptions.IgnoreCase))
            {
                map[m.Groups[1].Value] = m.Groups[2].Value;
            }

            var patterns = new[]
            {
                @"\b(v\d+)\s*=\s*(v\d+)\s*\.\s*get(?:Utc)?Value\s*\(\s*\)",
                @"\b(v\d+)\s*=\s*\(\s*(v\d+)\s*\)\s*\?\s*\2\s*\.\s*get(?:Utc)?Value\s*\(\s*\)"
            };

            foreach (var pattern in patterns)
            {
                foreach (Match m in Regex.Matches(js, pattern, RegexOptions.IgnoreCase))
                {
                    var created = m.Groups[1].Value;
                    var source = m.Groups[2].Value;
                    if (map.ContainsKey(source) && !map.ContainsKey(created))
                    {
                        map[created] = map[source];
                    }
                }
            }

            return map;
        }

        public static string ToLogic(string javascript)
        {
            if (string.IsNullOrWhiteSpace(javascript))
            {
                return "No client JavaScript found.";
            }

            var fields = MapVariables(javascript);
            List<Branch> branches;
            try
            {
                branches = ParseBranches(javascript, fields);
            }
            catch (Exception ex)
            {
                return "Review required: exception while parsing JavaScript (" + ex.Message + ").";
            }

            if (branches.Count == 0)
            {
                return "Review required: could not extract IF/ELSE structure from generated JavaScript.";
            }

            var lines = new List<string>();
            for (var i = 0; i < branches.Count; i++)
            {
                var branch = branches[i];
                var isElseTrue = branch.Keyword == "elseif"
                    && (branch.Condition == null
                        || branch.Condition.Equals("true", StringComparison.OrdinalIgnoreCase));

                if (isElseTrue || (branch.Keyword == "else" && string.IsNullOrEmpty(branch.Condition)))
                {
                    lines.Add("ELSE");
                }
                else if (!string.IsNullOrEmpty(branch.Condition))
                {
                    lines.Add(branch.Keyword == "elseif" ? "ELSE IF" : branch.Keyword == "else" ? "ELSE" : "IF");
                    lines.Add(branch.Condition);
                }
                else
                {
                    lines.Add(branch.Keyword == "elseif" ? "ELSE IF" : branch.Keyword == "else" ? "ELSE" : "IF");
                }

                lines.Add("THEN");
                if (branch.Actions.Count == 0)
                {
                    lines.Add("* (no interpreted actions in this branch)");
                }
                else
                {
                    foreach (var action in branch.Actions)
                    {
                        lines.Add("* " + action);
                    }
                }

                if (i < branches.Count - 1)
                {
                    lines.Add(string.Empty);
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        private sealed class Branch
        {
            public string Keyword;
            public string Condition;
            public List<string> Actions = new List<string>();
        }

        private static List<Branch> ParseBranches(string js, Dictionary<string, string> fields)
        {
            var text = ExtractBody(js);
            var branches = new List<Branch>();
            var pos = 0;
            var ifPattern = new Regex(@"\b(else\s+if|if|else)\b", RegexOptions.IgnoreCase);

            while (pos < text.Length)
            {
                var match = ifPattern.Match(text, pos);
                if (!match.Success)
                {
                    break;
                }

                var keyword = Regex.Replace(match.Groups[1].Value, @"\s+", "").ToLowerInvariant();
                var after = match.Index + match.Length;
                string condition = null;

                if (keyword == "if" || keyword == "elseif")
                {
                    var open = SkipWs(text, after);
                    if (open >= text.Length || text[open] != '(')
                    {
                        pos = after;
                        continue;
                    }

                    var close = MatchParen(text, open);
                    if (close < 0)
                    {
                        pos = after;
                        continue;
                    }

                    condition = text.Substring(open + 1, close - open - 1).Trim();
                    after = close + 1;
                }

                var braceAt = SkipWs(text, after);
                if (braceAt >= text.Length || text[braceAt] != '{')
                {
                    pos = after;
                    continue;
                }

                var braceEnd = MatchBrace(text, braceAt);
                if (braceEnd < 0)
                {
                    pos = after;
                    continue;
                }

                var body = text.Substring(braceAt + 1, braceEnd - braceAt - 1);
                var actions = new List<string>();
                foreach (var statement in SplitStatements(body))
                {
                    var action = InterpretAction(statement, fields);
                    if (!string.IsNullOrEmpty(action) && !actions.Contains(action))
                    {
                        actions.Add(action);
                    }
                }

                var readable = string.IsNullOrWhiteSpace(condition)
                    ? null
                    : InterpretCondition(condition, fields);

                if ((keyword == "if" || keyword == "elseif")
                    && actions.Count == 0
                    && string.IsNullOrEmpty(readable))
                {
                    pos = braceEnd + 1;
                    continue;
                }

                if ((keyword == "if" || keyword == "elseif")
                    && actions.Count == 0
                    && readable != null
                    && readable.IndexOf("does not contain data", StringComparison.OrdinalIgnoreCase) >= 0
                    && readable.IndexOf("equals", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    pos = braceEnd + 1;
                    continue;
                }

                if (readable != null
                    && (readable.IndexOf("indexOf", StringComparison.OrdinalIgnoreCase) >= 0
                        || readable.IndexOf("SId", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    pos = braceEnd + 1;
                    continue;
                }

                branches.Add(new Branch
                {
                    Keyword = keyword,
                    Condition = readable,
                    Actions = actions
                });
                pos = braceEnd + 1;
            }

            return branches;
        }

        private static string ExtractBody(string js)
        {
            var text = js;
            var func = Regex.Match(text, @"function\s+[A-Za-z_$][\w$]*\s*\([^)]*\)\s*\{", RegexOptions.IgnoreCase);
            if (func.Success)
            {
                var start = func.Index + func.Length - 1;
                var end = MatchBrace(text, start);
                if (end > start)
                {
                    text = text.Substring(start + 1, end - start - 1);
                }
            }

            var tryMatch = Regex.Match(text, @"\btry\s*\{", RegexOptions.IgnoreCase);
            if (tryMatch.Success)
            {
                var start = tryMatch.Index + tryMatch.Length - 1;
                var end = MatchBrace(text, start);
                if (end > start)
                {
                    text = text.Substring(start + 1, end - start - 1);
                }
            }

            return text;
        }

        private static string InterpretCondition(string cond, Dictionary<string, string> fields)
        {
            var text = StripOuter(cond.Trim());
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            if ((text.Contains("||") || text.Contains("&&")) && IsNullFragment(text))
            {
                var vars = ExtractNullVars(text);
                if (vars.Count > 0)
                {
                    var names = new List<string>();
                    foreach (var variable in vars)
                    {
                        var name = Resolve(variable, fields);
                        if (!names.Contains(name))
                        {
                            names.Add(name);
                        }
                    }

                    var contains = Regex.IsMatch(text, @"!=|!==");
                    var phrase = contains ? "contains data" : "does not contain data";
                    var joiner = text.Contains("||") ? " OR " : " AND ";
                    return names.Count == 1
                        ? names[0] + " " + phrase
                        : string.Join(joiner, names.Select(n => n + " " + phrase));
                }
            }

            var compare = Regex.Match(
                text,
                @"\(?\s*(v\d+)\s*\)?\s*(===|==|!==|!=)\s*\(?\s*(v\d+|-?\d+(?:\.\d+)?|true|false)\s*\)?",
                RegexOptions.IgnoreCase);
            if (compare.Success && !Regex.IsMatch(compare.Groups[3].Value, @"^(null|undefined)$", RegexOptions.IgnoreCase))
            {
                var left = Resolve(compare.Groups[1].Value, fields);
                var rightRaw = compare.Groups[3].Value;
                var right = Regex.IsMatch(rightRaw, @"^v\d+$", RegexOptions.IgnoreCase)
                    ? Resolve(rightRaw, fields)
                    : rightRaw;
                var equal = compare.Groups[2].Value == "==" || compare.Groups[2].Value == "===";
                return equal ? left + " equals " + right : left + " does not equal " + right;
            }

            var numeric = Regex.Match(
                text,
                @"\(?\s*(v\d+)\s*\)?\s*(>=|<=|>|<)\s*\(?\s*(v\d+|-?\d+(?:\.\d+)?)\s*\)?",
                RegexOptions.IgnoreCase);
            if (numeric.Success)
            {
                var left = Resolve(numeric.Groups[1].Value, fields);
                var op = numeric.Groups[2].Value;
                var rightRaw = numeric.Groups[3].Value;
                var right = Regex.IsMatch(rightRaw, @"^v\d+$", RegexOptions.IgnoreCase)
                    ? Resolve(rightRaw, fields)
                    : rightRaw;
                string words;
                switch (op)
                {
                    case ">": words = "is greater than"; break;
                    case "<": words = "is less than"; break;
                    case ">=": words = "is greater than or equal to"; break;
                    default: words = "is less than or equal to"; break;
                }

                return left + " " + words + " " + right;
            }

            var bareNull = Regex.Match(
                text,
                @"\(*\s*(v\d+)\s*\)*\s*(?:===|==)\s*(?:null|undefined|""""|'')",
                RegexOptions.IgnoreCase);
            if (bareNull.Success)
            {
                return Resolve(bareNull.Groups[1].Value, fields) + " does not contain data";
            }

            return Regex.Replace(text, @"\bv\d+\b", m => Resolve(m.Value, fields));
        }

        private static string InterpretAction(string statement, Dictionary<string, string> fields)
        {
            var stmt = statement.Trim().TrimEnd(';');
            if (stmt.Length == 0 || stmt.StartsWith("var ", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var disabled = Regex.Match(
                stmt,
                @"\b(v\d+)\s*\.\s*controls\s*\.\s*forEach\s*\([\s\S]*?setDisabled\s*\(\s*(true|false)\s*\)",
                RegexOptions.IgnoreCase);
            if (disabled.Success)
            {
                var field = Resolve(disabled.Groups[1].Value, fields);
                return (disabled.Groups[2].Value.Equals("true", StringComparison.OrdinalIgnoreCase) ? "Disable" : "Enable")
                    + " field " + field;
            }

            var visible = Regex.Match(
                stmt,
                @"\b(v\d+)\s*\.\s*controls\s*\.\s*forEach\s*\([\s\S]*?setVisible\s*\(\s*(true|false)\s*\)",
                RegexOptions.IgnoreCase);
            if (visible.Success)
            {
                var field = Resolve(visible.Groups[1].Value, fields);
                return (visible.Groups[2].Value.Equals("true", StringComparison.OrdinalIgnoreCase) ? "Show" : "Hide")
                    + " field " + field;
            }

            var required = Regex.Match(
                stmt,
                @"\b(v\d+)\s*\.\s*setRequiredLevel\s*\(\s*['""]([^'""]+)['""]\s*\)",
                RegexOptions.IgnoreCase);
            if (required.Success)
            {
                var field = Resolve(required.Groups[1].Value, fields);
                var level = required.Groups[2].Value.ToLowerInvariant();
                string label;
                switch (level)
                {
                    case "required": label = "Business Required"; break;
                    case "none": label = "Not Business Required"; break;
                    case "recommended": label = "Business Recommended"; break;
                    default: label = level; break;
                }

                return "Set " + field + " as " + label;
            }

            var clear = Regex.Match(
                stmt,
                @"\b(v\d+)\s*\.\s*setValue\s*\(\s*(null|undefined|""""|'')\s*\)",
                RegexOptions.IgnoreCase);
            if (clear.Success)
            {
                return "Clear " + Resolve(clear.Groups[1].Value, fields);
            }

            var notify = Regex.Match(
                stmt,
                @"\b(v\d+)\s*\.\s*controls\s*\.\s*forEach\s*\([\s\S]*?setNotification\s*\(\s*(?:Mscrm\.BusinessRulesScript\.GetResourceString\s*\(\s*['""][^'""]*['""]\s*,\s*)?['""]([^'""]+)['""]",
                RegexOptions.IgnoreCase);
            if (notify.Success)
            {
                return "Show error on " + Resolve(notify.Groups[1].Value, fields) + ": \"" + notify.Groups[2].Value + "\"";
            }

            if (Regex.IsMatch(stmt, @"clearNotification\s*\(", RegexOptions.IgnoreCase))
            {
                return null;
            }

            var setValue = Regex.Match(
                stmt,
                @"\b(v\d+)\s*\.\s*setValue\s*\(([\s\S]+)\)\s*$",
                RegexOptions.IgnoreCase);
            if (setValue.Success)
            {
                var field = Resolve(setValue.Groups[1].Value, fields);
                var expr = setValue.Groups[2].Value.Trim();
                var arith = Regex.Match(
                    expr,
                    @"[A-Za-z_$][\w$]*\s*\(\s*(?:\(\s*(v\d+)\s*\)\s*\?\s*\1\s*\.\s*get(?:Utc)?Value\s*\(\s*\)\s*:\s*null|(v\d+))\s*,\s*(?:\(\s*(v\d+)\s*\)\s*\?\s*\3\s*\.\s*get(?:Utc)?Value\s*\(\s*\)\s*:\s*null|(v\d+))\s*,\s*function",
                    RegexOptions.IgnoreCase);
                if (arith.Success)
                {
                    var leftVar = arith.Groups[1].Success ? arith.Groups[1].Value : arith.Groups[2].Value;
                    var rightVar = arith.Groups[3].Success ? arith.Groups[3].Value : arith.Groups[4].Value;
                    var op = "-";
                    if (Regex.IsMatch(expr, @"return\s+op1\s*\+\s*op2")) op = "+";
                    else if (Regex.IsMatch(expr, @"return\s+op1\s*\*\s*op2")) op = "*";
                    else if (Regex.IsMatch(expr, @"return\s+op1\s*/\s*op2")) op = "/";
                    return "Set " + field + " to " + Resolve(leftVar, fields) + " " + op + " " + Resolve(rightVar, fields);
                }

                var literal = Regex.Match(expr, @"^['""]([^'""]*)['""]$");
                if (literal.Success)
                {
                    return "Set " + field + " to '" + literal.Groups[1].Value + "'";
                }

                if (Regex.IsMatch(expr, @"^(true|false)$", RegexOptions.IgnoreCase))
                {
                    return "Set " + field + " to " + expr.ToLowerInvariant();
                }

                return "Set " + field + " to " + expr;
            }

            return null;
        }

        private static bool IsNullFragment(string text)
        {
            return Regex.IsMatch(text, @"(?:==|===|!=|!==)\s*(?:undefined|null|""""|'')", RegexOptions.IgnoreCase);
        }

        private static List<string> ExtractNullVars(string condition)
        {
            var found = new List<string>();
            foreach (Match m in Regex.Matches(
                condition,
                @"\(*\s*\b(v\d+)\b\s*\)*\s*(?:==|===|!=|!==)\s*(?:undefined|null|""""|'')",
                RegexOptions.IgnoreCase))
            {
                var variable = m.Groups[1].Value;
                if (!found.Contains(variable))
                {
                    found.Add(variable);
                }
            }

            return found;
        }

        /// <summary>
        /// Resolves a generated variable (v1, v2, ...) to the exact Dataverse logical name taken from the
        /// variable mapping. A variable that is not in the mapping is returned unchanged (never guessed).
        /// </summary>
        private static string Resolve(string token, Dictionary<string, string> fields)
        {
            token = token.Trim().Trim('(', ')');
            if (string.IsNullOrWhiteSpace(token))
            {
                return "Unknown Field";
            }

            string logical;
            if (fields.TryGetValue(token, out logical) && !string.IsNullOrWhiteSpace(logical))
            {
                return logical;
            }

            return token;
        }

        public static string Friendly(string logicalName)
        {
            if (string.IsNullOrWhiteSpace(logicalName))
            {
                return "Unknown Field";
            }

            var name = Regex.Replace(
                logicalName,
                @"^(new_|int_|cr\d+_|msdyn_|msdyncrm_|adx_|mspp_|cdi_|gc_)",
                "",
                RegexOptions.IgnoreCase);
            name = name.Replace("_", " ");
            name = Regex.Replace(name, @"([a-z])([A-Z])", "$1 $2");

            var pieces = WordPieces.OrderByDescending(p => p.Length).ToArray();
            for (var pass = 0; pass < 4; pass++)
            {
                var changed = false;
                foreach (var piece in pieces)
                {
                    var updated = Regex.Replace(name, @"(?i)([a-z]{3,})(" + piece + @")\b", "$1 $2");
                    if (!string.Equals(updated, name, StringComparison.Ordinal))
                    {
                        name = updated;
                        changed = true;
                    }
                }

                if (!changed)
                {
                    break;
                }
            }

            name = Regex.Replace(name, @"(?i)\s*id$", "").Trim();
            name = Regex.Replace(name, @"(?i)([a-z]{3,})(level)$", "$1 $2");
            name = Regex.Replace(name, @"\s+", " ").Trim();
            if (name.Length == 0)
            {
                return logicalName;
            }

            return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name.ToLowerInvariant());
        }

        private static string StripOuter(string text)
        {
            while (text.StartsWith("(") && text.EndsWith(")") && Balanced(text))
            {
                text = text.Substring(1, text.Length - 2).Trim();
            }

            return text;
        }

        private static bool Balanced(string text)
        {
            var depth = 0;
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] == '(') depth++;
                else if (text[i] == ')')
                {
                    depth--;
                    if (depth == 0 && i < text.Length - 1) return false;
                }
            }

            return depth == 0;
        }

        private static List<string> SplitStatements(string block)
        {
            var list = new List<string>();
            var depth = 0;
            var current = new StringBuilder();
            foreach (var ch in block)
            {
                if (ch == '{' || ch == '(') depth++;
                else if (ch == '}' || ch == ')') depth--;

                if (ch == ';' && depth == 0)
                {
                    var stmt = current.ToString().Trim();
                    if (stmt.Length > 0) list.Add(stmt);
                    current.Clear();
                }
                else
                {
                    current.Append(ch);
                }
            }

            var tail = current.ToString().Trim();
            if (tail.Length > 0) list.Add(tail);
            return list;
        }

        private static int SkipWs(string text, int index)
        {
            while (index < text.Length && char.IsWhiteSpace(text[index])) index++;
            return index;
        }

        private static int MatchBrace(string text, int open)
        {
            if (open < 0 || open >= text.Length || text[open] != '{') return -1;
            var depth = 0;
            for (var i = open; i < text.Length; i++)
            {
                if (text[i] == '{') depth++;
                else if (text[i] == '}')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }

            return -1;
        }

        private static int MatchParen(string text, int open)
        {
            if (open < 0 || open >= text.Length || text[open] != '(') return -1;
            var depth = 0;
            for (var i = open; i < text.Length; i++)
            {
                if (text[i] == '(') depth++;
                else if (text[i] == ')')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }

            return -1;
        }
    }
}
