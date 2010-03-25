<?xml version="1.0" encoding="utf-8"?>
<xsharper ID="HASH" xmlns="http://www.xsharper.com/schemas/1.0">
  <versionInfo value="Calculate SHA1 or MD5 over a set of files. Each file is calculated individually." />
  <usage options="ifHelp ifNoArguments default" lineSuffix=" [switches]" />
  <param count="multiple" value="Files to hash (- = stdin)" name="files" description="file [file...]" required="true" />
  <param switch="md5" count="none" default="0" unspecified="1" value="Calculate MD5" description="md5" typename="bool" />
  <param switch="sha1" count="none" default="1" unspecified="1" value="Calculate SHA1 (default)" description="sha1" typename="bool?" />
  <?h using System.Security.Cryptography; ?>
  <?_ foreach (string f in c.GetStringArray("files"))
		{
			HashAlgorithm h=c.GetBool("md5")?(HashAlgorithm)MD5.Create():(HashAlgorithm)SHA1.Create();
			using (Stream s=(f=="-"?System.Console.OpenStandardInput():c.OpenReadStream(f)))
				c.WriteLine(BitConverter.ToString(h.ComputeHash(s)).Replace("-",""));
		}
	?>
<Signature xmlns="http://www.w3.org/2000/09/xmldsig#"><SignedInfo><CanonicalizationMethod Algorithm="http://www.w3.org/2001/10/xml-exc-c14n#WithComments"><InclusiveNamespaces PrefixList="Sign" xmlns="http://www.w3.org/2001/10/xml-exc-c14n#" /></CanonicalizationMethod><SignatureMethod Algorithm="http://www.w3.org/2000/09/xmldsig#rsa-sha1" /><Reference URI=""><Transforms><Transform Algorithm="http://www.w3.org/2000/09/xmldsig#enveloped-signature" /></Transforms><DigestMethod Algorithm="http://www.w3.org/2000/09/xmldsig#sha1" /><DigestValue>UpHesny0le2dEwTXGc7vZfxZk1s=</DigestValue></Reference></SignedInfo><SignatureValue>iCWiU5WvmQINza6/bXZqMj2rRYhC63mnSSBh7Tr25Fr8XkRGGDWMVM+C2TKalPqJ+Pno6iXkQTDFa39lvwlXY/d2PizpZp9+gwgZzZ4SXdTsGieavoHlsumlL9bZtDmqwYvM1D5UT82P1Y0gQfCGyzuEbCMKKCk185PiPytaDVY=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>oCKTg0Lq8MruXHnFdhgJA8hS98P5rJSABfUFHicssx0mltfqeuGsgzzpk8gLpNPkmJV+ca+pqPILiyNmMfLnTg4w99zH3FRNd6sIoN1veU87OQ5a0Ren2jmlgAAscHy2wwgjxx8YuP/AIfROTtGVaqVT+PhSvl09ywFEQ+0vlnk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></xsharper>