using SCQueryConnect.Common;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SCQueryConnect
{
    public class UILogger : Logger
    {
        private readonly TextBox _textBox;

        public UILogger(TextBox textBox)
        {
            _textBox = textBox;
        }

        public override async Task Clear()
        {
            _textBox.Text = string.Empty;
            _textBox.ScrollToEnd();
            await Task.Delay(20);
        }

        public override async Task Log(string text)
        {
            _textBox.Text += FormatMessage(text);
            _textBox.ScrollToEnd();
            await Task.Delay(20);
        }
    }
}
