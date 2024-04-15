namespace SplitDecisions
{
    internal class BoardFinder
    {
        private Board Board;
        public BoardFinder(BoardSettings settings)
        {
            // Parse settings
            Board = new Board(settings);
        }

        public void FindBoard(Board board)
        {
            if (IsValidSolution(board))
            {
                Board = board;
                return;
            }
        }

        public bool IsValidSolution(Board board)
        {
            return board.IsFull() && board.IsAnchored();
        }
    }
}