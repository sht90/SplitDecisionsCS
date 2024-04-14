using System;
namespace SplitDecisions
{
	public class Intersection
    {
		public RowCol RowCol;
        public BoardWordPair BoardWordPairHorizontal;
        public BoardWordPair BoardWordPairVertical;
        public int IndexHorizontal;
        public int IndexVertical;
		private bool isValid = false;
		public Intersection(BoardWordPair wp1, BoardWordPair wp2, BoardSettings settings)
		{
			// if the wordPairs intersect, this should be guaranteed
			if (wp1.Placement.Dir == Orientation.Horizontal)
			{
				BoardWordPairHorizontal = wp1;
				BoardWordPairVertical = wp2;
			}
			else
			{
                BoardWordPairHorizontal = wp2;
                BoardWordPairVertical = wp1;
            }
			RowCol = new RowCol(BoardWordPairHorizontal.Placement.Row, BoardWordPairVertical.Placement.Col, settings);
            IndexHorizontal = BoardWordPairHorizontal.Placement.IndexOf(RowCol);
            IndexVertical = BoardWordPairVertical.Placement.IndexOf(RowCol);
			UpdateValidity();
        }

		public void UpdateValidity()
		{
			// Are the wordPairs perpendicular?
			if (BoardWordPairHorizontal.Placement.Dir != Orientation.Horizontal
				|| BoardWordPairVertical.Placement.Dir != Orientation.Vertical)
            {
                isValid = false;
				return;
            }
			// Do the wordPairs both contain the intersection?
			if (IndexHorizontal < 0 || IndexVertical < 0)
			{
				isValid = false;
				return;
			}
			// Is there only one letter at this intersection?
			if (BoardWordPairHorizontal.WordPair[IndexHorizontal] != BoardWordPairVertical.WordPair[IndexVertical]
				&& BoardWordPairHorizontal.WordPair[IndexHorizontal].Length == 1)
			{
				isValid = false;
				return;
			}
			// Seems valid to me
			isValid = true;
		}

		public bool IsValid()
		{
			return isValid;
		}
	}
}

