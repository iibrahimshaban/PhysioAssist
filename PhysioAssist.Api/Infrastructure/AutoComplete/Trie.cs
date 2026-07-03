namespace PhysioAssist.Api.Infrastructure.AutoComplete
{
    /*
     * A Trie pronounced "try" from retrieval is a tree where each node represents a single character. 
     * Words sharing a prefix share the same path from the root
     * 
     * 
     * Visualization of what your Trie looks like after inserting (quad, quadriceps, quadratus):
     *  root
         │
         q
         │
         u
         │
         a
         │
         d ← IsWordEnd=true, Term="quad"
        / \
       r   ...
       │
       i / a
       │   │
       c   t
       │   │
       e   u
       │   │
       p   s ← IsWordEnd=true, Term="quadratus"
       │
       s ← IsWordEnd=true, Term="quadriceps"
     */


    public class Trie
    {
        // The root is an empty sentinel node. It never represents a character —
        // its children are the FIRST characters of all inserted terms.
        private readonly TrieNode _root = new();

        // Track how many distinct terms exist.
        private int _wordCount;

        // Public of how many distinct terms.
        public int Count => _wordCount;

        public void Insert(string term, int baseWeight = 1, string? category = null)
        {
            if (string.IsNullOrWhiteSpace(term)) return;


            // Normalize for INSERTION path (lowercase, trimmed).
            // This ensures "Quadriceps", "quadriceps", and " QUADRICEPS " all
            // land at the same trie node — case-insensitive looku
            var normalized = term.Trim().ToLowerInvariant();

            // Start at root, walk down the tree letter by letter.
            var node = _root;

            foreach (var ch in normalized)
            {
                // TryGetValue avoids double lookup (Contains + [] indexer).
                // get if exists, else create.
                if (!node.Children.TryGetValue(ch, out var next))
                {
                    // Character has no path yet — create the node.
                    next = new TrieNode();
                    node.Children[ch] = next;
                }
                // Descend into the child node for the next iteration.
                node = next;
            }

            // We've walked to the node representing the final character of `term`.
            // Only increment count if this is a NEW word (avoids double-counting duplicates).
            if (!node.IsWordEnd) _wordCount++;

            
            node.IsWordEnd = true; // Mark this node as a valid word terminator.
            node.Term = term.Trim(); // Store original (untrimmed only) casing for display back to the user.

            // If the term was inserted twice with different weights, keep the higher one.
            // (Alternative: sum them — depends on your semantics. Max is safer.)
            node.BaseWeight = Math.Max(node.BaseWeight, baseWeight);

            // Store category metadata (last write wins if duplicated).
            node.Category = category;
        }

        /// <summary>
        /// Returns all terms matching the given prefix, ordered by weight desc.
        /// </summary>
        public IReadOnlyList<TrieMatch> Search(string prefix, int limit = 10)
        {
            // Defensive: reject invalid input. Returning Array.Empty avoids allocations.
            if (string.IsNullOrWhiteSpace(prefix) || limit <= 0)
                return Array.Empty<TrieMatch>();


            // Normalize prefix the same way we normalized during insert.
            // Critical: if you insert lowercase but search uppercase without normalizing,
            // you get zero results — a very common bug.
            var normalized = prefix.Trim().ToLowerInvariant();
            var node = _root;

            // Walk down the trie following the prefix characters.
            foreach (var ch in normalized)
            {
                // If any character has no matching child, the prefix doesn't exist.
                // Bail out immediately — no results possible.
                if (!node.Children.TryGetValue(ch, out var next))
                    return Array.Empty<TrieMatch>();
                node = next;
            }

            // Now `node` sits at the end of the prefix path.
            // Everything in its subtree is a valid completion of that prefix.
            var results = new List<TrieMatch>();
            CollectTerms(node, results);

            // Sort by weight (higher = more important), then alphabetically as tiebreaker.
            // .Take(limit) at the end caps result set size.
            // NOTE: For huge subtrees, a min-heap would be O(N log K) instead of O(N log N).
            //       Optimize later if profiling shows this as a bottleneck.
            // TO-DO: Optimize later if profiling shows this as a bottleneck.
            return results.OrderByDescending(r => r.BaseWeight)
                          .ThenBy(r => r.Term, StringComparer.OrdinalIgnoreCase)
                          .Take(limit)
                          .ToList();
        }

        // Recursive DFS to collect every word terminator under a subtree.
        // Marked static because it doesn't need instance state — cleaner and slightly faster.
        private static void CollectTerms(TrieNode node, List<TrieMatch> results)
        {
            // If this node marks a word, add it to results.
            if (node.IsWordEnd && node.Term is not null)
            {
                results.Add(new TrieMatch(node.Term, node.BaseWeight, node.Category));
            }

            // Recurse into every child. In a well-balanced trie this is very fast.
            // Worst case is when a single prefix has thousands of completions —
            // in that case we should add early termination once we have enough candidates.
            // TO-DO: add early termination once we have enough candidates. 
            foreach (var child in node.Children.Values)
            {
                CollectTerms(child, results);
            }
        }
    }

    // `readonly record struct` = value type (no heap allocation) + immutable + auto equality.
    // Perfect for lightweight data carriers passed between methods.
    public readonly record struct TrieMatch(string Term, int BaseWeight, string? Category);
}
