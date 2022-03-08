using System;
using System.Linq;

namespace UltimateCallout
{
	public class GuidelineIntersectionData
	{
		public CalloutSide CalloutDangleSide { get; set; }
		public CalloutSide TargetDangleSide { get; set; }
		public MyLine CalloutLeft { get; set; }
		public MyLine CalloutTop { get; set; }
		public MyLine CalloutRight { get; set; }
		public MyLine CalloutBottom { get; set; }
		public MyLine TargetLeft { get; set; }
		public MyLine TargetTop { get; set; }
		public MyLine TargetRight { get; set; }
		public MyLine TargetBottom { get; set; }
		public MyLine WindowLeft { get; set; }
		public MyLine WindowTop { get; set; }
		public MyLine WindowRight { get; set; }
		public MyLine WindowBottom { get; set; }

		public GuidelineIntersectionData()
		{

		}
	}
}
