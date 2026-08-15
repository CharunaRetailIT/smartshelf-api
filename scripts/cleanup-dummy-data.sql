/* =============================================================================
   Removes everything seed-dummy-data.sql created.
   =============================================================================
   Matches on the DUMMY tag only, so nothing real is touched. Children are
   deleted before parents so the foreign keys stay satisfied.
   ========================================================================== */

SET NOCOUNT ON;

DELETE FROM QueueMaster        WHERE QueueType       LIKE 'DUMMY%' OR ErrorMessage LIKE 'DUMMY%';
DELETE FROM ShelfMaster        WHERE Name            LIKE 'DUMMY%';
DELETE FROM AisleMaster        WHERE Name            LIKE 'DUMMY%';
DELETE FROM ProductMaster      WHERE ProductCode     LIKE 'DUMMY%';
DELETE FROM ProductSubCategory WHERE SubCategoryCode LIKE 'DUMMY%';
DELETE FROM ProductCategory    WHERE CategoryCode    LIKE 'DUMMY%';
DELETE FROM MessageMaster      WHERE Title           LIKE 'DUMMY%';
DELETE FROM DeviceMaster       WHERE Name            LIKE 'DUMMY%';
DELETE FROM StoreMaster        WHERE StoreCode       LIKE 'DUMMY%';
DELETE FROM tbUsers            WHERE EmployeeId      LIKE 'DUMMY%';

SELECT 'StoreMaster'        AS TableName, COUNT(*) AS DummyRowsLeft FROM StoreMaster        WHERE StoreCode       LIKE 'DUMMY%'
UNION ALL SELECT 'ProductCategory',       COUNT(*) FROM ProductCategory    WHERE CategoryCode    LIKE 'DUMMY%'
UNION ALL SELECT 'ProductSubCategory',    COUNT(*) FROM ProductSubCategory WHERE SubCategoryCode LIKE 'DUMMY%'
UNION ALL SELECT 'ProductMaster',         COUNT(*) FROM ProductMaster      WHERE ProductCode     LIKE 'DUMMY%'
UNION ALL SELECT 'MessageMaster',         COUNT(*) FROM MessageMaster      WHERE Title           LIKE 'DUMMY%'
UNION ALL SELECT 'DeviceMaster',          COUNT(*) FROM DeviceMaster       WHERE Name            LIKE 'DUMMY%'
UNION ALL SELECT 'AisleMaster',           COUNT(*) FROM AisleMaster        WHERE Name            LIKE 'DUMMY%'
UNION ALL SELECT 'ShelfMaster',           COUNT(*) FROM ShelfMaster        WHERE Name            LIKE 'DUMMY%'
UNION ALL SELECT 'QueueMaster',           COUNT(*) FROM QueueMaster        WHERE ErrorMessage    LIKE 'DUMMY%'
UNION ALL SELECT 'tbUsers',               COUNT(*) FROM tbUsers            WHERE EmployeeId      LIKE 'DUMMY%';
