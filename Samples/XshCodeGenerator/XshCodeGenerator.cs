using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using GuidAttribute=System.Runtime.InteropServices.GuidAttribute;
using System.Diagnostics;
using IServiceProvider=Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using System.Xml;

namespace XshCodeGen
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CustomToolAttribute : Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    [ComVisible(true)]
    [Guid("32317A71-1208-433d-B3F5-9BF0D3E93F58")]
    [CustomTool(Name="XshCodeGenerator", Description = "Generate C# file from XSharper script.")]
    public class XshCodeGenerator : IVsSingleFileGenerator
    {
        private object site;
        private ServiceProvider serviceProvider = null;

        private ServiceProvider SiteServiceProvider
        {
            get
            {
                if (serviceProvider == null)
                {
                    IServiceProvider oleServiceProvider = site as IServiceProvider;
                    serviceProvider = new ServiceProvider(oleServiceProvider);
                }
                return serviceProvider;
            }
        }

        internal static Guid CSharpCategoryGuid = new Guid("FAE04EC1-301F-11D3-BF4B-00C04F79EFBC");
        private const string VisualStudioVersion = "9.0";

        [ComRegisterFunction]
        public static void RegisterClass(Type t)
        {
            Type attributeType = typeof(GuidAttribute);
            object[] attributes = t.GetCustomAttributes(attributeType, /* inherit */ true);
            if (attributes.Length == 0)
                throw new Exception(
                    String.Format("Class '{0}' does not provide a '{1}' attribute.",
                                  t.FullName, attributeType.FullName));
            GuidAttribute guidAttribute = (GuidAttribute)((Attribute)attributes[0]);
            CustomToolAttribute customToolAttribute = getCustomToolAttribute(t);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(
              GetKeyName(CSharpCategoryGuid, customToolAttribute.Name)))
            {
                key.SetValue("", customToolAttribute.Description);
                key.SetValue("CLSID", "{" + guidAttribute.Value + "}");
                key.SetValue("GeneratesDesignTimeSource", 1);
            }
        }

        [ComUnregisterFunction]
        public static void UnregisterClass(Type t)
        {
            CustomToolAttribute customToolAttribute = getCustomToolAttribute(t);
            Registry.LocalMachine.DeleteSubKey(GetKeyName(
              CSharpCategoryGuid, customToolAttribute.Name), false);
        }

        internal static CustomToolAttribute getCustomToolAttribute(Type t)
        {
            Type attributeType = typeof(CustomToolAttribute);
            object[] attributes = t.GetCustomAttributes(attributeType, /* inherit */ true);
            if (attributes.Length == 0)
                throw new Exception(
                    String.Format("Class '{0}' does not provide a '{1}' attribute.",
                                  t.FullName, attributeType.FullName));
            return (CustomToolAttribute)(attributes[0]);
        }

        internal static string GetKeyName(Guid categoryGuid, string toolName)
        {
            return
              String.Format("SOFTWARE\\Microsoft\\VisualStudio\\" + VisualStudioVersion +
                "\\Generators\\{{{0}}}\\{1}\\", categoryGuid, toolName);
        }

        #region IVsSingleFileGenerator Members

        public int DefaultExtension(out string pbstrDefaultExtension)
        {
            pbstrDefaultExtension = ".generated.cs";
            return pbstrDefaultExtension.Length;
        }

        public int Generate(string wszInputFilePath, string bstrInputFileContents,
          string wszDefaultNamespace, IntPtr[] rgbOutputFileContents,
          out uint pcbOutput, IVsGeneratorProgress pGenerateProgress)
        {
            pcbOutput = 0;
            string sFile = Path.GetTempFileName();
            try
            {
                StringBuilder cmdLine=new StringBuilder();
                cmdLine.Append(QuoteArgument(wszInputFilePath) + " //gencs " + QuoteArgument(sFile));
                if (!string.IsNullOrEmpty(wszDefaultNamespace))
                    cmdLine.Append(" //namespace " + QuoteArgument(wszDefaultNamespace));

                try
                {
                    XmlDocument x = new XmlDocument();
                    x.LoadXml(bstrInputFileContents);
                    var n=x.CreateNavigator();
                    XmlProcessingInstruction proci=((XmlProcessingInstruction)x.SelectSingleNode("//processing-instruction('xsharper-args')"));
                    if (proci != null)
                    {
                        cmdLine.Append(" ");
                        cmdLine.Append(proci.Value.Trim());
                    }
                }
                catch
                {
                    
                }


                
                ProcessStartInfo pi=new ProcessStartInfo("xsharper", cmdLine.ToString());
                pi.RedirectStandardError = true;
                pi.ErrorDialog = true;
                pi.UseShellExecute = false;
                pi.WindowStyle = ProcessWindowStyle.Hidden;
                
                StringBuilder sb=new StringBuilder();
                Process p=new Process();
                p.StartInfo = pi;
                
                p.ErrorDataReceived += (f,x)=> 
                    { 
                        lock (sb) 
                            sb.AppendLine(x.Data);
                    };
                if (p.Start())
                {
                    p.BeginErrorReadLine();
                    p.WaitForExit();
                    
                    int exitCode = p.ExitCode;
                    p.Close();
                    
                    if (exitCode == 0)
                    {
                        byte[] bytes = File.ReadAllBytes(sFile);
                        int length = bytes.Length;
                        rgbOutputFileContents[0] = Marshal.AllocCoTaskMem(length);
                        Marshal.Copy(bytes, 0, rgbOutputFileContents[0], length);
                        pcbOutput = (uint) length;
                        return VSConstants.S_OK;
                    }
                    else
                    {
                        throw new ApplicationException(sb.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                pGenerateProgress.GeneratorError(0, 20, ex.Message, 0xffffffff, 0xffffffff);
                pcbOutput = 0;
            }
            finally
            {
                File.Delete(sFile);
            }
            return VSConstants.E_FAIL;
        }

        #endregion


        public static string QuoteArgument(string arg)
        {
            arg = arg ?? String.Empty;
            if (arg.Length == 0 || arg.IndexOfAny(" \t\r\n\"><|&()[]{}^=;!'+,`~%*".ToCharArray()) != -1)
                return "\"" + arg.Replace("\"", "\\\"") + "\"";
            return arg;
        }


        #region IObjectWithSite Members

        public void GetSite(ref Guid riid, out IntPtr ppvSite)
        {
            if (this.site == null)
            {
                throw new Win32Exception(-2147467259);
            }

            IntPtr objectPointer = Marshal.GetIUnknownForObject(this.site);

            try
            {
                Marshal.QueryInterface(objectPointer, ref riid, out ppvSite);
                if (ppvSite == IntPtr.Zero)
                {
                    throw new Win32Exception(-2147467262);
                }
            }
            finally
            {
                if (objectPointer != IntPtr.Zero)
                {
                    Marshal.Release(objectPointer);
                    objectPointer = IntPtr.Zero;
                }
            }
        }

        public void SetSite(object pUnkSite)
        {
            this.site = pUnkSite;
        }

        #endregion

    }


}
