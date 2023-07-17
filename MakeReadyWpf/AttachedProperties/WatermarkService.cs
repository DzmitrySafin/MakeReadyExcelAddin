using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;

namespace MakeReadyWpf.AttachedProperties
{
    internal class WatermarkService
    {
        public static readonly DependencyProperty WatermarkProperty = DependencyProperty.RegisterAttached("Watermark", typeof(object), typeof(WatermarkService),
            new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnWatermarkChanged)));

        private static readonly Dictionary<object, ItemsControl> itemsControls = new Dictionary<object, ItemsControl>();

        public static object GetWatermark(DependencyObject dep)
        {
            return dep.GetValue(WatermarkProperty);
        }

        public static void SetWatermark(DependencyObject dep, object value)
        {
            dep.SetValue(WatermarkProperty, value);
        }

        private static void OnWatermarkChanged(DependencyObject dep, DependencyPropertyChangedEventArgs e)
        {
            Control ctrl = (Control)dep;
            ctrl.Loaded += Control_Loaded;

            if (dep is ComboBox)
            {
                ctrl.GotKeyboardFocus += Control_GotKeyboardFocus;
                ctrl.LostKeyboardFocus += Control_Loaded;
            }
            else if (dep is TextBox)
            {
                ctrl.GotKeyboardFocus += Control_GotKeyboardFocus;
                ctrl.LostKeyboardFocus += Control_Loaded;
                ((TextBox)ctrl).TextChanged += Control_GotKeyboardFocus;
            }

            if (dep is ItemsControl && !(dep is ComboBox))
            {
                ItemsControl i = (ItemsControl)dep;

                // for Items property
                i.ItemContainerGenerator.ItemsChanged += ItemsChanged;
                itemsControls.Add(i.ItemContainerGenerator, i);

                // for ItemsSource property
                DependencyPropertyDescriptor prop = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, i.GetType());
                prop.AddValueChanged(i, ItemsSourceChanged);
            }
        }

        private static void Control_GotKeyboardFocus(object sender, RoutedEventArgs e)
        {
            Control ctrl = (Control)sender;
            if (ShouldShowWatermark(ctrl))
            {
                ShowWatermark(ctrl);
            }
            else
            {
                RemoveWatermark(ctrl);
            }
        }

        private static void Control_Loaded(object sender, RoutedEventArgs e)
        {
            Control ctrl = (Control)sender;
            if (ShouldShowWatermark(ctrl))
            {
                ShowWatermark(ctrl);
            }
        }

        private static void ItemsSourceChanged(object sender, EventArgs e)
        {
            ItemsControl ctrl = (ItemsControl)sender;
            if (ctrl.ItemsSource != null)
            {
                if (ShouldShowWatermark(ctrl))
                {
                    ShowWatermark(ctrl);
                }
                else
                {
                    RemoveWatermark(ctrl);
                }
            }
            else
            {
                ShowWatermark(ctrl);
            }
        }

        private static void ItemsChanged(object sender, ItemsChangedEventArgs e)
        {
            ItemsControl ctrl;
            if (itemsControls.TryGetValue(sender, out ctrl))
            {
                if (ShouldShowWatermark(ctrl))
                {
                    ShowWatermark(ctrl);
                }
                else
                {
                    RemoveWatermark(ctrl);
                }
            }
        }

        private static void RemoveWatermark(UIElement element)
        {
            AdornerLayer layer = AdornerLayer.GetAdornerLayer(element);

            // layer could be null if control is no longer in the visual tree
            if (layer != null)
            {
                Adorner[] adorners = layer.GetAdorners(element);
                if (adorners == null)
                {
                    return;
                }

                foreach (Adorner adorner in adorners)
                {
                    if (adorner is WatermarkAdorner)
                    {
                        adorner.Visibility = Visibility.Hidden;
                        layer.Remove(adorner);
                    }
                }
            }
        }

        private static void ShowWatermark(Control control)
        {
            AdornerLayer layer = AdornerLayer.GetAdornerLayer(control);

            if (layer != null) // can be null if control is no longer in the visual tree
            {
                layer.Add(new WatermarkAdorner(control, GetWatermark(control)));
            }
        }

        private static bool ShouldShowWatermark(Control control)
        {
            if (control is ComboBox)
            {
                return (control as ComboBox).Text == string.Empty;
            }
            else if (control is TextBoxBase)
            {
                return (control as TextBox).Text == string.Empty;
            }
            else if (control is ItemsControl)
            {
                return (control as ItemsControl).Items.Count == 0;
            }
            else
            {
                return false;
            }
        }
    }
}
