/* =============================================================================
   Dummy list data - 50 rows per list, for UI review only.
   =============================================================================

   NOTHING HERE TOUCHES MINEW. These are plain local INSERTs; no API call and
   no sync/bind path is involved. Every row is also created in a state the
   cloud paths ignore:

     StoreMaster    IsSynced = 0, SyncStatus = 'pending', MinewStoreId = NULL
     ProductMaster  IsSyncToCloud = 0
     DeviceMaster   MinewDeviceId = NULL, IsOnline = 0
     ShelfMaster    IsSyncToCloud = 0, MinewDeviceId = NULL
     QueueMaster    StatusId = 8 (Failed)

   That last one matters most: QueueProcessorService is a BackgroundService
   that activates queues, and activating one binds to Minew. It only ever
   selects StatusId 5 (Pending) and 7 (Completed), so Failed rows are inert and
   can never be picked up and pushed.

   Every row is tagged DUMMY so it can be identified and removed - see
   cleanup-dummy-data.sql.

   Re-running this script is safe: it deletes its own rows first.
   ========================================================================== */

SET NOCOUNT ON;

DECLARE @StoreId   BIGINT = 3;    -- Test STore, the current default store
DECLARE @UserId    INT    = 1;
DECLARE @Now       DATETIME2 = SYSUTCDATETIME();

/* 50 numbers to drive every insert. */
;WITH n AS (
    SELECT TOP (50) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i
    FROM sys.all_objects
)
SELECT i INTO #n FROM n;

/* ---------------------------------------------------------------------------
   Clean out any previous run, children first so the FKs stay happy.
   --------------------------------------------------------------------------- */
DELETE FROM QueueMaster        WHERE QueueType    LIKE 'DUMMY%' OR ErrorMessage LIKE 'DUMMY%';
DELETE FROM ShelfMaster        WHERE Name         LIKE 'DUMMY%';
DELETE FROM AisleMaster        WHERE Name         LIKE 'DUMMY%';
DELETE FROM ProductMaster      WHERE ProductCode  LIKE 'DUMMY%';
DELETE FROM ProductSubCategory WHERE SubCategoryCode LIKE 'DUMMY%';
DELETE FROM ProductCategory    WHERE CategoryCode LIKE 'DUMMY%';
DELETE FROM MessageMaster      WHERE Title        LIKE 'DUMMY%';
DELETE FROM DeviceMaster       WHERE Name         LIKE 'DUMMY%';
DELETE FROM StoreMaster        WHERE StoreCode    LIKE 'DUMMY%';
DELETE FROM tbUsers            WHERE EmployeeId   LIKE 'DUMMY%';

/* ---------------------------------------------------------------------------
   Stores
   --------------------------------------------------------------------------- */
INSERT INTO StoreMaster
    (MinewStoreId, StoreName, StoreCode, StoreType, Address, Phone, Email,
     ContactPerson, IsActive, IsSynced, SyncStatus, CreatedDate, CreatedUser)
SELECT
    NULL,
    CONCAT('DUMMY Store ', i),
    CONCAT('DUMMY-ST-', RIGHT('000' + CAST(i AS varchar(3)), 3)),
    'local',   -- CK_StoreMaster_StoreType_New allows only 'local' | 'minew';
               -- dummy stores are local so none implies a cloud counterpart
    CONCAT(i, ' Galle Road, Colombo ', (i % 15) + 1),
    CONCAT('011', RIGHT('0000000' + CAST(i * 7919 % 10000000 AS varchar(7)), 7)),
    CONCAT('store', i, '@example.invalid'),
    CONCAT('Contact Person ', i),
    CASE WHEN i % 7 = 0 THEN 0 ELSE 1 END,
    0, 'pending', @Now, @UserId
FROM #n;

/* ---------------------------------------------------------------------------
   Product categories / subcategories / products
   --------------------------------------------------------------------------- */
INSERT INTO ProductCategory
    (CategoryName, CategoryCode, CategoryDescription, IsActive, CreatedDate, CreatedUser, StoreId)
SELECT
    CONCAT('DUMMY Category ', i),
    CONCAT('DUMMY-CAT-', RIGHT('000' + CAST(i AS varchar(3)), 3)),
    CONCAT('Sample category ', i, ' for UI review'),
    CASE WHEN i % 9 = 0 THEN 0 ELSE 1 END,
    @Now, @UserId, @StoreId
FROM #n;

INSERT INTO ProductSubCategory
    (CategoryId, SubCategoryName, SubCategoryCode, SubCategoryDescription,
     IsActive, CreatedDate, CreatedUser, StoreId)
SELECT
    (SELECT MIN(Id) FROM ProductCategory WHERE CategoryCode LIKE 'DUMMY%'),
    CONCAT('DUMMY Subcategory ', i),
    CONCAT('DUMMY-SUB-', RIGHT('000' + CAST(i AS varchar(3)), 3)),
    CONCAT('Sample subcategory ', i),
    CASE WHEN i % 8 = 0 THEN 0 ELSE 1 END,
    @Now, @UserId, @StoreId
