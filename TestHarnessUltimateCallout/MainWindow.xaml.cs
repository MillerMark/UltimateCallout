using System;
using DevExpress.CodeRush.VisualizePlugins.Callouts.UI;
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
using System.Windows.Threading;
using System.Globalization;

namespace TestHarnessUltimateCallout
{
	
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		DispatcherTimer textChangedTimer;
		public MainWindow()
		{
			textChangedTimer = new DispatcherTimer(TimeSpan.FromSeconds(2), DispatcherPriority.Normal, textChanged, Dispatcher);
			InitializeComponent();
			centerPoint = new Point(Canvas.GetLeft(rctTarget) + rctTarget.Width / 2, Canvas.GetTop(rctTarget) + rctTarget.Height / 2);
		}

		FrmUltimateCallout frmUltimateCallout;
		Point centerPoint;
		Line angleGuideline;
		bool changingInternally;
		bool showDiagnostics;
		Color glowColor;

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
			
			UpdateTitle();
			ShowAngleGuidelineDiagnostic();
			
			if (frmUltimateCallout != null)
			{
				frmUltimateCallout.MoveCallout(tbxContent.Text, sldAngle.Value, sldWidth.Value);
				//frmUltimateCallout.Close();
			}
			else
			{
				frmUltimateCallout = FrmUltimateCallout.ShowCallout(tbxContent.Text, rctTarget, sldWidth.Value, sldAngle.Value, GetTheme(), GetFontSize());
				frmUltimateCallout.Closing += FrmUltimateCallout_Closing;
			}
		}

		private void FrmUltimateCallout_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			frmUltimateCallout = null;
		}

		private void sldAngle_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			CreateCallout();
		}

		private void ShowAngleGuidelineDiagnostic()
		{
			if (angleGuideline != null)
				cvsMain.Children.Remove(angleGuideline);

			if (showDiagnostics)
			{
				angleGuideline = MathEx.GetRotatedLine(centerPoint, sldAngle.Value);
				angleGuideline.Opacity = 0.25;
				cvsMain.Children.Add(angleGuideline);
			}
		}

		private void UpdateTitle()
		{
			if (sldWidth != null)
				Title = $"Angle: {sldAngle.Value}°, Width: {sldWidth.Value}";
		}

		private void SliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
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
				sldWidth.Value = next.Width;
			}
			finally
			{
				changingInternally = false;
			}
			CreateCallout();
		}

		private void ThemeRadioButton_Click(object sender, RoutedEventArgs e)
		{
			if (frmUltimateCallout == null)
				return;
			SetTheme();
		}

		void SetLightTheme()
		{
			SetForeground(Brushes.Black);
			SetBackground(Brushes.White);
		}

		private void SetBackground(Brush backgroundColor)
		{
			Background = backgroundColor;
			tbxContent.Background = backgroundColor;
		}

		void SetDarkTheme()
		{
			SetForeground(Brushes.White);
			SetBackground(Brushes.Black);
		}

		private void SetForeground(Brush foregroundColor)
		{
			Foreground = foregroundColor;
			tbxContent.Foreground = foregroundColor;
			ckDiagnostics.Foreground = foregroundColor;
			rbDark.Foreground = foregroundColor;
			rbLight.Foreground = foregroundColor;
			rbCyan.Foreground = foregroundColor;
			rbYellow.Foreground = foregroundColor;
			rbMagenta.Foreground = foregroundColor;
			rbPurple.Foreground = foregroundColor;
			rbRed.Foreground = foregroundColor;
			rbSkyBlue.Foreground = foregroundColor;
		}

		void SetTheme()
		{
			Theme theme = GetTheme();
			
			switch (theme)
			{
				case Theme.Light:
					SetLightTheme();
					glowColors.Visibility = Visibility.Collapsed;
					break;
				case Theme.Dark:
					SetDarkTheme();
					glowColors.Visibility = Visibility.Visible;
					break;
			}

			if (frmUltimateCallout != null)
				frmUltimateCallout.Theme = theme;
		}
		void SetDiagnostics()
		{
			if (frmUltimateCallout != null)
				frmUltimateCallout.ShowDiagnostics = showDiagnostics;
		}

		private Theme GetTheme()
		{
			if (rbLight.IsChecked == true)
				return Theme.Light;
			else if (rbDark.IsChecked == true)
				return Theme.Dark;
			return Theme.Custom;
		}

		private void ckDiagnostics_CheckedChanged(object sender, RoutedEventArgs e)
		{
			showDiagnostics = ckDiagnostics.IsChecked == true;
			if (frmUltimateCallout == null)
				return;
			SetDiagnostics();
			ShowAngleGuidelineDiagnostic();
		}

		private void tbxContent_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (changingInternally)
				return;
			textChangedTimer.Stop();
			textChangedTimer.Start();
		}

		void textChanged(object sender, EventArgs e)
		{
			textChangedTimer.Stop();
			CreateCallout();
		}

		private void GlowColorChanged(object sender, RoutedEventArgs e)
		{
			if (sender is RadioButton radioButton)
			{
				string colorCode = (string)radioButton.Tag;
				glowColor = (Color)ColorConverter.ConvertFromString(colorCode);
				if (frmUltimateCallout != null)
					frmUltimateCallout.GlowColor = glowColor;
			}
		}

		void UpdateTargetPosition()
		{
			if (sldTargetHeight == null || sldTargetWidth == null || rctTarget == null)
				return;
			rctTarget.Height = sldTargetHeight.Value;
			rctTarget.Width = sldTargetWidth.Value;
			Canvas.SetLeft(rctTarget, 340 + sldTargetX.Value);
			Canvas.SetTop(rctTarget, 120 + sldTargetY.Value);
			if (frmUltimateCallout != null)
				frmUltimateCallout.TargetMoved();
		}

		private void TargetSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			UpdateTargetPosition();
		}

		double GetFontSize()
		{
			return 12 * sldZoom.Value;
		}

		private void sldZoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (frmUltimateCallout == null)
				return;
			frmUltimateCallout.FontSize = GetFontSize();
			frmUltimateCallout.RefreshLayout();
		}
	}
}
