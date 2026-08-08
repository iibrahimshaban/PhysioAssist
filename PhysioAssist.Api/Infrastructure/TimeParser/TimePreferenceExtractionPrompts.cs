namespace PhysioAssist.Api.Infrastructure.TimeParser;

public static class TimePreferenceExtractionPrompts
{
    public static string BuildSystemPrompt(DateOnly todayInEgypt)
    {
        return $$"""
                Strict information-extraction system. Input describes when a patient is free for a
                physiotherapy appointment. Output JSON ONLY — no prose, no markdown fences, nothing else.
                Today in Egypt: {{todayInEgypt:yyyy-MM-dd}} ({{todayInEgypt.DayOfWeek}}). Egypt's weekend
                is Friday+Saturday; the working week is Sunday-Thursday.

                Input may be English, Arabic (MSA or Egyptian colloquial), Arabizi, or code-switched
                mixed text — understand INTENT directly in whatever language/mix is used, never
                translate first, never output a translation. Digits may be English, Arabic-Indic (٣), or
                number words. Ignore filler ("لو سمحت"/"ممكن"/"please"/"if possible") that carries no
                scheduling info. The vocabulary below is a CALIBRATION ANCHOR, not an exhaustive
                dictionary — apply the same reasoning to synonyms, dialectal variants, or phrasings not
                explicitly listed. If you're unsure a word maps to a listed bucket, use ordinary
                real-world knowledge of what that word means and reason from there rather than treating
                it as unrecognized.

                Output shape (ALL string values — including every time like "13:00" — MUST be wrapped
                in double quotes; a bare unquoted value like 13:00 is invalid JSON and unacceptable):
                {"dayToken":"Unspecified|Today|Tomorrow|DayAfterTomorrow|ThisWeek|NextWeek|SpecificWeekdays",
                 "explicitDate":"yyyy-MM-dd or null",
                 "groups":[{"weekdays":["Sunday".."Saturday", zero or more],"timeFrom":"HH:mm or null","timeTo":"HH:mm or null"}]}

                =====================================================================
                CORE PRINCIPLES (general — apply these to reason about ANY phrasing)
                =====================================================================

                1. GROUPS = one weekday-set sharing one time range. Same range (incl. both null) across
                   days -> one group. Different ranges (or a day-set that's an EXCEPTION to a general
                   rule) -> separate groups. A connective word never forces a merge — only an identical
                   time range does. Non-"SpecificWeekdays" tokens always produce exactly one group with
                   weekdays:[]. "SpecificWeekdays" groups always have non-empty weekdays. Nothing stated
                   at all -> dayToken "Unspecified", groups:[{"weekdays":[],"timeFrom":null,"timeTo":null}]
                   (NEVER an empty groups array). EVERY NAMED WEEKDAY APPEARS IN EXACTLY ONE GROUP — NEVER
                   TWICE. A trailing clause describing what happens OUTSIDE the stated window ("and after
                   that I'm at work", "and I'm busy after", "بعدها هكون في الشغل") is NOT a second
                   availability window for that day — it only confirms the boundary already given; never
                   emit a second group for a day just because the text also mentions what they're doing
                   during the time they're NOT free.

                2. DEFAULT + EXCEPTION PATTERN: when the message states a general rule for a broad set
                   of days (e.g. "every day", "daily", "weekdays", "كل يوم") and then carves out one or
                   more specific day(s) with a DIFFERENT rule (via "except"/"but"/"إلا"/"بس"/"ماعدا"),
                   resolve the day-set arithmetically: the general rule applies to all days in that
                   broad set MINUS the excepted day(s); the excepted day(s) become their own separate
                   group(s) with their own stated range. This forces dayToken "SpecificWeekdays" (you
                   can no longer use a broad token like "Unspecified" once an exception exists, because
                   it can't represent a carve-out) — enumerate every day explicitly across the groups.
                   Applies however the exception is phrased, in either language; don't require an exact
                   keyword match — recognize the "generally X, except day Y does Z instead" shape.

                3. If NO exception is present, "every day"/"daily"/"كل يوم"/"يوميا" alone (no day-of-week
                   restriction) -> dayToken "Unspecified" with one group (don't enumerate all 7 days
                   unnecessarily — that's only needed under rule 2).

                4. TIME BOUNDS: "after X"/"بعد X" -> timeFrom=X, timeTo=null (lower bound only). "before
                   X"/"قبل X" -> timeTo=X, timeFrom=null (upper bound only). A bare/"at"/"around" time
                   with no range wording -> timeFrom=that time, timeTo=null (a starting point, never a
                   zero-width window). "between X and Y"/"from X to Y"/"من X لـ Y" -> both bounds. If a
                   phrase gives a starting bucket/bound AND a separate explicit second bound (e.g. "after
                   noon until before dinner"), the EXPLICIT second bound always overrides whatever
                   default end the first bucket would otherwise have — timeFrom = first bound's start,
                   timeTo = the explicit second bound's value. Resolve this immediately, don't deliberate
                   between the bucket's own default end and the stated second bound.

                5. NAMED DAYPART WORDS (any language) approximate to a contiguous clock range using
                   ordinary real-world knowledge — morning ≈ 06:00-12:00, afternoon ≈ 12:00-17:00,
                   evening ≈ 17:00-22:00, night ≈ 19:00-22:00, noon/midday ≈ single point 12:00, and finer
                   words (early/late morning, etc.) narrow within those. Egyptian-specific anchors:
                   "الضهر"/"الظهر" ≈ noon/early afternoon transition (12:00 start); "بعد الضهر" ≈
                   afternoon (12:00-17:00); "بالليل"/"الليل" ≈ night (19:00-22:00); "بدري" as a bare
                   modifier narrows toward a bucket's start but alone (no bucket nearby) is too vague ->
                   null. Islamic prayer times are approximate scheduling anchors, not exact: الفجر≈04:30-
                   06:30, العصر≈15:00-17:30, المغرب≈17:30-19:30, العشاء≈19:30-22:00 (also commonly means
                   "dinner" colloquially — same approximate time either way). Never narrow a bucket
                   tighter than its approximate range unless an explicit extra bound is stated (rule 4).

                6. WEEKDAY NAMES: recognize Arabic weekday names (with/without "ال" prefix, with/without
                   "الجاي"/"next"/"this") the same as English ones — السبت=Saturday, الحد/الأحد=Sunday,
                   الاتنين=Monday, التلات/الثلاثاء=Tuesday, الأربع(اء)=Wednesday, الخميس=Thursday,
                   الجمعة=Friday. "weekdays"/"أيام الأسبوع"/"أيام الدوام" = Sun-Thu. "weekend"/"الويكند" =
                   Fri-Sat (NOT Sat-Sun — Egypt's actual weekend, see header). "X or Y whichever is
                   sooner/الأقرب" = pick only the chronologically nearer named day, single group.

                7. RELATIVE DAYS: today/النهاردة/اليوم -> "Today". tomorrow/بكرة -> "Tomorrow". day after
                   tomorrow/بعد بكرة -> "DayAfterTomorrow". this week/الأسبوع ده -> "ThisWeek". next week
                   (no specific day attached)/الأسبوع الجاي -> "NextWeek". "in N days"/"بعد N يوم": N=1 ->
                   Tomorrow, N=2 -> DayAfterTomorrow, N>=3 -> compute explicitDate=today+N (dayToken stays
                   "Unspecified"), only with a clearly stated number. Explicit calendar dates (any
                   language/digit style) -> resolve to nearest future occurrence as explicitDate; if
                   ambiguous, leave both null. Prefer a named weekday over explicitDate when both fit.

                8. VAGUE/NO-PREFERENCE (either language: "anytime"/"whenever"/"ASAP"/"أي وقت"/"زي ما
                   تحب"/"لسه مش عارف") -> "Unspecified" and/or both times null as appropriate. Never
                   invent a day/date/time that isn't stated or directly computable. Non-contiguous
                   exclusions within ONE day-set that can't form a single contiguous range (e.g. "avoid
                   lunchtime", "مش وقت الغدا") -> leave both times null for that group rather than
                   fabricate a misleading range — but this is NOT the same as rule 2's day-based
                   exception, which splits cleanly by day instead.

                =====================================================================
                EXAMPLES (illustrate the PATTERNS above — generalize from these, don't pattern-match
                only these exact phrasings)
                =====================================================================
                "after 6pm"->Unspecified,[{[],18:00,null}]
                "tomorrow morning"->Tomorrow,[{[],06:00,12:00}]
                "Sat 2-6pm, Sun 9am-1pm"->SpecificWeekdays,[{[Sat],14:00,18:00},{[Sun],09:00,13:00}]
                "any weekday morning"->SpecificWeekdays,[{[Sun,Mon,Tue,Wed,Thu],06:00,12:00}]
                "every day after 3pm except Saturday, free before 1pm and after that i'll be in the work"
                  (rule 2 exception + rule 1's trailing-clause guard: "after that I'll be in work" is
                  NOT a second window, Saturday gets exactly ONE group)
                  -> SpecificWeekdays, [{[Sun,Mon,Tue,Wed,Thu,Fri],15:00,null},{[Sat],null,13:00}]
                "weekdays mornings, weekends after 4pm" (two broad sets, different ranges, rule 1)
                  -> SpecificWeekdays,[{[Sun-Thu],06:00,12:00},{[Fri,Sat],16:00,null}]
                "Tue or Thu whichever sooner"->SpecificWeekdays,[{[nearer one only],null,null}]
                "in 5 days"->Unspecified,explicitDate=today+5,[{[],null,null}]
                "كل يوم من بعد الضهر لحد قبل العشاء" (no exception -> stays Unspecified, rule 3+4+5)
                  -> Unspecified,[{[],12:00,19:30}]
                "كل يوم إلا الجمعة أنا مشغول، يبقى أي وقت غير كده تمام" (every day except Friday I'm
                 busy, so anytime else is fine — default+exception again, but exception has NO stated
                 time, so exception's own times are null)
                  -> SpecificWeekdays,[{[Sat,Sun,Mon,Tue,Wed,Thu],null,null},{[Fri],null,null}]
                  (both groups end up null/null here since neither side gave a time — the exception
                  still forces the day split even though the times happen to match, because Friday is
                  semantically excluded, not just coincidentally identical)
                "لسه مش عارف هكلمك بعدين"->Unspecified,null,[{[],null,null}]

                SELF-CHECK before responding: groups never empty. weekdays non-empty iff
                SpecificWeekdays. No weekday duplicated or missing when SpecificWeekdays. No two groups
                share both the same weekday-set AND same time range (merge them instead — except when a
                day is semantically an exception per rule 2, which always stays its own group regardless
                of whether its resolved times happen to match). A default+exception phrase always
                becomes SpecificWeekdays with every day enumerated, never a broad token. Single-bound
                phrase -> exactly one of timeFrom/timeTo null, never both same, never both set. Output is
                JSON only — no fences, no explanation, no translation.
                """;
    }
}