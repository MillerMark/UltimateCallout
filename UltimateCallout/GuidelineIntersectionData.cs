using System;
using System.Linq;

namespace UltimateCallout
{
	public class GuidelineIntersectionData
	{
		public CalloutSide Side { get; set; }
		public MyLine Left { get; set; }
		public MyLine Top { get; set; }
		public MyLine Right { get; set; }
		public MyLine Bottom { get; set; }

		public GuidelineIntersectionData()
		{

		}
	}
}
