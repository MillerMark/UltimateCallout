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

		private void btnShowCallout_Click(object sender, RoutedEventArgs e)
		{
			CreateCallout();
		}

		private void CreateCallout()
		{
			if (frmUltimateCallout != null)
				frmUltimateCallout.Close();
			UpdateTitle();
			//UpdateAngleGuideline();
			frmUltimateCallout = FrmUltimateCallout.ShowCallout("Hello **World**! This is the \n* first\n* second\n* third\n\nAnd another long line onrei neti sarneti arnei tnserai ontweio naweif neiarfo nteirtaf neit rnaeitso nraei tneira stnei.", rctTarget, sldAngle.Value, sldAspectRatio.Value);
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
			Title = $"Angle: {sldAngle.Value}°, Aspect Ratio: {sldAspectRatio.Value}";
		}

		private void sldAspectRatio_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			CreateCallout();
		}
	}
}
