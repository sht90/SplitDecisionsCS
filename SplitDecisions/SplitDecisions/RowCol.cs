namespace SplitDecisions
{
    /// <summary>
    /// Basically just a tuple for Row, Column, but with some additional Compare stuff
    /// </summary>
    public class RowCol: IComparable, IComparable<RowCol>
	{
		public int Row;
		public int Col;
        private BoardSettings Settings;

		public RowCol(int row, int col, BoardSettings settings)
		{
			Row = row;
			Col = col;
            Settings = settings;
		}

        public int CompareTo(object? other)
        {
            if (other == null) { return 1; }
            if (other is RowCol rowcol) { return this.CompareTo(rowcol); }
            throw new ArgumentException("Object is not a RowCol");
        }

        public int CompareTo(RowCol? other)
        {
            if (other == null) { return 1; }
            return (this.Row * this.Settings.BoardWidth + this.Col).CompareTo(other.Row * other.Settings.BoardWidth + other.Col);
        }

        public override bool Equals(object? other)
        {
            RowCol? rc = other as RowCol;
            if (rc == null) return false;
            return this.CompareTo(rc) == 0;
        }

        public override int GetHashCode()
        {
            return this.Row * this.Settings.BoardWidth + this.Col;
        }
    }
}

