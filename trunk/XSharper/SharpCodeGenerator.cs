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
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using XSharper.Core;
using Microsoft.CSharp;
using System.Xml;

namespace XSharper
{
    [Flags]
    public enum GeneratorOptions
    {
        None,
        ForExe=1,
        ForceNet20=2,
        IncludeSource=4,
        CreateMain=8,
        WinExe=16
    }
    public class SharpCodeGenerator
    {
        private string _namespace = "Generated" + Guid.NewGuid().ToString("D").Replace("-", "");
        private string _class = "GeneratedScript";
        public bool CreateMain;
        
        public CSharpCompiler Compiler;


        public SharpCodeGenerator(CSharpCompiler compiler)
        {
            Compiler = compiler;
        }

        public string Namespace
        {
            get { return _namespace; }
            set { _namespace = ToValidName(value); }
        }

        public string Class
        {
            get { return _class; }
            set { _class = ToValidName(value); }
        }

        /// Convert a string to a valid C# name by replacing invalid chars with "_"
        public static string ToValidName(string s)
        {
            var sb = new StringBuilder();
            foreach (var c in s)
            {
                if (char.IsLetterOrDigit(c))
                {
                    if (sb.Length == 0 && char.IsDigit(c))
                        sb.Append("_");
                    sb.Append(c);
                }
                else
                    sb.Append("_");
            }
            return sb.ToString();
        }


        public void Generate(ScriptContext context, TextWriter codeStream, Script script, GeneratorOptions options)
        {
            Compiler.AddHeaders("using XS = " + typeof(Script).Namespace + ";");

            ScriptContext sv = new ScriptContext();
            sv["main"]= ((options & GeneratorOptions.CreateMain) != 0) ? genMain() : string.Empty;
            sv["assembly"] = genAssemblyInfo(context, script, options);
            sv["namespace"] = Namespace;
            sv["class"] = Class;
            sv["src"] = script.Location;
            sv["date"] = DateTime.Now.ToString();
            sv["version"] = context.CoreVersion.ToString();
            sv["script"] = Compiler.GetTypeName(script.GetType());
            sv["callIsolation"] = Compiler.GetTypeName(typeof (CallIsolation));
            sv["iscriptaction"] = Compiler.GetTypeName(typeof (IScriptAction));
            sv["usings"] = Compiler.GenerateFileHeader();
            sv["context"] = Compiler.GetTypeName(context.GetType());
            using (StringWriter swCode = new StringWriter())
            using (StringWriter swSnippets = new StringWriter())
            using (new ScriptContextScope(context))
            {
                Generator c = new Generator(Compiler, swCode, swSnippets, options);
                c.GenerateObjectCode(script, "_script", 3);
                sv["snippets-code"] = swSnippets.ToString();
                sv["script-code"] = swCode.ToString();
            }
            using (MemoryStream ms = AppDomainLoader.TryLoadResourceStream("XSharper.Embedded.Source.SourceTemplate"))
            using (var sr = new StreamReader(ms))
            codeStream.Write(sv.ExpandStr(sr.ReadToEnd()));
        }

        private string genAssemblyInfo(ScriptContext context, Script script, GeneratorOptions options)
        {
            if ((options & GeneratorOptions.CreateMain) == 0)
                return string.Empty;
            var d = new Dictionary<string, string>()
                        {
                            {"[assembly: System.Reflection.AssemblyTitle('{}')]", script.VersionInfo.Title},
                            {"[assembly: System.Reflection.AssemblyDescription('{}')]", script.VersionInfo.Value},
                            {"[assembly: System.Reflection.AssemblyCompany('{}')]", script.VersionInfo.Company},
                            {"[assembly: System.Reflection.AssemblyProduct('{}')]", script.VersionInfo.Product},
                            {"[assembly: System.Reflection.AssemblyCopyright('{}')]", script.VersionInfo.Copyright},
                            {"[assembly: System.Reflection.AssemblyVersion('{}')]", script.VersionInfo.Version},
                            {"[assembly: System.Reflection.AssemblyFileVersion('{}')]", script.VersionInfo.Version}
                        };
            StringBuilder sb=new StringBuilder();
            
            foreach (var pair in d)
            {
                var tr = context.TransformStr(pair.Value, script.VersionInfo.Transform | TransformRules.Trim | TransformRules.DoubleDoubleQuotes);
                if (!string.IsNullOrEmpty(tr))
                    sb.AppendLine(pair.Key.Replace("'{}'", "@\"" + tr + "\""));
            }
          
            if (sb.Length==0)
                return string.Empty;

            ScriptContext sv=new ScriptContext();
            sv["guid"] = ToValidName(Guid.NewGuid().ToString());
            sv["code"] = sb.ToString();

            string s = @"#region -- Assembly attributes --
${code}
// C# compiler bug may treat assembly attributes as applying to namespace
class C${guid} {}
#endregion";
            return sv.ExpandStr(s);
                
        }

