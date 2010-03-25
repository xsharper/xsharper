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
using System.Text;

namespace XSharper.Core
{
    /// <summary>
    /// Combination of flags defining the look of the usage
    /// </summary>
    [Flags]
    public enum UsageOptions
    {
        /// No options
        [Description("No options")]
        None = 0,

        /// Display usage if /help or /? command line argument is present
        [Description("Display usage if /help or /? command line argument is present")]
        IfHelp = 4,

        /// Display usage if the script is invoked without arguments
        [Description("Display usage if the script is invoked without arguments")]
        IfNoArguments = 8,

        /// Print automatically generated usage line, for example Usage: XXX /arg1 /arg2
        [Description("Print automatically generated usage line, for example Usage: XXX /arg1 /arg2")]
        UsageLine = 16,

        /// Print automatically generated usage line, for example Usage: XXX /arg1 /arg2
        [Description("Print automatically generated usage line, for example Usage: XXX /arg1 /arg2")]
        UseVersionInfo = 32,

        /// For required values, and parameters which have defaults, automatically append (default: ZZZ) to the parameter help text
        [Description("For required values, and parameters which have defaults, automatically append (default: ZZZ) to the parameter help text")]
        AutoSuffix = 64,

        /// Default, which is UsageLine | IfHelp | UseVersionInfo
        [Description("Default, which is UsageLine | IfHelp | UseVersionInfo")]
        Default = UsageLine | IfHelp | UseVersionInfo
    }


    /// <summary>
    /// Defines looks of the script usage text
    /// </summary>
    [XsType(null)]
    [Description("Defines looks of the script usage text")]
    public class UsageGenerator : XsElement
    {
        /// Options
        [Description("Options")]
        public UsageOptions Options { get; set; }

        /// Maximal width of usage text. If console buffer is wider than this, the output will be no more than MaxWidth columns. Default 150.
        [Description("Maximal width of usage text. If console buffer is wider than this, the output will be no more than MaxWidth columns.")]
        public int MaxWidth { get; set; }

        /// Minimal width of usage text. If console buffer is narrower than this, the output will be no less than MinWidth columns. Default 40.
        [Description("Minimal width of usage text. If console buffer is narrower than this, the output will be no less than MinWidth columns.")]
        public int MinWidth { get; set; }

        /// Usage line prefix, default 'Usage: '
        [Description("Usage line prefix")]
        public string LinePrefix { get; set; }

        /// Usage line suffix, default ''
        [Description("Usage line suffix")]
        public string LineSuffix { get; set; }

        /// Exit code to terminate script when usage is displayed. Default -2.
        [Description("Exit code to terminate script when usage is displayed.")]
        public int ExitCode { get; set; }

        /// Constructor
        public UsageGenerator()
        {
            Options = UsageOptions.Default;
            MaxWidth = 150;
            MinWidth = 40;
            LinePrefix = "Usage: ";
            ExitCode = -2;
        }

        /// 
        public int CorrectWidth(int width)
        {
            if (width < 0)
            {
                if (Utils.HasRealConsole)
                    width = Console.BufferWidth;
                else
                    width = MaxWidth;
            }
            if (width < MinWidth)
                width = MinWidth;
            if (width > MaxWidth)
                width = MaxWidth;
            return width;
        }

        /// <summary>
        /// Whether usage should be displayed for given arguments and variables
        /// </summary>
        /// <param name="args">List of script command line arguments</param>
        /// <param name="vars">Result of parsing the command line</param>
        /// <returns>true if usage needs to be displayed, false otherwise</returns>
        public bool ShouldDisplayUsage(IEnumerable<string> args, Vars vars)
        {
            if ((Options & UsageOptions.IfHelp) != 0 && vars.GetBool("help", false))
                return true;
            if ((Options & UsageOptions.IfNoArguments) != 0 && (args == null || !args.GetEnumerator().MoveNext()))
                return true;
            return false;
        }

