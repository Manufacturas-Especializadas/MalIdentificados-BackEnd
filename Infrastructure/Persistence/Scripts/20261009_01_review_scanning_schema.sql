-- READ-ONLY REVIEW. Prepared only; not executed by the application or tests.
-- Run manually against an explicitly selected database after authorization.
-- Confirm dbo is the application's schema before using the companion change script.
SELECT DB_NAME() AS databaseName;

SELECT s.name AS schemaName, t.name AS tableName, c.name AS columnName,
       ty.name AS typeName, c.max_length, c.is_nullable, dc.definition AS defaultValue
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.columns c ON c.object_id = t.object_id
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
LEFT JOIN sys.default_constraints dc ON dc.object_id = c.default_object_id
WHERE t.name IN (N'Lines', N'PartNumbers', N'ContainerValidations', N'ScanDetails')
ORDER BY s.name, t.name, c.column_id;

SELECT OBJECT_SCHEMA_NAME(i.object_id) AS schemaName,
       OBJECT_NAME(i.object_id) AS tableName, i.name AS indexName,
       i.is_unique, i.filter_definition, c.name AS columnName, ic.key_ordinal
FROM sys.indexes i
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.object_id IN (OBJECT_ID(N'dbo.PartNumbers'), OBJECT_ID(N'dbo.ContainerValidations'))
ORDER BY tableName, indexName, ic.key_ordinal;

SELECT fk.name, OBJECT_NAME(fk.parent_object_id) AS tableName,
       pc.name AS columnName, OBJECT_NAME(fk.referenced_object_id) AS referencedTable,
       rc.name AS referencedColumn, fk.delete_referential_action_desc,
       fk.is_disabled, fk.is_not_trusted
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
JOIN sys.columns pc ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
JOIN sys.columns rc ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
WHERE fk.parent_object_id = OBJECT_ID(N'dbo.ContainerValidations');

SELECT l.Id, l.lineName, l.isActive, COUNT(p.Id) AS catalogRows,
       SUM(CASE WHEN p.isActive = 1 THEN 1 ELSE 0 END) AS activeCatalogRows
FROM dbo.Lines l
LEFT JOIN dbo.PartNumbers p ON p.idLine = l.Id
GROUP BY l.Id, l.lineName, l.isActive;

SELECT idLine, partNumbers, COUNT(*) AS activeMatches
FROM dbo.PartNumbers
WHERE isActive = 1
GROUP BY idLine, partNumbers
HAVING COUNT(*) > 1;

-- Only a coverage indicator. Historical codes cannot prove future catalog coverage
-- or identify the actual line that produced any historical validation.
SELECT v.expectedPartCode, COUNT_BIG(*) AS historicalValidationsWithoutActiveCatalogMatch
FROM dbo.ContainerValidations v
WHERE v.expectedPartCode IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM dbo.PartNumbers p
      WHERE p.partNumbers = v.expectedPartCode AND p.isActive = 1
  )
GROUP BY v.expectedPartCode;