        private StringBuilder extractContents(string streamName)
        {
            // formatted C# source code is easy to parse. Cut everything that is not using & namespace
            StringBuilder appDomainLoader = new StringBuilder();
            using (MemoryStream ms = AppDomainLoader.TryLoadResourceStream(streamName))
            using (var sr = new StreamReader(ms))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                    if (line.Length == 0 || char.IsWhiteSpace(line[0]))
                        appDomainLoader.AppendLine(line);
                    else if (line.StartsWith("using", StringComparison.Ordinal))
                        Compiler.AddHeaders(line);
            }
            return appDomainLoader;
        }

        private string genMain()
        {
            StringBuilder appDomainLoader = extractContents("XSharper.Embedded.Source.AppDomainLoader");
            
            // Make it private
            appDomainLoader.Replace("public class", "class");

            // Read main program
            ScriptContext ctx=new ScriptContext();
            ctx["GENERATED_CLASS_PROGRAM"] = Class + "Program";
            ctx["GENERATED_CLASS"] = Class;
            ctx["GENERATED_NAMESPACE"] = Namespace;
            ctx["INSERT_APPDOMAIN_LOADER"] = appDomainLoader.ToString();
            return ctx.ExpandStr(extractContents("XSharper.Embedded.Source.GeneratedProgram"));
        }

        class Generator
        {
            private readonly CSharpCompiler _re;
            private TextWriter MainCode;
            private TextWriter SnippetsCode;
            private GeneratorOptions _options;
            private Dictionary<string, bool> _generated = new Dictionary<string, bool>();
            private int varCnt = 0;

            private string getNextVar(object o)
            {
                return " v" + varCnt++;
            }
            public Generator(CSharpCompiler re, TextWriter codeWriter, TextWriter snippetWriter, GeneratorOptions options)
            {
                _re = re;
                MainCode = codeWriter;
                SnippetsCode = snippetWriter;
                _options = options;
            }

