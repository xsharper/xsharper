#region -- Copyrights --
// ***********************************************************************
//  This file is a part of XSharper (http://xsharper.com)
// 
//  Copyright (C) 2006 - 2010, Alexei Shamov, DeltaX Inc.
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//  
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
//  
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
// ************************************************************************
#endregion
using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.IO;
using System.Xml;

namespace XSharper.Core
{
    public partial class Utils
    {
        /// <summary>
        /// Convert data table rows to CSV
        /// </summary>
        /// <param name="dt">Data table</param>
        /// <param name="rows">Rows to convert to CSV</param>
        /// <param name="withHeader">true, if a row with column names must be added</param>
        /// <param name="quote">Character to use as quote</param>
        /// <param name="separator">Field separator</param>
        /// <returns>CSV lines</returns>
        public static string ToCsv(DataTable dt, System.Collections.IEnumerable rows, bool withHeader, char quote, char separator)
        {
            char[] csvSpecialChars = new char[] { '\r', '\n', separator, quote };
            int i = 0;
            StringBuilder output = new StringBuilder();
            if (withHeader)
            {
                for (i = 0; i < dt.Columns.Count; ++i)
                    writeCsv(output, i == 0, dt.Columns[i].Caption ?? dt.Columns[i].ColumnName, csvSpecialChars, quote, separator);
                output.AppendLine();
            }

            // Print rows
            foreach (DataRow row in rows)
            {
                for (i = 0; i < dt.Columns.Count; ++i)
                {
                    string s = (row[i] ?? string.Empty).ToString();
                    writeCsv(output, i == 0, s, csvSpecialChars, quote, separator);
                }
                output.AppendLine();
            }
            return output.ToString();
        }


        private static void writeCsv(StringBuilder output, bool first, string s, char[] csvSpecialChars, char quote, char separator)
        {
            if (!first)
                output.Append(separator);
            if (s.IndexOf(quote) != -1)
                s = s.Replace(new string(quote, 1), new string(quote, 2));
            if (s.IndexOfAny(csvSpecialChars) != -1)
            {
                output.Append(quote);
                output.Append(s);
                output.Append(quote);
            }
            else
                output.Append(s);

        }

        /// <summary>
        /// Convert data table rows to CSV
        /// </summary>
        /// <param name="dt">Data table</param>
        /// <param name="select">Statement to execute on top of the data table</param>
        /// <param name="sort">How to sort the data</param>
        /// <param name="withHeader">true, if a row with column names must be added</param>
        /// <param name="quote">Character to use as quote</param>
        /// <param name="separator">Field separator</param>
        /// <returns></returns>
        public static string ToCsv(DataTable dt, string select, string sort, bool withHeader, char quote, char separator)
        {
            return ToCsv(dt, dt.Select(select, sort), withHeader, quote, separator);
        }

        /// <summary>
        /// Convert data table rows to CSV, enclosing some values in double quotes and separating them with commas
        /// </summary>
        /// <param name="dt">Data table</param>
        /// <param name="select">Statement to execute on top of the data table</param>
        /// <param name="sort">How to sort the data</param>
        /// <param name="withHeader">true, if a row with column names must be added</param>
        public static string ToCsv(DataTable dt, string select, string sort, bool withHeader)
        {
            return ToCsv(dt, select, sort, withHeader, '"', ',');
        }

        /// <summary>
        /// Convert all data table rows to CSV, enclosing some values in double quotes and separating them with commas
        /// </summary>
        /// <param name="dt">Data table</param>
        /// <param name="withHeader">true, if a row with column names must be added</param>
        /// <param name="quote">Character to use as quote</param>
        /// <param name="separator">Field separator</param>
        /// <returns>CSV lines</returns>
        public static string ToCsv(DataTable dt, bool withHeader, char quote, char separator)
        {
            return ToCsv(dt, dt.Rows, withHeader, quote, separator);
        }

        /// <summary>
        /// Convert all data table rows to CSV, enclosing some values in double quotes and separating them with commas
        /// </summary>
        /// <param name="dt">Data table</param>
        /// <param name="withHeader">true, if a row with column names must be added</param>
        /// <returns>CSV lines</returns>
        public static string ToCsv(DataTable dt, bool withHeader)
        {
            return ToCsv(dt, withHeader, '"', ',');
        }

        /// <summary>
        /// Convert all data table rows to CSV, enclosing some values in double quotes and separating them with commas, with header
        /// </summary>
        /// <param name="dt">Data table</param>
        /// <returns>CSV lines</returns>
        public static string ToCsv(DataTable dt)
        {
            return ToCsv(dt, true);
        }

        
        /// <summary>
        /// Read values from text reader and return them, or null for EOF
        /// </summary>
        public static string[] ReadCsvRow(TextReader reader)
        {
            return ReadCsvRow(reader, '"', ',', true);
        }

