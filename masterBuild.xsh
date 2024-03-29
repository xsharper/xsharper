<!-- XSHARPER BUILD SCRIPT -->
<xsharper xmlns="http://www.xsharper.com/schemas/1.0">
	<param switch="fromBuildBat" required="true" count="none" />
 	<param switch="upload" count="none" default="false" />
 	<param switch="increaseVersion" count="none" default="false" />
    
  <include id="ftp" from="${=.script.DirectoryName}Lib\ftpUpload.xsh" />
      
  <!-- Determine current build -->
  <set name="pattern" tr="trim">
	^(?'begin'\[assembly:\s*(AssemblyVersion|AssemblyFileVersion)\(")(?'major'.*?)\.(?'minor'.*?)\.(?'build'.*?)\.(?'revision'.*?)(?'end'".*$)
  </set>

  <regex pattern="${pattern}" value="${=.readText(string.Concat(.script.DirectoryName,'xsharper\properties\AssemblyInfo.cs'))}" options="Multiline" count="1"  setCaptures="true"/>
  
  <!-- Increment build -->
  <code>c.Set("build",c.GetInt("build")+1)</code>
  <set name="version">${major}.${minor}.${build}.${revision}</set>
  <print>Version=${version}</print>
  <set name="versionComma">${major}, ${minor}, ${build}, ${revision}</set>

    
  <!-- Updating files -->
	<if istrue="${increaseVersion|=false}">
  <rowset id="files" tr="expand">
  	<row >${=.script.DirectoryName}xsharper\properties\AssemblyInfo.cs</row>  
  	<row >${=.script.DirectoryName}xsharper.core\properties\AssemblyInfo.cs</row>
  </rowset>	  
  


	<forEach rowsetId="files">
		<!-- Note that expansion uses dual ${{ var }} syntax, because regex likes ${group} too. -->
		<regex 	pattern="${{pattern}}" 
				value="${{=c.readText($)}}" 
				options="Multiline"  
				replace="${begin}${{version}}${end}" 
				outTo="^#${{}}" tr="expandDual" />
	</forEach>

  <rowset id="files2" tr="expand">
  	<row>${=.script.DirectoryName}xsharper\resources\xsharper.rc</row>  
  </rowset>	  

	<forEach rowsetId="files2">
		<!-- Note that expansion uses dual ${{ var }} syntax, because regex likes ${group} too. -->
		<set fn="${}" />
		<set t="${=c.readText($fn)}" />
		<regex 	pattern="(FILEVERSION).*$" 	value="${{t}}" options="Multiline"  	replace="FILEVERSION ${{versionComma}}" outTo="t" tr="expandDual" />
		<regex 	pattern="(PRODUCTVERSION).*$" 	value="${{t}}" options="Multiline"  	replace="PRODUCTVERSION ${{versionComma}}" outTo="t" tr="expandDual" />
		<regex 	pattern='(\"FileVersion\").*$' 	value="${{t}}" options="Multiline"  	replace='"FileVersion", "${{versionComma}}"' outTo="t" tr="expandDual" />
		<regex 	pattern='(\"ProductVersion\").*$' 	value="${{t}}" options="Multiline"  	replace='"ProductVersion", "${{versionComma}}"' outTo="t" tr="expandDual" />
<!--
		<writeText to="${fn}|ascii" value="${t}" /> -->
	</forEach>
	</if>

    <set outDir='${=.script.DirectoryName}\Output' />
    <eval>Directory.CreateDirectory($outDir)</eval>

	<call subId='build'>
		<param>0</param>
	</call>

	<call subId='build'>
		<param>1</param>
	</call>

    	
  <sub id='build'>  
		<param name='net4'  required='1' />

    	<path operation="getTempFileName" outTo="tempFile" />
    	<path operation="getTempFileName" outTo="tempFile2" />
    
		<try>
		  <print outTo="^bold">Building XSharper ${version} (${=$net4?'.NET 4 version':'.NET 2 version'})...</print>
		  <path operation="combine" path="${=Environment.GetFolderPath('MyDocuments')}" param="Sites\DeltaX.snk" outTo="snkFile" />
		  <if exists="${snkFile}">
			<print outTo="^info">The produced assembly will be signed with a key from ${snkFile}</print>
			<set name="extraArgs" value="/p:SignAssembly=true /p:AssemblyOriginatorKeyFile=${snkFile}" />
		  <else>
			  <print outTo="^info">The produced assembly will NOT be signed </print>
		  </else></if>

		  <path operation="combine" path="${=XS.Utils.FindNETFrameworkDirectory(new Version($net4?'4.0.30319':'3.5')).FullName}" param="msbuild.exe" outTo="msbuild" />
		  <!-- Executing Microsoft Build"  -->
		  <block>
			  <try>
				  <set slnname='${=.script.DirectoryName}xsharper${=$net4?".net4":""}.sln' />
  				  <shell outTo="o1" errorTo="e1" tr="trim trimInternal expandAfterTrim">
				  								${msbuild} ${=.quoteArg($slnName)}
				  								/t:clean
				  								/p:Configuration=Release
				  								/nologo 
				  								/consoleloggerparameters:summary 
				  								/verbosity:minimal</shell>
				  <shell outTo="o2" errorTo="e2" tr="trim trimInternal expandAfterTrim">
				  								${msbuild} ${=.quoteArg($slnName)}
				  								/t:XSharper
				  								/p:Configuration=Release
				  								/nologo 
				  								/consoleloggerparameters:summary 
				  								${extraArgs|''}</shell>
			  </try>
			  <catch>
			  	<print outTo="^error">--- MSBuild failed ( ${=.CurrentException.Message} ) with the following output ----</print>
			  	<print>${o1|''}</print>
			  	<print outTo="^error">${e1|''}</print>
			  	<print outTo="^error">---  ----</print>
			  	<print>${o2|''}</print>
			  	<print outTo="^error">${e2|''}</print>
			  	<print outTo="^error">-----------</print>
			
			  	<exit value="1">MS Build error</exit>
			  </catch>

		  </block>			 


		  <print outTo="^info">Build completed</print>
		  <print />			  
			 
		  <set suff='${=$net4?"4":""}' />		  

		  <path operation="combine" path="${=.script.DirectoryName}" param="xsharper\bin\Release${suff}\xsharper.exe" existence="fileExists" outTo="exe"/>
  		  <path operation="combine" path="${=.script.DirectoryName}" param="xsharper\bin\Release${suff}\XSharper.Core.dll" existence="fileExists" outTo="coredll"/>

		  <print outTo="^bold">Generating XML schema</print>
		  <shell outTo="^info">
			<param value="${exe}" />
			<param value="//genxsd" />
			<param value="${tempFile}" />
		  </shell>
		  <print />

          <copy from='${exe}' to='${outDir}\xsharper${suff}.exe' />
          <copy from='${tempFile}' to='${outDir}\xsharper${suff}.zip' />
	      <if isFalse='${net4}'>
               <copy from='${coreDll}' to='${outDir}\XSharper.Core.dll' />
               <copy from='${tempfile}' to='${outDir}\schema.xsd' />
			  <writetext value="${version}" to="${outDir}\xsharper-version.txt" encoding="ascii" />
          </if>

		  <if istrue="${upload|0}">
			  <zip from="${=Path.GetDirectoryName($exe)}" to="${tempFile2}" filter="xsharper.exe"/>

			  <path operation="combine" path="${=Environment.GetFolderPath('MyDocuments')}" param="Sites\xsharper.site" outTo="siteFile" />
			  <if exists="${siteFile}">
				  <readtext from="${siteFile}" outTo="site" encoding="ascii" />  
				  
				  <print outTo="^bold">Uploading files</print>

				  <exec includeId="ftp" isolation="high">
					<param>/passive</param>
					<param value="ftp://${site}/xsharper${suff}.exe" />
					<param value="${exe}" />
				  </exec>
				  
				  <exec includeId="ftp" isolation="high">
					<param>/passive</param>
					<param value="ftp://${site}/xsharper${suff}.zip" />
					<param value="${tempFile2}" />
				  </exec>
				  
				  <if isFalse='${net4}'>
	  				  <exec includeId="ftp" isolation="high">
						<param>/passive</param>
						<param value="ftp://${site}/XSharper.Core.dll" />
						<param value="${coredll}" />
					  </exec>

					  

					  <exec includeId="ftp" isolation="high">
						<param>/passive</param>
	  					<param value="ftp://${site}/schemas/1.0" />
						<param value="/mkdir" />
						<param value="${tempFile}" />
					  </exec>
					  
					  <writetext value="${version}" to="${tempFile}" encoding="ascii" />
					  <exec includeId="ftp" isolation="high">
						<param>/passive</param>
						<param value="ftp://${site}/xsharper-version.txt" />
						<param value="${tempFile}" />
					  </exec>
				  </if>
				  <print />  		  
			  </if>
		  </if>
		  
  		  <print outTo="^bold">Done</print>
		</try>	
 
	  <finally>
	  	<delete from="${tempFile|''}" />
	  	<delete from="${tempFile2|''}" />
	  </finally>
	</sub>
	  
</xsharper>
