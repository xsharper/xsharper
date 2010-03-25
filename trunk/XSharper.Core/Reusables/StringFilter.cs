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
using System.IO;
using System.Text.RegularExpressions;

namespace XSharper.Core
{
    /// <summary>
    /// File/Directory name filter syntax
    /// </summary>
    public enum FilterSyntax
    {
        /// If filter starts with ^, treat it as Pattern, otherwise as wildcard
        [Description("If filter starts with ^, treat it as Pattern, otherwise as wildcard")]
        Auto,

        /// <summary>
        /// A semi-colon separated list of masks. Where *=any number of any characters, ?=single character. Mask may be prefixed with - to exclude, or + to include.
        /// For example: +*.x??;-*.xls => all files with 3 letter extension that starts with x, except xls
        /// </summary>
        [Description("A semi-colon separated list of masks. Where *=any number of any characters, ?=single character. Mask may be prefixed with - to exclude, or + to include.")]
        Wildcard,

        /// Complete regular expression
        [Description("Complete regular expression")]
        Pattern,

    }

    /// <summary>
    /// Abstract string filter interface. 
    /// </summary>
    public interface IStringFilter
    {
        /// <summary>
        /// Check if string matches filter.
        /// </summary>
        /// <param name="str">String to validate against filter</param>
        /// <returns>Returns true if <paramref name="str"/> matches filter, or false otherwise</returns>
        bool IsMatch(string str);
    }

    /// <summary>
    /// File path filter. Full file path is evaluated
    /// </summary>
    public class FullPathFilter : StringFilter
    {
        /// constructor
        public FullPathFilter(FilterSyntax syntax, string filter)
            : base(syntax, filter)
        {
        }

        /// True if name matches filter
        public override bool IsMatch(string name)
        {
            var z = Path.GetFullPath(name);
            return base.IsMatch(z);
        }
    }

    /// <summary>
    /// Filename only filter. Only filename is evaluated, not the path
    /// </summary>
    public class FileNameOnlyFilter : StringFilter
    {
        /// Constructor
        public FileNameOnlyFilter(FilterSyntax syntax, string filter)
            : base(syntax, filter)
        {
        }

        /// True if name matches filter
        public override bool IsMatch(string name)
        {
            FileInfo fi = new FileInfo(name);
            string sN = fi.Name;
            if (fi.Extension.Length == 0)
                sN += ".";
            return base.IsMatch(sN);
        }
    }


   /// <summary>
   /// Generic string filter
   /// </summary>
    public class StringFilter : IStringFilter
    {
        private readonly List<Regex> _included=new List<Regex>();
        private readonly List<Regex> _excluded=new List<Regex>();

        /// <summary>
        /// Constructor
        /// </summary>
        public StringFilter(FilterSyntax syntax, string filter)
        {
            if (filter != null)
            {
                switch (syntax)
                {
                    case FilterSyntax.Auto:
                        if (filter.StartsWith("^", StringComparison.Ordinal))
                            goto case FilterSyntax.Pattern;
                        goto case FilterSyntax.Wildcard;

                    case FilterSyntax.Wildcard:
                        foreach (string s in filter.Split(';'))
                            processFilter(s, true);
                        break;
                    case FilterSyntax.Pattern:
                        processFilter(filter, false);
                        break;
                }
            }
        }

        /// Construct filter, automatically detecting filter format
        public StringFilter(string filter) :this(FilterSyntax.Auto,filter)
        {
        }

       private void processFilter(string filter,bool convert)
        {
            bool included = true;
            if (filter.StartsWith("-", StringComparison.Ordinal))
            {
                filter = filter.Substring(1);
                included = false;
            }
            else if (filter.StartsWith("+", StringComparison.Ordinal))
                filter = filter.Substring(1);
            
            if (convert)
                filter = Utils.WildcardToPattern(filter);

            Regex r=new Regex(filter,RegexOptions.Singleline|RegexOptions.Compiled|RegexOptions.CultureInvariant|RegexOptions.IgnoreCase);
            if (included)
                _included.Add(r);
            else
                _excluded.Add(r);
        }

        /// True if name matches filter
        public virtual bool IsMatch(string name)
        {
            bool ret = (_included.Count==0);
            foreach (var regex in _included)
                if (regex.IsMatch(name))
                {
                    ret = true;
                    break;
                }
            
            if (ret)
                foreach (var regex in _excluded)
                    if (regex.IsMatch(name))
                    {
                        ret = false;
                        break;
                    }
            return ret;
        }
    }
}