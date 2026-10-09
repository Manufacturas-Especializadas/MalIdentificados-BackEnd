-- PENDING REVIEW AND MANUAL AUTHORIZATION. NOT EXECUTED.
-- First review 20261009_01_review_scanning_schema.sql and take a backup.
-- Assumes dbo; verify the target database and schema before execution.
-- Additive only: no backfill, defaults, indexes, product constraints or deletions.
-- The new API requires this column BEFORE it can be started against this database.
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @validations int = OBJECT_ID(N'dbo.ContainerValidations', N'U');
    DECLARE @lines int = OBJECT_ID(N'dbo.Lines', N'U');

    IF @validations IS NULL OR @lines IS NULL
        THROW 50001, 'Expected dbo.ContainerValidations and dbo.Lines. Review the schema.', 1;

    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = @lines AND name = N'Id'
          AND system_type_id = TYPE_ID(N'int') AND is_nullable = 0
    )
        THROW 50002, 'Lines.Id must be a non-nullable int. Review the schema.', 1;

    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = @validations AND name = N'containerNumber' AND is_nullable = 1
    ) OR NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = @validations AND name = N'shopOrder' AND is_nullable = 1
    )
        THROW 50003, 'Review existing containerNumber/shopOrder columns and nullability before enabling the new contract.', 1;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = @validations AND name = N'lineId')
        EXEC sys.sp_executesql N'ALTER TABLE dbo.ContainerValidations ADD lineId int NULL;';

    DECLARE @lineColumn int = (
        SELECT column_id FROM sys.columns
        WHERE object_id = @validations AND name = N'lineId'
          AND system_type_id = TYPE_ID(N'int') AND is_nullable = 1
          AND is_computed = 0 AND default_object_id = 0
    );

    IF @lineColumn IS NULL
        THROW 50004, 'Existing lineId must be nullable int without a default. No automatic correction was attempted.', 1;

    -- Refuse conflicting, composite, disabled or untrusted existing relationships.
    IF EXISTS (
        SELECT 1 FROM sys.foreign_key_columns fkc
        JOIN sys.foreign_keys fk ON fk.object_id = fkc.constraint_object_id
        WHERE fkc.parent_object_id = @validations AND fkc.parent_column_id = @lineColumn
          AND (fkc.referenced_object_id <> @lines
            OR COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) <> N'Id'
            OR fk.delete_referential_action <> 0 OR fk.update_referential_action <> 0
            OR fk.is_disabled = 1 OR fk.is_not_trusted = 1
            OR (SELECT COUNT(*) FROM sys.foreign_key_columns cols
                WHERE cols.constraint_object_id = fk.object_id) <> 1)
    )
        THROW 50005, 'Existing lineId relationship differs from the expected trusted NO ACTION foreign key.', 1;

    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_key_columns
        WHERE parent_object_id = @validations AND parent_column_id = @lineColumn
    )
    BEGIN
        EXEC sys.sp_executesql N'
            ALTER TABLE dbo.ContainerValidations WITH CHECK
            ADD CONSTRAINT FK_ContainerValidations_Lines_lineId
            FOREIGN KEY (lineId) REFERENCES dbo.Lines (Id)
            ON DELETE NO ACTION ON UPDATE NO ACTION;';
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

-- Rollback policy: revert the application while leaving this additive nullable
-- column in place. Do not drop it after new records have been written.
