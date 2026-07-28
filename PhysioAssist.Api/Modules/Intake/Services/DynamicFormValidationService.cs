using PhysioAssist.Api.Modules.Intake.Constants;
using PhysioAssist.Api.Modules.Intake.DTOs.DynamicForms;
using PhysioAssist.Api.Modules.Intake.Errors;
using System.Text.Json;

namespace PhysioAssist.Api.Modules.Intake.Services;

public class DynamicFormValidationService : IDynamicFormValidationService
{
    public Result ValidateSchema(DynamicFormSchemaDto schema)
    {
        if (schema is null)
            return Result.Failure(IntakeErrors.InvalidSchema);

        var validationResult = ValidateSchemaStructure(schema);
        if (validationResult.IsFailure)
            return validationResult;

        return Result.Success();
    }

    public Result ValidateSubmissionAgainstSchema(DynamicFormSchemaDto schema, DynamicFormSubmissionDto submission, string? painPointsData = null)
    {
        if (schema is null)
            return Result.Failure(IntakeErrors.InvalidSchema);

        if (submission is null)
            return Result.Failure(IntakeErrors.InvalidSubmission);

        var schemaValidationResult = ValidateSchemaStructure(schema);
        if (schemaValidationResult.IsFailure)
            return Result.Failure(IntakeErrors.InvalidSchema);

        var submissionStructureResult = ValidateSubmissionStructure(submission);
        if (submissionStructureResult.IsFailure)
            return submissionStructureResult;

        var crossReferenceResult = ValidateSubmissionAgainstSchemaStructure(schema, submission);
        if (crossReferenceResult.IsFailure)
            return crossReferenceResult;

        // NEW: Validate that all required fields have non-empty values
        var requiredFieldsResult = ValidateRequiredFields(schema, submission);
        if (requiredFieldsResult.IsFailure)
            return requiredFieldsResult;

        // NEW: if the form has a body-map question, at least one region must be selected.
        // The selection can arrive either as a bodyselector/painpoint answer inside the
        // submission, OR as the standalone painPointsData payload (public patient flow).
        var bodyMapResult = ValidateBodyMapRequired(schema, submission, painPointsData);
        if (bodyMapResult.IsFailure)
            return bodyMapResult;

        return Result.Success();
    }

    /// <summary>
    /// If the schema contains a body-map question (bodyselector / painpoint), the submission
    /// must include at least one selected region. Prevents a patient from submitting a form
    /// without marking where they feel pain. Works for the body-selector answer stored in the
    /// submission (regions array length >= 1).
    /// </summary>
    private static Result ValidateBodyMapRequired(DynamicFormSchemaDto schema, DynamicFormSubmissionDto submission, string? painPointsData)
    {
        bool hasBodyQuestion = false;
        foreach (var section in schema.Sections)
            foreach (var group in section.Groups)
                foreach (var question in group.Questions)
                    if (question.Type is "bodyselector" or "painpoint")
                        hasBodyQuestion = true;

        if (!hasBodyQuestion)
            return Result.Success();

        foreach (var section in submission.Sections)
        {
            foreach (var group in section.Groups)
            {
                foreach (var answer in group.Answers)
                {
                    var q = FindQuestionInSchema(schema, answer.QuestionId);
                    if (q is null || q.Type is not ("bodyselector" or "painpoint")) continue;

                    if (HasBodySelection(answer.Value))
                        return Result.Success();
                }
            }
        }

        // The public patient flow sends the body selection as a separate painPointsData payload
        // (the standalone body-pain-map component). If it contains at least one region, that
        // satisfies the requirement.
        if (HasBodySelectionInPainPoints(painPointsData))
            return Result.Success();

        return Result.Failure(IntakeErrors.BodyMapRequired);
    }

