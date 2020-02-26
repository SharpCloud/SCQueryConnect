using SCQueryConnect.Helpers;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace SCQueryConnect.Controls
{
    [TemplatePart(Name = "PART_PreviewSqlButton", Type = typeof(Button))]
    [TemplatePart(Name = "PART_QueryTextBox", Type = typeof(TextBox))]
    public class QueryEditor : Control
    {
        private Button _previewSqlButton;
        private TextBox _queryTextBox;

        private Button PreviewSqlButton
        {
            set
            {
                if (_previewSqlButton != null)
                {
                    _previewSqlButton.Click -= PreviewSqlButtonClick;
                }

                _previewSqlButton = value;

                if (_previewSqlButton != null)
                {
                    _previewSqlButton.Click += PreviewSqlButtonClick;
                }
            }
        }

        private TextBox QueryTextBox
        {
            get => _queryTextBox;

            set
            {
                if (_queryTextBox != null)
                {
                    _queryTextBox.LostFocus += QueryTextBoxLostFocus;
                }

                _queryTextBox = value;

                if (_queryTextBox != null)
                {
                    _queryTextBox.TextChanged += QueryTextBoxLostFocus;
                }
            }
        }

        public QueryEntityType TargetEntity { get; set; }

        public string SelectedQueryString => string.IsNullOrWhiteSpace(QueryTextBox.SelectedText)
            ? QueryTextBox.Text
            : QueryTextBox.SelectedText;

        public string ErrorText
        {
            get => (string) GetValue(ErrorTextProperty);
            set => SetValue(ErrorTextProperty, value);
        }

        public string QueryString
        {
            get => (string)GetValue(QueryStringProperty);
            set => SetValue(QueryStringProperty, value);
        }

        public DataView QueryResults
        {
            get => (DataView)GetValue(QueryResultsProperty);
            set => SetValue(QueryResultsProperty, value);
        }

        public Visibility ErrorVisibility
        {
            get => (Visibility) GetValue(ErrorVisibilityProperty);
            set => SetValue(ErrorVisibilityProperty, value);
        }

        public static readonly DependencyProperty ErrorTextProperty =
            DependencyProperty.Register("ErrorText", typeof(string), typeof(QueryEditor),
                new FrameworkPropertyMetadata(string.Empty));

        public static readonly DependencyProperty QueryStringProperty =
            DependencyProperty.Register("QueryString", typeof(string), typeof(QueryEditor),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty QueryResultsProperty =
            DependencyProperty.Register("QueryResults", typeof(DataView), typeof(QueryEditor),
                new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty ErrorVisibilityProperty =
            DependencyProperty.Register("ErrorVisibility", typeof(Visibility), typeof(QueryEditor),
                new FrameworkPropertyMetadata(Visibility.Collapsed));

        public event EventHandler<EventArgs> SqlPreviewClick;

        static QueryEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(QueryEditor),
                new FrameworkPropertyMetadata(typeof(QueryEditor)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            
            PreviewSqlButton = GetTemplateChild("PART_PreviewSqlButton") as Button;
            QueryTextBox = GetTemplateChild("PART_QueryTextBox") as TextBox;
        }

        private void PreviewSqlButtonClick(object sender, RoutedEventArgs e)
        {
            SqlPreviewClick?.Invoke(this, EventArgs.Empty);
        }

        private void QueryTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            QueryString = ((TextBox)sender).Text;
        }
    }
}
