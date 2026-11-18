# AD1 fixes manual checklist

## Last active Tenant Admin concurrency

- [ ] Sign in as two Tenant Admins in separate sessions; concurrently deactivate each other's accounts. The operation may refuse one request with a clean concurrency message, but both accounts must never become inactive.
- [ ] Repeat with one request using a stale Staff Version; it receives a clean conflict message and the other admin remains active.
- [ ] Confirm ordinary staff deactivation/reactivation and branch changes retain their existing behavior.
