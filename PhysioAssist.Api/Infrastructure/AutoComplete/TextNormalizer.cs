using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PhysioAssist.Api.Infrastructure.AutoComplete
{
    // Language codes we support. Enum instead of magic strings — compiler-checked.
    public enum Language
    {
        English,
        Arabic,
        Unknown
    }

    // Static class — pure functions, no state, thread-safe by design.
    public class TextNormalizer
    {
        // ============================================================
        // ARABIC NORMALIZATION
        // ============================================================

        // Regex to strip Arabic diacritics (Tashkeel).
        // Unicode range: U+064B to U+065F covers Fatha, Damma, Kasra, Sukun, Shadda, etc.
        // U+0670 is Superscript Alif. U+06D6-U+06ED are Quranic marks.
        // Compiled = pre-JITted for repeated use — much faster than interpreted regex.
        private static readonly Regex ArabicDiacriticsRegex = new(
            @"[\u064B-\u065F\u0670\u06D6-\u06ED]",RegexOptions.Compiled);


        // Strip Tatweel (kashida) — the stretching character ـ (U+0640).
        // Purely cosmetic; must be removed for matching.
        private static readonly Regex TatweelRegex = new(
            @"\u0640",RegexOptions.Compiled);



        /// <summary>
        /// Normalize Arabic text for storage and lookup.
        /// This is the SINGLE MOST IMPORTANT function for Arabic autocomplete quality.
        /// </summary>
        public static string NormalizeArabic(string input)
        {
            if (string.IsNullOrEmpty(input)) 
                return string.Empty;

            // Step 1: Trim and remove diacritics.
            // Most Arabic text in the wild is written WITHOUT tashkeel,
            // but some dictionary sources include it. Normalize both sides.
            var s = input.Trim();
            s = ArabicDiacriticsRegex.Replace(s, string.Empty);
            s = TatweelRegex.Replace(s, string.Empty);

            // Step 2: Unify Alif variants → plain Alif (ا).
            // Users typing quickly rarely distinguish أ إ آ from ا.
            // If we don't normalize, "احمد" won't match "أحمد" in the dictionary.
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s)
            {
                var normalized = ch switch
                {
                    // Alif with Hamza above, below, Madda → plain Alif
                    'أ' or 'إ' or 'آ' or 'ٱ' => 'ا',

                    // Ya Maqsura (ى) → Ya (ي)
                    // Common confusion, especially in Egyptian/Levantine typing
                    'ى' => 'ي',

                    // Ta Marbuta (ة) → Ha (ه)
                    // Controversial: some systems preserve ة. We normalize because
                    // users frequently type either interchangeably.
                    'ة' => 'ه',

                    // Hamza on Waw (ؤ) → Waw (و)
                    'ؤ' => 'و',

                    // Hamza on Ya (ئ) → Ya (ي)
                    'ئ' => 'ي',

                    // Everything else passes through unchanged
                    _ => ch
                };
                sb.Append(normalized);
            }

            return sb.ToString();
        }



        // ============================================================
        // ENGLISH NORMALIZATION
        // ============================================================

        /// <summary>
        /// Normalize English (and other Latin-script) text.
        /// Handles accents from imported foreign medical terms (e.g., "café" → "cafe").
        /// </summary>
        public static string NormalizeEnglish(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // Trim + lowercase using invariant culture.
            // NEVER use ToLower() without a culture — Turkish "I" is a famous bug:
            // "ISTANBUL".ToLower() in tr-TR returns "ıstanbul", breaking string matches.
            var s = input.Trim().ToLowerInvariant();

            // Remove diacritics via Unicode normalization form D (decomposed).
            // "café" → "cafe" + combining acute accent → strip the combining char → "cafe"
            var normalized = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);

            foreach (var ch in normalized)
            {
                // Skip combining marks (accents, diacritics).
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(ch);
                }
            }

            // Recompose to canonical form C — safe default for storage.
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }


        // ============================================================
        // LANGUAGE DETECTION
        // ============================================================

        /// <summary>
        /// Detect language from the first meaningful character.
        /// Fast heuristic — good enough for autocomplete (we don't need ML).
        /// </summary>
        public static Language DetectLanguage(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return Language.Unknown;

            // Scan first few characters. Skip whitespace and punctuation.
            foreach (var ch in input)
            {
                if (char.IsWhiteSpace(ch) || char.IsPunctuation(ch)) continue;

                // Arabic Unicode block: U+0600 to U+06FF (main)
                //                      U+0750 to U+077F (supplement)
                //                      U+FB50 to U+FDFF (presentation forms A)
                //                      U+FE70 to U+FEFF (presentation forms B)
                if ((ch >= '\u0600' && ch <= '\u06FF') ||
                    (ch >= '\u0750' && ch <= '\u077F') ||
                    (ch >= '\uFB50' && ch <= '\uFDFF') ||
                    (ch >= '\uFE70' && ch <= '\uFEFF'))
                {
                    return Language.Arabic;
                }

                // Basic Latin (ASCII letters)
                if ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z'))
                {
                    return Language.English;
                }

                // Latin Extended (accented letters like é, ñ)
                if (ch >= '\u00C0' && ch <= '\u024F')
                {
                    return Language.English;
                }
            }

            return Language.Unknown;
        }

        /// <summary>
        /// Dispatch to the correct normalizer based on language.
        /// Single entry point used everywhere in the pipeline.
        /// </summary>
        public static string Normalize(string input, Language language) => language switch
        {
            Language.Arabic => NormalizeArabic(input),
            Language.English => NormalizeEnglish(input),
            _ => input.Trim().ToLowerInvariant()   // Fallback: minimal normalization
        };

    }
}
