<?xml version="1.0" encoding="utf-8"?>
<xsharper xmlns="http://www.xsharper.com/schemas/1.0">
	<versionInfo title="FTPUpload" value="Command line utility to upload files to FTP server." Version="0.1.0.0" Copyright="(C) 2009 DeltaX Inc." />
	<usage options="ifNoArguments default" />
	<param name="ftp-server" count="single" value="FTP server address as ftp://user:password@server/directory/" required="true" />
	<param name="files" count="multiple" required="true" description="file1 file2 file3" value="files to upload" last="true" />
	<param value="" />
	<param value="Parameters:" />
	<param switch="passive" value="Use passive mode" unspecified="-1" count="none" default="false" typename="bool" />
	<param switch="text" value="Use text mode (default binary)" unspecified="-1" default="false" count="none" typename="bool" />
	<param switch="mkdir" value="Create directory first" unspecified="-1" default="false" count="none" typename="bool" />
	<param switch="skipexisting" value="Skip existing files" unspecified="-1" default="0" count="none" typename="bool" />
	<param switch="exitIfFirstExists" value="Exit if the first exists, w/o uploading subsequent files" unspecified="-1" default="0" count="none" typename="bool" />
	<?header using System.Net; ?>

	<?code string server=c.GetString("ftp-server");
		
		NetworkCredential nc=null;
		
		UriBuilder ub=new UriBuilder(server);
		string origPath=ub.Path;
		if (!string.IsNullOrEmpty(ub.UserName) ||
			!string.IsNullOrEmpty(ub.Password))
		{
			nc=new NetworkCredential(ub.UserName??"",ub.Password??"");
			ub.UserName=null;
			ub.Password=null;
		}
		bool ssl=false;
		if (ub.Scheme=="ftps" || ub.Scheme=="ftpas" || ub.Scheme=="ftpsa")
		{
			ssl=true;
			ub.Scheme="ftp";
		}			  
		
		System.Net.FtpWebRequest wr;
		if (c.GetBool("mkdir",false))
		{
			int n=ub.Path.LastIndexOf('/');
			if (n!=-1)
				ub.Path=ub.Path.Substring(0,n);

			c.Write(XS.OutputType.Info, "Checking directory " + ub.Path+ "...");
			bool create=false;
			try {
				wr = (System.Net.FtpWebRequest)System.Net.FtpWebRequest.Create(ub.Uri);
				if (nc!=null)
					wr.Credentials=nc;
				wr.Proxy=null;
				wr.KeepAlive=false;
				wr.Method = System.Net.WebRequestMethods.Ftp.PrintWorkingDirectory;
				using (FtpWebResponse response = (FtpWebResponse)wr.GetResponse())
					;
				c.WriteLine(XS.OutputType.Info, "Already exists.");
			}
			catch (WebException e)
			{
				c.WriteLine(XS.OutputType.Info, "Does not exist");
				create=true;
			}
				
			if (create)
			{
				c.Write(XS.OutputType.Info, "Creating directory " + ub.Path+ "...");
				wr = (System.Net.FtpWebRequest)System.Net.FtpWebRequest.Create(ub.Uri);
				if (nc!=null)
					wr.Credentials=nc;
				wr.Proxy=null;
				wr.KeepAlive=true;
				wr.Method = System.Net.WebRequestMethods.Ftp.MakeDirectory;
				c.WriteLine(XS.OutputType.Info, "Done.");
			}
		}
		
		int pr=10;
		int step=90/(c.GetStringArray("files").Length);
		bool first=true;
		foreach (string filename in c.GetStringArray("files"))
		{
			c.OnProgress(pr);
			step+=pr;
			// Upload file
			FileInfo fileInf = new FileInfo(filename);
			ub.Path=origPath;
			if (ub.Path.EndsWith("/"))
				ub.Path=ub.Path+Uri.EscapeDataString(fileInf.Name);			
				
			c.Write(XS.OutputType.Info, "Uploading " + ub.Uri + "...");
						
			if (c.GetBool("skipexisting") || (c.GetBool("exitIfFirstExists") && first))
			{
				wr = (System.Net.FtpWebRequest)System.Net.FtpWebRequest.Create(ub.Uri);
				wr.UseBinary=!c.GetBool("text",false);
				wr.UsePassive=c.GetBool("passive",false);
				if (ssl)
					wr.EnableSsl=ssl;
				if (nc!=null)
					wr.Credentials=nc;
				wr.KeepAlive=false;
				wr.Proxy=null;
				wr.Method = System.Net.WebRequestMethods.Ftp.GetFileSize;
				bool upload=false;
				try {
					using (FtpWebResponse response = (FtpWebResponse)wr.GetResponse())
						;
				}
				catch (WebException)
				{
					upload=true;
				}
				if (!upload)
				{
					c.WriteLine(XS.OutputType.Info, "Already exists.");				
					if (c.GetBool("exitIfFirstExists") && first)
						return null;
					continue;           
				}
			}
			
			first=false;
			
			wr = (System.Net.FtpWebRequest)System.Net.FtpWebRequest.Create(ub.Uri);
			wr.UseBinary=!c.GetBool("text",false);
			wr.UsePassive=c.GetBool("passive",false);
			if (ssl)
				wr.EnableSsl=ssl;
			if (nc!=null)
				wr.Credentials=nc;
			wr.KeepAlive=false;
			wr.Proxy=null;
			wr.Method = System.Net.WebRequestMethods.Ftp.UploadFile;
			
			byte[] buf = new byte[2048];
			wr.ContentLength = fileInf.Length;
			using (FileStream fs = fileInf.OpenRead())
			{
				
				using (Stream strm = wr.GetRequestStream())
				{
					int writeSince=0;
					int len;
					while ((len=fs.Read(buf,0,buf.Length))!=0)
					{
					    c.CheckAbort();
						strm.Write(buf,0,len);
						writeSince+=len;
						if (writeSince>1024*10)
						{
							writeSince=0;
							c.Write(XS.OutputType.Info,".");
						}
						
					}
				}
			}
			using (FtpWebResponse response = (FtpWebResponse)wr.GetResponse())
				;


			c.WriteLine(XS.OutputType.Info, "Done.");
			
			


		}		
			
	?>
