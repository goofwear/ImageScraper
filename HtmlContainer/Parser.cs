namespace HtmlContainer
{
    /// <summary>
    /// Base class for parsing tag based files, such as HTML,
    /// HTTP headers, or XML.
    ///
    /// This source code may be used freely under the
    /// Limited GNU Public License(LGPL).
    ///
    /// Written by Jeff Heaton (http://www.jeffheaton.com)
    /// </summary>
    public class Parser : AttributeList
    {
        /// <summary>
        /// The source text that is being parsed.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// The current position inside of the text that
        /// is being parsed.
        /// </summary>
        private int m_idx;

        /// <summary>
        /// The most recently parsed attribute delimiter.
        /// </summary>
        public char ParseDelim { get; set; }

        /// <summary>
        /// This most recently parsed attribute name.
        /// </summary>
        public string ParseName { get; set; }

        /// <summary>
        /// The most recently parsed attribute value.
        /// </summary>
        public string ParseValue { get; set; }

        /// <summary>
        /// The most recently parsed tag.
        /// </summary>
        public string m_tag;

        /// <summary>
        /// Determine if the specified character is whitespace or not.
        /// </summary>
        /// <param name="ch">A character to check</param>
        /// <returns>true if the character is whitespace</returns>
        public static bool IsWhiteSpace(char ch)
        {
            return ("\t\n\r ".IndexOf(ch) != -1);
        }


        /// <summary>
        /// Advance the index until past any whitespace.
        /// </summary>
        public void EatWhiteSpace()
        {
            while (!Eof())
            {
                if (!IsWhiteSpace(GetCurrentChar()))
                    return;
                m_idx++;
            }
        }

        /// <summary>
        /// Determine if the end of the source text has been reached.
        /// </summary>
        /// <returns>True if the end of the source text has been
        /// reached.</returns>
        public bool Eof()
        {
            return (m_idx >= Source.Length);
        }

        /// <summary>
        /// Parse the attribute name.
        /// </summary>
        public void ParseAttributeName()
        {
            EatWhiteSpace();
            // get attribute name
            while (!Eof())
            {
                if (IsWhiteSpace(GetCurrentChar()) || (GetCurrentChar() == '=') || (GetCurrentChar() == '>'))
                    break;
                ParseName += GetCurrentChar();
                m_idx++;
            }

            EatWhiteSpace();
        }

        /// <summary>
        /// Parse the attribute value
        /// </summary>
        public void ParseAttributeValue()
        {
            if (ParseDelim != 0)
                return;

            if (GetCurrentChar() == '=')
            {
                m_idx++;
                EatWhiteSpace();
                if ((GetCurrentChar() == '\'') || (GetCurrentChar() == '\"'))
                {
                    ParseDelim = GetCurrentChar();
                    m_idx++;
                    while (GetCurrentChar() != ParseDelim)
                    {
                        ParseValue += GetCurrentChar();
                        m_idx++;
                    }
                    m_idx++;
                }
                else
                {
                    while (!Eof() && !IsWhiteSpace(GetCurrentChar()) && (GetCurrentChar() != '>'))
                    {
                        ParseValue += GetCurrentChar();
                        m_idx++;
                    }
                }
                EatWhiteSpace();
            }
        }

        /// <summary>
        /// Add a parsed attribute to the collection.
        /// </summary>
        public void AddAttribute()
        {
            Add(new Attribute(ParseName, ParseValue, ParseDelim));
        }

        /// <summary>
        /// Get the current character that is being parsed.
        /// </summary>
        /// <returns></returns>
        public char GetCurrentChar()
        {
            return GetCurrentChar(0);
        }

        /// <summary>
        /// Get a few characters ahead of the current character.
        /// </summary>
        /// <param name="peek">How many characters to peek ahead
        /// for.</param>
        /// <returns>The character that was retrieved.</returns>
        public char GetCurrentChar(int peek)
        {
            if ((m_idx + peek) < Source.Length)
                return Source[m_idx + peek];
            else
                return (char)0;
        }

        /// <summary>
        /// Obtain the next character and advance the index by one.
        /// </summary>
        /// <returns>The next character</returns>
        public char AdvanceCurrentChar()
        {
            return Source[m_idx++];
        }

        /// <summary>
        /// Move the index forward by one.
        /// </summary>
        public void Advance()
        {
            m_idx++;
        }
    }
}
