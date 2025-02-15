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

#if NO_WEB

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VDS.RDF.Writing;

namespace VDS.RDF.Writing
{
    /// <summary>
    /// Custom implementation of <see cref="System.Web.UI.HtmlTextWriter">System.Web.UI.HtmlTextWriter</see> to replace it in builds where System.Web is not available
    /// </summary>
    /// <remarks>
    /// <para>
    /// Note that this is not a full implementation of HtmlTextWriter as per the original class, it simply emulates all the functionality that dotNetRDF requires for it's HTML outputting
    /// </para>
    /// </remarks>
    public class HtmlTextWriter : TextWriter, IDisposable
    {
        private TextWriter _writer;
        private bool _newline = true;
        private int _indent = 0;
        private Stack<String> _tags = new Stack<String>();
        private List<KeyValuePair<String, String>> _attributes = new List<KeyValuePair<String, String>>();
        private List<KeyValuePair<String, String>> _styles = new List<KeyValuePair<String, String>>();

        /// <summary>
        /// Creates a new HTML Text Writer
        /// </summary>
        /// <param name="writer">Text Writer</param>
        public HtmlTextWriter(TextWriter writer)
        {
            this._writer = writer;
        }

        /// <summary>
        /// Gets the encoding of the Inner Writer
        /// </summary>
        public override Encoding Encoding
        {
            get
            {
                return this._writer.Encoding;
            }
        }

        /// <summary>
        /// Gets/Sets the current Indent
        /// </summary>
        public Int32 Indent
        {
            get
            {
                return this._indent;
            }
            set
            {
                this._indent = (value >= 0 ? value : 0);
            }
        }

        /// <summary>
        /// Gets the Inner Writer
        /// </summary>
        public TextWriter InnerWriter
        {
            get
            {
                return this._writer;
            }
        }

        private String EncodeAttribute(String value)
        {
            value = WriterHelper.EncodeForXml(value);
            if (value.EndsWith("&")) value += "amp;";
            value = value.Replace("\"", "&quot;");
            value = value.Replace("<", "&lt;");
            value = value.Replace(">", "&gt;");
            return value;
        }

        private String EncodeStyle(String value)
        {
            value = WriterHelper.EncodeForXml(value);
            if (value.EndsWith("&")) value += "amp;";
            value = value.Replace("\"", "'");
            value = value.Replace("<", "&lt;");
            value = value.Replace(">", "&gt;");
            return value;
        }

        private String GetAttributeName(HtmlTextWriterAttribute key)
        {
            String name = key.ToString().ToLower();
            if (name.Contains(".")) name = name.Substring(name.LastIndexOf(".") + 1);
            return name;
        }

        private String GetStyleName(HtmlTextWriterStyle key)
        {
            String name = key.ToString();
            if (name.Contains(".")) name = name.Substring(name.LastIndexOf(".") + 1);

            StringBuilder output = new StringBuilder();
            char[] cs = name.ToCharArray();
            for (int i = 0; i < cs.Length; i++)
            {
                if (Char.IsUpper(cs[i]) && i > 0)
                {
                    output.Append('-');
                    output.Append(cs[i]);
                }
                else
                {
                    output.Append(cs[i]);
                }
            }
            return output.ToString();
        }

        private String GetTagName(HtmlTextWriterTag key)
        {
            String name = key.ToString().ToLower();
            if (name.Contains(".")) name = name.Substring(name.LastIndexOf(".") + 1);
            return name;
        }

        /// <summary>
        /// Adds an attribute to the next element to be written
        /// </summary>
        /// <param name="name">Attribute Name</param>
        /// <param name="value">Value</param>
        public void AddAttribute(String name, String value)
        {
            this._attributes.Add(new KeyValuePair<String, String>(name, this.EncodeAttribute(value)));
        }

