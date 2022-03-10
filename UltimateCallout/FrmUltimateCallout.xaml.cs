using Markdig;
using Markdig.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Net;
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
		private const double indicatorMargin = 10d;
		SolidColorBrush closeButtonBackgroundBrush = new SolidColorBrush(Color.FromRgb(222, 245, 255));
		SolidColorBrush closeButtonForegroundBrush = new SolidColorBrush(Color.FromRgb(69, 133, 161));
		SolidColorBrush closeButtonBorderBrush = new SolidColorBrush(Color.FromRgb(171, 205, 219));
		SolidColorBrush calloutStrokeBrush = new SolidColorBrush(Color.FromRgb(72, 130, 156));
		SolidColorBrush calloutFillBrush = new SolidColorBrush(Color.FromRgb(245, 252, 255));

		Theme theme = Theme.Light;
		public Theme Theme { get => theme;
			set
			{
				if (theme == value)
					return;
				theme = value;
				LoadColorsForTheme();
			}
		}

		void LoadColorsForTheme()
		{
			// TODO: Consider adding a Theme class.
			if (Theme == Theme.Light)
			{
				closeButtonBackgroundBrush = new SolidColorBrush(Color.FromRgb(222, 245, 255));
				closeButtonForegroundBrush = new SolidColorBrush(Color.FromRgb(69, 133, 161));
				closeButtonBorderBrush = new SolidColorBrush(Color.FromRgb(171, 205, 219));
				calloutStrokeBrush = new SolidColorBrush(Color.FromRgb(72, 130, 156));
				calloutFillBrush = new SolidColorBrush(Color.FromRgb(245, 252, 255));
			}
			else if (Theme == Theme.Dark)
			{
				closeButtonBackgroundBrush = new SolidColorBrush(Color.FromRgb(48, 55, 59));
				closeButtonForegroundBrush = new SolidColorBrush(Color.FromRgb(41, 105, 133));
				closeButtonBorderBrush = new SolidColorBrush(Color.FromRgb(49, 93, 110));
				calloutStrokeBrush = new SolidColorBrush(Color.FromRgb(50, 94, 115));
				calloutFillBrush = new SolidColorBrush(Color.FromRgb(36, 38, 41));
			}
			else
			{
				// TODO: Add event to load custom resource, passing in the markdownViewer in the event args.
				return;
			}
			InvalidateLayout();
			LayoutEverything();
		}

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
			closeButton.Background = closeButtonBackgroundBrush;
			closeButton.Foreground = closeButtonForegroundBrush;
			closeButton.BorderBrush = closeButtonBorderBrush;
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
			calloutTop = OutsideMargin;
			calloutLeft = OutsideMargin;
			Width = calloutWidth + OutsideMargin * 2;
			Height = calloutHeight + OutsideMargin * 2;
		}

		void CreateCallout()
		{
			AddCalloutPathToBackOfCanvas(calloutStrokeBrush, 1, calloutFillBrush);
			if (Theme == Theme.Light)
			{
				AddCalloutPathToBackOfCanvas(null, 0, new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)), 5, 5);
			}
			else if (Theme == Theme.Dark)
			{
				for (int i = 1; i <= 10; i += 2)
				{
					AddCalloutPathToBackOfCanvas(new SolidColorBrush(Color.FromArgb(47, 238, 255, 0)), i, null);
				}
				
				// Add shadow...
			}
		}

		private void AddCalloutPathToBackOfCanvas(SolidColorBrush? calloutStrokeBrush, int thickness, SolidColorBrush? calloutFillBrush, double offsetX = 0, double offsetY = 0)
		{
			System.Windows.Shapes.Path calloutPath = new System.Windows.Shapes.Path()
			{
				Stroke = calloutStrokeBrush,
				StrokeThickness = thickness,
				Fill = calloutFillBrush
			};
			CreateCalloutGeometry(calloutPath);
			// Place the callout in the back:
			cvsCallout.Children.Insert(0, calloutPath);
			if (offsetX != 0)
				Canvas.SetLeft(calloutPath, offsetX);
			if (offsetY != 0)
				Canvas.SetTop(calloutPath, offsetY);
		}

		private void CreateCalloutGeometry(System.Windows.Shapes.Path calloutPath)
		{
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

			Figure closeButtonFigure = new()
			{
				Width = new FigureLength(closeButtonEdgeSize * 0.6, FigureUnitType.Pixel),
				Height = new FigureLength(closeButtonEdgeSize / 3, FigureUnitType.Pixel),
				HorizontalAnchor = FigureHorizontalAnchor.PageRight,
				HorizontalOffset = 0,
				VerticalOffset = 0,
				Margin = new Thickness(0),
				Padding = new Thickness(0)
			};

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
			double topWindowDistance = GetDistanceToIntersection(testLine, data.InnerWindowTop);
			double leftWindowDistance = GetDistanceToIntersection(testLine, data.InnerWindowLeft);
			double rightWindowDistance = GetDistanceToIntersection(testLine, data.InnerWindowRight);
			double bottomWindowDistance = GetDistanceToIntersection(testLine, data.InnerWindowBottom);

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
			double calloutLeft = windowLeft + OutsideMargin;
			double calloutTop = windowTop + OutsideMargin;
			double calloutRight = calloutLeft + calloutWidth;
			double calloutBottom = calloutTop + calloutHeight;

			double targetLeft = targetCenter.X - target.Width / 2;
			double targetTop = targetCenter.Y - target.Height / 2;
			double targetRight = targetLeft + target.Width;
			double targetBottom = targetTop + target.Height;

			double windowRight = windowLeft + calloutWidth + 2 * OutsideMargin;
			double windowBottom = windowTop + calloutHeight + 2 * OutsideMargin;

			GuidelineIntersectionData guidelineIntersectionData = new GuidelineIntersectionData();

			guidelineIntersectionData.CalloutTop = MyLine.Horizontal(calloutLeft, calloutRight, calloutTop);
			guidelineIntersectionData.CalloutLeft = MyLine.Vertical(calloutLeft, calloutTop, calloutBottom);
			guidelineIntersectionData.CalloutRight = MyLine.Vertical(calloutRight, calloutTop, calloutBottom);
			guidelineIntersectionData.CalloutBottom = MyLine.Horizontal(calloutLeft, calloutRight, calloutBottom);

			guidelineIntersectionData.TargetTop = MyLine.Horizontal(targetLeft, targetRight, targetTop - Options.TargetSpacing);
			guidelineIntersectionData.TargetLeft = MyLine.Vertical(targetLeft + Options.TargetSpacing, targetTop, targetBottom);
			guidelineIntersectionData.TargetRight = MyLine.Vertical(targetRight - Options.TargetSpacing, targetTop, targetBottom);
			guidelineIntersectionData.TargetBottom = MyLine.Horizontal(targetLeft, targetRight, targetBottom + Options.TargetSpacing);

			guidelineIntersectionData.InnerWindowTop = MyLine.Horizontal(windowLeft, windowRight, windowTop + indicatorMargin);
			guidelineIntersectionData.InnerWindowLeft = MyLine.Vertical(windowLeft + indicatorMargin, windowTop, windowBottom);
			guidelineIntersectionData.InnerWindowRight = MyLine.Vertical(windowRight - indicatorMargin, windowTop, windowBottom);
			guidelineIntersectionData.InnerWindowBottom = MyLine.Horizontal(windowLeft, windowRight, windowBottom - indicatorMargin);

			SetCalloutSides(testLine, guidelineIntersectionData);

			return guidelineIntersectionData;
		}

		object diagnosticTag = new object();

		void AddDiagnostic(FrameworkElement element)
		{
			element.Tag = diagnosticTag;
			cvsCallout.Children.Add(element);
		}

		bool IsDiagnostic(FrameworkElement element)
		{
			return (element.Tag == diagnosticTag);
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
					Canvas.SetTop(sideIndicator, OutsideMargin);
					if (side == CalloutSide.Right)
						Canvas.SetLeft(sideIndicator, calloutWidth + OutsideMargin - indicatorThickness);
					else
						Canvas.SetLeft(sideIndicator, OutsideMargin);
					break;
				case CalloutSide.Top:
				case CalloutSide.Bottom:
					sideIndicator.Width = calloutWidth;
					sideIndicator.Height = indicatorThickness;
					Canvas.SetLeft(sideIndicator, OutsideMargin);
					if (side == CalloutSide.Bottom)
						Canvas.SetTop(sideIndicator, calloutHeight + OutsideMargin - indicatorThickness);
					else
						Canvas.SetTop(sideIndicator, OutsideMargin);
					break;
			}
			sideIndicator.Fill = Brushes.Blue;
			sideIndicator.Opacity = 0.25;
			AddDiagnostic(sideIndicator);
		}

		Point ScreenToCanvasPoint(Point screenPoint, double windowLeft, double windowTop)
		{
			return new Point(screenPoint.X - windowLeft, screenPoint.Y - windowTop);
		}

		void GetTrianglePoints(GuidelineIntersectionData guidelineIntersectionData, double windowLeft, double windowTop)
		{
			MyLine guideline = MathEx.GetRotatedMyLine(targetCenter, lastCalloutAnglePosition);
			Point pt1 = guidelineIntersectionData.CalloutDangleSide switch
			{
				CalloutSide.Right => guideline.GetSegmentIntersection(guidelineIntersectionData.InnerWindowRight),
				CalloutSide.Left => guideline.GetSegmentIntersection(guidelineIntersectionData.InnerWindowLeft),
				CalloutSide.Bottom => guideline.GetSegmentIntersection(guidelineIntersectionData.InnerWindowBottom),
				CalloutSide.Top => guideline.GetSegmentIntersection(guidelineIntersectionData.InnerWindowTop),
				_ => throw new Exception($"Come on!!!")
			};
			Point pt2 = GetTriangleScreenPoint(guidelineIntersectionData, pt1, Options.DangleInnerAngle);
			Point pt3 = GetTriangleScreenPoint(guidelineIntersectionData, pt1, -Options.DangleInnerAngle);

			double deltaLeft = Left - windowLeft;
			double deltaTop = Top - windowTop;

			Point adjustedCenter = targetCenter;
			adjustedCenter.Offset(-deltaLeft, -deltaTop);

			if (MathEx.IsBetween(adjustedCenter, pt1, calloutScreenCenter))
			{
				// Callout is over the target - no dangle needed!
				trianglePoint1 = calloutScreenCenter;
				trianglePoint2 = calloutScreenCenter;
				trianglePoint3 = calloutScreenCenter;
			}
			else
			{
				trianglePoint1 = ScreenToCanvasPoint(pt1, windowLeft, windowTop);
				trianglePoint2 = ScreenToCanvasPoint(pt2, windowLeft, windowTop);
				trianglePoint3 = ScreenToCanvasPoint(pt3, windowLeft, windowTop);
			}
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
			Point topConnector = data.CalloutTop.MidPoint;
			Point leftConnector = data.CalloutLeft.MidPoint;
			Point bottomConnector = data.CalloutBottom.MidPoint;
			Point rightConnector = data.CalloutRight.MidPoint;

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

		double windowLeft;
		double windowTop;

		GuidelineIntersectionData GetGuidelineIntersectionData(bool positionWindow = false)
		{
			targetCenter = target.PointToScreen(new Point(target.Width / 2, target.Height / 2));
			// TODO: Calculate the distance based on the angle and the aspect ratio or size of the rounded rect.


			const int almostInfiniteDistance = 222222;
			RotateCalloutToGetPosition(almostInfiniteDistance, out windowLeft, out windowTop);

			Point infiniteCalloutStartPos = target.PointToScreen(new Point(target.Width / 2, target.Height / 2 - almostInfiniteDistance));
			Point infiniteCalloutCenterPoint = MathEx.RotatePoint(infiniteCalloutStartPos, targetCenter, lastCalloutAnglePosition);

			MyLine testLine = new MyLine(targetCenter, infiniteCalloutCenterPoint);

			GuidelineIntersectionData guidelineIntersectionData = GetGuidelineIntersectionData(testLine, windowLeft, windowTop);
			//double distance = GetDistance(guidelineIntersectionData);

			//RotateCalloutToGetPosition(distance, guidelineIntersectionData.CalloutDangleSide, out windowLeft, out windowTop);
			GetCalloutPosition(guidelineIntersectionData, out windowLeft, out windowTop);

			if (positionWindow)
			{
				Left = windowLeft;
				Top = windowTop;
			}

			calloutScreenCenter = new Point(windowLeft + OutsideMargin + calloutWidth / 2, windowTop + OutsideMargin + calloutHeight / 2);

			GuidelineIntersectionData correctGuidelineIntersectionData = GetGuidelineIntersectionData(testLine, windowLeft, windowTop);

			GetTrianglePoints(correctGuidelineIntersectionData, windowLeft, windowTop);

			return guidelineIntersectionData;
		}

		private void RotateCalloutToGetPosition(double distance, out double windowLeft, out double windowTop)
		{
			Point calloutStartPos = target.PointToScreen(new Point(target.Width / 2, target.Height / 2 - distance));
			Point calloutCenterPoint = MathEx.RotatePoint(calloutStartPos, targetCenter, lastCalloutAnglePosition);
			windowLeft = calloutCenterPoint.X - (OutsideMargin + calloutWidth / 2);
			windowTop = calloutCenterPoint.Y - (OutsideMargin + calloutHeight / 2);
		}

		void GetCalloutPosition(GuidelineIntersectionData data, out double windowLeft, out double windowTop)
		{
			Point danglePoint = data.CalloutDangleSide switch
			{
				CalloutSide.Left => GetCalloutDanglePointForHorizontalExit(),
				CalloutSide.Right => GetCalloutDanglePointForHorizontalExit(),
				CalloutSide.Top => GetCalloutDanglePointForVerticalExit(),
				CalloutSide.Bottom => GetCalloutDanglePointForVerticalExit(),
				_ => throw new NotImplementedException()
			};

			//danglePoint = GetDanglePointAtCorrectLength(danglePoint, data);

			Point screenDanglePoint = data.TargetDangleSide switch
			{
				CalloutSide.Left => GetScreenDanglePointForHorizontalExit(),
				CalloutSide.Right => GetScreenDanglePointForHorizontalExit(),
				CalloutSide.Top => GetScreenDanglePointForVerticalExit(),
				CalloutSide.Bottom => GetScreenDanglePointForVerticalExit(),
				_ => throw new NotImplementedException()
			};

			windowLeft = screenDanglePoint.X - danglePoint.X;
			windowTop = screenDanglePoint.Y - danglePoint.Y;
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

		Point GetCalloutDanglePointForHorizontalExit()
		{
			// ![](164BA7B27FE650FD419F6223A6677E33.png)

			double adjacentC = calloutWidth / 2 + Options.OuterMargin;
			double theta = GetTheta();
			double oppositeD = Math.Abs(adjacentC * Math.Tan(theta));

			return GetCalloutPoint(adjacentC, oppositeD);
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
				throw new Exception($"Rory was wrong??? Noooooooo!!!");
				//System.Diagnostics.Debugger.Break();
				//adjacentC = calloutWidth / 2 + OutsideMargin;
			}

			return GetCalloutPoint(adjacentC, oppositeD);
		}

		public double OutsideMargin
		{
			get => Options.OuterMargin + indicatorMargin;
		}

		private Point GetCalloutPoint(double adjacentC, double oppositeD)
		{
			// ![](EF98A8132B6F583B59EB48677325D6BE.png)
			double calloutX = OutsideMargin + calloutWidth / 2 + GetXSign() * adjacentC;
			double calloutY = OutsideMargin + calloutHeight / 2 + GetYSign() * oppositeD;
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
				throw new Exception($"Rory was wrong??? Noooooooo!!!");
				//System.Diagnostics.Debugger.Break();
				//adjacentA = target.Width / 2 + Options.TargetSpacing;
			}

			return GetTargetPoint(adjacentA, oppositeB);
		}

		private double GetTheta()
		{
			return GetAngleDegrees() * Math.PI / 180;
		}

		private double GetAngleDegrees()
		{
			double angleDegrees = 90 - lastCalloutAnglePosition;
			while (angleDegrees < 0)
				angleDegrees += 360;
			return angleDegrees % 360;
		}

		void PlaceGuidelineDiagnostics()
		{
			Point calloutCenterPoint = new Point(calloutWidth / 2 + OutsideMargin, calloutHeight / 2 + OutsideMargin);
			Line angleGuideline = MathEx.GetRotatedLine(calloutCenterPoint, lastCalloutAnglePosition + 180);
			AddDiagnostic(angleGuideline);

			Rectangle outerMarginRect = new Rectangle();
			outerMarginRect.Width = calloutWidth + 2 * OutsideMargin;
			outerMarginRect.Height = calloutHeight + 2 * OutsideMargin;
			outerMarginRect.Stroke = Brushes.Purple;
			AddDiagnostic(outerMarginRect);
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
			AddDiagnostic(trianglePath);
		}

		void LayoutEverything()
		{
			if (layoutValid)
				return;

			if (target == null)
				return;

			cvsCallout.Children.Clear();
			CalculateBounds();
			GuidelineIntersectionData guidelineIntersectionData = GetGuidelineIntersectionData(true);
			CreateCallout();
			PlaceCloseButton();
			LayoutText();

			ShowDiagnostics(guidelineIntersectionData);

			layoutValid = true;
		}

		void RemoveDiagnostics()
		{
			for (int i = cvsCallout.Children.Count - 1; i >= 0; i--)
				if (cvsCallout.Children[i] is FrameworkElement frameworkElement)
					if (frameworkElement.Tag == diagnosticTag)
						cvsCallout.Children.RemoveAt(i);
		}

		private void ShowDiagnostics(GuidelineIntersectionData guidelineIntersectionData)
		{
			RemoveDiagnostics();
			ShowIntersectedSide(guidelineIntersectionData.CalloutDangleSide);
			PlaceGuidelineDiagnostics();
			ShowTriangleDiagnostics();
		}

		void LoadStyles(MarkdownViewer markdownViewer)
		{
			ResourceDictionary myResourceDictionary = new ResourceDictionary();
			string styleName;
			if (Theme == Theme.Light)
				styleName = "LightCalloutStyles";
			else if (Theme == Theme.Dark)
				styleName = "DarkCalloutStyles";
			else
			{
				// TODO: Add event to load custom resource, passing in the markdownViewer in the event args.
				return;
			}

			myResourceDictionary.Source = new Uri($"pack://application:,,,/UltimateCallout;component/Styles/{styleName}.xaml", UriKind.Absolute);
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
		double lastCalloutAnglePosition;

		public void PointTo(FrameworkElement target)
		{
			this.target = target;
			
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

		public static FrmUltimateCallout ShowCallout(string markDownText, FrameworkElement target, double angle, double aspectRatio, double height, Theme theme)
		{
			FrmUltimateCallout frmUltimateCallout = new FrmUltimateCallout();
			frmUltimateCallout.Options.InitialAngle = angle;
			frmUltimateCallout.Theme = theme;
			frmUltimateCallout.lastCalloutAnglePosition = angle;
			frmUltimateCallout.Options.AspectRatio = aspectRatio;
			frmUltimateCallout.Options.Height = height;
			frmUltimateCallout.markDownText = markDownText;
			frmUltimateCallout.PointTo(target);
			frmUltimateCallout.Show();

			return frmUltimateCallout;
		}

		public void MoveCallout(string markDownText, double angle, double aspectRatio, double height)
		{
			InvalidateLayout();
			lastCalloutAnglePosition = angle;
			Options.InitialAngle = angle;
			Options.AspectRatio = aspectRatio;
			Options.Height = height;
			if (this.markDownText != markDownText)
				this.markDownText = markDownText;
			LayoutEverything();
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

		private void Window_LocationChanged(object sender, EventArgs e)
		{
			if (cvsCallout == null || cvsCallout.Children.Count == 0)
				return;
			double calloutCenterScreenX = Left + OutsideMargin + calloutWidth / 2;
			double calloutCenterScreenY = Top + OutsideMargin + calloutHeight / 2;
			Point calloutCenter = new Point(calloutCenterScreenX, calloutCenterScreenY);
			double angleDegrees = MathEx.GetAngleDegrees(targetCenter, calloutCenter) + 90;
			while (angleDegrees < 0)
				angleDegrees += 360;
			angleDegrees %= 360;
			if (angleDegrees != lastCalloutAnglePosition)
			{
				for (int i = cvsCallout.Children.Count - 1; i >= 0; i--)
					if (cvsCallout.Children[i] is System.Windows.Shapes.Path)
						cvsCallout.Children.RemoveAt(i);

				lastCalloutAnglePosition = angleDegrees;
				GuidelineIntersectionData guidelineIntersectionData = GetGuidelineIntersectionData();
				CreateCallout();
				ShowDiagnostics(guidelineIntersectionData);
			}
		}

		Point GetDanglePointAtCorrectLength(Point danglePoint, GuidelineIntersectionData data)
		{
			Point calloutCenter = new Point(OutsideMargin + calloutWidth / 2, OutsideMargin + calloutHeight / 2);

			MyLine fullDangleLine = new MyLine(calloutCenter, danglePoint);
			double left = OutsideMargin;
			double top = OutsideMargin;
			double right = OutsideMargin + calloutWidth;
			double bottom = OutsideMargin + calloutHeight;
			MyLine calloutLeft = MyLine.Vertical(left, top, bottom);
			MyLine calloutTop = MyLine.Horizontal(left, right, top);
			MyLine calloutRight = MyLine.Vertical(right, top, bottom);
			MyLine calloutBottom = MyLine.Horizontal(left, right, bottom);
			Point dangleLineIntersect = fullDangleLine.GetClosestIntersect(danglePoint, calloutLeft, calloutTop, calloutRight, calloutBottom);
			MyLine dangleLine = new MyLine(calloutCenter, dangleLineIntersect);
			dangleLine.Extend(Options.OuterMargin);
			return dangleLine.End;
		}
	}
}
