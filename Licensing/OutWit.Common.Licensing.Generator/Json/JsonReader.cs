using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace OutWit.Common.Licensing.Generator.Json
{
    /// <summary>
    /// Reads the subset of JSON a product descriptor is written in, and reports
    /// where it stopped when it cannot.
    /// <para>
    /// Position matters: a descriptor that failed to parse must become a
    /// compiler diagnostic naming the offset, not an empty vocabulary. An empty
    /// vocabulary is exactly the silent failure this generator exists to remove.
    /// </para>
    /// <para>
    /// Comments are accepted — <c>//</c> and <c>/* */</c> — because the file is
    /// meant to be read and edited by people, and a descriptor nobody may
    /// annotate is one nobody explains.
    /// </para>
    /// </summary>
    internal sealed class JsonReader
    {
        #region Fields

        private readonly string m_text;

        private int m_position;

        #endregion

        #region Constructors

        private JsonReader(string text)
        {
            m_text = text;
        }

        #endregion

        #region Functions

        /// <summary>Parses, or returns null with a reason.</summary>
        public static JsonNode? Parse(string text, out string? error)
        {
            var reader = new JsonReader(text ?? string.Empty);

            try
            {
                reader.SkipTrivia();

                var node = reader.ReadValue();

                reader.SkipTrivia();

                if (reader.m_position < reader.m_text.Length)
                    throw reader.Fail("unexpected trailing content");

                error = null;

                return node;
            }
            catch (FormatException exception)
            {
                error = exception.Message;

                return null;
            }
        }

        #endregion

        #region Tools

        private JsonNode ReadValue()
        {
            SkipTrivia();

            if (m_position >= m_text.Length)
                throw Fail("the file ends where a value was expected");

            var character = m_text[m_position];

            switch (character)
            {
                case '{': return ReadObject();
                case '[': return ReadArray();
                case '"': return JsonNode.String(ReadString());
                case 't': return ReadLiteral("true", JsonNode.Boolean(true));
                case 'f': return ReadLiteral("false", JsonNode.Boolean(false));
                case 'n': return ReadLiteral("null", JsonNode.Null());
                default: return ReadNumber();
            }
        }

        private JsonNode ReadObject()
        {
            var members = new Dictionary<string, JsonNode>(StringComparer.Ordinal);

            m_position++;
            SkipTrivia();

            if (Peek() == '}')
            {
                m_position++;
                return JsonNode.Object(members);
            }

            while (true)
            {
                SkipTrivia();

                if (Peek() != '"')
                    throw Fail("a member name was expected");

                var name = ReadString();

                SkipTrivia();

                if (Peek() != ':')
                    throw Fail($"':' was expected after '{name}'");

                m_position++;

                members[name] = ReadValue();

                SkipTrivia();

                var next = Peek();

                if (next == ',')
                {
                    m_position++;
                    SkipTrivia();

                    // A trailing comma before the brace: common, harmless, and
                    // refusing it would mean rejecting a file over punctuation.
                    if (Peek() == '}')
                    {
                        m_position++;
                        return JsonNode.Object(members);
                    }

                    continue;
                }

                if (next == '}')
                {
                    m_position++;
                    return JsonNode.Object(members);
                }

                throw Fail("',' or '}' was expected");
            }
        }

        private JsonNode ReadArray()
        {
            var items = new List<JsonNode>();

            m_position++;
            SkipTrivia();

            if (Peek() == ']')
            {
                m_position++;
                return JsonNode.Array(items);
            }

            while (true)
            {
                items.Add(ReadValue());

                SkipTrivia();

                var next = Peek();

                if (next == ',')
                {
                    m_position++;
                    SkipTrivia();

                    if (Peek() == ']')
                    {
                        m_position++;
                        return JsonNode.Array(items);
                    }

                    continue;
                }

                if (next == ']')
                {
                    m_position++;
                    return JsonNode.Array(items);
                }

                throw Fail("',' or ']' was expected");
            }
        }

        private string ReadString()
        {
            var builder = new StringBuilder();

            m_position++;

            while (m_position < m_text.Length)
            {
                var character = m_text[m_position++];

                if (character == '"')
                    return builder.ToString();

                if (character != '\\')
                {
                    builder.Append(character);
                    continue;
                }

                if (m_position >= m_text.Length)
                    break;

                var escape = m_text[m_position++];

                switch (escape)
                {
                    case '"': builder.Append('"'); break;
                    case '\\': builder.Append('\\'); break;
                    case '/': builder.Append('/'); break;
                    case 'b': builder.Append('\b'); break;
                    case 'f': builder.Append('\f'); break;
                    case 'n': builder.Append('\n'); break;
                    case 'r': builder.Append('\r'); break;
                    case 't': builder.Append('\t'); break;
                    case 'u':
                        if (m_position + 4 > m_text.Length)
                            throw Fail("a truncated \\u escape");

                        builder.Append((char)ushort.Parse(m_text.Substring(m_position, 4), NumberStyles.HexNumber,
                            CultureInfo.InvariantCulture));

                        m_position += 4;
                        break;
                    default:
                        throw Fail($"an unknown escape '\\{escape}'");
                }
            }

            throw Fail("an unterminated string");
        }

        private JsonNode ReadNumber()
        {
            var start = m_position;

            while (m_position < m_text.Length && "+-.eE0123456789".IndexOf(m_text[m_position]) >= 0)
                m_position++;

            var text = m_text.Substring(start, m_position - start);

            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                throw Fail($"'{text}' is not a value");

            return JsonNode.Number(value);
        }

        private JsonNode ReadLiteral(string literal, JsonNode node)
        {
            if (m_position + literal.Length > m_text.Length ||
                string.CompareOrdinal(m_text, m_position, literal, 0, literal.Length) != 0)
                throw Fail($"'{literal}' was expected");

            m_position += literal.Length;

            return node;
        }

        private void SkipTrivia()
        {
            while (m_position < m_text.Length)
            {
                var character = m_text[m_position];

                if (char.IsWhiteSpace(character))
                {
                    m_position++;
                    continue;
                }

                if (character != '/' || m_position + 1 >= m_text.Length)
                    return;

                var next = m_text[m_position + 1];

                if (next == '/')
                {
                    while (m_position < m_text.Length && m_text[m_position] != '\n')
                        m_position++;

                    continue;
                }

                if (next == '*')
                {
                    m_position += 2;

                    while (m_position + 1 < m_text.Length && !(m_text[m_position] == '*' && m_text[m_position + 1] == '/'))
                        m_position++;

                    m_position = Math.Min(m_position + 2, m_text.Length);

                    continue;
                }

                return;
            }
        }

        private char Peek()
        {
            return m_position < m_text.Length ? m_text[m_position] : '\0';
        }

        private FormatException Fail(string reason)
        {
            return new FormatException($"{reason}, at offset {m_position}");
        }

        #endregion
    }
}
