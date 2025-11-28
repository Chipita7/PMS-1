-- =====================================================
-- Seed Script: Idea Intake Roles & Review Task Configs
-- Based on "Idea Intake Object Model.md" Requirements
-- =====================================================

-- 1. ADD MISSING ASSIGNMENT ROLE CONFIGS
-- Initial Evaluation Stage Roles
INSERT INTO AssignmentRoleConfigs (Name, Code, Priority, CanBeMultiple, IsActive, SortOrder, Description, CreatedDate, LastUpdatedDate, CreatedBy, LastUpdatedBy)
VALUES 
-- Initial Evaluation Roles
('Head of Product Management', 'HEAD_PRODUCT_MGMT', 1, 0, 1, 1, 'Leads the review of business requests, assesses strategic alignment', GETUTCDATE(), GETUTCDATE(), 'SYSTEM', 'SYSTEM'),
('Business Analyst', 'BUSINESS_ANALYST', 2, 1, 1, 2, 'Gathers and clarifies business requests, conducts evaluation', GETUTCDATE(), GETUTCDATE(), 'SYSTEM', 'SYSTEM'),
('Innovation Chapter', 'INNOVATION_CHAPTER', 3, 0, 1, 3, 'Provides insights into innovative approaches and opportunities', GETUTCDATE(), GETUTCDATE(), 'SYSTEM', 'SYSTEM'),
('Subject Matter Expert', 'SUBJECT_MATTER_EXPERT', 4, 1, 1, 4, 'Offers domain-specific expertise', GETUTCDATE(), GETUTCDATE(), 'SYSTEM', 'SYSTEM'),
('Stakeholder', 'STAKEHOLDER', 5, 1, 1, 5, 'Business stakeholders providing initial input and feedback', GETUTCDATE(), GETUTCDATE(), 'SYSTEM', 'SYSTEM'),

-- Idea Refinement Stage Roles (NEW - Missing from your system)
('Head of Engineering', 'HEAD_ENGINEERING', 6, 0, 1, 6, 'Provides technical insights into feasibility and helps refine ideas', GETUTCDATE(), GETUTCDATE(), 'SYSTEM', 'SYSTEM'),
('Head of Data & AI', 'HEAD_DATA_AI', 7, 0, 1, 7, 'Ensures data-related considerations are integrated into refinement', GETUTCDATE(), GETUTCDATE(), 'SYSTEM', 'SYSTEM'),
('Product Owner', 'PRODUCT_OWNER', 8, 1, 1, 8, 'Collaborates to break down ideas into user stories', GETUTCDATE(), GETUTCDATE(), 'SYSTEM', 'SYSTEM'),
('Information Security', 'INFO_SECURITY', 9, 0, 1, 9, 'Identifies security concerns and vulnerability assessment', GETUTCDATE(), GETUTCDATE(), 'SYSTEM', 'SYSTEM'),
('DevSecOps Team', 'DEVSECOPS_TEAM', 10, 1, 1, 10, 'Contributes automation and security feasibility insights', GETUTCDATE(), GETUTCDATE(), 'SYSTEM', 'SYSTEM'),

-- Review/Approval Roles
('VP/Director', 'VP_DIRECTOR', 11, 0, 1, 11, 'Reviews and approves requests for final approval', GETUTCDATE(), GETUTCDATE(), 'SYSTEM', 'SYSTEM'),
('Compliance Officer', 'COMPLIANCE_OFFICER', 12, 0, 1, 12, 'Ensures regulatory and compliance requirements are met', GETUTCDATE(), GETUTCDATE(), 'SYSTEM', 'SYSTEM'),
('PMO', 'PMO', 13, 0, 1, 13, 'Project Management Office oversight and approval', GETUTCDATE(), GETUTCDATE(), 'SYSTEM', 'SYSTEM');

-- 2. ADD REVIEW TASK CONFIGS FOR IDEA REFINEMENT STAGE
-- These define the template tasks that get auto-created during Idea Refinement

