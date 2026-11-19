# AD2-FIXES checklist

## Database-free tests

- [x] Pipeline configuration and pure stage-progression tests pass.
- [x] Sensitive values are redacted from exception text while exception type, message, and stack remain.

## Runtime checks

- [ ] With a lead in an inactive configurable stage, the Pipeline board shows an inactive column with the provider's display name.
- [ ] The lead can move from that inactive column to the next active stage, or to the closing stage when no later active stage exists.
- [ ] Race a Tenant Admin stage-off save against a move into that stage; at most one operation commits, and a refused move shows the refresh message.
- [ ] Race a Tenant Admin stage-off save against Request Closed; a request using a stage that is no longer closing is refused with the refresh message.
- [ ] A save checks the all-tenant open-lead count and refuses to disable a stage with open leads.
- [ ] A Setting Version conflict still returns the clean concurrent-change message.
- [ ] Verify `AppLog` does not write connection strings or unredacted password, user ID, token, or API-key values.
- [ ] Trigger a failure in Login, Team Admin load, Parcel map load, and payment expansion; each shows a short non-sensitive message and the application stays open.

## Scope and migrations

- [ ] No schema change or migration was added.
- [ ] No API was started and no database was accessed.