        /// <summary>
        /// Adds an attribute to the next element to be written
        /// </summary>
        /// <param name="name">Attribute Name</param>
        /// <param name="value">Value</param>
        /// <param name="fEncode">Whether to encode the attribute value</param>
        public void AddAttribute(String name, String value, Boolean fEncode)
        {
            if (fEncode) value = this.EncodeAttribute(value);
            this._attributes.Add(new KeyValuePair<String, String>(name, value));
        }

        /// <summary>
        /// Adds an attribute to the next element to be written
        /// </summary>
        /// <param name="key">Attribute</param>
        /// <param name="value">Value</param>
        public void AddAttribute(HtmlTextWriterAttribute key, String value)
        {
            this.AddAttribute(this.GetAttributeName(key), value);
        }

        /// <summary>
        /// Adds an attribute to the next element to be written
        /// </summary>
        /// <param name="key">Attribute</param>
        /// <param name="value">Value</param>
        /// <param name="fEncode">Whether to encode the attribute value</param>
        public void AddAttribute(HtmlTextWriterAttribute key, String value, Boolean fEncode)
        {
            this.AddAttribute(this.GetAttributeName(key), value, fEncode);
        }

        /// <summary>
        /// Adds a CSS style that will be used in the style attribute of the next element to be written
        /// </summary>
        /// <param name="name">CSS Attribute Name</param>
        /// <param name="value">Value</param>
        public void AddStyleAttribute(String name, String value)
        {
            this._styles.Add(new KeyValuePair<String, String>(name, value));
        }

        /// <summary>
        /// Adds a CSS style that will be used in the style attribute of the next element to be written
        /// </summary>
        /// <param name="key">CSS Attribute</param>
        /// <param name="value">Value</param>
        public void AddStyleAttribute(HtmlTextWriterStyle key, String value)
        {
            this._styles.Add(new KeyValuePair<String, String>(this.GetStyleName(key), value));
        }

#if PORTABLE
        /// <summary>
        /// Close the writer
        /// </summary>
        public void Close()
#else
        /// <summary>
        /// Close the writer
        /// </summary>
        public override void Close()
#endif
        {
            this._writer.Close();
        }

        /// <summary>
        /// Flush the writer
        /// </summary>
        public override void Flush()
        {
            this._writer.Flush();
        }

        /// <summary>
        /// Writes the begin tag for an element
        /// </summary>
        /// <param name="tagName">Tag Name</param>
        public void RenderBeginTag(String tagName)
        {
            this._tags.Push(tagName);
            if (this._newline)
            {
                this._writer.Write(new String('\t', this._indent));
                this._newline = false;
            }
            this._writer.Write("<" + tagName.ToLower());
            foreach (KeyValuePair<String, String> attr in this._attributes)
            {
                this._writer.Write(" " + attr.Key + "=\"" + attr.Value + "\"");
            }
            this._attributes.Clear();

            if (this._styles.Count > 0)
            {
                this._writer.Write(" style=\"");
                foreach (KeyValuePair<String, String> style in this._styles)
                {
                    this._writer.Write(style.Key + ": " + this.EncodeStyle(style.Value) + ";");
                }
                this._writer.Write("\"");
                this._styles.Clear();
            }
            this._writer.WriteLine(">");
            this._newline = true;
            this._indent++;
        }

        /// <summary>
        /// Writes the begin tag for an element
        /// </summary>
        /// <param name="tagKey">Tag</param>
        public void RenderBeginTag(HtmlTextWriterTag tagKey)
        {
            this.RenderBeginTag(this.GetTagName(tagKey));
        }

        /// <summary>
        /// Writes the end tag for an element
        /// </summary>
        public void RenderEndTag()
        {
            if (this._tags.Count > 0)
            {
                if (this._indent > 0) this._indent--;
                if (!this._newline) this._writer.WriteLine();
                this._writer.Write(new String('\t', this._indent));
                this._writer.WriteLine("</" + this._tags.Pop().ToLower() + ">");
                this._newline = true;
            }
            else
            {
                throw new Exception("Cannot end a tag as there are no open tags");
            }
        }

