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
using System.Text;

namespace XSharper.Core
{
    /// <summary>
    /// Script version information
    /// </summary>
    [XsType(null)]
    [Description("Script version information")]
    public class VersionInfo : XsElement
    {
        /// Version, as X.X.X.X 
        [Description("Version, as X.X.X.X ")]
        public string Version { get; set; }

        /// Script title
        [Description("Script title")]
        public string Title { get; set;}
        
        /// Script description. May contain multiple paragraphs.
        [XsAttribute(""),XsAttribute("value")]
        [Description("Script description. May contain multiple paragraphs.")]
        public string Value { get; set;}

        /// Product name, to put into the appropriate field of the version information in the generated .exe file
        [Description("Product name, to put into the corresponding field of the version information in the generated .exe file")]
        public string Product { get; set;}

        /// Company name, to put into the appropriate field of the version information in the generated .exe file
        [Description("Company name, to put into the corresponding field of the version information in the generated .exe file")]
        public string Company { get; set; }

        /// Copyright info, to put into the corresponding field of the version information in the generated .exe file
        [Description("Copyright info, to put into the corresponding field of the version information in the generated .exe file")]
        public string Copyright { get; set; }

        /// How to transform the fields before use
        [XsAttribute("tr"),XsAttribute("transform")]
        [Description("How to transform the fields before use")]
        public TransformRules Transform { get; set; }

        /// Constructor
        public VersionInfo()
        {
            Transform = TransformRules.Expand;
        }
        /// <summary>
        /// Generate usage header
        /// </summary>
        /// <param name="context">Script context</param>
        /// <param name="appendValue">true, if Value should be appended to the generated text</param>
        /// <returns></returns>
        public string GenerateInfo(ScriptContext context , bool appendValue)
        {
            var title=context.TransformStr(Title, Transform);
            var value = context.TransformStr(Value, Transform);
            var copyright = context.TransformStr(Copyright, Transform);
            var version = context.TransformStr(Version, Transform);

            var sb=new StringBuilder();
            if (!string.IsNullOrEmpty(title))
            {
                sb.Append(title);
                if (!string.IsNullOrEmpty(version))
                {
                    sb.Append(" version ");
                    sb.Append(version);
                }
                if (!string.IsNullOrEmpty(copyright))
                {
                    if (sb.Length > 60)
                        sb.AppendLine();

                    sb.Append("  ");
                    sb.Append(copyright);
                }
            }
            if (appendValue && !string.IsNullOrEmpty(value))
            {
                if (sb.Length > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine();
                }
                sb.Append(value);
            }
            return sb.ToString();
        }
    }
}