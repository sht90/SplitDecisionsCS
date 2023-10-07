using System;
namespace SplitDecisions
{
    internal class BoardFinder
    {

        enum Entropy
        {
            Resolved,
            //ForcedEmpty, // don't make a ForcedEmpty entropy, just make the cell empty when you place the WordPair
            Anchor,
            Floater,
            Default
        }

        private int MinWordLength;
        private int MaxWordLength;
        private int Height;
        private int Width;
        private List<WordPair> WordPairs = new() { };
        Entropy[][] BoardEntropy;
        string[][] Board;
        int[]? tb;
        int[]? tw;
        List<WordPair> usedWordPairs;
        List<string> usedPrompts;
        List<string> usedWords;

        public BoardFinder(BoardSettings settings)
        {
            // Parse settings
            MinWordLength = settings.MinWordLength;
            MaxWordLength = settings.MaxWordLength;
            Height = settings.BoardHeight;
            Width = settings.BoardWidth;
            // Make initial Board, including entropy
            BoardEntropy = Enumerable.Repeat(Enumerable.Repeat(Entropy.Default, Width).ToArray(), Height).ToArray();
            Board = Enumerable.Repeat(Enumerable.Repeat("", Width).ToArray(), Height).ToArray();
            // Make the lists for keeping track of which wordPairs/words/prompts we've used so far. We don't want any repeats.
            // Normally you'd just remove these things from the list, but since the list of Wordpairs (10k-100k) is so huge compared to the list of used WordPairs on the board (10-100), it's probably computationally cheaper just to check whether a word has been used yet.
            usedWordPairs = new() { };
            usedPrompts = new() { };
            usedWords = new() { };
        }

        public void ResetBoard()
        {
            // reset Board for everything except which words/prompts/wordPairs have been used already.
            BoardEntropy = Enumerable.Repeat(Enumerable.Repeat(Entropy.Default, Width).ToArray(), Height).ToArray();
            Board = Enumerable.Repeat(Enumerable.Repeat("", Width).ToArray(), Height).ToArray();
            tb = null;
            tw = null;
        }

        // Find one Board. Even if we could make every single board during my lifetime, most of them would be duplicates except for one 3-letter WordPair. Parsing the results would be harder than just generating a new one from scratch with a different seed. 
        public string[][]? FindBoard(List<WordPair> wordPairs, int seed = -1)
        {
            // setup
            // get traversal order for board (just top half) and word pairs list
            // abbreviate these because I'll be using them a lot, but Traversal Order Board -> tb and Traversal Order WordPairs -> tw.
            tb = Enumerable.Range(0, (int)Math.Floor(Height * 0.5) * Width + (int)Math.Floor(Width * 0.5)).ToArray();
            tw = Enumerable.Range(0, wordPairs.Count).ToArray();
            // shuffle traversal order
            Random rng = (seed < 0) ? new() : new(seed);
            FisherYatesShuffle(rng, tb);
            FisherYatesShuffle(rng, tw);
            
            // return the results of the recursive solving algorithm
            return Solve(Board);
        }

        public string[][]? Solve(string[][] board)
        {
            if (Reject(board)) { return null; }
            if (IsFullSolution(board)) { return board; }
            string[][]? attempt = Extend(board);
        }

        public bool Reject(string[][] board)
        {
            return true;
        }

        public bool IsFullSolution(string[][]board)
        {
            // Is the board a full, valid Split Decisions Board?

            // if you could add another word, you must
            //??? this might not belong in IsFullSolution, but basically check areas to see if they could physically fit another valid wordpair shape

            // verify that there are no "islands?"
            //??? this should definitely be forced by how we add new WordPairs to the board

            // verify that the board is the correct size and looks full enough
            double densityThreshold = 0.5;
            int fullTiles = 0;
            int totalTiles = Height * Width;
            int edgeConditions = 0;
            for (int r = 0; r < board.Length; r++)
            {
                for (int c = 0; c < board[r].Length; c++)
                {
                    // contribute to density check
                    if (board[r][c] != "") { fullTiles++; }
                    // check to see if the board has word pairs on the edges
                    // WordPairs on top
                    if (r == 0 && c < Width / 2 && board[r][c] != "") { edgeConditions |= (1 << 0); }
                    if (r == 0 && c > Width / 2 && board[r][c] != "") { edgeConditions |= (1 << 1); }
                    // WordPairs on bottom
                    if (r == Height - 1 && c < Width / 2 && board[r][c] != "") { edgeConditions |= (1 << 2); }
                    if (r == Height - 1 && c > Width / 2 && board[r][c] != "") { edgeConditions |= (1 << 3); }
                    // WordPairs on left
                    if (r < Height / 2 && c == 0 && board[r][c] != "") { edgeConditions |= (1 << 4); }
                    if (r > Height / 2 && c == 0 && board[r][c] != "") { edgeConditions |= (1 << 5); }
                    // WordPairs on right
                    if (r < Height / 2 && c == Width - 1 && board[r][c] != "") { edgeConditions |= (1 << 6); }
                    if (r > Height / 2 && c == Width - 1 && board[r][c] != "") { edgeConditions |= (1 << 7); }
                    // check to see if conditions were satisfied
                    if (edgeConditions == (1 << 8) - 1 && fullTiles * 1.0 / totalTiles > densityThreshold) { return true; }
                }
            }
            return false;
        }

        public string[][]? Extend(string[][] board)
        {
            return null;
        }

        public string[][]? Next(string[][] board)
        {
            return null;
        }

        private static void FisherYatesShuffle<T>(Random rng, T[] array)
        {
            // basically, for each index in the array, swap it with a random index.
            int n = array.Length;
            while (n > 1)
            {
                int k = rng.Next(n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
            // this is O(n), which is the fastest reputable shuffling alg I could find.
        }
    }
}

