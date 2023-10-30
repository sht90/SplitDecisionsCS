namespace SplitDecisions
{

    public enum Entropy
    {
        Resolved,  // a cell that's placed on the board. Besides backtracking, don't mess with resolved cells.
        Anchor,  // this cell must be resolved to constrain a wordPair
        HalfAnchor, // anchors appear in pairs beside a cell that needs to be intersected to constrain a given wordPair. But only one of them needs to be resolved, necessarily.
        Floater,  // this cell is next to a wordPair with multiple different anchoring conditions. It could serve as an Anchor, but if another Floater is treated as an Anchor instead, it could also be 'demoted' to Available.
        Available,  // this cell is next to a wordPair, and you could feasibly fit another WordPair next to it. A completed board should not have any Available cells in it, though that's not strictly necessary to guarantee a unique solution.
        Default  // default entropic state
    }

    internal class Cell
    {
        public string Contents;
        public Entropy Entropy;

        public Cell(string contents = "", Entropy entropy = Entropy.Default)
        {
            Contents = contents;
            Entropy = entropy;
        }
    }

    internal class BoardFinder
    {
        private int MinWordLength;
        private int MaxWordLength;
        private int Height;
        private int Width;

        List<WordPair> UsedWordPairs;
        List<string> UsedWords;

        // list of lists, sorted by entropy value
        //   list of cells, in arbitrary order
        //     list of ints: row, col
        List<Entropy> OrderedEntropies;
        Dictionary<Entropy, List<List<int>>> CellsQueue;
        // list of strings to indicate board cells
        Cell[][] Board;
        // map between WordPairs and their respective cells
        Dictionary<WordPair, List<List<int>>> WordPairToCellsLUT;
        Dictionary<List<int>, List<BoardWordPair>> CellsToWordPairsLUT;

        public BoardFinder(BoardSettings settings)
        {
            // Parse settings
            MinWordLength = settings.MinWordLength;
            MaxWordLength = settings.MaxWordLength;
            Height = settings.BoardHeight;
            Width = settings.BoardWidth;
            // Make initial board
            Board = Enumerable.Repeat(Enumerable.Repeat(new Cell(), Width).ToArray(), Height).ToArray();
            // Make the lists for keeping track of which wordPairs/words/prompts we've used so far. We don't want any repeats.
            // Normally you'd just remove these things from the list, but since the list of Wordpairs (10k-100k) is so huge compared to the list of used WordPairs on the board (10-100), it's probably computationally cheaper just to check whether a word has been used yet. I suppose I could test both approaches sooner or later.
            UsedWordPairs = new() { };
            UsedWords = new() { };
            OrderedEntropies = new()
            {
                Entropy.Anchor,
                Entropy.HalfAnchor,
                Entropy.Floater,
                Entropy.Available
            };
            CellsQueue = new ()
            {
                { Entropy.Anchor, new List<List<int>>() },
                { Entropy.HalfAnchor, new List<List<int>>() },
                { Entropy.Floater, new List<List<int>>() },
                { Entropy.Available, new List<List<int>>() }
                // This is just the queue. A resolved cell is effectively off the queue, and a default cell effectively hasn't been put on the queue yet.
            };
            WordPairToCellsLUT = new() { };
            CellsToWordPairsLUT = new() { };
            // May as well init CellsToWordPairsLUT with every possible cell in the constructor.
            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    CellsToWordPairsLUT.Add(new List<int>() { i, j }, new List<BoardWordPair>() { });
                }
            }
        }

        public static void PrintBoard(Cell[][] board, bool Contents=true, bool Entropy=false, bool toConsole=true, string fileName="")
        {
            string boardRep = "";
            for (int r = 0; r < board.Length; r++)
            {
                if (r != 0) boardRep += "\n";
                for (int c = 0; c < board[r].Length; c++)
                {
                    if (c != 0) boardRep += " ";
                    if (Contents)
                    {
                        // default cells should be empty
                        if (board[r][c].Contents == "") boardRep += "0";
                        else boardRep += board[r][c].Contents;
                    }
                    if (Contents && Entropy) boardRep += "|";
                    if (Entropy) boardRep += board[r][c].Entropy.ToString();
                }
            }
            if (toConsole)
            {
                Console.WriteLine(boardRep);
            }
            if (fileName != "")
            {
                File.WriteAllText(fileName, boardRep);
            }
        }

        public string[][] Solve()
        {
            Cell[][] board = Board;
            // Add starting point
            // Recursive function
            return boardAsStringArray(board);
        }

        private string[][] boardAsStringArray(Cell[][] board)
        {
            string[][] boardStr = Enumerable.Repeat(Enumerable.Repeat("", Width).ToArray(), Height).ToArray();
            for (int i = 0; i < board.Length; i++)
            {
                for (int j = 0; j < board[i].Length; j++)
                {
                    boardStr[i][j] = board[i][j].Contents;
                }
            }
            return boardStr;
        }

        // update entropy of cells that claim to be available. Are they really?
        private void UpdateboardAvailability(Cell[][] board)
        {
            return;
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
        public void Add(Cell[][] board, WordPair wordPair, Placement placement)
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
                currentIntersections.Add(board[row][col].Entropy == Entropy.Resolved);
                // add new tile to the board
                board[row][col].Contents = wordPair[i];
                // resolve cell's entropy
                board[row][col].Entropy = Entropy.Resolved;

                // Now that the cell itself is done, update the surrounding cells/entropy
                // there must be an empty cell right before and right after the wordPair
                if (i == 0)
                {
                    int tmpRow = row;
                    int tmpCol = col;
                    if (placement.Dir == Orientation.Horizontal) tmpCol = col - 1;
                    else tmpRow = row - 1;
                    board[tmpRow][tmpCol].Contents = "0";
                    board[tmpRow][tmpCol].Entropy = Entropy.Resolved;

                }
                else if (i == wordPair.Shape.Length - 1)
                {
                    int tmpRow = row;
                    int tmpCol = col;
                    if (placement.Dir == Orientation.Horizontal) tmpCol = col + 1;
                    else tmpRow = row + 1;
                    board[tmpRow][tmpCol].Contents = "0";
                    board[tmpRow][tmpCol].Entropy = Entropy.Resolved;
                }
            }
            // add cells list to LUT
            WordPairToCellsLUT.Add(wordPair, cells);
            foreach (List<int> cell in cells)
            {
                CellsToWordPairsLUT[cell].Add(new BoardWordPair(wordPair, placement));
            }
            // narrow down the anchor conditions based on the current intersections
            List<List<bool>> simplifiedAnchors = GetSimplifiedAnchors(wordPair, currentIntersections);
            // now go back for another traversal, to establish entropy values of neighboring cells
            int rowUL1, colUL1, rowDR1, colDR1, rowUL2, colUL2, rowDR2, colDR2;
            int srowULUL1, scolULUL1, srowULDR1, scolULDR1, srowULUL2, scolULUL2, srowULDR2, scolULDR2;
            int srowDRUL1, scolDRUL1, srowDRDR1, scolDRDR1, srowDRUL2, scolDRUL2, srowDRDR2, scolDRDR2;
            row = placement.Row;
            col = placement.Col;
            for (int i = 0; i < wordPair.Shape.Length; i++)
            {
                // get indexes of cells
                if (placement.Dir == Orientation.Horizontal)
                {
                    row = placement.Row + i;
                    colUL1 = col - 1;
                    colDR1 = col + 1;
                    colUL2 = col - 2;
                    colDR2 = col + 2;
                    rowUL1 = row;
                    rowDR1 = row;
                    rowUL2 = row;
                    rowDR2 = row;

                    scolULUL1 = col - 1;
                    scolULUL2 = col - 2;
                    scolULDR1 = col - 1;
                    scolULDR2 = col - 2;
                    scolDRUL1 = col + 1;
                    scolDRUL2 = col + 2;
                    scolDRDR1 = col + 1;
                    scolDRDR2 = col + 2;

                    srowULUL2 = row - 1;
                    srowULUL1 = row - 1;
                    srowULDR1 = row - 1;
                    srowULDR2 = row - 1;
                    srowDRUL1 = row + 1;
                    srowDRUL2 = row + 1;
                    srowDRDR1 = row + 1;
                    srowDRDR2 = row + 1;
                }
                else
                {
                    col = placement.Col + i;
                    rowUL1 = row - 1;
                    rowDR1 = row + 1;
                    rowUL2 = row - 2;
                    rowDR2 = row + 2;
                    colUL1 = col;
                    colDR1 = col;
                    colUL2 = col;
                    colDR2 = col;

                    srowULUL1 = row - 1;
                    srowULUL2 = row - 2;
                    srowULDR1 = row - 1;
                    srowULDR2 = row - 2;
                    srowDRUL1 = row + 1;
                    srowDRUL2 = row + 2;
                    srowDRDR1 = row + 1;
                    srowDRDR2 = row + 2;

                    scolULUL2 = col - 1;
                    scolULUL1 = col - 1;
                    scolULDR1 = col - 1;
                    scolULDR2 = col - 1;
                    scolDRUL1 = col + 1;
                    scolDRUL2 = col + 1;
                    scolDRDR1 = col + 1;
                    scolDRDR2 = col + 1;
                }
                // If this cell is an anchor in ANY of the possible anchors for this wordPair, it should have floaters on either side
                // if this cell is an anchhor for ALL of the possible anchors for this wordPair, overwrite its status as a floater -- it should have halfanchors on either side
                // if you can't put a halfanchor on one side (it's already resolved or it's too close to the edge of the board), then the other side is an anchor
                // IsValidPlacement makes sure that there's never a case where BOTH halfanchors are invalid.
                int[] counts = Enumerable.Repeat(0, wordPair.Letters.Length).ToArray();
                for (int j = 0; j < simplifiedAnchors.Count; j++)
                {
                    for (int k = 0; k < simplifiedAnchors[j].Count; k++)
                    {
                        if (simplifiedAnchors[j][k]) counts[k]++;
                    }
                }
                for (int m = 0; m < counts.Length; m++)
                {
                    // cells that want to be HalfAnchors
                    if (counts[m] == simplifiedAnchors.Count - 1)
                    {
                        bool ulBad = (
                            // cell furthest in the upper left direction needs to be on the board
                            !(srowULUL1 >= 0 && scolULUL2 >= 0)
                            // none of the split cells can be occupied, unless they're explicitly empty
                            || (board[srowULUL1][scolULUL1].Contents.Length > 0 && board[srowULUL1][scolULUL1].Contents[0] != '0')
                            || (board[srowULUL2][scolULUL2].Contents.Length > 0 && board[srowULUL2][scolULUL2].Contents[0] != '0')
                            || (board[srowULDR1][scolULDR1].Contents.Length > 0 && board[srowULDR1][scolULDR1].Contents[0] != '0')
                            || (board[srowULDR2][scolULDR2].Contents.Length > 0 && board[srowULDR2][scolULDR2].Contents[0] != '0')
                            // none of the check cells can be occupied at all
                            || (board[rowUL1][colUL1].Contents.Length > 0)
                            || (board[rowUL2][colUL2].Contents.Length > 0)
                        );
                        // do the same thing for down/right
                        bool drBad = (
                            // cell furthest in the down right direction needs to be on the board
                            !(srowDRDR2 < Height && scolDRDR2 < Width)
                            // none of the split cells can be occupied, unless they're explicitly empty
                            || (board[srowDRUL1][scolDRUL1].Contents.Length > 0 && board[srowDRUL1][scolDRUL1].Contents[0] != '0')
                            || (board[srowDRUL2][scolDRUL2].Contents.Length > 0 && board[srowDRUL2][scolDRUL2].Contents[0] != '0')
                            || (board[srowDRDR1][scolDRDR1].Contents.Length > 0 && board[srowDRDR1][scolDRDR1].Contents[0] != '0')
                            || (board[srowDRDR2][scolDRDR2].Contents.Length > 0 && board[srowDRDR2][scolDRDR2].Contents[0] != '0')
                            // none of the check cells can be occupied at all
                            || (board[rowDR1][colDR1].Contents.Length > 0)
                            || (board[rowDR2][colDR2].Contents.Length > 0)
                        );
                        // error check tho
                        //if (ulBad && drBad) Console.WriteLine("UH OH! ulBad && drBad in Add function!");
                        if (ulBad) board[rowDR1][colDR1].Entropy = Entropy.Anchor;
                        else if (drBad) board[rowUL1][colUL1].Entropy = Entropy.Anchor;
                        else
                        {
                            // both cells can be used as an anchor
                            board[rowUL1][colUL1].Entropy = Entropy.HalfAnchor;
                            board[rowDR1][colDR1].Entropy = Entropy.HalfAnchor;
                        }
                    }
                    // cells that want to be Floaters.
                    else if (counts[m] > 0)
                    {
                        if (!(colUL1 < 2 || rowUL1 < 2 || board[rowUL1][colUL1].Entropy == Entropy.Resolved || (board[rowUL2][colUL2].Entropy == Entropy.Resolved && board[rowUL2][colUL2].Contents[0] == '0')))
                        {
                            board[rowUL1][colUL1].Entropy = Entropy.Floater;
                        }
                        if (!(colDR1 >= Height - 2 || rowDR1 >= Height - 2 || (board[rowDR1][colDR1].Entropy == Entropy.Resolved && board[rowDR1][colDR1].Contents[0] == '0') || (board[rowDR2][colDR2].Entropy == Entropy.Resolved && board[rowDR2][colDR2].Contents[0] == '0')))
                        {
                            board[rowDR1][colDR1].Entropy = Entropy.Floater;
                        }
                    }
                    // cells that want to be Available
                    else
                    {
                        if (!(colUL1 < 2 || rowUL1 < 2 || board[rowUL1][colUL1].Entropy == Entropy.Resolved || (board[rowUL2][colUL2].Entropy == Entropy.Resolved && board[rowUL2][colUL2].Contents[0] == '0')))
                        {
                            board[rowUL1][colUL1].Entropy = Entropy.Available;
                        }
                        if (!(colDR1 >= Height - 2 || rowDR1 >= Height - 2 || (board[rowDR1][colDR1].Entropy == Entropy.Resolved && board[rowDR1][colDR1].Contents[0] == '0') || (board[rowDR2][colDR2].Entropy == Entropy.Resolved && board[rowDR2][colDR2].Contents[0] == '0')))
                        {
                            board[rowDR1][colDR1].Entropy = Entropy.Available;
                        }
                    }
                    // any cells left over stay as they are. They're either Resolved or Empty.
                }
            }
            // The Add method is "finished" here now, but I've set myself up for a hellish time when removing a wordPair, because I'd also need to undo all of its entropy changes (again, entropy reveals itself to be an inaccurate word choice here). I think a better implementation might be to have a stack of board instead of just a global array. I could also just pass it through the recursive functions instead of building my own "recursive stack." But also, I'm tired. Wait a sec... isn't that just what happens when you pass the board and board through to each function? TODO.
        }

        /// <summary>
        /// For modifying board entropy, it's less helpful to have the full anchors list to work with. Return a simplified one with all of the intersections already resolved.
        /// </summary>
        /// <param name="wordPair">WordPair with original anchors list</param>
        /// <param name="intersections">List of bools as long as the single letters in WordPair, where true represents an intersection with another WordPair on the board</param>
        /// <returns></returns>
        public List<List<bool>> GetSimplifiedAnchors(WordPair wordPair, List<bool> intersections)
        {
            // Start with a copy of anchors that you can modify
            List<List<bool>> anchors = wordPair.Anchors;
            // Also keep track of a score. 
            List<int> scores = new();
            int score;
            // Traverse through each anchor
            for (int j = 0; j < anchors.Count; j++)
            {
                score = 0;
                for (int i = 0; i < anchors[j].Count; i++)
                {
                    // If the anchor is already intersecting something, then its purpose is already fulfilled.
                    if (anchors[j][i] && intersections[i])
                    {
                        anchors[j][i] = false;
                    }
                    // We only care about anchor conditions with the lowest number of anchors (ie the lowest score). Since we might've changed that in the previou few lines, keep track of score here.
                    if (anchors[j][i])
                    {
                        score += 1;
                    }
                }
                scores.Add(score);
            }
            int minScore = scores[0];
            foreach (int s in scores)
            {
                if (s < minScore) minScore = s;
            }
            // Let simplifiedAnchors be our modified anchors list, but only the ones that are tied for the new lowest score.
            List<List<bool>> simplifiedAnchors = new();
            for (int i = 0; i < anchors.Count(); i++)
            {
                if (scores[i] == minScore)
                {
                    simplifiedAnchors.Add(anchors[i]);
                }
            }
            // Return the result
            return simplifiedAnchors;
        }

        /// <summary>
        /// Removes a word pair from the board and manages all relevant metadata
        /// </summary>
        /// <param name="wordPair">
        /// Word pair to be removed from board
        /// </param>
        public void RemoveWordPair(Cell[][] board, BoardWordPair wordPair)
        {
            // TODO Entropy??
            // Remove wordPair from LUTs
            foreach (List<int> cell in WordPairToCellsLUT[wordPair])
            {
                // Set the existing tiles to blank, unless that'd overwrite a cell that existed before the word (like via an intersection)
                if (CellsToWordPairsLUT[cell].Count == 1)
                {
                    board[cell[0]][cell[1]].Contents = "";
                }
                // when you're done, remove the WordPair from the Cells-->WordPairs LUT
                CellsToWordPairsLUT[cell].Remove(wordPair);
            }
            // when you're done all the cells, remove the WordPair from the WordPair-->Cells LUT
            WordPairToCellsLUT.Remove(wordPair);
        }

        /// <summary>
        /// Determine the validity of the Placement of a single WordPair
        /// </summary>
        /// <param name="wordPair">WordPair to be placed on the board</param>
        /// <param name="placement">Placement with which to place the WordPair on the board</param>
        /// <returns>returns true if the Placement is valid for the WordPair on the board</returns>
        public bool IsValidPlacement(Cell[][] board, WordPair wordPair, Placement placement)
        {
            // if the word starts off the board, it's definitely off the board
            if (placement.Row < 0 || placement.Col < 0 || placement.Row > Height || placement.Col > Width)
            {
                return false;
            }
            // if the end of the word goes off the board, that's also invalid
            if (placement.Dir == Orientation.Horizontal && placement.Col >= Width - wordPair.Shape.Length || placement.Dir == Orientation.Vertical && placement.Row >= Height - wordPair.Shape.Length)
            {
                return false;
            }
            // So the word pair is at least on the board. Determine whether its interactions with any of the other existing cells on the board are also valid. Traverse the WordPair one cell at a time.
            List<bool> anchorCandidates = new();
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
                    if (board[checkRow][checkCol].Entropy == Entropy.Resolved)
                    {
                        if (board[checkRow][checkCol].Contents[0] != '0')
                        {
                            // Obvious bad case. The end of this wordPair would butt up against another wordPair.
                            return false;
                        }
                        // If you're here, then the cell that has to be empty already had to be empty for some other reason. Nice!
                        continue;
                    }
                    if (board[checkRow][checkCol].Entropy == Entropy.Anchor)
                    {
                        // This cell is not populated yet, but NEEDS to be populated with a letter tile. That's a contradiction.
                        return false;
                    }
                    // If you've made it this far, you're able to place an empty tile here.
                    continue;
                }

                // If you've made it this far, you're looking at behavior inside the wordPair
                // Start with intersections with a preexisting cell
                if (board[checkRow][checkCol].Entropy == Entropy.Resolved)
                {
                    anchorCandidates.Add(true);
                    string compTile = wordPair[tile];
                    if (compTile.Length > 1)
                    {
                        // you're trying to place a double letter tile over a tile that was already filled in. That's no good.
                        return false;
                    }
                    if (compTile != board[checkRow][checkCol].Contents)
                    {
                        // you're trying to create an intersection between two letters that aren't the same. That won't work.
                        // This also applies exactly the same as if the cell is forced to be empty, ie its entropy is resolved and its value is '0'
                        return false;
                    }
                    // if an adjacent cell is an anchor or halfanchor, we also care about whether this WordPair would violate any mistakeables
                    if ((placement.Dir == Orientation.Horizontal && checkCol < Width && (board[checkRow][checkCol + 1].Entropy == Entropy.Anchor || board[checkRow][checkCol + 1].Entropy == Entropy.HalfAnchor)) || (placement.Dir == Orientation.Horizontal && checkCol > 0 && (board[checkRow][checkCol - 1].Entropy == Entropy.Anchor || board[checkRow][checkCol - 1].Entropy == Entropy.HalfAnchor))
                      || (placement.Dir == Orientation.Vertical && checkRow < Height && (board[checkRow + 1][checkCol].Entropy == Entropy.Anchor || board[checkRow + 1][checkCol].Entropy == Entropy.HalfAnchor)) || (placement.Dir == Orientation.Vertical && checkRow > 0 && (board[checkRow - 1][checkCol].Entropy == Entropy.Anchor || board[checkRow - 1][checkCol].Entropy == Entropy.HalfAnchor)))
                    {
                        // check mistakeables
                        // we know this is valid syntax. If it weren't, well, we would've hit problems way earlier
                        WordPair intersector = CellsToWordPairsLUT[new List<int>() { checkRow, checkCol }][0];
                        // now we need to find which cell in the intersector we're intersecting
                        // TODO: wait a sec... we need to know the placements too.
                        //intersector.Mistakeables
                    }
                    // if you've made it this far, then you're either trying to make an ordinary intersection, or you're going to find out very soon that you're trying to make an intersection like (ac/sa)me onto me(an/nd) by the shared 'me' (which is invalidated by the earlier rule of "there needs to be an empty cell at the start and end of each wordPair).
                    continue;
                }

                // Intersecting with a grid cell that's not resolved yet.
                // You can't place the word down next to another parallel wordPair
                // (well, technically you could if you were able to also resolve the overlaps into their own wordPairs in the future.)
                // TODO: accmmodate 'hooking' another wordPair

                // It'll also be important to look at neighboring cells...
                int checkRowUL1 = checkRow;
                int checkColUL1 = checkCol;
                int checkRowDR1 = checkRow;
                int checkColDR1 = checkCol;
                // in fact, look at enough neighboring cells that you could see if there's enough room for a double-letter split
                // explanation of this naming convention:
                // checkRow: a cell that will be part of a new wordPair, if one were to be placed here
                // splitRow: a cell that the double-letter cells spill into, so they must be empty
                // UL: cell goes in the up-or-left transverse direction (ie cell goes up if placement is horizontal, left if vertical)
                // DR: cell goes in the down-or-right tranverse direction
                // 1: cell goes 1 step in the transverse direction
                // 2: cell goes 2 steps in the transverse direction
                // split cells, in addition to extending in the transverse direction of the WordPair placement, also go one more step in the same direction. So each split cell has an additional UL / DR to indicate that
                int checkRowUL2 = checkRow;
                int checkColUL2 = checkCol;
                int checkRowDR2 = checkRow;
                int checkColDR2 = checkCol;
                int splitColULUL1 = checkCol;
                int splitColULUL2 = checkCol;
                int splitRowULUL1 = checkRow;
                int splitRowULUL2 = checkRow;
                int splitColDRDR1 = checkCol;
                int splitColDRDR2 = checkCol;
                int splitRowDRDR1 = checkRow;
                int splitRowDRDR2 = checkRow;
                int splitColULDR1 = checkCol;
                int splitColULDR2 = checkCol;
                int splitRowULDR1 = checkRow;
                int splitRowULDR2 = checkRow;
                int splitColDRUL1 = checkCol;
                int splitColDRUL2 = checkCol;
                int splitRowDRUL1 = checkRow;
                int splitRowDRUL2 = checkRow;
                if (placement.Dir == Orientation.Horizontal)
                {
                    checkColUL1 -= 1;
                    checkColUL2 -= 2;
                    checkColDR1 += 1;
                    checkColDR2 += 2;

                    splitColULUL1 -= 1;
                    splitColULUL2 -= 2;
                    splitColULDR1 -= 1;
                    splitColULDR2 -= 2;
                    splitColDRUL1 += 1;
                    splitColDRUL2 += 2;
                    splitColDRDR1 += 1;
                    splitColDRDR2 += 2;

                    splitRowULUL2 -= 1;
                    splitRowULUL1 -= 1;
                    splitRowULDR1 -= 1;
                    splitRowULDR2 -= 1;
                    splitRowDRUL1 += 1;
                    splitRowDRUL2 += 1;
                    splitRowDRDR1 += 1;
                    splitRowDRDR2 += 1;
                }
                else
                {
                    checkRowUL1 -= 1;
                    checkRowUL2 -= 2;
                    checkRowDR1 += 1;
                    checkRowDR2 += 2;

                    splitRowULUL1 -= 1;
                    splitRowULUL2 -= 2;
                    splitRowULDR1 -= 1;
                    splitRowULDR2 -= 2;
                    splitRowDRUL1 += 1;
                    splitRowDRUL2 += 2;
                    splitRowDRDR1 += 1;
                    splitRowDRDR2 += 2;

                    splitColULUL2 -= 1;
                    splitColULUL1 -= 1;
                    splitColULDR1 -= 1;
                    splitColULDR2 -= 1;
                    splitColDRUL1 += 1;
                    splitColDRUL2 += 1;
                    splitColDRDR1 += 1;
                    splitColDRDR2 += 1;
                }

                // verify that this wordPair isn't running parallel to another one that's immediately adjacent
                // checkRowUL is always < checkRow, so no need to check if it's < Height or < Width
                if (checkRowUL1 >= 0 && checkColUL1 >= 0)
                {
                    // this wordPair would run parallel to another wordPair that's immediately adjacent. Not allowed!
                    if (board[checkRowUL1][checkColUL1].Contents.Length > 0 && board[checkRowUL1][checkColUL1].Contents[0] != '0')
                    {
                        return false;
                    }
                }
                if (checkRowDR1 < Height && checkColDR1 < Width)
                {
                    // this wordPair would run parallel to another wordPair that's immediately adjacent. Not allowed!
                    if (board[checkRowDR1][checkColDR1].Contents.Length > 0 && board[checkRowDR1][checkColDR1].Contents[0] != '0')
                    {
                        return false;
                    }
                }
                // verify that there's enough space to put another word, if this cell happens to be an anchor.
                // If the cell has more than one anchor conditions, it might be fine for a given anchor cell to be invalidated. Just keep track of that.
                // Also, my technique for verifying that there's enough space to place another word takes a shortcut, reliant on the fact that I don't support hooking yet. So... TODO.
                // Check the up/left direction to see if anything's amiss
                bool ulBad = (
                    // cell furthest in the upper left direction needs to be on the board
                    !(splitRowULUL1 >= 0 && splitColULUL2 >= 0)
                    // none of the split cells can be occupied, unless they're explicitly empty
                    || (board[splitRowULUL1][splitColULUL1].Contents.Length > 0 && board[splitRowULUL1][splitColULUL1].Contents[0] != '0')
                    || (board[splitRowULUL2][splitColULUL2].Contents.Length > 0 && board[splitRowULUL2][splitColULUL2].Contents[0] != '0')
                    || (board[splitRowULDR1][splitColULDR1].Contents.Length > 0 && board[splitRowULDR1][splitColULDR1].Contents[0] != '0')
                    || (board[splitRowULDR2][splitColULDR2].Contents.Length > 0 && board[splitRowULDR2][splitColULDR2].Contents[0] != '0')
                    // none of the check cells can be occupied at all
                    || (board[checkRowUL1][checkColUL1].Contents.Length > 0)
                    || (board[checkRowUL2][checkColUL2].Contents.Length > 0)
                );
                // do the same thing for down/right
                bool drBad = (
                    // cell furthest in the down right direction needs to be on the board
                    !(splitRowDRDR2 < Height && splitColDRDR2 < Width)
                    // none of the split cells can be occupied, unless they're explicitly empty
                    || (board[splitRowDRUL1][splitColDRUL1].Contents.Length > 0 && board[splitRowDRUL1][splitColDRUL1].Contents[0] != '0')
                    || (board[splitRowDRUL2][splitColDRUL2].Contents.Length > 0 && board[splitRowDRUL2][splitColDRUL2].Contents[0] != '0')
                    || (board[splitRowDRDR1][splitColDRDR1].Contents.Length > 0 && board[splitRowDRDR1][splitColDRDR1].Contents[0] != '0')
                    || (board[splitRowDRDR2][splitColDRDR2].Contents.Length > 0 && board[splitRowDRDR2][splitColDRDR2].Contents[0] != '0')
                    // none of the check cells can be occupied at all
                    || (board[checkRowDR1][checkColDR1].Contents.Length > 0)
                    || (board[checkRowDR2][checkColDR2].Contents.Length > 0)
                );
                // this could be an anchor candidate as long as both UL and DR aren't bad
                anchorCandidates.Add(!(ulBad && drBad));
            }
            // if you've made it this far, you're able to compare anchors. At least one anchor condition must be able to be satisfied in this placement.
            bool passAny = false;
            foreach (List<bool> anchor in wordPair.Anchors)
            {
                bool passThis = true;
                for (int ai = 0; ai < anchor.Count; ai++)
                {
                    // if anchor[ai] then anchorCandidates[ai] must be true for this anchor to pass
                    if (anchor[ai] && (!anchorCandidates[ai]))
                    {
                        passThis = false;
                        break;
                    }
                }
                passAny = passAny || passThis;
                if (passAny) break;
            }
            // if any of the anchor conditions pass, you're good to go.
            if (!passAny) return false;
            // if you made it this far, this placement isn't inherently invalid. Nice!
            return true;
        }

        /// <summary>
        /// Get all the valid (or at least not-inherently-invalid) placements
        /// for a WordPair on the board such that it intersects the given cell.
        /// If the cell is not provided, this function assumes you're starting
        /// from an empty board and provides all possible starting placements.
        /// </summary>
        /// <param name="wordPair">WordPair to be placed on the board</param>
        /// <param name="cellRow">row of cell to be intersected by WordPair</param>
        /// <param name="cellCol">col of cell to be intersected by WordPair</param>
        /// <returns>all possible placements of the WordPair. Sometimes there are no possible placements, in which case this will be null</returns>
        public List<Placement>? ValidPlacements(Cell[][] board, WordPair wordPair, int cellRow=-1, int cellCol=-1)
        {
            List<Placement> placements = new();  // to be returned at the end of the function
            // If starting from a null cell or unassigned cell
            if (cellRow == -1 && cellCol == -1 || board[cellRow][cellCol].Contents.Length < 1)
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
            // Handle the remaining trivial case
            if (board[cellRow][cellCol].Contents.Length > 1 || board[cellRow][cellCol].Contents == "0")
            {
                // If the cell is already assigned to as being empty or a double-letter tile,
                // then no, there is never a valid placement here.
                return null;
            }
            
            // Start by finding any possible intersection points
            char cell = board[cellRow][cellCol].Contents[0];
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
                if (IsValidPlacement(board, wordPair, placement)) placements.Add(placement);
            }
            if (placements.Count == 0) return null;
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
        Entropy[][] board;
        string[][] board;
        int[]? tb; // Every board position, to be stored as an array in a randomly shuffled order
        int[]? tw; // Every wordPair, to be stored as an array in a randomly shuffled order
        List<WordPair> usedWordPairs;
        List<string> usedPrompts;
        List<string> usedWords;

        public boardFinder(BoardSettings settings)
        {
            // Parse settings
            MinWordLength = settings.MinWordLength;
            MaxWordLength = settings.MaxWordLength;
            Height = settings.BoardHeight;
            Width = settings.BoardWidth;
            // Make initial board, including entropy
            board = Enumerable.Repeat(Enumerable.Repeat(Entropy.Default, Width).ToArray(), Height).ToArray();
            board = Enumerable.Repeat(Enumerable.Repeat("", Width).ToArray(), Height).ToArray();
            // Make the lists for keeping track of which wordPairs/words/prompts we've used so far. We don't want any repeats.
            // Normally you'd just remove these things from the list, but since the list of Wordpairs (10k-100k) is so huge compared to the list of used WordPairs on the board (10-100), it's probably computationally cheaper just to check whether a word has been used yet. I suppose I could test both approaches sooner or later.
            usedWordPairs = new() { };
            usedPrompts = new() { };
            usedWords = new() { };
        }

        public void Resetboard()
        {
            // reset board for everything except which words/prompts/wordPairs have been used already.
            board = Enumerable.Repeat(Enumerable.Repeat(Entropy.Default, Width).ToArray(), Height).ToArray();
            board = Enumerable.Repeat(Enumerable.Repeat("", Width).ToArray(), Height).ToArray();
            tb = null;
            tw = null;
        }

        // Find one board. Even if we could make every single board during my lifetime, most of them would be duplicates except for one 3-letter WordPair. Parsing the results would be harder than just generating a new one from scratch with a different seed. 
        public string[][]? Findboard(List<WordPair> wordPairs, int seed = -1)
        {
            // setup
            // get traversal order for board (just top half) and word pairs list
            // abbreviate these because I'll be using them a lot, but Traversal Order board -> tb and Traversal Order WordPairs -> tw.
            tb = Enumerable.Range(0, (int)Math.Floor(Height * 0.5) * Width + (int)Math.Floor(Width * 0.5)).ToArray();
            tw = Enumerable.Range(0, wordPairs.Count).ToArray();
            // shuffle traversal order
            Random rng = (seed < 0) ? new() : new(seed);
            FisherYatesShuffle(rng, tb);
            FisherYatesShuffle(rng, tw);
            
            // return the results of the recursive solving algorithm
            return Solve(board);
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
            // Is the board a full, valid Split Decisions board?

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
