using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using ImeSharp.Native;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace ImeSharp.WindowsUniversalDemo
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private string _inputContent = string.Empty;

        public MainPage()
        {
            this.InitializeComponent();

            var window = Windows.UI.Core.CoreWindow.GetForCurrentThread();

            InputMethod.Initialize(window);

            InputMethod.TextInputCallback = OnTextInput;
            InputMethod.TextCompositionCallback = OnTextComposition;

            var textBoxCandidates = this.FindName("candidateBox") as TextBox;
            textBoxCandidates.IsEnabled = false;

            window.KeyDown += CoreWindow_KeyDown;
        }

        private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            if (args.VirtualKey == Windows.System.VirtualKey.F1)
                InputMethod.Enabled = !InputMethod.Enabled;
        }

        private void OnTextInput(char character)
        {
            switch (character)
            {
                case '\b':
                    if (_inputContent.Length > 0)
                        _inputContent = _inputContent.Remove(_inputContent.Length - 1, 1);
                    break;
                case '\r':
                    _inputContent = "";
                    break;
                default:
                    _inputContent += character;
                    break;
            }

            var resultTextLabel = (this.FindName("resultTextLabel") as TextBlock);

            //Console.WriteLine("inputContent: {0}", _inputContent);
            resultTextLabel.Text = _inputContent;
        }

        private void OnTextComposition(IMEString compositionText, int cursorPosition, IMEString[] candidateList, int candidatePageStart, int candidatePageSize, int candidateSelection)
        {
            var str = compositionText.ToString();
            str = str.Insert(cursorPosition, "|");
            var compStringLabel = this.FindName("compStringLabel") as TextBlock;
            compStringLabel.Text = str;

            string candidateText = string.Empty;

            for (int i = 0; candidateList != null && i < candidateList.Length; i++)
                candidateText += string.Format("  {2}{0}.{1}\r\n", i + 1, candidateList[i], i == candidateSelection ? "*" : "");

            var textBoxCandidates = this.FindName("candidateBox") as TextBox;

            textBoxCandidates.Text = candidateText;

            var ttv = compStringLabel.TransformToVisual(Window.Current.Content);
            Point screenCoords = ttv.TransformPoint(new Point(0, 0));


            InputMethod.SetTextInputRect((int)screenCoords.X + (int)compStringLabel.ActualWidth, (int)screenCoords.Y, (int)compStringLabel.ActualHeight, 30);
        }
    }
}
