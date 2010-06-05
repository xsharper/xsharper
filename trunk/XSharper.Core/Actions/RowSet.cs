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
using System.IO;
using System.Text;
using System.Xml;
using System.Collections;
using System.Text.RegularExpressions;
using System.Globalization;

namespace XSharper.Core
{
    /// Sort direction
    public enum ColumnSortDirection
    {
        /// No sorting
        None,

        /// Sort in ascending order
        Ascending,

        /// Sort in descending order
        Descending,

        /// Sort in ascending order
        Asc=Ascending,

        /// Sort in descending order
        Desc=Descending
    }

    /// Column
    [XsType(null, ScriptActionBase.XSharperNamespace, AnyAttribute = true)]
    [Description("Column")]
    public class Col : XsTransformableElement
    {
        /// Column name
        [Description("Column name")]
        public string Name { get; set; }

        /// Column type (may be null if not changed)
        [Description("Column type")]
        public object Type { get; set; }

        /// Column value
        [Description("Source column name, or XPath")]
        [XsAttribute("")]
        [XsAttribute("value")]
        [XsAttribute("xpath")]
        public string Value { get; set; }

        /// Column value
        [Description("Default value")]
        [XsAttribute("")]
        [XsAttribute("default")]
        public object Default { get; set; }

        /// <summary>
        /// Called when XML Reader reads an attribute or a text field
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="attribute">Attribute name, or an empty string for element text</param>
        /// <param name="value">Attribute value</param>
        /// <param name="previouslyProcessed">List of previously processed attributes, to detect duplicate attributes. May be null if duplicate attributes are allowed.</param>
        /// <returns>true, if the attribute if correctly processed and false otherwise</returns>
        protected override bool ProcessAttribute(IXsContext context, string attribute, string value, IDictionary<string, bool> previouslyProcessed)
        {
            if (!base.ProcessAttribute(context, attribute, value, previouslyProcessed))
            {
                if (Name != null)
                    throw new ParsingException("Only a single variable may be set by set command.");
                Name = attribute;
                Value = value;
                if (previouslyProcessed != null && !string.IsNullOrEmpty(attribute))
                    previouslyProcessed.Add(attribute, true);
            }
            return true;
        }
    }

    /// Sort Column
    [XsType(null, ScriptActionBase.XSharperNamespace)]
    [Description("Column")]
    public class SortCol : XsTransformableElement
    {
        /// Column name
        [Description("Column name")]
        public string Name { get; set; }

        /// Comparer type or object
        [Description("Comparer type or object")]
        public object Comparer { get; set; }

        /// Sort direction
        [Description("Sort direction")]
        [XsAttribute("sort")]
        public ColumnSortDirection Sort { get; set; }
    }

    /// Rowset options
    [Flags]
    public enum RowsetOptions
    {
        /// No options
        [Description("No options")]
        None,

        /// Ignore leading and trailing spaces
        [Description("Ignore leading and trailing spaces")]
        Trim=1,

        /// Ignore empty lines
        [Description("Ignore empty lines")]
        IgnoreEmpty=2,

        /// Assume first row to be CSV field names
        [Description("Assume first row to be CSV field names")]
        Header=4,

        /// Default value ( Trim|IgnoreEmpty )
        [Description("Default value ( Trim|IgnoreEmpty )")]
        Default=Trim|IgnoreEmpty
    }

    /// A set of rows
    [XsType(null)]
    [Description("Where clause")]
    public class Where : Conditional
    {
        /// Where is a simplification really
        protected override void ReadChildElement(IXsContext context, XmlReader reader, System.Reflection.PropertyInfo setToProperty)
        {
            throw new XsException(reader,"where clause cannot have any child elements");
        }
        /// Returns true if the expression should run
        public new bool ShouldRun()
        {
            return base.ShouldRun();
        }

        /// Returns true if the expression should run
        public object BaseExecute()
        {
            return base.Execute();    
        }
    }
    