        /// <summary>
        /// Writes a character
        /// </summary>
        /// <param name="value">Character</param>
        public override void Write(char value)
        {
            this._writer.Write(value);
        }

        /// <summary>
        /// Writes a boolean
        /// </summary>
        /// <param name="value">Boolean</param>
        public override void Write(bool value)
        {
            this._writer.Write(value);
        }

        /// <summary>
        /// Writes some characters
        /// </summary>
        /// <param name="buffer">Characters</param>
        public override void Write(char[] buffer)
        {
            this._writer.Write(buffer);
        }

        /// <summary>
        /// Writes some portion of the given characters
        /// </summary>
        /// <param name="buffer">Characters</param>
        /// <param name="index">Index to start at</param>
        /// <param name="count">Number of characters to write</param>
        public override void Write(char[] buffer, int index, int count)
        {
            this._writer.Write(buffer, index, count);
        }

        /// <summary>
        /// Writes a decimal
        /// </summary>
        /// <param name="value">Decimal</param>
        public override void Write(decimal value)
        {
            this._writer.Write(value);
        }

        /// <summary>
        /// Writes a double
        /// </summary>
        /// <param name="value">Double</param>
        public override void Write(double value)
        {
            this._writer.Write(value);
        }

        /// <summary>
        /// Writes a float
        /// </summary>
        /// <param name="value">Float</param>
        public override void Write(float value)
        {
            this._writer.Write(value);
        }

        /// <summary>
        /// Writes an integer
        /// </summary>
        /// <param name="value">Integer</param>
        public override void Write(int value)
        {
            this._writer.Write(value);
        }

        /// <summary>
        /// Writes a long integer
        /// </summary>
        /// <param name="value">Long Integer</param>
        public override void Write(long value)
        {
            this._writer.Write(value);
        }

        /// <summary>
        /// Writes an object
        /// </summary>
        /// <param name="value">Object</param>
        public override void Write(object value)
        {
            this._writer.Write(value);
        }

#if PORTABLE
        public void Write(string format, object arg0)
#else
        /// <summary>
        /// Writes a formatted string
        /// </summary>
        /// <param name="format">String with format</param>
        /// <param name="arg0">Argument to insert into string</param>
        public override void Write(string format, object arg0)
#endif
        {
            this._writer.Write(format, arg0);
        }

#if PORTABLE
        public void Write(string format, object arg0, object arg1)
#else
        /// <summary>
        /// Writes a formatted string
        /// </summary>
        /// <param name="format">String with format</param>
        /// <param name="arg0">Argument to insert into string</param>
        /// <param name="arg1">Argument to insert into string</param>
        public override void Write(string format, object arg0, object arg1)
#endif
        {
            this._writer.Write(format, arg0, arg1);
        }

        /// <summary>
        /// Writes a formatted string
        /// </summary>
        /// <param name="format">String with format</param>
        /// <param name="arg">Arguments to insert into string</param>
        public override void Write(string format, params object[] arg)
        {
            this._writer.Write(format, arg);
        }

        /// <summary>
        /// Writes a string
        /// </summary>
        /// <param name="value">String</param>
        public override void Write(string value)
        {
            this._writer.Write(value);
        }

        /// <summary>
        /// Writes an unsigned integer
        /// </summary>
        /// <param name="value">Unsigned Integer</param>
        public override void Write(uint value)
        {
            this._writer.Write(value);
        }

        /// <summary>
        /// Writes an unsigned long integer
        /// </summary>
        /// <param name="value">Unsigned Long Integer</param>
        public override void Write(ulong value)
        {
            this._writer.Write(value);
        }

        /// <summary>
        /// Writes an attribute
        /// </summary>
        /// <param name="name">Attribute Name</param>
        /// <param name="value">Value</param>
        public void WriteAttribute(String name, String value)
        {
            this._writer.Write(name + "=\"" + this.EncodeAttribute(value) + "\"");
        }

