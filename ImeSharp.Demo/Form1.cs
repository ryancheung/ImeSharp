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
        private bool _inputMethodEnabled;
        private TextCompositionEventArgs _compEvent;
        private string inputContent = string.Empty;

        public Form1()
        {
            InitializeComponent();

            CenterToScreen();

            Application.Idle += Application_Idle;
            KeyDown += Form1_KeyDown;

            InputMethod.Initialize(this.Handle);
            InputMethod.TextComposition += (o, e) =>
            {
                _compEvent = e;
                UpdateUI();
            };

            InputMethod.TextInput += (o, e) =>
            {
                switch ((int)e.Result)
                {
                    case 8:
                        if (inputContent.Length > 0)
                            inputContent = inputContent.Remove(inputContent.Length - 1, 1);
                        break;
                    case 27:
                    case 13:
                        inputContent = "";
                        break;
                    default:
                        inputContent += e.Result;
                        break;
                }

                textBoxResult.Text = inputContent;
            };
        }

        private void Application_Idle(object sender, EventArgs e)
        {
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
            {
                InputMethod.EnableOrDisableInputMethod(!_inputMethodEnabled);

                _inputMethodEnabled = !_inputMethodEnabled;
            }
        }

        private void UpdateUI()
        {
            labelComp.Text = _compEvent.CompositionString;

            if (_compEvent.CandidateList == null)
            {
                textBoxCandidates.Text = string.Empty;
                return;
            }

            var candidatesList = string.Empty;
            for (uint i = _compEvent.CandidatePageStart;
                i < Math.Min(_compEvent.CandidatePageStart + _compEvent.CandidatePageSize, _compEvent.CandidateList.Length);
                i++)
            {
                candidatesList += string.Format("{0}.{1}\r\n", i + 1 - _compEvent.CandidatePageStart, _compEvent.CandidateList[i]);
            }

            textBoxCandidates.Text = candidatesList;
        }

    }
}
