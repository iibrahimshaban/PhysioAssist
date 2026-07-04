namespace PhysioAssist.Api.Infrastructure.GitHubModelsClient;

public static class ChunkingFewShotExamples
{
    public const string TranscriptSample =
        "بس هو دلوقتي في الـ late stages خلاص هو كويس بنشتغل معاه بس core و بنشتغل معاه الـ lower limb " +
        "بيبقى كويس جدا و اغلب الجلسة معاه بتبقى strength بطريقة كبيرة يعني انت عايز تبقى عنده core كويس " +
        "وعنده الـ muscles بتاعته كويسة طبعا ان هو كان حصله paralysis عايزين نرجعله الـ muscles كويسة جدا " +
        "وهو في الـ core تمام يعني بنشتغل معاه advanced جدا في الـ core شغالين معاه برضه على الـ بالذات " +
        "على الـ hand ان هو الـ hand functions عنده طبعا بتبقى مشكلة اخر حاجة بتعرف تجيبها في حالات الـ " +
        "hemiplegia الـ hand functions طبعا عنده weak hand grip وطبعا ما بيعرفش يعمل الحاجات الصغيراة " +
        "البسيطة فـ شغالين معاه OT بيعمل طبعا دي مثلا الحاجات اللي هي اللي هو المسامير يفكها يربطها يحاول " +
        "يشيل حاجة بإيده وبصوابعه يحاول يمسك حاجة مثلا spherical او cylindrical على حسب طبعا الحاجات " +
        "اللي موجودة لكن الجلسة بتاعته اغلبها يعني 90% منها بتبقى strength للـ muscles كلها والـ core " +
        "طبعا muscles والـ OT";

    public const string ExpectedOutput =
        """
        [
          {
            "Recommendations": "core و lower limb strength exercises",
            "RecommendationDetails": "بنشتغل معاه advanced جدا في الـ core، وstrength بطريقة كبيرة للـ muscles، خصوصا بعد الـ paralysis عشان نرجع الـ muscles كويسة",
            "PatientResponse": null,
            "NextSessionFocus": "استمرار تقوية الـ core والـ lower limb",
            "Diagnosis": "hemiplegia، late stages، تاريخ paralysis",
            "Notes": null
          },
          {
            "Recommendations": "OT hand function exercises",
            "RecommendationDetails": "فك وربط مسامير، محاولة مسك حاجات spherical او cylindrical بالإيد والصوابع، تدريب على الحاجات الصغيرة البسيطة",
            "PatientResponse": null,
            "NextSessionFocus": "تحسين الـ hand functions والـ weak hand grip",
            "Diagnosis": "hemiplegia، weak hand grip، صعوبة في الحاجات الدقيقة البسيطة",
            "Notes": null
          }
        ]
        """;

    public const string TranscriptSample2 =
        "المريضة كانت عندها ألم في الركبة بعد العملية، النهاردة قولتلها تعمل knee extension exercises " +
        "وبعد الجلسة قالت إنها حسّت بتحسن كبير في الألم وقدرت تمشي أسهل من الأول، كانت متجاوبة جدا مع " +
        "التمارين. المرة الجاية هنزود شوية في الـ range of motion. برضو لاحظت إنها كانت متأخرة عن الميعاد " +
        "بسبب المواصلات.";

    public const string ExpectedOutput2 =
        """
        [
          {
            "Recommendations": "knee extension exercises",
            "RecommendationDetails": "تمارين لتحسين مدى حركة الركبة بعد العملية",
            "PatientResponse": "حسّت بتحسن كبير في الألم وقدرت تمشي أسهل، كانت متجاوبة جدا مع التمارين",
            "NextSessionFocus": "زيادة range of motion",
            "Diagnosis": "ألم في الركبة بعد عملية",
            "Notes": "المريضة كانت متأخرة عن الميعاد بسبب المواصلات"
          }
        ]
        """;

    public const string Formatted = $"""
        EXAMPLE 1 — optional fields null when not mentioned:
        Transcript: "{TranscriptSample}"
        Correct output (exactly TWO objects — the closing "90%" summary is NOT extracted as a 
        third object since it just restates the two domains above; PatientResponse and Notes 
        are null here because the doctor didn't mention them):
        {ExpectedOutput}

        EXAMPLE 2 — optional fields populated when explicitly mentioned:
        Transcript: "{TranscriptSample2}"
        Correct output (PatientResponse and Notes are filled in here because the doctor DID 
        mention how the patient responded and an unrelated logistical note):
        {ExpectedOutput2}

        RULE: Only fill PatientResponse and Notes when the doctor explicitly says something 
        relevant. Do not default to null out of habit, and do not invent content when nothing 
        was said — check each transcript independently.
        """;
}
