using System;
using System.Linq;
using System.Collections.Generic;

namespace TestHarnessUltimateCallout
{
	public static class Examples
	{
		static List<ExampleSetting> examples = new List<ExampleSetting>();
		static int index;

		public static ExampleSetting Next()
		{
			index++;
			if (index >= examples.Count)
				index = 0;
			return examples[index];
		}

		static void Add(string text, double aspectRatio, int width)
		{
			examples.Add(new ExampleSetting(text, width));
		}

		static Examples()
		{
			Add(@"
## Refactoring
It's pretty cool.

Press **Caps**+**Space** to toggle code. Here are some bullet points:

* first
* second
* third

And a long line at the end to show word wrapping because wrapping words is something that happens on a regular basis as you may already know because you have studied these kinds of things both in the lab and in the wild.", 0.56, 302);

			Add(@"This is the **StatInfo** class we created in the previous step (our **data model**).

Note we're passing the **UnitOfWork** (**uow**) to its constructor.", 1.6, 197);
			Add(@"This is the **Info** property we added.", 1.6, 171);
			Add(@"This is the **Date** property we added.", 1.6, 130);
			Add(@"First, we create a new **UnitOfWork**...", 2.4, 163);
			Add(@"Then we query...", 1.68, 156);
			Add(@"Revealing **data model** contents for each instance stored...", 1.6, 131);
			Add(@"And we send it all out to the console.", 1.6, 101);
			Add(@"A a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a.

Shows how word wrapping respects the close button in the upper right.", 0.894, 240);
		}
	}
}
