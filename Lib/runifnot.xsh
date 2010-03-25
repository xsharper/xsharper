<?xml version="1.0" encoding="utf-8"?>
<xsharper xmlns="http://www.xsharper.com/schemas/1.0">
	<versionInfo title="RunIfNot" value="Run a program if a certain top-level window does not exist." Version="0.1.0.0" Copyright="(C) 2009 DeltaX Inc." />
	<usage options="ifNoArguments | default" />
	<param name="process-name" required="true">Process name, w/o .exe. For example, 'explorer'</param>
	<param name="title-wildcard" required="true">Wildcard to match window title (if starts with ^ - treat as regex)</param>
	<param name="command-line" required="true" count="multiple" last="true">Command line to execute if the window is not found</param>
	<header>
		using System.Runtime.InteropServices;
		using System.Diagnostics;
		
		static class Native {
			[DllImport("user32.dll", SetLastError = true)]
			public static extern bool BringWindowToTop(IntPtr hWnd);

			[DllImport("user32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool SetForegroundWindow(IntPtr hWnd);
		}
	</header>
	<code>
		XS.StringFilter sf=new XS.StringFilter(c.GetStr("title-wildcard"));
		foreach (Process p in Process.GetProcessesByName(c.GetStr("process-name")))
		{
			if (sf.IsMatch(p.MainWindowTitle))
			{
				Native.SetForegroundWindow(p.MainWindowHandle);
				Native.BringWindowToTop(p.MainWindowHandle);
				return 0;
			}
			
		}
	</code>
	<shell mode="shellExecute" wait="false"><param>${command-line}</param></shell>
<Signature xmlns="http://www.w3.org/2000/09/xmldsig#"><SignedInfo><CanonicalizationMethod Algorithm="http://www.w3.org/2001/10/xml-exc-c14n#WithComments"><InclusiveNamespaces PrefixList="Sign" xmlns="http://www.w3.org/2001/10/xml-exc-c14n#" /></CanonicalizationMethod><SignatureMethod Algorithm="http://www.w3.org/2000/09/xmldsig#rsa-sha1" /><Reference URI=""><Transforms><Transform Algorithm="http://www.w3.org/2000/09/xmldsig#enveloped-signature" /></Transforms><DigestMethod Algorithm="http://www.w3.org/2000/09/xmldsig#sha1" /><DigestValue>M85dP0tALj/NM5N/OS95Q7sAH7o=</DigestValue></Reference></SignedInfo><SignatureValue>UaYMLqRpmzHf989uEegmKuNiYASj/AiLDyj8BSO2hyf0h+j/FN8ysbA2CVNSEifLkYm6wkf9K97ETah9GBur1MadNsaNhuid3EVFNTuGzmHlVhbjf8j+7azsBx9y1UmY/fvh+r4rl88M1VkL9TdCF3Tf0lz3ne/XAZWfgyWdO88=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>oCKTg0Lq8MruXHnFdhgJA8hS98P5rJSABfUFHicssx0mltfqeuGsgzzpk8gLpNPkmJV+ca+pqPILiyNmMfLnTg4w99zH3FRNd6sIoN1veU87OQ5a0Ren2jmlgAAscHy2wwgjxx8YuP/AIfROTtGVaqVT+PhSvl09ywFEQ+0vlnk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></xsharper>