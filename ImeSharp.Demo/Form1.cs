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
        private string _inputContent = string.Empty;
        public Form1()
        {
            InitializeComponent();

            CenterToScreen();

            Application.Idle += Application_Idle;
            KeyDown += Form1_KeyDown;

            InputMethod.Initialize(this.Handle, false);

            InputMethod.TextInput += (s, e) =>
            {
                switch (e.Character)
                {
                    case '\b':
                        if (_inputContent.Length > 0)
                            _inputContent = _inputContent.Remove(_inputContent.Length - 1, 1);
                        break;
                    case '\r':
                        _inputContent = "";
                        break;
                    default:
                        _inputContent += e.Character;
                        break;
                }

                textBoxResult.Text = _inputContent;
            };

            InputMethod.TextComposition += (s, e) =>
            {
                var str = e.CompositionString.ToString();
                str = str.Insert(e.CursorPosition, "|");
                labelComp.Text = str;

                string candidateList = string.Empty;

                for (int i = 0; e.CandidateList != null && i < e.CandidateList.Length; i++)
                    candidateList += string.Format("  {2}{0}.{1}\r\n", i + 1, e.CandidateList[i], i == e.CandidateSelection ? "*" : "");

                textBoxCandidates.Text = candidateList;
            };
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
