using System;
using System.Windows;
using System.Windows.Controls;

namespace SCQueryConnect.Controls
{
    [TemplatePart(Name = "PART_CancelButton", Type = typeof(Button))]
    public class BusyOverlay : Control
    {
        private Button _cancelButton;

        private Button CancelButton
        {
            set
            {
                if (_cancelButton != null)
                {
                    _cancelButton.Click -= CancelButtonClick;
                }

                _cancelButton = value;

                if (_cancelButton != null)
                {
                    _cancelButton.Click += CancelButtonClick;
                }
            }
        }

        public bool CanCancelUpdate
        {
            get => (bool)GetValue(CanCancelUpdateProperty);
            set => SetValue(CanCancelUpdateProperty, value);
        }

        public string UpdateText
        {
            get => (string)GetValue(UpdateTextProperty);
            set => SetValue(UpdateTextProperty, value);
        }

        public string UpdateSubtext
        {
            get => (string)GetValue(UpdateSubtextProperty);
            set => SetValue(UpdateSubtextProperty, value);
        }

        public static readonly DependencyProperty CanCancelUpdateProperty =
            DependencyProperty.Register("CanCancelUpdate", typeof(bool), typeof(BusyOverlay),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty UpdateSubtextProperty =
            DependencyProperty.Register("UpdateSubtext", typeof(string), typeof(BusyOverlay),
                new FrameworkPropertyMetadata(string.Empty));

        public static readonly DependencyProperty UpdateTextProperty =
            DependencyProperty.Register("UpdateText", typeof(string), typeof(BusyOverlay),
                new FrameworkPropertyMetadata(string.Empty));

        public event EventHandler<EventArgs> Cancel;

        static BusyOverlay()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BusyOverlay),
                new FrameworkPropertyMetadata(typeof(BusyOverlay)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            CancelButton = GetTemplateChild("PART_CancelButton") as Button;
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            Cancel?.Invoke(this, EventArgs.Empty);
        }
    }
}
