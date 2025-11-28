-- =====================================================
-- Fix Idea Refinement Flow Issues
-- =====================================================

-- 1. ENSURE ALL REQUEST TYPES TRIGGER IDEA REFINEMENT
UPDATE RequestTypeConfigs 
SET RequiresIdeaRefinementAlways = 1,
    RequireIdeaRefinementIfHighRisk = 1,
    IdeaRefinementMinScore = 5.0
WHERE IsActive = 1;

-- 2. ENSURE IDEA REFINEMENT WORKFLOW STAGE EXISTS AND IS ACTIVE
IF NOT EXISTS (SELECT 1 FROM WorkflowStageConfigs WHERE Code = 'IDEA_REFINEMENT')
BEGIN
    INSERT INTO WorkflowStageConfigs (Name, Code, Description, IsActive, SortOrder)
    VALUES ('Idea Refinement', 'IDEA_REFINEMENT', 'Idea refinement and prioritization with multi-team review', 1, 2);
END
ELSE
BEGIN
    UPDATE WorkflowStageConfigs 
    SET IsActive = 1, 
        Description = 'Idea refinement and prioritization with multi-team review'
    WHERE Code = 'IDEA_REFINEMENT';
END

-- 3. ENSURE REVIEW TASK CONFIGS EXIST (if they don't)
IF NOT EXISTS (SELECT 1 FROM ReviewTaskConfigs WHERE Code = 'DATA_AI_REVIEW')
BEGIN
    -- Run the data migration to create missing review task configs
    PRINT '⚠️ Review task configs missing - please run DataMigrationService.MigrateEnumDataToConfigAsync()';
END

-- 4. CHECK CURRENT STATE
SELECT 'REQUEST TYPE CONFIGS' as TableName, Name, Code, RequiresIdeaRefinementAlways, RequireIdeaRefinementIfHighRisk
FROM RequestTypeConfigs 
WHERE IsActive = 1;

SELECT 'WORKFLOW STAGE CONFIGS' as TableName, Name, Code, IsActive
FROM WorkflowStageConfigs 
WHERE Code IN ('INITIAL_EVAL', 'IDEA_REFINEMENT', 'READY_FOR_APPROVAL');

SELECT 'ASSIGNMENT ROLE CONFIGS' as TableName, Name, Code, IsActive
FROM AssignmentRoleConfigs 
WHERE Code IN ('HEAD_PRODUCT_MGMT', 'BUSINESS_ANALYST', 'HEAD_ENGINEERING', 'HEAD_DATA_AI', 'PRODUCT_OWNER');

SELECT 'REVIEW TASK CONFIGS' as TableName, Name, Code, AssigneeRole, IsActive
FROM ReviewTaskConfigs 
WHERE Code IN ('DATA_AI_REVIEW', 'PRODUCT_BREAKDOWN_REVIEW', 'DEVSECOPS_REVIEW');

PRINT '✅ Idea Refinement flow configuration updated';
PRINT '📋 Next steps:';
PRINT '   1. Restart your backend application';
PRINT '   2. Test with a NEW request (existing requests may need manual workflow update)';
PRINT '   3. Use ReviewerType = "EVALUATOR" for initial evaluation assignments';
PRINT '   4. Check logs for "Auto-transitioned request X to Idea Refinement" message';


