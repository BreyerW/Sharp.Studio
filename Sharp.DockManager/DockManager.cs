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

		public static readonly AttachedProperty<bool> AllowHeaderDropProperty =
            AvaloniaProperty.RegisterAttached<DockManager, DockableControl, bool>("AllowHeaderDrop", true, true, BindingMode.TwoWay);

		public static readonly AttachedProperty<bool> AllowCenterDropProperty =
			AvaloniaProperty.RegisterAttached<DockManager, DockableControl, bool>("AllowCenterDrop", true, true, BindingMode.TwoWay);
		public static readonly AttachedProperty<bool> AllowLeftDropProperty =
			AvaloniaProperty.RegisterAttached<DockManager, DockableControl, bool>("AllowLeftDrop", true, true, BindingMode.TwoWay);
		public static readonly AttachedProperty<bool> AllowRightDropProperty =
			AvaloniaProperty.RegisterAttached<DockManager, DockableControl, bool>("AllowRightDrop", true, true, BindingMode.TwoWay);
		public static readonly AttachedProperty<bool> AllowTopDropProperty =
			AvaloniaProperty.RegisterAttached<DockManager, DockableControl, bool>("AllowTopDrop", true, true, BindingMode.TwoWay);
		public static readonly AttachedProperty<bool> AllowBottomDropProperty =
			AvaloniaProperty.RegisterAttached<DockManager, DockableControl, bool>("AllowBottomDrop", true, true, BindingMode.TwoWay);

		public static readonly AttachedProperty<bool> AllowDropProperty =
			AvaloniaProperty.RegisterAttached<DockManager, TabItem, bool>("AllowDrop", true, true, BindingMode.TwoWay);
		public static readonly AttachedProperty<bool> AllowDragProperty =
			AvaloniaProperty.RegisterAttached<DockManager, TabItem, bool>("AllowDrag", true, true, BindingMode.TwoWay);

		/// <summary>
		/// Gets the value of the IsDropArea attached property on the specified control.
		/// </summary>
		/// <param name="control">The control.</param>
		/// <returns>The IsDropArea attached property.</returns>
		public static bool GetAllowHeaderDrop(Control control)
        {
            return control.GetValue(AllowHeaderDropProperty);
        }

        /// <summary>
        /// Sets the value of the IsDropArea attached property on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The value of the IsDropArea property.</param>
        public static void SetAllowHeaderDrop(Control control, Region value)
        {
            control.SetValue(AllowHeaderDropProperty, value);
        }
		/// <summary>
		/// Gets the value of the IsDropArea attached property on the specified control.
		/// </summary>
		/// <param name="control">The control.</param>
		/// <returns>The IsDropArea attached property.</returns>
		public static bool GetAllowCenterDrop(Control control)
		{
			return control.GetValue(AllowCenterDropProperty);
		}

		/// <summary>
		/// Sets the value of the IsDropArea attached property on the specified control.
		/// </summary>
		/// <param name="control">The control.</param>
		/// <param name="value">The value of the IsDropArea property.</param>
		public static void SetAllowCenterDrop(Control control, Region value)
		{
			control.SetValue(AllowCenterDropProperty, value);
		}
		/// <summary>
		/// Gets the value of the IsDropArea attached property on the specified control.
		/// </summary>
		/// <param name="control">The control.</param>
		/// <returns>The IsDropArea attached property.</returns>
		public static bool GetAllowLeftDrop(Control control)
		{
			return control.GetValue(AllowLeftDropProperty);
		}

		/// <summary>
		/// Sets the value of the IsDropArea attached property on the specified control.
		/// </summary>
		/// <param name="control">The control.</param>
		/// <param name="value">The value of the IsDropArea property.</param>
		public static void SetAllowLeftDrop(Control control, Region value)
		{
			control.SetValue(AllowHeaderDropProperty, value);
		}
		/// <summary>
		/// Gets the value of the IsDropArea attached property on the specified control.
		/// </summary>
		/// <param name="control">The control.</param>
		/// <returns>The IsDropArea attached property.</returns>
		public static bool GetAllowRightDrop(Control control)
		{
			return control.GetValue(AllowRightDropProperty);
		}

		/// <summary>
		/// Sets the value of the IsDropArea attached property on the specified control.
		/// </summary>
		/// <param name="control">The control.</param>
		/// <param name="value">The value of the IsDropArea property.</param>
		public static void SetAllowRightDrop(Control control, Region value)
		{
			control.SetValue(AllowRightDropProperty, value);
		}
		/// <summary>
		/// Gets the value of the IsDropArea attached property on the specified control.
		/// </summary>
		/// <param name="control">The control.</param>
		/// <returns>The IsDropArea attached property.</returns>
		public static bool GetAllowTopDrop(Control control)
		{
			return control.GetValue(AllowTopDropProperty);
		}

		/// <summary>
		/// Sets the value of the IsDropArea attached property on the specified control.
		/// </summary>
		/// <param name="control">The control.</param>
		/// <param name="value">The value of the IsDropArea property.</param>
		public static void SetAllowTopDrop(Control control, Region value)
		{
			control.SetValue(AllowTopDropProperty, value);
		}/// <summary>
		 /// Gets the value of the IsDropArea attached property on the specified control.
		 /// </summary>
		 /// <param name="control">The control.</param>
		 /// <returns>The IsDropArea attached property.</returns>
		public static bool GetAllowBottomDrop(Control control)
		{
			return control.GetValue(AllowBottomDropProperty);
		}

		/// <summary>
		/// Sets the value of the IsDropArea attached property on the specified control.
		/// </summary>
		/// <param name="control">The control.</param>
		/// <param name="value">The value of the IsDropArea property.</param>
		public static void SetAllowBottomDrop(Control control, Region value)
		{
			control.SetValue(AllowBottomDropProperty, value);
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
