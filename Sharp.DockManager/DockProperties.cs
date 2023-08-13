using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace Sharp.DockManager
{
    /// <summary>
    /// Dock properties.
    /// </summary>
    public class DockProperties : AvaloniaObject
    {
        /// <summary>
        /// Defines the IsDockTarget attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> IsDockTargetProperty =
            AvaloniaProperty.RegisterAttached<DockProperties, Control, bool>("IsDockTarget", false, false, BindingMode.TwoWay);

        /// <summary>
        /// Defines the IsDragArea attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> IsDragAreaProperty =
            AvaloniaProperty.RegisterAttached<DockProperties, Control, bool>("IsDragArea", false, false, BindingMode.TwoWay);

        /// <summary>
        /// Defines the IsDropArea attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> IsDropAreaProperty =
            AvaloniaProperty.RegisterAttached<DockProperties, Control, bool>("IsDropArea", false, false, BindingMode.TwoWay);

        /// <summary>
        /// Define IsDragEnabled attached property.
        /// </summary>
        public static readonly StyledProperty<bool> IsDragEnabledProperty =
            AvaloniaProperty.RegisterAttached<DockProperties, Control, bool>("IsDragEnabled", true, true, BindingMode.TwoWay);

        /// <summary>
        /// Define IsDropEnabled attached property.
        /// </summary>
        public static readonly StyledProperty<bool> IsDropEnabledProperty =
            AvaloniaProperty.RegisterAttached<DockProperties, Control, bool>("IsDropEnabled", true, true, BindingMode.TwoWay);

        /// <summary>
        /// Defines the MinimumProportionSize attached property.
        /// </summary>
        public static readonly AttachedProperty<double> MinimumProportionSizeProperty =
            AvaloniaProperty.RegisterAttached<DockProperties, Control, double>("MinimumProportionSize", 75, true);

        /// <summary>
        /// Gets the value of the MinimumProportion attached property on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The minimum size a proportion can be resized to.</returns>
        public static double GetMinimumProportionSize(Control control)
        {
            return control.GetValue(MinimumProportionSizeProperty);
        }

        /// <summary>
        /// Sets the value of the MinimumProportionSize attached property on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The minimum size a proportion can be resized to.</param>
        public static void SetMinimumProportionSize(Control control, double value)
        {
            control.SetValue(MinimumProportionSizeProperty, value);
        }

        /// <summary>
        /// Gets the value of the IsDockTarget attached property on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The IsDockTarget attached property.</returns>
        public static bool GetIsDockTarget(Control control)
        {
            return control.GetValue(IsDockTargetProperty);
        }

        /// <summary>
        /// Sets the value of the IsDockTarget attached property on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The value of the IsDockTarget property.</param>
        public static void SetIsDockTarget(Control control, bool value)
        {
            control.SetValue(IsDockTargetProperty, value);
        }

        /// <summary>
        /// Gets the value of the IsDragArea attached property on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The IsDragArea attached property.</returns>
        public static bool GetIsDragArea(Control control)
        {
            return control.GetValue(IsDragAreaProperty);
        }

        /// <summary>
        /// Sets the value of the IsDragArea attached property on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The value of the IsDragArea property.</param>
        public static void SetIsDragArea(Control control, bool value)
        {
            control.SetValue(IsDragAreaProperty, value);
        }

        /// <summary>
        /// Gets the value of the IsDropArea attached property on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The IsDropArea attached property.</returns>
        public static bool GetIsDropArea(Control control)
        {
            return control.GetValue(IsDropAreaProperty);
        }

        /// <summary>
        /// Sets the value of the IsDropArea attached property on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The value of the IsDropArea property.</param>
        public static void SetIsDropArea(Control control, bool value)
        {
            control.SetValue(IsDropAreaProperty, value);
        }

        /// <summary>
        /// Gets the value of the IsDragEnabled attached property on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The IsDragEnabled attached property.</returns>
        public static bool GetIsDragEnabled(Control control)
        {
            return control.GetValue(IsDragEnabledProperty);
        }

        /// <summary>
        /// Sets the value of the IsDragEnabled attached property on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The value of the IsDragEnabled property.</param>
        public static void SetIsDragEnabled(Control control, bool value)
        {
            control.SetValue(IsDragEnabledProperty, value);
        }

        /// <summary>
        /// Gets the value of the IsDropEnabled attached property on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The IsDropEnabled attached property.</returns>
        public static bool GetIsDropEnabled(Control control)
        {
            return control.GetValue(IsDropEnabledProperty);
        }

        /// <summary>
        /// Sets the value of the IsDropEnabled attached property on the specified control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The value of the IsDropEnabled property.</param>
        public static void SetIsDropEnabled(Control control, bool value)
        {
            control.SetValue(IsDropEnabledProperty, value);
        }
    }
}
