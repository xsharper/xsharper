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
using System.Reflection;
using System.Text;
using XSharper.Core;
using System.Text.RegularExpressions;

namespace XSharper
{
    
    static class HelpHelper
    {
        public static string GetLogo(ScriptContext context)
        {
            var titleAttr=(AssemblyTitleAttribute[])Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute),true);
            var cpr = (AssemblyCopyrightAttribute[])Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), true);
            var cn=(AssemblyCompanyAttribute[])Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), true);
            string tt = (titleAttr.Length>0)?titleAttr[0].Title:"XSharper";

            return tt + " v." + context.CoreVersion + " "+
                ((cn.Length > 0) ? cn[0].Company : "DeltaX Inc.") +" "+
                ((cpr.Length > 0) ? cpr[0].Copyright : "(c) 2006-2010");   
        }
        public static int Help(ScriptContext context, UsageGenerator usage, CommandLineParameters xsParams)
        {
            context.WriteLine(OutputType.Bold, GetLogo(context));
            context.WriteLine();

            var command = context.GetStr(xs.help, null);
            if (!string.IsNullOrEmpty(command))
            {
                var tt = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
                tt["param"] = typeof (CommandLineParameter);
                tt["versionInfo"] = typeof(VersionInfo);
                tt["usage"] = typeof(UsageGenerator);

                foreach (var s in context.GetKnownTypes())
                {
                    foreach (var at in CustomAttributeHelper.All<XsTypeAttribute>(s))
                        if (!string.IsNullOrEmpty(at.Name))
                            tt[at.Name] = s;
                }

                Type type;
                if (tt.TryGetValue(command,out type))
                {

                    writeCommandHelp(context, type, usage.CorrectWidth(-1));
                    return -2;
                }
                if (command == "*")
                {
                    List<Var> v = new List<Var>();
                    v.Add(new Var("param",getDescr(typeof(CommandLineParameter))));
                    v.Add(new Var("versioninfo", getDescr(typeof(VersionInfo))));
                    v.Add(new Var("usage", getDescr(typeof(UsageGenerator))));


                    foreach (var s in context.GetKnownTypes())
                    {
                        var xst = CustomAttributeHelper.First<XsTypeAttribute>(s);
                        if (xst == null || string.IsNullOrEmpty(xst.Name))
                            continue;
                        v.Add(new Var(xst.Name, getDescr(s)));
                    }
                    v.Sort((a, b) => string.Compare(a.Name, b.Name));
                    v.Insert(0, new Var("Actions:", null));
                    v.Insert(1, new Var("", null));
                    Utils.WrapTwoColumns(context.Out, v, 30, usage.CorrectWidth(-1));
                    return -2;
                }
                if (command.StartsWith(".", StringComparison.Ordinal))
                {
                    bool success = false;
                    foreach (var nn in ((IEvaluationContext)context).GetNonameObjects())
                    {
                        success =writeTypeHelp(context, nn.Type, new StringFilter(command.Substring(1)),usage.CorrectWidth(-1)) || success;
                    }
                    if (!success)
                        context.Error.WriteLine("Cannot find method '" + command + "'. ");
                    return -2;
                }
                
                Type t = context.FindType(command);
                if (t == null)
                    t = context.FindType("XS." + command);
                if (t != null)
                    writeTypeHelp(context, t, null, usage.CorrectWidth(-1));
                else if (command.Contains("?") || command.Contains("*"))
                {
                    var r = Utils.WildcardToRegex(command, RegexOptions.IgnoreCase);
                    foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                        foreach (var ttt in a.GetTypes())
                            if (ttt != null && ttt.IsPublic && (r.IsMatch(ttt.Name) || r.IsMatch(Dump.GetFriendlyTypeName(ttt))))
                            {
                                context.WriteLine(Dump.GetFriendlyTypeName(ttt,true));
                            }
                }
                else
                    context.Error.WriteLine("Cannot find command or type '" + command + "'. Use //help * to display the list of commands");
            }
            else
                context.WriteLine(usage.GetUsage(context, null, null, -1, xsParams));
            return -2;
        }

        private static string getDescr(Type type)
        {
            var desc = (DescriptionAttribute[])type.GetCustomAttributes(typeof(DescriptionAttribute), false);
            string desct = (desc.Length == 0) ? type.FullName : desc[0].Description;
            if (type.Assembly != typeof(Print).Assembly)
                desct = "* " + desct;
            return desct;
        }

        class EleInfo
        {
            public XsElementAttribute Attr;
            public string Description;
            public PropertyInfo Property;
        }

        private static void writeHeader(ScriptContext context, Type s)
        {
            if (s.IsClass)
                context.Bold.Write("class ");
            else if (s.IsInterface)
                context.Bold.Write("interface ");
            else if (s.IsEnum)
                context.Bold.Write("enum ");
            else if (!s.IsPrimitive)
                context.Bold.Write("struct ");
            context.Bold.WriteLine(s.Name + " (" + s.FullName + ")");
            context.WriteLine();

        }
        private static bool writeTypeHelp(ScriptContext context, Type s, IStringFilter sf, int width)
        {
            bool hdr = false;
            List<Var> v = null;
            if (sf == null)
            {
                v = getConstructorsHelp(s);
                if (v.Count > 0)
                {
                    if (!hdr) writeHeader(context, s); hdr = true;
                    context.Bold.WriteLine("Public constructors:");
                    context.Write(Utils.WrapTwoColumns(v, 40, width));
                    context.WriteLine();
                }
            }

            v = getPropertiesHelp(s, sf, true);
            if (v.Count > 0)
            {
                if (!hdr) writeHeader(context, s); hdr = true;
                context.Bold.WriteLine("Public properties:");
                context.Write(Utils.WrapTwoColumns(v, 40, width));
                context.WriteLine();
            }
            v = getPropertiesHelp(s, sf, false);
            if (v.Count > 0)
            {
                if (!hdr) writeHeader(context, s); hdr = true;
                if (s.IsEnum)
                    context.Bold.WriteLine("Enum values:");
                else
                    context.Bold.WriteLine("Public fields:");
                context.Write(Utils.WrapTwoColumns(v, 40, width));
                context.WriteLine();
            }

            

            v = getMethodsHelp(s,sf);
            if (v.Count > 0)
            {
                if (!hdr) writeHeader(context, s); hdr = true;
                context.Bold.WriteLine("Public members:");
                context.Write(Utils.WrapTwoColumns(v, 40, width));
                context.WriteLine();
            }
            return hdr;
        }

        private static void writeCommandHelp(ScriptContext context, Type commandType, int width)
        {
            var desc = (DescriptionAttribute[])commandType.GetCustomAttributes(typeof(DescriptionAttribute),false);
            XsTypeAttribute xst = ((XsTypeAttribute[])commandType.GetCustomAttributes(typeof(XsTypeAttribute), false))[0];
            string desct = (desc.Length==0) ? commandType.FullName : desc[0].Description;

            context.Bold.WriteLine(xst.Name + " (" + commandType.FullName + ")");
            context.WriteLine(Utils.Wrap(desct, width, string.Empty));
            context.WriteLine();

            bool hasValue = false;
            string hasValueName = null;
            var def = Utils.CreateInstance(commandType);
            for (int required = 1; required >= 0; required--)
            {
                List<Var> v = new List<Var>();


                foreach (var p in commandType.GetProperties(BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.Instance))
                {
                    var aa = XsAttributeAttribute.GetNames(p,false);
                    if (aa == null)
                        continue;

                    // If more than 1 are returned, choose the preferred one
                    string ba = null;
                    var hs = false;
                    foreach (var attribute in aa)
                    {
                        if (attribute.Length > 0 && ba==null)
                            ba = attribute;
                        if (attribute.Length == 0)
                        {
                            hasValue = true;
                            hs = true;
                        }
                    }
                    if (ba == null)
                        ba = aa[0];
                    if (hasValue && hs)
                        hasValueName = ba;
                    if (CustomAttributeHelper.Has<XsRequiredAttribute>(p) != (required == 1))
                        continue;

                    var type = p.PropertyType;
                    string dv = null;
                    string typeName;
                    StringBuilder d = new StringBuilder();
                    var dd = CustomAttributeHelper.First<DescriptionAttribute>(p);
                    if (dd != null)
                        d.Append(dd.Description);

                    if (type == typeof(bool))
                    {
                        typeName = "bool";
                        dv = "false";
                    }
                    else if (type == typeof(int))
                    {
                        typeName = "int";
                        dv = "0";
                    }
                    else if (type == typeof(object))
                    {
                        typeName = CustomAttributeHelper.Has<XsNotTransformed>(p)?"object":"object*";
                        dv = "null";
                    }
                    else if (type == typeof(string))
                    {
                        typeName = CustomAttributeHelper.Has<XsNotTransformed>(p) ? "string" : "string*";
                        dv = "null";
                    }
                    else if (type.IsEnum)
                    {
                        typeName = Dump.GetFriendlyTypeName(type);
                        bool f = true;
                        if (d.Length > 0)
                            d.Append(". ");
                        d.Append("Values: ");
                        foreach (var s1 in Enum.GetNames(type))
                        {
                            if (!f)
                                d.Append(CustomAttributeHelper.Has<FlagsAttribute>(type) ? " | " : " / ");
                            f = false;
                            d.Append(Utils.LowercaseFirstLetter(s1));
                        }
                    }
                    else
                        typeName = Dump.GetFriendlyTypeName(type);

                    string vn = ba.PadRight(20, ' ');
                    vn += typeName.PadRight(10, ' ');

                    if (required == 0)
                    {
                        if (d.Length > 0)
                            d.Append(" ");
                        d.Append("(default: ");
                        var ddd = p.GetValue(def, null);
                        if (type == typeof(string) && (ddd != null || dv != "null"))
                            d.Append("'" + (ddd ?? dv) + "'");
                        else if (type.IsEnum)
                            d.Append(Utils.LowercaseFirstLetter((ddd ?? dv).ToString()));
                        else
                            d.Append(ddd ?? dv);
                        d.Append(")");
                    }
                    v.Add(new Var("  " + vn, d.ToString()));
                }
                v.Sort((a, b) => string.Compare(a.Name, b.Name));
                if (v.Count == 0)
                    continue;

                context.Bold.WriteLine((required == 1) ? "Required attributes:" : "Optional attributes:");

                Utils.WrapTwoColumns(context.Out, v, 35, width);
                context.Out.WriteLine();

            }

            //z1.Sort((a, b) => string.Compare(a.Name, b.Name));
            //UsageGenerator ug = new UsageGenerator();
            //ug.Options = UsageOptions.AutoSuffix;
            //context.WriteLine(ug.GetUsage(context, null, null, -1, z1));
            context.Bold.WriteLine("Syntax:");
            context.Bold.Write("  <" + xst.Name);
            context.Write(" ... attributes ... ");
            context.Bold.WriteLine("  >");

            List<EleInfo> att = new List<EleInfo>();
            foreach (var p in commandType.GetProperties(BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.SetProperty | BindingFlags.Instance))
            {
                var aa = CustomAttributeHelper.First<XsElementAttribute>(p);
                if (aa == null)
                    continue;
                var d = CustomAttributeHelper.First<DescriptionAttribute>(p);
                att.Add(new EleInfo() { Attr = aa, Description = (d == null) ? null : d.Description, Property = p });
            }
            att.Sort((a, b) => (a.Attr.Ordering - b.Attr.Ordering) * 10000 + a.Attr.Name.CompareTo(b.Attr.Name));

            foreach (var aa in att)
            {

                string pref = string.Empty;
                if (!string.IsNullOrEmpty(aa.Attr.Name))
                {
                    if (aa.Attr.SkipIfEmpty)
                        context.Bold.WriteLine("    [<" + aa.Attr.Name + ">");
                    else
                        context.Bold.WriteLine("    <" + aa.Attr.Name + ">");
                    pref = "  ";
                    if (aa.Description != null)
                        context.Info.Write(Utils.Wrap(pref + "    " + aa.Description, width, pref + "    "));
                }

                if (!string.IsNullOrEmpty(aa.Attr.CollectionItemElementName))
                {
                    for (int i = 0; i < 2; ++i)
                    {
                        context.Bold.Write(pref + "    ");
                        if (aa.Attr.SkipIfEmpty)
                            context.Bold.Write("[");
                        context.Bold.Write("<" + aa.Attr.CollectionItemElementName + " ");
                        var xs = CustomAttributeHelper.First<XsTypeAttribute>(aa.Attr.CollectionItemType);
                        var hv = false;
                        if (xs != null && string.IsNullOrEmpty(xs.Name))
                        {
                            // Write attributes here
                            foreach (var pp in aa.Attr.CollectionItemType.GetProperties(BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.SetProperty | BindingFlags.Instance))
                            {
                                var tt = XsAttributeAttribute.GetNames(pp,false);
                                if (tt==null)
                                    continue;

                                var bba = tt[0];
                                foreach (var attribute in tt)
                                {
                                    if (attribute==tt[0] && attribute.Length > 0)
                                        bba = attribute;
                                    if (attribute.Length == 0)
                                        hv = true;
                                }
                                if (bba.Length>0)
                                    context.Write(bba + "=\"...\" ");
                            }
                        }
                        if (hv)
                        {
                            context.Bold.Write(">");
                            context.Write("..value..");
                            context.Bold.Write("</" + aa.Attr.CollectionItemElementName + ">");
                        }
                        else
                            context.Bold.Write(" />");
                        if (aa.Attr.SkipIfEmpty)
                            context.Bold.Write("]");
                        context.Bold.WriteLine();

                    }
                }
                else if (aa.Attr.CollectionItemType != null)
                {
                    var name = "action";
                    if (aa.Attr.CollectionItemType != typeof(IScriptAction))
                        name = "<" + aa.Attr.CollectionItemType.Name + " />";
                    context.WriteLine(pref + "    " + name);
                    context.WriteLine(pref + "    " + name);
                    context.WriteLine(pref + "    ...");

                }

                if (!string.IsNullOrEmpty(aa.Attr.Name))
                {
                    if (aa.Attr.SkipIfEmpty)
                        context.Bold.WriteLine("    </" + aa.Attr.Name + ">]");
                    else
                        context.Bold.WriteLine("    </" + aa.Attr.Name + ">");
                }
            }
            if (hasValue)
                context.WriteLine("    value (see " + hasValueName + " attribute)");
            context.Bold.WriteLine("  </" + xst.Name + ">");


        }

        private static List<Var> getPropertiesHelp(Type type, IStringFilter sf, bool isProperties)
        {
            var v = new List<Var>();
            for (int stat = 0; stat < 2; ++stat)
            {
                if (type.IsEnum && stat==0)
                    continue;
                
                
                List<MemberInfo> mi = new List<MemberInfo>();
                if (isProperties)
                {
                    BindingFlags bf = BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty;
                    bf |= ((stat == 1) ? BindingFlags.Static : BindingFlags.Instance);
                    foreach (var p in type.GetProperties(bf))
                        mi.Add(p);
                }
                else
                {
                    BindingFlags bf = BindingFlags.Public | BindingFlags.GetField | BindingFlags.SetProperty;
                    bf |= ((stat == 1) ? BindingFlags.Static : BindingFlags.Instance);
                    foreach (var p in type.GetFields(bf))
                        mi.Add(p);
                }
                if (!type.IsEnum)
                    mi.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
                foreach (var info in mi)
                {
                    if (sf != null && !sf.IsMatch(info.Name))
                        continue;
                    var pinfo = info as PropertyInfo;
                    var finfo = info as FieldInfo;
                    StringBuilder sb1 = new StringBuilder(), sb = new StringBuilder();
                    sb1.Append("  ");
                    if (!type.IsEnum)
                    {

                        if (stat == 1)
                            sb1.Append("static ");
                        sb1.Append(Dump.GetFriendlyTypeName((finfo != null) ? finfo.FieldType : pinfo.PropertyType));
                        sb.Append(info.Name);
                    }
                    else
                    {
                        sb1.Append(finfo.Name);
                        sb.AppendFormat("0x{0:x10} ",Convert.ToUInt64( finfo.GetValue(null)));
                        var dd = (DescriptionAttribute[])finfo.GetCustomAttributes(typeof(DescriptionAttribute),true);
                        if (dd != null && dd.Length>0)
                            sb.Append(dd[0].Description.Trim());
                    }


                    bool first = true;
                    if (pinfo != null)
                    {
                        foreach (var parameter in pinfo.GetIndexParameters())
                        {
                            if (!first)
                                sb.Append(", ");
                            else
                                sb.Append("[");
                            first = false;

                            if (parameter.IsOut)
                                sb.Append("out ");

                            if (parameter.GetCustomAttributes(typeof (ParamArrayAttribute), false).Length > 0)
                                sb.Append("params ");

                            sb.Append(Dump.GetFriendlyTypeName(parameter.ParameterType));
                            sb.Append(" ");
                            sb.Append(parameter.Name);
                        }

                        if (!first)
                            sb.Append("]");
                        bool gt = pinfo.GetGetMethod() != null;
                        bool st = pinfo.GetSetMethod() != null;
                        if (gt && st)
                            sb.Append(" { get; set;}");
                        else if (!gt && st)
                            sb.Append(" { set; }");
                        else if (gt && !st)
                            sb.Append(" { get; }");
                    }

                    v.Add(new Var(sb1.ToString(), sb.ToString()));
                }
            }
            return v;
        }
        private static List<Var> getConstructorsHelp(Type type)
        {
            var v = new List<Var>();
            for (int stat = 0; stat < 2; ++stat)
            {
                List<ConstructorInfo> mi = new List<ConstructorInfo>();
                var flags = BindingFlags.Public | BindingFlags.InvokeMethod | (stat == 0 ? BindingFlags.Instance : BindingFlags.Static);
                
                foreach (var p in type.GetConstructors(flags))
                {
                    mi.Add(p);
                }
                
                foreach (var info in mi)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(" ");
                    if (info.IsStatic)
                        sb.Append("static ");
                    sb.Append(type.Name);
                    sb.Append("(");
                    bool first = true;
                    foreach (var parameter in info.GetParameters())
                    {
                        if (!first)
                            sb.Append(", ");
                        first = false;

                        if (parameter.IsOut)
                            sb.Append("out ");

                        if (parameter.GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0)
                            sb.Append("params ");

                        sb.Append(Dump.GetFriendlyTypeName(parameter.ParameterType));
                           sb.Append(" ");
                        sb.Append(parameter.Name);
                    }
                    sb.Append(")");
                    v.Add(new Var(string.Empty, sb.ToString()));
                }
            }
            return v;
        }
        private static List<Var> getMethodsHelp(Type type, IStringFilter sf)
        {
            var v = new List<Var>();
            for (int stat = 0; stat < 2; ++stat)
            {
                List<MethodInfo> mi = new List<MethodInfo>();
                var flags = BindingFlags.Public | BindingFlags.InvokeMethod | (stat == 0 ? BindingFlags.Instance : BindingFlags.Static);
                
                foreach (var p in type.GetMethods(flags))
                {
                    if (p.Name.StartsWith("get_", StringComparison.OrdinalIgnoreCase) || p.Name.StartsWith("set_", StringComparison.OrdinalIgnoreCase) || p.Name.StartsWith("CreateObjRef",StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (sf != null && !sf.IsMatch(p.Name))
                        continue;
                    mi.Add(p);
                }
                mi.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

                foreach (var info in mi)
                {
                    StringBuilder sb1 = new StringBuilder(), sb = new StringBuilder();
                    sb1.Append("  ");
                    if (info.IsStatic)
                        sb1.Append("static ");
                    if (info.IsAbstract)
                        sb1.Append("abstract ");
                    sb1.Append(Dump.GetFriendlyTypeName(info.ReturnType));

                    sb.Append(info.Name);
                    var ge = info.GetGenericArguments();
                    if (ge!=null && ge.Length>0)
                    {
                        sb.Append("<");
                        for (int i = 0; i < ge.Length; ++i)
                        {
                            sb.Append(Dump.GetFriendlyTypeName(ge[i]));
                            if (i != ge.Length - 1)
                                sb.Append(", ");
                        }
                        sb.Append(">");
                    }
                    sb.Append("(");
                    bool first = true;
                    foreach (var parameter in info.GetParameters())
                    {
                        if (!first)
                            sb.Append(", ");
                        first = false;

                        if (parameter.IsOut)
                            sb.Append("out ");

                        if (parameter.GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0)
                            sb.Append("params ");

                        sb.Append(Dump.GetFriendlyTypeName(parameter.ParameterType));
                        sb.Append(" ");
                        sb.Append(parameter.Name);
                    }
                    sb.Append(")");
                    v.Add(new Var(sb1.ToString(), sb.ToString()));
                }
            }
            return v;
        }
    }

 
}