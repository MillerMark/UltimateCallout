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
			double rightEdge = calloutLeft + calloutWidth;
			Canvas.SetLeft(closeButton, rightEdge - Options.CornerRadius - closeButton.Width);
			Canvas.SetTop(closeButton, calloutTop + Options.CornerRadius);
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		void CalculateBounds()
		{
			calloutHeight = 250;
			calloutWidth = calloutHeight * Options.AspectRatio;
			calloutTop = Options.OuterMargin;
			calloutLeft = Options.OuterMargin;
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
			rect.Location = new Point(calloutLeft, calloutTop);
			rectangleGeometry.Rect = rect;

			combinedGeometry.Geometry1 = rectangleGeometry;

			StreamGeometry triangleGeometry = new StreamGeometry();
			using (StreamGeometryContext ctx = triangleGeometry.Open())
			{
				// No impact yet.
				ctx.BeginFigure(new Point(calloutWidth / 2.0, calloutHeight / 2.0), true, true);
				ctx.LineTo(new Point(calloutWidth / 2.0, calloutTop), true, true);
				ctx.LineTo(new Point(calloutLeft, calloutHeight / 2.0), true, true);
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

			Canvas.SetLeft(markdownViewer, calloutLeft + Options.CornerRadius - marginAdjust);
			Canvas.SetTop(markdownViewer, calloutTop + Options.CornerRadius - marginAdjust);
			cvsCallout.Children.Add(markdownViewer);
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

		GuidelineIntersectionData GetGuidelineIntersectionData(MyLine testLine, double windowLeft, double windowTop)
		{
			double right = windowLeft + calloutWidth + 2 * Options.OuterMargin;
			double bottom = windowTop + calloutHeight + 2 * Options.OuterMargin;

			MyLine leftLine = MyLine.Vertical(windowLeft, windowTop, bottom);
			MyLine rightLine = MyLine.Vertical(right, windowTop, bottom);

			MyLine topLine = MyLine.Horizontal(windowLeft, right, windowTop);
			MyLine bottomLine = MyLine.Horizontal(windowLeft, right, bottom);

			GuidelineIntersectionData guidelineIntersectionData = new GuidelineIntersectionData();

			guidelineIntersectionData.Top = topLine;
			guidelineIntersectionData.Left = leftLine;
			guidelineIntersectionData.Right = rightLine;
			guidelineIntersectionData.Bottom = bottomLine;

			SetCalloutSide(testLine, guidelineIntersectionData);

			return guidelineIntersectionData;
		}

		void ShowIntersectedSide(CalloutSide side)
		{
			const double indicatorThickness = 7d;
			Rectangle sideIndicator = new Rectangle();
			switch (side)
			{
				case CalloutSide.Left:
				case CalloutSide.Right:
					sideIndicator.Width = indicatorThickness;
					sideIndicator.Height = calloutHeight;
					Canvas.SetTop(sideIndicator, Options.OuterMargin);
					if (side == CalloutSide.Right)
						Canvas.SetLeft(sideIndicator, calloutWidth + Options.OuterMargin - indicatorThickness);
					else
						Canvas.SetLeft(sideIndicator, Options.OuterMargin);
					break;
				case CalloutSide.Top:
				case CalloutSide.Bottom:
					sideIndicator.Width = calloutWidth;
					sideIndicator.Height = indicatorThickness;
					Canvas.SetLeft(sideIndicator, Options.OuterMargin);
					if (side == CalloutSide.Bottom)
						Canvas.SetTop(sideIndicator, calloutHeight + Options.OuterMargin - indicatorThickness);
					else
						Canvas.SetTop(sideIndicator, Options.OuterMargin);
					break;
			}
			sideIndicator.Fill = Brushes.Blue;
			sideIndicator.Opacity = 0.25;
			cvsCallout.Children.Add(sideIndicator);
		}

		void PositionWindow()
		{
			targetCenter = target.PointToScreen(new Point(target.Width / 2, target.Height / 2));
			// TODO: Calculate the distance based on the angle and the aspect ratio or size of the rounded rect.


			const int almostInfiniteDistance = 222222;
			RotateCalloutToGetPosition(almostInfiniteDistance, out double windowLeft, out double windowTop);

			Point infiniteCalloutStartPos = target.PointToScreen(new Point(target.Width / 2, target.Height / 2 - almostInfiniteDistance));
			Point infiniteCalloutCenterPoint = MathEx.RotatePoint(infiniteCalloutStartPos, targetCenter, Options.InitialAngle);

			MyLine testLine = new MyLine(targetCenter, infiniteCalloutCenterPoint);

			GuidelineIntersectionData guidelineIntersectionData = GetGuidelineIntersectionData(testLine, windowLeft, windowTop);
			SetTopLeft(guidelineIntersectionData);

			//Point trianglePoint = GetTrianglePoint(guidelineIntersectionData);
			double distance = GetDistance(guidelineIntersectionData);

			//! Seems to fail by about 180° when InitialAngle is between 235.6° and 303°.

			RotateCalloutToGetPosition(distance, out windowLeft, out windowTop);
			Left = windowLeft;
			Top = windowTop;

			ShowIntersectedSide(guidelineIntersectionData.Side);
		}

		private void RotateCalloutToGetPosition(double distance, out double windowLeft, out double windowTop)
		{
			Point calloutStartPos = target.PointToScreen(new Point(target.Width / 2, target.Height / 2 - distance));
			Point calloutCenterPoint = MathEx.RotatePoint(calloutStartPos, targetCenter, Options.InitialAngle);
			windowLeft = calloutCenterPoint.X - (Options.OuterMargin + calloutWidth / 2);
			windowTop = calloutCenterPoint.Y - (Options.OuterMargin + calloutHeight / 2);
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
			double angleDegrees = 90 - Options.InitialAngle;

			if (Math.Abs(angleDegrees) == 180)
				return opposite;

			double angleRadians = angleDegrees * Math.PI / 180;
			return Math.Abs(opposite / Math.Sin(angleRadians));
		}

		void PlaceGuideline()
		{
			Point centerPoint = new Point(calloutWidth / 2 + Options.OuterMargin, calloutHeight /2 + Options.OuterMargin);
			Line angleGuideline = MathEx.GetRotatedLine(centerPoint, Options.InitialAngle + 180);
			cvsCallout.Children.Add(angleGuideline);

			Rectangle outerMarginRect = new Rectangle();
			outerMarginRect.Width = calloutWidth + 2 * Options.OuterMargin;
			outerMarginRect.Height = calloutHeight + 2 * Options.OuterMargin;
			outerMarginRect.Stroke = Brushes.Purple;
			cvsCallout.Children.Add(outerMarginRect);
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
			PlaceGuideline();
			AddTriangle();
			PositionWindow();
			layoutValid = true;
		}

		Window? targetParentWindow;
		bool layoutValid;
		double calloutWidth;
		double calloutHeight;
		double calloutLeft;
		double calloutTop;
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
		
		public static FrmUltimateCallout ShowCallout(string markDownText, FrameworkElement target, double angle, double aspectRatio)
		{
			FrmUltimateCallout frmUltimateCallout = new FrmUltimateCallout();
			frmUltimateCallout.Options.InitialAngle = angle;
			frmUltimateCallout.Options.AspectRatio = aspectRatio;
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
