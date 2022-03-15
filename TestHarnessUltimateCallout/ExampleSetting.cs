using System;
using System.Linq;

namespace TestHarnessUltimateCallout
{
	public class ExampleSetting
	{
		public string Text { get; set; }
		public double Width { get; set; }
		public ExampleSetting(string text, double width)
		{
			Text = text;
			Width = width;
		}
	}
}
