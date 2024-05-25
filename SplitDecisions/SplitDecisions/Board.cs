using System;
namespace SplitDecisions
{
	public class Board
	{
		List<BoardWordPair> BoardWordPairs;
		int Height;
		int Width;
		string[][] Cells;
        List<Intersection> Intersections;
		BoardSettings Settings;
        //private bool isAnchored;
		//private bool isUnmistakeable;

		public Board(BoardSettings settings)
		{
			BoardWordPairs = new();
            Height = settings.BoardHeight;
            Width = settings.BoardWidth;
            Cells = Enumerable.Repeat(Enumerable.Repeat("", Width).ToArray(), Height).ToArray();
			Settings = settings;
			Intersections = new();
        }

		public bool IsConstrained()
        {
            // reset all intersections to 0. Let it represent anchor points here.
            foreach (Intersection x in Intersections)
            {
				x.BoardWordPairHorizontal.Intersections = 0;
				x.BoardWordPairVertical.Intersections = 0;
            }
            // if an intersection is unmistakeable, set it to 1. Otherwise it can't be an anchor.
            foreach (Intersection x in Intersections)
			{
				if (x.IsMistakeable()) continue;
				x.BoardWordPairHorizontal.Intersections |= 1 << (x.BoardWordPairHorizontal.WordPair.Letters.Length - 1 - x.BoardWordPairHorizontal.WordPair.ConvertToLettersIndex(x.IndexHorizontal));
                x.BoardWordPairVertical.Intersections |= 1 << (x.BoardWordPairVertical.WordPair.Letters.Length - 1 - x.BoardWordPairVertical.WordPair.ConvertToLettersIndex(x.IndexVertical));
            }
			// now we can traverse anchor conditions
			foreach (Intersection x in Intersections)
			{
				bool hgood = false;
				bool vgood = false;
				foreach (int anchorCondition in x.BoardWordPairHorizontal.WordPair.Anchors)
				{
					if ((anchorCondition & x.BoardWordPairHorizontal.Intersections) > 0)
					{
						hgood = true;
						break;
					}
                }
                if (!hgood) return false;
                foreach (int anchorCondition in x.BoardWordPairVertical.WordPair.Anchors)
                {
                    if ((anchorCondition & x.BoardWordPairVertical.Intersections) > 0)
                    {
                        vgood = true;
						break;
                    }
                }
				if (!vgood) return false;
            }
			return true;
            // TODO: do I use these intersections for their intended purpose anywhere else? If so, I really should restore them after calling this function. Which I guess would just be...
            /*foreach (Intersection x in Intersections)
            {
                x.BoardWordPairHorizontal.Intersections |= 1 << (x.BoardWordPairHorizontal.WordPair.Letters.Length - 1 - x.BoardWordPairHorizontal.WordPair.ConvertToLettersIndex(x.IndexHorizontal));
                x.BoardWordPairVertical.Intersections |= 1 << (x.BoardWordPairVertical.WordPair.Letters.Length - 1 - x.BoardWordPairVertical.WordPair.ConvertToLettersIndex(x.IndexVertical));
            }*/
        }


		public bool IsFull()
		{
            // Okay so I have a lot of different ideas for how this should work:
            // Idea #1: A board is full if you can't put another wordPair in it
            // I'd considered this a hard rule for a long time, and it's true for most -- though not all -- of Mr. Piscop's published puzzles. But it's not a standalone rule: e.g. a pinwheel of 4 4-letter-long wordPairs would count as a full board under this rule. It's also annoying and computationally expensive to calculate. So hopefully the other ideas will be more useful.
            // Idea #2: A board is full if it touches all edges. This is mandatory -- if you say you want a 13x13 puzzle and you get a 5x8, the code just isn't doing its job. But it's also not a standalone rule: this could easily make a thin donut around the edges of the board, or a shape sorta close to that, and then it'd be finished
            // Idea #3: A board is full if it's adequately dense. This probably works. It can't guarantee #1 or #2, but it can guarantee that the puzzle looks complete at a glance.
            // Idea #4: A board is full if its gaps are all adequately small. This may be a way to consolidate ideas #3 and #1. If you disallow any gaps equal to or larger than a 3x3 square, you can guarantee that you can't put a wordPair anywhere else on the board. You could still get a board that has lots of long 2xn gaps.
            // The easiest things to check -- and potentially the rules that'll lead to the most visually appealing result -- will be density and verifying if a board touches the edges. Let's do that!
            // Rely on symmetry of our process of adding words to quickly check the edges:
            bool touchesHorizontalEdges = false;
            bool touchesVerticalEdges = false;
			// Also keep track of this for checking the density later
			int fullCells = 0;
            foreach (BoardWordPair bwp in BoardWordPairs)
			{
				touchesHorizontalEdges = touchesHorizontalEdges || bwp.Placement.Row == 0;
                touchesVerticalEdges = touchesVerticalEdges || bwp.Placement.Row == 0;
				fullCells += bwp.Placement.Length;
            }
			if (!(touchesHorizontalEdges && touchesVerticalEdges)) return false;
			// Now check the density
			double densityThreshold = 0.5; // TODO: move densityThreshold to BoardSettings, it feels like it should be there.
			if ((fullCells - Intersections.Count) * 1.0 / (Settings.BoardHeight * Settings.BoardWidth) < densityThreshold) return false;
			return true;
		}
	}
}

