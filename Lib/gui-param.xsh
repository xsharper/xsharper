<?xml version="1.0" encoding="utf-8"?>
<xsharper xmlns="http://www.xsharper.com/schemas/1.0" unknownSwitches="true">
	<versionInfo title="gui-console" value="Run XSharper script requesting parameters in GUI." Version="0.1.0.0" Copyright="(C) 2009 DeltaX Inc." />
	<usage options="ifNoArguments default" />
	<param switch="run" required="false" value="Run immediately after loading" count="none" />	
	<param name="filename" required="true" value="Script to execute" description="filename.xsh" />
	<param name="args" required="false" value="Command line arguments for the script" count="multiple" description="arguments" last="true" />


	<include id="myScript" from="${filename}" dynamic="true" />
	<call subId="run-in-gui-param">
		<param>${=$~myScript.IncludedScript}</param>
		<param>${=.QuoteArgs(${args|=null})}</param>
	</call>

<sub id="run-in-gui-param">
	<param name="script" required="true" />
	<param name="scriptArgs" required="true" />

	<?_ Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		ExecutorForm f1=new ExecutorForm();
		f1.Context=c;
		if (c["script"] is XS.Script)
			f1.Script=(XS.Script)c["script"];
		else
			f1.Script=c.Find<XS.Script>(c.GetString("script"),true);

		f1.Args=c.GetString("scriptArgs");
		f1.Autorun=c.GetBool("run",false);
		Application.Run(f1);
	?>

<reference name="System.Windows.Forms" />
<reference name="System.Drawing" />

<?header using System;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using XS=XSharper.Core;
using System.Runtime.InteropServices;
using System.Threading;

public class ExecutorForm : Form
{
    private ManualResetEvent _running = new ManualResetEvent(false);
    private ManualResetEvent _stopEvent = new ManualResetEvent(false);
    private bool _closeOnStop = false;

    public XS.ScriptContext Context;
    public XS.Script Script;
    public string Args;
    public bool Autorun;

    public ExecutorForm()
    {
        InitializeComponent();
    }


    private void Form1_Load(object sender, EventArgs e)
    {
        KeyPreview = true;
        this.Icon = Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule.FileName);

        // Change font & tab stops
        reOut.LoadFonts();


        lblDescription.Text = Script.VersionInfo.GenerateInfo(Context, true);

        string title = Context.TransformStr(Script.VersionInfo.Title, Script.VersionInfo.Transform);
        if (string.IsNullOrEmpty(title))
            title = Script.Id;
        if (string.IsNullOrEmpty(title))
            title = Script.Location;
        if (string.IsNullOrEmpty(title))
            Text = "XSharper";
        else
            Text = title + " - XSharper";
        edArgs.Text = Args ?? "";
        edArgs.SelectionStart = 0;
        edArgs.SelectionLength = 0;

        // 
        CustomPropertyCollection c = new CustomPropertyCollection();
        string cat = null;
        int catNo = 0;
        foreach (XS.CommandLineParameter p in Script.Parameters)
        {
            if (string.IsNullOrEmpty(p.Var))
            {
                if (cat != null && !string.IsNullOrEmpty(p.Value))
                {
                    cat = p.Value;
                    catNo++;
                }
                continue;
            }
            if (cat == null) cat = "Parameters:";
            c.Add(createProperty(p, Context, string.Concat((char)31, (char)(catNo + 32), cat)));
        }

        propertyGrid.SelectedObject = c;
        propertyGrid.SetRatio(4);
        propertyGrid.PropertyValueChanged += delegate { updateCommandLine(); };
        edArgs.Validating += delegate(object snder, CancelEventArgs ex) { ex.Cancel = !updatePropertyGrid(); };
        updatePropertyGrid();

