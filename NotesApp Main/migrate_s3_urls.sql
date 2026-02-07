-- Migration Script: Update Paths to Full S3 URLs
-- Run this to convert all relative paths to full URLs in database

-- =========================================================
-- STEP 1: Update Users Profile Image Paths
-- =========================================================
UPDATE Users
SET ProfileImagePath = 'https://testing-akshay-cm.s3.eu-south-1.amazonaws.com/' + ProfileImagePath
WHERE ProfileImagePath IS NOT NULL 
  AND ProfileImagePath <> ''
  AND ProfileImagePath NOT LIKE 'http%'
  AND LEN(ProfileImagePath) > 0;

-- =========================================================
-- STEP 2: Update Notes Attachments (JSON Arrays)
-- =========================================================
-- Since FilePaths and ImagePaths are stored as JSON ["path1", "path2"]
-- We need to replace '"' with '"https://.../' for relative paths inside JSON
-- Note: This is an approximation for SQL Server without complex JSON parsing.
-- A more robust way is to do it in C# or with a proper JSON function if available.

UPDATE Notes
SET FilePaths = REPLACE(FilePaths, '["', '["https://testing-akshay-cm.s3.eu-south-1.amazonaws.com/'),
    ImagePaths = REPLACE(ImagePaths, '["', '["https://testing-akshay-cm.s3.eu-south-1.amazonaws.com/')
WHERE FilePaths LIKE '["notes-files/%' OR ImagePaths LIKE '["notes-images/%';

-- Handle multiple items (replace ", " with ", "https://.../)
UPDATE Notes
SET FilePaths = REPLACE(FilePaths, '", "', '", "https://testing-akshay-cm.s3.eu-south-1.amazonaws.com/'),
    ImagePaths = REPLACE(ImagePaths, '", "', '", "https://testing-akshay-cm.s3.eu-south-1.amazonaws.com/')
WHERE FilePaths LIKE '%", "notes-files/%' OR ImagePaths LIKE '%", "notes-images/%';

-- =========================================================
-- STEP 3: Verification
-- =========================================================
SELECT 'Users with Full URLs' as Category, COUNT(*) as Count FROM Users WHERE ProfileImagePath LIKE 'http%';
SELECT 'Notes with Full URLs (Files)' as Category, COUNT(*) as Count FROM Notes WHERE FilePaths LIKE '%http%';
SELECT 'Notes with Full URLs (Images)' as Category, COUNT(*) as Count FROM Notes WHERE ImagePaths LIKE '%http%';

-- Show samples
SELECT TOP 5 Id, ProfileImagePath FROM Users WHERE ProfileImagePath IS NOT NULL ORDER BY UpdatedAt DESC;
SELECT TOP 5 Id, Title, FilePaths, ImagePaths FROM Notes WHERE FilePaths IS NOT NULL OR ImagePaths IS NOT NULL ORDER BY UpdatedAt DESC;
