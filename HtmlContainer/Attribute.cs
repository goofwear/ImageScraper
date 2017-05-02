using System;

namespace HtmlContainer
{
    /// <summary>
    /// Attribute holds one attribute, as is normally stored in an
    /// HTML or XML file. This includes a name, value and delimiter.
    /// This source code may be used freely under the
    /// Limited GNU Public License(LGPL).
    ///
    /// Written by Jeff Heaton (http://www.jeffheaton.com)
    /// </summary>
    public class Attribute : ICloneable
    {
        /// <summary>
        /// The name of this attribute
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The value of this attribute
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// The delimiter for the value of this attribute(i.e. " or ').
        /// </summary>
        public char Delim { get; set; }

        /// <summary>
        /// Construct a new Attribute.  The name, delim, and value
        /// properties can be specified here.
        /// </summary>
        /// <param name="name">The name of this attribute.</param>
        /// <param name="value">The value of this attribute.</param>
        /// <param name="delim">The delimiter character for the value.
        /// </param>
        public Attribute(string name, string value, char delim)
        {
            Name = name;
            Value = value;
            Delim = delim;
        }

        /// <summary>
        /// The default constructor.  Construct a blank attribute.
        /// </summary>
        public Attribute() : this("", "", (char)0)
        {
        }

        /// <summary>
        /// Construct an attribute without a delimiter.
        /// </summary>
        /// <param name="name">The name of this attribute.</param>
        /// <param name="value">The value of this attribute.</param>
        public Attribute(String name, String value) : this(name, value, (char)0)
        {
        }

        #region ICloneable Members
        public virtual object Clone()
        {
            return new Attribute(Name, Value, Delim);
        }
        #endregion
    }
}