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
using static System.Net.WebRequestMethods;

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

		const double closeButtonEdgeSize = 16d;

		void PlaceCloseButton()
		{
			Button closeButton = new Button();
			closeButton.Background = new SolidColorBrush(Color.FromRgb(222, 245, 255));
			closeButton.Foreground = new SolidColorBrush(Color.FromRgb(69, 133, 161));
			closeButton.BorderBrush = new SolidColorBrush(Color.FromRgb(69, 133, 161));
			closeButton.Content = "x";
			closeButton.Padding = new Thickness(0, -6, 0, 0);
			closeButton.FontSize = closeButtonEdgeSize;
			closeButton.Width = closeButtonEdgeSize;
			closeButton.Height = closeButtonEdgeSize;
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
			calloutHeight = Options.Height;
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
				Stroke = new SolidColorBrush(Color.FromRgb(72, 130, 156)),
				StrokeThickness = 1,
				Fill = new SolidColorBrush(Color.FromRgb(245, 252, 255))
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
				ctx.BeginFigure(trianglePoint1, true, true);
				ctx.LineTo(trianglePoint2, true, true);
				ctx.LineTo(trianglePoint3, true, true);
			}
			triangleGeometry.Freeze();

			combinedGeometry.Geometry2 = triangleGeometry;

			cvsCallout.Children.Add(calloutPath);
		}

		void LayoutText()
		{
			MarkdownViewer markdownViewer = new MarkdownViewer();
			LoadStyles(markdownViewer);
			markdownViewer.Markdown = markDownText;
			markdownViewer.Padding = new Thickness(0);
			markdownViewer.Margin = new Thickness(0);
			markdownViewer.IsHitTestVisible = false;
			const double leftExtension = 14d;
			const double topExtension = 16d;
			const double rightExtension = 2d;
			const double bottomExtension = 10d;

			markdownViewer.Loaded += MarkdownViewer_Loaded;

			markdownViewer.Width = leftExtension + calloutWidth + rightExtension;
			markdownViewer.Height = topExtension + calloutHeight + bottomExtension;
			Canvas.SetLeft(markdownViewer, calloutLeft + Options.CornerRadius - leftExtension);
			Canvas.SetTop(markdownViewer, calloutTop + Options.CornerRadius - topExtension);
			cvsCallout.Children.Add(markdownViewer);
		}

		private void MarkdownViewer_Loaded(object sender, RoutedEventArgs e)
		{
			MarkdownViewer? markdownViewer = sender as MarkdownViewer;
			ReserveSpaceForCloseButton(markdownViewer);
		}

		/// <summary>
		/// Adds a figure to the layout to reserve space for the close button so words don't wrap behind it.
		/// </summary>
		private static void ReserveSpaceForCloseButton(MarkdownViewer? markdownViewer)
		{
			if (markdownViewer == null)
				return;
			
			FlowDocument? flowDocument = markdownViewer.Document;
			if (flowDocument == null)
				return;
			
			Block firstBlock = flowDocument.Blocks.First();
			if (firstBlock == null)
				return;
			
			if (!(firstBlock is Paragraph paragraph))
				return;
			
			Figure closeButtonFigure = new Figure();
			closeButtonFigure.Width = new FigureLength(closeButtonEdgeSize * 0.6, FigureUnitType.Pixel);
			closeButtonFigure.Height = new FigureLength(closeButtonEdgeSize / 3, FigureUnitType.Pixel);
			closeButtonFigure.HorizontalAnchor = FigureHorizontalAnchor.PageRight;
			closeButtonFigure.HorizontalOffset = 0;
			closeButtonFigure.VerticalOffset = 0;
			closeButtonFigure.Margin = new Thickness(0);
			closeButtonFigure.Padding = new Thickness(0);
			paragraph.Inlines.InsertBefore(paragraph.Inlines.FirstInline, closeButtonFigure);
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
			double topDistance = GetDistanceToIntersection(testLine, data.CalloutTop);
			double leftDistance = GetDistanceToIntersection(testLine, data.CalloutLeft);
			double rightDistance = GetDistanceToIntersection(testLine, data.CalloutRight);
			double bottomDistance = GetDistanceToIntersection(testLine, data.CalloutBottom);

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
			double calloutLeft = windowLeft + Options.OuterMargin;
			double calloutTop = windowTop + Options.OuterMargin;
			double calloutRight = calloutLeft + calloutWidth;
			double calloutBottom = calloutTop + calloutHeight;

			double windowRight = windowLeft + calloutWidth + 2 * Options.OuterMargin;
			double windowBottom = windowTop + calloutHeight + 2 * Options.OuterMargin;

			GuidelineIntersectionData guidelineIntersectionData = new GuidelineIntersectionData();

			guidelineIntersectionData.CalloutTop = MyLine.Horizontal(calloutLeft, calloutRight, calloutTop);
			guidelineIntersectionData.CalloutLeft = MyLine.Vertical(calloutLeft, calloutTop, calloutBottom);
			guidelineIntersectionData.CalloutRight = MyLine.Vertical(calloutRight, calloutTop, calloutBottom);
			guidelineIntersectionData.CalloutBottom = MyLine.Horizontal(calloutLeft, calloutRight, calloutBottom);

			guidelineIntersectionData.WindowTop = MyLine.Horizontal(windowLeft, windowRight, windowTop);
			guidelineIntersectionData.WindowLeft = MyLine.Vertical(windowLeft, windowTop, windowBottom);
			guidelineIntersectionData.WindowRight = MyLine.Vertical(windowRight, windowTop, windowBottom);
			guidelineIntersectionData.WindowBottom = MyLine.Horizontal(windowLeft, windowRight, windowBottom);

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

		Point ScreenToCanvasPoint(Point screenPoint, double windowLeft, double windowTop)
		{
			return new Point(screenPoint.X - windowLeft, screenPoint.Y - windowTop);
		}

		void GetTrianglePoints(GuidelineIntersectionData guidelineIntersectionData, double windowLeft, double windowTop)
		{
			MyLine guideline = MathEx.GetRotatedMyLine(targetCenter, Options.InitialAngle);
			var triangleScreenPoint1 = guidelineIntersectionData.Side switch
			{
				CalloutSide.Right => guideline.GetSegmentIntersection(guidelineIntersectionData.WindowRight),
				CalloutSide.Left => guideline.GetSegmentIntersection(guidelineIntersectionData.WindowLeft),
				CalloutSide.Bottom => guideline.GetSegmentIntersection(guidelineIntersectionData.WindowBottom),
				CalloutSide.Top => guideline.GetSegmentIntersection(guidelineIntersectionData.WindowTop),
				_ => throw new Exception($"Come on!!!")
			};
			var triangleScreenPoint2 = MathEx.GetRotatedMyLineSegment(triangleScreenPoint1, calloutScreenCenter, Options.DangleInnerAngle).End;
			var triangleScreenPoint3 = MathEx.GetRotatedMyLineSegment(triangleScreenPoint1, calloutScreenCenter, -Options.DangleInnerAngle).End;
			trianglePoint1 = ScreenToCanvasPoint(triangleScreenPoint1, windowLeft, windowTop);
			trianglePoint2 = ScreenToCanvasPoint(triangleScreenPoint2, windowLeft, windowTop);
			trianglePoint3 = ScreenToCanvasPoint(triangleScreenPoint3, windowLeft, windowTop);
		}

		void PositionWindow()
		{
			targetCenter = target.PointToScreen(new Point(target.Width / 2, target.Height / 2));
			// TODO: Calculate the distance based on the angle and the aspect ratio or size of the rounded rect.


			const int almostInfiniteDistance = 222222;
			RotateCalloutToGetPosition(almostInfiniteDistance, CalloutSide.None, out double windowLeft, out double windowTop);

			Point infiniteCalloutStartPos = target.PointToScreen(new Point(target.Width / 2, target.Height / 2 - almostInfiniteDistance));
			Point infiniteCalloutCenterPoint = MathEx.RotatePoint(infiniteCalloutStartPos, targetCenter, Options.InitialAngle);

			MyLine testLine = new MyLine(targetCenter, infiniteCalloutCenterPoint);

			GuidelineIntersectionData guidelineIntersectionData = GetGuidelineIntersectionData(testLine, windowLeft, windowTop);
			SetTopLeft(guidelineIntersectionData);

			double distance = GetDistance(guidelineIntersectionData);

			RotateCalloutToGetPosition(distance, guidelineIntersectionData.Side, out windowLeft, out windowTop);
			calloutScreenCenter = new Point(windowLeft + Options.OuterMargin + calloutWidth / 2, windowTop + Options.OuterMargin + calloutHeight / 2);
			Left = windowLeft;
			Top = windowTop;

			GuidelineIntersectionData correctGuidelineIntersectionData = GetGuidelineIntersectionData(testLine, windowLeft, windowTop);

			GetTrianglePoints(correctGuidelineIntersectionData, windowLeft, windowTop);

			ShowIntersectedSide(guidelineIntersectionData.Side);
		}

		private void RotateCalloutToGetPosition(double distance, CalloutSide side, out double windowLeft, out double windowTop)
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
			return Math.Abs(adjacent / Math.Cos(angleRadians));
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

		void PlaceGuidelineDiagnostics()
		{
			Point calloutCenterPoint = new Point(calloutWidth / 2 + Options.OuterMargin, calloutHeight / 2 + Options.OuterMargin);
			Line angleGuideline = MathEx.GetRotatedLine(calloutCenterPoint, Options.InitialAngle + 180);
			cvsCallout.Children.Add(angleGuideline);

			Rectangle outerMarginRect = new Rectangle();
			outerMarginRect.Width = calloutWidth + 2 * Options.OuterMargin;
			outerMarginRect.Height = calloutHeight + 2 * Options.OuterMargin;
			outerMarginRect.Stroke = Brushes.Purple;
			cvsCallout.Children.Add(outerMarginRect);
		}

		void ShowTriangleDiagnostics()
		{
			System.Windows.Shapes.Path trianglePath = new System.Windows.Shapes.Path()
			{
				Stroke = Brushes.Black,
				StrokeThickness = 1,
				Fill = Brushes.Red
			};
			StreamGeometry triangleGeometry = new StreamGeometry();
			trianglePath.Data = triangleGeometry;
			using (StreamGeometryContext ctx = triangleGeometry.Open())
			{
				ctx.BeginFigure(trianglePoint1, true, true);
				ctx.LineTo(trianglePoint2, true, true);
				ctx.LineTo(trianglePoint3, true, true);
			}
			cvsCallout.Children.Add(trianglePath);
		}

		void LayoutEverything()
		{
			if (layoutValid)
				return;

			cvsCallout.Children.Clear();
			CalculateBounds();
			PositionWindow();
			CreateCallout();
			PlaceCloseButton();
			LayoutText();

			//PlaceGuidelineDiagnostics();
			//ShowTriangleDiagnostics();

			layoutValid = true;
		}

		void LoadStyles(MarkdownViewer markdownViewer)
		{
			ResourceDictionary myResourceDictionary = new ResourceDictionary();
			myResourceDictionary.Source = new Uri("pack://application:,,,/UltimateCallout;component/Styles/CalloutStyles.xaml", UriKind.Absolute);
			markdownViewer.Resources.MergedDictionaries.Add(myResourceDictionary);
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
		Point trianglePoint1;
		Point trianglePoint2;
		Point trianglePoint3;
		Point calloutScreenCenter;

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

		public static FrmUltimateCallout ShowCallout(string markDownText, FrameworkElement target, double angle, double aspectRatio, double height)
		{
			FrmUltimateCallout frmUltimateCallout = new FrmUltimateCallout();
			frmUltimateCallout.Options.InitialAngle = angle;
			frmUltimateCallout.Options.AspectRatio = aspectRatio;
			frmUltimateCallout.Options.Height = height;
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
