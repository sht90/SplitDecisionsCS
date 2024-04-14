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
        public int Length;
        public Orientation Dir;
        public Placement(int row, int col, int length, Orientation horizontal)
        {
            Row = row;
            Col = col;
            Length = length;
            Dir = horizontal;
        }

        public override string ToString()
        {
            return Dir == Orientation.Horizontal ? string.Format("{0},{1} Across", Row, Col) : string.Format("{0},{1} Down  ", Row, Col);
        }

        public RowCol LastLetterRowCol(BoardSettings settings)
        {
            if (Dir == Orientation.Horizontal) return new RowCol(Row, Col + Length, settings);
            return new RowCol(Row + Length, Col, settings);
        }

        public bool Contains(RowCol rowCol)
        {
            if (Dir == Orientation.Horizontal)
                return (this.Row == rowCol.Row && this.Col <= rowCol.Col && this.Col + Length > rowCol.Col);
            return (this.Col == rowCol.Col && this.Row <= rowCol.Row && this.Row + Length > rowCol.Row);
        }
    }
}

