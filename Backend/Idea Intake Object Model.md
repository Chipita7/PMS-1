
 
Commercial Bank of Ethiopia
Technology Sector – Digital Factory

Agile-ready Project Management System - Project

Request / Idea Intake Object Model 
(Draft Version)
V 1.1


Nov 01, 2025


⦁	Request / Idea Intake Object Model
⦁	Introduction
⦁	Entity/object Name: Project or Idea request
⦁	Purpose: Captures all necessary metadata, context, and attachments from a business team or customer when submitting a new project or idea request to the APMS (Agile Project Management System).
⦁	Core Identification Fields (M: Mandatory during creation)
Field Name	Type	Description
RequestID
(M)	String (12 chars)	System-generated unique identifier for each request of format (PRYYYYMMDDXX) where 
PR to refer Project Request,
YYYY refer current Year e.g. 2025, MM - Month, DD – date, XX continuous number from 01-99
Example: PR202500123
ReferenceNo	String
(Max 15 chars)	human-friendly reference number or business ticket to be provided from business/ requester
Example: BR-DVS-012345
RequestTitle
(M)	String (Max 200 chars)	Brief, descriptive title of the request
Eg: “New Mobile Payment Gateway Integration”
RequestDescription
(M)	Text	Detailed description of the idea or request

⦁	Requestor & Origin Details
Field Name	Type	Description
RequestedBy
(M)	String (AD User ID/ Alias)	The person submitting the request
E.g. anwarindris
RequestedByName
(M)	String (max 50 chars	Display name (auto-populated from AD)
E.g. Anwar Indris
BusinessSector
(M)	String (max 50 chars	(auto-populated from ERP/AD)
BusinessDivision
(M)	String (max 50 chars	(auto-populated from ERP/AD)
BusinessDepartment
(M)	String (max 50 chars	(auto-populated from ERP/AD)
OrganizationName
(M)	String
(max 30 chars	Default: Commercial Bank of Ethiopia
PrimaryContactEmail
(M)	String
(max 30 chars	Primary contact email of the requestor
e.g. anwarindris@cbe.com.et
SecondaryContactEmail
(M)	String
(max 30 chars	Additional contact email for follow-up
PrimaryContactPhone
(M)	String 
(max 15 chars	Ethipia Phone standard
e.g. +251-911-098765
SecondaryContactPhone
(M)	String	Additional phone for follow-up

⦁	Classification & Metadata
Field Name	Type	Description
RequestType
(M)	Lookup	Defines the nature of the request
List: New Development, Enhancement, Internal-Integration, Third-Party Integration, (to be Added or removed or updated as needed)
RequestCategory
(M)	Lookup	Defines the categories of the request type
List: Remittance, Government, Inhouse-development
ServiceCategory
(M)	Lookup	Defines the categories of affected service
List: Digital Banking Management, Card Banking Management, Credit Management, Risk Management, 
ProductCategory
(M)	Lookup	Affected product or domain
List: Mobile Banking, CBE Birr Wallet, T24 Core Banking, IMAL Core Banking, other Legacy System
PriorityLevel
(M)	Lookup	Requested priority level (auto assigned based on impact and urgency)
List: P1, P2, P3, P4, P5
BusinessImpact
(M)	Lookup	Impact on business 
List: High, Medium, Low
RequestUrgency
(M)	Lookup	Impact to the business 
List: High, Medium, Low
StrategicAlignment
(M)	Lookup	To which current strategy is aligned to?
List of Existing Strategies
EstimatedCost	Decimal	Optional rough cost estimate in ETB
EstimatedBenefit	Decimal	Business value or ROI (if known)
BenefitCaptureDuration	Decimal	Business value or ROI capture duration after delivery in months (if known)
RequestedDeliveryDate	Date	Desired completion date
RiskLevel	Lookup	Business risk associated with delay
List: Low, Medium, High
ComplexityLevel	Lookup	Low, Medium, High

⦁	Evaluation & Scoring Fields
Field Name	Type	Description
EvaluatorID	Lookup (AD User)	Person responsible for initial evaluation
FeasibilityScore	Integer	Computed score based on feasibility evaluation
BusinessValueScore	Integer	Weighted score based on business input
TechnicalComplexityScore	Integer	Assessment from technical team
TotalScore	Decimal	Weighted total score (auto-calculated)
EvaluationRemarks	Text	Notes from evaluator

⦁	Workflow & Status Tracking
Field Name	Type	Description
Status	Lookup	Current workflow status
List: Submitted, Under Evaluation, Approved, Rejected, Backlogged, InProgress, Completed, Delivered, Closed
WorkflowStage	Lookup	Current lifecycle stage (based on the 12 workflow stages)
List: Initial Evaluation, Idea Refinement, Sprint Planning, UAT, Deployment, Handover
AssignedTeam	Lookup	Team handling the request
AssignedTo	Lookup (AD User)	Responsible person / lead
CreatedDate	DateTime	Timestamp of submission
E.g. 2025-10-30T09:12:00Z
LastUpdatedDate	DateTime	Auto-updated on modification
E.g. 2025-10-30T09:12:00Z
LastUpdatedBy	Lookup (AD User)	Auto-updated on modification from login detail
ApprovalDate	Date	Date approved / rejected
RequestDurationDays	Integer/ Number	Current duration of the project since created
Auto calculated, to be auto updated daily

⦁	Attachments & Supporting Documents (Separate but related Entity)
Field Name	Fields	FileCategory
AttachmentList	List of {FileName, Description, FileType (Work, Excell, PPT, PDF, JPEG, JPG), BlobID, UploadedBy, UploadedOn, Links}	Supporting documents
List: Business Case, Cost Estimates, Business Requirement Document, Business Feasibility Report, Technical Feasibility Report, UAT Test Report, INSA Security Certificate, Security Clearance, Request Memo, Resource Assignment Memo

⦁	Communication & Collaboration (Separate but related Entity)
Field Name	Type	Description
Comments	Collection of {CommentID, AuthorID, CommentText, Timestamp}	Discussion thread attached to request
Feedback	Collection of {FeedbackID, AuthorID, FeedbackText, Rating (1 - 5), Timestamp} 	Any stakeholders’ feedback post-evaluation

⦁	Audit, History & Compliance Fields (Separate but related Entity)
Field Name	Type	Description
CreatedBy	AD User	Automatically populated from logged-in user
CreatedOn	DateTime	Submission time
ModifiedBy	AD User	Auto-updated
ModifiedOn	DateTime	Auto-updated
AuditTrailID	GUID	Linked to audit logs module
UpdateType	String	List (Creation, Update, Delete)
DataBeforeUpdate	string	Description of the data before update or delete
DataAfterUpdate	string	Description of the data After update


⦁	Workflow History (Separate but related Entity)
Field Name	Type	Description
WorkflowID	String	The ID that uniquely identify 
Workflowdecription	String	Description of the workflow
StartDate	DateTime	The workflow first initiated as active status
EndDate	DateTime	The workflow completed and transferred to Inactive status
WorkflowDuration	Integer/Number	Workflow Duration in Active Status
Status	String	Status of the Workflow
List: Active, Inactive
WorkflowOwner	AD User	

⦁	Status History (Separate but related Entity)
Field Name	Type	Description
StatusID	String	The ID that uniquely identify 
StatusDescription	String	Description/name of the status
StartDate	DateTime	The status first initiated
EndDate	DateTime	The status end date and time
StatusDuration	Integer/Number	Serialized state transitions for audit
Status	String	Status of the Workflow
List: Active, Inactive
StatusOwner	AD User	

⦁	Owner History (Separate but related Entity)
Field Name	Type	Description
OwnerID	String	The ID that uniquely identify 
OwnerName	AD User	The name of the owner
StartDate	DateTime	The Date and time the ownership started
EndDate	DateTime	The Date and time the ownership ended
OwnerDuration	Integer/Number	Ownership duration
OwnerRole	String	The role of the owner
List: Active, Inactive


⦁	Idea Intaking Work Flow
⦁	Request Intaking 
⦁	The Requestor: 
⦁	provide Request Summary 
⦁	Classify and Categorize the request 
⦁	Enrich the request with additional metadata 
⦁	Provide Requestor and Origin Details 
⦁	Attach Attachments & Supporting Documents 
⦁	Review and Submit the request for approval 
⦁	The VP/Director
⦁	Review and approve the request for approval 
⦁	Initial Evaluation of Ideas
⦁	Head of Product Management: Leads the review of business requests, assesses strategic alignment, and defines product goals by creating specific tasks, select and assign to reviewers.
⦁	Business Analyst: Gathers and clarifies the business requests, conducts the evaluation, and documents the product objectives and user stories. 
⦁	Innovation Chapter: Provides insights into innovative approaches and opportunities for enhancement. 
⦁	Subject Matter Expert (if engaged): Offers domain-specific expertise. 
⦁	Stakeholders: Provide the initial input, business requests, and feedback.
⦁	Idea Refinement and Prioritisation
⦁	Head of Product Management: Facilitates the refinement process and leads prioritization based on strategic goals by creating specific tasks, select and assign to reviewers. 
⦁	Head of Engineering: Provides technical insights into feasibility and helps refine the idea. 
⦁	Head of Data & AI: Ensures data-related considerations are integrated into the refinement process. 
⦁	Product Owner: Collaborates to break down the idea into user stories and align with development needs. 
⦁	Information Security (IS) Division – Vulnerability Assessment and Penetration Testing: Identifies security concerns early on. 
⦁	DevSecOps Team: Contributes automation and security feasibility insights (e.g., CI/CD potential), supporting IS Division and Head of Engineering. 
⦁	Subject Matter Expert (if engaged): Offers specific insights to refine the idea further. 
⦁	Deliver and Request for Project Ideation 

⦁	UI Screen Designs
⦁	Intake Screens and with required input
Screen No	Screen/Form Titles	Input Fields
1st	Request Summary
(the First screen to be displayed when Request intaking initiated)	Request ID (M)
Reference No
Title (M)
Description
2nd	Classification and Categorization	Request Type (M)
Request Category (M)
Service Category (M)
Product Category (M)
Business Impact (M)
Request Urgency (M)
Priority Level (M)
3rd	Additional Metadata	Strategic Alignment (M)
Estimated Cost
Estimated Benefit
Benefit Capture Duration
Proposed Delivery Date
Risk Level
Complexity Level
4th	Requestor and Origin Details	Requester Name (M)
Organization Name(M)
Sector(M)
Division(M)
Department(M)
Primary Contact Email(M)
Secondary Contact Email(M)
Primary Contact Phone(M)
Secondary Contact Phone(M)
5th	Attachments & Supporting Documents	Note describing required document types
attachment in tabular structure containing 
File Name	Description	Uploaded By	Uploaded On

6th	Review and Submit	Review of all provided data, 
Confirmation check,
Submission
⦁	Intake Screen - Sample Screen Structure
 