        if (Autorun)
			BeginInvoke((MethodInvoker)delegate() { click(); });
    }

    
    private static CustomProperty createProperty(XS.CommandLineParameter p, XS.ScriptContext context, string category)
    {
        CustomProperty ret = new CustomProperty();
        ret.DisplayName = p.GetDescription(context);
        ret.Description = p.GetTransformedValue(context);
        ret.Category = category;
        string typename = context.TransformStr(p.TypeName, p.Transform);
        if (p.Count == XS.CommandLineValueCount.Multiple)
            ret.PropertyType = typeof(string[]);
        else
            ret.PropertyType = string.IsNullOrEmpty(typename) ? typeof(string) : (context.FindType(typename) ?? typeof(string));
        
        if (p.Default != null)
            ret.Value = ret.DefaultValue = XS.Utils.To(ret.PropertyType, context.Transform(p.Default, p.Transform));
        else if (ret.PropertyType.IsValueType && !p.Required)
        {
            if (!(ret.PropertyType.IsGenericType && ret.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                ret.PropertyType = typeof (Nullable<>).MakeGenericType(ret.PropertyType);
        }
        if (ret.PropertyType == typeof(string))
        {
            List<string> possV = new List<string>();
            bool regex = false;
            if (!string.IsNullOrEmpty(p.Pattern) && p.Pattern.StartsWith("^(") && p.Pattern.EndsWith(")$"))
            {
                regex = true;
                foreach (string s in p.Pattern.Substring(2, p.Pattern.Length - 4).Split('|'))
                {
                    string un = System.Text.RegularExpressions.Regex.Unescape(s);
                    possV.Add(un);
                }
            }
            if (ret.DefaultValue != null)
                possV.Add(XS.Utils.To<string>(ret.DefaultValue));
            else if (!p.Required && !possV.Contains(null))
                possV.Add(null);
            if (p.Unspecified != null)
            {
                string v = XS.Utils.To<string>(context.Transform(p.Unspecified, p.Transform));
                if (!possV.Contains(v))
                    possV.Add(v);
            }
            if (possV.Count != 0)
            {
                ret.StandardValues = possV.ToArray();
                ret.OnlyStandardValues = (p.Count == XS.CommandLineValueCount.None || regex);
            }
        }
        ret.Tag = p;

        return ret;
    }

    bool updatePropertyGrid()
    {
        try
        {
            using (new XS.ScriptContextScope(Context))
            {
                XS.CommandLineParameters c = new XS.CommandLineParameters(Script.Parameters, Script.SwitchPrefixes, Script.UnknownSwitches);
                c.Parse(Context, XS.Utils.SplitArgs(edArgs.Text), false);
                c.ApplyDefaultValues(Context);
            }
            foreach (CustomProperty p in (CustomPropertyCollection)propertyGrid.SelectedObject)
            {
                XS.CommandLineParameter pp = (XS.CommandLineParameter)p.Tag;
                if (Context.IsSet(pp.Var))
                    p.Value = XS.Utils.To(p.PropertyType, Context[pp.Var]);
                else
                    p.Value = null;
            }
            propertyGrid.Refresh();
            return true;
        }
        catch (Exception ee)
        {
            MessageBox.Show(ee.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }
	static bool isDefault(CustomProperty p)
	{
		XS.CommandLineParameter pp = (XS.CommandLineParameter)p.Tag;
		return !(p.Value != null && (pp.Required || p.DefaultValue == null || !XS.Utils.To(p.PropertyType, p.Value).Equals(XS.Utils.To(p.PropertyType, p.DefaultValue))));
	}
    void updateCommandLine()
    {
        List<XS.ShellArg> args = new List<XS.ShellArg>();

		CustomPropertyCollection coll=(CustomPropertyCollection)propertyGrid.SelectedObject;
		for (int i=0;i<coll.Count;++i)
		{
			CustomProperty p =coll[i];
            XS.CommandLineParameter pp = (XS.CommandLineParameter)p.Tag;

            if (isDefault(p))
            {
				if (string.IsNullOrEmpty(pp.Switch))
				{
					for (int j=i+1;j<coll.Count;++j)	
						if (string.IsNullOrEmpty(((XS.CommandLineParameter)coll[j].Tag).Switch) && !isDefault(coll[j]))
						{
							XS.ShellArg a = new XS.ShellArg();
			                args.Add(a);
			                if (pp.Count != XS.CommandLineValueCount.None)
            			        a.Value = p.Value;
			                a.Transform = XS.TransformRules.None;
						}
				}
			}
			else
			{
                XS.ShellArg a = new XS.ShellArg();
                if (!string.IsNullOrEmpty(pp.Switch))
                {
                    if (!string.IsNullOrEmpty(this.Script.SwitchPrefixes))
                        a.Switch = Script.SwitchPrefixes[0] + pp.Switch;
					else
						a.Switch = pp.Switch;
                }
                if (pp.Count != XS.CommandLineValueCount.None)
                    a.Value = p.Value;
                a.Transform = XS.TransformRules.None;
                args.Add(a);
            }
        }

        edArgs.Text = XS.ShellArg.GetCommandLine(Context, args);
    }
    void btnRun_Click(object sender, EventArgs e)
    {
        click();
    }

    void click()
    {
        if (_running.WaitOne(0, false))
        {
            _stopEvent.Set();
            return;
        }
        Stopwatch sw = Stopwatch.StartNew();
        object ret = null;
        string oldText = btnRun.Text;
        string oldDir = Directory.GetCurrentDirectory();
        EventHandler<XS.OutputEventArgs> oldOut = Context.Output;
        EventHandler<XS.OperationProgressEventArgs> oldProgress = Context.Progress;

        try
        {
            _stopEvent.Reset();
            _running.Set();
            btnRun.Text = "&Cancel";
            btnRun.Update();
            reOut.Clear();

            Context.MinOutputType = chkDebug.Checked ? XS.OutputType.Debug : XS.OutputType.Info;
            Context.Output = OnOutput;
            Cursor = Cursors.WaitCursor;

            Context.Progress = delegate(object sender1, XS.OperationProgressEventArgs e1)
            {
                Application.DoEvents();
                if (_stopEvent.WaitOne(0, false))
                {
                    e1.Cancel = true;
                    _stopEvent.Reset();
                }
            };

            using (XS.ConsoleRedirector r = new XS.ConsoleRedirector(Context))
            {
                Context.In = TextReader.Null;
                ret = Context.ExecuteScript(Script, XS.Utils.SplitArgs(edArgs.Text), XS.CallIsolation.High);
            }
        }
        catch (Exception ex)
        {
            Context.WriteException(ex);
            ret = -1;
        }


        Directory.SetCurrentDirectory(oldDir);
        _running.Reset();
        btnRun.Text = oldText;
        Context.Info.WriteLine("--- Completed in " + sw.Elapsed + " with return value=" + ret + " ---");
        reOut.ScrollToBottom();
        Context.Progress = oldProgress;
        Context.Output = oldOut;
        Cursor = Cursors.Arrow;
        Context.Progress = null;
        Context.Output = null;
        if (_closeOnStop)
            Close();
    }



    private void OnOutput(object sender1, XS.OutputEventArgs e1)
    {
        reOut.Output(e1.OutputType, e1.Text);
    }
    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (_running.WaitOne(0, false))
        {
            Text = "Cancelling script...";
            _stopEvent.Set();
            _closeOnStop = true;
            e.Cancel = true;

            return;
        }
    }



    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.splitContainer1 = new System.Windows.Forms.SplitContainer();
        this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
        this.label2 = new System.Windows.Forms.Label();
        this.labelGrid = new System.Windows.Forms.Label();
        this.propertyGrid = new PropertyGridEx();
        this.btnRun = new System.Windows.Forms.Button();
        this.chkDebug = new System.Windows.Forms.CheckBox();
        this.lblDescription = new System.Windows.Forms.Label();
        this.label1 = new System.Windows.Forms.Label();
        this.edArgs = new System.Windows.Forms.TextBox();
        this.reOut = new OutputRichTextBox();
        this.splitContainer1.Panel1.SuspendLayout();
        this.splitContainer1.Panel2.SuspendLayout();
        this.splitContainer1.SuspendLayout();
        this.tableLayoutPanel1.SuspendLayout();
        this.SuspendLayout();
        // 
        // splitContainer1
        // 
        this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
        this.splitContainer1.Location = new System.Drawing.Point(0, 0);
        this.splitContainer1.Name = "splitContainer1";
        this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
        // 
        // splitContainer1.Panel1
        // 
        this.splitContainer1.Panel1.Controls.Add(this.tableLayoutPanel1);
        // 
        // splitContainer1.Panel2
        // 
        this.splitContainer1.Panel2.Controls.Add(this.reOut);
        this.splitContainer1.Size = new System.Drawing.Size(800, 550);
        this.splitContainer1.SplitterDistance = 390;
        this.splitContainer1.SplitterWidth = 5;
        this.splitContainer1.TabIndex = 0;
        // 
        // tableLayoutPanel1
        // 
        this.tableLayoutPanel1.ColumnCount = 3;
        this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
        this.tableLayoutPanel1.Controls.Add(this.label2, 0, 0);
        this.tableLayoutPanel1.Controls.Add(this.labelGrid, 0, 1);
        this.tableLayoutPanel1.Controls.Add(this.propertyGrid, 1, 1);
        this.tableLayoutPanel1.Controls.Add(this.btnRun, 2, 1);
        this.tableLayoutPanel1.Controls.Add(this.chkDebug, 2, 0);
        this.tableLayoutPanel1.Controls.Add(this.lblDescription, 1, 0);
        this.tableLayoutPanel1.Controls.Add(this.label1, 0, 2);
        this.tableLayoutPanel1.Controls.Add(this.edArgs, 1, 2);
        this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
        this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
        this.tableLayoutPanel1.Name = "tableLayoutPanel1";
        this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(3);
        this.tableLayoutPanel1.RightToLeft = System.Windows.Forms.RightToLeft.No;
        this.tableLayoutPanel1.RowCount = 3;
        this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 80F));
        this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this.tableLayoutPanel1.Size = new System.Drawing.Size(800, 390);
        this.tableLayoutPanel1.TabIndex = 0;
        // 
        // label2
        // 
        this.label2.Location = new System.Drawing.Point(6, 3);
        this.label2.Name = "label2";
        this.label2.Size = new System.Drawing.Size(100, 23);
        this.label2.TabIndex = 0;
        this.label2.Text = "Script description:";
        this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // labelGrid
        // 
        this.labelGrid.Location = new System.Drawing.Point(6, 26);
        this.labelGrid.Name = "labelGrid";
        this.labelGrid.Size = new System.Drawing.Size(100, 23);
        this.labelGrid.TabIndex = 2;
        this.labelGrid.Text = "&Parameters:";
        this.labelGrid.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // propertyGrid
        // 
        this.propertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
        this.propertyGrid.Location = new System.Drawing.Point(112, 29);
        this.propertyGrid.Name = "propertyGrid";
        this.propertyGrid.Size = new System.Drawing.Size(601, 329);
        this.propertyGrid.TabIndex = 3;
        this.propertyGrid.ToolbarVisible = false;
        // 
        // btnRun
        // 
        this.btnRun.DialogResult = System.Windows.Forms.DialogResult.OK;
        this.btnRun.Dock = System.Windows.Forms.DockStyle.Top;
        this.btnRun.Location = new System.Drawing.Point(719, 29);
        this.btnRun.Name = "btnRun";
        this.btnRun.Size = new System.Drawing.Size(75, 21);
        this.btnRun.TabIndex = 7;
        this.btnRun.Text = "&Run";
        this.btnRun.UseVisualStyleBackColor = true;
        this.btnRun.Click += new System.EventHandler(this.btnRun_Click);
        // 
        // chkDebug
        // 
        this.chkDebug.AutoSize = true;
        this.chkDebug.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.chkDebug.Location = new System.Drawing.Point(719, 6);
        this.chkDebug.Name = "chkDebug";
        this.chkDebug.Size = new System.Drawing.Size(75, 17);
        this.chkDebug.TabIndex = 6;
        this.chkDebug.Text = "&Debug";
        this.chkDebug.UseVisualStyleBackColor = true;
        // 
        // lblDescription
        // 
        this.lblDescription.AccessibleRole = System.Windows.Forms.AccessibleRole.Document;
        this.lblDescription.AutoSize = true;
        this.lblDescription.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
        this.lblDescription.Dock = System.Windows.Forms.DockStyle.Fill;
        this.lblDescription.Location = new System.Drawing.Point(112, 3);
        this.lblDescription.Name = "lblDescription";
        this.lblDescription.Size = new System.Drawing.Size(601, 23);
        this.lblDescription.TabIndex = 1;
        this.lblDescription.Text = "...";
        this.lblDescription.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // label1
        // 
        this.label1.Location = new System.Drawing.Point(6, 361);
        this.label1.Name = "label1";
        this.label1.Size = new System.Drawing.Size(95, 26);
        this.label1.TabIndex = 4;
        this.label1.Text = "&Command line:";
        this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        // 
        // edArgs
        // 
        this.edArgs.Dock = System.Windows.Forms.DockStyle.Fill;
        this.edArgs.Location = new System.Drawing.Point(112, 364);
        this.edArgs.Name = "edArgs";
        this.edArgs.Size = new System.Drawing.Size(601, 20);
        this.edArgs.TabIndex = 5;
        // 
        // reOut
        // 
        this.reOut.BackColor = System.Drawing.Color.Black;
        this.reOut.Dock = System.Windows.Forms.DockStyle.Fill;
        this.reOut.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
        this.reOut.ForeColor = System.Drawing.Color.White;
        this.reOut.Location = new System.Drawing.Point(0, 0);
        this.reOut.Name = "reOut";
        this.reOut.ReadOnly = true;
        this.reOut.RightToLeft = System.Windows.Forms.RightToLeft.No;
        this.reOut.Size = new System.Drawing.Size(800, 155);
        this.reOut.TabIndex = 0;
        this.reOut.Text = "";
        // 
        // ExecutorForm
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(800, 550);
        this.Controls.Add(this.splitContainer1);
        this.Name = "ExecutorForm";
        this.Text = " Sample Script Executor";
        this.Load += new System.EventHandler(this.Form1_Load);
        this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
        this.splitContainer1.Panel1.ResumeLayout(false);
        this.splitContainer1.Panel2.ResumeLayout(false);
        this.splitContainer1.ResumeLayout(false);
        this.tableLayoutPanel1.ResumeLayout(false);
        this.tableLayoutPanel1.PerformLayout();
        this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
    private OutputRichTextBox reOut;
    private System.Windows.Forms.Button btnRun;
    private System.Windows.Forms.CheckBox chkDebug;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.TextBox edArgs;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label labelGrid;
    private System.Windows.Forms.Label lblDescription;
    private PropertyGridEx propertyGrid;
    private System.Windows.Forms.SplitContainer splitContainer1;

}

