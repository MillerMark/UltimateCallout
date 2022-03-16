using System;
using System.Linq;

namespace UltimateCallout
{
	public class CalloutOptions
	{
		public bool AnimateBackAfterDrag { get; set; } = true;
		public double CornerRadius { get; set; } = 6;
		public double OuterMargin { get; set; } = 30;
		public double TargetSpacing { get; set; } = 5;
		public double InitialAngle { get; set; } = 45;

		/// <summary>
		/// The angle of the dangle, in degrees.
		/// </summary>
		public double DangleAngle { get; set; } = 38;

		public double Width { get; set; } = 200;
		public double AnimationTimeMs { get; set; } = 450;
		public bool AnimateAppearance { get; set; } = true;

		public CalloutOptions()
		{

		}
	}
}
