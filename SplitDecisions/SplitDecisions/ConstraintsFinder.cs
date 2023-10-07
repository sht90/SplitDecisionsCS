namespace SplitDecisions
{
    internal class ConstraintsFinder
    {
        private int MinUsability;
        public ConstraintsFinder(BoardSettings settings)
        {
            MinUsability = settings.MinUsability;
        }

        public List<WordPair> FindConstraints(List<WordPair> wordPairs)
        {
            // this function will populate and return this list
            List<WordPair> retList = new() { };
            // traverse prompts (since wordPairs are already sorted, don't need to make a separate nested list)
            int promptStartIndex = 0;
            int promptEndIndex = -1;
            for (int i = 0; i < wordPairs.Count; i++)
            {
                WordPair wordPair = wordPairs[i];
                // set start and end of prompt, if you're just starting a new prompt
                if (i > promptEndIndex)
                {
                    promptStartIndex = i;
                    promptEndIndex = wordPairs.Count;
                    for (int j = i + 1; j < wordPairs.Count; j++)
                    {
                        if (wordPairs[j].GetPrompt() != wordPair.GetPrompt())
                        {
                            promptEndIndex = j - 1;
                            break;
                        }
                    }
                }
                // board constraints only matter for usable words
                if (wordPair.Usability < this.MinUsability) { continue; }
                // get mistakeables: all the other possibile valid letters for the same prompt
                wordPairs[i].Mistakeables = Enumerable.Repeat(0, wordPair.Letters.Length).ToList();
                for (int k = promptStartIndex; k <= promptEndIndex; k++)
                {
                    if (k == i) { continue; }
                    WordPair other = wordPairs[k];
                    for (int l = 0; l < wordPair.Letters.Length; l++)
                    {
                        if (wordPair.Letters[l] != other.Letters[l])
                        {
                            wordPairs[i].Mistakeables[l] |= LetterCode.Encode(other.Letters[l]);
                        }
                    }
                }
                // Get anchors. Each anchor is a set of indices where, if correctly filled in, guarantees that there is only one unique solution to a WordPair
                // Initialize anchors
                wordPairs[i].Anchors = new List<List<bool>>() { };
                // Some WordPairs are inherently constrained by their prompt.
                // Handle the easy edge case, then break.
                if (promptStartIndex == promptEndIndex)
                {
                    wordPairs[i].Anchors.Add(Enumerable.Repeat(false, wordPair.Letters.Length).ToList());
                    retList.Add(wordPair);
                    continue;
                }
                // Now anchors are nontrivial.
                // Determine which indexes we want to look at when comparing wordPair to others with the same prompt
                for (int size = 1; size <= wordPair.Letters.Length; size++)
                {
                    // Traverse all possible combinations of indexes
                    List<List<int>> indexCombos = GetIndexCombos(Enumerable.Range(0, wordPair.Letters.Length).ToList(), size);
                    foreach (List<int> indexCombo in indexCombos)
                    {
                        // Check if this combination of indexes constrains the wordPair
                        // ie check if solving these letters reduces the number of possible solutions to exactly 1
                        // constrainsAll monitors whether the indexCombo collectively constrains the wordPair relative to the entire prompt
                        bool constrainsAll = true;
                        for (int k = promptStartIndex; k <= promptEndIndex; k++)
                        {
                            if (k == i) { continue; }
                            // constrains monitors whether the indexCombo constrains the wordPair relative to other
                            bool constrains = false;
                            WordPair other = wordPairs[k];
                            foreach (int index in indexCombo)
                            {
                                // it only takes one of the indexes to have a different letter for it to be a constraining combination
                                constrains = constrains || (wordPair.Letters[index] != other.Letters[index]);
                            }
                            if (!constrains)
                            {
                                constrainsAll = false;
                                break;
                            }
                        }
                        if (constrainsAll)
                        {
                            // If you made it here, you found an index combination that constrians the wordPair. These will be a set of possible anchor points for putting this wordPair on the board.
                            List<bool> indexComboBool = Enumerable.Repeat(false, wordPair.Letters.Length).ToList();
                            foreach (int index in indexCombo) { indexComboBool[index] = true; }
                            wordPairs[i].Anchors.Add(indexComboBool);
                        }
                        // keep going in this loop, in case there are other index combos tied for the same size
                    }
                    // if you found one or more anchors at the current size, exit the size loop
                    if (wordPairs[i].Anchors.Count > 0) { break; }
                }
                // it should not possible to exit the previous loop without finding a constraint condition.
                if (wordPairs[i].Anchors.Count == 0)
                {
                    Console.WriteLine(wordPair.ToString());
                    throw new Exception("no anchors found -- should be impossible");
                }
                retList.Add(wordPair);
            }
            return retList;
        }

        private List<List<int>> GetIndexCombos(List<int> startingItems, int size)
        {
            List<List<int>> listOfLists = new() { };
            GetIndexCombosHelper(startingItems, 0, size, listOfLists);
            return listOfLists;
        }

        private void GetIndexCombosHelper(List<int> items, int i, int r, List<List<int>> lol)
        {
            if (r == 0)
            {
                List<int> shallowCopyItems = new() { };
                for (int j = 0; j < i; j++)
                {
                    if (j > 0 && items[j] < items[j - 1]) { return; } // copout?
                    shallowCopyItems.Add(items[j]);
                }
                lol.Add(shallowCopyItems);
                return;
            }
            for (int j = i; j < items.Count; j++)
            {
                (items[i], items[j]) = (items[j], items[i]);
                GetIndexCombosHelper(items, i + 1, r - 1, lol);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }
    }
}
