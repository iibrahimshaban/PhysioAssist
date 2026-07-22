namespace PhysioAssist.Api.Infrastructure.GitHubModelsClient.Prompts;

public static class ChunkingFewShotExamples
{
    public const string TranscriptSample1 =
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

    public const string ExpectedOutput1 =
        """
        [
          {
            "Recommendations": "Core and lower limb strength exercises",
            "RecommendationDetails": "Advanced core exercises combined with significant strength training for the muscles, particularly important after paralysis to help restore muscle strength.",
            "PatientResponse": null,
            "NextSessionFocus": "Continue strengthening the core and lower limb",
            "Diagnosis": "Hemiplegia, late-stage recovery, history of paralysis",
            "Notes": null
          },
          {
            "Recommendations": "Occupational therapy hand function exercises",
            "RecommendationDetails": "Tasks such as unscrewing and screwing bolts, attempting to pick up objects with the hand and fingers, and gripping objects of different shapes such as spherical or cylindrical items, depending on what is available.",
            "PatientResponse": null,
            "NextSessionFocus": "Improve hand function and weak hand grip",
            "Diagnosis": "Hemiplegia, weak hand grip, difficulty with fine motor tasks",
            "Notes": null
          }
        ]
        """;

    public const string TranscriptSample2 =
        "طبعا في حالات الhigh paraplegia طبعا هو مبقاش عنده perineal sensation وعنده weakness طبعا " +
        "بيبقى لابس catheter ما بيتحكمش في الحمام يخش الحمام وكده فطبعا بيلبسوا catheter مبقاش عنده " +
        "control على أي حاجة في الlower limb بسبب طبعا الhigh paraplegia بيبقى الcontrol بتاعه كله من " +
        "الtrunk والdepressor muscles والhand والshoulder muscles دول أهم حاجة عنده والtrunk وبيبقى مثلا " +
        "ممكن يبقى جاي مع كمان الquadratus lumborum فدي بيحرك رجله بفكره الhip hiking الaction اللي بيعمله " +
        "quadratus lumborum بنشتغل مع طبعا ان احنا strengthen الcore جدا و strengthen الtrunk جدا " +
        "والhand muscles عشان تساعده ان هو على الأقل يوصل ان هو يمشي على مشاية او يمشي على canes وممكن " +
        "يوصل فعلا ان هو يمشي بcanes بالhip hiking وان هو معتمد على الtrunk والshoulder muscles فبنشتغل " +
        "معاه مثلا من supine طبعا لو هو عنده لو هو spastic بنحاول نcontrol الspasticity بتاعته دي ونهدي " +
        "الspasticity علشان يعرف يقف على رجله في بعد كده في الgait stage يعني ان هو يقف على رجله وهو " +
        "ماشي بالcane او بالwalker بنبدأ نشتغل معاه من supine على الshoulder functions بنعمل معاه exercise " +
        "نقوي له الshoulder flexion و abduction و extension نعمل ايه تاني وبنشتغل معاه اغلب الsessions " +
        "دلوقتي معايا بنشتغل من على الparallel bar بيحاول يعمل sides كده ان هو يجيب مثلا يشغل عضلات " +
        "الobliques يشغل عضلات الtransversus abdominis والrectus abdominis نشتغل معاه طبعا على mainly " +
        "الpelvis ان هو يعرف يزق الpelvis لقدام وال ورا وي control بجسمه الpelvis وان هو يعمل pelvic " +
        "clock علشان يبقى قادر يقف بالKAFO وبنلبسه طبعا الKAFO ونبدأ نشتغل معاه ان هو walk بالwalker " +
        "واحدة واحدة بس نشتغل معاه كده";

    public const string ExpectedOutput2 =
        """
        [
          {
            "Recommendations": "Core, trunk, and hand muscle strengthening, shoulder exercises",
            "RecommendationDetails": "Exercises to strengthen the core, trunk, and hand muscles, with focus on shoulder muscles (flexion, abduction, extension) to improve the ability to walk using canes or a walker. Exercises include shoulder muscle work performed from a supine position.",
            "PatientResponse": null,
            "NextSessionFocus": "Continue strengthening the core, trunk, and hand muscles, and improve shoulder function",
            "Diagnosis": "High paraplegia, no perineal sensation, no lower limb control, muscle weakness",
            "Notes": "Patient wears a catheter due to lack of bladder control. Spasticity control is worked on, when present, to make standing and walking easier. The quadratus lumborum can compensate for lower limb control by producing a hip-hiking movement, which lets the patient move the leg forward while relying on trunk and shoulder muscles for support."
          },
          {
            "Recommendations": "Parallel bar training, pelvic control exercises, KAFO-assisted walking",
            "RecommendationDetails": "Parallel bar exercises to activate the obliques, transversus abdominis, and rectus abdominis. Exercises to move the pelvis forward and backward and control the pelvic clock. Walking training using a KAFO brace and a walker.",
            "PatientResponse": null,
            "NextSessionFocus": "Improve pelvic control and continue walking training with the KAFO and walker",
            "Diagnosis": "High paraplegia, no lower limb control, muscle weakness",
            "Notes": null
          }
        ]
        """;

    public const string Formatted = $"""
        EXAMPLE 1 — two distinct treatment domains, translated to English, redundant closing 
        summary NOT extracted as a third object:
        Transcript: "{TranscriptSample1}"
        Correct output:
        {ExpectedOutput1}

        EXAMPLE 2 — Notes captures avoided/compensatory reasoning (rule 6 and 7) and incidental 
        clinical facts, translated to natural clinical English, NOT a catch-all diagnosis dump:
        Transcript: "{TranscriptSample2}"
        Correct output:
        {ExpectedOutput2}

        RULE: Every field must be in English. Preserve established medical/technical terms 
        (e.g. "KAFO", "quadratus lumborum", "dorsiflexion") as-is — only translate the 
        connective Arabic phrasing around them. Only fill PatientResponse and Notes when the 
        doctor explicitly said something relevant; do not default to null out of habit, and do 
        not invent content when nothing was said.
        """;
}