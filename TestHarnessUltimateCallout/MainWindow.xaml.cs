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
		}

		FrmUltimateCallout frmUltimateCallout;
		private void btnShowCallout_Click(object sender, RoutedEventArgs e)
		{
			frmUltimateCallout = FrmUltimateCallout.ShowCallout("Hello **World**! This is the \n* first\n* second\n* third\n\nAnd another long line onrei neti sarneti arnei tnserai ontweio naweif neiarfo nteirtaf neit rnaeitso nraei tneira stnei.", rctTarget, sldAngle.Value);
		}

		private void sldAngle_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			this.Title = $"Angle: {sldAngle.Value.ToString()}°";		
		}
	}
}
