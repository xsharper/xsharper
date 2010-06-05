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
using System.ComponentModel;

namespace XSharper.Core
{
    /// <summary>
    /// Execute block while ANY or ALL of the conditions are true
    /// </summary>
    [XsType("while", ScriptActionBase.XSharperNamespace)]
    [Description("Execute block while ANY or ALL of the conditions are true")]
    public class While : Conditional
    {
        /// <summary>
        /// Number of loops. Default - infinite
        /// </summary>
        [Description("Maximum number of loops. null = infinite")]
        [XsAttribute("maxCount"), XsAttribute("maxLoops", Deprecated = true), XsAttribute("max", Deprecated = true), XsAttribute("count", Deprecated = true)]
        public string MaxCount { get; set; }
        
        /// <summary>
        /// Loop counter variable name
        /// </summary>
        [Description("Loop counter variable name")]
        public string Name { get; set; }
        
        /// Default constructor
        public While()
        {
            MaxCount = null;
        }
        /// Block constructor
        public While(params IScriptAction[] data)
            : base(data)
        {
            MaxCount = null;
        }

        object baseexecute() { 
            return base.Execute();
        }
        /// Execute action
        public override object Execute()
        {
            var pref = Context.TransformStr(Name, Transform);

            int? maxCount = Utils.To<int?>(Context.TransformStr(MaxCount, Transform));  
            for (int n = 0; (maxCount == null || n< maxCount) && ShouldRun(); ++n)
            {
                Context.CheckAbort();
                Vars sv=new Vars();
                sv[string.Empty] = n;
                object o = Context.ExecuteWithVars(baseexecute, sv, pref);
                
                if (ReturnValue.IsBreak(o))
                    return null;
                if (o!=null)
                    return o;
            }
            return null;
        }
    }
}