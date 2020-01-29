using System.Text;
using SCQueryConnect.Common;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SCQueryConnect
{
    public class UILogger : Logger
    {
        private readonly StringBuilder _logText = new StringBuilder();
        private TextBox[] _outputs;

        public void Initialise(params TextBox[] outputs)
        {
            _logText.Clear();
            _outputs = outputs;
        }

        public override async Task Clear()
        {
            _logText.Clear();

            foreach (var textBox in _outputs)
            {
                textBox.Text = string.Empty;
                textBox.ScrollToEnd();
            }

            await Task.Delay(20);
        }

        public override async Task Log(string text)
        {
            var toAppend = FormatMessage(text);
            _logText.Append(toAppend);

            foreach (var textBox in _outputs)
            {
                textBox.Text += toAppend;
                textBox.ScrollToEnd();
            }

            await Task.Delay(20);
        }

        public string GetLogText()
        {
            return _logText.ToString();
        }
    }
}
