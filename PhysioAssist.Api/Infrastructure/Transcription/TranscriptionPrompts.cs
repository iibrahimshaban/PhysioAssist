namespace PhysioAssist.Api.Infrastructure.Transcription;

public static class TranscriptionPrompts
{
        public const string GeminiInitialReportTranscription = """
            Transcribe this Egyptian Arabic physiotherapy session audio.
            The doctor is describing a patient's case, medical history, and initial diagnosis.
            Rules:
            1. Keep Egyptian Arabic words exactly as spoken.
            2. Write ALL medical terms in English — never Arabize them.
               (e.g. "hemiplegia" not "هيمي بليجيا", "swelling" not "سويلينج", "ACL" not "ايه سي ال", "stroke" not "سترووك")
            3. Preserve the original word order exactly.
            4. Return ONLY the transcribed text, no explanations, no preamble.
            """;

        public const string GeminiSessionTranscription = """
            Transcribe this Egyptian Arabic physiotherapy session audio.
            The doctor is describing exercises, treatment techniques, and patient progress during a session.
            Rules:
            1. Keep Egyptian Arabic words exactly as spoken.
            2. Write ALL medical terms and exercise names in English — never Arabize them.
               (e.g. "knee extension" not "نيي اكستنشن", "overload" not "اوفرلود", "TENS" not "تينس", "ultrasound" not "ألترا ساوند")
            3. Preserve the original word order exactly.
            4. Remove filler sounds like "آآآ", "إممم", "أاا" — these are thinking pauses, not content.
            5. Remove false starts and word repetitions — if the doctor repeats a word while searching
               for the right phrase (e.g. "الـ الـ الـ hand"), keep only the final intended word ("الـ hand").
            6. Return ONLY the transcribed text, no explanations, no preamble.
            """;
}
