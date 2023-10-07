namespace SplitDecisions
{
    internal class BoardFinder
    {
        private int MinWordLength;
        private int MaxWordLength;
        private int Height;
        private int Width;

        List<WordPair> UsedWordPairs;
        List<string> UsedWords;

        private List<List<List<int>>> CellsQueue;
        // list of lists, sorted by entropy value
        //   list of cells, in arbitrary order
        //     list of ints: row, col
        string[][] Board;
        Entropy[][] BoardEntropy;
        // list of strings to indicate board cells
        Dictionary<WordPair, List<int>> BoardWords;
        // LUT for wordpairs that are actually on the board, and their cells

        enum Entropy
        {
            Resolved,
            Anchor,
            Floater,
            Default
        }

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
            // Normally you'd just remove these things from the list, but since the list of Wordpairs (10k-100k) is so huge compared to the list of used WordPairs on the board (10-100), it's probably computationally cheaper just to check whether a word has been used yet. I suppose I could test both approaches sooner or later.
            UsedWordPairs = new() { };
            UsedWords = new() { };
            CellsQueue = new List<List<List<int>>>()
            {
                new List<List<int>>() { },  // for Anchor in Entropy Enum
                new List<List<int>>() { },  // for Floater
                new List<List<int>>() { }   // for Default
            };
            BoardWords = new() { };
        }

        /// <summary>
        /// Adds a new word pair to the board and manages all relevant metadata:
        /// * Adds a new cell to the board for each tile in the word pair
        /// * Puts empty cells around the new word pair where necessary
        /// * Adds wordPair and indexes of new cell to BoardWords LUT
        /// * Adds new cells to CellsQueue
        /// * Updates entropy of each cell in queue and on board
        /// </summary>
        /// <param name="wordPair">
        /// Word pair to be added to the board
        /// </param>
        public void Add(WordPair wordPair)
        {

        }

        /// <summary>
        /// Removes a word pair from the board and manages all relevant metadata
        /// </summary>
        /// <param name="wordPair">
        /// Word pair to be removed from board
        /// </param>
        public void RemoveWordPair(WordPair wordPair)
        {

        }

        /// <summary>
        /// Returns true if you can place a word pair through the given cell
        /// </summary>
        /// <param name="cellRow"></param>
        /// <param name="cellCol"></param>
        /// <param name="wordPair"></param>
        /// <returns></returns>
        public bool IsInvalid (int cellRow, int cellCol, WordPair wordPair)
        {

        }

        /// <summary>
        /// If it exists, get a cell for where to put 
        /// </summary>
        /// <param name="cellRow"></param>
        /// <param name="cellColumn"></param>
        /// <param name="wordPair"></param>
        /// <returns></returns>
        public List<int>? GetPlacement(int cellRow, int cellColumn, WordPair wordPair)
        {
            return null;
        }

























        /* Everything below this point was made with a generalized approach to backtracking recursion that I learned in school.
         * I've unfortunately temporarily misplaced those files (hopefully they're recoverable?), but I also think I was trying
         * to force-fit something that I understood well into an abstract framework that I didn't understand very well (even
         * though, theoretically, it should've worked perfeclty fine). I recently came into contact with a recursive algorithm
         * for generating blue noise, and it inspired me to revisit this project. It's not tailor-made to fit this application,
         * and it doesn't even backtrack, but I think it's a good starting point. So...
         * 
         * 
         * IGNORE EVERYTHING BELOW HERE (for now!)

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
        Entropy[][] BoardEntropy;
        string[][] Board;
        int[]? tb; // Every board position, to be stored as an array in a randomly shuffled order
        int[]? tw; // Every wordPair, to be stored as an array in a randomly shuffled order
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
            // Normally you'd just remove these things from the list, but since the list of Wordpairs (10k-100k) is so huge compared to the list of used WordPairs on the board (10-100), it's probably computationally cheaper just to check whether a word has been used yet. I suppose I could test both approaches sooner or later.
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
            string[][]? attempt = AddNewShape(board);
            while (attempt != null)
            {
                attempt = AddNewPrompt(attempt);
                if (attempt == null) break;
                return Solve(attempt);
            }
            return null;
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

        public string[][]? AddNewShape(string[][] board)
        {
            return null;
        }

        public string[][]? AddNewPrompt(string[][] board)
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
        }*/
    }
}
