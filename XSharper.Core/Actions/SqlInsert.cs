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

namespace XSharper.Core
{
    /// <summary>
    /// In which format to generate SQL INSERT commands
    /// </summary>
    public enum SqlInsertSyntax
    {
        /// Generate INSERT FROM ... (SELECT 'X' UNION ALL SELECT 'Y')
        [Description("Generate INSERT FROM ... (SELECT 'X' UNION ALL SELECT 'Y')")]
        Select,

        /// Generate INSERT VALUES (...)
        [Description("Generate INSERT VALUES (...)")]
        Values
    }


    /// <summary>
    /// How to check for existing records
    /// </summary>
    public enum SqlUpdateMode
    {
        /// No update
        [Description("No update")]
        None,

        /// Determine the best way automatically
        [Description("Determine the mode automatically")]
        Auto,

        /// Simple update mode, dangerous but works everywhere
        [Description("Simple update mode, dangerous but works everywhere")]
        Simple,

        /// MS SQL update mode, requires @@rowcount
        [Description("MS SQL update mode, requires @@rowcount")]
        MsSql,

        /// Using MERGE, requires SQL2008
        [Description("Using MERGE, requires SQL2008")]
        Merge,
    }



    /// <summary>
    /// Insert rows into a SQL database
    /// </summary>
    [XsType("sqlInsert", ScriptActionBase.XSharperNamespace)]
    [Description("Insert rows into a SQL database")]
    public class SqlInsert : RowSet
    {
        /// True if identity insert must be switched on before running the statement. Default false
        [Description("True if identity insert must be switched on before running the statement. Default false")]
        public bool IdentityInsert { get; set; }

        /// True if data errors are to be treated as warnings, not as errors
        [Description("True if data errors are to be treated as warnings, not as errors")]
        public bool IgnoreDataErrors { get; set; }

        /// Table name, where to insert. Required
        [Description("Table name, where to insert. Required")]
        [XsAttribute("table")]
        [XsAttribute("tableName",Deprecated = true)]
        [XsRequired]
        public string Table { get; set; }

        /// True, if before first insert TRUNCATE command must be run
        [Description("True, if before first insert TRUNCATE command must be run")]
        public bool Truncate { get; set; }

        /// Maximal number of SQL statements per batch. Default 256
        [Description("Maximal number of SQL statements per batch. ")]
        public int MaxBatch { get; set; }

        /// Maximal length of the SQL command that is run at one time. Default 4000 characters
        [Description("Maximal length of the SQL command that is run at one time. Default 4000 characters")]
        public int MaxLength { get; set; }

        /// How SQL INSERT statements are generated
        [Description("How SQL INSERT statements are generated")]
        public SqlInsertSyntax InsertSyntax { get; set; }

        /// Whether existing records should be updated instead of inserting
        [Description("Whether existing records should be updated instead of inserting")]
        public SqlUpdateMode UpdateMode { get; set; }

        /// Comma or semicolon separated list of key fields (used for updates)
        [Description("Comma or semicolon separated list of key fields (used for updates)")]
        public string Keys { get; set; }

        /// Values that start from a given prefix are not SQL-quoted. Default - empty string.
        /// This may be useful to, for example, insert NULL values. Define prefix as #, and then '#NULL' will be inserted into SQL statement as NULL
        [Description("Values that start from a given prefix are not SQL-quoted. This may be useful to, for example, insert NULL values. Define prefix as #, and then '#NULL' will be inserted into SQL statement as NULL")]
        public string NoQuotePrefix { get; set; }

        /// A set comma-separated name/value pairs, which must be translated from name to value
        [Description("A set comma-separated name/value pairs, which must be translated from name to value")]
        public string Translate { get; set; }

        /// Timeout for SQL command execution. Default - not set.
        [Description("Timeout for SQL command execution. Default - not set.")]
        public string Timeout { get; set; }

        /// Where to output SQL statements. Default is SQL server, but can output to console instead
        [Description("/// Where to output SQL statements. Default is SQL server, but can output to console instead")]
        public string SqlTo { get; set; }

