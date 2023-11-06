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
    }
}