-- Check if ReviewTaskConfigs table exists, if not this will help identify the issue
INSERT INTO ReviewTaskConfigs (Code, Name, Description, DefaultAssigneeRole, IsActive, Priority, EstimatedHours, CreatedDate, LastUpdatedDate, CreatedBy, LastUpdatedBy)
VALUES 
-- Idea Refinement Review Tasks
('TECH_FEASIBILITY_REVIEW', 'Technical Feasibility Review', 'Head of Engineering reviews technical feasibility and implementation approach', 'HEAD_ENGINEERING', 1, 1, 8, GETUTCDATE(), GETUTCDATE(), 'SYSTEM', 'SYSTEM'),
('DATA_AI_REVIEW', 'Data & AI Integration Review', 'Head of Data & AI ensures data considerations are integrated', 'HEAD_DATA_AI', 1, 2, 4, GETUTCDATE(), GETUTCDATE(), 'SYSTEM', 'SYSTEM'),
('PRODUCT_BREAKDOWN_REVIEW', 'Product Breakdown Review', 'Product Owner breaks down idea into user stories and development needs', 'PRODUCT_OWNER', 1, 3, 6, GETUTCDATE(), GETUTCDATE(), 'SYSTEM', 'SYSTEM'),
('SECURITY_ASSESSMENT', 'Security Assessment Review', 'Information Security identifies security concerns and requirements', 'INFO_SECURITY', 1, 4, 4, GETUTCDATE(), GETUTCDATE(), 'SYSTEM', 'SYSTEM'),
('DEVSECOPS_REVIEW', 'DevSecOps Feasibility Review', 'DevSecOps team reviews automation and CI/CD potential', 'DEVSECOPS_TEAM', 1, 5, 3, GETUTCDATE(), GETUTCDATE(), 'SYSTEM', 'SYSTEM'),
('SME_REFINEMENT_REVIEW', 'Subject Matter Expert Refinement', 'SME provides specific insights to refine the idea further', 'SUBJECT_MATTER_EXPERT', 1, 6, 4, GETUTCDATE(), GETUTCDATE(), 'SYSTEM', 'SYSTEM'),

-- Final Approval Review Tasks
('VP_DIRECTOR_APPROVAL', 'VP/Director Final Approval', 'VP or Director reviews and provides final approval decision', 'VP_DIRECTOR', 1, 7, 2, GETUTCDATE(), GETUTCDATE(), 'SYSTEM', 'SYSTEM'),
('COMPLIANCE_APPROVAL', 'Compliance Final Review', 'Compliance Officer ensures all regulatory requirements are satisfied', 'COMPLIANCE_OFFICER', 1, 8, 3, GETUTCDATE(), GETUTCDATE(), 'SYSTEM', 'SYSTEM'),
('PMO_APPROVAL', 'PMO Strategic Alignment Review', 'PMO reviews strategic alignment and resource allocation', 'PMO', 1, 9, 2, GETUTCDATE(), GETUTCDATE(), 'SYSTEM', 'SYSTEM');

-- 3. UPDATE WORKFLOW STAGE CONFIG TO ENSURE IDEA REFINEMENT EXISTS
-- Make sure Idea Refinement stage exists and is properly configured
UPDATE WorkflowStageConfigs 
SET IsActive = 1, Description = 'Idea refinement and prioritization with multi-team review'
WHERE Code = 'IDEA_REFINEMENT';

-- If it doesn't exist, create it
IF NOT EXISTS (SELECT 1 FROM WorkflowStageConfigs WHERE Code = 'IDEA_REFINEMENT')
BEGIN
    INSERT INTO WorkflowStageConfigs (Name, Code, Description, IsActive, SortOrder)
    VALUES ('Idea Refinement', 'IDEA_REFINEMENT', 'Idea refinement and prioritization with multi-team review', 1, 2);
END

PRINT '✅ Successfully seeded Idea Intake roles and review task configurations';
PRINT '📋 Added roles: Head of Engineering, Head of Data & AI, Product Owner, Information Security, DevSecOps Team, VP/Director, Compliance Officer, PMO';
PRINT '🔄 Added review task templates for automatic generation during Idea Refinement stage';


