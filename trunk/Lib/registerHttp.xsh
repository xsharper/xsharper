<?xml version="1.0" encoding="utf-8"?>
<xsharper xmlns="http://www.xsharper.com/schemas/1.0" requireAdmin="1">

<versionInfo>Program to grant access to a HTTP/HTTPS registration to a given user, as well as associate an SSL certificate with such address.</versionInfo>
<usage options="ifNoArguments default autosuffix" />
<param name="listenUri" required="true" value="Server URI, for example https://127.0.0.1:443/cp/ " />
<param name="accountName" default="${=WindowsIdentity.GetCurrent().Name}" value="Account to grant privileges to." />
<param name="certFilter" value="Certificate subject filter for MY store of the local machine, if an SSL certificate is to be associated with the address. * and ? characters can be used." />
<param name="certGuid" value="Application GUID to identify the owning application. New GUID is generated if not specified." />
<param />
<param>Switches:</param>
<param />
<param switch="unregister" count="none" default="0" unspecified="1" value="Unregister the URI" />
<param switch="unregisterSSL" count="none" default="0" unspecified="1" value="Unregister the URI and remove the SSL certificate from port:host" />

<set sid="${accountName}" />
<if isFalse="${=$sid.StartsWith('S-1-')}">
	<set sid="${=new NTAccount($accountName).Translate(typeof(SecurityIdentifier)).ToString()}" />
</if>
<set sddl="D:(A;;GX;;;${sid})" />
<?_ var store=new X509Store(StoreName.My,StoreLocation.LocalMachine);
	var ub=new UriBuilder(c.GetStr("listenUri"));
	
    try
    {	
		var uri=ub.Scheme+"://+:"+ub.Port+ub.Path;
		if (c.GetBool("unregister") || c.GetBool("unregisterSSL"))
		{
			HttpCfg.ReserveUrl(uri, null);
			c.Info.WriteLine("Access to "+uri+" removed.");
		
			if (c.GetBool("unregisterSSL"))
			{  	
        		HttpCfg.SetSslCert(new Guid(c.GetStr("certGuid",Guid.Empty.ToString())), ub.Host, ub.Port, null,null);
				c.Info.WriteLine("SSL certificate for  "+ub.Host+":"+ub.Port+" removed.");  	
        	}
		}
		else
		{
			HttpCfg.ReserveUrl(uri, c.GetStr("sddl"));
			c.Info.WriteLine("Access to "+uri+" granted to user "+c.GetStr("accountName"));  	

			if (c.IsSet("certFilter"))
			{
    			store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
	
				X509Certificate2 cert=null;
				var filter=new XS.StringFilter(c.GetStr("certFilter"));
				foreach (var crt in store.Certificates)
        			if (filter.IsMatch(crt.SubjectName.Format(false)))
					{
						if (cert!=null)
							throw new ApplicationException("Too many certificates match the filter");
						cert=crt;
					}

				if (cert==null)
					throw new ApplicationException("Certificate not found");
		
        		HttpCfg.SetSslCert(new Guid(c.GetStr("certGuid",Guid.NewGuid().ToString())), ub.Host, ub.Port, store.Name, cert.GetCertHash());
				c.Info.WriteLine("SSL certificate '"+cert.SubjectName.Format(false) +"' registered for "+ub.Host+":"+ub.Port+".");  	
			}
		}
	}
    finally
    {
    	store.Close();
    }
?>

<!-- =========================================================================== -->