// Extended richedit box, that supports different fonts and backspace character
public class OutputRichTextBox : RichTextBox
{
    private Font _font, _fontBold;

    [DllImport("User32.dll", CharSet = CharSet.Auto)]
    private static extern int SendMessage(IntPtr h, int msg, int wParam, int lParam);

    private Font createFont(bool bold)
    {
        Font f = new Font("Consolas", 9, bold ? FontStyle.Bold : FontStyle.Regular);
        if (f.Name != "Consolas")
            f = new Font("Courier New", 9, bold ? FontStyle.Bold : FontStyle.Regular);
        return f;
    }

    public void LoadFonts()
    {
        _font = createFont(false);
        _fontBold = createFont(true);
        this.Font = _font;
    }
    public void ScrollToBottom()
    {
        SendMessage(this.Handle, 0x115, 7, 0);
    }

    public void Output(XS.OutputType otype, string text)
    {
        this.Select(this.TextLength, 0);
        StringBuilder sb = new StringBuilder();
        foreach (char ch in text)
        {
            if (ch == (char)8)
            {
                if (sb.Length > 0)
                    this.AppendText(sb.ToString());
                while (this.TextLength > 0)
                {
                    this.Select(this.TextLength - 1, 1);
                    string s = this.SelectedText;
                    if (s == "\n" || s == "\r" || s == "")
                    {
                        this.Select(this.TextLength, 0);
                        break;
                    }
                    this.Select(this.TextLength - 1, 1);
                    this.ReadOnly = false;
                    this.SelectedText = string.Empty;
                    this.ReadOnly = true;
                }
                sb.Length = 0;
            }
            else
            {
                if (sb.Length == 0)
                {
                    switch (otype)
                    {
                        case XS.OutputType.Debug: this.SelectionColor = Color.Cyan; break;
                        case XS.OutputType.Error: this.SelectionColor = Color.Yellow; break;
                        case XS.OutputType.Info: this.SelectionColor = Color.LightGreen; break;
                        case XS.OutputType.Bold: this.SelectionColor = Color.White; break;
                        default: this.SelectionColor = Color.LightGray; break;
                    }
                    if (otype == XS.OutputType.Bold)
                    {
                        if (this.SelectionFont.Bold != true)
                            this.SelectionFont = _fontBold;
                    }
                    else if (this.SelectionFont.Bold != false)
                        this.SelectionFont = _font;
                }
                sb.Append(ch);
                if (sb.Length > 5000)
                {
                    this.AppendText(sb.ToString());
                    sb.Length = 0;
                }

            }
        }
        this.AppendText(sb.ToString());

        ScrollToBottom();
        Application.DoEvents();
    }
}

