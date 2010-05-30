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
using System.Collections;
using System.ComponentModel;

namespace XSharper.Core
{
    /// Command line parameter, for execution of a shell command or another XSharper script
    [XsType(null)]
    public class ShellArg : XsTransformableElement
    {
        /// Add switch before value. 
        public string Switch { get; set; }

        /// Value to pass
        [XsAttribute("value"), XsAttribute("")]
        public object Value { get; set; }

        /// Default constructor
        public ShellArg()
        {

        }

        /// Constructor with value
        public ShellArg(object value, TransformRules transformRules)
        {
            Value = value;
            Transform = transformRules;
        }

        /// Create list of command line parameters, transforming and separating elements as appropriate
        public static string[] GetParams(ScriptContext context, IEnumerable<ShellArg> parameters)
        {
            return getArgs(context, parameters, false);
        }

        /// Build command line
        public static string GetCommandLine(ScriptContext context, IEnumerable<ShellArg> parameters)
        {
            return string.Join(" ",getArgs(context, parameters, true));
        }
        static string[] getArgs(ScriptContext context, IEnumerable<ShellArg> parameters, bool quoted)
        {
            List<string> p = new List<string>();
            foreach (ShellArg parameter in parameters)
            {
                string s = context.TransformStr(parameter.Switch, parameter.Transform);
                if (!string.IsNullOrEmpty(s))
                    p.Add(s);
                
                object o = context.Transform(parameter.Value, parameter.Transform);
                var arr = o as IEnumerable;
                if (arr != null && !(o is string))
                {
                    foreach (var obj in arr)
                        if (quoted)
                            p.Add(context.TransformStr(Utils.To<string>(obj), TransformRules.QuoteArg));
                        else
                            p.Add(Utils.To<string>(obj));
                }
                else if (o!=null)
                {
                    if (quoted)
                        p.Add(context.TransformStr(Utils.To<string>(o), TransformRules.QuoteArg));
                    else
                        p.Add(Utils.To<string>(o));
                }
            }
            return p.ToArray();
        }
    }

    
    /// Call isolation to use when calling scripts or subroutines
    public enum CallIsolation
    {
        /// No isolation at all. Subroutine can read or write all variables
        [Description("No isolation at all. Subroutine can read or write all variables")]
        None = 0,

        /// Medium isolation (default). Subroutine can can access all existing variables, but changes will be restored when the call returns
        [Description("Medium isolation (default). Subroutine can can access all existing variables, but changes will be restored when the call returns")]
        Default=1,

        /// Subroutine cannot access any of the variables except its parameters
        [Description("Subroutine cannot access any of the variables except its parameters")]
        High = 2,

    }

    /// Execute script
    [XsType("exec", ScriptActionBase.XSharperNamespace)]
    [Description("Execute script")]
    public class Exec : ValueBase
    {
        /// if not null, find script action with this ID and execute the loaded script
        [Description("if not null, find script action with this ID and execute")]
        [XsAttribute("scriptId")]
        public string ScriptId { get; set; }

        /// if not null, find Include action with this ID and execute the loaded script
        [Description("if not null, find Include action with this ID and execute the loaded script")]
        [XsAttribute("includeId")]
        public string IncludeId { get; set; }

        /// Dynamically load the script from file
        [Description("Dynamically load the script from file")]
        [XsAttribute("from"),XsAttribute("location")]
        public string From { get; set; }

        /// Extra path to try
        [Description("Extra path to try")]
        public string Path { get; set; }

        /// Where to put script output value
        [Description("Where to put script output value")]
        public string OutTo { get; set; }

        /// Call isolation. Default is Medium.
        [Description("Call isolation.")]
        public CallIsolation Isolation { get; set; }

        /// List of script arguments
        [Description("List of script arguments")]
        [XsElement("", SkipIfEmpty = true, CollectionItemElementName = "param", CollectionItemType = typeof(ShellArg))]
        public List<ShellArg> Args { get; set; }

        /// Validate script signature after reading the file. 
        [Description("Validate script signature after reading the file. ")]
        public bool ValidateSignature { get; set; }

        /// Default constructor
        public Exec()
        {
            Args = new List<ShellArg>();
            Isolation = CallIsolation.Default;
        }

        private Script _loadedScript
        {
            get { return (Script) Context.StateBag.Get(this, "loadedScript", null); }
            set { Context.StateBag.Set(this, "loadedScript", value); }
        }

        /// <summary>
        /// Execute a delegate for all child nodes of the current node
        /// </summary>
        public override bool ForAllChildren(Predicate<IScriptAction> func,bool isFind)
        {
            return base.ForAllChildren(func,isFind) || func(_loadedScript);
        }

        /// Execute action
        public override object Execute()
        {
            try
            {

                // Prepare parameters
                List<string> p=new List<string>();
                string v = GetTransformedValueStr();
                if (!string.IsNullOrEmpty(v))
                    p.AddRange(Utils.SplitArgs(v));
                p.AddRange(ShellArg.GetParams(Context, Args));

                // Choose script
                string incId = Context.TransformStr(IncludeId, Transform);
                string scriptId = Context.TransformStr(ScriptId, Transform);
                if (!string.IsNullOrEmpty(scriptId))
                    _loadedScript = Context.Find<Script>(scriptId);
                else if (!string.IsNullOrEmpty(incId))
                    _loadedScript = Context.Find<Include>(incId).IncludedScript;
                else
                {
                    string f = Context.TransformStr(From, Transform);
                    if (string.IsNullOrEmpty(From))
                    {
                        if (p.Count > 0)
                        {
                            f = p[0];
                            p.RemoveAt(0);
                        }
                        else
                            throw new ScriptRuntimeException("Script not specified");
                    }
                    string fFound = Context.FindScriptPartFileName(f, Context.TransformStr(Path, Transform));
                    if (fFound != null)
                        f = fFound;
                    _loadedScript = Context.LoadScript(f, ValidateSignature);
                    Context.Initialize(_loadedScript);
                }
                // Execute script
                Context.OutTo(Context.TransformStr(OutTo, Transform), Context.ExecuteScript(_loadedScript, p, Isolation));
                return null;
            }
            finally
            {
                _loadedScript = null;
            }
        }
    }
}