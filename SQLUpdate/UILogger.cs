using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using SCQueryConnect.Common.Interfaces;

namespace SCQueryConnect
{
    public class UILogger : ILog
    {
        private readonly TextBox _textBox;

        public UILogger(TextBox textBox)
        {
            _textBox = textBox;
        }

        public async Task Clear()
        {
            _textBox.Text = "";
            _textBox.ScrollToEnd();
            await Task.Delay(20);
        }

        public async Task Log(string text)
        {
            _textBox.Text += $"{text}{Environment.NewLine}";
            _textBox.ScrollToEnd();
            await Task.Delay(20);
        }
    }
}
