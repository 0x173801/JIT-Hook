using dnlib.DotNet;
using jit_winform.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.IO;


namespace jit_winform
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            bool flag = openFileDialog.ShowDialog() == DialogResult.OK;
            if (flag)
            {
                this.textBox1.Text = openFileDialog.FileName;
                this.Target = this.textBox1.Text;
            }
        }
        public static string PrintBytes(byte[] byteArray)
        {
            var sb = new StringBuilder("new byte[] { ");
            for (var i = 0; i < byteArray.Length; i++)
            {
                var b = byteArray[i];
                sb.Append(b.ToString("X2"));
                if (i < byteArray.Length - 1)
                {
                    sb.Append(", ");
                }
            }
            sb.Append(" }");
            return sb.ToString();
        }
        private string Target = string.Empty;
        private void button2_Click(object sender, EventArgs e)
        {
            ModuleDef module = ModuleDefMD.Load(textBox1.Text);
            var path = textBox1.Text;
            var output = path.Replace(".exe", "_jit.exe");
            var context = new FileContext(path);
            var save = output;

            var stages = new List<Stage>();


            if (ckjit.Checked)
            {
                var outputBytes = context.Output;

                if (outputBytes == null)
                    outputBytes = context.GetBytes();


                var newCtx = new jit_winform.Core.Context(outputBytes);
                var jit = new Protection1(newCtx);
                var resultjit = jit.Protect();
                File.WriteAllBytes(output, resultjit);
                MessageBox.Show("DONE");

            }
            else
            {
      


            }


        }
    }
}
