<xsharper xmlns="http://www.xsharper.com/schemas/1.0">

  <!-- Determine current build -->
  <readtext from="${=.scriptDirectory}SetOfVariablesTempl.cs" outTo="text" />
  <set name="pattern"><![CDATA[
  
	^(?<begin>.*?\#if\s+TEMPLATE)
	(?<templ>.*?)
	(?<templend>\#endif.*?)
	(?<between>.*?)
	(?<regionbegin>\#region)
	(?<generated>.*?)	
	(?<regionend>\#endregion.*?)$		
	
	]]></set>

  <regex pattern="${pattern}" text="${text}" options="SingleLine IgnorePatternWhitespace" count="1"  />
 
  <rowset id="r">
   	<row typeSuffix="Bool" typeName="bool" />
  	<row typeSuffix="String" typeName="string" />
  	<row typeSuffix="Str" typeName="string" />
  	<row typeSuffix="Int" typeName="int" />
  	<row typeSuffix="Long" typeName="long" />
  	<row typeSuffix="Float" typeName="float" />
  	<row typeSuffix="Double" typeName="double" />
	<row typeSuffix="Decimal" typeName="decimal" />
  </rowset>
  
 	<forEach rowsetId="r">
  		<set name="+out" tr="expand" >${=.Expand($templ)}</set>
  	</forEach>

 
  <set name="file" tr="newlineToCRLF expand">${begin}${templ}${templend}${between}${regionbegin}
		${out}
${regionend}</set>

  <writetext to="${=.scriptDirectory}\SetOfVariablesTempl.cs" text="${file}" />
</xsharper>