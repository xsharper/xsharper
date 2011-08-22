<?xml version="1.0" encoding="utf-8"?>
<xsharper xmlns="http://www.xsharper.com/schemas/1.0" id="gui_menu">
    <usage options="none" />
    <throw>This script must be executed only from another script</throw>
    
<sub id="gui-menu">
    <param name="title" />
    <param name="description" />
    <param name="scriptNames" required="true" />

<?_ Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false); 
    ScriptMenu f1=new ScriptMenu();
    f1.Context=c;
    f1.Title=c.GetString("title");
    f1.Description=c.GetString("description");
    foreach (string s in c.GetString("scriptNames").Split(';'))
        f1.Scripts.Add(c.Find<Script>(s,true));
    
    Application.Run(f1);
?>


<reference name="System.Windows.Forms" />
<reference name="System.Drawing" />
<?header using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Threading;
    using System.Windows.Forms;
    using XSharper.Core;
    using System.Runtime.InteropServices;


    public partial class ScriptMenu : Form
    {
        private ManualResetEvent _running = new ManualResetEvent(false);
        private ManualResetEvent _stopEvent = new ManualResetEvent(false);
        private bool _closeOnStop = false;
        
        public ScriptContext Context;
        public List<Script> Scripts = new List<Script>();
        public string Description;
        public string Title;
        
        private List<Button> _buttons=new List<Button>();
        private Button btnExit;
        public ScriptMenu()
        {
            InitializeComponent();

        }
       
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr h, int msg, int wParam, int lParam);



        private void Form1_Load(object sender, EventArgs e)
        {
            this.Icon=Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule.FileName);
            layButtons.RowCount = Scripts.Count + 2;
            for (int i = 0; i < Scripts.Count + 1; ++i)
            {

                Button b = new Button();
                b.Anchor = (AnchorStyles.Left | AnchorStyles.Right);
                b.Margin = new System.Windows.Forms.Padding(13, 3, 13, 3);
                b.Size = new System.Drawing.Size(260, 23);
                b.AutoSize=true;
                b.TabIndex = i;
                b.UseVisualStyleBackColor = true;
                if (i == Scripts.Count)
                {
                    b.DialogResult = DialogResult.Cancel;
                    CancelButton = b;
                    b.Text = "Exit";
                    b.Click += delegate { if (_running.WaitOne(0,false)) _stopEvent.Set(); else Close(); };
                    btnExit = b;
                    b.Anchor = (AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom);
                    b.Dock = DockStyle.Bottom;
                }
                else
                {
                    var script = Scripts[i];
                    b.Text = script.VersionInfo.Title;
                    b.Click += delegate { Run(script,b); };
                    _buttons.Add(b);
                }
                layButtons.Controls.Add(b, 0, i+1);
            }

            // Change font & tab stops
            reOut.LoadFonts();
            lblDescription.Text = Description;
            Text = Title+" - XSharper";            

            this.status.ResumeLayout(false);
            this.status.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }


        private void Run(Script script, Button b)
        {
            if (_running.WaitOne(0,false))
            {
                _stopEvent.Set();
                return;
            }
            Stopwatch sw = Stopwatch.StartNew();
            object ret = "";
            string oldText = btnExit.Text;
            string oldDir = Directory.GetCurrentDirectory();
            EventHandler<OutputEventArgs> oldOut=Context.Output;
            EventHandler<OperationProgressEventArgs> oldProgress=Context.Progress;
            try
            {
                _stopEvent.Reset();
                _running.Set();
                btnExit.Text = "&Cancel";
                btnExit.Update();
                reOut.Clear();
                foreach (var button in _buttons)
                    button.Enabled = false;

                Context.Output= OnOutput;
                Cursor = Cursors.WaitCursor;
                Context.Progress= OnProgress;
                

                statusText.Text = "Executing " + script.Id+"...";
                Context.Info.WriteLine("--- Executing " + script.Id +" ---");
                using (XS.ConsoleRedirector r=new XS.ConsoleRedirector(Context)) 
                {
                    Context.In=TextReader.Null;
                    ret = Context.ExecuteScript(script, null, CallIsolation.High);
                }
            }
            catch(Exception ex)
            {
                Context.WriteException(ex);
                statusText.Text = "Error: " + ex.Message;
                ret = -1;
            }
            finally 
    {
                Context.Info.WriteLine("--- Completed in "+sw.Elapsed+" with return value="+ret+" ---");
                Directory.SetCurrentDirectory(oldDir);
                _running.Reset();
                reOut.ScrollToBottom();
                foreach (var button in _buttons)
                    button.Enabled = true;
                b.Focus();
                btnExit.Text = oldText;
                
                Context.Progress=oldProgress;
                Context.Output=oldOut;
                        
                statusText.Text = "Completed in " + sw.Elapsed + ", return value=" + ret;
                Cursor = Cursors.Arrow;
                if (_closeOnStop)
                    Close();
            }
        }

        private void OnProgress(object sender1, OperationProgressEventArgs e1)
        {
            Application.DoEvents();
            if (_stopEvent.WaitOne(0,false))
            {
                e1.Cancel = true;
                _stopEvent.Reset();
            }
        }
        
        private void OnOutput(object sender1, OutputEventArgs e1)
        {
            reOut.Output(e1.OutputType,e1.Text);
        }

        
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_running.WaitOne(0,false))
            {
                Text = "Cancelling script before exit...";
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
            this.status = new System.Windows.Forms.StatusStrip();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.reOut = new OutputRichTextBox();
            this.layButtons = new System.Windows.Forms.TableLayoutPanel();
            this.lblDescription = new System.Windows.Forms.Label();
            this.statusText = new System.Windows.Forms.ToolStripStatusLabel();
            this.status.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // status
            // 
            this.status.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusText});
            this.status.Location = new System.Drawing.Point(0, 251);
            this.status.Name = "status";
            this.status.Size = new System.Drawing.Size(734, 22);
            this.status.TabIndex = 1;
            this.status.Text = "statusStrip1";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 80F));
            this.tableLayoutPanel1.Controls.Add(this.reOut, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.layButtons, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.lblDescription, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(734, 251);
            this.tableLayoutPanel1.TabIndex = 2;
            // 
            // reOut
            // 
            this.reOut.BackColor = System.Drawing.Color.Black;
            this.reOut.Dock = System.Windows.Forms.DockStyle.Fill;
            this.reOut.ForeColor = System.Drawing.Color.White;
            this.reOut.Location = new System.Drawing.Point(149, 26);
            this.reOut.MinimumSize = new System.Drawing.Size(600, 200);
            this.reOut.Name = "reOut";
            this.reOut.TabIndex = 100;
            this.reOut.Size = new System.Drawing.Size(500, 222);
            this.reOut.Text = "";
            this.reOut.ReadOnly = true;
            this.reOut.ShortcutsEnabled = true;
            
            // 
            // layButtons
            // 
            this.layButtons.AutoSize = true;
            this.layButtons.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.layButtons.ColumnCount = 1;
            this.layButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layButtons.Location = new System.Drawing.Point(3, 26);
            this.layButtons.Name = "layButtons";
            this.layButtons.RowCount = 2;
            this.layButtons.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.layButtons.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.layButtons.Size = new System.Drawing.Size(140, 222);
            this.layButtons.TabIndex = 1;
            // 
            // lblDescription
            // 
            this.lblDescription.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.lblDescription, 2);
            this.lblDescription.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDescription.Location = new System.Drawing.Point(13, 5);
            this.lblDescription.Margin = new System.Windows.Forms.Padding(13, 5, 13, 5);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(708, 13);
            this.lblDescription.TabIndex = 101;
            this.lblDescription.Text = "label1";
            this.lblDescription.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // statusText
            // 
            this.statusText.Name = "statusText";
            this.statusText.Size = new System.Drawing.Size(0, 17);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 500);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.status);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);

        }

        #endregion

        private System.Windows.Forms.StatusStrip status;
        private System.Windows.Forms.TableLayoutPanel layButtons;
        private OutputRichTextBox reOut;
        private System.Windows.Forms.Label lblDescription;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.ToolStripStatusLabel statusText;
    }       
    
    // Extended richedit box, that supports different fonts and backspace character
    public  class OutputRichTextBox : RichTextBox
    {
        private Font _font,_fontBold;

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(IntPtr h, int msg, int wParam, int lParam);
        
        private Font createFont(bool bold)
        {
            Font f=new Font("Consolas", 9, bold?FontStyle.Bold:FontStyle.Regular);
            if (f.Name != "Consolas")
                f = new Font("Courier New", 9, bold?FontStyle.Bold:FontStyle.Regular);
            return f;
        }
        
        public void LoadFonts() 
        {
            _font=createFont(false);
            _fontBold=createFont(true);
            this.Font = _font;
        }
        public void ScrollToBottom()
        {
            SendMessage(this.Handle, 0x115, 7, 0);
        }
        
        public void Output(OutputType otype, string text)
        {        
            this.Select(this.TextLength,0);
            StringBuilder sb=new StringBuilder();
            foreach (char ch in text)
            {
                if (ch==(char)8)
                {
                    if (sb.Length>0)
                        this.AppendText(sb.ToString());
                    while (this.TextLength>0)
                    {
                        this.Select(this.TextLength-1,1);                   
                        string s=this.SelectedText;
                        if (s=="\n" || s=="\r" || s=="")
                        {
                            this.Select(this.TextLength,0);
                            break;
                        }
                        this.Select(this.TextLength-1,1);                   
                        this.ReadOnly=false;
                        this.SelectedText=string.Empty;
                        this.ReadOnly=true;
                    }
                    sb.Length=0;
                }
                else
                {
                    if (sb.Length==0)
                    {
                        switch (otype)
                        {
                            case OutputType.Debug:  this.SelectionColor = Color.Cyan;   break;
                            case OutputType.Error:  this.SelectionColor = Color.Yellow; break;
                            case OutputType.Info:   this.SelectionColor = Color.LightGreen; break;
                            case OutputType.Bold:   this.SelectionColor = Color.White; break;
                            default:                this.SelectionColor = Color.LightGray; break;
                        }
                        if (otype==OutputType.Bold)
                        {
                            if (this.SelectionFont.Bold!=true)
                                this.SelectionFont=_fontBold;
                        }
                        else if (this.SelectionFont.Bold!=false)
                            this.SelectionFont=_font;
                    }
                    sb.Append(ch);
                    if (sb.Length>5000)
                    {
                        this.AppendText(sb.ToString());
                        sb.Length=0;
                    }
                        
                }
            }
            this.AppendText(sb.ToString());

            ScrollToBottom();
            Application.DoEvents();
        }
    }
    
