using System.Threading.Tasks;
using System.Windows.Controls;

namespace SCQueryConnect.Logging
{
    public class TextBoxLoggingDestination : LoggingDestination<TextBox>
    {
        public TextBoxLoggingDestination(TextBox destination) : base(destination)
        {
        }

        public override async Task Log(string text)
        {
            var toAppend = FormatMessage(text);
            Destination.Text += toAppend;
            Destination.ScrollToEnd();
            await Task.Delay(20);
        }

        public override async Task Clear()
        {
            Destination.Clear();
            Destination.Text = string.Empty;
            Destination.ScrollToEnd();
            await Task.Delay(20);
        }
    }
}
