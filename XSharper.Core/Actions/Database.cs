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
using System.ComponentModel;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;

namespace XSharper.Core
{
    /// Type of database
    [Flags]
    public enum DatabaseType
    {
        /// Automatic
        Auto=0,

        /// Other database, not MS SQL
        Other=1,

        /// Some MS SQL version, not 2005 or 2008
        MsSql=2,

        /// Some oracle version
        Oracle=4,

        /// MS SQL 2005 
        MsSql2005=MsSql|0x100,

        /// Ms Sql 2008
        MsSql2008=MsSql2005|0x200
    }

    /// Create a database connection
    [XsType("database", ScriptActionBase.XSharperNamespace)]
    [Description("Create a database connection")]
    public class Database : Block
    {
        /// Connection string
        [XsAttribute("connectionString"),XsAttribute("cs"),XsRequired("connectionString")]
        [Description("Connection string")]
        public string ConnectionString { get; set; }

        /// True if connection pool must be cleared before trying this connection
        [Description("True if connection pool must be cleared before trying this connection")]
        public bool ClearPool { get; set; }

        /// Connection client factory class name. Default is 'System.Data.SqlClient' for SQLClient
        [Description("Connection client factory class name")]
        public string Factory { get; set; }

        /// Database type
        [Description("Database type")]
        public DatabaseType DatabaseType { get; set; }

        private IDbConnection _current
        {
            get { return (IDbConnection)Context.StateBag.Get(this, "dbConnection", null); }
            set { Context.StateBag.Set(this, "dbConnection", value); }
        }


        private DatabaseType _dbType
        {
            get { return (DatabaseType)Context.StateBag.Get(this, "dbType", DatabaseType.Other); }
            set { Context.StateBag.Set(this, "dbType", value); }
        }
        

        /// Constructor
        public Database()
        {
            Factory = "System.Data.SqlClient";
            DatabaseType = DatabaseType.Auto;
        }

        /// Execute action
        public override object Execute()
        {
            string cs = Context.TransformStr(ConnectionString, Transform);
            string factory = Context.TransformStr(Factory, Transform);
            DbProviderFactory dbFactory = DbProviderFactories.GetFactory(factory);


			if (Context.Verbose)
			{
				var cb=dbFactory.CreateConnectionStringBuilder();
				cb.ConnectionString=cs;
				if (cb.ContainsKey("Password"))
					cb["Password"]="<removed>";
				if (cb.ContainsKey("pwd"))
					cb["pwd"]="<removed>";
	            VerboseMessage("Opening a DB connection {0} with cs='{1}'", factory, cb.ConnectionString);
			}
            var conn = dbFactory.CreateConnection();
            try {
                conn.ConnectionString = cs;

                if (ClearPool)
                {
                    VerboseMessage("Cleaning DB connection pool for cs='{0}'", cs);
                
                    if (conn is SqlConnection)
                        SqlConnection.ClearPool((SqlConnection)conn);
                    conn=dbFactory.CreateConnection();
                    conn.ConnectionString = cs;
                }

                conn.Open();
                
                if (DatabaseType == DatabaseType.Auto)
                {
                    _dbType = Core.DatabaseType.Other;
                     
                    using (var testCmd = conn.CreateCommand())
                    {
                        if (testCmd.GetType().FullName.Contains("Oracle"))
                            _dbType = DatabaseType.Oracle;
                        if (testCmd is SqlCommand)
                        {
                            _dbType = Core.DatabaseType.MsSql;
                            try
                            {
                                Version v = new Version(conn.ServerVersion);
                                if (v.Major >= 10)
                                    _dbType = Core.DatabaseType.MsSql2008;
                                else
                                    _dbType = Core.DatabaseType.MsSql2005;
                            }
                            catch (ArgumentException)
                            {

                            }
                        }
                    }
                }
                else
                    _dbType = DatabaseType;



                IDbConnection old = _current;
                try
                {
                    _current = conn;
                    return base.Execute();
                }
                finally
                {
                    _current = old;
                }

            }
            finally
            {
                if (conn!=null)
                    conn.Dispose();
            }
        }


        /// Specific database type, auto does not count
        public DatabaseType SpecificDatabaseType
        {
            get
            {
                return _dbType;
            }
        }

        
        /// Current database connection 
        public static IDbConnection CurrentConnection
        {
            get
            {
                if (ScriptContextScope.Current==null)
                    return null;
                var db = ScriptContextScope.Current.CallStack.Find<Database>(x=>true);
                if (db==null)
                    return null;
                return db._current;
            }
        }

        /// Current database connection 
        public static Database Current
        {
            get
            {
                if (ScriptContextScope.Current == null)
                    return null;
                var db = ScriptContextScope.Current.CallStack.Find<Database>(x => true);
                if (db == null)
                    return null;
                return db;
            }
        }

        
    }
}