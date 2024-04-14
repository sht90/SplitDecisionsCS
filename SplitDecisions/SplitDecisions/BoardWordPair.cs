using System;
namespace SplitDecisions
{
	public class BoardWordPair
	{
		public Placement Placement;
		public WordPair WordPair;
		public int Intersections;

		public BoardWordPair(Placement placement, WordPair wordPair)
		{
			Placement = placement;
			WordPair = wordPair;
			Intersections = 0;
		}
	}
}

