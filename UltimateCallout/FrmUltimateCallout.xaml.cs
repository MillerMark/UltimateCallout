using Markdig;
using Markdig.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
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
using System.Windows.Markup;
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

		void SetCalloutSides(MyLine testLine, GuidelineIntersectionData data)
		{
			// TODO: Opportunities to refactor here, but it's tricky so be careful.
			double topWindowDistance = GetDistanceToIntersection(testLine, data.WindowTop);
			double leftWindowDistance = GetDistanceToIntersection(testLine, data.WindowLeft);
			double rightWindowDistance = GetDistanceToIntersection(testLine, data.WindowRight);
			double bottomWindowDistance = GetDistanceToIntersection(testLine, data.WindowBottom);

			double minCalloutDistance = Min(topWindowDistance, leftWindowDistance, rightWindowDistance, bottomWindowDistance);

			if (minCalloutDistance == topWindowDistance)
				data.CalloutDangleSide = CalloutSide.Top;
			else if (minCalloutDistance == rightWindowDistance)
				data.CalloutDangleSide = CalloutSide.Right;
			else if (minCalloutDistance == bottomWindowDistance)
				data.CalloutDangleSide = CalloutSide.Bottom;
			else if (minCalloutDistance == leftWindowDistance)
				data.CalloutDangleSide = CalloutSide.Left;

			double topTargetDistance = GetDistanceToIntersection(testLine, data.TargetTop);
			double leftTargetDistance = GetDistanceToIntersection(testLine, data.TargetLeft);
			double rightTargetDistance = GetDistanceToIntersection(testLine, data.TargetRight);
			double bottomTargetDistance = GetDistanceToIntersection(testLine, data.TargetBottom);

			double minTargetDistance = Min(topTargetDistance, leftTargetDistance, rightTargetDistance, bottomTargetDistance);

			if (minTargetDistance == topTargetDistance)
				data.TargetDangleSide = CalloutSide.Top;
			else if (minTargetDistance == rightTargetDistance)
				data.TargetDangleSide = CalloutSide.Right;
			else if (minTargetDistance == bottomTargetDistance)
				data.TargetDangleSide = CalloutSide.Bottom;
			else if (minTargetDistance == leftTargetDistance)
				data.TargetDangleSide = CalloutSide.Left;

		}

		private static double Min(params double[] args) => args.Min();

		GuidelineIntersectionData GetGuidelineIntersectionData(MyLine testLine, double windowLeft, double windowTop)
		{
			double calloutLeft = windowLeft + Options.OuterMargin;
			double calloutTop = windowTop + Options.OuterMargin;
			double calloutRight = calloutLeft + calloutWidth;
			double calloutBottom = calloutTop + calloutHeight;

			double targetLeft = targetCenter.X - target.Width / 2;
			double targetTop = targetCenter.Y - target.Height / 2;
			double targetRight = targetLeft + target.Width;
			double targetBottom = targetTop + target.Height;

			double windowRight = windowLeft + calloutWidth + 2 * Options.OuterMargin;
			double windowBottom = windowTop + calloutHeight + 2 * Options.OuterMargin;

			GuidelineIntersectionData guidelineIntersectionData = new GuidelineIntersectionData();

			guidelineIntersectionData.CalloutTop = MyLine.Horizontal(calloutLeft, calloutRight, calloutTop);
			guidelineIntersectionData.CalloutLeft = MyLine.Vertical(calloutLeft, calloutTop, calloutBottom);
			guidelineIntersectionData.CalloutRight = MyLine.Vertical(calloutRight, calloutTop, calloutBottom);
			guidelineIntersectionData.CalloutBottom = MyLine.Horizontal(calloutLeft, calloutRight, calloutBottom);

			guidelineIntersectionData.TargetTop = MyLine.Horizontal(targetLeft, targetRight, targetTop - Options.TargetSpacing);
			guidelineIntersectionData.TargetLeft = MyLine.Vertical(targetLeft + Options.TargetSpacing, targetTop, targetBottom);
			guidelineIntersectionData.TargetRight = MyLine.Vertical(targetRight - Options.TargetSpacing, targetTop, targetBottom);
			guidelineIntersectionData.TargetBottom = MyLine.Horizontal(targetLeft, targetRight, targetBottom + Options.TargetSpacing);

			guidelineIntersectionData.WindowTop = MyLine.Horizontal(windowLeft, windowRight, windowTop);
			guidelineIntersectionData.WindowLeft = MyLine.Vertical(windowLeft, windowTop, windowBottom);
			guidelineIntersectionData.WindowRight = MyLine.Vertical(windowRight, windowTop, windowBottom);
			guidelineIntersectionData.WindowBottom = MyLine.Horizontal(windowLeft, windowRight, windowBottom);

			SetCalloutSides(testLine, guidelineIntersectionData);

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
			Point pt1 = guidelineIntersectionData.CalloutDangleSide switch
			{
				CalloutSide.Right => guideline.GetSegmentIntersection(guidelineIntersectionData.WindowRight),
				CalloutSide.Left => guideline.GetSegmentIntersection(guidelineIntersectionData.WindowLeft),
				CalloutSide.Bottom => guideline.GetSegmentIntersection(guidelineIntersectionData.WindowBottom),
				CalloutSide.Top => guideline.GetSegmentIntersection(guidelineIntersectionData.WindowTop),
				_ => throw new Exception($"Come on!!!")
			};
			Point pt2 = GetTriangleScreenPoint(guidelineIntersectionData, pt1, Options.DangleInnerAngle);
			Point pt3 = GetTriangleScreenPoint(guidelineIntersectionData, pt1, -Options.DangleInnerAngle);

			trianglePoint1 = ScreenToCanvasPoint(pt1, windowLeft, windowTop);
			trianglePoint2 = ScreenToCanvasPoint(pt2, windowLeft, windowTop);
			trianglePoint3 = ScreenToCanvasPoint(pt3, windowLeft, windowTop);
		}

		private Point GetTriangleScreenPoint(GuidelineIntersectionData guidelineIntersectionData, Point triangleScreenPoint1, double angle)
		{
			Point rotatedScreenPt = MathEx.GetRotatedMyLineSegment(triangleScreenPoint1, calloutScreenCenter, angle).End;
			MyLine line = new MyLine(triangleScreenPoint1, rotatedScreenPt);

			Point intersectionPoint = guidelineIntersectionData.CalloutDangleSide switch
			{
				CalloutSide.Right => line.GetSegmentIntersection(guidelineIntersectionData.CalloutRight),
				CalloutSide.Left => line.GetSegmentIntersection(guidelineIntersectionData.CalloutLeft),
				CalloutSide.Bottom => line.GetSegmentIntersection(guidelineIntersectionData.CalloutBottom),
				CalloutSide.Top => line.GetSegmentIntersection(guidelineIntersectionData.CalloutTop),
				_ => throw new NotImplementedException(),
			};

			if (double.IsNaN(intersectionPoint.X))
			{
				intersectionPoint = GetClosestConnectionPoint(rotatedScreenPt, guidelineIntersectionData);
			}

			rotatedScreenPt = intersectionPoint;

			return rotatedScreenPt;
		}

		Point GetClosestConnectionPoint(Point rotatedScreenPt, GuidelineIntersectionData data)
		{
			Point topConnector = new Point((data.CalloutTop.Start.X + data.CalloutTop.End.X) / 2, (data.CalloutTop.Start.Y + data.CalloutTop.End.Y) / 2);
			Point leftConnector = new Point((data.CalloutLeft.Start.X + data.CalloutLeft.End.X) / 2, (data.CalloutLeft.Start.Y + data.CalloutLeft.End.Y) / 2);
			Point bottomConnector = new Point((data.CalloutBottom.Start.X + data.CalloutBottom.End.X) / 2, (data.CalloutBottom.Start.Y + data.CalloutBottom.End.Y) / 2);
			Point rightConnector = new Point((data.CalloutRight.Start.X + data.CalloutRight.End.X) / 2, (data.CalloutRight.Start.Y + data.CalloutRight.End.Y) / 2);

			double topLength = (rotatedScreenPt - topConnector).Length;
			double leftLength = (rotatedScreenPt - leftConnector).Length;
			double bottomLength = (rotatedScreenPt - bottomConnector).Length;
			double rightLength = (rotatedScreenPt - rightConnector).Length;

			if (topLength < leftLength)
				if (topLength < bottomLength)
					if (topLength < rightLength)
						return topConnector;
					else
						return rightConnector;
				else if (bottomLength < rightLength)
					return bottomConnector;
				else
					return rightConnector;
			else if (leftLength < bottomLength)
				if (leftLength < rightLength)
					return leftConnector;
				else
					return rightConnector;
			else if (bottomLength < rightLength)
				return bottomConnector;
			else
				return rightConnector;
		}

		GuidelineIntersectionData PositionWindow()
		{
			targetCenter = target.PointToScreen(new Point(target.Width / 2, target.Height / 2));
			// TODO: Calculate the distance based on the angle and the aspect ratio or size of the rounded rect.


			const int almostInfiniteDistance = 222222;
			RotateCalloutToGetPosition(almostInfiniteDistance, out double windowLeft, out double windowTop);

			Point infiniteCalloutStartPos = target.PointToScreen(new Point(target.Width / 2, target.Height / 2 - almostInfiniteDistance));
			Point infiniteCalloutCenterPoint = MathEx.RotatePoint(infiniteCalloutStartPos, targetCenter, Options.InitialAngle);

			MyLine testLine = new MyLine(targetCenter, infiniteCalloutCenterPoint);

			GuidelineIntersectionData guidelineIntersectionData = GetGuidelineIntersectionData(testLine, windowLeft, windowTop);
			//double distance = GetDistance(guidelineIntersectionData);

			//RotateCalloutToGetPosition(distance, guidelineIntersectionData.CalloutDangleSide, out windowLeft, out windowTop);
			GetCalloutPosition(guidelineIntersectionData, out windowLeft, out windowTop);

			calloutScreenCenter = new Point(windowLeft + Options.OuterMargin + calloutWidth / 2, windowTop + Options.OuterMargin + calloutHeight / 2);
			Left = windowLeft;
			Top = windowTop;

			GuidelineIntersectionData correctGuidelineIntersectionData = GetGuidelineIntersectionData(testLine, windowLeft, windowTop);

			GetTrianglePoints(correctGuidelineIntersectionData, windowLeft, windowTop);

			return guidelineIntersectionData;
		}

		private void RotateCalloutToGetPosition(double distance, out double windowLeft, out double windowTop)
		{
			Point calloutStartPos = target.PointToScreen(new Point(target.Width / 2, target.Height / 2 - distance));
			Point calloutCenterPoint = MathEx.RotatePoint(calloutStartPos, targetCenter, Options.InitialAngle);
			windowLeft = calloutCenterPoint.X - (Options.OuterMargin + calloutWidth / 2);
			windowTop = calloutCenterPoint.Y - (Options.OuterMargin + calloutHeight / 2);
		}

		void GetCalloutPosition(GuidelineIntersectionData data, out double windowLeft, out double windowTop)
		{
			Point calloutDanglePoint = data.CalloutDangleSide switch
			{
				CalloutSide.Left => GetCalloutDanglePointForHorizontalExit(),
				CalloutSide.Right => GetCalloutDanglePointForHorizontalExit(),
				CalloutSide.Top => GetCalloutDanglePointForVerticalExit(),
				CalloutSide.Bottom => GetCalloutDanglePointForVerticalExit()
			};
			Point screenDanglePoint = data.TargetDangleSide switch
			{
				CalloutSide.Left => GetScreenDanglePointForHorizontalExit(),
				CalloutSide.Right => GetScreenDanglePointForHorizontalExit(),
				CalloutSide.Top => GetScreenDanglePointForVerticalExit(),
				CalloutSide.Bottom => GetScreenDanglePointForVerticalExit()
			};

			windowLeft = screenDanglePoint.X - calloutDanglePoint.X;
			windowTop = screenDanglePoint.Y - calloutDanglePoint.Y;
		}

		double GetXSign()
		{
			// ![](5D631E255DF1F17130A1FB5820FE16E3.png)
			double angleDegrees = GetAngleDegrees();
			if (angleDegrees > 90 && angleDegrees <= 270)
				return 1;

			return -1;
		}

		double GetYSign()
		{
			// ![](7EB85C87527FE5FBB12762A9DD59A1B1.png)
			double angleDegrees = GetAngleDegrees();
			if (angleDegrees > 0 && angleDegrees <= 180)
				return 1;

			return -1;
		}

		Point GetCalloutDanglePointForVerticalExit()
		{
			// ![](9536BE665614588B86AA0DAF4F971BBB.png)
			double oppositeD = calloutHeight / 2 + Options.OuterMargin;
			double theta = GetTheta();
			double tanTheta = Math.Tan(theta);
			double adjacentC;
			if (tanTheta != 0)
				adjacentC = Math.Abs(oppositeD / tanTheta);
			else
			{
				System.Diagnostics.Debugger.Break();
				adjacentC = calloutWidth / 2 + Options.OuterMargin;
			}

			return GetCalloutPoint(adjacentC, oppositeD);
		}

		private Point GetCalloutPoint(double adjacentC, double oppositeD)
		{
			// ![](EF98A8132B6F583B59EB48677325D6BE.png)
			double calloutX = Options.OuterMargin + calloutWidth / 2 + GetXSign() * adjacentC;
			double calloutY = Options.OuterMargin + calloutHeight / 2 + GetYSign() * oppositeD;
			return new Point(calloutX, calloutY);
		}

		private Point GetTargetPoint(double adjacentA, double oppositeB)
		{
			// ![](626312DF98111F2CC2588E8F547D3DAE.png)

			double screenX = targetCenter.X - GetXSign() * adjacentA;
			double screenY = targetCenter.Y - GetYSign() * oppositeB;
			return new Point(screenX, screenY);
		}

		Point GetScreenDanglePointForHorizontalExit()
		{
			// ![](473394D46C1D2A4F0FA89BEEE7DA7405.png)
			double adjacentA = target.Width / 2 + Options.TargetSpacing;
			double theta = GetTheta();
			double oppositeB = Math.Abs(adjacentA * Math.Tan(theta));

			return GetTargetPoint(adjacentA, oppositeB);
		}

		Point GetScreenDanglePointForVerticalExit()
		{
			// ![](1DDD9F289F77FC56734B77A13828B6B0.png)
			double oppositeB = target.Height / 2 + Options.TargetSpacing;
			double theta = GetTheta();
			double tanTheta = Math.Tan(theta);
			double adjacentA;
			if (tanTheta != 0)
				adjacentA = Math.Abs(oppositeB / tanTheta);
			else
			{
				System.Diagnostics.Debugger.Break();
				adjacentA = target.Width / 2 + Options.TargetSpacing;
			}

			return GetTargetPoint(adjacentA, oppositeB);
		}

		Point GetCalloutDanglePointForHorizontalExit()
		{
			// ![](164BA7B27FE650FD419F6223A6677E33.png)

			double adjacentC = calloutWidth / 2 + Options.OuterMargin;
			double theta = GetTheta();
			double oppositeD = Math.Abs(adjacentC * Math.Tan(theta));

			return GetCalloutPoint(adjacentC, oppositeD);
		}

		private double GetTheta()
		{
			return GetAngleDegrees() * Math.PI / 180;
		}

		private double GetAngleDegrees()
		{
			double angleDegrees = 90 - Options.InitialAngle;
			while (angleDegrees < 0)
				angleDegrees += 360;
			return angleDegrees % 360;
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
				Stroke = new SolidColorBrush(Color.FromArgb(177, 140, 0, 0)),
				StrokeThickness = 1,
				Fill = new SolidColorBrush(Color.FromArgb(44, 255, 0, 0))
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
			GuidelineIntersectionData guidelineIntersectionData = PositionWindow();
			CreateCallout();
			PlaceCloseButton();
			LayoutText();

			ShowDiagnostics(guidelineIntersectionData);

			layoutValid = true;
		}

		private void ShowDiagnostics(GuidelineIntersectionData guidelineIntersectionData)
		{
			//ShowIntersectedSide(guidelineIntersectionData.CalloutDangleSide);
			//PlaceGuidelineDiagnostics();
			//ShowTriangleDiagnostics();
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

		public void MoveCallout(string markDownText, FrameworkElement target, double angle, double aspectRatio, double height)
		{
			InvalidateLayout();
			Options.InitialAngle = angle;
			Options.AspectRatio = aspectRatio;
			Options.Height = height;
			if (this.markDownText != markDownText)
				this.markDownText = markDownText;
			PointTo(target);
			//Show();
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
