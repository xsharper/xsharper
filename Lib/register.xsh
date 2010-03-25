<?xml version="1.0" encoding="utf-8"?>
<xsharper xmlns="http://www.xsharper.com/schemas/1.0" requireAdmin="true">
	<versionInfo title="register" value="Register XSharper as default handler for .XSH file extension" Version="0.1.0.0" Copyright="(C) 2009 DeltaX Inc." />
	<set process="${=System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName}" />
	<set filename="${=string.Concat(Path.Combine(Path.GetTempPath(),Path.GetRandomFileName()),'.reg')}" />
	<try>
		<set cmdLine="${=${process}.Replace('\','\\')}" />
		<writetext to="${filename}" tr="expand trimStart multiline" encoding="ascii">

				REGEDIT4

				[HKEY_CLASSES_ROOT\.xsh]
				@="xshfile"
				"Content Type"="text/xml"
				
				[HKEY_CLASSES_ROOT\xshfile]
				@="XSharper Script"
				
				[HKEY_CLASSES_ROOT\xshfile\DefaultIcon]
				@="${cmdLine},1"
				[HKEY_CLASSES_ROOT\xshfile\Shell]
				[HKEY_CLASSES_ROOT\xshfile\Shell\Edit]
				[HKEY_CLASSES_ROOT\xshfile\Shell\Edit\Command]
				@="notepad.exe '%1'"
				[HKEY_CLASSES_ROOT\xshfile\Shell]
				[HKEY_CLASSES_ROOT\xshfile\Shell\Open]
				@="Execute"
				[HKEY_CLASSES_ROOT\xshfile\Shell\Open\Command]
				@="\"${cmdline}\" \"%1\" %*"
				[HKEY_CLASSES_ROOT\xshfile\Shell\OpenAndWait]
				@="Execute and wait"
				[HKEY_CLASSES_ROOT\xshfile\Shell\OpenAndWait\Command]
				@="\"${cmdline}\" //wait \"%1\" %*"
		</writetext>
		<shell>
				regedit
				<param>/s</param>
				<param>${filename}</param>
		</shell>
		<print>XSharper registration was successful</print>
	</try>
	<finally>
		<delete from="${filename}" />
	</finally>
	

<Signature xmlns="http://www.w3.org/2000/09/xmldsig#"><SignedInfo><CanonicalizationMethod Algorithm="http://www.w3.org/2001/10/xml-exc-c14n#WithComments"><InclusiveNamespaces PrefixList="Sign" xmlns="http://www.w3.org/2001/10/xml-exc-c14n#" /></CanonicalizationMethod><SignatureMethod Algorithm="http://www.w3.org/2000/09/xmldsig#rsa-sha1" /><Reference URI=""><Transforms><Transform Algorithm="http://www.w3.org/2000/09/xmldsig#enveloped-signature" /></Transforms><DigestMethod Algorithm="http://www.w3.org/2000/09/xmldsig#sha1" /><DigestValue>GHo1hz4mXDtQ6B3CMA8irlc0ENU=</DigestValue></Reference></SignedInfo><SignatureValue>U9yRUNgbmb/iC1h3t9sbcJO3KAeMkReUTGdQLPGx3MXW/TR4pEsqFssagK3HeQWuomWkTICF3avJFFTr9FgaDm02tKPegKpLqFJsvbZIj4FEeu/epd4eFH7PFSF9q6cvlN+xtSbCJWWGTCHrlUkrqDsIcmh3K/tdS1e1Qj68obs=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>oCKTg0Lq8MruXHnFdhgJA8hS98P5rJSABfUFHicssx0mltfqeuGsgzzpk8gLpNPkmJV+ca+pqPILiyNmMfLnTg4w99zH3FRNd6sIoN1veU87OQ5a0Ren2jmlgAAscHy2wwgjxx8YuP/AIfROTtGVaqVT+PhSvl09ywFEQ+0vlnk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></xsharper>
