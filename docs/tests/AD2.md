# AD2: Configurable pipeline stages

- [ ] With no saved configuration, the board shows New, Contacted, Viewing, Negotiation, Closed, and Lost in that order.
- [ ] Rename Viewing; verify the new name appears on the board, in dialogs, and in old stage history.
- [ ] Activate Custom1 as “Site visit 2”, move it before Negotiation, and verify the board column order.
- [ ] Verify Request Closed is enabled only for leads in the last active configurable stage.
- [ ] Try to switch off a stage containing open leads; verify save is refused and reports the count.
- [ ] Empty Negotiation, switch it off, and verify the previous active stage becomes the closing stage.
- [ ] Try to switch off every configurable stage; verify the service refuses the save.
- [ ] Verify Agents and Branch Managers do not see Configure stages and the service refuses their saves.
- [ ] Save from two Tenant Admin sessions using the same expected version; verify the later save gets a conflict message.
- [ ] Sign out and back in; verify the saved stage configuration is loaded.
