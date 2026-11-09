# Scoped integration changes

Original backend files remain untouched. The new controllers are:
- POST `/api/assets/validated` validates active `MockExternalAccounts` by provider+username+password before creating a credential Asset; DOCUMENT/OTHER bypass mock and get encrypted content.
- GET `/api/executor-inbox` and POST `/api/executor-inbox/{assignmentId}/respond` are restricted to the authenticated executor.

Existing endpoints and schema were not changed. Frontend: category mappings, shared Add Asset fields, beneficiary search using real UUID, delayed Will creation, executor inbox, async errors, and proper asset creation.

Not implemented: inheritance handover, payments, document uploads, identity verification. These are not simulated.

Please check production behavior with the actual Supabase schema and secrets; no live database or complete end-to-end run was available here.
