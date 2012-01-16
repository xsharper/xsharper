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
using System.Xml;

namespace XSharper.Core
{
    /// <summary>
    /// Include a script from file. This script is loaded only, not executed.
    /// To execute the loaded script use exec action
    /// </summary>
    [XsType("include", ScriptActionBase.XSharperNamespace)]
    [Description("Include a script from file. This script is loaded only, not executed. To execute the loaded script use exec action")]
    public class Include : ScriptActionBase
    {
        /// File from where to load the file
        [XsAttribute("from"), XsAttribute("location")]
        [Description("File from where to load the file")]
        public string From { get; set; }

        /// Extra path to try
        [Description("Extra path to try")]
        public string Path { get; set; }

        /// Whether signature is to be validated
        [Description("Whether signature is to be validated")]
        public bool ValidateSignature { get; set; }

        /// Included script, after reading the file. 
        [XsElement("includedScript", SkipIfEmpty = true)]
        public Script IncludedScript { get; set; }

        /// True if the script must be loaded during execution phase (for example if filename is an expression), or
        /// false if the script must be loaded during compilation phase
        public bool Dynamic { get; set; }

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
            if (WithTypes)
            {
                Initialize();
            }
        }

        /// Initialize action
        public override void Initialize()
        {
            base.Initialize();
            if (!Dynamic && IncludedScript == null)
                load();
            Context.Initialize(IncludedScript);
        }

        private void load()
        {
            string sloc = Context.FindScriptPartFileName(Context.TransformStr(From, Transform), Context.TransformStr(Path, Transform));
            VerboseMessage("Loading include file '"+sloc+"'");
            IncludedScript = Context.LoadScript(sloc, ValidateSignature);
        }

        /// Execute a delegate for all child nodes of the current node
        public override bool ForAllChildren(Predicate<IScriptAction> func,bool isFind)
        {
            return base.ForAllChildren(func, isFind) || func(IncludedScript);
        }

        /// Execute action
        public override object Execute()
        {
            // Execute script
            if (IncludedScript == null || Dynamic)
            {
                load();
                Context.Initialize(IncludedScript);
            }
            return null;
        }
    }
 
}