        /// <summary>
        /// Writes an attribute
        /// </summary>
        /// <param name="name">Attribute Name</param>
        /// <param name="value">Value</param>
        /// <param name="fEncode">Whether to encode the value</param>
        public void WriteAttribute(String name, String value, Boolean fEncode)
        {
            if (fEncode) value = this.EncodeAttribute(value);
            this._writer.Write(name + "=\"" + value + "\"");
        }

        /// <summary>
        /// Writes a begin tag but does not terminate it so that methods like <see cref="HtmlTextWriter.WriteAttribute(String,String)"/> may be used
        /// </summary>
        /// <param name="tagName">Tag Name</param>
        public void WriteBeginTag(String tagName)
        {
            if (this._newline)
            {
                this._writer.Write(new String('\t', this._indent));
                this._newline = false;
            }
            this._writer.Write("<" + tagName.ToLower());
        }

        /// <summary>
        /// Writes a line break
        /// </summary>
        public void WriteBreak()
        {
            if (this._newline)
            {
                this._writer.WriteLine(new String('\t', this._indent));
                this._newline = false;
            }
            this._writer.Write("<br />");
        }

        /// <summary>
        /// Writes encoded text
        /// </summary>
        /// <param name="text">Text</param>
        public void WriteEncodedText(String text)
        {
            if (this._newline)
            {
                this._writer.Write(new String('\t', this._indent));
                this._newline = false;
            }
            this._writer.Write(HttpUtility.HtmlEncode(text));
        }

        /// <summary>
        /// Writes an encoded URL
        /// </summary>
        /// <param name="url">URL</param>
        public void WriteEncodedUrl(String url)
        {
            this._writer.Write(Uri.EscapeUriString(url));
        }

        /// <summary>
        /// Writes an encoded URL parameter
        /// </summary>
        /// <param name="urlText">URL parameter</param>
        public void WriteEncodedUrlParameter(String urlText)
        {
            this._writer.Write(HttpUtility.UrlEncode(urlText));
        }

        /// <summary>
        /// Writes an end tag
        /// </summary>
        /// <param name="tagName">Tag Name</param>
        public void WriteEndTag(String tagName)
        {
            if (this._newline)
            {
                this._writer.Write(new String('\t', this._indent));
                this._newline = false;
            }
            this._writer.Write("</" + tagName.ToLower() + ">");
        }

        /// <summary>
        /// Writes a begin tag with the terminating &lt;, use <see cref="WriteBeginTag(String)"/> instead if you need to add attributes afterwards
        /// </summary>
        /// <param name="tagName">Tag Name</param>
        public void WriteFullBeginTag(String tagName)
        {
            if (this._newline)
            {
                this._writer.Write(new String('\t', this._indent));
                this._newline = false;
            }
            this._writer.Write("<" + tagName.ToLower() + ">");
        }

        /// <summary>
        /// Writes a new line
        /// </summary>
        public override void WriteLine()
        {
            this._writer.WriteLine();
        }

        /// <summary>
        /// Writes a boolean followed by a new line
        /// </summary>
        /// <param name="value">Boolean</param>
        public override void WriteLine(bool value)
        {
            this._writer.WriteLine(value);
        }

        /// <summary>
        /// Writes a character followed by a new line
        /// </summary>
        /// <param name="value">Character</param>
        public override void WriteLine(char value)
        {
            this._writer.WriteLine(value);
        }

        /// <summary>
        /// Writes some characters followed by a new line
        /// </summary>
        /// <param name="buffer">Characters</param>
        public override void WriteLine(char[] buffer)
        {
            this._writer.WriteLine(buffer);
        }

        /// <summary>
        /// Writes some portion of the characters followed by a new line
        /// </summary>
        /// <param name="buffer">Characters</param>
        /// <param name="index">Index to start at</param>
        /// <param name="count">Number of characters to write</param>
        public override void WriteLine(char[] buffer, int index, int count)
        {
            this._writer.WriteLine(buffer, index, count);
        }

