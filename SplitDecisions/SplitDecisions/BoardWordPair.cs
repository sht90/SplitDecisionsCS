using System;
namespace SplitDecisions
{
	public class BoardWordPair
	{
		public Placement Placement;
		public WordPair WordPair;
		public bool BaseWordPair = false;
		public int Intersections;
		public List<bool> AllPossibleAnchors;

		public BoardWordPair(Placement placement, WordPair wordPair, bool baseWordPair)
		{
			Placement = placement;
			WordPair = wordPair;
			BaseWordPair = baseWordPair;
			Intersections = 0;
			AllPossibleAnchors = Enumerable.Repeat(false, wordPair.Letters.Length).ToList();
			int allPossibleAnchorsInt = 0;
			foreach (int anchor in WordPair.Anchors) { allPossibleAnchorsInt |= anchor; }
			for (int i = 0; i < AllPossibleAnchors.Count; i++)
			{
				AllPossibleAnchors[i] = (allPossibleAnchorsInt & 1 << WordPair.Letters.Length - 1 - i) > 0;
			}
		}
	}
}

