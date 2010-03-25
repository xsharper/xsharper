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

namespace XSharper.Core
{
    /// <summary>
    /// Embed an external file into the produced executable
    /// </summary>
    [XsType("embed", ScriptActionBase.XSharperNamespace)]
    [Description("Embed an external file into the produced executable")]
    public class Embed : ScriptActionBase
    {
        /// Internal name. The file will be accessible as embed:///name
        public string Name { get; set; }

        /// File location
        [Description("File location")]
        public string From { get; set; }

        /// True if this is an assembly that will be called by the script
        [Description("True if this is an assembly that will be called by the script")]
        public bool IsAssembly { get; set; }

        /// Initialize action
        public override void Initialize()
        {
            base.Initialize();
            bool found = false;
            if (Name!=null)
            {
                // try to get embedded resource, perhaps it's been already embedded?
                try
                {
                    Context.OpenReadStream("embed:///" + Name);
                    found = true;
                }
                catch (System.IO.IOException)
                {
                    
                }
            }
            if (!found)
            {
                string f = Context.TransformStr(From, Transform);
                string fromex = Context.FindScriptPartFileName(f);
                if (fromex == null)
                    throw new FileNotFoundException("Cannot find file to embed", f);
                string nameex = Context.TransformStr(Name, Transform);
                Name=Context.AddEmbeddedFile(nameex, fromex, IsAssembly);
            }
        }

        /// Execute action
        public override object Execute()
        {
            return null;
        }
    }
}