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

        Dictionary<Entropy, List<List<int>>> CellsQueue;
        // list of lists, sorted by entropy value
        //   list of cells, in arbitrary order
        //     list of ints: row, col
        string[][] Board;
        Entropy[][] BoardEntropy;
        // list of strings to indicate board cells
        Dictionary<WordPair, List<List<int>>> WordPairCellsLUT;
        // LUT for wordpairs that are actually on the board, and their cells

        enum Entropy
        {
            Resolved,  // a cell that's placed on the board. Besides backtracking, don't mess with resolved cells.
            Anchor,  // this cell must be resolved to constrain a wordPair
            HalfAnchor, // anchors appear in pairs beside a cell that needs to be intersected to constrain a given wordPair. But only one of them needs to be resolved, necessarily.
            Floater,  // this cell is next to a wordPair with multiple different anchoring conditions. Resolving this cell narrows down which floaters become anchors, and which floaters go back to default entropy (which... going back up in entropy isn't really how entropy works... maybe I've strayed far enough away from the original intention that I should consider a new name).
            Default  // default entropic state
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
            CellsQueue = new ()
            {
                { Entropy.Anchor, new List<List<int>>() },
                { Entropy.HalfAnchor, new List<List<int>>() },
                { Entropy.Floater, new List<List<int>>() }
                // This is just the queue. A resolved cell is effectively off the queue, and a default cell effectively hasn't been put on the queue yet.
            };
            WordPairCellsLUT = new() { };
        }

        /// <summary>
        /// Adds a new word pair to the board and manages all relevant metadata:
        /// * Adds a new cell to the board for each tile in the word pair
        /// * Puts empty cells around the new word pair where necessary
        /// * Adds wordPair and indexes of new cell to WordPairCellsLUT
        /// * Adds new cells to CellsQueue
        /// * Updates entropy of each cell in queue and on board
        /// </summary>
        /// <param name="wordPair">
        /// Word pair to be added to the board
        /// </param>
        public void Add(WordPair wordPair, Placement placement)
        {
            // We'll put this cells list in an LUT soon
            List<List<int>> cells = new();
            // prepare to traverse new wordPair
            int row = placement.Row;
            int col = placement.Col;
            // in this traversal, we should also track where the intersections are
            List<bool> currentIntersections = new();
            for (int i = 0; i < wordPair.Shape.Length; i++)
            {
                // get indexes of cells
                if (placement.Dir == Orientation.Horizontal) col = placement.Col + i;
                else row = placement.Row + i;
                // add cell index to cells list for LUT
                cells.Add(new List<int>() { row, col });
                // a cell is an intersection if it existed before adding this new cell
                currentIntersections.Add(BoardEntropy[row][col] == Entropy.Resolved);
                // add new tile to the board
                Board[row][col] = wordPair[i];
                // resolve cell's entropy
                BoardEntropy[row][col] = Entropy.Resolved;

                // Now that the cell itself is done, update the surrounding cells/entropy
                // there must be an empty cell right before and right after the wordPair
                if (i == 0)
                {
                    int tmpRow = row;
                    int tmpCol = col;
                    if (placement.Dir == Orientation.Horizontal) tmpCol = col - 1;
                    else tmpRow = row - 1;
                    Board[tmpRow][tmpCol] = "0";
                    BoardEntropy[tmpRow][tmpCol] = Entropy.Resolved;

                }
                else if (i == wordPair.Shape.Length - 1)
                {
                    int tmpRow = row;
                    int tmpCol = col;
                    if (placement.Dir == Orientation.Horizontal) tmpCol = col + 1;
                    else tmpRow = row + 1;
                    Board[tmpRow][tmpCol] = "0";
                    BoardEntropy[tmpRow][tmpCol] = Entropy.Resolved;
                }
            }
            // add cells list to LUT
            WordPairCellsLUT.Add(wordPair, cells);
            // narrow down the anchor conditions based on the current intersections
            SimplifyAnchors(wordPair, currentIntersections);
            // now go back for another traversal, to establish entropy values of neighboring cells
            int rowUL, colUL, rowDR, colDR, rowUL1, colUL1, rowDR1, colDR1;
            row = placement.Row;
            col = placement.Col;
            for (int i = 0; i < wordPair.Shape.Length; i++)
            {
                // get indexes of cells
                if (placement.Dir == Orientation.Horizontal)
                {
                    col = placement.Col + i;
                    rowUL = row - 1;
                    rowDR = row + 1;
                    rowUL1 = row - 2;
                    rowDR1 = row + 2;
                    colUL = col;
                    colDR = col;
                    colUL1 = col;
                    colDR1 = col;
                }
                else
                {
                    row = placement.Row + i;
                    colUL = col - 1;
                    colDR = col + 1;
                    colUL1 = col - 2;
                    colDR1 = col + 2;
                    rowUL = row;
                    rowDR = row;
                    rowUL1 = row;
                    rowDR1 = row;
                }
                // If this cell is an anchor in ANY of the possible anchors for this wordPair, it should have floaters on either side
                // if this cell is an anchhor for ALL of the possible anchors for this wordPair, overwrite its status as a floater -- it should have halfanchors on either side
                // if you can't put a halfanchor on one side (it's already resolved or it's too close to the edge of the board), then the other side is an anchor
                // if you can't do that for either side, then you shouldn't be able to place this on the board at all... TODO: how do we prevent that from happening? The only situation I can think of is if a mandatory anchor point passes in between two cells that must be empty. Or if one cell must be empty and the other side is too close to the edge of the board. That check sounds feasible to do in ValidPlacement, okay... nice. Let's assume that check is already implemented in ValidPlacement, and we're not immediatley screwed for putting this word on the board.
                int[] counts = Enumerable.Repeat(0, wordPair.Letters.Length).ToArray();
                for (int j = 0; j < wordPair.Anchors.Count; j++)
                {
                    for (int k = 0; k < wordPair.Anchors[j].Count; k++)
                    {
                        if (wordPair.Anchors[j][k]) counts[k]++;
                    }
                }
                for (int m = 0; m < counts.Length; m++)
                {
                    // cells that want to be HalfAnchors
                    if (counts[m] == wordPair.Anchors.Count - 1)
                    {
                        if (colUL < 2 || rowUL < 2 || BoardEntropy[rowUL][colUL] == Entropy.Resolved || (BoardEntropy[rowUL1][colUL1] == Entropy.Resolved && Board[rowUL1][colUL1][0] == '0'))
                        {
                            // the up-or-left neighboring cell is off the board, too close to the edge of the board, already resolved, or too close to a resolved empty cell.
                            BoardEntropy[rowDR][colDR] = Entropy.Anchor;
                        }
                        else if (colDR >= Height - 2 || rowDR >= Height - 2 || (BoardEntropy[rowDR][colDR] == Entropy.Resolved && Board[rowDR][colDR][0] == '0') || (BoardEntropy[rowDR1][colDR1] == Entropy.Resolved && Board[rowDR1][colDR1][0] == '0'))
                        {
                            // the down-or-right neighboring cell is off the board, too close to the edge of the board, already resolved, or too close to a resolved empty cell.
                            BoardEntropy[rowUL][colUL] = Entropy.Anchor;
                        }
                        else
                        {
                            // both cells can be used as an anchor
                            BoardEntropy[rowUL][colUL] = Entropy.HalfAnchor;
                            BoardEntropy[rowDR][colDR] = Entropy.HalfAnchor;
                        }
                    }
                    // cells that want to be Floaters.
                    else if (counts[m] > 0)
                    {
                        if (!(colUL < 2 || rowUL < 2 || BoardEntropy[rowUL][colUL] == Entropy.Resolved || (BoardEntropy[rowUL1][colUL1] == Entropy.Resolved && Board[rowUL1][colUL1][0] == '0')))
                        {
                            BoardEntropy[rowUL][colUL] = Entropy.Floater;
                        }
                        if (!(colDR >= Height - 2 || rowDR >= Height - 2 || (BoardEntropy[rowDR][colDR] == Entropy.Resolved && Board[rowDR][colDR][0] == '0') || (BoardEntropy[rowDR1][colDR1] == Entropy.Resolved && Board[rowDR1][colDR1][0] == '0')))
                        {
                            BoardEntropy[rowDR][colDR] = Entropy.Anchor;
                        }
                        // if neither of these things can be a floater, then we need to update the anchors. But I think this goes back to the earlier issue about being able to verify whether the placement is valid based on anchor conditions and adjacent criteria. TODO.
                    }
                    // for every other cell, just leaves its neighbors as their existing entropy value.
                }
            }
            // The Add method is "finished" here now, but I've set myself up for a hellish time when removing a wordPair, because I'd also need to undo all of its entropy changes (again, entropy reveals itself to be an inaccurate word choice here). I think a better implementation might be to have a stack of BoardEntropy instead of just a global array. I could also just pass it through the recursive functions instead of building my own "recursive stack." But also, I'm tired. TODO.
        }

        public void SimplifyAnchors(WordPair wordPair, List<bool> intersections)
        {
            List<int> scores = new();
            int score;
            foreach (List<bool> anchor in wordPair.Anchors)
            {
                score = 0;
                for (int i = 0; i < anchor.Count; i++)
                {
                    if (anchor[i] && !intersections[i])
                    {
                        score += 1;
                    }
                    if (anchor[i] && intersections[i])
                    {
                        anchor[i] = false;
                    }
                }
                scores.Add(score);
            }
            int minScore = scores[0];
            foreach (int s in scores)
            {
                if (s < minScore) minScore = s;
            }
            List<List<bool>> simplifiedAnchors = new();
            for (int i = 0; i < wordPair.Anchors.Count(); i++)
            {
                if (scores[i] == minScore)
                {
                    simplifiedAnchors.Add(wordPair.Anchors[i]);
                }
            }
            wordPair.Anchors = simplifiedAnchors;
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
        /// If it exists, get a cell for where to put 
        /// </summary>
        /// <param name="cellRow"></param>
        /// <param name="cellColumn"></param>
        /// <param name="wordPair"></param>
        /// <returns></returns>
        public List<Placement>? ValidPlacements(WordPair wordPair, int cellRow=-1, int cellCol=-1)
        {
            List<Placement> placements = new();  // to be returned at the end of the function
            // If starting from a null cell or unassigned cell
            if (cellRow == -1 && cellCol == -1 || Board[cellRow][cellCol].Length < 1)
            {
                // Any placement is valid as long as the word pair doesn't run off the board
                // Traverse every cell
                bool broken = false;
                for (int row = 0; row < Height; row++)
                {
                    for (int col = 0; col < Width; col++)
                    {
                        if (row >= Height - wordPair.Shape.Length && col > Width - wordPair.Shape.Length)
                        {
                            broken = true;
                            break;
                        }
                        if (row < Height - wordPair.Shape.Length)
                        {
                            placements.Add(new Placement(row, col, Orientation.Vertical));
                        }
                        if (col < Width - wordPair.Shape.Length)
                        {
                            placements.Add(new Placement(row, col, Orientation.Horizontal));
                        }
                    }
                    if (broken) break;
                }
                return placements;
            }
            // invalid case that should never happen
            if (cellRow < 0 || cellCol < 0 || cellRow >= Height || cellCol >= Width)
            {
                return null;
            }
            // If you've made it this far, you're starting from a not-null cell
            // Handle the remaining trivial cases
            if (Board[cellRow][cellCol].Length > 1 || Board[cellRow][cellCol] == "0")
            {
                // If the cell is already assigned to as being empty or a double-letter tile,
                // then no, there is never a valid intersection here. So yes it's invalid
                return null;
            }
            
            // Start by finding any possible intersection points
            char cell = Board[cellRow][cellCol][0];
            List<int> intersections = new() { };
            for (int i = 0; i < wordPair.Letters.Length; i++)
            {
                if (wordPair.Letters[i] == cell) intersections.Add(i);
            }
            // An example of when this might happen is if you're trying to see where a word like abc can intersect a word like zzz. It can't, because they have no letters in common.
            if (intersections.Count < 1) return null;

            // Generate a full list of all possible placements...
            List<Placement> possibilities = new();
            foreach (int i in intersections)
            {
                possibilities.Add(new Placement(cellRow - i, cellCol, Orientation.Vertical));
                possibilities.Add(new Placement(cellRow, cellCol - i, Orientation.Horizontal));
            }
            // ... and now try to rule out each of the possible placements
            foreach (Placement placement in possibilities)
            {
                // if the word starts off the board, it's definitely off the board
                if (placement.Row < 0 || placement.Col < 0 || placement.Row > Height || placement.Col > Width)
                {
                    continue;
                }
                // if the end of the word goes off the board, that's also invalid
                if (placement.Dir == Orientation.Horizontal && placement.Col >= Width - wordPair.Shape.Length || placement.Dir == Orientation.Vertical && placement.Row >= Height - wordPair.Shape.Length)
                {
                    continue;
                }
                // So the word pair is at least on the board. Determine whether its interactions with any of the other existing cells on the board are also valid.
                bool broken = false;
                for (int tile = -1; tile < wordPair.Shape.Length + 1; tile++)
                {
                    int checkRow = placement.Row;
                    int checkCol = placement.Col;
                    if (placement.Dir == Orientation.Horizontal) checkRow += tile;
                    else checkCol += tile;

                    // There needs to be room for you to place an empty cell before and after the wordPair. The spacing of an empty tile is the only way to discern where one wordPair ends and where another begins.
                    if (tile < 0 || tile == wordPair.Shape.Length)
                    {
                        if (checkRow < 0 || checkRow >= Height || checkCol < 0 || checkCol >= Width)
                        {
                            // unless the empty cell would be off the board, which is actually fine
                            continue;
                        }
                        if (BoardEntropy[checkRow][checkCol] == Entropy.Resolved)
                        {
                            if (Board[checkRow][checkCol][0] != '0')
                            {
                                // Obvious bad case. The end of this wordPair would butt up against another wordPair.
                                broken = true;
                                break;
                            }
                            // If you're here, then the cell that has to be empty already had to be empty for some other reason. Nice!
                            continue;
                        }
                        if (BoardEntropy[checkRow][checkCol] == Entropy.Anchor)
                        {
                            // This cell is not populated yet, but NEEDS to be populated with a letter tile. That's a contradiction.
                            broken = true;
                            break;
                        }
                        // If you've made it this far, you're able to place an empty tile here.
                        continue;
                    }

                    // If you've made it this far, you're looking at intersections inside the wordPair
                    // Intersecting with a grid cell that's not resolved yet.
                    if (BoardEntropy[checkRow][checkCol] != Entropy.Resolved)
                    {
                        // You can't place the word down next to another parallel wordPair
                        // (well, technically you could if you were able to also resolve the overlaps into their own wordPairs in the future.)
                        // TODO: accmmodate 'hooking' another wordPair

                        // look at neighboring cells...
                        int checkRowUL = checkRow;
                        int checkColUL = checkCol;
                        int checkRowDR = checkRow;
                        int checkColDR = checkCol;
                        if (placement.Dir == Orientation.Horizontal)
                        {
                            checkColUL--;
                            checkColDR++;
                        }
                        else
                        {
                            checkRowUL--;
                            checkRowDR++;
                        }
                        if (checkRowUL >= 0 && checkRowUL < Height && checkColUL >= 0 && checkColUL < Width)
                        {
                            // this wordPair would run parallel to another wordPair that's immediately adjacent. Not allowed!
                            if (Board[checkRowUL][checkColUL].Length > 0 && Board[checkRowUL][checkColUL][0] != '0')
                            {
                                broken = true;
                                break;
                            }
                        }
                        if (checkRowDR >= 0 && checkRowDR < Height && checkColDR >= 0 && checkColDR < Width)
                        {
                            // this wordPair would run parallel to another wordPair that's immediately adjacent. Not allowed!
                            if (Board[checkRowDR][checkColDR].Length > 0 && Board[checkRowDR][checkColDR][0] != '0')
                            {
                                broken = true;
                                break;
                            }
                        }
                        // this cell is fine if you've made it this far.
                        continue;
                    }
                    // If you're creating another intersection on the board...
                    else
                    {
                        if (tile >= wordPair.Shape.Index && tile <= wordPair.Shape.Index + 1)
                        {
                            // you're trying to place a double letter tile over a tile that was already filled in. That's no good.
                            broken = true;
                            break;
                        }
                        char compLetter;
                        if (tile < wordPair.Shape.Index)
                        {
                            compLetter = wordPair.Letters[tile];
                        }
                        else
                        {
                            compLetter = wordPair.Letters[tile - 2];
                        }
                        if (compLetter != Board[checkRow][checkCol][0])
                        {
                            // you're trying to create an intersection between two letters that aren't the same. That won't work.
                            // This also applies exactly the same as if the cell is forced to be empty, ie its entropy is resolved and its value is '0'
                            broken = true;
                            break;
                        }
                        // if you've made it this far, then you're either trying to make an ordinary intersection, or you're going to find out very soon that you're trying to make an intersection like (ac/sa)me onto me(an/nd) by the shared 'me' (which is invalidated by the earlier rule of "there needs to be an empty cell at the start and end of each wordPair).
                        continue;
                    }
                }
                if (broken) continue;
                // if you made it this far, this placement isn't inherently invalid. Nice!
                placements.Add(placement);
            }
            return placements;
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
