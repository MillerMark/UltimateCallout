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

		static void Add(string text, double aspectRatio, int height)
		{
			examples.Add(new ExampleSetting(text, aspectRatio, height));
		}

		static Examples()
		{
			Add(@"
## Refactoring
It's pretty cool.

![](FileDiff.png) | ![](Refactoring.png)
-- | --
File Diff | Refactoring

```code sample```\
\
Press **Caps**+**Space** to toggle code. Here are some bullet points:

* first
* second
* third

And a long line at the end to show word wrapping because wrapping words is something that happens on a regular basis as you may already know because you have studied these kinds of things both in the lab and in the wild.", 0.56, 567);

			Add(@"This is the **StatInfo** class we created in the previous step (our **data model**).

Note we're passing the **UnitOfWork** (**uow**) in to its constructor.", 1.6, 142);
			Add(@"This is the **Info** property we added.", 1.6, 83);
			Add(@"This is the **Date** property we added.", 1.6, 83);
			Add(@"First, we create a new **UnitOfWork**...", 2.4, 68);
			Add(@"Then we query...", 1.68, 65);
			Add(@"Revealing **data model** contents for each instance stored...", 1.6, 98);
			Add(@"And we send it all out to the console.", 1.6, 83);
			Add(@"A a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a.", 0.894, 196);
		}
	}
}
