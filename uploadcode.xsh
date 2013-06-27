<xsharper>

	<include id="ftp" from="${=.script.DirectoryName}Lib\ftpUpload.xsh" />


	<set upload="${=.Script.DirectoryName}Upload" />
	<delete from='${upload}' deleteRoot='false' recursive='true' />
	
	
	<!-- Determine current build -->
	<set name="pattern" tr="trim">
		^(?'begin'\[assembly:\s*(AssemblyVersion|AssemblyFileVersion)\(")(?'major'.*?)\.(?'minor'.*?)\.(?'build'.*?)\.(?'revision'.*?)(?'end'".*$)
	</set>

	<regex pattern="${pattern}" value="${=.readText(string.Concat(.script.DirectoryName,'xsharper\properties\AssemblyInfo.cs'))}" options="Multiline" count="1"  setCaptures="true"/>
	<set name="version">${major}.${minor}.${build}.${revision}</set>

	<print outTo='^bold'>Compressing files</print>
	<set allSrc='${upload}\xsharper-source-${version}.zip' />
	<set coreSrc='${upload}\xsharper-source-core-${version}.zip' />
	<set back='${=new string(8,100)}${=new string(32,100)}${=new string(8,100)}' />

	<zip from="." to="${allSrc}" recursive='true' 
								directoryFilter='-*\.svn\*;-*\obj\*;-*\bin\*;-*\bin4\*;-*\_*;-*\XSharper\Embedded\*;-*\lib\*' 
								filter='-*.res;-*.user;-*.idc;-*.aps;-*.cache;-uploadcode.xsh;-*.suo;-*.resharper' >
		<print nl='0'>${back}${=}</print>
	</zip>

	<zip from="." to="${coreSrc}" recursive='true' 
								directoryFilter='-*\.svn\*;-*\obj\*;-*\bin\*;-*\bin4\*;-*\_*;-*\XSharper\Embedded\*;-*\lib\*;*\XSharper.Core\*;*\XSharper.Core\*' 
								filter='-*.res;-*.user;-*.idc;-*.aps;-*.cache;-uploadcode.xsh;-*.suo;-*.resharper' >
		<print nl='0'>${back}${}</print>
	</zip>

	<print>${back}</print>

  <path operation="combine" path="${=Environment.GetFolderPath('MyDocuments')}" param="Sites\xsharper.site" outTo="siteFile" />
  <if exists="${siteFile}">
		  <readtext from="${siteFile}" outTo="site" encoding="ascii" />  
		  <exec includeId="ftp" isolation="high">
			<param>/passive</param>
			<param value="ftp://${site}/xsharper-source-${version}.zip" />
			<param value="${allSrc}" />
		  </exec>
		  <exec includeId="ftp" isolation="high">
			<param>/passive</param>
			<param value="ftp://${site}/xsharper-source-core-${version}.zip" />
			<param value="${coreSrc}" />
		  </exec>

	</if>
	  

</xsharper>