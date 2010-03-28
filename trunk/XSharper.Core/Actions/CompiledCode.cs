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
using System.Text;
using System.Xml;

namespace XSharper.Core
{
    /// <summary>
    /// Action that represents compiled code, unlike Code action that represents an action with c# source code
    /// </summary>
    public class CompiledCode : Code
    {
        /// <summary>
        /// Executable script
        /// </summary>
        public ExecutableScriptBase Script { get; set; }

        private class UserCode : ExecutableScriptBase
        {
            readonly ScriptContext.ScriptExecuteMethod _method;
            public UserCode(ScriptContext.ScriptExecuteMethod method)
            {
                _method = method;
            }
            public override object Execute()
            {
                return _method();
            }
        }

        /// Default constructor
        public CompiledCode()
        {
            
        }

        /// Constructor
        public CompiledCode(Code code)
        {
            Methods = code.Methods;
            Id = code.Id;
            
        }

        /// Constructor accepting delegate
        public CompiledCode(ScriptContext.ScriptExecuteMethod method)
        {
            Script = new UserCode(method);
        }
        #region IScriptAction Members


        /// Execute action
        public override object Execute()
        {
            return Script.Execute();
        }


        #endregion

        /// <summary>
        /// Write element to the output stream
        /// </summary>
        /// <param name="writer">Where to write</param>
        /// <param name="nameOverride">Local name to be used, or null if name should be retirevent from <see cref="XsTypeAttribute"/> of the type.</param>
        public override void WriteXml(XmlWriter writer, string nameOverride)
        {
            Code ce = new Code();
            ce.Methods = Methods;
            ce.Id = Id;
            string sourceCode=Script.GetSourceCode();
            if (sourceCode==null)
                throw new ScriptRuntimeException("This script cannot be decompiled!");
            ce.Value = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(sourceCode));

            ce.WriteXml(writer,nameOverride);
        }
    }
}