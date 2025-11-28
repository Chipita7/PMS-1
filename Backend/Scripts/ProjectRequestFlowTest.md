## Project Request Flow & Status Coverage Test Plan

This checklist exercises every workflow scenario from the **Idea Intake Object Model** and ensures the new backend logic (auto/manual reviews + full status coverage) behaves as designed.

---

### 1. Base Setup (applies to every scenario)
1. **Create request**
   ```http
   POST /api/ProjectRequests
   {
     "RequestTitle": "Mobile Payment Gateway Integration",
     "RequestDescription": "Testing full intake flow",
     "ReferenceNo": "TEST-001",
     "RequestTypeConfigId": 1,
     "RequestCategoryConfigId": 1,
     "ServiceCategoryConfigId": 1,
     "ProductCategoryConfigId": 1,
     "PriorityConfigId": 1,
     "BusinessImpactConfigId": 1,
     "RequestUrgencyConfigId": 1,
     "StrategicAlignmentConfigId": 1,
     "RequestedByName": "Jane Doe",
     "BusinessSector": "Digital Banking",
     "BusinessDivision": "Technology",
     "BusinessDepartment": "Digital Factory",
     "OrganizationName": "Commercial Bank of Ethiopia",
     "PrimaryContactEmail": "jane.doe@cbe.com.et",
     "PrimaryContactPhone": "+251900000000"
   }
   ```
   *Expected:* Status = `SUBMITTED`, Stage = `SUBMITTED`.

2. **Verify configuration**  
   `GET /api/ProjectRequests/configurations` – note the IDs for statuses, roles, stages if needed.

---

### 2. Automatic Review Task Scenario
1. **Assign all evaluators _and_ review roles up front**
   ```http
   POST /api/ProjectRequests/{id}/assign
   { "AssigneeId": "head.pm@cbe.com.et", "AssigneeRole": "Head of Product Management" }

   POST ... { "AssigneeRole": "Business Analyst" }
   POST ... { "AssigneeRole": "Innovation Chapter" }
   POST ... { "AssigneeRole": "Subject Matter Expert" }

   POST ... { "AssigneeRole": "Head of Engineering" }
   POST ... { "AssigneeRole": "Head of Data & AI" }
   POST ... { "AssigneeRole": "Product Owner" }
   POST ... { "AssigneeRole": "Information Security" }
   POST ... { "AssigneeRole": "DevSecOps Team" }
   ```
   *Expected:* Status automatically flips to `UNDER_EVALUATION`, Stage = `INITIAL_EVAL`.

2. **Submit evaluations for each evaluator**
   ```http
   POST /api/Evaluations
   {
     "ProjectRequestId": {id},
     "EvaluatorID": "head.pm@cbe.com.et",
     "FeasibilityScore": 8,
     "BusinessValueScore": 8,
     "TechnicalComplexityScore": 6,
     "EvaluationRemarks": "Aligned with strategy"
   }
   ```
   (Repeat for BA, Innovation Chapter, SME.)

3. **After last evaluation**
   - `GET /api/ProjectRequests/{id}` → Stage should be `IDEA_REFINEMENT`.
   - `GET /api/ProjectRequests/{id}/review-tasks` → Review tasks exist for *every* reviewer role assigned above.

4. **Complete each review task**
   ```http
   POST /api/ReviewTasks/{taskId}/complete
   {
     "Decision": "Approve",
     "ReviewerId": "head.engineering@cbe.com.et",
     "Remarks": "Technically feasible"
   }
   ```
   When all are completed, Stage auto-moves to `APPROVAL_READY`.

5. **Approve**
   ```http
   POST /api/ProjectRequests/{id}/approve
   { "remarks": "Green light" }
   ```
   *Expected:* Status = `APPROVED`, Stage = `APPROVED_STAGE`.

---

### 3. Manual Review Task Scenario
1. Follow steps 1–2 above but **only assign evaluators** initially.
2. After reaching `IDEA_REFINEMENT`, assign ad-hoc reviewers:
   ```http
   POST /api/ProjectRequests/{id}/assign
   { "AssigneeId": "product.owner@cbe.com.et", "AssigneeRole": "Product Owner" }
   ```
