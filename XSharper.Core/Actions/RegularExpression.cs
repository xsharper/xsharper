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
using System.Text.RegularExpressions;

namespace XSharper.Core
{
    /// Evaluate regular expression
    [XsType("regex")]
    [Description("Evaluate regular expression")]
    public class RegularExpression : Block
    {
        /// Object text property
        [Description("Object text property")]
        [XsAttribute(""),XsAttribute("value")]
        public string Value { get; set; }

        /// True if Transform property should be ignored and text returned verbatim (it would still apply to string fields of the object)
        [Description("True if Transform property should be ignored and text returned verbatim (it would still apply to string fields of the object")]
        public bool Verbatim { get; set; }

        /// <summary>
        /// Get Transformed Text 
        /// </summary>
        public string GetTransformedText()
        {
            if (Verbatim)
                return Value;
            return Context.TransformStr(Value, Transform);
        }

        /// Regular expression
        [Description("Regular expression")]
        public string Pattern { get; set; }

        /// If this is not null, with what to replace the found matches. Default null = search only
        [Description("If this is not null, with what to replace the found matches. Default null = search only")]
        public string Replace { get; set; }

        /// Maximal number of matches to find/replace. Default - unlimited
        [Description("Maximal number of matches to find/replace.")]
        [XsAttribute("maxCount"), XsAttribute("maxLoops", Deprecated = true), XsAttribute("max", Deprecated = true), XsAttribute("count", Deprecated = true)]
        public int? MaxCount { get; set; }
        
        /// True if variables should be set to captures after command completion. $_1 = \1, $_2= \2 etc
        [Description("True if variables should be set to captures after command completion. $_1 = \\1, $_2= \\2 etc.")]
        public bool SetCaptures { get; set; }

        /// <summary>
        /// For replace operation, output replaced string there. For match operation (default) - output all matched strings there.
        /// Default is null = no output
        /// </summary>
        [Description("For replace operation, output replaced string there. For match operation (default) - output all matched strings there.")]
        public string OutTo { get; set; }

        /// Regex options. Default is RegexOptions.IgnoreCase
        [Description("Regex options")]
        public RegexOptions Options { get; set; }

        /// Block to execute if nothing is found
        [XsElement("noMatch", SkipIfEmpty = true, Ordering=2)]
        [Description("Block to execute if nothing is found")]
        public Block NoMatch { get; set; }

        /// Variable prefix
        [Description("Variable prefix")]
        public string Name { get; set; }

        /// Execute a delegate for all child nodes of the current node
        public override bool ForAllChildren(Predicate<IScriptAction> func, bool isFind)
        {
            return base.ForAllChildren(func,isFind) || func(NoMatch);
        }

        /// Constructor
        public RegularExpression()
        {
            Options = RegexOptions.IgnoreCase;
        }
        /// <summary>
        /// Initialize action
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            Context.Initialize(NoMatch);    
        }

        static bool isEmpty(Block b)
        {
            if (b == null)
                return true;
            if (b.Items != null && b.Items.Count > 0)
                return false;
            
            return (b.Try == null && b.Catch == null && b.Finally == null);
        }

        /// Execute action
        public override object Execute()
        {
            // Get me text
            string text = GetTransformedText()??string.Empty;
            string pattern = Context.TransformStr(Pattern, Transform);
            Regex r = new Regex(pattern, Options);

            // Do match/replace
            string outp = null;
            if (Replace != null)
            {
                string rep = Context.TransformStr(Replace, Transform);
                if (MaxCount.HasValue)
                    outp = r.Replace(text, rep, MaxCount.Value);
                else
                    outp = r.Replace(text, rep);
            }
            else
            {
                Match m = r.Match(text);
                if (m.Success)
                    outp = m.Value;
            }
            Context.OutTo(Context.TransformStr(OutTo, Transform), outp);

            object ret = null;

            if (!(isEmpty(this) && isEmpty(NoMatch)) || SetCaptures)
            {
                MatchCollection coll = r.Matches(text);
                if (coll.Count == 0)
                {
                    if (NoMatch != null)
                    {
                        ret = Context.Execute(NoMatch);
                    }
                }
                else
                {
                    int mx = (MaxCount == null) ? coll.Count : Math.Min(coll.Count, MaxCount.Value);
                    for (int i = 0; i < mx; ++i)
                    {
                        Vars sv = new Vars();
                        if (SetCaptures)
                        {
                            setCaptureVars(r, coll[0], Context);
                            ret = baseexecute();
                        }
                        else
                        {
                            setCaptureVars(r, coll[i], sv);
                            ret = Context.ExecuteWithVars(baseexecute, sv, null);
                        }
                        if (ReturnValue.IsBreak(ret))
                        {
                            ret = null;
                            break;
                        }
                        if (ret != null)
                            break;
                    }
                }
            }
            return ret;
        }

        private object baseexecute()
        {
            return base.Execute();
        }
        private void setCaptureVars(Regex regex, Match match, Vars variables)
        {
            string pref = Context.TransformStr(Name, Transform);
            for (int j = 0; j < match.Groups.Count; ++j)
            {
                Group g = match.Groups[j];
                string name = regex.GroupNameFromNumber(j);
                if (name.Length > 0 && char.IsDigit(name[0]))
                    name = "_" + name;
                if (!string.IsNullOrEmpty(pref))
                    name = pref+name;
                if (g.Success)
                    variables[name] = g.Value;
                else
                    variables.Remove(name);
            }
        }
    }
}