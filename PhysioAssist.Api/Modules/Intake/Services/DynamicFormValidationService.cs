using PhysioAssist.Api.Modules.Intake.DTOs.DynamicForms;
using PhysioAssist.Api.Modules.Intake.Errors;

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

    public Result ValidateSubmissionAgainstSchema(DynamicFormSchemaDto schema, DynamicFormSubmissionDto submission)
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

        return Result.Success();
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
            "bodyselector"
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
