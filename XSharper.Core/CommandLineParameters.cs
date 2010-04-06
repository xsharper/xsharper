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
using System.Text.RegularExpressions;

namespace XSharper.Core
{
    /// <summary>
    /// Collection of command line processing instructions
    /// </summary>
    [Serializable]
    public class CommandLineParameters : List<CommandLineParameter>
    {
        private readonly string _switchPrefixes="/;-";
        private readonly bool _unknownSwitches;
        
        /// Default constructor
        public CommandLineParameters() 
        {
        }

        /// Constructor
        public CommandLineParameters(IEnumerable<CommandLineParameter> args, string switchPrefixes, bool unknownSwitches) : base(args)
        {
            _switchPrefixes=switchPrefixes;
            _unknownSwitches = unknownSwitches;
        }
        

        /// <summary>
        /// Parse command line
        /// </summary>
        /// <param name="context">Script context</param>
        /// <param name="args">Script command line arguments</param>
        /// <param name="autoHelp">true if "/?" and "/help" should be handled, even if no appropriate instructions are in the list</param>
        public void Parse(ScriptContext context,IEnumerable<string> args, bool autoHelp)
        {
            clearArgs(context);

            bool searchParameters = true;
            CommandLineParameter currentParam = null;

            int positionalCount = 0;
            
            List<string> argsList = new List<string>();
            if (args!=null)
                argsList.AddRange(args);

            CommandLineParameter prevMultiple = null;
            foreach (string original in argsList)
            {
                string value=null;
                CommandLineParameter p=null;
                if (searchParameters && 
                    !(Count > positionalCount && this[positionalCount].Count == CommandLineValueCount.ForceSingle) &&
                    !(currentParam != null && currentParam.Count == CommandLineValueCount.ForceSingle))
                    p=findNamedParam(original, autoHelp, out value);
                
                // If there is a non-param, add it to the previous param values
                if (p==null)
                {
                    if (currentParam != null)
                        currentParam = setSwitchValue(context,currentParam, original);
                    else
                        searchParameters=addUnprocessedArg(context, original, positionalCount++);
                    continue;
                }

                // Another command found. If there is a pending command, that must have values - throw
                if (currentParam != null)
                    completeSwitch(context,currentParam);

                if (p.Last)
                    searchParameters=false;

                if (currentParam != null && currentParam.Count == CommandLineValueCount.Multiple)
                    prevMultiple = currentParam;
                currentParam = setSwitchValue(context, p , value);
                if (currentParam == null && prevMultiple != null)
                    currentParam = prevMultiple;
            }
            completeSwitch(context,currentParam);

            foreach (CommandLineParameter p in this)
            {
                if (p.Var!=null && context.IsSet(p.Var) && p.TypeName != null)
                    context[p.Var] = fixType(context, p, context[p.Var]);
            }
            ApplyDefaultValues(context);
        }

        /// Apply default values to unset variables 
        public void ApplyDefaultValues(ScriptContext context)
        {
            foreach (CommandLineParameter p in this)
            {
                if (p.Var != null && p.Default != null && !context.IsSet(p.Var))
                {
                    object o = context.Transform(p.Default, p.Transform);
                    context[p.Var] = fixType(context,p,o);
                }
            }
        }

        private static object fixType(ScriptContext context, CommandLineParameter parameter, object o)
        {
            var p = context.Transform(parameter.TypeName, parameter.Transform);
            var t = p as Type;
            if (t==null && p!=null)
            {
                t = context.FindType(Utils.To<string>(p));
                if (t==null)
                    throw new ParsingException(string.Format("Type {0} cannot be resolved",p));
            }
            if (t != null)
                return Utils.To(t, o);
            return o;
        }

        /// Ensure that all required values are set, and throw <see cref="ParsingException"/> in case of error
        public void CheckRequiredValues(ScriptContext context)
        {
            foreach (CommandLineParameter p in this)
                if (p.Required && !context.IsSet(p.Var))
                {
                    if (!string.IsNullOrEmpty(p.Switch))
                        throw new ParsingException(string.Format("Required parameter {0}{1} is not specified",
                                                                GetSwitchPrefixesArray()[0], p.Switch));
                    throw new ParsingException(string.Format("Required parameter {0} is not specified", p.GetDescription(context)));
                }

        }

        /// Return array of allowed switch prefixes
        public string[] GetSwitchPrefixesArray()
        {
            string[] r = (_switchPrefixes ?? string.Empty).Split(';');
            if (r.Length == 0)
                return new string[0];
            return r;
        }


        #region -- Implementation details --
        private void clearArgs(ScriptContext context)
        {
            // restore only parameters
            Dictionary<string, bool> toClear = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (CommandLineParameter v in this)
                if (!string.IsNullOrEmpty(v.Var))
                    toClear[v.Var] = true;

            List<string> toRemove = new List<string>();
            foreach (Var pair in context)
            {
                if (toClear.ContainsKey(pair.Name))
                    toRemove.Add(pair.Name);
            }
            foreach (string tr in toRemove)
                context.Remove(tr);

            
        }

