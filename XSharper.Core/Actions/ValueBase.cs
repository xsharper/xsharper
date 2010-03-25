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
    /// Abstract base class for actions which have a text field, that can be transformed
    /// </summary>
    public abstract class ValueBase : ScriptActionBase
    {
        /// Default object property
        [XsAttribute(""),XsAttribute("value")]
        [Description("Default object property")]
        public object Value { get; set; }

        /// True if Transform property should be ignored and value used verbatim (it would still apply to string attributes)
        [Description("True if Transform property should be ignored and value used verbatim (it would still apply to string attributes)")]
        public bool Verbatim { get; set; }

        /// Transformed Text property
        public string GetTransformedValueStr()
        {
            return Utils.To<string>(GetTransformedValue());
        }

        /// Transformed text property (if no transformation is required, returns original object)
        public object GetTransformedValue()
        {
            if (Verbatim)
                return Value;
            return Context.Transform(Value, Transform);
        }
    }

    /// <summary>
    /// Abstract base class for objects which allow its Text field to be either specified explicitly in the script, or specified in the external file
    /// </summary>
    public abstract class ValueFromFileBase : ValueBase
    {
        /// Filename or URI from where to read the text. 
        [Description("Filename or URI from where to read the text. ")]
        public string From { get; set; }

        /// Encoding of the file
        [Description("Encoding of the file")]
        public string Encoding { get; set; }
    }

    /// <summary>
    /// Abstract base class for actions that normally include text inline in the document,
    /// and as such the text should be affected by transformations, and loaded from file only
    /// during the execution time. 
    /// </summary>
    public abstract class DynamicValueFromFileBase : ValueFromFileBase
    {
        /// Constructor
        protected DynamicValueFromFileBase()
        {
        }

        /// Execute action
        public override object Execute()
        {
            if (!string.IsNullOrEmpty(From))
            {
                string strLoc = Context.TransformStr(From, Transform);
                Value=Context.ReadText(strLoc, Utils.GetEncoding(Context.TransformStr(Encoding, Transform)));
            }
            return null;
        }
    }

    /// <summary>
    /// Abstract base class for actions that normally include text verbatim, and any files included
    /// should be included during script compilation time
    /// </summary>
    public abstract class StaticValueFromFileBase : ValueFromFileBase
    {
        /// Constructor
        protected StaticValueFromFileBase()
        {
            Verbatim = true;
        }

        /// False if the From location should be read during script precompilation and included into program text (this would be appropriate for Code or Headers). True if the From location should be read during script execution only.
        [Description("False if the From location should be read during script precompilation and included into program text (this would be appropriate for Code or Headers). True if the From location should be read during script execution only.")]
        public bool Dynamic { get; set; }

        private string loadText()
        {
            string strLoc = Context.TransformStr(From, Transform);
            strLoc = Context.FindScriptPartFileName(strLoc);
            return Context.ReadText(strLoc, Utils.GetEncoding(Context.TransformStr(Encoding, Transform)));
        }

        /// <summary>
        /// Initialize action
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            if (!Dynamic && !string.IsNullOrEmpty(From))
            {
                Value = loadText();
                From = null;
            }
        }

        /// Execute action
        public override object Execute()
        {
            if (Dynamic && !string.IsNullOrEmpty(From))
            {
                Value = loadText();
            }
            return null;
        }
   
    }

}