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
    /// Reference a .NET assembly
    /// </summary>
    [XsType("reference", ScriptActionBase.XSharperNamespace)]
    [Description("Reference a .NET assembly")]
    public class Reference : ScriptActionBase
    {
        /// Assembly name. 
        [Description("Assembly name")]
        public string Name { get; set; }

        /// Assembly DLL filename. 
        [Description("Assembly DLL filename. ")]
        public string From { get; set; }

        /// true, if assembly contains additional actions and must be loaded immediately
        [Description("true, if assembly contains additional actions and must be loaded immediately")]
        public bool WithTypes { get; set; }

        /// true, if this assembly must be embedded into the executable
        [Description("true, if this assembly must be embedded into the executable")]
        public bool Embed { get; set; }

        /// true, if this assembly name should also be added as namespace
        [Description("true, if this assembly name should also be added as namespace")]
        public bool AddUsing { get; set; }

        /// true, if assembly is loaded during execution phase. By default assembly is loaded during initialization phase.
        [Description("true, if assembly is loaded during execution phase. By default assembly is loaded during initialization phase.")]
        public bool Dynamic { get; set; }

        /// <summary>
        /// Initialize action
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            if (!Dynamic && !WithTypes)
            {
                var ass=AddReference(Context, true);
                Name = ass.FullName;
                if (Embed)
                {
                    Context.AddEmbeddedFile(ass.GetName().Name + ".dll", ass.Location, true);
                }
            }
        }

        /// Add reference to the script context
        public Assembly AddReference(ScriptContext context,bool forceLoad)
        {
            string name = context.TransformStr(Name, Transform);
            bool addUsing = AddUsing;

            if (name!=null)
                if (name.StartsWith("@", StringComparison.Ordinal))
                {
                    addUsing = true;
                    AddUsing = true;
                    name = name.Substring(1);
                }

            string f = context.TransformStr(From, Transform);
            string from;
            if (string.IsNullOrEmpty(f))
                from = null;
            else
            {
                from = context.FindScriptPartFileName(f);
                if (from==null)
                    throw new FileNotFoundException("Cannot find referenced assembly", f);
                if (name == null)
                    name = AssemblyName.GetAssemblyName(from).FullName;
            }
            if (addUsing && !string.IsNullOrEmpty(name))
            {
                int n = name.IndexOf(",");
                if (n!=-1)
                    context.Compiler.AddHeaders("using " + name.Substring(0,n) + ";");
                else
                    context.Compiler.AddHeaders("using " + name + ";");
            }
            return context.Compiler.AddReference(from, name, false, forceLoad,string.Empty);
        }

        /// Execute action
        public override object Execute()
        {
            if (Dynamic && !WithTypes)
                AddReference(Context, true);
            return null;
            
        }

        /// <summary>
        /// Read element from the XML reader
        /// </summary>
        /// <param name="context">XML context</param>
        /// <param name="reader">Reader</param>
        public override void ReadXml(IXsContext context, XmlReader reader)
        {
            base.ReadXml(context,reader);
            if (WithTypes)
            {
                ScriptContext c = (ScriptContext) context;
                c.AddAssembly(AddReference(c, true),true);
            }
        }

        
    }
}