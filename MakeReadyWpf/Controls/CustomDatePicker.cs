using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace MakeReadyWpf.Controls
{
    public class CustomDatePicker : DatePicker
    {
        public string WatermarkText
        {
            get { return (string)GetValue(WatermarkTextProperty); }
            set { SetValue(WatermarkTextProperty, value); }
        }

        public static readonly DependencyProperty WatermarkTextProperty = DependencyProperty.Register("WatermarkText", typeof(string), typeof(CustomDatePicker), new PropertyMetadata("Select a date"));

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            DatePickerTextBox box = GetTemplateChild("PART_TextBox") as DatePickerTextBox;
            box.ApplyTemplate();

            ContentControl watermark = box.Template.FindName("PART_Watermark", box) as ContentControl;
            watermark.Content = WatermarkText;
            watermark.Foreground = Brushes.DarkGray;
        }
    }
}
