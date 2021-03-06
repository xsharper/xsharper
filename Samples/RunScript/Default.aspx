﻿<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="RunScript._Default" ValidateRequest="false" EnableSessionState="True"  EnableViewState="true" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title></title>
    <style type="text/css">
        body { font-family: Segoe UI,Tahoma,Arial,Helvetica; font-size: small; margin-left:auto;margin-right:auto; width:80%;}
        .Error { color:Red;}
        .Info { color:Green;}
        .Debug { color:Blue;}
        .Bold { font-weight: bold;}
        .normalStatus { color:Black;}
        .errorStatus { color:Red; }
        #formTable { width:100%;  }
        .scriptBody { font-family: Consolas,Courier New,Courier; font-size: small; width:100%;}
        #output { font-family: Consolas,Courier New,Courier; background: #eee; font-size: small; max-height: 30em; height: 30em; overflow:scroll;}
    </style>
    <script type="text/javascript">
        var timer = null;
        var refreshPeriod = 1000;
        var callCount = 0;
        function pageLoad() {
            var btn = $get('<%=btnStop.ClientID %>');
            if (!btn.disabled)
                timer = setTimeout(updateStatus, refreshPeriod);
            callCount = 0;
        }

        function updateStatus() {
            var jid = $get('<%=hfJobId.ClientID %>');
            if (timer)
                clearTimeout(timer);
            PageMethods.GetUpdate(jid.value, statusUpdated, onFail);
        }

        function enableControls() {
            $get('<%=btnStop.ClientID %>').disabled = true;
            $get('<%=btnRun.ClientID %>').disabled = false;
            if ($get('<%=tbScript.ClientID %>'))
                $get('<%=tbScript.ClientID %>').readOnly = false;
        }
        function statusUpdated(result, userContext, methodName) {
            var output = $get('<%=output.ClientID %>');
            var received = $get('<%=received.ClientID %>');
            received.className = 'normalStatus';
            output.innerHTML += result.HtmlUpdate;

            var r;
            if (received.innerHTML)
                r = parseInt(received.innerHTML, 10) + result.HtmlUpdate.length;
            else
                r = result.HtmlUpdate.length;
            received.innerHTML = r + " characters received in " + (++callCount) + " calls. Last update: "+("0" + time.getHours()).slice(-2)   + ":" + 
			    ("0" + time.getMinutes()).slice(-2) + ":" + 
			    ("0" + time.getSeconds()).slice(-2);

            
            output.scrollTop = output.scrollHeight;
            if (timer)
                clearTimeout(timer);
            if (result.IsCompleted)
                enableControls();
            else
                timer = setTimeout(updateStatus, refreshPeriod);
        }

        function stop() {
            var jid = $get('<%=hfJobId.ClientID %>');
            PageMethods.Stop(jid.value);
        }


        function onFail(error) {
            var received = $get('<%=received.ClientID %>');
            var message=error.get_message()
                                .replace(/&/g, '&amp;')
                                .replace(/</g, '&lt;')
                                .replace(/>/g, '&gt;');

            var now = new Date();
            var hours = now.getHours();
            var minutes = now.getMinutes();
            var seconds = now.getSeconds();
            var timeValue = "" + ((hours > 12) ? hours - 12 : hours);
            if (timeValue == "0") timeValue = 12;
            timeValue += ((minutes < 10) ? ":0" : ":") + minutes;
            timeValue += ((seconds < 10) ? ":0" : ":") + seconds;
            timeValue += (hours >= 12) ? " P.M." : " A.M.";
            
            received.innerHTML = timeValue +" : "+message;
            received.className = 'errorStatus';
            timer = setTimeout(updateStatus, refreshPeriod);
        }
    </script>
</head>
<body>
    <form id="form1" runat="server">
    <asp:ScriptManager EnablePageMethods="true" runat="server" ID="scriptManager" />
    <div>
        <table border="0" id="formTable">
            <tr><td colspan="2">
                <asp:Label runat="server" ID="lblScript" AssociatedControlID="tbScript">Script:</asp:Label> <br />
                <asp:DropDownList runat="server" ID="cbScript" spellcheck='false'/>
                <asp:TextBox runat="server" ID="tbScript" Rows="20" TextMode="MultiLine" CssClass='scriptBody'  EnableViewState="false" spellcheck='false'/>
            </td></tr>
            <tr><td colspan="2">
                <asp:Label runat="server" ID="Label1" AssociatedControlID="tbArguments" spellcheck='false'>Arguments:</asp:Label> <br />
                <asp:TextBox runat="server" ID="tbArguments" CssClass='scriptBody'  EnableViewState="false"/>
            </td></tr>
            <tr>
            <td align="left"><asp:CheckBox id="cbDebug" runat="server" Text="Debug mode"/> </td>
            <td align="right"><asp:Button runat="server" ID="btnRun" Text="Run" onclick="btnRun_Click" UseSubmitBehavior="true" CausesValidation="true" /></td>
            </tr>
        </table>
        
        <asp:HiddenField ID="hfJobId" runat="server" />
        <br />
        <br />
        <label for="output">Output:</label>&nbsp;<asp:Button runat="server" ID="btnStop" Text="Stop"  Enabled="false" OnClientClick="stop();return false;" CausesValidation="false" UseSubmitBehavior="false"/>
        <div id="output" runat="server" Enableviewstate="false">Click "Run" to execute script</div>
        <p id="received" runat="server"></p>

    </div>
    </form>
</body>
</html>
