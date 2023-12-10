using System;
namespace SplitDecisions
{
    public enum Orientation
    {
        Horizontal,
        Vertical
    }

    public class Placement
    {
        public int Row;
        public int Col;
        public Orientation Dir;
        public Placement(int row, int col, Orientation horizontal)
		{
            Row = row;
            Col = col;
            Dir = Orientation.Horizontal;
		}

        public override string ToString()
        {
            return Dir == Orientation.Horizontal ? String.Format("{0},{1} Across", Row, Col) : String.Format("{0},{1} Down  ", Row, Col);
        }
    }
}

