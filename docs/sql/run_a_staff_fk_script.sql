BEGIN TRANSACTION;
DECLARE @count int;
DECLARE @message nvarchar(2048);

SET @count = (SELECT COUNT(*) FROM Contacts c LEFT JOIN Staffs s ON s.Id = c.AssignedStaffId
              WHERE c.AssignedStaffId IS NOT NULL AND s.Id IS NULL);
IF @count > 0 BEGIN SET @message = CONCAT('Run A Part B blocked: Contacts.AssignedStaffId has ', @count, ' orphan IDs.'); THROW 51101, @message, 1; END;

SET @count = (SELECT COUNT(*) FROM Contacts c LEFT JOIN Staffs s ON s.Id = c.ReferrerStaffId
              WHERE c.ReferrerStaffId IS NOT NULL AND s.Id IS NULL);
IF @count > 0 BEGIN SET @message = CONCAT('Run A Part B blocked: Contacts.ReferrerStaffId has ', @count, ' orphan IDs.'); THROW 51102, @message, 1; END;

SET @count = (SELECT COUNT(*) FROM Commissions c LEFT JOIN Staffs s ON s.Id = c.RecipientStaffId
              WHERE c.RecipientStaffId IS NOT NULL AND s.Id IS NULL);
IF @count > 0 BEGIN SET @message = CONCAT('Run A Part B blocked: Commissions.RecipientStaffId has ', @count, ' orphan IDs.'); THROW 51103, @message, 1; END;

SET @count = (SELECT COUNT(*) FROM Deals d LEFT JOIN Staffs s ON s.Id = d.AgentStaffId
              WHERE d.AgentStaffId IS NOT NULL AND s.Id IS NULL);
IF @count > 0 BEGIN SET @message = CONCAT('Run A Part B blocked: Deals.AgentStaffId has ', @count, ' orphan IDs.'); THROW 51104, @message, 1; END;

SET @count = (SELECT COUNT(*) FROM Deals d LEFT JOIN Staffs s ON s.Id = d.ConfirmedByStaffId
              WHERE d.ConfirmedByStaffId IS NOT NULL AND s.Id IS NULL);
IF @count > 0 BEGIN SET @message = CONCAT('Run A Part B blocked: Deals.ConfirmedByStaffId has ', @count, ' orphan IDs.'); THROW 51105, @message, 1; END;

SET @count = (SELECT COUNT(*) FROM Documents d LEFT JOIN Staffs s ON s.Id = d.UploadedByStaffId
              WHERE d.UploadedByStaffId IS NOT NULL AND s.Id IS NULL);
IF @count > 0 BEGIN SET @message = CONCAT('Run A Part B blocked: Documents.UploadedByStaffId has ', @count, ' orphan IDs.'); THROW 51106, @message, 1; END;

SET @count = (SELECT COUNT(*) FROM DuplicateInquiries i LEFT JOIN Staffs s ON s.Id = i.RecordedByStaffId
              WHERE i.RecordedByStaffId IS NOT NULL AND s.Id IS NULL);
IF @count > 0 BEGIN SET @message = CONCAT('Run A Part B blocked: DuplicateInquiries.RecordedByStaffId has ', @count, ' orphan IDs.'); THROW 51107, @message, 1; END;

SET @count = (SELECT COUNT(*) FROM Interactions i LEFT JOIN Staffs s ON s.Id = i.LoggedByStaffId
              WHERE i.LoggedByStaffId IS NOT NULL AND s.Id IS NULL);
IF @count > 0 BEGIN SET @message = CONCAT('Run A Part B blocked: Interactions.LoggedByStaffId has ', @count, ' orphan IDs.'); THROW 51108, @message, 1; END;

SET @count = (SELECT COUNT(*) FROM Leads l LEFT JOIN Staffs s ON s.Id = l.AssignedStaffId
              WHERE l.AssignedStaffId IS NOT NULL AND s.Id IS NULL);
IF @count > 0 BEGIN SET @message = CONCAT('Run A Part B blocked: Leads.AssignedStaffId has ', @count, ' orphan IDs.'); THROW 51109, @message, 1; END;

SET @count = (SELECT COUNT(*) FROM Parcels p LEFT JOIN Staffs s ON s.Id = p.AssignedStaffId
              WHERE p.AssignedStaffId IS NOT NULL AND s.Id IS NULL);
IF @count > 0 BEGIN SET @message = CONCAT('Run A Part B blocked: Parcels.AssignedStaffId has ', @count, ' orphan IDs.'); THROW 51110, @message, 1; END;