FROM #n;

INSERT INTO ProductMaster
    (ProductCode, BarCode, ProductName, CategoryId, SubCategoryId, Quantity,
     UnitOfMeasure, CostPrice, SellingPrice, DiscountPrice, DiscountPercentage,
     DiscountedPrice, WholesalePrice, MinimumPrice, MaximumPrice, Description,
     IsActive, CreatedDate, CreatedUser, IsSyncToCloud, StoreId)
SELECT
    CONCAT('DUMMY-PRD-', RIGHT('000' + CAST(i AS varchar(3)), 3)),
    CONCAT('49', RIGHT('00000000000' + CAST(i * 104729 AS varchar(11)), 11)),
    CONCAT('DUMMY Product ', i),
    (SELECT MIN(Id) FROM ProductCategory    WHERE CategoryCode    LIKE 'DUMMY%'),
    (SELECT MIN(Id) FROM ProductSubCategory WHERE SubCategoryCode LIKE 'DUMMY%'),
    (i % 40) + 1,
    CASE i % 3 WHEN 0 THEN 'pcs' WHEN 1 THEN 'kg' ELSE 'pack' END,
    100.00 + i,  250.00 + (i * 10),
    CASE WHEN i % 4 = 0 THEN 25.00 ELSE 0.00 END,
    CASE WHEN i % 4 = 0 THEN 10.00 ELSE 0.00 END,
    CASE WHEN i % 4 = 0 THEN 225.00 + (i * 10) ELSE 0.00 END,
    200.00 + (i * 8), 150.00 + i, 400.00 + (i * 12),
    CONCAT('Sample product ', i, ' for UI review'),
    CASE WHEN i % 11 = 0 THEN 0 ELSE 1 END,
    @Now, @UserId, 0, @StoreId
FROM #n;

/* ---------------------------------------------------------------------------
   Messages
   --------------------------------------------------------------------------- */
INSERT INTO MessageMaster
    (Title, ContentType, ContentData, Duration, IsActive, CreatedDate,
     CreatedUser, StoreId, ScreenSizeId)
SELECT
    CONCAT('DUMMY Message ', i),
    ((i - 1) % 4) + 1,
    CONCAT('Sample message body ', i),
    (i % 30) + 5,
    CASE WHEN i % 6 = 0 THEN 0 ELSE 1 END,
    @Now, @UserId, @StoreId,
    (SELECT MIN(Id) FROM DeviceScreens)
FROM #n;

/* ---------------------------------------------------------------------------
   Devices - never given a MinewDeviceId, so nothing addresses the cloud
   --------------------------------------------------------------------------- */
INSERT INTO DeviceMaster
    (MACAddress, IPAddress, NetworkName, Name, Description, StatusId, DeviceType,
     StoreId, MinewDeviceId, ScreenColor, Firmware, Hardware, Battery,
     IsActive, IsOnline, CreatedDate, CreatedUser, ScreenId)
SELECT
    CONCAT('DD:MY:00:', RIGHT('00' + CAST(i AS varchar(2)), 2), ':',
           RIGHT('00' + CAST((i * 3) % 100 AS varchar(2)), 2), ':01'),
    CONCAT('192.168.50.', (i % 250) + 1),
    'DUMMY-NET',
    CONCAT('DUMMY Device ', i),
    CONCAT('Sample device ', i, ' for UI review'),
    CASE WHEN i % 5 = 0 THEN 2 ELSE 1 END,
    'ESL',
    @StoreId, NULL,
    CASE i % 3 WHEN 0 THEN 'BWR' WHEN 1 THEN 'BW' ELSE 'BWY' END,
    '1.0.0', 'HW-2.9', (i * 2 % 100),
    CASE WHEN i % 10 = 0 THEN 0 ELSE 1 END,
    0, @Now, @UserId,
    (SELECT MIN(Id) FROM DeviceScreens)
FROM #n;

/* ---------------------------------------------------------------------------
   Racks (AisleMaster) and shelves
   --------------------------------------------------------------------------- */
INSERT INTO AisleMaster
    (Name, Description, Location, Coordinates, IsActive, CreatedDate, CreatedUser, StoreId)
SELECT
    CONCAT('DUMMY Rack ', i),
    CONCAT('Sample rack ', i),
    CONCAT('Floor ', (i % 4) + 1, ' - Aisle ', (i % 12) + 1),
    CONCAT('x:', i * 3, ', y:', i * 5),
    CASE WHEN i % 7 = 0 THEN 0 ELSE 1 END,
    @Now, @UserId, @StoreId
FROM #n;