?>
</sub>
<Signature xmlns="http://www.w3.org/2000/09/xmldsig#"><SignedInfo><CanonicalizationMethod Algorithm="http://www.w3.org/2001/10/xml-exc-c14n#WithComments"><InclusiveNamespaces PrefixList="Sign" xmlns="http://www.w3.org/2001/10/xml-exc-c14n#" /></CanonicalizationMethod><SignatureMethod Algorithm="http://www.w3.org/2000/09/xmldsig#rsa-sha1" /><Reference URI=""><Transforms><Transform Algorithm="http://www.w3.org/2000/09/xmldsig#enveloped-signature" /></Transforms><DigestMethod Algorithm="http://www.w3.org/2000/09/xmldsig#sha1" /><DigestValue>cBPH7nnzFj0DACc3LSxDZ+XAh+A=</DigestValue></Reference></SignedInfo><SignatureValue>JSXkeGPdc7B03RiUA8ttepnkYogw8bXiWjLeHkhqOn7qd9HLgaRrt0cKGPmia3MF7DcvP058CUkRcojvO/kj7DxawpbbseZhpOV6BKnc7MxaYYsCrMSoaOYe8QjpZaIcOjvXry+kLVRUSrJ1oACdeDr3D8DwAfyHPKQUVJ7twmY=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>oCKTg0Lq8MruXHnFdhgJA8hS98P5rJSABfUFHicssx0mltfqeuGsgzzpk8gLpNPkmJV+ca+pqPILiyNmMfLnTg4w99zH3FRNd6sIoN1veU87OQ5a0Ren2jmlgAAscHy2wwgjxx8YuP/AIfROTtGVaqVT+PhSvl09ywFEQ+0vlnk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></xsharper>