        /// <summary>
        /// Read values from text reader and return them, or null for EOF
        /// </summary>
        /// <param name="reader">Text stream</param>
        /// <param name="quote">Quote</param>
        /// <param name="separator">Column separator</param>
        /// <param name="trimNonQuoted">true, if "aa,  bbb , ccc" translates to {"aa","bbb","ccc"}. false if it translates to { "aa", "  bbb "," ccc" }</param>
        /// <returns>null if EOF, otherwise the values</returns>
        public static string[] ReadCsvRow(TextReader reader, char? quote, char separator, bool trimNonQuoted)
        {
            List<string> data = new List<string>();

            bool wasQuoted = false;
            bool qinside = false;
            StringBuilder sb = new StringBuilder();
            StringBuilder sbWhiteSpace = new StringBuilder();
            int c;
            if (reader.Peek() == -1)
                return null;
            while ((c = reader.Read()) != -1)
            {
                char ch = (char)c;
                if (ch == separator && !qinside)
                {
                    if (!wasQuoted && sbWhiteSpace.Length != 0 && !trimNonQuoted)
                        sb.Append(sbWhiteSpace.ToString());
                    sbWhiteSpace.Length = 0;
                    data.Add(sb.ToString());
                    sb.Length = 0;
                    wasQuoted = false;
                    continue;
                }
                if ((ch == '\r' || ch == '\n') && !qinside)
                {
                    if (ch == '\r' && reader.Peek() == '\n')
                        reader.Read();
                    break;
                }
                if (ch == quote)
                {
                    if (!qinside || reader.Peek() != quote)
                    {
                        if (qinside)
                            wasQuoted = true;
                        else
                            sbWhiteSpace.Length = 0;
                        qinside = !qinside;
                        continue;
                    }
                    reader.Read();
                }
                if (char.IsWhiteSpace(ch))
                    sbWhiteSpace.Append(ch);
                else
                {
                    if (sbWhiteSpace.Length > 0)
                    {
                        bool app = true;
                        if (trimNonQuoted && sb.Length == 0 && !qinside)
                            app = false;
                        if (app)
                            sb.Append(sbWhiteSpace.ToString());
                    }
                    sbWhiteSpace.Length = 0;
                    sb.Append(ch);
                }
            }
            if (!wasQuoted && sbWhiteSpace.Length != 0 && !trimNonQuoted)
                sb.Append(sbWhiteSpace.ToString());

            data.Add(sb.ToString());
            return data.ToArray();
        }

        /// Convert data table to XML document, representing every row value as element
        public static XmlDocument ToXml(DataTable dt, string rowElement)
        {
            return ToXml(dt, dt.TableName, dt.Namespace, rowElement, null, false);
        }

        /// <summary>
        /// Convert data table to XML document
        /// </summary>
        /// <param name="dt">Data table</param>
        /// <param name="root">XML Document root element name</param>
        /// <param name="xmlns">XML namespace</param>
        /// <param name="rowElement">Name of the row element</param>
        /// <param name="nullValue">How to translate null value</param>
        /// <param name="useAttributes">true = store row values in XML attributes. false= store values in element nodes</param>
        /// <returns>XML document</returns>
        public static XmlDocument ToXml(DataTable dt, string root, string xmlns, string rowElement, string nullValue, bool useAttributes)
        {
            return ToXml(dt, dt.Rows, root, xmlns, rowElement, nullValue, useAttributes);
        }

        /// <summary>
        /// Convert data table to XML document
        /// </summary>
        /// <param name="dt">Data table</param>
        /// <param name="rows">Table rows to store in XML</param>
        /// <param name="root">XML Document root element name</param>
        /// <param name="xmlns">XML namespace</param>
        /// <param name="rowElement">Name of the row element</param>
        /// <param name="nullValue">How to translate null value</param>
        /// <param name="useAttributes">true = store row values in XML attributes. false= store values in element nodes</param>
        /// <returns>XML document</returns>
        public static XmlDocument ToXml(DataTable dt, System.Collections.IEnumerable rows, string root, string xmlns, string rowElement, string nullValue, bool useAttributes)
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlNode cn = xmlDoc.CreateElement(root, xmlns);
            xmlDoc.AppendChild(cn);

            string[] colName = new string[dt.Columns.Count];
            int i = 0;
            for (i = 0; i < dt.Columns.Count; ++i)
            {
                colName[i] = dt.Columns[i].ColumnName;
                if (colName[i].Length == 0)
                    colName[i] = "Column" + i;
            }
            foreach (DataRow row in rows)
            {
                XmlElement xn = xmlDoc.CreateElement(rowElement, xmlns);
                cn.AppendChild(xn);

                for (i = 0; i < colName.Length; ++i)
                {
                    object o = row[i];
                    if (o == null)
                        o = nullValue;
                    if (o != null)
                    {
                        if (useAttributes)
                            xn.SetAttribute(colName[i], Utils.To<string>(o));
                        else
                        {
                            var e = xmlDoc.CreateElement(colName[i], xmlns);
                            e.InnerText = Utils.To<string>(o);
                            xn.AppendChild(e);
                        }
                    }
                }
            }
            return xmlDoc;
        }

    }

}
