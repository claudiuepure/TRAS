/*
dotNetRDF is free and open source software licensed under the MIT License

-----------------------------------------------------------------------------

Copyright (c) 2009-2012 dotNetRDF Project (dotnetrdf-developer@lists.sf.net)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is furnished
to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace VDS.RDF.Parsing
{
    /// <summary>
    /// Helper Class which defines some Test Functions for testing the Unicode Category of Characters
    /// </summary>
    public class UnicodeSpecsHelper
    {
        /// <summary>
        /// Start of high surrogate range
        /// </summary>
        public const int HighSurrogateStart = 0xD800;
        /// <summary>
        /// End of high surrogate range
        /// </summary>
        public const int HighSurrogateEnd = 0xDBFF;

        /// <summary>
        /// Start of low surrogate range
        /// </summary>
        public const int LowSurrogateStart = 0xDC00;
        /// <summary>
        /// End of low surrogate range
        /// </summary>
        public const int LowSurrogateEnd = 0xDFFF;

        /// <summary>
        /// Checks whether a given Character is considered a Letter
        /// </summary>
        /// <param name="c">Character to Test</param>
        /// <returns></returns>
        public static bool IsLetter(char c)
        {
            switch (GetUnicodeCategory(c))
            {
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.ModifierLetter:
                case UnicodeCategory.OtherLetter:
                case UnicodeCategory.TitlecaseLetter:
                case UnicodeCategory.UppercaseLetter:
                    return true;
                default:
                    return false;
            }
        }

        private static UnicodeCategory GetUnicodeCategory(char c)
        {
#if PORTABLE
            return CharUnicodeInfo.GetUnicodeCategory(c);
#else
            return Char.GetUnicodeCategory(c);
#endif
        }
        /// <summary>
        /// Checks whether a given Character is considered a Letter or Digit
        /// </summary>
        /// <param name="c">Character to Test</param>
        /// <returns></returns>
        public static bool IsLetterOrDigit(char c)
        {
            return (UnicodeSpecsHelper.IsLetter(c) || UnicodeSpecsHelper.IsDigit(c));
        }

        /// <summary>
        /// Checks whether a given Character is considered a Letter Modifier
        /// </summary>
        /// <param name="c">Character to Test</param>
        /// <returns></returns>
        public static bool IsLetterModifier(char c)
        {
            switch (GetUnicodeCategory(c))
            {
                case UnicodeCategory.ModifierLetter:
                case UnicodeCategory.NonSpacingMark:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks whether a given Character is considered a Digit
        /// </summary>
        /// <param name="c">Character to Test</param>
        /// <returns></returns>
        public static bool IsDigit(char c)
        {
            switch (GetUnicodeCategory(c))
            {
                case UnicodeCategory.DecimalDigitNumber:
                case UnicodeCategory.LetterNumber:
                case UnicodeCategory.OtherNumber:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Indicates whether the specified <see cref="T:System.Char"/> object is a high surrogate.
        /// </summary>
        /// 
        /// <returns>
        /// true if the numeric value of the <paramref name="c"/> parameter ranges from U+D800 through U+DBFF; otherwise, false.
        /// </returns>
        /// <param name="c">The Unicode character to evaluate. </param><filterpriority>1</filterpriority>
        public static bool IsHighSurrogate(char c)
        {
#if SILVERLIGHT
            return (c >= HighSurrogateStart && c <= HighSurrogateEnd);
#else
            return char.IsHighSurrogate(c);
#endif
        }

        /// <summary>
        /// Indicates whether the specified <see cref="T:System.Char"/> object is a low surrogate.
        /// </summary>
        /// 
        /// <returns>
        /// true if the numeric value of the <paramref name="c"/> parameter ranges from U+DC00 through U+DFFF; otherwise, false.
        /// </returns>
        /// <param name="c">The character to evaluate. </param><filterpriority>1</filterpriority>
        public static bool IsLowSurrogate(char c)
        {
#if SILVERLIGHT
            return (c >= LowSurrogateStart && c <= LowSurrogateEnd);
#else
            return char.IsLowSurrogate(c);
#endif
        }

        /// <summary>
        /// Converts the value of a UTF-16 encoded surrogate pair into a Unicode code point.
        /// </summary>
        /// 
        /// <returns>
        /// The 21-bit Unicode code point represented by the <paramref name="highSurrogate"/> and <paramref name="lowSurrogate"/> parameters.
        /// </returns>
        /// <param name="highSurrogate">A high surrogate code point (that is, a code point ranging from U+D800 through U+DBFF). </param>
        /// <param name="lowSurrogate">A low surrogate code point (that is, a code point ranging from U+DC00 through U+DFFF). </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="highSurrogate"/> is not in the range U+D800 through U+DBFF, or <paramref name="lowSurrogate"/> is not in the range U+DC00 through U+DFFF. </exception>
        /// <filterpriority>1</filterpriority>
        public static int ConvertToUtf32(char highSurrogate, char lowSurrogate)
        {
#if SILVERLIGHT
            //TODO: Should we use the algorithm from http://www.unicode.org/faq/utf_bom.html#utf16-2 instead?
            if (!IsHighSurrogate(highSurrogate))
                throw new ArgumentOutOfRangeException("highSurrogate");
            if (!IsLowSurrogate(lowSurrogate))
                throw new ArgumentOutOfRangeException("lowSurrogate");
            
            return (highSurrogate - HighSurrogateStart) * 1024 + (lowSurrogate - LowSurrogateStart) + 65536;
#else
            return char.ConvertToUtf32(highSurrogate, lowSurrogate);
#endif
        }

        /// <summary>
        /// Converts a Hex Escape into the relevant Unicode Character
        /// </summary>
        /// <param name="hex">Hex code</param>
        /// <returns></returns>
        public static char ConvertToChar(String hex)
        {
            if (hex.Length != 4) throw new RdfParseException("Unable to convert the String + '" + hex + "' into a Unicode Character, 4 characters were expected but received " + hex.Length + " characters");
            try
            {
                //Convert to an Integer
                int i = Convert.ToInt32(hex, 16);
                //Try to cast to a Char
                char c = (char)i;
                //Append to Output
                return c;
            }
            catch (Exception ex)
            {
                throw new RdfParseException("Unable to convert the String '" + hex + "' into a Unicode Character", ex);
            }
        }

        /// <summary>
        /// Converts a Hex Escape into the relevant UTF-16 codepoints
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static char[] ConvertToChars(String hex)
        {
            if (hex.Length != 8) throw new RdfParseException("Unable to convert the String + '" + hex + "' into a Unicode Character, 8 characters were expected but received " + hex.Length + " characters");
            try
            {
                //Convert to an Integer
                int i = Convert.ToInt32(hex, 16);
                if (i > Char.MaxValue)
                {
                    //UTF-32 character so down-convert to UTF-16
#if SILVERLIGHT
                    //Use the algorithm from http://www.unicode.org/faq/utf_bom.html#utf16-2

                    //UTF16 X = (UTF16) C;
                    //UTF32 U = (C >> 16) & ((1 << 5) - 1);
                    //UTF16 W = (UTF16) U - 1;
                    //UTF16 HiSurrogate = HI_SURROGATE_START | (W << 6) | X >> 10;
                    //where X, U and W correspond to the labels used in Table 3-5 UTF-16 Bit Distribution. 
                    int u = (i >> 16) & ((1 << 5) - 1);
                    var x = (ushort) i;
                    int w = u - 1;
                    int high = HighSurrogateStart | (w << 6) | (x >> 10);

                    //The next snippet does the same for the low surrogate.
                    //UTF16 X = (UTF16) C;
                    //UTF16 LoSurrogate = (UTF16) (LO_SURROGATE_START | X & ((1 << 10) - 1));
                    int low = LowSurrogateStart | x & ((1 << 10) - 1);
                    return new char[] { (char)high, (char)low };
#else
                    return Char.ConvertFromUtf32(i).ToCharArray();
#endif
                }
                else
                {
                    //Within single character range
                    return new char[] { (char)i };
                }
            }
            catch (Exception ex)
            {
                throw new RdfParseException("Unable to convert the String '" + hex + "' into Unicode characters", ex);
            }
        }
    }
}