    /// A set of rows
    [XsType("rowset", ScriptActionBase.XSharperNamespace)]
    [Description("A set of rows")]
    public class RowSet : DynamicValueFromFileBase, IEnumerable<Vars>
    {
        /// Original rowset rows
        [XsElement("", SkipIfEmpty = true, CollectionItemElementName = "row", CollectionItemType = typeof(Row),Ordering = 0)]
        [Description("Original rowset rows")]
        public List<Row> Rows { get; set; }

        /// ID of the XML document used as source for this rowset
        [Description("ID of the XML document used as source for this rowset")]
        [XsAttribute("xmldocId")]
        public string XmlDocId { get; set; }

        /// If source of this rowset is an XML document, xpath to the data to be used as input
        [Description("If source of this rowset is an XML document, xpath to the data to be used as input")]
        [XsAttribute("xpath")]
        public string XPath { get; set; }

        /// ID of another rowset used as source for this rowset
        [Description("ID of another rowset used as source for this rowset")]
        [XsAttribute("rowsetId")]
        public string RowsetId { get; set; }

        /// if source of this rowset is another rowset, list of columns to be copied. Null = all columns
        [XsElement("", SkipIfEmpty = true, CollectionItemElementName = "col", CollectionItemType = typeof(Col))]
        [Description("If source of this rowset is another rowset, list of columns to be copied. Null = all columns")]
        public List<Col> Cols { get; set; }

        /// Sort by these columns
        [XsElement("", SkipIfEmpty = true, CollectionItemElementName = "sortcol", CollectionItemType = typeof(SortCol))]
        [Description("Sort by these columns")]
        public List<SortCol> SortCols { get; set; }

        /// Skip rows that don't match the condition
        [XsElement("where", SkipIfEmpty = true)]
        [Description("Skip rows that don't match the condition")]
        public Where Where { get; set; }

        /// The list of columns 
        [Description("The list of columns")]
        [XsAttribute("columns")]
        [XsAttribute("csvColumns",Deprecated = true)]
        public string Columns { get; set; }

        /// Sort columns expression
        [Description("Sort columns expression")]
        public string SortColumns { get; set; }

        /// Rowset options (mainly CSV related)
        [Description("Rowset options (mainly CSV related)")]
        [XsAttribute("options")]
        [XsAttribute("csvOptions", Deprecated = true)]
        public RowsetOptions Options { get; set; }

        /// CSV separator character (default: comma)
        [Description("CSV separator character (default: comma)")]
        [XsAttribute("separator")]
        [XsAttribute("csvSeparator", Deprecated = true)]
        public string Separator { get; set; }

        /// CSV quote character (default: double quote)
        [Description("CSV quote character (default: double quote)")]
        [XsAttribute("quote")]
        [XsAttribute("csvQuote", Deprecated = true)]
        public string Quote { get; set; }

        /// Constructor
        public RowSet()
        {
            Rows = new List<Row>();
            Options = RowsetOptions.Default;
            Quote = "\"";
            Separator = ",";
        }

        /// Select all rows from the datatable and add them to the rowset
        public RowSet(DataTable dt) : this()
        {
            AddRows(dt, null, null);
        }

        /// Select some rows from the datatable and add them to the rowset
        public RowSet(DataTable dt, string filter) : this()
        {
            AddRows(dt, filter, null);
        }
        /// Select some rows from the datatable and add them to the rowset
        public RowSet(DataTable dt, string filter,string sort):this()
        {
            AddRows(dt, filter, sort);
        }

        /// Add rows to the rowset
        public RowSet(IEnumerable<Row> rows):this()
        {
            AddRows(rows);
        }

        /// Add rows to the rowset
        public RowSet(IEnumerable<Vars> rows):this()
        {
            AddRows(rows);
        }


        
        /// Return only the matching rows using AND expression
        public IEnumerable<Vars> GetData(string columns)
        {
            return getTransformedRows(columns,null,null);
        }

