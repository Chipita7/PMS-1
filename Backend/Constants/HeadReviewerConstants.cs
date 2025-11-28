namespace ProjectManagementSystem1.Constants
{
    /// <summary>
    /// Constants for Head Reviewer role tracking
    /// 
    /// IMPORTANT CLARIFICATION:
    /// - "Head Reviewer" = Workflow owner for a request (assigned via /api/ProjectRequests/{id}/assign-head)
    /// - Tracked in ProjectRequestOwnerHistory table with OwnerRole = HEAD_REVIEWER_ROLE_NAME
    /// - This is NOT the same as the AssignmentRoleConfig "Head of Product Management" (HEAD_PRODUCT_MGMT)
    /// - The config role can be assigned as a reviewer in the evaluation process
    /// - Head Reviewer is the person who manages the workflow and assigns reviewers
    /// </summary>
    public static class HeadReviewerConstants
    {
        /// <summary>
        /// Role name used in ProjectRequestOwnerHistory.OwnerRole when assigning Head Reviewer
        /// This identifies the workflow owner for a request
        /// </summary>
        public const string HEAD_REVIEWER_ROLE_NAME = "Head of Product Management";
        
        /// <summary>
        /// Alternative role name (for future use or migration)
        /// </summary>
        public const string HEAD_REVIEWER_ROLE_NAME_ALT = "Head Reviewer";
    }
}