        /// Where to output SQL messages (like PRINT). Default: ^info
        [Description("Where to output SQL messages (like PRINT). ")]
        public string MessagesTo { get; set; }

        /// Where to output SQL errors. Default: ^error
        [Description("Where to output SQL errors.")]
        public string ErrorsTo { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public SqlInsert()
        {
            Rows= new List<Row>();
            MaxBatch = 256;
            MaxLength = 65536;
            InsertSyntax = SqlInsertSyntax.Select;
            MessagesTo="^info";
            ErrorsTo = "^error";
            UpdateMode = SqlUpdateMode.None;
        }

        /// Execute action
        public override object Execute()
        {
            object o=base.Execute();
            if (o != null)
                return 0;

            if (Database.Current==null)
                throw new ScriptRuntimeException("Database is not specified");
            var dbType = Database.Current.SpecificDatabaseType;
            string tableName = SqlUtil.SqlName(dbType, Context.TransformStr(Table, Transform));
            string firstHeader = string.Empty;
            if (Truncate)
                firstHeader="TRUNCATE TABLE " + tableName + Environment.NewLine;

            SqlUpdateMode upMode = UpdateMode;
            if (upMode==SqlUpdateMode.Auto)
            {
                if (dbType == DatabaseType.MsSql2008)
                    upMode = SqlUpdateMode.Merge;
                else if ((dbType & DatabaseType.MsSql)==DatabaseType.MsSql)
                    upMode = SqlUpdateMode.MsSql;
                else
                    upMode = SqlUpdateMode.Simple;
            }
            
            StringBuilder sbFooter = new StringBuilder();
            
            StringBuilder batch = new StringBuilder();
            List<string> keys = new List<string>();

            int rowsInBatch = 0;
            int prevColsCount = -1;
            int maxBatch = MaxBatch;
            if (maxBatch < 1)
                maxBatch = 1;
            if ((dbType & DatabaseType.MsSql) != DatabaseType.MsSql && InsertSyntax==SqlInsertSyntax.Values)
                maxBatch = 1;
            int rowNo = 0;
            StringBuilder sbO=new StringBuilder();
            foreach (Vars sv in GetData())
            {
                Context.CheckAbort();
            flush:
                if (sv.Count!=prevColsCount && batch.Length!=0)
                {
                    if (string.IsNullOrEmpty(SqlTo))
                        runCommand(firstHeader + batch + sbFooter);
                    else
                        sbO.AppendLine(firstHeader + batch + sbFooter);

                    batch.Length = 0;
                    firstHeader = string.Empty;
                    rowsInBatch = 0;
                }

                if (rowsInBatch == 0)
                {
                    keys.Clear();
                    foreach (Var c in sv)
                        keys.Add(c.Name);
                    prevColsCount = sv.Count;
                    sbFooter.Length = 0;
                    prepareHeaderAndFooter(batch, sbFooter, tableName, keys, upMode, dbType);
                }

                string cr = createRowStatement(rowNo, keys, sv, tableName, rowsInBatch == 0, upMode, dbType);
                if (rowsInBatch >= 1 && (cr.Length + firstHeader.Length + batch.Length + sbFooter.Length > MaxLength || rowsInBatch >= maxBatch))
                {
                    // If we append this command to the batch, it will be too long. 
                    prevColsCount = -1;
                    goto flush;
                }
                batch.Append(cr);
                rowsInBatch++;   
            }
            if (batch.Length != 0)
            {
                if (string.IsNullOrEmpty(SqlTo))
                    runCommand(firstHeader + batch + sbFooter);
                else
                    sbO.AppendLine(firstHeader + batch + sbFooter);
            }
            if (!string.IsNullOrEmpty(SqlTo))
            {
                Context.OutTo(Context.TransformStr(SqlTo,Transform),sbO.ToString());
            }

            return null;
        }

        private void prepareHeaderAndFooter(StringBuilder batch, StringBuilder footer, string tableName, List<string> keys, SqlUpdateMode updateMode, DatabaseType dbType)
        {
            if (IdentityInsert)
            {
                batch.Append("SET IDENTITY_INSERT " + tableName + " ON\n");
                footer.AppendLine("SET IDENTITY_INSERT " + tableName + " OFF\n");
            } 
            if (updateMode==SqlUpdateMode.Merge)
            {
                string[] sqlKeys = new string[keys.Count];
                for (int i = 0; i < keys.Count; ++i)
                    sqlKeys[i] = SqlUtil.SqlName(dbType, keys[i]);

                batch.AppendLine("MERGE " + tableName + " AS DST");
                batch.AppendLine("USING (");
                footer.AppendLine(") AS SRC("+string.Join(", ",sqlKeys)+")");
                footer.Append("    ON ");

                var updateKeys = (Context.TransformStr(Keys, Transform) ?? string.Empty);
                if (string.IsNullOrEmpty(updateKeys))
                    throw new ParsingException("Key columns must be specified in order to enable updates");
                string[] kfield = updateKeys.Split(';', ',');
                for (int i = 0; i < kfield.Length; ++i)
                    kfield[i] = SqlUtil.SqlName(dbType, kfield[i].Trim());

                Dictionary<string,bool> kk=new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase);
                for (int i = 0; i < kfield.Length; ++i)
                {
                    if (i != 0)
                        footer.Append(" AND ");
                    footer.AppendFormat("DST.{0}=SRC.{0}", kfield[i]);
                    kk[kfield[i]] = true;
                }
                footer.AppendLine();
                footer.AppendLine("WHEN MATCHED THEN");
                footer.Append("    UPDATE SET ");
                bool ff = true;
                
                for (int i = 0; i < sqlKeys.Length; ++i)
                {
                    if (kk.ContainsKey(sqlKeys[i]))
                        continue;
                    if (!ff)
                        footer.Append(",");
                    ff = false;
                    footer.AppendFormat("{0} = SRC.{0}",sqlKeys[i]);
                }
                footer.AppendLine();
                footer.AppendLine("WHEN NOT MATCHED THEN");
                footer.Append("    INSERT (");
                for (int i = 0; i < sqlKeys.Length; ++i)
                {
                    if (i != 0)
                        footer.Append(",");
                    footer.Append(sqlKeys[i]);
                }
                footer.AppendLine(" )");
                footer.Append("    VALUES (");
                for (int i = 0; i < sqlKeys.Length; ++i)
                {
                    if (i != 0)
                        footer.Append(",");
                    footer.Append("SRC."+sqlKeys[i]);
                }
                footer.AppendLine(");");
            }

        }