        /// Return only the matching rows using AND expression, in the specified order
        public IEnumerable<Vars> GetData(string columns, string where)    {   return getTransformedRows(columns,where,null);}

        /// Return only the matching rows using AND expression, in the specified order
        public IEnumerable<Vars> GetData(string columns, string where, string sort) { return getTransformedRows(columns, sort, where);}

        /// Convert the rowset to individual rows, do the transformations, and return the produced rows enumerator
        public IEnumerable<Vars> GetData()  { return getTransformedRows(null, null, null); }

        private IEnumerable<Vars> getTransformedRows(string columnsOverride, string sortOverride, string whereExpression)
        {
            bool overrideSort=(sortOverride != null);
            if (!overrideSort)
                sortOverride = Context.TransformStr(SortColumns,Transform);

            //
            ColumnsInfo ci = new ColumnsInfo();

            // Load CSV, if present
            string text = GetTransformedValueStr();
            string quote = Context.TransformStr(Quote, Transform);
            string separator = Context.TransformStr(Separator, Transform);
            char? quoteChar = string.IsNullOrEmpty(quote) ? (char?)null : quote[0];
            bool trim = (Options & RowsetOptions.Trim) != 0;
            string colNames = Context.TransformStr(Columns, Transform); ;
            // Read additional column names from the first CSV row, if specified
            ParsingReader csvReader = null;
            if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(text.Trim()))
            {
                csvReader = new ParsingReader(new StringReader(text));
                if ((Options & RowsetOptions.Header) == RowsetOptions.Header)
                {
                    csvReader.SkipWhiteSpace();
                    colNames = Context.TransformStr(csvReader.ReadLine(), Verbatim ? TransformRules.None : Transform);
                }
            }

            // Add these columns
            if (!string.IsNullOrEmpty(colNames))
                ci.AddParsed(Context, colNames, quoteChar, separator[0], trim);

            // Add extra columns
            if (Cols != null)
            {
                foreach (var col in Cols)
                {
                    ColumnInfo cc = new ColumnInfo();
                    cc.Name = Context.TransformStr(col.Name, col.Transform);
                    cc.Value = Context.TransformStr(col.Value, col.Transform);
                    cc.IsDefault = (col.Default != null);
                    if (cc.IsDefault)
                        cc.Default = Context.Transform(col.Default, col.Transform);
                    object t = Context.Transform(col.Type, col.Transform);
                    if (t != null)
                    {
                        if (t is Type)
                            cc.Type = (Type)t;
                        else
                            cc.Type = Context.FindType(t.ToString());
                    }
                    ci.Add(cc);
                }
            }

            SortInfos si=new SortInfos(sortOverride,quoteChar,separator[0],trim);
            if (!overrideSort && SortCols != null)
            {
                foreach (var col in SortCols)
                {
                    var ss = new SortInfo
                        {
                            Name = Context.TransformStr(col.Name, col.Transform),
                            Sort = col.Sort
                        };

                    var cmp = Context.Transform(col.Comparer, Transform);
                    if (cmp!=null)
                    {
                        object c1 = null;
                        if (cmp is Type)
                            ss.Comparer = (IComparer)Utils.CreateInstance((Type)cmp);
                        else if (cmp is string && !string.IsNullOrEmpty((string)cmp))
                        {
                            string smp = (string)cmp;
                            if (smp.ToUpperInvariant() == "IGNORECASE" || smp.ToUpperInvariant() == "NOCASE" || smp.ToUpperInvariant() == "IC")
                                ss.Comparer = StringComparer.CurrentCultureIgnoreCase;
                            else if (Utils.TryGetProperty(null,typeof(StringComparer),smp,null,false, out c1))
                                ss.Comparer=(IComparer)c1;
                            else
                                ss.Comparer = (IComparer)Utils.CreateInstance(Context.FindType(smp));
                        }
                        else
                            ss.Comparer = (IComparer)cmp;
                    }
                    si.Add(ss);
                }
            }

