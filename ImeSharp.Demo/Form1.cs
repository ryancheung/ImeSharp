﻿using System;
using System.Windows.Forms;

namespace ImeSharp.Demo
{
    public partial class Form1 : Form
    {
        private bool _inputMethodEnabled;
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
            {
                InputMethod.EnableOrDisableInputMethod(!_inputMethodEnabled);

                _inputMethodEnabled = !_inputMethodEnabled;
            }
        }

        private void FakeDraw()
        {
        }

    }
}
