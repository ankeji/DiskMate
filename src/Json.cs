using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DiskMate
{
    /// <summary>
    /// 极简 JSON 解析（只为解析 GitHub Releases 的返回，零依赖）。
    /// 对象 → Dictionary&lt;string, object&gt;，数组 → List&lt;object&gt;，数字 → double，其余为 string / bool / null。
    /// </summary>
    internal static class MiniJson
    {
        public static Dictionary<string, object> ParseObject(string text)
        {
            if (text == null) return null;
            var p = new Parser(text);
            object v = p.ReadValue();
            return v as Dictionary<string, object>;
        }

        public static string Str(Dictionary<string, object> d, string key)
        {
            if (d == null) return "";
            object v;
            if (!d.TryGetValue(key, out v) || v == null) return "";
            return v as string ?? Convert.ToString(v, CultureInfo.InvariantCulture);
        }

        public static long Num(Dictionary<string, object> d, string key)
        {
            if (d == null) return 0;
            object v;
            if (!d.TryGetValue(key, out v) || v == null) return 0;
            if (v is double) return (long)(double)v;
            long n;
            return long.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), out n) ? n : 0;
        }

        public static List<object> Arr(Dictionary<string, object> d, string key)
        {
            if (d == null) return null;
            object v;
            if (!d.TryGetValue(key, out v)) return null;
            return v as List<object>;
        }

        public static Dictionary<string, object> Obj(object v)
        {
            return v as Dictionary<string, object>;
        }

        private class Parser
        {
            private readonly string s;
            private int i;

            public Parser(string text) { s = text; i = 0; }

            public object ReadValue()
            {
                Skip();
                if (i >= s.Length) return null;
                char c = s[i];
                if (c == '{') return ReadObject();
                if (c == '[') return ReadArray();
                if (c == '"') return ReadString();
                if (c == 't' || c == 'f') return ReadBool();
                if (c == 'n') { i += 4; return null; }
                return ReadNumber();
            }

            private void Skip()
            {
                while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\r' || s[i] == '\n')) i++;
            }

            private Dictionary<string, object> ReadObject()
            {
                var d = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                i++;   // {
                while (true)
                {
                    Skip();
                    if (i >= s.Length) return d;
                    if (s[i] == '}') { i++; return d; }
                    if (s[i] == ',') { i++; continue; }
                    string key = ReadString();
                    Skip();
                    if (i < s.Length && s[i] == ':') i++;
                    d[key] = ReadValue();
                }
            }

            private List<object> ReadArray()
            {
                var list = new List<object>();
                i++;   // [
                while (true)
                {
                    Skip();
                    if (i >= s.Length) return list;
                    if (s[i] == ']') { i++; return list; }
                    if (s[i] == ',') { i++; continue; }
                    list.Add(ReadValue());
                }
            }

            private string ReadString()
            {
                Skip();
                if (i >= s.Length || s[i] != '"') return "";
                i++;
                var sb = new StringBuilder();
                while (i < s.Length)
                {
                    char c = s[i++];
                    if (c == '"') break;
                    if (c != '\\') { sb.Append(c); continue; }
                    if (i >= s.Length) break;
                    char e = s[i++];
                    switch (e)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'u':
                            if (i + 4 <= s.Length)
                            {
                                int code;
                                if (int.TryParse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code))
                                    sb.Append((char)code);
                                i += 4;
                            }
                            break;
                        default: sb.Append(e); break;
                    }
                }
                return sb.ToString();
            }

            private object ReadBool()
            {
                if (s[i] == 't') { i += 4; return true; }
                i += 5;
                return false;
            }

            private object ReadNumber()
            {
                int start = i;
                while (i < s.Length && "-+.eE0123456789".IndexOf(s[i]) >= 0) i++;
                double d;
                if (double.TryParse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture, out d)) return d;
                return null;
            }
        }
    }
}