3. Generate manual review task (when needed):
   ```http
   POST /api/ReviewTasks/request/{id}/manual
   {
     "AssigneeId": "product.owner@cbe.com.et",
     "AssigneeName": "Product Owner",
     "AssigneeRole": "Product Owner",
     "TaskDescription": "Break down user stories",
     "DueDate": "2025-12-01T00:00:00Z"
   }
   ```
4. Complete manually-created task via `/complete` endpoint – Stage transition rules remain the same.

---

### 4. Status Coverage Scenarios

| Status Code    | How to Trigger                                                                                          | API / Notes |
|----------------|---------------------------------------------------------------------------------------------------------|-------------|
| `SUBMITTED`    | Automatic on create                                                                                     | Step 1 |
| `UNDER_EVALUATION` | Auto when first evaluator or reviewer is assigned                                                     | Happens inside `POST /assign` |
| `BACKLOGGED`   | Business defers request                                                                                 | `POST /api/ProjectRequests/{id}/backlog` `{ "Reason": "Waiting for dependencies" }` |
| `UNDER_EVALUATION` (reactivate) | Bring back a backlogged idea                                                                 | `POST /api/ProjectRequests/{id}/reactivate` |
| `APPROVED`     | After evaluations + reviews + final approval                                                            | `POST /api/ProjectRequests/{id}/approve` |
| `REJECTED`     | Early rejection with reason                                                                             | `POST /api/ProjectRequests/{id}/reject` `{ "remarks": "...", "reason": "NotAligned" }` |
| `IN_PROGRESS`  | After approval, start execution                                                                         | `POST /api/ProjectRequests/{id}/start-execution` (requires current status = `APPROVED`) |
| `COMPLETED`    | Finish development/testing                                                                              | `POST /api/ProjectRequests/{id}/mark-completed` (requires status `IN_PROGRESS`) |
| `DELIVERED`    | Handover to business                                                                                    | `POST /api/ProjectRequests/{id}/mark-delivered` (requires `IN_PROGRESS` or `COMPLETED`) |
| `CLOSED`       | Final closure once delivered/completed                                                                  | `POST /api/ProjectRequests/{id}/close` (requires `COMPLETED` or `DELIVERED`) |

*Verification Tip:* After each call, run `GET /api/ProjectRequests/{id}` and confirm `statusConfig.name`, `workflowStageConfig.name`, and `statusHistory/workflowHistory` entries.

---

### 5. Backlog + Reactivation Regression
1. Create request, assign evaluators.
2. `POST /api/ProjectRequests/{id}/backlog { "reason": "Awaiting budget" }`
   - Status = `BACKLOGGED`, stage resets to `SUBMITTED`.
3. `POST /api/ProjectRequests/{id}/reactivate`
   - Status returns to `UNDER_EVALUATION`, stage = `INITIAL_EVAL`.
4. Continue with standard flow: evaluations, reviews, approval.

---

### 6. Manual Close-out Path
Use this when a request needs to skip delivery (e.g., pilot cancelled after approval):
1. Approve request.
2. `POST /api/ProjectRequests/{id}/start-execution`
3. `POST /api/ProjectRequests/{id}/close`
   - Status = `CLOSED`, stage = `COMPLETED`.

---

### 7. Quick Reference – Key Endpoints
- `POST /api/ProjectRequests` – create
- `POST /api/ProjectRequests/{id}/assign` – assign any role
- `POST /api/Evaluations` – evaluator submission
- `GET /api/ProjectRequests/{id}/review-tasks` – view review tasks
- `POST /api/ReviewTasks/{taskId}/complete` – finish tasks
- `POST /api/ProjectRequests/{id}/approve` / `reject`
- `POST /api/ProjectRequests/{id}/backlog` / `reactivate`
- `POST /api/ProjectRequests/{id}/start-execution`
- `POST /api/ProjectRequests/{id}/mark-completed`
- `POST /api/ProjectRequests/{id}/mark-delivered`
- `POST /api/ProjectRequests/{id}/close`

Following this script validates every workflow stage, every status code in the Idea Intake spec, and both the auto/manual review capabilities now supported by the backend.