        private void runCommand(string s)
        {
            SqlUtil.RunCommand(Context, s, null, Utils.ToTimeSpan(Timeout),
                               cmd => cmd.ExecuteNonQuery(),
                               Context.TransformStr(MessagesTo, Transform),
                               Context.TransformStr(ErrorsTo, Transform),
                               IgnoreDataErrors);

        }

        string createRowStatement(int rowNo,List<string> keys, Vars sv, string tableName, bool firstInBatch, SqlUpdateMode updateMode, DatabaseType dbType)
        {
            StringBuilder sbInsert = new StringBuilder();
            if (updateMode == SqlUpdateMode.Merge)
            {
                appendSelect(sbInsert, rowNo, keys, sv, firstInBatch, dbType);
                return sbInsert.ToString();
            }
            if (InsertSyntax == SqlInsertSyntax.Select)
            {
                if (firstInBatch || UpdateMode != SqlUpdateMode.None)
                {
                    sbInsert.Append("INSERT INTO ");
                    sbInsert.Append(tableName);
                    sbInsert.Append(" ( ");
                    appendColumns(sbInsert, keys, dbType);
                    sbInsert.Append(" )");
                    sbInsert.AppendLine();
                }
                appendSelect(sbInsert, rowNo, keys, sv, firstInBatch || UpdateMode != SqlUpdateMode.None, dbType);
            }
            else
            {
                sbInsert.Append("INSERT INTO ");
                sbInsert.Append(tableName);
                sbInsert.Append(" ( ");
                appendColumns(sbInsert, sv.Keys, dbType);
                sbInsert.AppendLine(" )");
                sbInsert.Append("VALUES (");
                bool ff = true;
                foreach (var v in sv.Values)
                {
                    if (!ff)
                        sbInsert.Append(",");
                    ff = false;
                    sbInsert.Append(SqlUtil.GetQuotedValue(dbType, v, NoQuotePrefix, Translate));
                }
                sbInsert.AppendLine(")");
            }

            if (updateMode == SqlUpdateMode.None)
                return sbInsert.ToString();                    
            
            var updateKeys = (Context.TransformStr(Keys, Transform) ?? string.Empty);
            if (string.IsNullOrEmpty(updateKeys))
                throw new ParsingException("Key columns must be specified in order to enable updates");
            string[] kfield = updateKeys.Split(';', ',');
                
            StringBuilder sList = new StringBuilder();
            StringBuilder sCond = new StringBuilder();
            StringBuilder sUpd = new StringBuilder();
            Dictionary<string,bool> k=new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase);
            for (int i=0;i<kfield.Length;++i)
            {
                string kf = kfield[i].Trim();
                kfield[i] = kf;
                if (sList.Length!=0)
                    sList.Append(", ");
                sList.Append(SqlUtil.SqlName(dbType, kf));
                if (sCond.Length!=0)
                    sCond.Append(" AND ");
                sCond.Append("(");
                sCond.Append(SqlUtil.SqlName(dbType, kf));
                sCond.Append(" = ");
                sCond.Append(SqlUtil.GetQuotedValue(dbType, sv[kf], NoQuotePrefix, Translate));
                sCond.Append(")");
                k[kf] = true;
            }
            // Prepare update statement
            sUpd.AppendFormat("UPDATE " + tableName);
            sUpd.AppendLine();
            sUpd.Append("SET ");
            bool first = true;
            foreach (var v in sv)
                if (!k.ContainsKey(v.Name))
                {
                    if (!first)
                        sUpd.Append(", ");
                    first = false;
                    sUpd.Append(SqlUtil.SqlName(dbType, v.Name));
                    sUpd.Append("=");
                    sUpd.Append(SqlUtil.GetQuotedValue(dbType, v.Value, NoQuotePrefix, Translate));
                }
            sUpd.AppendLine();
            var kfieldCond = sCond.ToString();
            sUpd.AppendLine("WHERE "+kfieldCond);
            