<?h using System.Security.Principal;
	using System.Runtime.InteropServices;
	using System.Net;
	using System.ComponentModel;
	using System.Security.Cryptography.X509Certificates;


	public static class HttpCfg
    {
        public static void SetSslCert(Guid appId, string ipAddress, int port, string storeName, byte[] hash)
        {
            var httpApiVersion = new HTTPAPI_VERSION(1, 0);
            uint retVal = HttpInitialize(httpApiVersion, HTTP_INITIALIZE_CONFIG, IntPtr.Zero);
            if (NOERROR != retVal)
                throw new Win32Exception(Convert.ToInt32(retVal));

            try
            {
                IPAddress ip = IPAddress.Parse(ipAddress);
                var ipEndPoint = new IPEndPoint(ip, port);

                // serialize the endpoint to a SocketAddress and create an array to hold the values.  Pin the array.
                SocketAddress socketAddress = ipEndPoint.Serialize();
                byte[] socketBytes = new byte[socketAddress.Size];
                GCHandle handleSocketAddress = GCHandle.Alloc(socketBytes, GCHandleType.Pinned);
                // Should copy the first 16 bytes (the SocketAddress has a 32 byte buffer, the size will only be 16,
                //which is what the SOCKADDR accepts
                for (int i = 0; i < socketAddress.Size; ++i)
                    socketBytes[i] = socketAddress[i];

                if (hash == null)
                    hash = new byte[0];
                GCHandle handleHash = GCHandle.Alloc(hash, GCHandleType.Pinned);

                var configSslSet = new HTTP_SERVICE_CONFIG_SSL_SET
                                       {
                                           ParamDesc = new HTTP_SERVICE_CONFIG_SSL_PARAM
                                                           {
                                                               AppId = appId,
                                                               DefaultCertCheckMode = 0,
                                                               DefaultFlags = HTTP_SERVICE_CONFIG_SSL_FLAG_NEGOTIATE_CLIENT_CERT,
                                                               DefaultRevocationFreshnessTime = 0,
                                                               DefaultRevocationUrlRetrievalTimeout = 0,
                                                               pSslCertStoreName = storeName,
                                                               SslHashLength = hash.Length,
                                                               pSslHash = handleHash.AddrOfPinnedObject()
                                                           },
                                           KeyDesc = new HTTP_SERVICE_CONFIG_SSL_KEY
                                                         {
                                                             pIpPort = handleSocketAddress.AddrOfPinnedObject()
                                                         }
                                       };

                IntPtr pInputConfigInfo = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(HTTP_SERVICE_CONFIG_SSL_SET)));
                try
                {
                    Marshal.StructureToPtr(configSslSet, pInputConfigInfo, false);

                    retVal = HttpDeleteServiceConfiguration(IntPtr.Zero,
                                                            HTTP_SERVICE_CONFIG_ID.HttpServiceConfigSSLCertInfo,
                                                            pInputConfigInfo, Marshal.SizeOf(configSslSet), IntPtr.Zero);
                    if (storeName != null)
                        retVal = HttpSetServiceConfiguration(IntPtr.Zero,
                                                             HTTP_SERVICE_CONFIG_ID.HttpServiceConfigSSLCertInfo,
                                                             pInputConfigInfo,
                                                             Marshal.SizeOf(configSslSet),
                                                             IntPtr.Zero);
                }
                finally
                {
                    handleSocketAddress.Free();
                    handleHash.Free();
                    Marshal.FreeCoTaskMem(pInputConfigInfo);
                }
                if (NOERROR != retVal && !(retVal == ERROR_NOT_FOUND && storeName == null))
                    throw new Win32Exception(Convert.ToInt32(retVal));
            }
            finally
            {
                HttpTerminate(HTTP_INITIALIZE_CONFIG, IntPtr.Zero);
            }
        }

        public static void ReserveUrl(string networkUrl, string sddl)
        {
            var version = new HTTPAPI_VERSION(1, 0);
            uint retVal = HttpInitialize(version, HTTP_INITIALIZE_CONFIG, IntPtr.Zero);
            if (NOERROR != retVal)
                throw new Win32Exception(Convert.ToInt32(retVal));

            try
            {
                var keyDesc = new HTTP_SERVICE_CONFIG_URLACL_KEY { UrlPrefix = networkUrl };
                var paramDesc = new HTTP_SERVICE_CONFIG_URLACL_PARAM { Sddl = sddl ?? "" };
                var inputConfigInfoSet = new HTTP_SERVICE_CONFIG_URLACL_SET { Key = keyDesc, Param = paramDesc };
                var pInputConfigInfo = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(HTTP_SERVICE_CONFIG_URLACL_SET)));
                try
                {
                    Marshal.StructureToPtr(inputConfigInfoSet, pInputConfigInfo, false);
                    retVal = HttpDeleteServiceConfiguration(IntPtr.Zero,
                                                            HTTP_SERVICE_CONFIG_ID.HttpServiceConfigUrlAclInfo,
                                                            pInputConfigInfo, Marshal.SizeOf(inputConfigInfoSet),
                                                            IntPtr.Zero);
                    if (sddl != null)
                        retVal = HttpSetServiceConfiguration(IntPtr.Zero,
                                                             HTTP_SERVICE_CONFIG_ID.HttpServiceConfigUrlAclInfo,
                                                             pInputConfigInfo,
                                                             Marshal.SizeOf(inputConfigInfoSet),
                                                             IntPtr.Zero);
                }
                finally
                {
                    Marshal.FreeCoTaskMem(pInputConfigInfo);
                }
                if (NOERROR != retVal && !(retVal==ERROR_NOT_FOUND && sddl==null))
                    throw new Win32Exception(Convert.ToInt32(retVal));
            }
            finally
            {
                HttpTerminate(HTTP_INITIALIZE_CONFIG, IntPtr.Zero);
            }

        }


        #region DllImport

        [DllImport("httpapi.dll", SetLastError = true)]
        static extern uint HttpInitialize(HTTPAPI_VERSION Version, uint Flags, IntPtr pReserved);

        [DllImport("httpapi.dll", SetLastError = true)]
        static extern uint HttpSetServiceConfiguration(IntPtr ServiceIntPtr, HTTP_SERVICE_CONFIG_ID ConfigId, IntPtr pConfigInformation, int ConfigInformationLength, IntPtr pOverlapped);

        [DllImport("httpapi.dll", SetLastError = true)]
        static extern uint HttpDeleteServiceConfiguration(IntPtr ServiceIntPtr, HTTP_SERVICE_CONFIG_ID ConfigId, IntPtr pConfigInformation, int ConfigInformationLength, IntPtr pOverlapped);

        [DllImport("httpapi.dll", SetLastError = true)]
        static extern uint HttpTerminate(uint Flags, IntPtr pReserved);

        [StructLayout(LayoutKind.Sequential)]
        struct HTTP_SERVICE_CONFIG_URLACL_KEY
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string UrlPrefix;
        }
        [StructLayout(LayoutKind.Sequential)]
        struct HTTP_SERVICE_CONFIG_URLACL_PARAM
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Sddl;
        }
        [StructLayout(LayoutKind.Sequential)]
        struct HTTP_SERVICE_CONFIG_URLACL_SET
        {
            public HTTP_SERVICE_CONFIG_URLACL_KEY Key;
            public HTTP_SERVICE_CONFIG_URLACL_PARAM Param;
        }

        enum HTTP_SERVICE_CONFIG_ID
        {
            HttpServiceConfigIPListenList = 0,
            HttpServiceConfigSSLCertInfo,
            HttpServiceConfigUrlAclInfo,
            HttpServiceConfigMax
        }

        [StructLayout(LayoutKind.Sequential)]
        struct HTTP_SERVICE_CONFIG_IP_LISTEN_PARAM
        {
            public ushort AddrLength;
            public IntPtr pAddress;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct HTTP_SERVICE_CONFIG_SSL_SET
        {
            public HTTP_SERVICE_CONFIG_SSL_KEY KeyDesc;
            public HTTP_SERVICE_CONFIG_SSL_PARAM ParamDesc;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HTTP_SERVICE_CONFIG_SSL_KEY
        {
            public IntPtr pIpPort;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct HTTP_SERVICE_CONFIG_SSL_PARAM
        {
            public int SslHashLength;
            public IntPtr pSslHash;
            public Guid AppId;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pSslCertStoreName;
            public uint DefaultCertCheckMode;
            public int DefaultRevocationFreshnessTime;
            public int DefaultRevocationUrlRetrievalTimeout;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pDefaultSslCtlIdentifier;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pDefaultSslCtlStoreName;
            public uint DefaultFlags;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        struct HTTPAPI_VERSION
        {
            public ushort HttpApiMajorVersion;
            public ushort HttpApiMinorVersion;

            public HTTPAPI_VERSION(ushort majorVersion, ushort minorVersion)
            {
                HttpApiMajorVersion = majorVersion;
                HttpApiMinorVersion = minorVersion;
            }
        }

        #endregion

        #region Constants

        public const uint HTTP_INITIALIZE_CONFIG = 0x00000002;
        public const uint HTTP_SERVICE_CONFIG_SSL_FLAG_USE_DS_MAPPER = 0x00000001;
        public const uint HTTP_SERVICE_CONFIG_SSL_FLAG_NEGOTIATE_CLIENT_CERT = 0x00000002;
        public const uint HTTP_SERVICE_CONFIG_SSL_FLAG_NO_RAW_FILTER = 0x00000004;

        private const uint NOERROR = 0;
        private const uint ERROR_NOT_FOUND = 2;
        #endregion
    }
?>

<Signature xmlns="http://www.w3.org/2000/09/xmldsig#"><SignedInfo><CanonicalizationMethod Algorithm="http://www.w3.org/2001/10/xml-exc-c14n#WithComments"><InclusiveNamespaces PrefixList="Sign" xmlns="http://www.w3.org/2001/10/xml-exc-c14n#" /></CanonicalizationMethod><SignatureMethod Algorithm="http://www.w3.org/2000/09/xmldsig#rsa-sha1" /><Reference URI=""><Transforms><Transform Algorithm="http://www.w3.org/2000/09/xmldsig#enveloped-signature" /></Transforms><DigestMethod Algorithm="http://www.w3.org/2000/09/xmldsig#sha1" /><DigestValue>vDE4DjNy6UZIjEsDp4MtZDDZHAQ=</DigestValue></Reference></SignedInfo><SignatureValue>fhrsY3GDcCYTzFW3IN+E6dxz8QWZlF3ZbVNIakdTLVGDFgxetAzC8xgITkJoBgkPApYPZ9itS7lXqLbpsFXtsGprna/+cenFb8fG5REd5IBE1mST0wVNaCSknKgMz31fqpuiwPL0FKiT9HM7++MiNTNaOtOowYPOLxue2Xg2bpQ=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>oCKTg0Lq8MruXHnFdhgJA8hS98P5rJSABfUFHicssx0mltfqeuGsgzzpk8gLpNPkmJV+ca+pqPILiyNmMfLnTg4w99zH3FRNd6sIoN1veU87OQ5a0Ren2jmlgAAscHy2wwgjxx8YuP/AIfROTtGVaqVT+PhSvl09ywFEQ+0vlnk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></xsharper>