using System;
using System.Linq;

namespace UltimateCallout
{
	public class CalloutOptions
	{
		public static double GoldenRatio = 1.61803398875d;
		public double AspectRatio { get; set; } = GoldenRatio;
		public double CornerRadius { get; set; } = 6;
		public double OuterMargin { get; set; } = 30;
		public double TargetSpacing { get; set; } = 5;
		public double InitialAngle { get; set; } = 45;
		public double DangleInnerAngle { get; set; } = 19;
		public double Height { get; set; } = 200;

		public CalloutOptions()
		{

		}
	}
}
