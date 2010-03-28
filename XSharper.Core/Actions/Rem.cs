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
    /// Script comment
    /// </summary>
    [XsType("rem", ScriptActionBase.XSharperNamespace)]
    [Description("Script comment")]
    public class Rem : ScriptActionBase
    {
        /// <summary>
        /// Comment text
        /// </summary>
        [XsAttribute("")]
        public string Text { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public Rem()
        {
            
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="text">comment text</param>
        public Rem(string text)
        {
            Text = text;
        }

        /// Execute action
        public override object Execute()
        {
            return null;
        }

        /// <summary>
        /// Write element to the output stream
        /// </summary>
        /// <param name="writer">Where to write</param>
        /// <param name="nameOverride">Local name to be used, or null if name should be retirevent from <see cref="XsTypeAttribute"/> of the type.</param>
        public override void WriteXml(System.Xml.XmlWriter writer, string nameOverride)
        {
            if (Text!=null && (Text.Contains("---") || Text.IndexOfAny("<>&".ToCharArray())!=-1))
                writer.WriteProcessingInstruction("rem",Text);
            else
                writer.WriteComment(Text);
        }


    }
}