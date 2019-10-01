using SCQueryConnect.Common;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SCQueryConnect
{
    public class UILogger : Logger
    {
        internal TextBox Output { private get; set; }

        public override async Task Clear()
        {
            Output.Text = string.Empty;
            Output.ScrollToEnd();
            await Task.Delay(20);
        }

        public override async Task Log(string text)
        {
            Output.Text += FormatMessage(text);
            Output.ScrollToEnd();
            await Task.Delay(20);
        }
    }
}
