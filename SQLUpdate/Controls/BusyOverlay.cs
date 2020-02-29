using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SCQueryConnect.Controls
{
    public class BusyOverlay : Control
    {
        public bool CanCancelUpdate
        {
            get => (bool)GetValue(CanCancelUpdateProperty);
            set => SetValue(CanCancelUpdateProperty, value);
        }

        public ICommand CancelUpdateCommand
        {
            get => (ICommand)GetValue(CancelUpdateCommandProperty);
            set => SetValue(CancelUpdateCommandProperty, value);
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

        public static readonly DependencyProperty CancelUpdateCommandProperty =
            DependencyProperty.Register("CancelUpdateCommand", typeof(ICommand), typeof(BusyOverlay),
                new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty UpdateSubtextProperty =
            DependencyProperty.Register("UpdateSubtext", typeof(string), typeof(BusyOverlay),
                new FrameworkPropertyMetadata(string.Empty));

        public static readonly DependencyProperty UpdateTextProperty =
            DependencyProperty.Register("UpdateText", typeof(string), typeof(BusyOverlay),
                new FrameworkPropertyMetadata(string.Empty));

        static BusyOverlay()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BusyOverlay),
                new FrameworkPropertyMetadata(typeof(BusyOverlay)));
        }
    }
}
