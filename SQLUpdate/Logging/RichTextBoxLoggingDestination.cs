using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace SCQueryConnect.Logging
{
    public class RichTextBoxLoggingDestination : LoggingDestination<RichTextBox>
    {
        private readonly char[] _logEntrySeparators = {'\r', '\n'};
        private readonly Regex _hasTimestamp = new Regex("^\\[");
        private readonly Regex _newSection = new Regex("^\\[.+\\] Running '.+'\\.{3}");
        private readonly Regex _error = new Regex(ErrorPrefix);
        private readonly Regex _warning = new Regex(WarningPrefix);

        public RichTextBoxLoggingDestination(RichTextBox destination) : base(destination)
        {
        }

        public override async Task Log(string text)
        {
            await Log(text, false, null);
        }

        public override async Task Clear()
        {
            Destination.Document.Blocks.Clear();
            Destination.ScrollToEnd();
            await Task.Delay(20);
        }

        public async Task SetLogText(string logText)
        {
            await Clear();

            var logEntries = logText?.Split(
                _logEntrySeparators,
                StringSplitOptions.RemoveEmptyEntries);

            if (logEntries == null)
            {
                return;
            }

            Brush lastUsedBrush = null;
            
            foreach (var entry in logEntries)
            {
                var brush = _hasTimestamp.IsMatch(entry)
                    ? null
                    : lastUsedBrush;

                var withNewLine = entry + Environment.NewLine;
                lastUsedBrush = await Log(withNewLine, true, brush);
            }
        }

        private async Task<Brush> Log(
            string text,
            bool rebuildLog,
            Brush brushOverride)
        {
            var toAppend = rebuildLog
                ? text
                : FormatMessage(text);

            var isNewSection = _newSection.IsMatch(toAppend);
            
            if (isNewSection)
            {
                Destination.Document.Blocks.Add(new Paragraph());
            }

            Brush brush;

            if (brushOverride == null)
            {
                if (_error.IsMatch(text))
                {
                    brush = Brushes.Pink;
                }
                else if (_warning.IsMatch(text))
                {
                    brush = Brushes.LightGoldenrodYellow;
                }
                else
                {
                    brush = Destination.Foreground;
                }
            }
            else
            {
                brush = brushOverride;
            }

            var run = new Run(toAppend)
            {
                Foreground = brush
            };
            
            if (Destination.Document.Blocks.Count == 0)
            {
                Destination.Document.Blocks.Add(new Paragraph());
            }

            var paragraph = (Paragraph)Destination.Document.Blocks.LastBlock;
            paragraph.Inlines.Add(run);

            if (!rebuildLog)
            {
                Destination.ScrollToEnd();
                await Task.Delay(20);
            }

            return brush;
        }
    }
}