SET @count = (SELECT COUNT(*) FROM Payments p LEFT JOIN Staffs s ON s.Id = p.RecordedByStaffId
              WHERE p.RecordedByStaffId IS NOT NULL AND s.Id IS NULL);
IF @count > 0 BEGIN SET @message = CONCAT('Run A Part B blocked: Payments.RecordedByStaffId has ', @count, ' orphan IDs.'); THROW 51111, @message, 1; END;

SET @count = (SELECT COUNT(*) FROM Payments p LEFT JOIN Staffs s ON s.Id = p.ConfirmedByStaffId
              WHERE p.ConfirmedByStaffId IS NOT NULL AND s.Id IS NULL);
IF @count > 0 BEGIN SET @message = CONCAT('Run A Part B blocked: Payments.ConfirmedByStaffId has ', @count, ' orphan IDs.'); THROW 51112, @message, 1; END;

SET @count = (SELECT COUNT(*) FROM ReferralBonuses r LEFT JOIN Staffs s ON s.Id = r.ReferrerStaffId
              WHERE r.ReferrerStaffId IS NOT NULL AND s.Id IS NULL);
IF @count > 0 BEGIN SET @message = CONCAT('Run A Part B blocked: ReferralBonuses.ReferrerStaffId has ', @count, ' orphan IDs.'); THROW 51113, @message, 1; END;

SET @count = (SELECT COUNT(*) FROM StageHistories h LEFT JOIN Staffs s ON s.Id = h.ChangedByStaffId
              WHERE h.ChangedByStaffId IS NOT NULL AND s.Id IS NULL);
IF @count > 0 BEGIN SET @message = CONCAT('Run A Part B blocked: StageHistories.ChangedByStaffId has ', @count, ' orphan IDs.'); THROW 51114, @message, 1; END;

ALTER TABLE [Contacts] ADD CONSTRAINT [FK_Contacts_Staffs_AssignedStaffId] FOREIGN KEY ([AssignedStaffId]) REFERENCES [Staffs] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [Contacts] ADD CONSTRAINT [FK_Contacts_Staffs_ReferrerStaffId] FOREIGN KEY ([ReferrerStaffId]) REFERENCES [Staffs] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [Commissions] ADD CONSTRAINT [FK_Commissions_Staffs_RecipientStaffId] FOREIGN KEY ([RecipientStaffId]) REFERENCES [Staffs] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [Deals] ADD CONSTRAINT [FK_Deals_Staffs_AgentStaffId] FOREIGN KEY ([AgentStaffId]) REFERENCES [Staffs] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [Deals] ADD CONSTRAINT [FK_Deals_Staffs_ConfirmedByStaffId] FOREIGN KEY ([ConfirmedByStaffId]) REFERENCES [Staffs] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [Documents] ADD CONSTRAINT [FK_Documents_Staffs_UploadedByStaffId] FOREIGN KEY ([UploadedByStaffId]) REFERENCES [Staffs] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [DuplicateInquiries] ADD CONSTRAINT [FK_DuplicateInquiries_Staffs_RecordedByStaffId] FOREIGN KEY ([RecordedByStaffId]) REFERENCES [Staffs] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [Interactions] ADD CONSTRAINT [FK_Interactions_Staffs_LoggedByStaffId] FOREIGN KEY ([LoggedByStaffId]) REFERENCES [Staffs] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [Leads] ADD CONSTRAINT [FK_Leads_Staffs_AssignedStaffId] FOREIGN KEY ([AssignedStaffId]) REFERENCES [Staffs] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [Parcels] ADD CONSTRAINT [FK_Parcels_Staffs_AssignedStaffId] FOREIGN KEY ([AssignedStaffId]) REFERENCES [Staffs] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [Payments] ADD CONSTRAINT [FK_Payments_Staffs_RecordedByStaffId] FOREIGN KEY ([RecordedByStaffId]) REFERENCES [Staffs] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [Payments] ADD CONSTRAINT [FK_Payments_Staffs_ConfirmedByStaffId] FOREIGN KEY ([ConfirmedByStaffId]) REFERENCES [Staffs] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [ReferralBonuses] ADD CONSTRAINT [FK_ReferralBonuses_Staffs_ReferrerStaffId] FOREIGN KEY ([ReferrerStaffId]) REFERENCES [Staffs] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [StageHistories] ADD CONSTRAINT [FK_StageHistories_Staffs_ChangedByStaffId] FOREIGN KEY ([ChangedByStaffId]) REFERENCES [Staffs] ([Id]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261117130000_RunA_StaffForeignKeys', N'10.0.15');

COMMIT;
GO

