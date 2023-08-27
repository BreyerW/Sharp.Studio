using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SharpHook;
using System;
using System.Linq;
using System.Windows.Input;

namespace Sharp.DockManager.Behaviours
{
	//TODO: change to DragDropBehaviour and add Drop, DragLeave, DragEnter events, leave/enter based on parent Bounds property
	//resignate from global hooks here instead implement dragleave asap andd here enable hooks while disabling this behaviour
	//Also consider putting this on parent then drag bounds not as necessary
	/*public class DraggableTabsBehaviours
	{

		private static readonly TranslateTransform cachedTransform=new TranslateTransform();
		private static TabItem draggedItem = null;
		private static TabControl startingParent = null;
		private static Dock stripPlacement;
		private static PointerPressedEventArgs draggingPointer;



		#region IsSet Attached Avalonia Property
		public static bool GetIsSet(Control obj)
		{
			return obj.GetValue(IsSetProperty);
		}

		public static void SetIsSet(Control obj, bool value)
		{
			obj.SetValue(IsSetProperty, value);
		}

		public static readonly AttachedProperty<bool> IsSetProperty =
			AvaloniaProperty.RegisterAttached<DraggableTabsBehaviours, Control, bool>
			(
				"IsSet"
			);
		#endregion IsSet Attached Avalonia Property
		#region BlockXAxis Attached Avalonia Property
		public static bool GetBlockXAxis(Control obj)
		{
			return obj.GetValue(BlockXAxisProperty);
		}

		public static void SetBlockXAxis(Control obj, bool value)
		{
			obj.SetValue(BlockXAxisProperty, value);
		}

		public static readonly AttachedProperty<bool> BlockXAxisProperty =
			AvaloniaProperty.RegisterAttached<DraggableTabsBehaviours, Control, bool>
			(
				"BlockXAxis"
			);
		#endregion BlockXAxis Attached Avalonia Property
		#region BlockYAxis Attached Avalonia Property
		public static bool GetBlockYAxis(Control obj)
		{
			return obj.GetValue(BlockYAxisProperty);
		}

		public static void SetBlockYAxis(Control obj, bool value)
		{
			obj.SetValue(BlockYAxisProperty, value);
		}

		public static readonly AttachedProperty<bool> BlockYAxisProperty =
			AvaloniaProperty.RegisterAttached<DraggableTabsBehaviours, Control, bool>
			(
				"BlockYAxis"
			);
		#endregion BlockYAxis Attached Avalonia Property
		#region DragBounds Attached Avalonia Property
		public static Rect GetDragBounds(Control obj)
		{
			return obj.GetValue(DragBoundsProperty);
		}

		public static void SetDragBounds(Control obj, Rect value)
		{
			obj.SetValue(DragBoundsProperty, value);
		}
		//TODO: change name to DragBoundsOverride
		public static readonly AttachedProperty<Rect> DragBoundsProperty =
			AvaloniaProperty.RegisterAttached<DraggableTabsBehaviours, Control, Rect>
			(
				"DragBounds"
			);
		#endregion DragBounds Attached Avalonia Property
		#region OnDragDrop Attached Avalonia Property
		public static Action<object, PointerReleasedEventArgs>? GetOnDrop(Control obj)
		{
			return obj.GetValue(OnDropProperty);
		}

		public static void SetOnDrop(Control obj, Action<object, PointerReleasedEventArgs>? value)
		{
			obj.SetValue(OnDropProperty, value);
		}
		public static readonly AttachedProperty<Action<object, PointerReleasedEventArgs>?> OnDropProperty = AvaloniaProperty.RegisterAttached<DraggableTabsBehaviours, TabControl, Action<object,PointerReleasedEventArgs>?>
			(
				"OnDrop"
			);
		#endregion OnDragDrop Attached Avalonia Property
		private static Point GetShift(Control control)
		{
			return new Point(cachedTransform.X, cachedTransform.Y);
		}

		private static void SetShift(Control sender, Control control, Point shift)
		{
			cachedTransform.X = shift.X;
			cachedTransform.Y = shift.Y;
			var dragBounds = GetDragBounds(sender);
			//TODO: add Inflate/Bounds (or Padding)
			//dragBounds.Inflate();
			if (dragBounds.Width > 0)
					cachedTransform.X=Math.Clamp(shift.X, 0, dragBounds.Width - control.Bounds.Width);
				if (dragBounds.Height > 0)
					cachedTransform.Y=Math.Clamp(shift.Y, 0, dragBounds.Height - control.Bounds.Height);

		}

		#region InitialPointerLocation Attached Avalonia Property
		private static Point GetInitialPointerLocation(Control obj)
		{
			return obj.GetValue(InitialPointerLocationProperty);
		}

		private static void SetInitialPointerLocation(Control obj, Point value)
		{
			obj.SetValue(InitialPointerLocationProperty, value);
		}

		private static readonly AttachedProperty<Point> InitialPointerLocationProperty =
			AvaloniaProperty.RegisterAttached<DraggableTabsBehaviours, Control, Point>
			(
				"InitialPointerLocation"
			);
		#endregion InitialPointerLocation Attached Avalonia Property


		#region InitialDragShift Attached Avalonia Property
		public static Point GetInitialDragShift(Control obj)
		{
			return obj.GetValue(InitialDragShiftProperty);
		}

		public static void SetInitialDragShift(Control obj, Point value)
		{
			obj.SetValue(InitialDragShiftProperty, value);
		}

		public static readonly AttachedProperty<Point> InitialDragShiftProperty =
			AvaloniaProperty.RegisterAttached<DraggableTabsBehaviours, Control, Point>
			(
				"InitialDragShift"
			);
		#endregion InitialDragShift Attached Avalonia Property

		static DraggableTabsBehaviours()
		{
			IsSetProperty.Changed.AddClassHandler<Control, bool>(OnIsSetChanged);
			InputElement.PointerPressedEvent.AddClassHandler<TabItem>((s,e)=>{
				var parent = s.FindAncestorOfType<TabControl>();
				//while (parent is { Name : not "PART_ItemsPresenter" })
				//	parent=parent.FindAncestorOfType<ContentPresenter>();
				if (GetIsSet(parent))
				{
					startingParent = parent;
					stripPlacement = parent.TabStripPlacement;
					s.ZIndex = int.MaxValue;
					draggedItem = s;
					Control_PointerPressed(parent, e);
				}
			},handledEventsToo:true);
			InputElement.PointerMovedEvent.AddClassHandler<ContentPresenter>((s, e) => {
				if (draggedItem is not null && GetIsSet(startingParent))
				{
					Control_PointerMoved(draggedItem, e);
				}
			}, handledEventsToo: true);
			InputElement.PointerReleasedEvent.AddClassHandler<ContentPresenter>((s, e) => {
				if (draggedItem is not null)
				{
					Control_PointerReleased(draggedItem, e);
				}
			}, handledEventsToo: true);
		}

		// set the PointerPressed handler when 
		private static void OnIsSetChanged(Control s, AvaloniaPropertyChangedEventArgs<bool> args)
		{
			Control control = (Control)args.Sender;
			
			if (args.NewValue.Value == true)
			{
				// connect the pointer pressed event handler
				//control.PointerPressed += Control_PointerPressed;
			}
			else
			{
				// disconnect the pointer pressed event handler
				//control.PointerPressed -= Control_PointerPressed;
			}
		}

		private static Window GetWindow(Control control)
		{
			return control.GetVisualAncestors().OfType<Window>().FirstOrDefault()!;
		}

		public static Point GetCurrentPointerPositionInWindow(Control control, PointerEventArgs e)
		{
			return e.GetPosition(GetWindow(control));
		}

		// start drag by pressing the point on draggable control
		private static void Control_PointerPressed(object? sender, PointerPressedEventArgs e)
		{
			Control control = draggedItem!;
			control.RenderTransform = cachedTransform;
			// capture the pointer on the control
			// meaning - the mouse pointer will be producing the
			// pointer events on the control
			// even if it is not directly above the control
			e.Pointer.Capture(sender as Control);

			draggingPointer = e;
			// calculate the drag-initial pointer position within the window
			Point currentPointerPositionInWindow = GetCurrentPointerPositionInWindow(control, e);

			// record the drag-initial pointer position within the window
			SetInitialPointerLocation(control, currentPointerPositionInWindow);

			Point startControlPosition = GetShift(control);

			// record the drag-initial shift of the control
			SetInitialDragShift(control, startControlPosition);

			// add handler to do the shift and 
			// other processing on PointerMoved
			// and PointerReleased events. 
			//control.PointerMoved += Control_PointerMoved;
			//control.PointerReleased += Control_PointerReleased;
			
		}

		// update the shift when pointer is moved
		private static void Control_PointerMoved(object? sender, PointerEventArgs e)
		{
			Control control = draggedItem;
			// Shift control to the current position
			ShiftControl(sender as Control, control, e);

		}


		// Drag operation ends when the pointer is released. 
		private static void Control_PointerReleased(object? sender, PointerReleasedEventArgs e)
		{
			Control control = draggedItem!;

			// release the capture
			e.Pointer.Capture(null);
			//ShiftControl(control, e);

			cachedTransform.X = 0;
			cachedTransform.Y = 0;
			
			GetOnDrop(startingParent)?.Invoke(sender, e);
			// disconnect the handlers
			control.RenderTransform = null; 
			draggedItem = null;
		}


		// modifies the shift on the control during the drag
		// this essentially moves the control
		private static void ShiftControl(Control sender, Control control, PointerEventArgs e)
		{
			// get the current pointer location
			Point currentPointerPosition = GetCurrentPointerPositionInWindow(control, e);

			// get the pointer location when Drag operation was started
			Point startPointerPosition = GetInitialPointerLocation(control);

			// diff is how far the pointer shifted
			Point diff = currentPointerPosition - startPointerPosition;
			if (stripPlacement is Dock.Right or Dock.Left)
				diff = diff.WithX(0);
			else
				diff = diff.WithY(0);

			// get the original shift when the drag operation started
			Point startControlPosition = GetInitialDragShift(control);

			// get the resulting shift as the sum of 
			// pointer shift during the drag and the original shift
			Point shift = diff + startControlPosition;

			// set the shift on the control
			SetShift(sender, control, shift);
		}
	}*/
}
