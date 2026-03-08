using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public static class Rules
{
    public static int RV(Rank r) => (int)r;
    public static int SP(Suit s) => s switch { Suit.Spades => 0, Suit.Clubs => 1, Suit.Diamonds => 2, _ => 3 };

    public static Suit ParseSuit(string s)
    {
        switch (s.ToLower())
        {
            case "spades": return Suit.Spades;
            case "clubs": return Suit.Clubs;
            case "diamonds": return Suit.Diamonds;
            case "hearts": return Suit.Hearts;
            default: return Suit.Hearts;
        }
    }

    public static int ParseRank(string r)
    {
        switch (r.ToLower())
        {
            case "jack": return 11;
            case "queen": return 12;
            case "king": return 13;
            case "ace": return 14;
            case "2": return 15;
            default:
                int val;
                return int.TryParse(r, out val) ? val : 0;
        }
    }

    public static string PlayTypeToString(PlayType type)
    {
        return type switch
        {
            PlayType.Single => "Single",
            PlayType.Pair => "Pair",
            PlayType.Run => "Run",
            PlayType.SuitedRun => "Suited Run",
            PlayType.Set => "Set",
            PlayType.PairedRun => "Paired Run",
            PlayType.Bomb => "Bomb",
            _ => "None",
        };
    }

    // Build a Play object from a list of cards for the player
    public static Play BuildPlay(List<Card> cards)
    {
        Play play = null;
        if (cards == null || cards.Count == 0)
        {
            Debug.Log("[Rules.BuildPlay] EXIT - null/empty cards");
            return null;
        }
        var sorted = SortCards(cards);
        int count = sorted.Count;

        // Use lastIndex instead of .Last() for WebGL compatibility
        int lastIndex = count - 1;
        
        if (lastIndex < 0 || lastIndex >= sorted.Count)
        {
            Debug.LogError($"[Rules.BuildPlay] ERROR - Invalid lastIndex: {lastIndex}, sorted.Count: {sorted.Count}");
            return null;
        }
        
        // SINGLE
        if (count == 1)
        {
            play = new Play(PlayType.Single, sorted, sorted[lastIndex].rank);
        }

        // PAIR
        if (count == 2 && IsSameRank(sorted))
            play = new Play(PlayType.Pair, sorted, sorted[lastIndex].rank);

        // SET / BOMB
        if (count >= 3 && IsSameRank(sorted))
        {
            // Set
            if (count == 3)
            {
                play = new Play(PlayType.Set, sorted, sorted[lastIndex].rank);
            } // Bomb
            else if (count == 4)
            {
                play = new Play(PlayType.Bomb, sorted, sorted[lastIndex].rank + 20);
            }
        }

        // RUNS (3 or more sequential ranks)
        if (count >= 3 && IsSequential(sorted, out bool suited))
        {
            if (suited)
            {
                play = new Play(PlayType.SuitedRun, sorted, sorted[lastIndex].rank);
            }
            else
            {
                play = new Play(PlayType.Run, sorted, sorted[lastIndex].rank);
            }

        }

        // PAIRED RUNS
        if (IsPairedRun(sorted))
        {
            play = new Play(PlayType.PairedRun, sorted, sorted[lastIndex].rank);
        }

        return play;
    }

    public static List<Card> SortCards(List<Card> cards)
    {
        if (cards == null)
        {
            Debug.LogError("[Rules.SortCards] ERROR - cards is null");
            return new List<Card>();
        }
        try
        {
            var sorted = cards.OrderBy(c => c.rank).ThenByDescending(c => SP(c.suit)).ToList();
            return sorted;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Rules.SortCards] ERROR: {ex.Message}");
            return new List<Card>();
        }
    }

    // Generate all possible plays from a given hand for CPU player
    public static List<Play> GetAllPossiblePlays(List<Card> hand)
    {
        if (hand == null)
        {
            Debug.LogError("[Rules.GetAllPossiblePlays] ERROR - hand is null");
            return new List<Play>();
        }
        List<Play> plays = new List<Play>();
        hand = SortCards(hand);

        // Runs or suited runs
        try
        {
            plays.AddRange(GetRunPlays(hand));
            Debug.Log($"[Rules.GetAllPossiblePlays] GetRunPlays returned {GetRunPlays(hand).Count} plays");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Rules.GetAllPossiblePlays] ERROR in GetRunPlays: {ex.Message}");
        }

        // Paired Runs
        try
        {
            var pairedRuns = GetPairedRunPlays(hand);
            plays.AddRange(pairedRuns);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Rules.GetAllPossiblePlays] ERROR in GetPairedRunPlays: {ex.Message}");
        }

        // Singles
        foreach (var c in hand)
            plays.Add(new Play(PlayType.Single, new List<Card> { c }, c.rank));

        // Pairs
        try
        {
            var groups = hand.GroupBy(c => c.rank).Where(g => g.Count() >= 2).ToList();
            foreach (var g in groups)
            {
                int count = g.Count();
                var cards = g.ToList();

                // Use index instead of .Last() for WebGL compatibility
                int cardLastIndex = cards.Count - 1;

                if (count == 2)
                {
                    plays.Add(new Play(PlayType.Pair, cards.Take(2).ToList(), cards[cardLastIndex].rank));
                }
                if (count == 3)
                {
                    plays.Add(new Play(PlayType.Set, cards, cards[cardLastIndex].rank));
                }
                if (count == 4)
                {
                    plays.Add(new Play(PlayType.Bomb, cards, cards[cardLastIndex].rank + 20));
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Rules.GetAllPossiblePlays] ERROR processing groups: {ex.Message}");
        }

        return plays;
    }

    private static List<Play> GetPairedRunPlays(List<Card> hand)
    {
        if (hand == null || hand.Count == 0)
        {
            Debug.Log("[Rules.GetPairedRunPlays] EXIT - null/empty hand");
            return new List<Play>();
        }
        List<Play> runs = new();

        try
        {
            var groups = hand.GroupBy(c => c.rank).OrderBy(g => g.Key).ToList();
            var dict = groups.ToDictionary(g => g.Key, g => g.ToList());
            var pairRanks = groups.Where(g => g.Count() >= 2).Select(g => g.Key).ToList();

            for (int i = 0; i < pairRanks.Count; i++)
            {
                int j = i;
                while (j + 1 < pairRanks.Count && pairRanks[j + 1] == pairRanks[j] + 1) j++;

                int len = j - i + 1;
                if (len >= 3)
                {
                    for (int size = 3; size <= len; size++)
                        for (int start = i; start <= j - size + 1; start++)
                        {
                            int targetIndex = start + size - 1;
                            
                            if (targetIndex >= pairRanks.Count)
                            {
                                Debug.LogError($"[Rules.GetPairedRunPlays] ERROR - Index out of bounds: {targetIndex} >= {pairRanks.Count}");
                                continue;
                            }
                            
                            try
                            {
                                var rankKeys = pairRanks.Skip(start).Take(size).ToList();
                                
                                var cards = rankKeys.SelectMany(r =>
                                {
                                    if (!dict.ContainsKey(r))
                                    {
                                        Debug.LogError($"[Rules.GetPairedRunPlays] ERROR - Key {r} not in dictionary");
                                        return new List<Card>();
                                    }
                                    return dict[r].Take(2);
                                }).ToList();
                                
                                int rankValue = pairRanks[targetIndex];
                                runs.Add(new Play(PlayType.PairedRun, cards, rankValue));
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogError($"[Rules.GetPairedRunPlays] ERROR creating PairedRun: {ex.Message}");
                            }
                        }
                }
                i = j;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Rules.GetPairedRunPlays] ERROR: {ex.Message}\n{ex.StackTrace}");
        }

        return runs;
    }

    private static List<Play> GetRunPlays(List<Card> sorted)
    {
        if (sorted == null || sorted.Count == 0)
        {
            Debug.Log("[Rules.GetRunPlays] EXIT - null/empty sorted");
            return new List<Play>();
        }
        List<Play> runPlays = new List<Play>();
        // Sort cards by rank, then suit (for stability)
        for (int i = 0; i < sorted.Count; i++)
        {
            List<Card> currentRun = new() { sorted[i] };

            for (int j = i + 1; j < sorted.Count; j++)
            {
                // check if next card rank increases by 1 from the last card in currentRun
                // Use index instead of .Last() for WebGL compatibility
                int currentRunLastIndex = currentRun.Count - 1;
                if (sorted[j].rank == currentRun[currentRunLastIndex].rank + 1)
                {
                    currentRun.Add(sorted[j]);
                }
                else
                {
                    // not consecutive → stop this run
                    break;
                }
            }

            // If we found 3+ consecutive cards, make a run Play
            if (currentRun.Count >= 3)
            {
                // Use Count - 1 instead of [^1] for WebGL compatibility
                int lastIndex = currentRun.Count - 1;
                int lastRank = currentRun[lastIndex].rank;
                
                // Normal run
                runPlays.Add(new Play(PlayType.Run, new List<Card>(currentRun), lastRank));

                // Check if all cards share the same suit → Suited Run
                bool suited = currentRun.All(c => c.suit == currentRun[0].suit);
                if (suited)
                {
                    runPlays.Add(new Play(PlayType.SuitedRun, new List<Card>(currentRun), lastRank));
                }
            }
        }

        // Optional: remove duplicates (same rank/suit combination)
        try
        {
            runPlays = runPlays.DistinctBy(p => new { p.Type, Key = string.Join(",", p.Cards.Select(c => $"{c.suit}-{c.rank}")) }).ToList();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Rules.GetRunPlays] ERROR removing duplicates: {ex.Message}");
        }

        return runPlays;
    }


    /// Determines if a new play can legally match and beat the previous play.
    /// Follows the game rules: special cases first, then type/length matching, then tiebreakers, finally strength comparison.
    public static bool CanMatchAndBeat(Play newPlay, Play previousPlay)
    {
        // Null check: cannot play null
        if (newPlay == null)
        {
            Debug.Log("[Rules.CanMatchAndBeat] EXIT - newPlay is null");
            return false;
        }
        
        if (newPlay.Cards == null)
        {
            Debug.LogError("[Rules.CanMatchAndBeat] ERROR - newPlay.Cards is null");
            return false;
        }

        // New round: if no previous play, any play is valid
        if (previousPlay == null)
        {
            Debug.Log("[Rules.CanMatchAndBeat] EXIT - previousPlay is null, returning true");
            return true;
        }
        
        if (previousPlay.Cards == null)
        {
            Debug.LogError("[Rules.CanMatchAndBeat] ERROR - previousPlay.Cards is null");
            return false;
        }

        // SPECIAL CASES: These override normal rules

        // Bomb (4 of a kind) beats all plays except another Bomb
        if (newPlay.Type == PlayType.Bomb && previousPlay.Type != PlayType.Bomb)
            return true;

        // PairedRun can beat a single deuce (2)
        if (newPlay.Type == PlayType.PairedRun && IsDeuce(previousPlay))
            return true;

        // PairedRun with more than 6 cards can beat a pair of deuces (two 2s)
        if (newPlay.Type == PlayType.PairedRun && newPlay.Cards.Count > 6 && IsPairDeuce(previousPlay))
            return true;

        // SuitedRun beats a regular Run of the same length
        if (newPlay.Type == PlayType.SuitedRun && previousPlay.Type == PlayType.Run
            && previousPlay.Cards.Count == newPlay.Cards.Count && newPlay.Strength >= previousPlay.Strength)
        {
            return true;
        }

        // TYPE MATCHING: Types must match to compare

        // If types don't match, cannot beat (except for special cases above)
        if (newPlay.Type != previousPlay.Type)
            return false;

        // From here on, types must match

        // LENGTH MATCHING: For sequential plays, lengths must match

        // For Runs, SuitedRuns, and PairedRuns, the number of cards must match
        if ((previousPlay.Type == PlayType.Run || previousPlay.Type == PlayType.SuitedRun || previousPlay.Type == PlayType.PairedRun) &&
            previousPlay.Cards.Count != newPlay.Cards.Count)
            return false;

        // From here on, types and lengths match in Run and SuitedRun

        // TIEBREAKERS: When strength is equal, use suit comparison

        // Run tiebreaker: If strength is equal and last card rank > 9 (Jack, Queen, King, Ace, 2),
        // compare suits of the last cards (Spades=0 < Clubs=1 < Diamonds=2 < Hearts=3)
        if (newPlay.Type == PlayType.Run &&
            newPlay.Strength == previousPlay.Strength)
        {
            if (newPlay.Cards.Count == 0 || previousPlay.Cards.Count == 0)
            {
                Debug.LogError($"[Rules.CanMatchAndBeat] ERROR - Empty Cards: newPlay={newPlay.Cards.Count}, previousPlay={previousPlay.Cards.Count}");
                return false;
            }
            int newLastIndex = newPlay.Cards.Count - 1;
            int prevLastIndex = previousPlay.Cards.Count - 1;
            if (newPlay.Cards[newLastIndex].rank > 9)
            {
                return SP(newPlay.Cards[newLastIndex].suit) > SP(previousPlay.Cards[prevLastIndex].suit);
            }
        }

        // SuitedRun tiebreaker: Same as Run - compare suits of last cards when strength is equal and rank > 9
        if (newPlay.Type == PlayType.SuitedRun 
            && newPlay.Strength == previousPlay.Strength)
        {
            if (newPlay.Cards.Count == 0 || previousPlay.Cards.Count == 0)
            {
                Debug.LogError($"[Rules.CanMatchAndBeat] ERROR - Empty Cards: newPlay={newPlay.Cards.Count}, previousPlay={previousPlay.Cards.Count}");
                return false;
            }
            int newLastIndex = newPlay.Cards.Count - 1;
            int prevLastIndex = previousPlay.Cards.Count - 1;
            if (newPlay.Cards[newLastIndex].rank > 9)
            {
                return SP(newPlay.Cards[newLastIndex].suit) > SP(previousPlay.Cards[prevLastIndex].suit);
            }
        }

        // Single tiebreaker: If ranks are equal and above 9, compare suits
        // (For Singles, strength equals rank)
        if (newPlay.Type == PlayType.Single)
        {
            if (newPlay.Cards.Count == 0 || previousPlay.Cards.Count == 0)
            {
                Debug.LogError($"[Rules.CanMatchAndBeat] ERROR - Empty Cards: newPlay={newPlay.Cards.Count}, previousPlay={previousPlay.Cards.Count}");
                return false;
            }
            Card newCard = newPlay.Cards[0];
            Card prevCard = previousPlay.Cards[0];
            if (newCard == null || prevCard == null)
            {
                Debug.LogError("[Rules.CanMatchAndBeat] ERROR - Card is null");
                return false;
            }
            // If ranks are equal and above 9, compare suits
            if (newCard.rank > 9 && newPlay.Strength == previousPlay.Strength)
            {
                return SP(newCard.suit) > SP(prevCard.suit);
            }
        }

        // Pair tiebreaker: Red pairs (Hearts/Diamonds) beat black pairs (Spades/Clubs)
        // when strength is equal and strength > 9
        if (newPlay.Type == PlayType.Pair)
        {
            if (newPlay.Strength > 9 && newPlay.Strength == previousPlay.Strength)
            {
                if (newPlay.Cards.Count == 0 || previousPlay.Cards.Count == 0)
                {
                    Debug.LogError($"[Rules.CanMatchAndBeat] ERROR - Empty Cards: newPlay={newPlay.Cards.Count}, previousPlay={previousPlay.Cards.Count}");
                    return false;
                }
                try
                {
                    Card prevHigherCard = previousPlay.Cards.OrderByDescending(c => SP(c.suit)).FirstOrDefault();
                    Card newHigherCard = newPlay.Cards.OrderByDescending(c => SP(c.suit)).FirstOrDefault();
                    if (prevHigherCard == null || newHigherCard == null)
                    {
                        Debug.LogError("[Rules.CanMatchAndBeat] ERROR - Higher card is null");
                        return false;
                    }
                    return SP(newHigherCard.suit) > SP(prevHigherCard.suit);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Rules.CanMatchAndBeat] ERROR in Pair tiebreaker: {ex.Message}");
                    return false;
                }
            }
        }

        // FINAL COMPARISON: Compare play strength

        // If no tiebreaker applies, compare strength (newPlay must be >= previousPlay to beat)
        bool result = newPlay.Strength >= previousPlay.Strength;
        Debug.Log($"[Rules.CanMatchAndBeat] EXIT - result: {result}");
        return result;
    }

    public static Play GetFirstPlay(List<Play> possiblePlays)
    {
        if (possiblePlays == null || possiblePlays.Count == 0)
        {
            Debug.Log("[Rules.GetFirstPlay] EXIT - null/empty possiblePlays");
            return null;
        }
        // Prioritize plays that contain 3 of Spades
        List<Play> firstPlayList;
        try
        {
            firstPlayList = possiblePlays.Where(p => 
            {
                if (p == null || p.Cards == null)
                {
                    Debug.LogWarning("[Rules.GetFirstPlay] WARNING - null play or Cards in filter");
                    return false;
                }
                return p.Cards.Any(c => c != null && c.suit == Suit.Spades && c.rank == 3);
            }).ToList();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Rules.GetFirstPlay] ERROR filtering plays: {ex.Message}");
            return null;
        }
        
        // Check all play types in priority order
        Play firstPlay = firstPlayList.FirstOrDefault(p => p.Type == PlayType.Run)
            ?? firstPlayList.FirstOrDefault(p => p.Type == PlayType.SuitedRun)
            ?? firstPlayList.FirstOrDefault(p => p.Type == PlayType.PairedRun)
            ?? firstPlayList.FirstOrDefault(p => p.Type == PlayType.Pair)
            ?? firstPlayList.FirstOrDefault(p => p.Type == PlayType.Set)
            ?? firstPlayList.FirstOrDefault(p => p.Type == PlayType.Single)
            ?? firstPlayList.FirstOrDefault(p => p.Type == PlayType.Bomb);

        return firstPlay;
    }

    // Select the best play based on CPU difficulty
    public static Play GetBestPlay(CPUDifficulty difficulty, List<Play> possiblePlays)
    {
        if (possiblePlays == null || possiblePlays.Count == 0)
        {
            Debug.Log("[Rules.GetBestPlay] EXIT - null/empty possiblePlays");
            return null;
        }
        Play bestPlay = null;
        if (difficulty == CPUDifficulty.Hard) // Hard difficulty
        {
            // Prioritize single, then runs, then pairs
            bestPlay = possiblePlays.FirstOrDefault(p => p.Type == PlayType.Run)
                ?? possiblePlays.FirstOrDefault(p => p.Type == PlayType.PairedRun)
                ?? possiblePlays.FirstOrDefault(p => p.Type == PlayType.Set)
                ?? possiblePlays.FirstOrDefault(p => p.Type == PlayType.Pair)
                ?? possiblePlays.OrderBy(p => p.Type).FirstOrDefault();
            return bestPlay;
        }
        else // Normal difficulty
        {
            // Prioritize single, then pairs, then runs
            bestPlay = possiblePlays.FirstOrDefault(p => p.Type == PlayType.Pair)
                ?? possiblePlays.FirstOrDefault(p => p.Type == PlayType.Set)
                ?? possiblePlays.FirstOrDefault(p => p.Type == PlayType.Run)
                ?? possiblePlays.OrderBy(p => p.Type).FirstOrDefault();

        }
        return bestPlay;
    }

    // Selects a play from hand based on game state (FirstTrick, empty table, or beating current play)
    public static Play SelectPlayFromHand(List<Card> hand, GameState state, CPUDifficulty difficulty)
    {
        if (hand == null || hand.Count == 0)
        {
            Debug.Log("[Rules.SelectPlayFromHand] EXIT - null/empty hand");
            return null;
        }
        
        if (state == null)
        {
            Debug.LogError("[Rules.SelectPlayFromHand] ERROR - state is null");
            return null;
        }

        var possiblePlays = GetAllPossiblePlays(hand);
        if (possiblePlays == null || possiblePlays.Count == 0)
        {
            Debug.Log("[Rules.SelectPlayFromHand] EXIT - no possible plays");
            return null;
        }

        Play selected = null;

        if (state.FirstTrick)
        {
            selected = GetFirstPlay(possiblePlays);
        }
        else if (state.CurrentTopPlay == null)
        {
            selected = GetBestPlay(difficulty, possiblePlays);
        }
        else
        {
            // Find plays that can beat the current top play
            try
            {
                List<Play> beatList = possiblePlays.Where(p =>
                {
                    if (p == null || p.Cards == null)
                    {
                        Debug.LogWarning("[Rules.SelectPlayFromHand] WARNING - null play or Cards in beatList filter");
                        return false;
                    }
                    return CanMatchAndBeat(p, state.CurrentTopPlay);
                }).ToList();
                if (beatList.Count != 0)
                {
                    selected = beatList.OrderBy(b => b.Strength).FirstOrDefault();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Rules.SelectPlayFromHand] ERROR filtering beatList: {ex.Message}");
            }
        }

        return selected;
    }

    // Check if newPlay can legally match and beat previousPlay
   
    private static bool IsDeuce(Play play)
    {
        return play.Cards.Count == 1 && play.Cards[0].rank == (int)Rank.Two;
    }

    private static bool IsPairDeuce(Play play)
    {
        return play.Cards.Count == 2 && play.Cards.All(c => c.rank == (int)Rank.Two);
    }

    private static bool IsSameRank(List<Card> cards)
    {
        if (cards == null || cards.Count == 0)
        {
            Debug.Log("[Rules.IsSameRank] EXIT - false (null/empty)");
            return false;
        }
        try
        {
            bool result = cards.All(c => c != null && c.rank == cards[0].rank);
            return result;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Rules.IsSameRank] ERROR: {ex.Message}");
            return false;
        }
    }

    private static bool IsSequential(List<Card> cards, out bool suited)
    {
        suited = false;
        if (cards == null || cards.Count == 0)
        {
            Debug.Log("[Rules.IsSequential] EXIT - false (null/empty)");
            return false;
        }
        try
        {
            suited = cards.All(c => c != null && c.suit == cards[0].suit);

            for (int i = 1; i < cards.Count; i++)
            {
                if (cards[i] == null || cards[i - 1] == null)
                {
                    Debug.LogError($"[Rules.IsSequential] ERROR - null card at index {i} or {i-1}");
                    return false;
                }
                if (cards[i].rank != cards[i - 1].rank + 1)
                {
                    return false;
                }
            }
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Rules.IsSequential] ERROR: {ex.Message}");
            return false;
        }
    }

    private static bool IsPairedRun(List<Card> sorted)
    {
        if (sorted == null)
        {
            return false;
        }
        // Must have at least 6 cards (3 pairs) and an even number of cards
        if (sorted.Count < 6 || sorted.Count % 2 != 0)
        {
            Debug.Log($"[Rules.IsPairedRun] EXIT - false (count: {sorted.Count})");
            return false;
        }

        try
        {
            // Check that every two cards form a pair (same rank)
            for (int i = 0; i < sorted.Count; i += 2)
            {
                if (sorted[i] == null || sorted[i + 1] == null)
                {
                    Debug.LogError($"[Rules.IsPairedRun] ERROR - null card at index {i} or {i+1}");
                    return false;
                }
                if (sorted[i].rank != sorted[i + 1].rank)
                {
                    Debug.Log($"[Rules.IsPairedRun] EXIT - false (not pairs at index {i})");
                    return false;
                }
            }

            // Check that pairs are consecutive in rank
            for (int i = 2; i < sorted.Count; i += 2)
            {
                if (sorted[i] == null || sorted[i - 2] == null)
                {
                    Debug.LogError($"[Rules.IsPairedRun] ERROR - null card at index {i} or {i-2}");
                    return false;
                }
                if (sorted[i].rank != sorted[i - 2].rank + 1)
                {
                    Debug.Log($"[Rules.IsPairedRun] EXIT - false (not consecutive at index {i})");
                    return false;
                }
            }

            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Rules.IsPairedRun] ERROR: {ex.Message}");
            return false;
        }
    }

}