#region -- Extended property grid --
public class PropertyGridEx : PropertyGrid
{
    private const int WM_KEYDOWN = 0x100;
    private const int TAB = 9;
    private bool _setEvent;
    private double _ratio;
    private Control _propertyGridView;

    public PropertyGridEx()
    {
    }

    protected override bool ProcessKeyPreview(ref Message m)
    {
        if (m.Msg == WM_KEYDOWN && m.WParam.ToInt32() == TAB)
        {
            bool forward = (ModifierKeys & Keys.Shift) == 0;
            if (moveSelectedGridItem(forward))
                return true;
        }
        return ProcessKeyEventArgs(ref m);
    }

    protected override bool ProcessTabKey(bool forward)
    {
        moveSelectedGridItem(forward);
        return true;
    }

    public void SetRatio(double ratio)
    {
        _ratio = ratio;
        _propertyGridView = Controls[2];
        if (!_setEvent)
        {
            _setEvent = true;
            _propertyGridView.VisibleChanged += delegate { setRatio(); };
            _propertyGridView.SizeChanged += delegate { setRatio(); };
        }
        setRatio();

    }

    private void setRatio()
    {
        try
        {
            if (_propertyGridView != null)
            {
                Type propertyGridViewType = _propertyGridView.GetType();
                System.Reflection.FieldInfo fldLabelRatio = propertyGridViewType.GetField("labelRatio", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (fldLabelRatio != null)
                {
                    fldLabelRatio.SetValue(_propertyGridView, _ratio);
                    Invalidate();
                }
            }
        }
        catch
        {

        }
    }


    private bool moveSelectedGridItem(bool forward)
    {
        if (SelectedGridItem == null || SelectedGridItem.Parent == null)
            return false;
        GridItem p = SelectedGridItem.Parent.Parent;
        GridItemCollection allItems = SelectedGridItem.Parent.GridItems;
        int currentIndex = -1;
        for (int i = 0; i < allItems.Count; i++)
            if (allItems[i] == SelectedGridItem)
            {
                currentIndex = i;
                break;
            }
        if (forward)
        {
            if (currentIndex >= 0 && currentIndex < allItems.Count - 1)
            {
                SelectedGridItem = allItems[currentIndex + 1];
                return false;
            }
            if (currentIndex == allItems.Count - 1 && p != null)
            {
                GridItemCollection pitems = p.GridItems;
                for (int j = 0; j < pitems.Count - 1; j++)
                    if (pitems[j] == SelectedGridItem.Parent)
                    {
                        SelectedGridItem = pitems[j + 1].GridItems[0];
                        return false;
                    }
                Parent.SelectNextControl(this, forward, true, false, true);
                return false;
            }
        }
        else
        {
            if (currentIndex == 0 && p != null)
            {
                GridItemCollection pitems = p.GridItems;
                for (int j = 1; j < pitems.Count; j++)
                    if (pitems[j] == SelectedGridItem.Parent)
                    {
                        SelectedGridItem = pitems[j - 1].GridItems[pitems[j - 1].GridItems.Count - 1];
                        return false;
                    }

                Parent.SelectNextControl(this, forward, true, false, true);
            }
            else if (currentIndex >= 1 && currentIndex < allItems.Count)
            {
                SelectedGridItem = allItems[currentIndex - 1];
                return true;
            }
        }
        return true;
    }
}

public class CustomPropertyCollection : List<CustomProperty>, ICustomTypeDescriptor
{
    public AttributeCollection GetAttributes() { return TypeDescriptor.GetAttributes(this, true); }
    public string GetClassName() { return TypeDescriptor.GetClassName(this, true); }
    public string GetComponentName() { return TypeDescriptor.GetComponentName(this, true); }
    public TypeConverter GetConverter() { return new PropertySorter(ConvertAll(delegate(CustomProperty x) { return x.DisplayName; }).ToArray()); }
    public EventDescriptor GetDefaultEvent() { return TypeDescriptor.GetDefaultEvent(this, true); }
    public PropertyDescriptor GetDefaultProperty() { return TypeDescriptor.GetDefaultProperty(this, true); }
    public object GetEditor(System.Type editorBaseType) { return TypeDescriptor.GetEditor(this, editorBaseType, true); }
    public EventDescriptorCollection GetEvents() { return TypeDescriptor.GetEvents(this, true); }
    public EventDescriptorCollection GetEvents(System.Attribute[] attributes) { return TypeDescriptor.GetEvents(this, attributes, true); }
    public PropertyDescriptorCollection GetProperties() { return GetProperties(null); }
    public object GetPropertyOwner(PropertyDescriptor pd) { return this; }
    public PropertyDescriptorCollection GetProperties(System.Attribute[] attributes)
    {
        return new PropertyDescriptorCollection(ConvertAll(delegate(CustomProperty x) { return x.CreateDescriptor(); }).ToArray());
    }

