<download from='embed:///XSharper.Embedded.Assemblies.XSharper.Core.dll' to='Template\3rdParty\XSharper.Core.dll' />
<download from='embed:///XSharper.Embedded.Assemblies.ICSharpCode.SharpZipLib.dll' to='Template\3rdParty\ICSharpCode.SharpZipLib.dll' />

<!-- Build XshCodeGenerator -->
<path operation="combine" path="${=XS.Utils.FindNETFrameworkDirectory(new Version('3.5')).FullName}" param="msbuild.exe" outTo="msbuild" />

<!-- Executing Microsoft Build"  -->
<print>Building XshCodeGenerator</print>
<block>
  <try>
	  <shell  tr="trim trimInternal expandAfterTrim">
								${msbuild} ${=.script.DirectoryName}..\XshCodeGenerator\XshCodeGenerator.sln 
									/t:clean
	 								/p:Configuration=Release
	  								/consoleloggerparameters:summary 
	  								/verbosity:minimal</shell>
	
	  <shell  tr="trim trimInternal expandAfterTrim" ignoreExitCode='true' outTo='^nul' errorTo='^nul'>
								${msbuild} ${=.script.DirectoryName}..\XshCodeGenerator\XshCodeGenerator.sln 
	 								/p:Configuration=Release
	  								/consoleloggerparameters:summary 
	  								/verbosity:minimal</shell>
	  <shell  tr="trim trimInternal expandAfterTrim">
								${msbuild} ${=.script.DirectoryName}..\XshCodeGenerator\XshCodeGenerator.sln 
	 								/p:Configuration=Release
	  								/consoleloggerparameters:summary 
	  								/verbosity:minimal</shell>
	
 </try>
  <catch>
  	<print outTo="^error">--- MSBuild failed with the following output ----</print>
  	<print>${o1|''}</print>
  	<print outTo="^error">${e1|''}</print>
  	<print outTo="^error">---  ----</print>
  	<exit value="1" />
  </catch>
</block>			 


<print>Compressing files</print>
<zip from='Template' to='template.zip' recursive='true' />
<shell>Xsharper template-installer.xsh //genwinexe bin\template-installer.exe</shell>
<delete from='template.zip' />