INSERT INTO ShelfMaster
    (AisleId, Name, Location, Coordinates, Description, IsActive, CreatedDate,
     CreatedUser, MinewDeviceId, IsSyncToCloud, StoreId)
SELECT
    (SELECT MIN(Id) FROM AisleMaster WHERE Name LIKE 'DUMMY%'),
    CONCAT('DUMMY Shelf ', i),
    CONCAT('Level ', (i % 5) + 1),
    CONCAT('x:', i, ', y:', i * 2),
    CONCAT('Sample shelf ', i),
    CASE WHEN i % 6 = 0 THEN 0 ELSE 1 END,
    @Now, @UserId, NULL, 0, @StoreId
FROM #n;

/* ---------------------------------------------------------------------------
   Queues - StatusId 8 (Failed) so QueueProcessorService never picks them up
   --------------------------------------------------------------------------- */
INSERT INTO QueueMaster
    (DeviceId, ProductId, ShelfId, StartDate, EndDate, IsActive, DisplayOrder,
     CreatedDate, CreatedUser, StatusId, PriorityId, QueueType, LocationType,
     LocationId, ErrorMessage, RetryCount, MaxRetries, IsRecurring, StoreId)
SELECT
    (SELECT MIN(Id) FROM DeviceMaster  WHERE Name        LIKE 'DUMMY%'),
    -- CHK_QueueMaster_Location: LocationType 'PRODUCT' requires ProductId set
    -- and ShelfId null.
    (SELECT MIN(Id) FROM ProductMaster WHERE ProductCode LIKE 'DUMMY%'),
    NULL,
    DATEADD(HOUR, -i, @Now),
    NULL,
    1, i, @Now, @UserId,
    8,                              -- Failed: inert, never activated or bound
    ((i - 1) % 5) + 1,
    CASE i % 2 WHEN 0 THEN 'TEMPLATE_QUEUE' ELSE 'MESSAGE_QUEUE' END,
    'PRODUCT',
    (SELECT MIN(Id) FROM ProductMaster WHERE ProductCode LIKE 'DUMMY%'),
    'DUMMY seeded row - not a real failure',
    0, 3, 0, @StoreId
FROM #n;

/* ---------------------------------------------------------------------------
   Users - PasswordHash is deliberately not a valid hash, so none of these
   accounts can be signed in to.
   --------------------------------------------------------------------------- */
INSERT INTO tbUsers
    (FirstName, LastName, UserName, RoleId, Email, DepartmentId, Address1,
     EmployeeId, PasswordHash, IsActive, IsNew, IsDeleted, CreatedDate, StoreId)
SELECT
    CONCAT('Dummy', i),
    CONCAT('User', i),
    CONCAT('dummy.user', i),
    ((i - 1) % 4) + 1,
    CONCAT('dummy.user', i, '@example.invalid'),
    ((i - 1) % 6) + 1,
    CONCAT(i, ' Sample Street, Colombo'),
    CONCAT('DUMMY-EMP-', RIGHT('000' + CAST(i AS varchar(3)), 3)),
    'DUMMY-NO-LOGIN',           -- not a valid hash; cannot authenticate
    CASE WHEN i % 8 = 0 THEN 0 ELSE 1 END,
    0, 0, @Now, @StoreId
FROM #n;

DROP TABLE #n;

/* ---------------------------------------------------------------------------
   Report
   --------------------------------------------------------------------------- */
SELECT 'StoreMaster'        AS TableName, COUNT(*) AS DummyRows FROM StoreMaster        WHERE StoreCode       LIKE 'DUMMY%'
UNION ALL SELECT 'ProductCategory',       COUNT(*) FROM ProductCategory    WHERE CategoryCode    LIKE 'DUMMY%'
UNION ALL SELECT 'ProductSubCategory',    COUNT(*) FROM ProductSubCategory WHERE SubCategoryCode LIKE 'DUMMY%'
UNION ALL SELECT 'ProductMaster',         COUNT(*) FROM ProductMaster      WHERE ProductCode     LIKE 'DUMMY%'
UNION ALL SELECT 'MessageMaster',         COUNT(*) FROM MessageMaster      WHERE Title           LIKE 'DUMMY%'
UNION ALL SELECT 'DeviceMaster',          COUNT(*) FROM DeviceMaster       WHERE Name            LIKE 'DUMMY%'
UNION ALL SELECT 'AisleMaster',           COUNT(*) FROM AisleMaster        WHERE Name            LIKE 'DUMMY%'
UNION ALL SELECT 'ShelfMaster',           COUNT(*) FROM ShelfMaster        WHERE Name            LIKE 'DUMMY%'
UNION ALL SELECT 'QueueMaster',           COUNT(*) FROM QueueMaster        WHERE ErrorMessage    LIKE 'DUMMY%'
UNION ALL SELECT 'tbUsers',               COUNT(*) FROM tbUsers            WHERE EmployeeId      LIKE 'DUMMY%';
