using System.Globalization;
using System.Text;

namespace Easy.Template.XCS.Compilation;

/// <summary>
/// A small, lenient (JSON5-like) parser for tag options.
///
/// Supports unquoted keys, single and double quoted strings, numbers,
/// booleans, null, nested objects and arrays, and trailing commas.
///
/// Example input (without the surrounding braces): <c>loopOver: "row", size: 3</c>
/// </summary>
public static class TagOptionsParser
{
    public static IReadOnlyDictionary<string, object?> Parse(string optionsText)
    {
        var parser = new Parser("{" + optionsText + "}");
        var result = parser.ParseValue();
        parser.SkipWhitespace();
        if (!parser.AtEnd)
            throw new FormatException($"Unexpected character '{parser.Current}' at position {parser.Position}");

        return (IReadOnlyDictionary<string, object?>)result!;
    }

    private sealed class Parser
    {
        private readonly string text;

        public int Position { get; private set; }

        public Parser(string text)
        {
            this.text = text;
        }

        public bool AtEnd => Position >= text.Length;

        public char Current => text[Position];

        public object? ParseValue()
        {
            SkipWhitespace();
            if (AtEnd)
                throw new FormatException("Unexpected end of input");

            var c = Current;
            if (c == '{')
                return ParseObject();
            if (c == '[')
                return ParseArray();
            if (c == '"' || c == '\'')
                return ParseString();
            if (c == '-' || c == '+' || c == '.' || char.IsDigit(c))
                return ParseNumber();

            var word = ReadIdentifier();
            return word switch
            {
                "true" => true,
                "false" => false,
                "null" => null,
                "" => throw new FormatException($"Unexpected character '{c}' at position {Position}"),
                _ => throw new FormatException($"Unexpected token '{word}' at position {Position}")
            };
        }

        private Dictionary<string, object?> ParseObject()
        {
            Expect('{');
            var result = new Dictionary<string, object?>(StringComparer.Ordinal);
            while (true)
            {
                SkipWhitespace();
                if (AtEnd)
                    throw new FormatException("Unterminated object");
                if (Current == '}')
                {
                    Position++;
                    return result;
                }

                var key = (Current == '"' || Current == '\'') ? ParseString() : ReadIdentifier();
                if (key.Length == 0)
                    throw new FormatException($"Expected a property name at position {Position}");

                SkipWhitespace();
                Expect(':');
                var value = ParseValue();
                result[key] = value;

                SkipWhitespace();
                if (!AtEnd && Current == ',')
                {
                    Position++;
                    continue;
                }
                if (!AtEnd && Current == '}')
                {
                    Position++;
                    return result;
                }
                throw new FormatException($"Expected ',' or '}}' at position {Position}");
            }
        }

        private List<object?> ParseArray()
        {
            Expect('[');
            var result = new List<object?>();
            while (true)
            {
                SkipWhitespace();
                if (AtEnd)
                    throw new FormatException("Unterminated array");
                if (Current == ']')
                {
                    Position++;
                    return result;
                }

                result.Add(ParseValue());

                SkipWhitespace();
                if (!AtEnd && Current == ',')
                {
                    Position++;
                    continue;
                }
                if (!AtEnd && Current == ']')
                {
                    Position++;
                    return result;
                }
                throw new FormatException($"Expected ',' or ']' at position {Position}");
            }
        }

        private string ParseString()
        {
            var quote = Current;
            Position++;
            var sb = new StringBuilder();
            while (true)
            {
                if (AtEnd)
                    throw new FormatException("Unterminated string");

                var c = Current;
                Position++;

                if (c == quote)
                    return sb.ToString();

                if (c != '\\')
                {
                    sb.Append(c);
                    continue;
                }

                if (AtEnd)
                    throw new FormatException("Unterminated string");

                var escaped = Current;
                Position++;
                switch (escaped)
                {
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u':
                        if (Position + 4 > text.Length)
                            throw new FormatException("Invalid unicode escape sequence");
                        sb.Append((char)int.Parse(text.Substring(Position, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        Position += 4;
                        break;
                    default: sb.Append(escaped); break;
                }
            }
        }

        private object ParseNumber()
        {
            var start = Position;
            while (!AtEnd && (char.IsLetterOrDigit(Current) || Current is '.' or '-' or '+'))
                Position++;

            var token = text.Substring(start, Position - start);
            if (long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
                return l;
            if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                return d;

            throw new FormatException($"Invalid number '{token}' at position {start}");
        }

        private string ReadIdentifier()
        {
            var start = Position;
            while (!AtEnd && (char.IsLetterOrDigit(Current) || Current is '_' or '$' or '-' or '.'))
                Position++;
            return text.Substring(start, Position - start);
        }

        private void Expect(char c)
        {
            if (AtEnd || Current != c)
                throw new FormatException($"Expected '{c}' at position {Position}");
            Position++;
        }

        public void SkipWhitespace()
        {
            while (!AtEnd && char.IsWhiteSpace(Current))
                Position++;
        }
    }
}