    #region -- private classes --
    class PropertySorter : ExpandableObjectConverter
    {
        private readonly string[] _names;
        public PropertySorter(string[] names) { _names = names; }
        public override bool GetPropertiesSupported(ITypeDescriptorContext context) { return true; }
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            return TypeDescriptor.GetProperties(value, attributes).Sort(_names);
        }
    }
    #endregion
}

public class CustomProperty
{
    public object Tag ;
    public string DisplayName ;
    public bool IsReadOnly ;
    public object Value ;
    public string Description ;
    public string Category ;
    public bool Parenthesize ;
    public bool IsPassword ;
    public bool IsPercentage ;
    public Type PropertyType ;
    public object DefaultValue ;
    public object[] StandardValues ;
    public bool OnlyStandardValues ;

    public PropertyDescriptor CreateDescriptor()
    {
        List<Attribute> attrs = new List<Attribute>();
        if (IsPassword) attrs.Add(new PasswordPropertyTextAttribute(true));
        if (Parenthesize) attrs.Add(new ParenthesizePropertyNameAttribute(true));
        FlagsAttribute[] a = (FlagsAttribute[])PropertyType.GetCustomAttributes(typeof(FlagsAttribute), true);
        if (PropertyType != null && a.Length > 0) attrs.Add(new EditorAttribute(typeof(FlagsEditor), typeof(UITypeEditor)));
        if (IsPercentage) attrs.Add(new TypeConverterAttribute(typeof(OpacityConverter)));
        if (PropertyType == typeof(string)) attrs.Add(new TypeConverterAttribute(typeof(List2PropertyConverter)));
        if (DefaultValue == null)
            attrs.Add(new DefaultValueAttribute(DefaultValue));
        return new CustomPropertyDescriptor(this, attrs.ToArray());
    }