        /// <summary>
        /// Generate usage text for the script with given ID and descriptions
        /// </summary>
        /// <param name="context">Script context</param>
        /// <param name="description">Script description</param>
        /// <param name="id">Script ID</param>
        /// <param name="width">Console width</param>
        /// <param name="items">Command line parameters</param>
        /// <returns></returns>
        public string GetUsage(ScriptContext context, string description, string id, int width, CommandLineParameters items)
        {
            width = CorrectWidth(width);

            StringWriter sw = new StringWriter();

            // Write description
            if (!string.IsNullOrEmpty(description))
            {
                Utils.Wrap(sw, description, width, string.Empty);
                sw.WriteLine();
            }

            if ((Options & UsageOptions.UsageLine) != 0)
            {
                string prefix = LinePrefix + id;

                StringBuilder line = new StringBuilder(prefix);
                bool optional = false;
                foreach (CommandLineParameter p in items)
                {
                    string text = p.GetTransformedValue(context);
                    string vardescr = p.GetDescription(context);
                    if (p.Required)
                    {
                        if (!string.IsNullOrEmpty(p.Switch))
                            line.Append(" " + items.GetSwitchPrefixesArray()[0] + p.Switch);
                        if (p.Count != CommandLineValueCount.None)
                            line.Append(" " + p.GetDescription(context));
                    }
                    else if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(p.Switch))
                        optional = true;
                    else if (p.Count != CommandLineValueCount.None && string.IsNullOrEmpty(p.Switch) && !string.IsNullOrEmpty(vardescr))
                        line.Append(" [" + vardescr + "]");
                }

                if (optional && LineSuffix == null)
                    line.Append(" [parameters]");
                line.Append(LineSuffix);
                Utils.Wrap(sw, line.ToString(), width, new string(' ', prefix.Length + 1));
            }

            foreach (CommandLineParameter p in items)
            {
                string text = p.GetTransformedValue(context);
                if (!string.IsNullOrEmpty(text) &&
                    ((p.Switch ?? string.Empty).Trim().Length != 0 ||
                     (!string.IsNullOrEmpty(p.Var) && !string.IsNullOrEmpty(p.Value))))
                {
                    if ((Options & UsageOptions.UsageLine) != 0)
                        sw.WriteLine();
                    break;
                }
            }
            sw.Write(getArgsUsage(context, width, items));
            return sw.ToString();
        }
        private string getArgsUsage(ScriptContext context, int width, CommandLineParameters items)
        {
            List<Var> sb = new List<Var>();
            
            foreach (CommandLineParameter p in items)
            {
                string text = p.GetTransformedValue(context);
                string vardescr = p.GetDescription(context);

                string name;
                bool emptyName = (p.Switch ?? string.Empty).Trim().Length == 0;
                if (emptyName && string.IsNullOrEmpty(p.Var))
                    name = p.Switch ?? string.Empty;
                else
                {

                    if (string.IsNullOrEmpty(text))
                        continue;
                    if (emptyName)
                        name = " ";
                    else
                    {
                        var pp = items.GetSwitchPrefixesArray();
                        name = "  " + ((pp != null && pp.Length > 0) ? pp[0] : string.Empty) + p.Switch;
                    }
                }

                if (p.Count != CommandLineValueCount.None)
                {

                    if (p.Unspecified == null || emptyName)
                    {
                        if (!string.IsNullOrEmpty(vardescr))
                            name += " " + vardescr;
                    }
                    else
                        name += " [" + vardescr + "]";
                }

                StringBuilder vb = new StringBuilder();
                vb.Append(text);
                if ((Options & UsageOptions.AutoSuffix) != 0)
                {
                    if (p.Required)
                        vb.Append(" (required)");
                    else if (p.Default != null && p.Count != CommandLineValueCount.None)
                    {
                        string def = Utils.To<string>(context.Transform(p.Default, p.Transform));
                        vb.Append(" (default: " + def + ")");
                    }

                }
                Var v = new Var(name, vb.ToString());
                sb.Add(v);
            }

            StringWriter sw = new StringWriter();
            Utils.WrapTwoColumns(sw, sb, 30, width);
            return sw.ToString();
        }

        
    }
}