        /// <summary>
        /// Writes a decimal followed by a new line
        /// </summary>
        /// <param name="value">Decimal</param>
        public override void WriteLine(decimal value)
        {
             this._writer.WriteLine(value);
        }

        /// <summary>
        /// Writes a double followed by a new line
        /// </summary>
        /// <param name="value">Double</param>
        public override void WriteLine(double value)
        {
            this._writer.WriteLine(value);
        }

        /// <summary>
        /// Writes a float followed by a new line
        /// </summary>
        /// <param name="value">Float</param>
        public override void WriteLine(float value)
        {
            this._writer.WriteLine(value);
        }

        /// <summary>
        /// Writes an integer followed by a new line
        /// </summary>
        /// <param name="value">Integer</param>
        public override void WriteLine(int value)
        {
            this._writer.WriteLine(value);
        }

        /// <summary>
        /// Writes a long integer followed by a new line
        /// </summary>
        /// <param name="value">Long Integer</param>
        public override void WriteLine(long value)
        {
            this._writer.WriteLine(value);
        }

        /// <summary>
        /// Writes an object followed by a new line
        /// </summary>
        /// <param name="value">Object</param>
        public override void WriteLine(object value)
        {
            this._writer.WriteLine(value);
        }

        /// <summary>
        /// Writes a string followed by a new line
        /// </summary>
        /// <param name="value">String</param>
        public override void WriteLine(string value)
        {
            this._writer.WriteLine(value);
        }

#if PORTABLE
        public void WriteLine(string format, object arg0)
#else
        /// <summary>
        /// Writes a formatted string followed by a new line
        /// </summary>
        /// <param name="format">String</param>
        /// <param name="arg0">Argument to insert into string</param>
        public override void WriteLine(string format, object arg0)
#endif
        {
            this._writer.WriteLine(format, arg0);
        }

#if PORTABLE
        public void WriteLine(string format, object arg0, object arg1)
#else
        /// <summary>
        /// Writes a formatted string followed by a new line
        /// </summary>
        /// <param name="format">String</param>
        /// <param name="arg0">Argument to insert into string</param>
        /// <param name="arg1">Argument to insert into string</param>
        public override void WriteLine(string format, object arg0, object arg1)
#endif
        {
            this._writer.WriteLine(format, arg0, arg1);
        }

        /// <summary>
        /// Writes a formatted string followed by a new line
        /// </summary>
        /// <param name="format">String</param>
        /// <param name="arg">Arguments to insert into string</param>
        public override void WriteLine(string format, params object[] arg)
        {
            this._writer.WriteLine(format, arg);
        }

        /// <summary>
        /// Writes an unsigned integer followed by a new line
        /// </summary>
        /// <param name="value">Unsigned Integer</param>
        public override void WriteLine(uint value)
        {
            this._writer.WriteLine(value);
        }

        /// <summary>
        /// Writes an unsigned long integer followed by a new line
        /// </summary>
        /// <param name="value">Unsigned Long Integer</param>
        public override void WriteLine(ulong value)
        {
            this._writer.WriteLine(value);
        }

        /// <summary>
        /// Writes a string on a line with no tabs
        /// </summary>
        /// <param name="s">String</param>
        public void WriteLineNoTabs(String s)
        {
            this._writer.WriteLine();
            this._writer.WriteLine(s);
        }

        /// <summary>
        /// Writes a style attribute
        /// </summary>
        /// <param name="name">Attribute Name</param>
        /// <param name="value">Value</param>
        public void WriteStyleAttribute(String name, String value)
        {
            this._writer.Write(name + ": " + this.EncodeStyle(value) + ";");
        }

        /// <summary>
        /// Writes a style attribute
        /// </summary>
        /// <param name="name">Attribute Name</param>
        /// <param name="value">Value</param>
        /// <param name="fEncode">Whether to encode the value</param>
        public void WriteStyleAttribute(String name, String value, Boolean fEncode)
        {
            if (fEncode) value = this.EncodeAttribute(value);
            this._writer.Write(name + ": " + value + ";");
        }

    }
}

#endif