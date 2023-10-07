namespace SplitDecisions
{
	/// <summary>
	/// The settings that define a board are:
	/// * board dimensions
	///   * board width
	///   * board height
	/// * geometric restrictions on words
	///   * min word length
	///   * max word length
	/// * how many words per WordPair use the reference dictionary
	///   * minUsability
	/// </summary>
    public class BoardSettings
	{
		public int MinUsability;
		public int BoardWidth;
		public int BoardHeight;
		public int MinWordLength;
		public int MaxWordLength;

		public BoardSettings(int minUsability, int boardWidth, int boardHeight, int minWordLength = (int)WordLengths.MIN, int maxWordLength = (int)WordLengths.MAX)
		{
			MinUsability = minUsability;
			BoardWidth = boardWidth;
			BoardHeight = boardHeight;
			MinWordLength = Math.Max(minWordLength, (int)WordLengths.MIN);
			MaxWordLength = Math.Min(maxWordLength, Math.Min((int)WordLengths.MAX, Math.Max(boardWidth - 1, boardHeight - 1)));
		}
	}
}

