using Markdig;
using Markdig.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xaml;

namespace UltimateCallout
{
	/// <summary>
	/// Interaction logic for FrmUltimateCallout.xaml
	/// </summary>
	public partial class FrmUltimateCallout : Window
	{
		public CalloutOptions Options { get; set; } = new CalloutOptions();
		public FrmUltimateCallout()
		{
			InitializeComponent();
		}

		void InvalidateLayout()
		{
			layoutValid = false;
		}

		void AddTriangle()
		{
			// DetermineCalloutPointOffset();
		}

		void PlaceCloseButton()
		{
			Button closeButton = new Button();
			closeButton.Content = "X";
			closeButton.Width = 20;
			closeButton.Height = 20;
			closeButton.Click += CloseButton_Click;
			cvsCallout.Children.Add(closeButton);
			double rightEdge = roundedRectLeft + roundedRectWidth;
			Canvas.SetLeft(closeButton, rightEdge - Options.CornerRadius - closeButton.Width);
			Canvas.SetTop(closeButton, roundedRectTop + Options.CornerRadius);
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		void CalculateBounds()
		{
			roundedRectHeight = 250;
			roundedRectWidth = roundedRectHeight * CalloutOptions.GoldenRatio;
			roundedRectTop = Options.OuterMargin;
			roundedRectLeft = Options.OuterMargin;
			Width = roundedRectWidth + Options.OuterMargin * 2;
			Height = roundedRectHeight + Options.OuterMargin * 2;
		}

		void CreateCallout()
		{
			System.Windows.Shapes.Path calloutPath = new System.Windows.Shapes.Path()
			{
				Stroke = Brushes.Black,
				StrokeThickness	= 1,
				Fill = Brushes.AliceBlue
			};
			CombinedGeometry combinedGeometry = new CombinedGeometry() { GeometryCombineMode = GeometryCombineMode.Union };
			calloutPath.Data = combinedGeometry;

			RectangleGeometry rectangleGeometry = new RectangleGeometry();
			rectangleGeometry.RadiusX = Options.CornerRadius;
			rectangleGeometry.RadiusY = Options.CornerRadius;

			Rect rect = new Rect();
			rect.Width = roundedRectWidth;
			rect.Height = roundedRectHeight;
			rect.Location = new Point(roundedRectLeft, roundedRectTop);
			rectangleGeometry.Rect = rect;

			combinedGeometry.Geometry1 = rectangleGeometry;

			StreamGeometry triangleGeometry = new StreamGeometry();
			using (StreamGeometryContext ctx = triangleGeometry.Open())
			{
				ctx.BeginFigure(new Point(0, 0), true, true);
				ctx.LineTo(new Point(roundedRectWidth / 2.0, roundedRectTop), true, true);
				ctx.LineTo(new Point(roundedRectLeft, roundedRectHeight / 2.0), true, true);
			}
			triangleGeometry.Freeze();

			combinedGeometry.Geometry2 = triangleGeometry;

			cvsCallout.Children.Add(calloutPath);

			//Rectangle oldRect = new Rectangle();
			//rect.RadiusX = 6;
			//rect.RadiusY = 6;
			//oldRect.Width = roundedRectWidth;
			//oldRect.Height = roundedRectHeight;
			//oldRect.Fill = new SolidColorBrush(Colors.AliceBlue);
			//oldRect.Stroke = new SolidColorBrush(Colors.Blue);
			//cvsCallout.Children.Add(oldRect);
			//Canvas.SetLeft(oldRect, roundedRectLeft);
			//Canvas.SetTop(oldRect, roundedRectTop);
		}

		void LayoutText()
		{
			MarkdownViewer markdownViewer = new MarkdownViewer();
			markdownViewer.Markdown = markDownText;
			markdownViewer.Padding = new Thickness(0);
			markdownViewer.Margin = new Thickness(0);
			markdownViewer.IsHitTestVisible = false;
			const double marginAdjust = 10d;
			
			markdownViewer.Width = roundedRectWidth + marginAdjust * 2;
			markdownViewer.Height = roundedRectHeight + marginAdjust * 2;

			Canvas.SetLeft(markdownViewer, roundedRectLeft + Options.CornerRadius - marginAdjust);
			Canvas.SetTop(markdownViewer, roundedRectTop + Options.CornerRadius - marginAdjust);
			cvsCallout.Children.Add(markdownViewer);
		}

		/// <summary>
		/// Rotates one point around another
		/// </summary>
		/// <param name="pointToRotate">The point to rotate.</param>
		/// <param name="centerPoint">The center point of rotation.</param>
		/// <param name="angleInDegrees">The rotation angle in degrees.</param>
		/// <returns>Rotated point</returns>
		static Point RotatePoint(Point pointToRotate, Point centerPoint, double angleInDegrees)
		{
			double angleInRadians = angleInDegrees * (Math.PI / 180);
			double cosTheta = Math.Cos(angleInRadians);
			double sinTheta = Math.Sin(angleInRadians);
			return new Point
			{
				X =
					(int)
					(cosTheta * (pointToRotate.X - centerPoint.X) -
					sinTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.X),
				Y =
					(int)
					(sinTheta * (pointToRotate.X - centerPoint.X) +
					cosTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.Y)
			};
		}

		double GetDistanceToIntersection(MyLine testLine, MyLine topLine)
		{
			Point intersection = testLine.GetSegmentIntersection(topLine);
			if (double.IsNaN(intersection.X))
				return double.MaxValue;
			double deltaX = intersection.X - targetCenter.X;
			double deltaY = intersection.Y - targetCenter.Y;
			return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
		}

		void PositionWindow()
		{
			targetCenter = target.PointToScreen(new Point(target.Width / 2, target.Height / 2));
			// TODO: Calculate the distance based on the angle and the aspect ratio or size of the rounded rect.

			Options.InitialAngle = 45;

			double distance = 250;
			Point calloutStartPos = target.PointToScreen(new Point(target.Width / 2, target.Height / 2 - distance));
			Point calloutCenterPoint = RotatePoint(calloutStartPos, targetCenter, Options.InitialAngle);

			Point infiniteCalloutStartPos = target.PointToScreen(new Point(target.Width / 2, target.Height / 2 - 222222));
			Point infiniteCalloutCenterPoint = RotatePoint(infiniteCalloutStartPos, targetCenter, Options.InitialAngle);

			MyLine testLine = new MyLine(targetCenter, infiniteCalloutCenterPoint);

			Left = calloutCenterPoint.X - (Options.OuterMargin + roundedRectWidth / 2);
			Top = calloutCenterPoint.Y - (Options.OuterMargin + roundedRectHeight / 2);

			double right = Left + roundedRectWidth + 2 * Options.OuterMargin;
			double bottom = Top + roundedRectHeight + 2 * Options.OuterMargin;

			MyLine leftLine = MyLine.Vertical(Left, Top, bottom);
			MyLine rightLine = MyLine.Vertical(right, Top, bottom);

			MyLine topLine = MyLine.Horizontal(Left, right, Top);
			MyLine bottomLine = MyLine.Horizontal(Left, right, bottom);
			
			double topDistanceToIntersection = GetDistanceToIntersection(testLine, topLine);
			double leftDistanceToIntersection = GetDistanceToIntersection(testLine, leftLine);
			double rightDistanceToIntersection = GetDistanceToIntersection(testLine, rightLine);
			double bottomDistanceToIntersection = GetDistanceToIntersection(testLine, bottomLine);

		}

		void LayoutEverything()
		{
			if (layoutValid)
				return;

			cvsCallout.Children.Clear();
			CalculateBounds();
			CreateCallout();
			PlaceCloseButton();
			LayoutText();
			AddTriangle();
			PositionWindow();
			layoutValid = true;
		}

		Window? targetParentWindow;
		bool layoutValid;
		double roundedRectWidth;
		double roundedRectHeight;
		double roundedRectLeft;
		double roundedRectTop;
		string markDownText;
		FrameworkElement target;
		Point targetCenter;

		public void PointTo(FrameworkElement target)
		{
			this.target = target;
			Point pointToScreen = target.PointToScreen(new Point(target.Width / 2, target.Height / 2));
			this.Left = pointToScreen.X;
			this.Top = pointToScreen.Y;

			targetParentWindow = Window.GetWindow(target);
			HookTargetParentWindowEvents();

			LayoutEverything();
		}

		void HookTargetParentWindowEvents()
		{
			if (targetParentWindow == null)
				return;
			targetParentWindow.Closed += ParentWindow_Closed;
			targetParentWindow.Activated += TargetParentWindow_Activated;
			targetParentWindow.Deactivated += TargetParentWindow_Deactivated;
		}

		private void TargetParentWindow_Deactivated(object? sender, EventArgs e)
		{
			CheckTopMostWindow();
		}

		private void TargetParentWindow_Activated(object? sender, EventArgs e)
		{
			CheckTopMostWindow();
		}

		private void UnhookTargetParentWindowEvents()
		{
			if (targetParentWindow == null)
				return;
			targetParentWindow.Closed -= ParentWindow_Closed;
		}
		
		public static FrmUltimateCallout ShowCallout(string markDownText, FrameworkElement target)
		{
			FrmUltimateCallout frmUltimateCallout = new FrmUltimateCallout();
			frmUltimateCallout.markDownText = markDownText;
			frmUltimateCallout.PointTo(target);
			frmUltimateCallout.Show();
			

			return frmUltimateCallout;
		}

		private void ParentWindow_Closed(object? sender, EventArgs e)
		{
			Close();
		}

		private void Callout_Closed(object sender, EventArgs e)
		{
			UnhookTargetParentWindowEvents();
		}



		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				this.DragMove();
		}

		private void Window_Activated(object sender, EventArgs e)
		{
			CheckTopMostWindow();
		}

		private void Window_Deactivated(object sender, EventArgs e)
		{
			CheckTopMostWindow();
		}

		void CheckTopMostWindow()
		{
			if (targetParentWindow != null)
				Topmost = WindowHelper.IsForegroundWindow(targetParentWindow);
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			WindowInteropHelper wndHelper = new WindowInteropHelper(this);
			WindowHelper.HideFromAltTab(this);
		}
	}
}
