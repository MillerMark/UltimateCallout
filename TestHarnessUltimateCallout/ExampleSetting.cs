using System;
using System.Linq;

namespace TestHarnessUltimateCallout
{
	public class ExampleSetting
	{
		public string Text { get; set; }
		public double AspectRatio { get; set; }
		public double Height { get; set; }
		public ExampleSetting(string text, double aspectRatio, double height)
		{
			Text = text;
			AspectRatio = aspectRatio;
			Height = height;
		}
	}
}
