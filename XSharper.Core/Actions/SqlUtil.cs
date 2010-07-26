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
using System.Reflection;

namespace XSharper.Core
{
    /// <summary>
    /// SQL parameter
    /// </summary>
    [XsType(null, ScriptActionBase.XSharperNamespace, AnyAttribute = true)]
    [Description("SQL parameter")]
    public class SqlParam : XsTransformableElement
    {
        /// Parameter name
        public string Name { get; set; }

        /// SQL Type. Default is "varchar(4000)" or an appropriate primitive type
        public string SqlType { get; set; }

        /// Value
        [XsAttribute("")]
        [XsAttribute("value")]
        public object Value { get; set; }

        /// Send empty string is the value is Null
        public bool EmptyAsNull { get; set; }

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
                if (previouslyProcessed!=null && !string.IsNullOrEmpty(attribute))
                    previouslyProcessed.Add(attribute, true);
            }
            return true;
        }
    }

    /// <summary>
    /// SQL Utilities
    /// </summary>
    public static class SqlUtil 
    {
        /// <summary>
        /// Callback to execute
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public delegate int ExecCommand(IDbCommand cmd);


        private static readonly string[] s_sqlDbTypeNames = Enum.GetNames(typeof (SqlDbType));

        /// <summary>
        /// Run SQL command
        /// </summary>
        /// <param name="context">script context</param>
        /// <param name="cmdText">SQL statement text</param>
        /// <param name="parameters">list of parameters</param>
        /// <param name="timeout">Timeout, may be null</param>
        /// <param name="execCommand">callback</param>
        /// <param name="messagesTo">Where to output messages (for example, by PRINT SQL statement)</param>
        /// <param name="errorsTo">Where to output errors</param>
        /// <param name="ignoreDataErrors">If true, SQL errors below severity 17 do not throw exceptions and instead just write messages to the output</param>
        public static void RunCommand(      ScriptContext context, 
                                            string cmdText, 
                                            List<SqlParam> parameters, 
                                            TimeSpan? timeout, 
                                            ExecCommand execCommand, 
                                            string messagesTo, 
                                            string errorsTo, 
                                            bool ignoreDataErrors)
        {
            
            var connection = Database.CurrentConnection;
            if (connection == null)
                throw new ScriptRuntimeException("There is no active database connection");

            {
                IDbCommand cmd = connection.CreateCommand();
                if (timeout.HasValue)
                    cmd.CommandTimeout = (int) (timeout.Value.TotalSeconds + 0.999);
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = cmdText;

                context.WriteVerbose(Utils.PrefixEachLine("SQL> ", cmdText));
                if (parameters != null && parameters.Count > 0)
                {
                    context.WriteVerbose(String.Format("SQL> -- with {0} params: ", parameters.Count));
                    foreach (var parameter in parameters)
                    {
                        object val = context.Transform(parameter.Value, parameter.Transform);
                        string type = context.TransformStr(parameter.SqlType, parameter.Transform);

                        var param = cmd.CreateParameter();
                        if (String.IsNullOrEmpty(type))
                        {
                            if (param is SqlParameter)
                                type = "nvarchar(4000)";
                            else
                                type = "string(4000)";
                            if (val != null)
                            {
                                if (val is DateTime || val is DateTime?)
                                    type = "datetime";
                                else if (val is DateTimeOffset || val is DateTimeOffset?)
                                    type = "datetimeoffset";
                                if (param is SqlParameter)
                                {
                                    if (val is int || val is int?)
                                        type = SqlDbType.Int.ToString();
                                    else if (val is short || val is short? || val is byte || val is byte?)
                                        type = SqlDbType.SmallInt.ToString();
                                    else if (val is sbyte || val is sbyte?)
                                        type = SqlDbType.TinyInt.ToString();
                                    else if (val is decimal || val is decimal?)
                                        type = SqlDbType.Money.ToString();
                                    else if (val is byte[])
                                        type = SqlDbType.Binary.ToString();
                                    else if (val is double || val is double? || val is float || val is float?)
                                        type = SqlDbType.Real.ToString();
                                    else if (val is uint || val is Nullable<uint> ||
                                            val is ushort || val is Nullable<ushort>)
                                        type = SqlDbType.Int.ToString();
                                    else if (val is long || val is Nullable<long> ||
                                             val is Nullable<ulong> || val is ulong)
                                        type = SqlDbType.BigInt.ToString();
                                }
                                else
                                {
                                    if (val is int || val is int?)
                                        type = DbType.Int32.ToString();
                                    else if (val is short || val is short? || val is byte || val is byte?)
                                        type = DbType.Int16.ToString();
                                    else if (val is sbyte || val is sbyte?)
                                        type = DbType.SByte.ToString();
                                    else if (val is decimal || val is decimal?)
                                        type = DbType.Decimal.ToString();
                                    else if (val is byte[])
                                        type = DbType.Binary.ToString();
                                    else if (val is double || val is double? || val is float || val is float?)
                                        type = DbType.Double.ToString();
                                    else if (val is uint || val is Nullable<uint> ||
                                            val is ushort || val is Nullable<ushort>)
                                        type = DbType.Int64.ToString();
                                    else if (val is long || val is Nullable<long> ||
                                             val is Nullable<ulong> || val is ulong)
                                        type = DbType.Int64.ToString();
                                }
                            }
                        }

                        int? size = null;
                        int n = type.IndexOf('(');
                        if (n != -1)
                        {
                            var sz = type.Substring(n + 1).TrimEnd().TrimEnd(new char[] { ')' }).Trim();
                            size = (string.Compare(sz,"max",StringComparison.OrdinalIgnoreCase)==0)?-1:Utils.To<int>(sz);
                            type = type.Substring(0, n).Trim();
                        }
                        if (param is SqlParameter && Array.Exists(s_sqlDbTypeNames, x => StringComparer.OrdinalIgnoreCase.Compare(type, x) == 0))
                            ((SqlParameter)param).SqlDbType = Utils.To<SqlDbType>(type);
                        else
                            param.DbType = Utils.To<DbType>(type);
                        

                        param.ParameterName = parameter.Name;
                        param.Direction = ParameterDirection.Input;
                            
                        
                        if (size.HasValue)
                            param.Size = size.Value;

                        if (val == null || ( (val is string && String.IsNullOrEmpty((string)val)) && parameter.EmptyAsNull))
                        {
                            param.Value = DBNull.Value;
                            val = null;
                        }
                        else
                        {
                            param.Value = val;
                        }
                        cmd.Parameters.Add(param);
   
                        context.WriteVerbose(String.Format("SQL> --     {1} {0}=> {2}", param.ParameterName, param.DbType, Dump.ToDump(val)));

                    }
                }



                SqlInfoMessageEventHandler handler = (sender, e) =>
                                                         {
                                                             if (e.Errors.Count > 0)
                                                             {
                                                                 foreach (SqlError s1 in e.Errors)
                                                                 {
                                                                     context.Debug.WriteLine(String.Format("SQL> (Error: {0}, Class:{1}, State:{2}, Proc: {3}, #: {4}): {5}", s1.Number, s1.Class, s1.State, s1.Procedure, s1.LineNumber, s1.Message));
                                                                     if (s1.Class >= 11)
                                                                     {
                                                                         context.OutTo(errorsTo, s1.Message + Environment.NewLine);
                                                                     }
                                                                     else
                                                                         context.OutTo(messagesTo, s1.Message + Environment.NewLine);
                                                                 }
                                                             }
                                                         };

                var sql = connection as SqlConnection;
                bool b = false;
                if (sql!=null)
                {
                    b = sql.FireInfoMessageEventOnUserErrors;
                    sql.FireInfoMessageEventOnUserErrors = ignoreDataErrors;
                    sql.InfoMessage += handler;
                }

                try
                {
                    int nRows = execCommand(cmd);
                    if (nRows != -1)
                        context.WriteVerbose(String.Format("SQL>-- {0} rows affected", nRows));
                }
                finally
                {
                    if (sql!=null)
                    {
                        sql.InfoMessage -= handler;
                        sql.FireInfoMessageEventOnUserErrors = b;
                    }
                }
            }
        }

        ///<summary>Get string quoted for SQL</summary>
        public static string GetQuotedValue(DatabaseType dbType, object val, string noQuotePrefix, string translate)
        {
            if (val==null)
                return "NULL";
            if (val.GetType()==typeof(Nullable<>))
                return GetQuotedValue(dbType, Utils.To(Nullable.GetUnderlyingType(val.GetType()), val), noQuotePrefix, translate);
            if (val is DateTime)
            {
                string s="'" + ((DateTime) val).ToString("yyyy-MM-dd hh:mm:ss.ff") + "'";
                if (dbType==DatabaseType.Oracle)
                    return "TIMESTAMP " + s;
                return s;
            }
            if (val is bool)
                return ((bool) val) ? "1" : "0";
            if (val is byte[])
                return "'"+Utils.ToHex((byte[])val)+"'";
            
            string value = Utils.To<string>(val);
            if (!String.IsNullOrEmpty(noQuotePrefix) && value.StartsWith(noQuotePrefix, StringComparison.Ordinal))
                value=(value.Substring(noQuotePrefix.Length));
            else
            {
                bool quote = true;
                if (!String.IsNullOrEmpty(translate))
                    foreach (string v in translate.Split(';'))
                    {
                        int n = v.IndexOf('=');
                        if (n != -1 && String.Compare(value, v.Substring(0, n), StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            value=(v.Substring(n + 1));
                            quote = false;
                            break;
                        }
                    }
                if (quote)
                    value=("'" + value.Replace("'", "''") + "'");
            }
            return value;
        }

        /// Wrap column/value name into [] or double quote if it's not already encloded into []
        public static string SqlName(DatabaseType dbType, string key)
        {
            key = key.Trim();
            if (key.Length>0 && "[\"`".IndexOf(key[0])!=-1)
                return key;
            
            if ((dbType & DatabaseType.MsSql) == DatabaseType.MsSql)
                key = "[" + key + "]";
            else if ((dbType & DatabaseType.Oracle) == DatabaseType.Oracle)
                key = "\"" + key.ToUpperInvariant() + "\"";
            else
                key = "\"" + key + "\"";
            return key;
        }

        
    }
}