namespace SplitDecisions
{
    public class Shape: IComparable, IComparable<Shape>
    {
        public int Length;
        public int Index;
        public int Id;
        public Shape(int length, int index)
        {
            Length = length;
            Index = index;
            // A Shape Id is a unique consecutive int for each shape.
            // This felt very sum-of-first-n-natural-numbers, and
            // once you start from there the derivation is pretty easy
            Id = Length * (Length - 3) / 2 + Index;
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
            return this.Id.CompareTo(other.Id);
        }

        public override int GetHashCode()
        {
            return this.Id;
        }
    }
}
