using System;
using System.Linq;

namespace UltimateCallout
{
	public class GuidelineIntersectionData
	{
		public CalloutSide Side { get; set; }
		public MyLine CalloutLeft { get; set; }
		public MyLine CalloutTop { get; set; }
		public MyLine CalloutRight { get; set; }
		public MyLine CalloutBottom { get; set; }
		public MyLine WindowLeft { get; set; }
		public MyLine WindowTop { get; set; }
		public MyLine WindowRight { get; set; }
		public MyLine WindowBottom { get; set; }

		public GuidelineIntersectionData()
		{

		}
	}
}