    private static bool HasBodySelectionInPainPoints(string? painPointsData)
    {
        if (string.IsNullOrWhiteSpace(painPointsData))
            return false;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(painPointsData);
            return HasBodySelection(doc.RootElement);
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    private static bool HasBodySelection(object? value)
    {
        if (value is null) return false;
        JsonElement? element = value is JsonElement je ? je : null;
        if (element is null && value is System.Text.Json.Nodes.JsonNode node)
        {
            // Accept JsonNode from in-process callers; treat as present if it has a regions array > 0.
            if (node is System.Text.Json.Nodes.JsonObject obj && obj.TryGetPropertyValue("regions", out var reg))
                return reg is System.Text.Json.Nodes.JsonArray arr && arr.Count > 0;
            return false;
        }
        if (element is null) return false;

        // Accept either { regions: [...] } or the legacy { regions: [...] } wrapper.
        if (element.Value.ValueKind == JsonValueKind.Object)
        {
            if (element.Value.TryGetProperty("regions", out var regions) && regions.ValueKind == JsonValueKind.Array)
                return regions.GetArrayLength() > 0;
            // Also accept a direct { "bodyselector": {...} } / { "painpoint": {...} } wrapper.
            foreach (var prop in element.Value.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Object &&
                    prop.Value.TryGetProperty("regions", out var r) && r.ValueKind == JsonValueKind.Array)
                    return r.GetArrayLength() > 0;
            }
        }
        return false;
    }

    /// <summary>
    /// Validates that a schema is ready for publishing:
    /// - All core fields are present
    /// - Core fields have Required = true
    /// - Core fields have compatible types
    /// </summary>
    public Result ValidateSchemaForPublish(DynamicFormSchemaDto schema)
    {
        if (schema is null)
            return Result.Failure(IntakeErrors.InvalidSchema);

        var structureResult = ValidateSchemaStructure(schema);
        if (structureResult.IsFailure)
            return structureResult;

        // Check all core fields exist in the schema
        var missingCoreFields = new List<string>();
        var misconfiguredFields = new List<string>();

        foreach (var coreField in CoreFieldConstants.HardRequiredFields)
        {
            var found = FindQuestionInSchema(schema, coreField.QuestionId);

            if (found is null)
            {
                // Try to find by text match (for backward compatibility with existing schemas)
                found = FindQuestionByTextInSchema(schema, coreField.Text);
            }

            if (found is null)
            {
                missingCoreFields.Add(coreField.Text);
                continue;
            }

            // Check Required flag
            if (!found.Required)
            {
                misconfiguredFields.Add($"'{coreField.Text}' — Required flag is disabled");
            }

            // Check type compatibility
            if (CoreFieldConstants.AllowedTypesForCoreField.TryGetValue(coreField.QuestionId, out var allowedTypes))
            {
                if (!allowedTypes.Contains(found.Type))
                {
                    misconfiguredFields.Add($"'{coreField.Text}' — Type changed to '{found.Type}' (must be one of: {string.Join(", ", allowedTypes)})");
                }
            }
        }

        if (missingCoreFields.Count > 0)
        {
            return Result.Failure(IntakeErrors.CoreFieldsMissing(missingCoreFields));
        }

        if (misconfiguredFields.Count > 0)
        {
            var details = string.Join("; ", misconfiguredFields);
            return Result.Failure(new Error(
                "Intake.PublishValidationFailed",
                $"The schema cannot be published due to misconfigured core fields: {details}.",
                StatusCodes.Status400BadRequest));
        }

        return Result.Success();
    }

    /// <summary>
    /// Validates that all required fields in the schema have non-empty values in the submission.
    /// </summary>
    private static Result ValidateRequiredFields(DynamicFormSchemaDto schema, DynamicFormSubmissionDto submission)
    {
        var emptyRequiredFields = new List<string>();

        foreach (var section in schema.Sections)
        {
            foreach (var group in section.Groups)
            {
                foreach (var question in group.Questions)
                {
                    if (!question.Required) continue;

                    // Body-map questions (bodyselector / painpoint) are satisfied by the separate
                    // painPointsData payload (public patient flow) or an in-form bodyselector answer,
                    // not by a regular form answer. Their requirement is enforced by
                    // ValidateBodyMapRequired, so skip them here to avoid a false "required empty".
                    if (question.Type is "bodyselector" or "painpoint") continue;

                    var answer = FindAnswerInSubmission(submission, section.SectionId, group.GroupId, question.QuestionId);

                    if (answer is null || IsAnswerEmpty(answer.Value))
                    {
                        emptyRequiredFields.Add(question.Text);
                    }
                }
            }
        }

        if (emptyRequiredFields.Count > 0)
        {
            return Result.Failure(IntakeErrors.RequiredFieldsEmpty(emptyRequiredFields));
        }

        return Result.Success();
    }