            StringBuilder sb=new StringBuilder();
            if (updateMode == SqlUpdateMode.MsSql)
            {
                sb.Append(sUpd.ToString());
                sb.AppendLine("IF @@ROWCOUNT=0");
            }
            else
            {
                sb.AppendFormat("IF EXISTS(SELECT {0} FROM {1} WHERE {2})", sList.ToString(), tableName, kfieldCond);
                sb.AppendLine();
                sb.Append(sUpd.ToString());
                sb.AppendLine("ELSE");
            }
            sb.AppendLine(sbInsert.ToString());
            sb.AppendLine();
            return sb.ToString();
        }

        private static void appendColumns(StringBuilder sbInsert, IEnumerable<string> keys, DatabaseType dbType)
        {
            bool firstCol = true;
            foreach (var key in keys)
            {
                if (!firstCol)
                    sbInsert.Append(", ");
                sbInsert.Append(SqlUtil.SqlName(dbType, key));
                firstCol = false;
            }
        }

        private void appendSelect(StringBuilder sbInsert, int rowNo, List<string> keys, Vars sv, bool firstInBatch, DatabaseType dbType)
        {
            if (!firstInBatch)
                sbInsert.AppendLine(" UNION ALL ");

            if (sv.Count != keys.Count)
                throw new ParsingException(string.Format("Error in row #{0}. All inserted rows must contain the following columns: {{ {1} }} ", rowNo, string.Join(",", keys.ToArray())));

            List<string> values = new List<string>();
            foreach (string key in keys)
            {
                string value = SqlUtil.GetQuotedValue(dbType, sv[key], NoQuotePrefix, Translate);
                values.Add(value);
            }
            sbInsert.Append("SELECT " + string.Join(",", values.ToArray()));
            
            if ((dbType & DatabaseType.Oracle)==DatabaseType.Oracle)
                sbInsert.Append(" FROM dual ");
        }
    }
}