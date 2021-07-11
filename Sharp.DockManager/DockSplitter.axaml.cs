using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using System;
using System.Linq;

namespace Sharp.DockManager
{

    /// <summary>
    ///   Like <see cref="GridSplitter"/>, but for <see cref="DockPanel"/>
    ///   instead of <see cref="Grid"/>.
    /// </summary>
    public class DockSplitter : Thumb, IStyleable
    {
        static readonly Control targetNullObject = new Control();
        static readonly DockPanel parentNullObject = new DockPanel();
        Type IStyleable.StyleKey => typeof(DockSplitter);
        bool isHorizontal;
        bool isBottomOrRight;
        Control target = targetNullObject;
        double? initialLength;
        double availableSpace;


        /// <summary> </summary>
        public DockSplitter()
        {
            AvaloniaXamlLoader.Load(this);
        }

        DockPanel Panel => Parent as DockPanel ?? parentNullObject;

        public Orientation Orientation => Orientation.Horizontal;

        protected override void OnDragStarted(VectorEventArgs e)
        {
            base.OnDragStarted(e);
            isHorizontal = GetIsHorizontal(this);
            isBottomOrRight = GetIsBottomOrRight();
            target = GetTargetOrDefault() ?? targetNullObject;
            initialLength ??= GetTargetLength();
            availableSpace = GetAvailableSpace();
        }
        protected override void OnDragDelta(VectorEventArgs e)
        {
            base.OnDragDelta(e);
            var change = isHorizontal ? e.Vector.Y : e.Vector.X;
            if (isBottomOrRight) change = -change;

            var targetLength = GetTargetLength();
            var newTargetLength = targetLength + change;
            newTargetLength = Clamp(newTargetLength, 0, availableSpace);
            newTargetLength = Math.Round(newTargetLength);

            SetTargetLength(newTargetLength);
        }
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            if (!(Parent is DockPanel))
                throw new InvalidOperationException($"{nameof(DockSplitter)} must be directly in a DockPanel.");

            if (GetTargetOrDefault() == default)
                throw new InvalidOperationException($"{nameof(DockSplitter)} must be directly after a FrameworkElement");

        }
        Control? GetTargetOrDefault()
        {
            var children = Panel.Children.OfType<object>();
            var splitterIndex = Panel.Children.IndexOf(this);
            return children.ElementAtOrDefault(splitterIndex - 1) as Control;
        }

        bool GetIsBottomOrRight()
        {
            var position = DockPanel.GetDock(this);
            return position == Avalonia.Controls.Dock.Bottom || position == Avalonia.Controls.Dock.Right;
        }

        double GetAvailableSpace()
        {
            var lastChild =
                Panel.LastChildFill ?
                Panel.Children.OfType<object>().Last() as Control :
                null;

            var fixedChildren =
                from child in Panel.Children.OfType<Control>()
                where GetIsHorizontal(child) == isHorizontal
                where child != target
                where child != lastChild
                select child;

            var panelLength = GetLength(Panel);
            var unavailableSpace = fixedChildren.Sum(c => GetLength(c));
            return panelLength - unavailableSpace;
        }

        void SetTargetLength(double length)
        {
            if (isHorizontal) target.Height = length;
            else target.Width = length;
        }

        double GetTargetLength() => GetLength(target);

        static bool GetIsHorizontal(Control element)
        {
            var position = DockPanel.GetDock(element);
            return GetIsHorizontal(position);
        }

        static bool GetIsHorizontal(Avalonia.Controls.Dock position)
            => position == Avalonia.Controls.Dock.Top || position == Avalonia.Controls.Dock.Bottom;

        static double Clamp(double value, double min, double max)
            => value < min ? min :
               value > max ? max :
               value;

        double GetLength(Control element)
        {
            return isHorizontal ?
                  element.Bounds.Width :
                  element.Bounds.Width;
        }
    }
}