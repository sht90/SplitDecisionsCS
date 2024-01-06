namespace SplitDecisions
{
    public class Cell
	{
        public string Contents;
        public Entropy Entropy;

        public Cell(string contents = "", Entropy entropy = Entropy.Default)
        {
            Contents = contents;
            Entropy = entropy;
        }

        public override string ToString()
        {
            if (Contents.Equals(""))
            {
                return "0";
            }
            return Contents;
        }
    }
}

