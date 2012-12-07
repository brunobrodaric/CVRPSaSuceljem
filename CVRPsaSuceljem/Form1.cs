using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CVRPsaSuceljem
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            Form2 f2 = new Form2();
            f2.Show();
            this.Hide();
        }

        Form2 f2 = new Form2();

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            
            f2.otac = this;
            
            f2.Show();
            this.Hide();
        }
    }
}
