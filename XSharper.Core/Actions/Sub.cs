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

namespace XSharper.Core
{
    /// <summary>
    /// Subroutine parameter declaration
    /// </summary>
    [XsType(null)]
    public class SubParam : XsTransformableElement
    {
        /// Parameter name
        [XsRequired]
        public string Name { get; set; }

        /// Default value, if not specified (default: null )
        public object Default { get; set; }

        /// True if this parameter is required
        public bool Required { get; set; }
    }

    /// <summary>
    /// Subroutine
    /// </summary>
    [XsType("sub", ScriptActionBase.XSharperNamespace)]
    [Description("Subroutine")]
    public class Sub : Block
    {
        /// <summary>
        /// Subroutine parameters
        /// </summary>
        [XsElement("", SkipIfEmpty = true, CollectionItemElementName = "param", CollectionItemType = typeof(SubParam),Ordering=-1)]
        public List<SubParam> Parameters { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public Sub()
        {
            Parameters=new List<SubParam>();
        }
        
        /// Constructor
        public Sub(string id, params IScriptAction[] actions) : base(actions)
        {
            Id = id;
        }

        /// Explicitly execute this subroutine (implicit execution does nothing)
        public object ExecuteSub(IEnumerable<CallParam> args)
        {
            bool[] set=new bool[Parameters.Count];
            int p = 0;
            foreach (var sp in args)
            {
                object value=sp.Value;
                if (sp.Transform != TransformRules.None)
                    value = Context.Transform(value, sp.Transform);

                if (!string.IsNullOrEmpty(sp.Name))
                {
                    for (p=0;p<Parameters.Count;++p)
                        if (string.Compare(Parameters[p].Name,sp.Name,StringComparison.OrdinalIgnoreCase)==0)
                            break;
                    if (p==Parameters.Count)
                        throw new ParsingException("Unknown sub parameter "+sp.Name);
                }

                if (p >= Parameters.Count)
                    throw new ParsingException("Too many sub parameters specified");
                Context.Set(Parameters[p].Name, value);
                set[p] = true;
                p++;
            }

            for (int i = 0; i < Parameters.Count; ++i)
            {
                var parameter = Parameters[i];
                if (!set[i])
                {
                    if (parameter.Required)
                        throw new ParsingException("Required sub parameter " + parameter.Name + " is not specified.");
                    Context.Set(parameter.Name,Context.Transform(parameter.Default,parameter.Transform));
                }
            }

            return base.Execute();
        }

        /// Execute action
        public override object Execute()
        {
            return null;
        }
    }

}