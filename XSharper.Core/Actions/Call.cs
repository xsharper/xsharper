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
    /// <summary>
    /// Subroutine parameter to a <see cref="Call"/> action
    /// </summary>
    [XsType(null)]
    public class CallParam : XsTransformableElement
    {
        /// Parameter name. May be null for sequential parameters
        [XsAttribute("name")]
        public string Name { get; set; }

        /// Parameter value.
        [XsAttribute("")]
        [XsAttribute("value")]
        public object Value { get; set; }

        /// Constructor
        public CallParam()
        {
            
        }

        /// Constructor
        public CallParam(string name, object value, TransformRules transformRules)
        {
            Name = name;
            Value = value;
            Transform = transformRules;
        }

        /// <summary>
        /// Called when XML Reader reads an attribute or a text field
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="attribute">Attribute name, or an empty string for element text</param>
        /// <param name="value">Attribute value</param>
        /// <param name="previouslyProcessed">List of previously processed attributes, to detect duplicate attributes. May be null if duplicate attributes are allowed.</param>
        /// <returns>true, if the attribute if correctly processed and false otherwise</returns>
        protected override bool ProcessAttribute(IXsContext context, string attribute, string value, IDictionary<string, bool> previouslyProcessed)
        {
            if (!base.ProcessAttribute(context, attribute, value, previouslyProcessed))
            {
                if (Name != null)
                    throw new ParsingException("Only a single variable may be set by set command.");
                Name = attribute;
                Value = value;
                if (previouslyProcessed != null && !string.IsNullOrEmpty(attribute))
                    previouslyProcessed.Add(attribute, true);
            }
            return true;
        }
    }

    /// <summary>
    /// Call a subroutine by ID
    /// </summary>
    [XsType("call", ScriptActionBase.XSharperNamespace)]
    [Description("Call a subroutine by ID")]
    public class Call : ValueBase
    {
        /// ID of the existing subroutine
        [Description("ID of the existing subroutine")]
        public string SubId { get; set; }
        

        /// Where to output the subroutine value. 
        [Description("Where to output the subroutine value. ")]
        public string OutTo { get; set; }

        /// Call isolation
        [Description("Call isolation")]
        public CallIsolation Isolation { get; set;}

        /// List of call parameters
        [XsElement("",SkipIfEmpty = true, CollectionItemElementName = "param", CollectionItemType = typeof(CallParam))]
        public List<CallParam> Parameters { get; set; }

        /// Constructor
        public Call()
        {
            Parameters = new List<CallParam>();
            Isolation = CallIsolation.Default;
        }

        /// <summary>
        /// Initialize action
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            if (string.IsNullOrEmpty(SubId))
                throw new ParsingException("SubId must be specified");
        }

        /// Execute action
        public override object Execute()
        {
            string id = Context.TransformStr(SubId, Transform);
            Sub f = Context.Find<Sub>(id);
            if (f == null)
                throw new ParsingException("A subroutine with id=" + id + " not found");
            
            var cp=new List<CallParam>();
            if (Value!=null)
            {
                var o = GetTransformedValue();
                if (o is Array)
                {
                    foreach (var elem in (Array)o)
                        cp.Add(new CallParam(null, elem, TransformRules.None));
                }
                else
                {
                    List<string> p = new List<string>();
                    string v = Utils.To<string>(o);
                    if (!string.IsNullOrEmpty(v))
                        p.AddRange(Utils.SplitArgs(v));
                    foreach (var o1 in p)
                        cp.Add(new CallParam(null, o1, TransformRules.None));
                }
            }
            if (Parameters!=null)
                cp.AddRange(Parameters);
            object r= Context.ExecuteAction(f,cp,Isolation);
            Context.OutTo(OutTo, ReturnValue.Unwrap(r));
            return null;
        }
    }
}