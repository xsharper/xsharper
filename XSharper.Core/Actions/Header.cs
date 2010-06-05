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
using System.IO;
using System.Reflection;
using System.Xml;

namespace XSharper.Core
{
    /// <summary>
    /// Include C# header into the generated code
    /// </summary>
    [XsType("header", ScriptActionBase.XSharperNamespace)]
    [Description("Insert C# header into the generated code (can also be done as <?h ... ?>)")]
    public class Header : StaticValueFromFileBase
    {
        /// Constructor
        public Header()
        {
        }
        
        /// Constructor
        public Header(string text) 
        {
            Value = text;
        }
        
        /// true if the code must be compiled immediately during parsing, as it contains types and actions 
        /// essential to further script parsing
        [Description("true if the code must be compiled immediately during parsing, as it contains types and actions essential to further script parsing")]
        public bool WithTypes { get; set; }

        
        /// <summary>
        /// Read element from the XML reader
        /// </summary>
        /// <param name="context">XML context</param>
        /// <param name="reader">Reader</param>
        public override void ReadXml(IXsContext context, XmlReader reader)
        {
            base.ReadXml(context, reader);
            RunWithTypes(context);
        }

        internal void RunWithTypes(IXsContext context)
        {
            if (WithTypes)
            {
                Initialize();
                ScriptContext c = (ScriptContext) context;
                c.Compiler.AddHeaders(GetTransformedValueStr());
                Code code = new Code();
                c.Initialize(code);
                Assembly a = c.GetClassInstanceType(code).Assembly;
                c.AddAssembly(a,true);
            }
        }

        /// <summary>
        /// Initialize action
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            if (!Dynamic)
            {
                string text = GetTransformedValueStr();
                Context.Compiler.AddHeaders(text);
            }
        }

        /// Execute action
        public override object Execute()
        {
            if (Dynamic)
            {
                string text = GetTransformedValueStr();
                Context.Compiler.AddHeaders(text);
            }
            return null;
        }
    }
}