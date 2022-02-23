using Markdig;
using Markdig.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
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

		void CreateRectangle()
		{
			roundedRectHeight = 250;
			roundedRectWidth = roundedRectHeight * CalloutOptions.GoldenRatio;
			roundedRectTop = 92;
			roundedRectLeft = 167;

			Rectangle rect = new Rectangle();
			rect.RadiusX = 6;
			rect.RadiusY = 6;
			rect.Width = roundedRectWidth;
			rect.Height = roundedRectHeight;
			rect.Fill = new SolidColorBrush(Colors.AliceBlue);
			rect.Stroke = new SolidColorBrush(Colors.Blue);
			cvsCallout.Children.Add(rect);
			Canvas.SetLeft(rect, roundedRectLeft);
			Canvas.SetTop(rect, roundedRectTop);
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

		void LayoutEverything()
		{
			if (layoutValid)
				return;

			cvsCallout.Children.Clear();
			CreateRectangle();
			PlaceCloseButton();
			LayoutText();
			AddTriangle();
			layoutValid = true;
		}

		Window? targetParentWindow;
		bool layoutValid;
		double roundedRectWidth;
		double roundedRectHeight;
		double roundedRectLeft;
		double roundedRectTop;
		string markDownText;

		public void PointTo(FrameworkElement target)
		{
			Point pointToScreen = target.PointToScreen(new Point(target.Width / 2, target.Height / 2));
			this.Left = pointToScreen.X;
			this.Top = pointToScreen.Y;

			targetParentWindow = Window.GetWindow(target);
			if (targetParentWindow != null)
				targetParentWindow.Closed += ParentWindow_Closed;

			LayoutEverything();
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
			if (targetParentWindow != null)
				targetParentWindow.Closed -= ParentWindow_Closed;
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				this.DragMove();
		}
	}
}
