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
using System.Windows.Media.TextFormatting;
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
			double rightEdge = roundedRectLeft + calloutWidth;
			Canvas.SetLeft(closeButton, rightEdge - Options.CornerRadius - closeButton.Width);
			Canvas.SetTop(closeButton, roundedRectTop + Options.CornerRadius);
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		void CalculateBounds()
		{
			calloutHeight = 250;
			calloutWidth = calloutHeight * CalloutOptions.GoldenRatio;
			roundedRectTop = Options.OuterMargin;
			roundedRectLeft = Options.OuterMargin;
			Width = calloutWidth + Options.OuterMargin * 2;
			Height = calloutHeight + Options.OuterMargin * 2;
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
			rect.Width = calloutWidth;
			rect.Height = calloutHeight;
			rect.Location = new Point(roundedRectLeft, roundedRectTop);
			rectangleGeometry.Rect = rect;

			combinedGeometry.Geometry1 = rectangleGeometry;

			StreamGeometry triangleGeometry = new StreamGeometry();
			using (StreamGeometryContext ctx = triangleGeometry.Open())
			{
				// No impact yet.
				ctx.BeginFigure(new Point(calloutWidth / 2.0, calloutHeight / 2.0), true, true);
				ctx.LineTo(new Point(calloutWidth / 2.0, roundedRectTop), true, true);
				ctx.LineTo(new Point(roundedRectLeft, calloutHeight / 2.0), true, true);
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
			
			markdownViewer.Width = calloutWidth + marginAdjust * 2;
			markdownViewer.Height = calloutHeight + marginAdjust * 2;

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

		void SetCalloutSide(MyLine testLine, GuidelineIntersectionData data)
		{
			double topDistance = GetDistanceToIntersection(testLine, data.Top);
			double leftDistance = GetDistanceToIntersection(testLine, data.Left);
			double rightDistance = GetDistanceToIntersection(testLine, data.Right);
			double bottomDistance = GetDistanceToIntersection(testLine, data.Bottom);

			double minDistance = Min(topDistance, leftDistance, rightDistance, bottomDistance);

			if (minDistance == topDistance)
				data.Side = CalloutSide.Top;
			else if (minDistance == rightDistance)
				data.Side = CalloutSide.Right;
			else if (minDistance == bottomDistance)
				data.Side = CalloutSide.Bottom;
			else if (minDistance == leftDistance)
				data.Side = CalloutSide.Left;
		}

		private static double Min(params double[] args) => args.Min();

		GuidelineIntersectionData GetGuidelineIntersectionData(MyLine testLine, double left, double top)
		{
			double right = left + calloutWidth + 2 * Options.OuterMargin;
			double bottom = top + calloutHeight + 2 * Options.OuterMargin;

			MyLine leftLine = MyLine.Vertical(left, top, bottom);
			MyLine rightLine = MyLine.Vertical(right, top, bottom);

			MyLine topLine = MyLine.Horizontal(left, right, top);
			MyLine bottomLine = MyLine.Horizontal(left, right, bottom);

			GuidelineIntersectionData guidelineIntersectionData = new GuidelineIntersectionData();

			guidelineIntersectionData.Top = topLine;
			guidelineIntersectionData.Left = leftLine;
			guidelineIntersectionData.Right = rightLine;
			guidelineIntersectionData.Bottom = bottomLine;

			SetCalloutSide(testLine, guidelineIntersectionData);

			return guidelineIntersectionData;
		}

		void PositionWindow()
		{
			targetCenter = target.PointToScreen(new Point(target.Width / 2, target.Height / 2));
			// TODO: Calculate the distance based on the angle and the aspect ratio or size of the rounded rect.


			double distance = 250;

			const int almostInfiniteDistance = 222222;
			RotateCalloutToGetPosition(almostInfiniteDistance, out double left, out double top);

			Point infiniteCalloutStartPos = target.PointToScreen(new Point(target.Width / 2, target.Height / 2 - almostInfiniteDistance));
			Point infiniteCalloutCenterPoint = RotatePoint(infiniteCalloutStartPos, targetCenter, Options.InitialAngle);

			MyLine testLine = new MyLine(targetCenter, infiniteCalloutCenterPoint);

			GuidelineIntersectionData guidelineIntersectionData = GetGuidelineIntersectionData(testLine, left, top);
			SetTopLeft(guidelineIntersectionData);

			//Point trianglePoint = GetTrianglePoint(guidelineIntersectionData);
			distance = GetDistance(guidelineIntersectionData);

			//! Seems to fail by about 180° when InitialAngle is between 235.6° and 303°.

			RotateCalloutToGetPosition(distance, out left, out top);
			Left = left;
			Top = top;
		}

		private void RotateCalloutToGetPosition(double distance, out double left, out double top)
		{
			Point calloutStartPos = target.PointToScreen(new Point(target.Width / 2, target.Height / 2 - distance));
			Point calloutCenterPoint = RotatePoint(calloutStartPos, targetCenter, Options.InitialAngle);
			left = calloutCenterPoint.X - (Options.OuterMargin + calloutWidth / 2);
			top = calloutCenterPoint.Y - (Options.OuterMargin + calloutHeight / 2);
		}

		private static void SetTopLeft(GuidelineIntersectionData guidelineIntersectionData)
		{

			switch (guidelineIntersectionData.Side)
			{
				case CalloutSide.Left:

					break;
				case CalloutSide.Top:

					break;
				case CalloutSide.Right:

					break;
				case CalloutSide.Bottom:

					break;
			}
		}

		double GetDistance(GuidelineIntersectionData data)
		{
			switch (data.Side)
			{
				case CalloutSide.Left:
				case CalloutSide.Right:
					return GetDistanceCalloutCenterHorizontal();

				case CalloutSide.Top:
				case CalloutSide.Bottom:
					return GetDistanceCalloutCenterVertical();
			}
			throw new Exception($"Side invalid");
		}

		private double GetDistanceCalloutCenterHorizontal()
		{
			// ![](DFBD074F29E3C8F3F2151FE6478C8DA4.png)
			double a = target.Width / 2 + Options.Spacing;
			double c = calloutWidth / 2 + Options.OuterMargin;
			double adjacent = a + c;
			double angleDegrees;
			double angle = Options.InitialAngle % 360.0;
			angleDegrees = 90 - Options.InitialAngle;

			double angleRadians = angleDegrees * Math.PI / 180;
			return adjacent / Math.Cos(angleRadians);
		}

		private double GetDistanceCalloutCenterVertical()
		{
			// ![](32D1DA49A62BFFFA4ECADEFA182E1FDB.png)
			double b = target.Height / 2 + Options.Spacing;
			double d = calloutHeight / 2 + Options.OuterMargin;
			double opposite = b + d;
			double angleDegrees;
			//double angle = Options.InitialAngle;
			angleDegrees = 90 - Options.InitialAngle;
			//if (angle < 90)
			//	angleDegrees = 90 - Options.InitialAngle;
			//else if (angle < 180)
			//	angleDegrees = 90 - Options.InitialAngle;
			//else if (angle < 270)
			//	angleDegrees = 270 - Options.InitialAngle;
			//else
			//	angleDegrees = 360 - Options.InitialAngle;

			if (Math.Abs(angleDegrees) == 180)
				return opposite;

			double angleRadians = angleDegrees * Math.PI / 180;
			return Math.Abs(opposite / Math.Sin(angleRadians));
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
		double calloutWidth;
		double calloutHeight;
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
		
		public static FrmUltimateCallout ShowCallout(string markDownText, FrameworkElement target, double angle)
		{
			FrmUltimateCallout frmUltimateCallout = new FrmUltimateCallout();
			frmUltimateCallout.Options.InitialAngle = angle;
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