        private CommandLineParameter findNamedParam(string original, bool autoHelp, out string value)
        {
            string command = null;
            value = null;
            Match m = Regex.Match(original, @"^(?<cmd>[\w\d\._!@#$%&*()/-]+)\s*(=|:)(?<value>.*)$");
            if (m.Success)
            {
                command = m.Groups["cmd"].Value;
                value = m.Groups["value"].Value;
            }
            else
                command = original;

            bool helpVar = false;
            foreach (CommandLineParameter p in this)
            {
                if (p.Var == "help")
                    helpVar = true;
                if (string.IsNullOrEmpty(p.Switch))
                    continue;

                foreach (string cp in GetSwitchPrefixesArray())
                    if (command.StartsWith(cp, StringComparison.Ordinal))
                    {
                        string s = command.Substring(cp.Length);
                        
                        if (string.Compare(p.Switch, s, StringComparison.OrdinalIgnoreCase) == 0)
                            return p;

                        if (!string.IsNullOrEmpty(p.Synonyms))
                            foreach (string syn in p.Synonyms.Split(';'))
                                if (string.Compare(syn, s, StringComparison.OrdinalIgnoreCase) == 0)
                                    return p;
                    }
            }
            
            foreach (string cp in GetSwitchPrefixesArray())
                if (command.StartsWith(cp, StringComparison.Ordinal) && cp.Length > 0 && command.Length != cp.Length)
                {
                    string s = command.Substring(cp.Length);
                    if (!helpVar && autoHelp)
                    {
                        if (string.Compare(s, "help", StringComparison.OrdinalIgnoreCase) == 0 ||
                            string.Compare(s, "?", StringComparison.OrdinalIgnoreCase) == 0)
                            return new CommandLineParameter("help", "help", CommandLineValueCount.None, null, "true")
                                       {Synonyms = "?"};
                    }
                    if (!_unknownSwitches)
                        throw new ParsingException(string.Format("Unknown parameter {0}", original));
                }
            return null;
        }

        
        private static CommandLineParameter setSwitchValue(ScriptContext context,CommandLineParameter param, object value)
        {
            if (param != null && !string.IsNullOrEmpty(param.Var))
            {
                if (!string.IsNullOrEmpty(param.Pattern) && value != null && !Regex.IsMatch(Utils.To<string>(value), param.Pattern, RegexOptions.IgnoreCase))
                    throw new ScriptRuntimeException("'" + value + "' is an invalid value for "+param.GetDescription(context));

                if (param.Count==CommandLineValueCount.None)
                {
                    context[param.Var] = (param.Unspecified == null)? true : context.Transform(param.Unspecified,param.Transform); 
                    param = null;
                }
                else if ((param.Count == CommandLineValueCount.Single || param.Count == CommandLineValueCount.ForceSingle) && value != null)
                {
                    context[param.Var] = value;
                    param = null;
                }
                else if (param.Count == CommandLineValueCount.Multiple && value != null)
                {
                    var old = context.IsSet(param.Var) ? context[param.Var] : null;
                    var oldArr = old as object[];
                    var arr = new object[((oldArr != null) ? oldArr.Length : 0) + 1];
                    if (oldArr!=null)
                        Array.Copy(oldArr,arr,oldArr.Length);
                    arr[arr.Length - 1] = value;
                    context[param.Var] = arr;
                }
            }
            return param;
        }

        private static void completeSwitch(ScriptContext context,CommandLineParameter currentParameter)
        {
            if (currentParameter == null)
                return;
            if (!context.IsSet(currentParameter.Var))
            {
                if (currentParameter.Count == CommandLineValueCount.Single ||
                    currentParameter.Count == CommandLineValueCount.Multiple ||
                    currentParameter.Count == CommandLineValueCount.ForceSingle)
                {
                    if (currentParameter.Unspecified == null)
                        throw new ScriptRuntimeException(string.Format("No value is set for parameter {0}", currentParameter.Switch));
                    context[currentParameter.Var] = context.Transform(currentParameter.Unspecified, currentParameter.Transform);
                }
                else
                    context[currentParameter.Var] = (currentParameter.Unspecified == null)
                                                        ? true
                                                        : context.Transform(currentParameter.Unspecified,
                                                                            currentParameter.Transform);
            }
        }

        private bool addUnprocessedArg(ScriptContext context,string original, int positionalCount)
        {
            int nPos = 0;
            foreach (CommandLineParameter p in this)
            {
                if (string.IsNullOrEmpty(p.Switch) && !string.IsNullOrEmpty(p.Name))
                {
                    if (nPos == positionalCount || p.Count == CommandLineValueCount.Multiple)
                    {
                        setSwitchValue(context, p, original);
                        if (p.Count==CommandLineValueCount.None)
                            break;
                        return !p.Last;
                    }
                    nPos++;
                }
            }
            
            throw new ScriptRuntimeException("Unexpected argument '" + original + "'");
        }

        #endregion
    }
}