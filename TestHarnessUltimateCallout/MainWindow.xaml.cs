using System;
using UltimateCallout;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TestHarnessUltimateCallout
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			centerPoint = new Point(Canvas.GetLeft(rctTarget) + rctTarget.Width / 2, Canvas.GetTop(rctTarget) + rctTarget.Height / 2);
		}

		FrmUltimateCallout frmUltimateCallout;
		Point centerPoint;
		Line angleGuideline;
		bool changingInternally;

		private void btnShowCallout_Click(object sender, RoutedEventArgs e)
		{
			CreateCallout();
		}

		private void CreateCallout()
		{
			if (changingInternally)
				return;
			if (tbxContent == null)
				return;
			if (frmUltimateCallout != null)
				frmUltimateCallout.Close();
			UpdateTitle();
			UpdateAngleGuideline();
			frmUltimateCallout = FrmUltimateCallout.ShowCallout(tbxContent.Text, rctTarget, sldAngle.Value, sldAspectRatio.Value, sldHeight.Value);
		}

		private void sldAngle_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			CreateCallout();
		}

		private void UpdateAngleGuideline()
		{
			if (angleGuideline != null)
				cvsMain.Children.Remove(angleGuideline);

			angleGuideline = MathEx.GetRotatedLine(centerPoint, sldAngle.Value);
			cvsMain.Children.Add(angleGuideline);
		}

		private void UpdateTitle()
		{
			if (sldAspectRatio != null)
				Title = $"Angle: {sldAngle.Value}°, Aspect Ratio: {sldAspectRatio.Value}, Height: {sldHeight.Value}";
		}

		private void sldAspectRatio_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			CreateCallout();
		}

		private void sldHeight_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			CreateCallout();
		}

		private void btnNextSample_Click(object sender, RoutedEventArgs e)
		{
			ExampleSetting next = Examples.Next();
			changingInternally = true;
			try
			{
				tbxContent.Text = next.Text;
				sldAspectRatio.Value = next.AspectRatio;
				sldHeight.Value = next.Height;
			}
			finally
			{
				changingInternally = false;
			}
			CreateCallout();
		}
	}
}
