<?xml version="1.0"?>
<xsharper xmlns="http://www.xsharper.com/schemas/1.0">
    <versionInfo value="Search/Replace in multiple text files" Version="0.1.0.0" Copyright="(C) 2009 DeltaX Inc." />
    <usage options="ifNoArguments default autoSuffix" />
    <param name="filter" required="true" value="file filter to search, for example *.txt" />
    <param name="search" required="true" value="what to search" />
    <param name="replace" value="with what to replace" default="" />
    <param />
    <param synonyms="d" switch="directory" default="." value="Directory where to start searching" />
    <param synonyms="r" switch="recursive" count="none" default="0" unspecified="-1" value="Scan all subdirectories" />
    <param synonyms="i" switch="ignoreCase" count="none" default="0" unspecified="-1" value="Ignore case" />
    <param switch="regex" count="single" unspecified="" description="regex-options" value="Treat search and replace as regular expressions." />
    <param synonyms="e" switch="encoding" value="Force encoding (utf-8/ascii/utf-16/windows-1252/...)" />
    <param synonyms="w" switch="write" count="none" value="Write files. If this switch is not specified, script will just print filenames." />
    <param synonyms="nb" switch="nobackup" count="none" value="Do not create backup copies (w/o this key file.ext~ are created for all modified files)." />
   
    <setAttr actionId="dir" recursive="${recursive}" />
    <setAttr actionId="r" options="multiline|${regex|=null}" />
        
    <set count="0" />
    <dir id="dir" from="${directory}" filter="${filter}">
        <readText from="${from}" encoding="${encoding|=null}" outTo="original" />
        <if isSet="regex">
                <regex id="r" pattern="${search}" replace="${replace}" options="multiline" value="${original}" outTo="changed" />
            <else>
                <set name="changed" value="${=$original.Replace($search,$replace)}" />
            </else>
        </if>
        
        <if isNotZero="${=string.Compare($original,$changed)}">
            <print>${from}</print>          
            <if isSet="write">
                    <if isNotSet="nobackup">
                        <copy from="${from}" to="${from}.~" overwrite="always" />
                    </if>
                    <writeText to="${from}" encoding="${encoding|=null}" value="${changed}" />
            </if>
            <code>
                 c.Set("count",c.GetInt("count")+1) 
            </code>
        </if>
    </dir>
    
    <!-- Example of using if/else construction in C# -->
    <code>
     if (c.IsSet("write")) 
        <print outTo="$info">${count} files were changed.</print>
     else
        <print outTo="$info">${count} files found.</print>
    </code>

    
    
<Signature xmlns="http://www.w3.org/2000/09/xmldsig#"><SignedInfo><CanonicalizationMethod Algorithm="http://www.w3.org/2001/10/xml-exc-c14n#WithComments"><InclusiveNamespaces PrefixList="Sign" xmlns="http://www.w3.org/2001/10/xml-exc-c14n#" /></CanonicalizationMethod><SignatureMethod Algorithm="http://www.w3.org/2000/09/xmldsig#rsa-sha1" /><Reference URI=""><Transforms><Transform Algorithm="http://www.w3.org/2000/09/xmldsig#enveloped-signature" /></Transforms><DigestMethod Algorithm="http://www.w3.org/2000/09/xmldsig#sha1" /><DigestValue>5CitE/EsQ8JaOdRbgWDUFsEIag8=</DigestValue></Reference></SignedInfo><SignatureValue>licOtygwK+T9pbFQMieP7tOU1QXQkq5LKs7RpfssNgR1xDAhdj+OA3RY/xNhXH594e52Kl0PTzTwxXgnKjehDtpMWWDyuPabzL2zlRYMYWyI2iHI1YBR+wV+DTZrSfBRl3EdUkuMJo0uCbdKxUOqlwh0u9BhcO8JJelnY4s07Fc=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>oCKTg0Lq8MruXHnFdhgJA8hS98P5rJSABfUFHicssx0mltfqeuGsgzzpk8gLpNPkmJV+ca+pqPILiyNmMfLnTg4w99zH3FRNd6sIoN1veU87OQ5a0Ren2jmlgAAscHy2wwgjxx8YuP/AIfROTtGVaqVT+PhSvl09ywFEQ+0vlnk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></xsharper>  