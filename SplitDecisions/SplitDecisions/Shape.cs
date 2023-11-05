namespace SplitDecisions
{
    internal class Shape: IComparable, IComparable<Shape>
    {
        public int Length;
        public int Index;
        public Shape(int length, int index)
        {
            Length = length;
            Index = index;
        }

        public int CompareTo(object? other)
        {
            if (other == null) { return 1; }
            if (other is Shape shape) { return this.CompareTo(shape); }
            throw new ArgumentException("Object is not a Shape");
        }

        public int CompareTo(Shape? other)
        {
            if (other == null) { return 1; }
            int compareByLength = this.Length.CompareTo(other.Length);
            if (compareByLength != 0) { return compareByLength; }
            return this.Index.CompareTo(other.Index);
        }

        public override bool Equals(object? other)
        {
            Shape? o = other as Shape;
            if (o == null) return false;
            return this.CompareTo(o) == 0;
        }

        public override int GetHashCode()
        {
            return (int)((Length * 1.0 - 3.0) * (Length * 1.0 / 2.0)) + Index;
        }
    }
}
