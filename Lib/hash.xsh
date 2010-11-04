<?xml version="1.0" encoding="utf-8"?>
<xsharper ID="HASH" xmlns="http://www.xsharper.com/schemas/1.0">
  <versionInfo value="Calculate SHA1 or MD5 over a set of files. Each file is calculated individually." />
  <usage options="ifHelp ifNoArguments default" lineSuffix=" [switches]" />
  <param count="multiple" value="Files to hash (- = stdin)" name="files" description="file [file...]" required="true" />
  <param switch="md5" count="none" default="0" unspecified="1" value="Calculate MD5" description="md5" typename="bool" />
  <param switch="sha256" count="none" default="0" unspecified="1" value="Calculate SHA256" description="md5" typename="bool" />
  <param switch="sha1" count="none" default="1" unspecified="1" value="Calculate SHA1 (default)" description="sha1" typename="bool?" />
  <?h using System.Security.Cryptography; ?>
  <foreach in="${files}">
    <set hash="${= $md5 ? MD5.Create() : null }" />
    <set hash="${= $sha256 ? SHA256.Create() : $hash }" />
	<set hash="${= $hash ?? SHA1.Create()  }" />

	<set stream="${= ($==&quot;-&quot;)?System.Console.OpenStandardInput():c.OpenStream($)}" />
	<try>
		<print>${=.ToHex($hash.ComputeHash($stream)).ToUpper()}</print>
	</try>
	<finally>
		<eval>$stream.Dispose();</eval>
	</finally>
  </foreach>
<Signature xmlns="http://www.w3.org/2000/09/xmldsig#"><SignedInfo><CanonicalizationMethod Algorithm="http://www.w3.org/2001/10/xml-exc-c14n#WithComments"><InclusiveNamespaces PrefixList="Sign" xmlns="http://www.w3.org/2001/10/xml-exc-c14n#" /></CanonicalizationMethod><SignatureMethod Algorithm="http://www.w3.org/2000/09/xmldsig#rsa-sha1" /><Reference URI=""><Transforms><Transform Algorithm="http://www.w3.org/2000/09/xmldsig#enveloped-signature" /></Transforms><DigestMethod Algorithm="http://www.w3.org/2000/09/xmldsig#sha1" /><DigestValue>BBGvvgL1309WcTpHLd5+irmYpMQ=</DigestValue></Reference></SignedInfo><SignatureValue>lIWzSzYp6S9P71g2PPAkTN9XsC0ur5Avg3WzhbZXeJIyihyzzcjOz2s1nr4Tk+R0QJZLhwrIGHTyj9hcxspku+vktHMlDVt1ebCkeU5fa46Q05HF/f96CPEBzN+Vs6tBy7EX2TFVo4wx0RVM4K+qoJRU2d6kV2LgHLzerhnGt24=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>oCKTg0Lq8MruXHnFdhgJA8hS98P5rJSABfUFHicssx0mltfqeuGsgzzpk8gLpNPkmJV+ca+pqPILiyNmMfLnTg4w99zH3FRNd6sIoN1veU87OQ5a0Ren2jmlgAAscHy2wwgjxx8YuP/AIfROTtGVaqVT+PhSvl09ywFEQ+0vlnk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></xsharper>