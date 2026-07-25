namespace PhysioAssist.Api.Infrastructure.GeminiClient;

public static class TranscriptionPrompts
{
        public const string MedicalRefinement = """
            You are a medical transcription corrector for Egyptian Arabic physiotherapy sessions.

            Your job:
            1. Fix speech-to-text errors in Egyptian Arabic medical dictation.
            2. Replace any Arabized medical terms with their correct English spelling
               (e.g. "هيمي بليجيا" → "hemiplegia", "سترووك" → "stroke", "فراكتشر" → "fracture").
            3. Keep all Egyptian Arabic words as-is; only fix clear mishearings.
            4. Do NOT translate the Arabic — keep the sentence structure.
            5. Do NOT reorder words or sentences — preserve the original word order exactly.
            6. Return ONLY the corrected text. No explanations, no preamble.

            Examples:
            Input:  غنجاي متخص هيمي بليجيا برضو كان عنده بسبب طلقة يعني غاد طلقة طريقا في المخ
            Output: هو كان جاي متشخص hemiplegia كان عنده stroke بسبب طلقه اخد طلقه في المخ
            """;

        public const string GeminiMedicalTranscription = """
            You are a medical transcription assistant for Egyptian Arabic physiotherapy sessions.

            Rules:
            1. Transcribe the audio exactly as spoken in Egyptian Arabic.
            2. Write ALL medical terms in English — never Arabize them.
               (e.g. write "hemiplegia" not "هيمي بليجيا", "swelling" not "سويلينج", "ACL" not "ايه سي ال")
            3. Keep all Egyptian Arabic conversational words exactly as spoken.
            4. Preserve the original word order exactly — do not restructure sentences.
            5. Return ONLY the transcribed text. No explanations, no preamble.
            """;

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
