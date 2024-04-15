namespace SplitDecisions
{
    public class BoardFinder
    {
        public BoardSettings Settings;
        public List<WordPair> WordPairs;
        Dictionary<Shape, (int Start, int End)> ShapeRange;
        // TODO add this:  Dictionary<string, List<WordPair>> WordPairsWithLetter;
        public BoardFinder(List<WordPair> wordPairs, BoardSettings settings)
        {
            Settings = settings;
            WordPairs = wordPairs;
            // Find shape indices of WordPairs
            ShapeRange = new();
            Shape currentShape = WordPairs[0].Shape;
            int currentStart = 0;
            int currentEnd;
            for (int i = 0; i < WordPairs.Count; i++)
            {
                if (WordPairs[i].Shape == currentShape) { continue; }
                currentEnd = i;
                ShapeRange.Add(currentShape, (currentStart, currentEnd));
                currentShape = WordPairs[i].Shape;
                currentStart = i;
            }
            ShapeRange.Add(currentShape, (currentStart, WordPairs.Count));
        }

        public void FindBoard()
        {
            Board board = new Board(Settings);
            foreach (WordPair wp1 in WordPairs)
            {

            }
        }

        public void FindBoard(Board board)
        {
            if (IsValidSolution(board))
            {
                Environment.Exit(0);
            }
        }

        public bool IsValidSolution(Board board)
        {
            return board.IsFull() && board.IsAnchored();
        }
    }
}