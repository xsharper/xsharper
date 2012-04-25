/****************************************************************
 This file is generated from XSharper (http://www.xsharper.com) script.

 Source script:      ${src} 
 Date:               ${date}
 XSharper version:   ${version}

 ***************************************************************
 DO NOT MODIFY CONTENTS OF THIS FILE. MODIFY THE SCRIPT INSTEAD.
 ****************************************************************/
${usings}
${assembly}
${headers}

namespace ${namespace} {

    // Generated XSharper script class
    public class ${class}
    {
        ${script} _script;
        public ${script} Script { get { return _script; } }
        
        public ${class}()
        {
            _script=${script-code};
        }
        #region -- Code snippets --
        ${snippets-code}
        #endregion -- Code snippets --
    }

    ${main}
}
