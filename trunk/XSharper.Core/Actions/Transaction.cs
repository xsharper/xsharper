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
using System.Transactions;

namespace XSharper.Core
{
    
    /// Transaction scope
    [XsType("transaction", ScriptActionBase.XSharperNamespace)]
    [Description("Transaction scope")]
    public class Transaction : Block
    {
        /// Transaction scope options. Default: TransactionScopeOption.Required
        [Description("Transaction scope options.")]
        public TransactionScopeOption Option { get; set; }

        /// Transaction scope isolation level. Default: IsolationLevel.Unspecified
        [Description("Transaction scope isolation level")]
        public IsolationLevel IsolationLevel { get; set; }

        /// Transaction scope timeout. Default: 1 minute
        [Description("Transaction scope timeout")]
        public string Timeout { get; set; }

        /// Constructor
        public Transaction()
        {
            
            Option = TransactionScopeOption.Required;
            IsolationLevel = IsolationLevel.Unspecified;
            Timeout = "00:01:00";
        }
        /// Execute action
        public override object Execute()
        {
            TransactionOptions options = new TransactionOptions
                                             {
                                                 IsolationLevel = IsolationLevel, 
                                                 Timeout = (Utils.ToTimeSpan(Context.TransformStr(Timeout, Transform)) ?? TimeSpan.FromSeconds(60))
                                             };

            VerboseMessage(" -- Starting transaction {0} (Isolation: {1}) --",Option,IsolationLevel);
            
            object ret;
            using (TransactionScope scope=new TransactionScope(Option,options))
            {
                try
                {
                    ret = base.Execute();
                    scope.Complete();
                    VerboseMessage(" -- Transaction commit --");
                }
                catch
                {
                    VerboseMessage(" -- Transaction rollback --");
                    throw;
                }
            }

            return ret;
        }
    }
}