<Signature xmlns="http://www.w3.org/2000/09/xmldsig#"><SignedInfo><CanonicalizationMethod Algorithm="http://www.w3.org/2001/10/xml-exc-c14n#WithComments"><InclusiveNamespaces PrefixList="Sign" xmlns="http://www.w3.org/2001/10/xml-exc-c14n#" /></CanonicalizationMethod><SignatureMethod Algorithm="http://www.w3.org/2000/09/xmldsig#rsa-sha1" /><Reference URI=""><Transforms><Transform Algorithm="http://www.w3.org/2000/09/xmldsig#enveloped-signature" /></Transforms><DigestMethod Algorithm="http://www.w3.org/2000/09/xmldsig#sha1" /><DigestValue>XlXr4xu0WpHPyb1FVPmlOFMQV6M=</DigestValue></Reference></SignedInfo><SignatureValue>PAdyNZ6cHfodJBFMVz8iK6nAzz40EUd88cH5HhECqB0DAyfP+lHhcPqkYoJt5OePg3wYDLeu1OOErGYzxN5/wNjIUs6upOo8I73k6Gnp8HLov0adwtofhueA8A6djMZjzoqAIfXi43vKLxuIFxqzcDFLLeM/CYs2iekTmO7LbYk=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>oCKTg0Lq8MruXHnFdhgJA8hS98P5rJSABfUFHicssx0mltfqeuGsgzzpk8gLpNPkmJV+ca+pqPILiyNmMfLnTg4w99zH3FRNd6sIoN1veU87OQ5a0Ren2jmlgAAscHy2wwgjxx8YuP/AIfROTtGVaqVT+PhSvl09ywFEQ+0vlnk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></xsharper>

