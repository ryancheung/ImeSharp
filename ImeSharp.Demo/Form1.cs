using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImeSharp.Demo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            CenterToScreen();

            Application.Idle += Application_Idle;
            KeyDown += Form1_KeyDown;

            InputMethod.Initialize(this.Handle);
        }

        private void Application_Idle(object sender, EventArgs e)
        {
            FakeDraw();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
                InputMethod.Enabled = !InputMethod.Enabled;
        }

        private void FakeDraw()
        {
        }

    }
}