            private bool writeSimple(object o)
            {
                if (ReferenceEquals(o, null))
                {
                    MainCode.Write("null");
                    return true;
                }

                Type t = o.GetType();
                if (t.IsPrimitive || t==typeof(decimal))
                {
                    if (o is bool) MainCode.Write(((bool)o) ? "true" : "false");
                    else if (o is float) MainCode.Write(((float)o).ToString(CultureInfo.InvariantCulture) + "f");
                    else if (o is double) MainCode.Write(((double)o).ToString(CultureInfo.InvariantCulture) + "d");
                    else if (o is long) MainCode.Write(((int)o).ToString(CultureInfo.InvariantCulture)+"l");
                    else if (o is ulong) MainCode.Write(((int)o).ToString(CultureInfo.InvariantCulture) + "l");
                    else if (o is int) MainCode.Write(((int)o).ToString(CultureInfo.InvariantCulture));
                    else if (o is uint) MainCode.Write(((int)o).ToString(CultureInfo.InvariantCulture)+"u");
                    else if (o is decimal) MainCode.Write(((int)o).ToString(CultureInfo.InvariantCulture) + "m");
                    else if (o is char) MainCode.Write("'"+((char)o).ToString(CultureInfo.InvariantCulture)+"'");
                    else throw new InvalidOperationException("Not supported primitive type");
                    return true;
                }
                if (t.IsEnum)
                {
                    bool first=true;
                    foreach (string s in o.ToString().Split(','))
                    {
                        if (!first) MainCode.Write(" | ");
                        first = false;
                        MainCode.Write(_re.GetTypeName(t) + "." + s.Trim());
                    }
                    return true;
                }
                if (t == typeof(string))
                {
                    MainCode.Write("@\"");
                    MainCode.Write(Utils.TransformStr(o.ToString(), TransformRules.NewLineToCRLF | TransformRules.DoubleDoubleQuotes));
                    MainCode.Write("\"");
                    return true;
                }
                return false;
            }
            public void GenerateObjectCode(object o, string prevVarName, int level)
            {
                if (writeSimple(o))
                    return;

                Type t = o.GetType();
                if (CustomAttributeHelper.Has<CodegenAsXmlAttribute>(t))
                {
                    StringWriter xml = new StringWriter();

                    using (XmlTextWriter tw = new XmlTextWriter(xml))
                        ((IXsElement)o).WriteXml(tw, null);

                    MainCode.Write("new " + _re.GetTypeName(t) + "(@\"" + xml.ToString().Replace("\"", "\"\"") + "\")");
                    return;
                }

                // On the fly replace Code with CompiledCode 
                Code originalCode = null;
                if (t == typeof(Code))
                {
                    if (((Code)o).Dynamic == false)
                    {
                        originalCode = (Code) o;
                        string className = originalCode.GetClassName();
                        if (!_generated.ContainsKey(className))
                        {
                            SnippetsCode.WriteLine();
                            SnippetsCode.WriteLine(originalCode.GenerateSourceCode(ScriptContextScope.Current, (_options & GeneratorOptions.IncludeSource) != 0, false));
                            SnippetsCode.WriteLine();
                            _generated[className] = true;
                        }
                        o = new CompiledCode(originalCode);
                    }
                }
                if (_re.MaxAvailableNETVersion >= new Version(3, 5) && (_options & GeneratorOptions.ForceNet20)==0)
                    graphSaveNET35(o, originalCode, level);
                else
                    graphSaveNET20(o, prevVarName, originalCode, level);

            }

            private void graphSaveNET20(object o, string prevVarName, Code originalCode, int level)
            {
                Type t = o.GetType();
                string indent = new string('\t', level);

                if (o is IEnumerable && (CustomAttributeHelper.All<XsTypeAttribute>(o.GetType()).Length==0))
                {
                    if (t.GetGenericArguments().Length==0)
                        MainCode.Write("new "+_re.GetTypeName(t)+"()");
                    else
                    {
                        string bestType = _re.GetTypeName(t.GetGenericArguments()[0]);
                        MainCode.Write("new List<" + bestType + ">()");
                    }

                    foreach (var obj in (o as IEnumerable))
                    {
                        MainCode.WriteLine(";");
                        MainCode.Write(indent);
                        string v = getNextVar(obj);
                        if (obj is Code && ((Code)obj).Dynamic==false)
                            MainCode.Write(_re.GetTypeName(typeof(CompiledCode)));
                        else
                            MainCode.Write(_re.GetTypeName(obj.GetType()));

                        MainCode.Write(" " + v + " = ");
                        GenerateObjectCode(obj, v, level + 1);
                        MainCode.WriteLine(";");
                        MainCode.Write(indent + prevVarName + ".Add(" + v + ")");
                    }
                }
                else
                {
                    newClass(o);
                    foreach (var pi in getPropertiesToSave(o))
                    {
                        MainCode.WriteLine(";");
                        MainCode.Write(indent);
            
                        if (!pi.PropertyType.IsClass || pi.PropertyType == typeof(string) || pi.PropertyType == typeof(ExecutableScriptBase))
                        {
                            MainCode.Write(prevVarName + "." + pi.Name + " = ");
                            if (pi.PropertyType == typeof(ExecutableScriptBase))
                                MainCode.Write("new " + originalCode.GetClassName() + "()");
                            else
                            {
                                object propValue = pi.GetValue(o, null);
                                writeSimple(propValue);
                            }
                        }
                        else
                        {
                            object propValue = pi.GetValue(o, null);
                            if (propValue != null)
                            {
                                string v = getNextVar(propValue);
                                MainCode.Write(_re.GetTypeName(propValue.GetType()) + " " + v + " = ");
                                GenerateObjectCode(propValue, v, level + 1);
                                MainCode.WriteLine(";");
                                MainCode.Write(indent + prevVarName + "." + pi.Name + " = " + v);
                            }
                        }
                    }
                }

            }

