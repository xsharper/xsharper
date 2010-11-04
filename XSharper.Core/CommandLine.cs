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
using System.Globalization;

namespace XSharper.Core
{
    /// Number of command line values following this switch
    public enum CommandLineValueCount
    {
        /// No values. This is just a switch. For example /verbose
        [Description("No values. This is just a switch. For example /verbose")]
        None,

        /// Single value. For example, /length 5
        [Description("Single value. For example, /length 5")]
        Single,

        /// Force single value, even if it starts with switch prefix
        [Description("Force single value. For example, /param /s ")]
        ForceSingle,

        /// Multiple values. For example, /files file1 file2 file3
        [Description("Multiple values. For example, /files file1 file2 file3")]
        Multiple,
    }

    
    /// <summary>
    /// Command line parsing instruction
    /// </summary>
    [XsType(null)]    
    [Description("Command line argument")]
    public class CommandLineParameter : XsTransformableElement
    {
        /// Switch name
        [Description("Switch name")]
        public string Switch { get; set; }
        

        /// Semicolon-separated list of switch name synonyms
        [Description("Semicolon-separated list of switch name synonyms")]
        public string Synonyms { get; set; }
        

        /// Number of values following this switch
        [Description("Number of values following this switch")]
        public CommandLineValueCount Count { get; set;}
        

        /// Value assumed if no value is specified
        [Description("Value assumed if no value is specified")]
        public object Unspecified { get; set; }

        /// Verify that value matches the specified regex pattern
        [Description("Verify that value matches the specified regex pattern")]
        [XsNotTransformed]
        public string Pattern { get; set; }
        
        /// Default value
        [Description("Default value")]
        public object Default { get; set; }

        /// Convert the passed argument to this type
        [Description("Convert the passed argument to this type")]
        [XsAttribute("type")]
        [XsAttribute("typeName")]
        public string TypeName { get; set; }

        /// Hint
        [Description("Hint")]
        public string Hint { get; set; }
        
        /// Ignore any switches after this one
        [Description("Ignore any switches after this one")]
        public bool Last { get; set; }
        
        /// Value
        [XsAttribute("")]
        [XsAttribute("value")]
        [Description("Value")]
        public string Value { get; set; }

        /// Return value transformed
        public string GetTransformedValue(ScriptContext ctx)
        { 
            return ctx.TransformStr(Value, Transform);
        }
        
        /// Variable name
        [Description("Variable name")]
        public string Name { get; set; }
        

        /// Brief description. This is usually a single word (like "file") and matches as variable name (in which case it may be null).
        [Description("Brief description. This is usually a single word (like 'file') and matches as variable name (in which case it may be null).")]
        public string Description { get; set; }
        
        /// Get description of the command line argument by choosing between <see cref="Description"/> and <see cref="Var"/>
        public string GetDescription(ScriptContext ctx)
        {
            if (Description == null)
                return Var;
            return ctx.TransformStr(Description,Transform);
        }

        /// True if this argument is required
        [Description("True if this argument is required")]
        public bool Required { get; set; }
        
        /// Variable name
        public string Var
        {
            get
            {
                if (!string.IsNullOrEmpty(Name))
                    return Name;
                return Switch;
            }
        }

        /// Constructor
        public CommandLineParameter()
        {
            Count = CommandLineValueCount.Single;
        }

        /// Constructor (simple parameter)
        public CommandLineParameter(string name, CommandLineValueCount vt, object dv, object uv)
            : this()
        {
            Name = name;
            Count = vt;
            Default = dv;
            Unspecified = uv;
        }
        /// Constructor (required parameter)
        public CommandLineParameter(string name, CommandLineValueCount vt) : this()
        {
            Name = name;
            Count = vt;
            Required = true;
        }

        /// Constructor (switch)
        public CommandLineParameter(string name,string switchName, CommandLineValueCount vt, object dv, object uv) : this()
        {
            Name = name;
            Switch = switchName;
            Count = vt;
            Default = dv;
            Unspecified = uv;
        }

        /// Constructor (simple Switch)
        public CommandLineParameter(string switchName) : this()
        {
            Name = switchName;
            Switch = switchName;
            Count = CommandLineValueCount.None;
            Default = false;
            Unspecified = true;
        }
    }
}
