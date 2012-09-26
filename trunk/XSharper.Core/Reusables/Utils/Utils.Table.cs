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
using System.ComponentModel;
using System.Text;
using System.Data;
using System.Collections;
using System.Globalization;

namespace XSharper.Core
{

    /// <summary>
    /// Table format options
    /// </summary>
    [Flags]
    public enum TableFormatOptions : int
    {
        /// No options
        [Description("No options")]
        None = 0,

        /// Output table header and footer
        [Description("Output table header and footer")]
        Header = 0x01000000,

        /// Draw table lines like 
        /// +---+-----+
        /// | A |  B  |
        /// +---+-----+
        [Description("Draw table lines using + and -")]
        Lines = 0x02000000,

        /// Output number of rows in the footer
        [Description("Output number of rows in the footer")]
        Count = 0x04000000,
        

        /// Output every row as record, i.e. two column table with field names on the left, and values on the right
        [Description("Output every row as record, i.e. two column table with field names on the left, and values on the right")]
        Records = 0x08000000,

        /// Right-align numbers
        [Description("Right-align numbers")]
        NumbersRight = 0x10000000,

        /// For trimmed columns put ellipsis at the beginning
        [Description("For trimmed columns put ellipsis at the beginning")]
        EllipsisStart = 0x20000000,

        /// Create HTML table instead of drawing table lines
        [Description("Create HTML table instead of drawing table lines")]
        Html = 0x40000000,


        /// low 8 bits represent max column width
        [Description("low 8 bits represent max column width")]
        MaxColWidthMask = 0xff,

        /// Header | Lines | NumbersRight| 200
        [Description("Header | Lines | NumbersRight| 200")]
        Default = Header | Lines | NumbersRight | 200
    }

    public partial class Utils
    {
        /// <summary>
        /// Convert all public properties of enumerable to a data table
        /// </summary>
        /// <param name="enumerable">Interface to enumerate</param>
        /// <returns>Data table constructed, or null if enumerable has no rows</returns>
        public static DataTable ToDataTable(IEnumerable enumerable)
        {
            if (enumerable==null)
                return null;
            DataTable dt = null;
            foreach (var o in enumerable)
            {
                if (o == null)
                    continue;
                if (dt == null)
                {
                    dt = new DataTable(o.GetType().Name);
                    foreach (var prop in o.GetType().GetProperties())
                    {
                        if (prop.GetIndexParameters().Length == 0)
                            dt.Columns.Add(new DataColumn(prop.Name, prop.PropertyType));
                    }
                }
                var r = dt.NewRow();
                foreach (var prop in o.GetType().GetProperties())
                {
                    if (prop.GetIndexParameters().Length == 0)
                        r[prop.Name] = prop.GetValue(o, null);
                }
                dt.Rows.Add(r);
            }
            return dt;
        }

        /// Convert all public properties to a text table with default formatting options
        public static string ToTextTable(IEnumerable dt)
        {
            return ToTextTable(dt, TableFormatOptions.Default);
        }

        /// Convert all public properties to a text table with specified formatting options
        public static string ToTextTable(IEnumerable enumerable, TableFormatOptions options)
        {
            return ToTextTable(ToDataTable(enumerable), options);
        }


        /// Convert DataTable to a text table with default formatting options
        public static string ToTextTable(DataTable dt)
        {
            return ToTextTable(dt, dt.Rows, TableFormatOptions.Default);
        }

        /// Convert DataTable to a text table with specified formatting options
        public static string ToTextTable(DataTable dt, TableFormatOptions options)
        {
            return ToTextTable(dt, dt.Rows, options);
        }

        /// Select and sort some rows from the data table, then format them with the specified formatting options
        public static string ToTextTable(DataTable dt, string select, string sort, TableFormatOptions options)
        {
            return ToTextTable(dt, dt.Select(select, sort), options);
        }

        /// Select and sort some rows from the data table, then format them with default formatting options
        public static string ToTextTable(DataTable dt, string select, string sort)
        {
            return ToTextTable(dt, dt.Select(select, sort), TableFormatOptions.Default);
        }

        /// Select from the data table, then format them with default formatting options
        public static string ToTextTable(DataTable dt, string select)
        {
            return ToTextTable(dt, dt.Select(select, null), TableFormatOptions.Default);
        }


