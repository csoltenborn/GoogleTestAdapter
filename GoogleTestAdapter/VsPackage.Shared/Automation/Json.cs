using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GoogleTestAdapter.VsPackage.Automation
{
    /// <summary>
    /// A JSON object keeping the order of its members.
    /// </summary>
    public class JsonObject : List<KeyValuePair<string, object>>
    {
        public void Add(string name, object value)
        {
            Add(new KeyValuePair<string, object>(name, value));
        }
    }

    /// <summary>
    /// Writes JSON from JsonObjects, dictionaries with string keys, enumerables, strings, numbers, booleans, enums (as
    /// their names), and null; other values are written as their string representation.
    /// </summary>
    public static class Json
    {
        public static string Write(object value)
        {
            var builder = new StringBuilder();
            Write(builder, value);
            return builder.ToString();
        }

        private static void Write(StringBuilder builder, object value)
        {
            switch (value)
            {
                case null:
                    builder.Append("null");
                    break;
                case string s:
                    WriteString(builder, s);
                    break;
                case bool b:
                    builder.Append(b ? "true" : "false");
                    break;
                case Enum e:
                    WriteString(builder, e.ToString());
                    break;
                case int _:
                case long _:
                case short _:
                case uint _:
                case ulong _:
                case double _:
                case float _:
                case decimal _:
                    builder.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                    break;
                case JsonObject jsonObject:
                    WriteObject(builder, jsonObject);
                    break;
                case IDictionary<string, string> dictionary:
                    var members = new JsonObject();
                    foreach (var pair in dictionary)
                        members.Add(pair.Key, pair.Value);
                    WriteObject(builder, members);
                    break;
                case IEnumerable enumerable:
                    builder.Append('[');
                    bool first = true;
                    foreach (object element in enumerable)
                    {
                        if (!first)
                            builder.Append(',');
                        first = false;
                        Write(builder, element);
                    }
                    builder.Append(']');
                    break;
                default:
                    WriteString(builder, value.ToString());
                    break;
            }
        }

        private static void WriteObject(StringBuilder builder, JsonObject jsonObject)
        {
            builder.Append('{');
            bool first = true;
            foreach (var member in jsonObject)
            {
                if (!first)
                    builder.Append(',');
                first = false;
                WriteString(builder, member.Key);
                builder.Append(':');
                Write(builder, member.Value);
            }
            builder.Append('}');
        }

        private static void WriteString(StringBuilder builder, string s)
        {
            builder.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    default:
                        if (c < 0x20)
                            builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            builder.Append(c);
                        break;
                }
            }
            builder.Append('"');
        }
    }
}
