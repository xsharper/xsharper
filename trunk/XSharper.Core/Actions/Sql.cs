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
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Xml;

namespace XSharper.Core
{
    /// Sql result type
    public enum SqlMode
    {
        /// Non-query. There is no return value
        [Description("Non-query. There is no return value")]
        NonQuery,

        /// One rowset is output
        [Description("One rowset is output")]
        Query,

        /// One rowset is output by the query, which is converted to DataTable
        [Description("One rowset is output by the query, which is converted to DataTable")]
        DataTable,

        /// Output is XML
        [Description("Output is XML")]
        Xml,

        /// Output is a single value
        [Description("Output is a single value")]
        Scalar
    }


    /// Execute SQL command or query
    [XsType("sql", ScriptActionBase.XSharperNamespace)]
    [Description("Execute SQL command or query")]
    public class Sql : DynamicValueFromFileBase
    {
        /// Timeout
        [Description("Timeout")]
        public string Timeout { get; set; }

        /// Where to output messages, for example created by PRINT SQL statement. Default '^info'
        [Description("Where to output messages, for example created by PRINT SQL statement. Default '^info'")]
        public string MessagesTo { get; set; }

        /// Where to output errors. Default '^error'
        [Description("Where to output errors. Default '^error'")]
        public string ErrorsTo { get; set; }

        /// Command parameters
        [Description("Command parameters")]
        [XsElement("",SkipIfEmpty = true,CollectionItemType = typeof(SqlParam),CollectionItemElementName = "param",Ordering = -2)]
        public List<SqlParam> Parameters { get; set; }

        /// Where output goes. 
        [Description("Where output goes. ")]
        public string OutTo { get; set; }

        /// Type of the SQL query. Default - ResultType.NonQuery
        [Description("Type of the SQL query.")]
        [XsAttribute("mode")]
        [XsAttribute("resultType", Deprecated = true)]
        [XsAttribute("type",Deprecated=true)]
        public SqlMode Mode { get; set; }

        /// Table format options, for output
        [Description("Table format options, for output")]
        public TableFormatOptions TableFormat { get; set; }

        /// ID of the <see cref="RowSet"/> where the query output goes
        [Description("ID of the rowset where the query output goes")]
        public string ToRowsetId { get; set; }

        /// true if errors with severity below 17 should not throw exception and should print error message instead.
        [Description("true if errors with severity below 17 should not throw exception and should print error message instead.")]
        public bool IgnoreDataErrors { get; set; }
        
        /// Constructor
        public Sql()
        {
            Parameters = new List<SqlParam>();
            MessagesTo = "^out";
            ErrorsTo = "^error";
            Mode = SqlMode.NonQuery;
            TableFormat = TableFormatOptions.Default;
        }

        /// Execute action
        public override object Execute()
        {
            object o=base.Execute();
            if (o!=null)
                return o;

            
            string sql= GetTransformedValueStr();
            SqlUtil.RunCommand(Context, sql, Parameters, 
                            Utils.ToTimeSpan(Timeout),
                            ExecCommand,
                            Context.TransformStr(MessagesTo, Transform),
                            Context.TransformStr(ErrorsTo, Transform),
                            IgnoreDataErrors);
            return null;
        }
        
        /// Execute database command
        protected int ExecCommand(IDbCommand cmd)
        {
            string outTo = Context.TransformStr(OutTo, Transform);

            RowSet rowset = (!string.IsNullOrEmpty(ToRowsetId))
                ? Context.Find<RowSet>(Context.TransformStr(ToRowsetId, Transform),true) : null;
            if (rowset == null && !string.IsNullOrEmpty(outTo))
            {
                rowset = new RowSet();
                Context.Initialize(rowset);
            }
            bool canSave = (rowset != null);

            if (rowset != null)
            {
                rowset.Rows.Clear();
                rowset.Columns = null;
                rowset.Transform = TransformRules.None;
            }

            int nRows = 0;
            switch (Mode)
            {
                case SqlMode.Query:
                case SqlMode.DataTable:
                    using (var v = cmd.ExecuteReader())
                    {
                        if (v != null)
                            nRows = v.RecordsAffected;
                        if (v != null)
                        {

                            do
                            {
                                string[] colName = new string[v.FieldCount];
                                for (int i = 0; i < v.FieldCount; ++i)
                                {
                                    colName[i] = v.GetName(i) ?? string.Empty;
                                    if (colName[i].Length == 0)
                                        colName[i] = "Column" + i;
                                }

                                while (v.Read())
                                {
                                    Row r = new Row();
                                    for (int i = 0; i < v.FieldCount; ++i)
                                    {
                                        object o = v.GetValue(i);
                                        if (o == DBNull.Value)
                                            o = null;
                                        r[colName[i]] = o;
                                    }
                                    if (rowset!=null)
                                        rowset.Rows.Add(r);
                                }

                                // Print as table
                                if (outTo != null)
                                {
                                    if (Mode == SqlMode.DataTable)
                                    {
                                        if (canSave)
                                            Context.OutTo(outTo, rowset.ToDataTable("resultset"));
                                    }
                                    else if (rowset != null)
                                        Context.OutTo(outTo,rowset.ToTextTable(TableFormat));
                                }
                        
                                // Prepare for the next rowset
                                rowset = null;
                                if (!string.IsNullOrEmpty(outTo))
                                    rowset = new RowSet();
                                canSave = false;
                            } while (v.NextResult());
                        }
                    }


                    break;
                case SqlMode.Xml:
                    XmlDocument xdoc = new XmlDocument();
                    if (!(cmd is SqlCommand))
                        throw new NotSupportedException("This database type does not support XML");
                    using (var reader=((SqlCommand)cmd).ExecuteXmlReader())
                    {
                        xdoc.Load(reader);    
                    }
                    
                    if (!string.IsNullOrEmpty(outTo))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            XmlTextWriter tw = new XmlTextWriter(ms, System.Text.Encoding.UTF8);
                            tw.Formatting = Formatting.Indented;
                            xdoc.Save(tw);
                            tw.Flush();
                            ms.Position = 0;
                            Context.OutTo(outTo, new StreamReader(ms).ReadToEnd());
                        }
                    }
                    nRows = -1;
                    break;
                case SqlMode.NonQuery:
                    nRows = cmd.ExecuteNonQuery();
                    break;
                case SqlMode.Scalar:
                    object res = cmd.ExecuteScalar();
                    Context.OutTo(outTo, res);
                    break;
                default:
                    throw new ParsingException("Unknown SQL statement type");
            }
            return nRows;
        }
    }

    
}