    private static SubmissionAnswerDto? FindAnswerInSubmission(
        DynamicFormSubmissionDto submission,
        string sectionId,
        string groupId,
        string questionId)
    {
        foreach (var section in submission.Sections)
        {
            if (section.SectionId != sectionId) continue;

            foreach (var group in section.Groups)
            {
                if (group.GroupId != groupId) continue;

                foreach (var answer in group.Answers)
                {
                    if (answer.QuestionId == questionId)
                        return answer;
                }
            }
        }

        return null;
    }

    private static bool IsAnswerEmpty(object? value)
    {
        if (value is null) return true;

        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null => true,
                JsonValueKind.String => string.IsNullOrWhiteSpace(element.GetString()),
                JsonValueKind.Array => element.GetArrayLength() == 0,
                JsonValueKind.Object => !element.EnumerateObject().Any(),
                _ => false
            };
        }

        if (value is string str) return string.IsNullOrWhiteSpace(str);

        return false;
    }

    private static FormQuestionDto? FindQuestionInSchema(DynamicFormSchemaDto schema, string questionId)
    {
        foreach (var section in schema.Sections)
        {
            foreach (var group in section.Groups)
            {
                foreach (var question in group.Questions)
                {
                    if (question.QuestionId == questionId)
                        return question;
                }
            }
        }
        return null;
    }

    private static FormQuestionDto? FindQuestionByTextInSchema(DynamicFormSchemaDto schema, string text)
    {
        foreach (var section in schema.Sections)
        {
            foreach (var group in section.Groups)
            {
                foreach (var question in group.Questions)
                {
                    if (string.Equals(question.Text, text, StringComparison.OrdinalIgnoreCase))
                        return question;
                }
            }
        }
        return null;
    }

    private static Result ValidateSchemaStructure(DynamicFormSchemaDto schema)
    {
        if (schema.SchemaVersion <= 0)
            return Result.Failure(IntakeErrors.InvalidSchema);

        if (schema.Sections is null || schema.Sections.Count == 0)
            return Result.Failure(IntakeErrors.InvalidSchema);

        var uniqueIdResult = ValidateUniqueIds(schema);
        if (uniqueIdResult.IsFailure)
            return uniqueIdResult;

        var questionsResult = ValidateQuestions(schema);
        if (questionsResult.IsFailure)
            return questionsResult;

        var validationRulesResult = ValidateValidationRules(schema);
        if (validationRulesResult.IsFailure)
            return validationRulesResult;

        var conditionalLogicResult = ValidateConditionalLogic(schema);
        if (conditionalLogicResult.IsFailure)
            return conditionalLogicResult;

        return Result.Success();
    }

    private static Result ValidateUniqueIds(DynamicFormSchemaDto schema)
    {
        var sectionIds = new HashSet<string>();
        var allQuestionIds = new HashSet<string>();

        foreach (var section in schema.Sections)
        {
            if (string.IsNullOrWhiteSpace(section.SectionId))
                return Result.Failure(IntakeErrors.InvalidSchema);

            if (!sectionIds.Add(section.SectionId))
                return Result.Failure(IntakeErrors.InvalidSchema);

            if (section.Groups is null || section.Groups.Count == 0)
                return Result.Failure(IntakeErrors.InvalidSchema);

            var groupIds = new HashSet<string>();
            foreach (var group in section.Groups)
            {
                if (string.IsNullOrWhiteSpace(group.GroupId))
                    return Result.Failure(IntakeErrors.InvalidSchema);

                if (!groupIds.Add(group.GroupId))
                    return Result.Failure(IntakeErrors.InvalidSchema);

                if (group.Questions is null || group.Questions.Count == 0)
                    return Result.Failure(IntakeErrors.InvalidSchema);

                foreach (var question in group.Questions)
                {
                    if (string.IsNullOrWhiteSpace(question.QuestionId))
                        return Result.Failure(IntakeErrors.InvalidSchema);

                    if (!allQuestionIds.Add(question.QuestionId))
                        return Result.Failure(IntakeErrors.InvalidSchema);
                }
            }
        }

        return Result.Success();
    }

    private static Result ValidateQuestions(DynamicFormSchemaDto schema)
    {
        foreach (var section in schema.Sections)
        {
            foreach (var group in section.Groups)
            {
                foreach (var question in group.Questions)
                {
                    if (string.IsNullOrWhiteSpace(question.Text))
                        return Result.Failure(IntakeErrors.InvalidSchema);

                    if (string.IsNullOrWhiteSpace(question.Type))
                        return Result.Failure(IntakeErrors.InvalidSchema);

                    if (!QuestionTypes.IsSupported(question.Type))
                        return Result.Failure(IntakeErrors.InvalidSchema);
                }
            }
        }

        return Result.Success();
    }

    private static Result ValidateValidationRules(DynamicFormSchemaDto schema)
    {
        foreach (var section in schema.Sections)
        {
            foreach (var group in section.Groups)
            {
                foreach (var question in group.Questions)
                {
                    if (question.ValidationRules is not null)
                    {
                        foreach (var rule in question.ValidationRules)
                        {
                            if (string.IsNullOrWhiteSpace(rule.RuleType))
                                return Result.Failure(IntakeErrors.InvalidSchema);

                            if (!ValidationRuleTypes.IsSupported(rule.RuleType))
                                return Result.Failure(IntakeErrors.InvalidSchema);
                        }
                    }
                }
            }
        }

        return Result.Success();
    }

    private static Result ValidateConditionalLogic(DynamicFormSchemaDto schema)
    {
        var allQuestionIds = new HashSet<string>();
        foreach (var section in schema.Sections)
        {
            foreach (var group in section.Groups)
            {
                foreach (var question in group.Questions)
                {
                    allQuestionIds.Add(question.QuestionId);
                }
            }
        }

        foreach (var section in schema.Sections)
        {
            foreach (var group in section.Groups)
            {
                foreach (var question in group.Questions)
                {
                    if (question.Conditions is not null)
                    {
                        foreach (var condition in question.Conditions)
                        {
                            if (string.IsNullOrWhiteSpace(condition.TargetQuestionId))
                                return Result.Failure(IntakeErrors.InvalidSchema);

                            if (!allQuestionIds.Contains(condition.TargetQuestionId))
                                return Result.Failure(IntakeErrors.InvalidSchema);

                            if (string.IsNullOrWhiteSpace(condition.Operator))
                                return Result.Failure(IntakeErrors.InvalidSchema);

                            if (!ConditionOperators.IsSupported(condition.Operator))
                                return Result.Failure(IntakeErrors.InvalidSchema);
                        }
                    }
                }
            }
        }

        return Result.Success();
    }

    private static Result ValidateSubmissionStructure(DynamicFormSubmissionDto submission)
    {
        if (submission.SchemaVersion <= 0)
            return Result.Failure(IntakeErrors.InvalidSubmission);

        if (submission.FormSchemaId == Guid.Empty)
            return Result.Failure(IntakeErrors.InvalidSubmission);

        if (submission.FormSchemaVersion <= 0)
            return Result.Failure(IntakeErrors.InvalidSubmission);

        if (submission.Sections is null || submission.Sections.Count == 0)
            return Result.Failure(IntakeErrors.InvalidSubmission);

        foreach (var section in submission.Sections)
        {
            if (string.IsNullOrWhiteSpace(section.SectionId))
                return Result.Failure(IntakeErrors.InvalidSubmission);

            if (section.Groups is null || section.Groups.Count == 0)
                return Result.Failure(IntakeErrors.InvalidSubmission);

            foreach (var group in section.Groups)
            {
                if (string.IsNullOrWhiteSpace(group.GroupId))
                    return Result.Failure(IntakeErrors.InvalidSubmission);

                if (group.Answers is null)
                    return Result.Failure(IntakeErrors.InvalidSubmission);

                foreach (var answer in group.Answers)
                {
                    if (string.IsNullOrWhiteSpace(answer.QuestionId))
                        return Result.Failure(IntakeErrors.InvalidSubmission);

                    if (answer.Attachments is not null)
                    {
                        var attachmentResult = ValidateAttachments(answer.Attachments);
                        if (attachmentResult.IsFailure)
                            return attachmentResult;
                    }
                }
            }
        }

        return Result.Success();
    }

    private static Result ValidateAttachments(List<AttachmentAnswerDto> attachments)
    {
        foreach (var attachment in attachments)
        {
            if (string.IsNullOrWhiteSpace(attachment.FileName))
                return Result.Failure(IntakeErrors.InvalidSubmission);

            if (string.IsNullOrWhiteSpace(attachment.FileUrl))
                return Result.Failure(IntakeErrors.InvalidSubmission);

            if (string.IsNullOrWhiteSpace(attachment.FileType))
                return Result.Failure(IntakeErrors.InvalidSubmission);

            if (attachment.FileSize <= 0)
                return Result.Failure(IntakeErrors.InvalidSubmission);
        }

        return Result.Success();
    }

    private static Result ValidateSubmissionAgainstSchemaStructure(DynamicFormSchemaDto schema, DynamicFormSubmissionDto submission)
    {
        var schemaIndex = BuildSchemaIndex(schema);

        foreach (var submissionSection in submission.Sections)
        {
            if (!schemaIndex.SectionIds.Contains(submissionSection.SectionId))
                return Result.Failure(IntakeErrors.InvalidSubmission);

            if (!schemaIndex.SectionGroupMap.TryGetValue(submissionSection.SectionId, out var validGroupIds))
                return Result.Failure(IntakeErrors.InvalidSubmission);

            foreach (var submissionGroup in submissionSection.Groups)
            {
                if (!validGroupIds.Contains(submissionGroup.GroupId))
                    return Result.Failure(IntakeErrors.InvalidSubmission);

                var groupKey = $"{submissionSection.SectionId}:{submissionGroup.GroupId}";
                if (!schemaIndex.GroupQuestionMap.TryGetValue(groupKey, out var validQuestionIds))
                    return Result.Failure(IntakeErrors.InvalidSubmission);

                foreach (var submissionAnswer in submissionGroup.Answers)
                {
                    if (!validQuestionIds.Contains(submissionAnswer.QuestionId))
                        return Result.Failure(IntakeErrors.InvalidSubmission);
                }
            }
        }

        return Result.Success();
    }

    private static SchemaIndex BuildSchemaIndex(DynamicFormSchemaDto schema)
    {
        var index = new SchemaIndex
        {
            SectionIds = new HashSet<string>(),
            SectionGroupMap = new Dictionary<string, HashSet<string>>(),
            GroupQuestionMap = new Dictionary<string, HashSet<string>>()
        };

        foreach (var section in schema.Sections)
        {
            index.SectionIds.Add(section.SectionId);

            var groupIds = new HashSet<string>();
            foreach (var group in section.Groups)
            {
                groupIds.Add(group.GroupId);

                var questionIds = new HashSet<string>();
                foreach (var question in group.Questions)
                {
                    questionIds.Add(question.QuestionId);
                }

                var groupKey = $"{section.SectionId}:{group.GroupId}";
                index.GroupQuestionMap[groupKey] = questionIds;
            }

            index.SectionGroupMap[section.SectionId] = groupIds;
        }

        return index;
    }

    private static class QuestionTypes
    {
        private static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "text",
            "textarea",
            "number",
            "email",
            "phone",
            "date",
            "datetime",
            "select",
            "multiselect",
            "radio",
            "checkbox",
            "boolean",
            "file",
            "fileupload",
            "painpoint",
            "painscale",
            "bodyselector",
            "summary"
        };

        public static bool IsSupported(string type) => SupportedTypes.Contains(type);
    }

    private static class ValidationRuleTypes
    {
        private static readonly HashSet<string> SupportedRuleTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "required",
            "minLength",
            "maxLength",
            "min",
            "max",
            "pattern",
            "email",
            "url"
        };

        public static bool IsSupported(string ruleType) => SupportedRuleTypes.Contains(ruleType);
    }

    private static class ConditionOperators
    {
        private static readonly HashSet<string> SupportedOperators = new(StringComparer.OrdinalIgnoreCase)
        {
            "equals",
            "notEquals",
            "contains",
            "greaterThan",
            "lessThan",
            "in",
            "notIn"
        };

        public static bool IsSupported(string operatorType) => SupportedOperators.Contains(operatorType);
    }

    private class SchemaIndex
    {
        public HashSet<string> SectionIds { get; init; } = new();
        public Dictionary<string, HashSet<string>> SectionGroupMap { get; init; } = new();
        public Dictionary<string, HashSet<string>> GroupQuestionMap { get; init; } = new();
    }
}