            var ret=si.SortAndFilter(Context, columnsOverride,whereExpression, getTransformedRowsInternal(ci, csvReader));
            
            if (columnsOverride==null)
                return ret;
                
            
            ci=new ColumnsInfo();
            ci.AddParsed(Context, columnsOverride, quoteChar, separator[0], trim);
            return ci.Process(ret);
        }

        private IEnumerable<Vars> getTransformedRowsInternal(ColumnsInfo ci, ParsingReader csvReader)
        {
            int rowNo = 0;

            string quote = Context.TransformStr(Quote, Transform);
            string separator = Context.TransformStr(Separator, Transform);
            char? quoteChar = string.IsNullOrEmpty(quote) ? (char?)null : quote[0];
            bool trim = (Options & RowsetOptions.Trim) != 0;

            // Read the rest of CSV, if available
            if (csvReader!=null)
            {
                // If there are no columns, treat data as the list of strings
                if (ci.Count == 0)
                {
                    string s;
                    while ((s=csvReader.ReadLine())!=null)
                    {

                        if ((Options & RowsetOptions.Trim)==RowsetOptions.Trim)
                            s=s.Trim();
                        if (string.IsNullOrEmpty(s) && (Options & RowsetOptions.IgnoreEmpty)==RowsetOptions.IgnoreEmpty)
                            continue;

                        Vars r = new Vars();
                        r[string.Empty] = s;

                        Context.CheckAbort();

                        yield return r;
                    }
                }
                else
                {
                    // We have columns, and therefore a complete CSV
                    string[] data;
                    while ((data = Utils.ReadCsvRow(csvReader, quoteChar, separator[0], trim)) != null)
                    {
                        if ((Options & RowsetOptions.IgnoreEmpty) != 0 && (data.Length == 0 || (data.Length == 1 && data[0].Length == 0)))
                            continue;
                        rowNo++;

                        if (data.Length > ci.Count)
                            throw new ScriptRuntimeException(string.Format("{0} columns expected in row #{1}, but {2} found.",
                                                                           ci.Count, rowNo, data.Length));
                        Vars r = new Vars();
                        for (int i = 0; i < Math.Min(ci.Count, data.Length); ++i)
                        {
                            object o = Context.Transform(data[i], Verbatim ? TransformRules.None : Transform);
                            if (i < ci.Count)
                                r[ci[i].Name] = ci[i].AdjustType(o);
                            else
                                r[ci[i].Name] = o;
                        }

                        ci.ApplyDefaults(r);
                        Context.CheckAbort();

                        var c = checkWhere(r);
                        if (c == null)
                            yield break;
                        if (c.Value)
                            yield return r;
                    }
                }
            }

            // Rowset ID
            string id = Context.TransformStr(RowsetId, Transform);
            if (!string.IsNullOrEmpty(id))
            {
                var rs = Context.Find<RowSet>(id, true);
                foreach (var v in rs.GetData())
                {
                    rowNo++;
                    Vars sv1 = null;
                    if (ci.Count==0)
                        sv1 = v;
                    else
                    {
                        sv1 = new Vars();
                        Context.ExecuteWithVars(() =>
                        {
                            foreach (var col in ci)
                                sv1.Set(col.Name, col.AdjustType(v.GetOrDefault(col.Name,null)));
                            return null;
                        }, v, null);
                    }
                    ci.ApplyDefaults(sv1);
                    Context.CheckAbort();

                    var c = checkWhere(sv1);
                    if (c == null)
                        yield break;
                    if (c.Value)
                        yield return sv1;
                }
            }

            // Try XmlDoc
            id = Context.TransformStr(XmlDocId, Transform);
            if (!string.IsNullOrEmpty(id))
            {
                XmlDoc doc = Context.Find<XmlDoc>(id, true);
                foreach (XmlNode n in doc.Nodes(Context.TransformStr(XPath, Transform)))
                {
                    Vars sv = new Vars();
                    rowNo++;
                    if (ci.Count>0)
                    {
                        foreach (var col in ci)
                        {
                            var node = n.SelectSingleNode(col.Value);
                            if (node!=null)
                                sv[col.Name] = node.Value;
                        }
                        
                    }
                    else
                    {
                        foreach (XmlAttribute attr in n.Attributes)
                            sv[attr.LocalName] = attr.Value;
                        sv[string.Empty] = n.InnerText;

                    }
                    ci.ApplyDefaults(sv);
                    Context.CheckAbort();

                    var c = checkWhere(sv);
                    if (c == null)
                        yield break;
                    if (c.Value)
                        yield return sv;
                }
            }
            
            foreach (var row in Rows)
            {
                rowNo++;
                Vars sv = new Vars();
                foreach (Var v in row)
                {
                    var val = (Verbatim) ? v.Value : Context.Transform(v.Value, Transform);
                    sv.Set(v.Name,ci.AdjustType(v.Name,val));
                }
                ci.ApplyDefaults(sv);
                Context.CheckAbort();

                var c = checkWhere(sv);
                if (c==null)
                    yield break;
                if (c.Value)
                    yield return sv;
            }

            
        }

