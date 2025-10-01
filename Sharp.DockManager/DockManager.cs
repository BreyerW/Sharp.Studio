using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using System;
using System.ComponentModel;

namespace Sharp.DockManager
{
    /// <summary>
    /// Dock properties.
    /// </summary>
    public class DockManager : AvaloniaObject
    {
	/// <summary>
	/// Defines the IsDropArea attached property.
	/// </summary>

        public static readonly AttachedProperty<bool> AllowLastCloseProperty =
            AvaloniaProperty.RegisterAttached<DockManager, Window, bool>("AllowLastClose", true, true, BindingMode.TwoWay);
        public static readonly AttachedProperty<bool> AllowCloseProperty =
            AvaloniaProperty.RegisterAttached<DockManager, DockableControl, bool>("AllowClose", true, true, BindingMode.TwoWay);
        public static readonly AttachedProperty<bool> AllowDropProperty =
			AvaloniaProperty.RegisterAttached<DockManager, Control, bool>("AllowDrop", true, true, BindingMode.TwoWay);
		public static readonly AttachedProperty<bool> AllowDragProperty =
			AvaloniaProperty.RegisterAttached<DockManager, Control, bool>("AllowDrag", true, true, BindingMode.TwoWay);
        public static readonly AttachedProperty<Region> AllowDropAreaProperty =
    AvaloniaProperty.RegisterAttached<DockManager, DockableControl, Region>("AllowAreaDrop", Region.All, true, BindingMode.TwoWay);

        public static Region GetAllowDropArea(Control control)
        {
            return control.GetValue(AllowDropAreaProperty);
        }

        public static void SetAllowDropArea(Control control, Region value)
        {
            control.SetValue(AllowDropAreaProperty, value);
        }
       
        /// <summary>
        /// Gets the value of the IsDropArea attached property on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The IsDropArea attached property.</returns>
        public static bool GetAllowLastClose(Control control)
        {
            return control.GetValue(AllowLastCloseProperty);
        }
        /// <summary>
        /// Sets the value of the IsDropArea attached property on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The value of the IsDropArea property.</param>
        public static void SetAllowLastClose(Control control, bool value)
        {
            control.SetValue(AllowLastCloseProperty, value);
        }
        /// <summary>
        /// Gets the value of the IsDropArea attached property on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The IsDropArea attached property.</returns>
        public static bool GetAllowClose(Control control)
        {
            return control.GetValue(AllowCloseProperty);
        }
        /// <summary>
        /// Sets the value of the IsDropArea attached property on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The value of the IsDropArea property.</param>
        public static void SetAllowClose(Control control, bool value)
        {
            control.SetValue(AllowCloseProperty, value);
        }
        /// <summary>
        /// Gets the value of the IsDropArea attached property on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The IsDropArea attached property.</returns>
        public static bool GetAllowDrop(Control control)
		{
			return control.GetValue(AllowDropProperty);
		}

		/// <summary>
		/// Sets the value of the IsDropArea attached property on the specified control.
		/// </summary>
		/// <param name="control">The control.</param>
		/// <param name="value">The value of the IsDropArea property.</param>
		public static void SetAllowDrop(Control control, bool value)
		{
			control.SetValue(AllowDropProperty, value);
		}
		/// Gets the value of the IsDropArea attached property on the specified control.
		/// </summary>
		/// <param name="control">The control.</param>
		/// <returns>The IsDropArea attached property.</returns>
		public static bool GetAllowDrag(Control control)
		{
			return control.GetValue(AllowDragProperty);
		}

		/// <summary>
		/// Sets the value of the IsDropArea attached property on the specified control.
		/// </summary>
		/// <param name="control">The control.</param>
		/// <param name="value">The value of the IsDropArea property.</param>
		public static void SetAllowDrag(Control control, bool value)
		{
			control.SetValue(AllowDragProperty, value);
		}
	}
}