        /// Format some rows of the data table using the specified formatting options
        public static string ToTextTable(DataTable dt, IEnumerable datarows, TableFormatOptions options)
        {
            StringBuilder output = new StringBuilder();
            if (dt.Columns.Count == 0)
                return string.Empty;
            int[] colMaxWidth = new int[dt.Columns.Count];
            string[] colName = new string[dt.Columns.Count];
            bool[] rightAlign = new bool[dt.Columns.Count];
            int i = 0;
            for (i = 0; i < dt.Columns.Count; ++i)
            {
                colName[i] = dt.Columns[i].Caption ?? dt.Columns[i].ColumnName;
                colMaxWidth[i] = colName[i].Length + 2;
            }

            if ((options & TableFormatOptions.Records) != 0)
            {
                DataTable dt1 = new DataTable(dt.TableName + " (records)");
                dt1.Columns.Add("Field");
                dt1.Columns.Add("Value", typeof(object));
                foreach (DataRow row in datarows)
                {
                    dt1.Rows.Clear();
                    for (i = 0; i < colMaxWidth.Length; ++i)
                        dt1.Rows.Add(colName[i], row[i]);
                    output.AppendLine(ToTextTable(dt1, (options & ~TableFormatOptions.Records)));
                }
                return output.ToString();
            }

            List<string[]> rows = new List<string[]>();
            foreach (DataRow row in datarows)
            {
                string[] sv = new string[colMaxWidth.Length];
                for (i = 0; i < colMaxWidth.Length; ++i)
                {
                    var val = toDisplay(row[i], options);
                    if ((options & TableFormatOptions.NumbersRight) != 0 && !rightAlign[i] && row[i] != null)
                    {
                        Type t = row[i].GetType();
                        if (t.IsPrimitive || t == typeof(decimal))
                            rightAlign[i] = true;
                    }
                    sv[i] = val;
                    if (val.Length + 2 > colMaxWidth[i])
                        colMaxWidth[i] = val.Length + 2;
                }
                rows.Add(sv);
            }
            bool withLines = ((options & TableFormatOptions.Lines) != 0);
            bool withHeader = ((options & TableFormatOptions.Header) != 0);
            bool html = ((options & TableFormatOptions.Html) != 0);


            // Print header
            StringBuilder brk = new StringBuilder();
            int totalWidth = 0;
            foreach (var col in colMaxWidth)
            {
                totalWidth += col + ((brk.Length == 0) ? 0 : 1);
                brk.Append(withLines ? "+" : (brk.Length == 0) ? "" : " ");
                brk.Append(new string('-', col));
            }
            if (withLines)
                brk.Append('+');
            brk.AppendLine();

            if (html)
                output.Append(withLines ? "<table border='1' class='xshTable'>" : "<table class='xshTable'>");
            else if (withLines)
                output.Append(brk.ToString());

            if (withHeader)
            {
                if (html)
                    output.Append("<thead><tr>");
                // Print column names
                for (i = 0; i < colName.Length; ++i)
                {
                    if (html)
                    {
                        output.Append("<th>");
                        output.Append(EscapeHtml(colName[i]));
                        output.Append("</th>");
                    }
                    else
                    {
                        string s = " " + colName[i] + " ";
                        output.Append(withLines ? "|" : (i == 0) ? "" : " ");
                        if (rightAlign[i])
                            output.Append(s.PadLeft(colMaxWidth[i]));
                        else
                            output.Append(s.PadRight(colMaxWidth[i]));
                    }
                }
                if (html)
                    output.AppendLine("</tr></thead>");
                else
                {
                    output.Append(withLines ? "|" : "");
                    output.AppendLine();

                    // Print separator again
                    output.Append(brk.ToString());
                }
            }

            // Print rows
            if (html)
                output.AppendLine("<tbody>");
            foreach (var r in rows)
            {
                if (html)
                    output.Append("<tr>");

                for (i = 0; i < colName.Length; ++i)
                {
                    string rw = r[i] ?? string.Empty;
                    if (html)
                    {
                        output.Append(rightAlign[i] ? "<td style='text-align:right;'>" : "<td>");
                        output.Append(EscapeHtml(rw));
                        output.Append("</td>");
                    }
                    else
                    {
                        output.Append(withLines ? "|" : (i == 0) ? "" : " ");
                        output.Append(" ");
                        if (rightAlign[i])
                            output.Append(rw.PadLeft(colMaxWidth[i] - 2));
                        else
                            output.Append(rw.PadRight(colMaxWidth[i] - 2));
                        output.Append(" ");
                    }
                }
                if (html)
                    output.Append("</tr>");
                else
                    output.Append(withLines ? "|" : "");
                output.AppendLine();
            }
            if (html)
                output.AppendLine("</tbody>");
            
            // Print footer - separator again
            bool withCount = ((options & TableFormatOptions.Count) != 0);
            if (withCount)
            {
                if (html)
                {
                    output.AppendFormat("<tfoot><tr><th colspan='{0}'>{1} rows</th></tr></tfoot>", colMaxWidth.Length, rows.Count);
                    output.AppendLine();
                }
                else
                {
                    if (withLines || withHeader)
                        output.Append(brk.ToString());
                    output.Append(withLines ? "|" : "");
                    output.Append((" " + rows.Count + " rows").PadRight(totalWidth, ' '));
                    output.Append(withLines ? "|" : "");
                    if (withLines)
                        output.AppendLine();
                }
            }
            if (html)
                output.AppendLine("</table>");
            else if (withLines)
                output.Append(brk.ToString());
            return output.ToString();
        }

        static string toDisplay(object p, TableFormatOptions tf)
        {
            if (p == null)
                return string.Empty;
            Type t = p.GetType();
            if (t == typeof(decimal) || t == typeof(decimal?))
                p = string.Format("{0:##0.00}", p);
            else if (t == typeof(double) || t == typeof(double?) || t == typeof(float) || t == typeof(float?))
                p = string.Format("{0:G}", p);
            else if (t == typeof(bool) || t == typeof(bool?))
                p = string.Format("{0}", p);
            else if (t == typeof(char) || t == typeof(char?))
                p = new string((char)p, 1);
            else if (t.IsPrimitive)
                p = string.Format("{0}", p);
            string ret = Utils.TransformStr(Utils.To<string>(p), TransformRules.RemoveControl) ?? string.Empty;
            if ((tf & TableFormatOptions.MaxColWidthMask) != 0)
                ret = Utils.FitWidth(ret, (int)(tf & TableFormatOptions.MaxColWidthMask), ((tf & TableFormatOptions.EllipsisStart) != 0) ? FitWidthOption.EllipsisStart : FitWidthOption.EllipsisEnd);
            return ret;
        }

    }
}
