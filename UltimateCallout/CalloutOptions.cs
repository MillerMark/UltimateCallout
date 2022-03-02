using System;
using System.Linq;

namespace UltimateCallout
{
	public class CalloutOptions
	{
		public const double GoldenRatio = 1.61803398875d;
		public double CornerRadius { get; set; } = 6;
		public double OuterMargin { get; set; } = 25;
		public double Spacing { get; set; } = 5;
		public double InitialAngle { get; set; } = 45;
		public CalloutOptions()
		{

		}
	}
}
