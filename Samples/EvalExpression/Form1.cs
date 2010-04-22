using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using XSharper.Core;

namespace EvalExpression
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            variableBindingSource.Add(new Variable { Name = "Hello", Value = "World" });
            variableBindingSource.Add(new Variable { Name = "A", Value = "50" });
            variableBindingSource.Add(new Variable { Name = "B", Value = "0x0a" });
            variableBindingSource.Add(new Variable { Name = "D", Value = "30.5d" });

            tbExpression.Text = "Math.Sqrt( $A+$B+$hello.Length)+$D+System.IO.Directory.GetFiles('c:\').Length;";
        }

        private void btnCalc_Click(object sender, EventArgs e)
        {
            BasicEvaluationContext be=new BasicEvaluationContext();

            foreach (Variable var in variableBindingSource    )
            {
                ParsingReader pr=new ParsingReader(var.Value);
                be.SetVariable(var.Name, (object)pr.ReadNumber() ?? var.Value);
            }


            try
            {
                tbResult.Text = Dump.ToDump(be.Eval(tbExpression.Text));
            }
            catch (Exception ex)
            {
                tbResult.Text = ex.ToString();
            }

        }


        

    }
}