        private bool? checkWhere(Vars sv)
        {
            if (Where==null)
                return true;
            bool? ret=(bool?)Context.ExecuteWithVars(() =>
                {
                    return Where.ShouldRun();
                }, sv, null);
            return ret;
        }

        /// <summary>
        /// Initialize action
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            Context.Initialize(Where);
        }


        /// <summary>
        /// Execute a delegate for all child nodes of the current node
        /// </summary>
        public override bool ForAllChildren(Predicate<IScriptAction> func,bool isFind)
        {
            return base.ForAllChildren(func, isFind) || func(Where);
        }
        /// Select all rows from the datatable and add them to the rowset
        public int AddRows(DataTable dt)
        {
            return AddRows(dt, null, null);
        }

        /// Select some rows from the datatable and add them to the rowset
        public int AddRows(DataTable dt, string filter)
        {   
            return AddRows(dt, filter, null);
        }

        /// Add rows to the rowset
        public int AddRows(IEnumerable<Row> rows)
        {
            int c = 0;
            if (rows!=null)
                foreach (var row in rows)
                {
                    Rows.Add(row);
                    c++;
                }
            return c;
        }

        /// Add rows to the rowset
        public int AddRows(IEnumerable<Vars> rows)
        {
            int c = 0;
            if (rows != null)
                foreach (var row in rows)
                {
                    Rows.Add(new Row(row));
                    c++;
                }
            return c;
        }

        /// Add row to the rowset
        public void AddRow(IEnumerable obj)
        {
            AddRow(Utils.ToVars(obj));
        }

        /// Add row to the rowset
        public void AddRow(Vars r)
        {
             AddRow(new Row(r));
        }

        /// Add row to the rowset
        public void AddRow(Row r)
        {
            Rows.Add(r);
        }
        
        /// Select rows from the data table and add them to the rowset
        public int AddRows(DataTable dt, string filter, string sort)
        {
            Transform = TransformRules.None;

            string[] col=new string[dt.Columns.Count];
            for (int i = 0; i < col.Length; ++i)
                col[i] = dt.Columns[i].ColumnName;
            int row = 0;
            foreach (var r in dt.Select(filter,sort))
            {
                Row sv = new Row();
                for (int i = 0; i < col.Length; ++i)
                    sv[col[i]] = r[i];
                Rows.Add(sv);
                row++;
            }
            return row;
        }

        /// Convert rowset to datatable
        public DataTable ToDataTable()
        {
            return ToDataTable(null,null,null,null);
        }

        /// Convert rowset to datatable
        public DataTable ToDataTable(string columns)
        {
            return ToDataTable(columns, null, null, null);
        }
        /// Convert rowset to datatable
        public DataTable ToDataTable(string columns,string where)
        {
            return ToDataTable(columns, where, null, null);
        }
        /// Convert rowset to datatable
        public DataTable ToDataTable(string columns, string where,string sort)
        {
            return ToDataTable(columns, where, sort, null);
        }
        /// Convert rowset to datatable with a given name
        public DataTable ToDataTable(string columns,string where, string sort, string tableName)
        {
            return Utils.ToDataTable(GetData(columns, where, sort), tableName);
        }

        /// Convert rowset to a text table with default formatting options
        public string ToTextTable()
        {
            return ToTextTable(TableFormatOptions.Default);
        }

        /// Convert rowset to a text table with the specified formating options
        public string ToTextTable(TableFormatOptions options)
        {
            using (var dt = ToDataTable())
                return Utils.ToTextTable(dt, options);
        }

        /// Convert rowset to a text table with the specified formating options
        public string ToTextTable(TableFormatOptions options,string columns)
        {
            using (var dt = ToDataTable(columns))
                return Utils.ToTextTable(dt, options);
        }
        
        /// Convert rowset to a text table with the specified formating options
        public string ToTextTable(TableFormatOptions options, string columns, string where)
        {
            using (var dt = ToDataTable(columns, where))
                return Utils.ToTextTable(dt, options);
        }
        
        /// Convert rowset to a text table with the specified formating options
        public string ToTextTable(TableFormatOptions options, string columns, string where, string sort)
        {
            using (var dt = ToDataTable(columns, where, sort))
                return Utils.ToTextTable(dt, options);
        }
        
        /// Convert rowset to a CSV with header
        public string ToCSV()
        {
            return ToCSV(true);
        }

        /// Convert rowset to a CSV with or without header
        public string ToCSV(bool withHeader)
        {
            string quote = Context.TransformStr(Quote, Transform);
            string separator = Context.TransformStr(Separator, Transform);

            using (var dt = ToDataTable("data"))
                return Utils.ToCsv(dt, withHeader, string.IsNullOrEmpty(quote) ? '"' : quote[0], separator[0]);
        }

        /// Convert rowset to a CSV with or without header, and using specified separators and quote characters
        public string ToCSV(bool withHeader, char quote, char separator)
        {
            using (var dt = ToDataTable("data"))
                return Utils.ToCsv(dt, withHeader, quote, separator);
        }

        /// Returns an enumerator that iterates through the collection.
        public IEnumerator<Vars> GetEnumerator()
        {
            foreach (var v in GetData())
                yield return v;
        }

        /// Returns an enumerator that iterates through the collection.
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #region -- private classes --
        
        class SortInfo
        {
            public string Name;
            public IComparer Comparer;
            public ColumnSortDirection Sort;
        }
        class SortInfos : List<SortInfo>
        {
            public SortInfos(string sort, char? quoteChar, char separator, bool trim)
            {
                if (sort==null)
                    return;
                foreach (var str in Utils.ReadCsvRow(new StringReader(sort), quoteChar, separator, trim) ?? new string[1])
                {
                    SortInfo cc = new SortInfo();
                    int n = str.LastIndexOf(':');
                    cc.Sort = ColumnSortDirection.Ascending;
                    if (n == -1)
                        cc.Name = str;
                    else
                    {
                        cc.Name = str.Substring(0, n);
                        switch (str.Substring(n + 1).ToUpper(CultureInfo.InvariantCulture))
                        {
                            case "ASC":
                                cc.Sort = ColumnSortDirection.Ascending;
                                break;
                            case "DESC":
                                cc.Sort = ColumnSortDirection.Descending;
                                break;
                        }
                    }
                    Add(cc);
                }
            }
            public int Compare(Vars x, Vars y)
            {
                foreach (var info in this)
                {
                    if (info.Sort == ColumnSortDirection.None)
                        continue;
                    bool o1 = x.IsSet(info.Name);
                    bool o2 = y.IsSet(info.Name);
                    
                    int r;
                    if (o1 == false && o2 == false) r = 0;
                    else if (o1 == true && o2 == false) r = 1;
                    else if (o1 == false && o2 == true) r = -1;
                    else
                    {

                        object ob1 = x.Get(info.Name);
                        object ob2 = y.Get(info.Name);

                        if (info.Comparer != null)
                            r = info.Comparer.Compare(ob1, ob2);
                        else
                            r = Comparer.Default.Compare(ob1, ob2);
                    }
                    if (info.Sort == ColumnSortDirection.Descending)
                        r = -r;
                    if (r != 0)
                        return r;
                }
                return 0;
            }

            private bool IsSortRequired
            {
                get
                {
                    foreach (var info in this)
                        if (info.Sort != ColumnSortDirection.None) return true;
                    return false;
                }
            }

            private static IEnumerable<Vars>  filter(ScriptContext context,string where, IEnumerable<Vars> input)
            {
                foreach (var enumerable in input)
                {
                    if ((bool)context.ExecuteWithVars(() => Utils.To<bool>(context.Eval(where)), enumerable, null))
                        yield return enumerable;
                }
            }
            public IEnumerable<Vars> SortAndFilter(ScriptContext context, string columnsOverride,string whereExpr, IEnumerable<Vars> enumerable)
            {
                if (string.IsNullOrEmpty(whereExpr) && !IsSortRequired)
                    return enumerable;
                if (!IsSortRequired)
                    return filter(context, whereExpr, enumerable);

                var r = new List<Vars>(string.IsNullOrEmpty(whereExpr) ? enumerable : filter(context, whereExpr, enumerable));
                r.Sort(Compare);
                return r;
            }
        }


        class ColumnInfo
        {
            public string Name;
            public Type Type;
            public string Value;
            public bool IsDefault;
            public object Default;

            public object AdjustType(object o)
            {
                if (Type!=null)
                {
                    if (o == null && Type.IsValueType)
                        return Utils.CreateInstance(Type);
                    return Utils.To(Type, o);

                }
                return o;
            }
        }
        class ColumnsInfo : System.Collections.ObjectModel.KeyedCollection<string,ColumnInfo>
        {
            public ColumnsInfo() : base(StringComparer.OrdinalIgnoreCase)
            {
                
            }
            protected override string GetKeyForItem(ColumnInfo item)
            {
                return item.Name;
            }

            public object AdjustType(string name, object val)
            {
                if (Contains(name))
                    return this[name].AdjustType(val);
                return val;
            }

            public void AddParsed(ScriptContext context,string colnames, char? quoteChar, char separator, bool trim)
            {
                foreach (var str in Utils.ReadCsvRow(new StringReader(colnames), quoteChar, separator, trim) ?? new string[1])
                {
                    ColumnInfo cc = new ColumnInfo();
                    int n = str.LastIndexOf(':');
                    if (n == -1)
                        cc.Name = str;
                    else
                    {
                        cc.Name = str.Substring(0, n);
                        cc.Type = context.FindType(str.Substring(n + 1));
                    }
                    Add(cc);
                }
            }

            public void ApplyDefaults(Vars variables)
            {
                foreach (var col in this)
                    if (col.IsDefault && !variables.IsSet(col.Name))
                        variables[col.Name] = col.AdjustType(col.Default);
            }


            public IEnumerable<Vars> Process(IEnumerable<Vars> enumerable)
            {
                foreach (var src in enumerable)
                {
                    Vars v=new Vars();
                    foreach (var info in this)
                    {
                        object o;
                        if (src.TryGetValue(info.Name,out o))
                            v[info.Name] = o;
                        else if (info.IsDefault)
                            v[info.Name] = info.AdjustType(info.Default);
                    }
                    yield return v;
                }
            }
        }
#endregion
    }
}