            private void newClass(object o)
            {
                Type t = o.GetType();
                MainCode.Write("new " + _re.GetTypeName(t) + "(");
                if (t == typeof(Script))
                    MainCode.Write("(System.Diagnostics.Process.GetCurrentProcess().MainModule!=null)?System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName:null   ");
                MainCode.Write(")");
            }

            private static PropertyInfo[] getPropertiesToSave(object o)
            {
                List<PropertyInfo> ret = new List<PropertyInfo>();
                Type t = o.GetType();
                var def = Utils.CreateInstance(t);
                foreach (var pi in t.GetProperties(BindingFlags.Public | BindingFlags.Default | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
                {
                    if (pi.PropertyType != typeof(ExecutableScriptBase))
                    {
                        if (!CustomAttributeHelper.Has<XsElementAttribute>(pi) && XsAttributeAttribute.GetNames(pi,false) == null)
                            continue;

                        object propValue = pi.GetValue(o, null);
                        var ea = CustomAttributeHelper.First<XsElementAttribute>(pi);
                        if (ea != null && ea.SkipIfEmpty && ea.IsEmpty(propValue))
                            continue;

                        // References are supposed to be resolved during initialization, they're not included into executable
                        if (pi.Name == "From" && o is Reference && !((Reference) o).Dynamic)
                            continue;

                        if (propValue == null || propValue.Equals(pi.GetValue(def, null)))
                        {
                            if (pi.PropertyType != typeof (ExecutableScriptBase))
                                continue;
                        }
                    }
                    ret.Add(pi);
                }
                return ret.ToArray();
            }

            private void graphSaveNET35(object o, Code originalCode, int level)
            {
                bool first;
                Type t = o.GetType();
                string indent = new string('\t', level);
                string indent1 = new string('\t', level + 1);

                if (o is IEnumerable && (CustomAttributeHelper.All<XsTypeAttribute>(o.GetType()).Length == 0))
                {
                    first = true;
                    MainCode.Write("new " + _re.GetTypeName(t)  );
                    foreach (var obj in (o as IEnumerable))
                    {
                        MainCode.WriteLine(first ? " {" : ",");
                        MainCode.Write(indent1);
                        first = false;
                        GenerateObjectCode(obj, null, level + 1);
                    }
                    if (!first)
                    {
                        MainCode.WriteLine();
                        MainCode.Write(indent);
                        MainCode.Write("}");
                    }
                    else
                    {
                        MainCode.Write("()");
                    }
                    MainCode.Write(" ");
                }
                else
                {
                    newClass(o);

                    first = true;
                    foreach (var pi in getPropertiesToSave(o))
                    {
                        object propValue = pi.GetValue(o, null);
                        
                        MainCode.WriteLine(first ? "{" : ",");
                        MainCode.Write(indent1);
                        first = false;
                        MainCode.Write(pi.Name + " = ");

                        if (pi.PropertyType == typeof(ExecutableScriptBase))
                            MainCode.Write("new " + originalCode.GetClassName() + "()");
                        else
                            GenerateObjectCode(propValue, null, level + 1);
                    }
                    if (!first)
                    {
                        MainCode.WriteLine();
                        MainCode.Write(indent);
                        MainCode.Write("}");
                    }
                }
            }
        }
    }


}