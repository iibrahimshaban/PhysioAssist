namespace PhysioAssist.Api.Infrastructure.AutoComplete
{
    public class TrieNode
    {
        /* Using Dictionary for Unicode support (medical terms may have accents)
         * Dictionary maps a character to the child node representing that character.
         * Why Dictionary vs. array[26]?
         * - Medical terms contain spaces ("rotator cuff"), hyphens ("anti-inflammatory"), apostrophes ("Achilles'"), digits, and sometimes Unicode (é, ñ).
         * - Array[26] only fits English lowercase letters — insufficient here.
         * - Dictionary has slight memory overhead but flexibility wins for medical data.*/
        public Dictionary<char, TrieNode> Children { get; } = new();


        /*Marks whether this node represents the end of a valid, complete word.
         * Example: For "quad" and "quadriceps", BOTH the 'd' after "qua" AND the 's' 
         * after "quadricep" have IsWordEnd=true. Intermediate letters do not.*/
        public bool IsWordEnd { get; set; }


        /* The full term stored at the end node (preserves original casing)
         * Stores the original term (preserving casing like "ACL" or "McMurray").
         * We normalize to lowercase for LOOKUP, but store the display form here.
         * Nullable because non-terminal nodes have no term.*/
        public string? DisplayTerm { get; set; }


        // The NORMALIZED term (lowercase, no diacritics, unified Alif, etc.)
        // Kept for debugging and duplicate-detection during insert.
        public string? NormalizedTerm { get; set; }


        /* Base frequency/priority from the seed dataset
         * In personalization, we'll combine this with per-user scores.*/
        public int BaseWeight { get; set; }


        /*Semantic tag: "muscle", "diagnosis", "modality", etc.
         * Enables UI grouping ("Show category badges") and future context-aware ranking.*/
        public string? Category { get; set; }


        // language tag on the node itself, useful for mixed-language tries too.
        public Language Language { get; set; } = Language.Unknown;
    }
}