    #region == private adapter classes==
    public class CustomPropertyDescriptor : PropertyDescriptor
    {
        private CustomProperty _property;
        public CustomPropertyDescriptor(CustomProperty myProperty, Attribute[] attrs)
            : base(myProperty.DisplayName, attrs)
        {
            _property = myProperty;
        }
        public CustomProperty Property { get { return _property; } }
        public override Type ComponentType { get { return GetType(); } }
        public override object GetValue(object component) { return Property.Value; }
        public override bool IsReadOnly { get { return Property.IsReadOnly; } }
        public override Type PropertyType { get { return Property.PropertyType; } }
        public override string Description { get { return Property.Description; } }
        public override string Category { get { return Property.Category; } }
        public override string DisplayName { get { return Property.DisplayName; } }
        public override bool IsBrowsable { get { return false; } }
        public override bool CanResetValue(object component)
        {
            return (Property.DefaultValue != null);
        }
        public override void ResetValue(object component)
        {
            Property.Value = Property.DefaultValue;
            OnValueChanged(component, EventArgs.Empty);
        }
        public override void SetValue(object component, object value)
        {
            Property.Value = value;
            OnValueChanged(component, EventArgs.Empty);
        }

        public override bool ShouldSerializeValue(object component)
        {
            object oValue = Property.Value;
            if ((Property.DefaultValue != null) && (oValue != null))
                return !oValue.Equals(Property.DefaultValue);
            return false;
        }

    }
    internal class List2PropertyConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            CustomPropertyDescriptor cp = context.PropertyDescriptor as CustomPropertyDescriptor;
            return (cp != null && cp.Property.StandardValues != null);
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            CustomPropertyDescriptor cp = context.PropertyDescriptor as CustomPropertyDescriptor;
            return (cp != null && cp.Property.OnlyStandardValues);
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            CustomPropertyDescriptor cp = context.PropertyDescriptor as CustomPropertyDescriptor;
            return new StandardValuesCollection(cp.Property.StandardValues);
        }
    }
    internal class FlagsEditor : UITypeEditor
    {
        class ComboItem
        {
            public ComboItem(string displayName, ulong value, string tooltip)
            {
                DisplayName = displayName;
                Value = value;
                Tooltip = tooltip;
            }

            public ulong Value;
            public string Tooltip;
            public string DisplayName;
            public override string ToString() { return DisplayName; }
        }
        private System.Windows.Forms.Design.IWindowsFormsEditorService _edSvc = null;
        private CheckedListBox _clb;
        private ToolTip _tooltipControl;
        private bool _handleLostfocus = false;

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (context == null || context.Instance == null || provider == null)
                return value;
            _edSvc = (System.Windows.Forms.Design.IWindowsFormsEditorService)provider.GetService(typeof(System.Windows.Forms.Design.IWindowsFormsEditorService));
            if (_edSvc == null)
                return value;

            // Create a CheckedListBox and populate it with all the enum values
            _clb = new CheckedListBox();
            _clb.Sorted = false;
            _clb.BorderStyle = BorderStyle.FixedSingle;
            _clb.CheckOnClick = true;
            _clb.MouseDown += OnMouseDown;
            _clb.MouseMove += OnMouseMoved;
            _clb.ItemCheck += OnItemCheck;
            _tooltipControl = new ToolTip();
            _tooltipControl.ShowAlways = true;


            ulong intEdited = (ulong)Convert.ChangeType(value ?? 0, typeof(ulong));
            foreach (string name in Enum.GetNames(context.PropertyDescriptor.PropertyType))
            {
                object enumVal = Enum.Parse(context.PropertyDescriptor.PropertyType, name);
                ulong enumNumValue = (ulong)Convert.ChangeType(enumVal, typeof(ulong));

                System.Reflection.FieldInfo fi = context.PropertyDescriptor.PropertyType.GetField(name);
                DescriptionAttribute[] attrs = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
                string tooltip = attrs.Length > 0 ? attrs[0].Description : string.Empty;

                CheckState cs = CheckState.Indeterminate;
                if ((intEdited & enumNumValue) == 0)
                    cs = CheckState.Unchecked;
                else if ((intEdited & enumNumValue) == enumNumValue)
                    cs = CheckState.Checked;

                _clb.Items.Add(new ComboItem(name, enumNumValue, tooltip), cs);
            }
            if (_clb.Items.Count > 4)
            {
                _clb.Height = _clb.ItemHeight * Math.Min(_clb.Items.Count + 1, 10);
            }

            // Show our CheckedListbox as a DropDownControl. 
            // This methods returns only when the dropdowncontrol is closed
            _edSvc.DropDownControl(_clb);

            // Get the sum of all checked flags
            ulong result = GetResult(-1, CheckState.Indeterminate);

            // return the right enum value corresponding to the result
            return Enum.ToObject(context.PropertyDescriptor.PropertyType, result);
        }

        ulong GetResult(int nv, CheckState cs)
        {
            ulong result = 0;
            for (int i = 0; i < _clb.Items.Count; ++i)
            {
                CheckState c = (i == nv) ? cs : _clb.GetItemCheckState(i);
                if (c == CheckState.Checked)
                    result |= ((ComboItem)_clb.Items[i]).Value;
            }
            return result;
        }

        private volatile int _loopCheck = 0;
        private void OnItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_loopCheck++ == 0)
            {
                ulong result = GetResult(e.Index, e.NewValue);
                for (int i = 0; i < _clb.Items.Count; ++i)
                {
                    ulong v = ((ComboItem)_clb.Items[i]).Value;
                    CheckState cs = CheckState.Indeterminate;
                    if ((result & v) == 0)
                        cs = CheckState.Unchecked;
                    else if ((result & v) == v)
                        cs = CheckState.Checked;
                    _clb.SetItemCheckState(i, cs);
                }
            }
            _loopCheck--;
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) { return UITypeEditorEditStyle.DropDown; }
        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (!_handleLostfocus && _clb.ClientRectangle.Contains(_clb.PointToClient(new Point(e.X, e.Y))))
            {
                _clb.LostFocus += ValueChanged;
                _handleLostfocus = true;
            }
        }
        private void OnMouseMoved(object sender, MouseEventArgs e)
        {
            int index = _clb.IndexFromPoint(e.X, e.Y);
            if (index >= 0)
                _tooltipControl.SetToolTip(_clb, ((ComboItem)_clb.Items[index]).Tooltip);
        }
        private void ValueChanged(object sender, EventArgs e)
        {
            if (_edSvc != null)
                _edSvc.CloseDropDown();
        }
    }
    #endregion
}
#endregion

	
?>
</sub>
<Signature xmlns="http://www.w3.org/2000/09/xmldsig#"><SignedInfo><CanonicalizationMethod Algorithm="http://www.w3.org/2001/10/xml-exc-c14n#WithComments"><InclusiveNamespaces PrefixList="Sign" xmlns="http://www.w3.org/2001/10/xml-exc-c14n#" /></CanonicalizationMethod><SignatureMethod Algorithm="http://www.w3.org/2000/09/xmldsig#rsa-sha1" /><Reference URI=""><Transforms><Transform Algorithm="http://www.w3.org/2000/09/xmldsig#enveloped-signature" /></Transforms><DigestMethod Algorithm="http://www.w3.org/2000/09/xmldsig#sha1" /><DigestValue>CeDFdE6R9HjESQMzXrB54y+fILo=</DigestValue></Reference></SignedInfo><SignatureValue>b88hntUDUb/7Tsdul43YQa5EXHHrydyTDFoELbg9kki9qbjZWJn+GwQh6LL8+/hhE4Nr9EpPwK2zQwf+A21r3cFHjaMGvre/hesSieKaWnDiP6Upy6unyQsQ+HlUGC8pewFyC+Pj3Tv2HThDtq4uKACuuSmerUx7FAH9/7qCBDs=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>oCKTg0Lq8MruXHnFdhgJA8hS98P5rJSABfUFHicssx0mltfqeuGsgzzpk8gLpNPkmJV+ca+pqPILiyNmMfLnTg4w99zH3FRNd6sIoN1veU87OQ5a0Ren2jmlgAAscHy2wwgjxx8YuP/AIfROTtGVaqVT+PhSvl09ywFEQ+0vlnk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></xsharper>