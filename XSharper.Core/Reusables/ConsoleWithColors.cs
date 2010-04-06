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
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace XSharper.Core
{
    #region Console with color output


    /// <summary>
    /// Console with colorful output
    /// </summary>
    public class ConsoleWithColors : TextWriter
    {
        private readonly Dictionary<OutputType, ConsoleColor> _colors = new Dictionary<OutputType, ConsoleColor>();
        private readonly object _lock = new object();
        private TextWriter _log;
        private readonly TextWriter _out = Console.Out;
        private readonly TextWriter _error = Console.Error;

        private OutputType _prevType = OutputType.Null;
        private bool _unevenOutput;
        private string _logFile;
        private bool _useColors = true;
        private bool _debugMode;
        private bool _debugToConsole;


        /// True, if different colors must be used
        public bool UseColors
        {
            get { return _useColors; }
            set { _useColors = value; }
        }

        /// Where to save log
        public string LogFile
        {
            get { return _logFile; }
            set
            {
                lock (_lock)
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (value != _logFile)
                        {
                            _log = new StreamWriter(new FileStream(value, FileMode.Create, FileAccess.Write, FileShare.Read));
                            _logFile = value;
                        }
                    }
                    else
                    {
                        Flush();
                        if (_log != null)
                            _log.Close();
                        _logFile = null;
                        _log = null;
                    }
                }
            }
        }

        /// Constructor
        public ConsoleWithColors()
        {
            Colors[OutputType.Debug] = ConsoleColor.Cyan;
            Colors[OutputType.Out] = ConsoleColor.Gray;
            Colors[OutputType.Bold] = ConsoleColor.White;
            Colors[OutputType.Error] = ConsoleColor.Yellow;
            Colors[OutputType.Info] = ConsoleColor.Green;

            try
            {
                var b = Console.BackgroundColor;
                if (b == ConsoleColor.White)
                {
                    Colors[OutputType.Debug] = ConsoleColor.DarkCyan;
                    Colors[OutputType.Out] = Console.ForegroundColor;
                    Colors[OutputType.Bold] = (Console.ForegroundColor == ConsoleColor.Black) ? ConsoleColor.Blue : ConsoleColor.Black;
                    Colors[OutputType.Error] = ConsoleColor.DarkRed;
                    Colors[OutputType.Info] = ConsoleColor.DarkGreen;
                }
                else if (b != ConsoleColor.Black && b != ConsoleColor.Blue)
                {
                    _useColors = false;
                }
            }
            catch (Exception)
            {
                _useColors = false;
            }
        }

        /// Constructor
        public ConsoleWithColors(string overrideColors) : this()
        {
            string s=overrideColors;
            if (s != null)
            {
                if (s.Trim() == string.Empty)
                    _useColors = false;
                else
                {
                    _useColors = true;
                    foreach (Match c in Regex.Matches(s, @"(?<var>\w+)\s*(=|:)\s*(?<value>\w+)\s*(;|$)"))
                    {
                        try
                        {
                            OutputType ot = (OutputType)Enum.Parse(typeof(OutputType), c.Groups["var"].Value, true);
                            ConsoleColor cc = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), c.Groups["value"].Value, true);
                            Colors[ot] = cc;
                            
                        }
                        catch (Exception)
                        {
                            break;
                        }
                    }
                }
            }
        }

        /// Colors
        public Dictionary<OutputType, ConsoleColor> Colors
        {
            get { return _colors; }
        }

        /// Encoding
        public override Encoding Encoding
        {
            get { return Console.Out.Encoding; }
        }

        /// True if debug mode. If false, debug output is ignored
        public bool DebugMode
        {
            get { return _debugMode; }
            set { _debugMode = value; }
        }

        /// Output debug output to console. Otherwise only to debug viewer
        public bool DebugToConsole
        {
            get
            {
                return _debugToConsole;
            }
            set { _debugToConsole=value; }
        }


        /// Obtains a lifetime service object to control the lifetime policy for this instance.
        public override object InitializeLifetimeService()
        {
            return null;
        }

        /// Releases the unmanaged resources 
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_log != null) _log.Close();
                _log = null;
            }
            base.Dispose(disposing);
        }


        /// <summary>
        /// Writes a character to the text stream.
        /// </summary>
        /// <param name="value">The character to write to the text stream. 
        ///                 </param><exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. 
        ///                 </exception><exception cref="T:System.IO.IOException">An I/O error occurs. 
        ///                 </exception><filterpriority>1</filterpriority>
        public override void Write(char value)
        {
            Write(OutputType.Out, value.ToString());
        }

        /// <summary>
        /// Writes a string to the text stream.
        /// </summary>
        /// <param name="value">The string to write. 
        ///                 </param><exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. 
        ///                 </exception><exception cref="T:System.IO.IOException">An I/O error occurs. 
        ///                 </exception><filterpriority>1</filterpriority>
        public override void Write(string value)
        {
            Write(OutputType.Out, value);
        }

        /// <summary>
        /// Writes a string followed by a line terminator to the text stream.
        /// </summary>
        /// <param name="value">The string to write. If <paramref name="value"/> is null, only the line termination characters are written. 
        ///                 </param><exception cref="T:System.ObjectDisposedException">The <see cref="T:System.IO.TextWriter"/> is closed. 
        ///                 </exception><exception cref="T:System.IO.IOException">An I/O error occurs. 
        ///                 </exception><filterpriority>1</filterpriority>
        public override void WriteLine(string value)
        {
            WriteLine(OutputType.Out, value);
        }

        /// Output event handler
        public void OnOutput(object s, OutputEventArgs e)
        {
            Write(e.OutputType, e.Text);
        }

        /// Progress event handler
        public void OnOutputProgress(object sender, OperationProgressEventArgs e)
        {
            ScriptContext ctx = (ScriptContext)sender;
            if (e.PercentCompleted == 0)
                ctx.WriteLine(OutputType.Debug, "@ " + ctx.CallStack.StackTraceFlat);
        }


        /// Write text followed by new line
        public void WriteLine(OutputType type, string text)
        {
            Write(type, (text ?? string.Empty) + Environment.NewLine);
        }

        /// Write text 
        public void Write(OutputType type, string text)
        {
            if (string.IsNullOrEmpty(text))
                return;
            lock (_lock)
            {
                bool _prevUneven = _unevenOutput;
                if (_unevenOutput &&
                    _prevType != type &&
                    _prevType != OutputType.Null &&
                    !(type == OutputType.Out && _prevType == OutputType.Bold) &&
                    !(type == OutputType.Bold && _prevType == OutputType.Out))
                {
                    outputInternal(_prevType, Environment.NewLine);
                    _unevenOutput = false;
                }
                if (type == OutputType.Debug)
                {
                    const string prefix = "# ";
                    if (_prevUneven)
                        text = Utils.PrefixEachLine("# ", text).Substring(prefix.Length);
                    else
                        text = Utils.PrefixEachLine("# ", text);
                }
                outputInternal(type, text);
                _prevType = type;
                _unevenOutput = !text.EndsWith("\n", StringComparison.Ordinal);
            }
        }

        
        private void outputInternal(OutputType type, string text)
        {
            ConsoleColor cOld = Console.ForegroundColor, cNew;
            if (UseColors && Colors.TryGetValue(type, out cNew))
                Console.ForegroundColor = cNew;
            if (type == OutputType.Error)
            {
                if (_error != null)
                    _error.Write(text);
            }
            else if (type != OutputType.Debug || _debugToConsole)   // Debug only goes to debug output
            {
                if (_out != null)
                    _out.Write(text);
            }
            if (UseColors)
                Console.ForegroundColor = cOld;

            if (_log != null)
            {
                _log.Write(text);
                _log.Flush();
            }
            if (DebugMode)
                if (Debugger.IsLogging())
                    Debugger.Log(0,null,text);
                else
                    OutputDebugString(text);
        }

        [DllImport("kernel32.dll")]
        static extern void OutputDebugString(string lpOutputString);
    }

    #endregion Console with color output
}