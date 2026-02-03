-- Migration Script: Update Profile Image Paths to Full S3 URLs
-- Run this to convert all relative paths to full URLs in database

-- Step 1: Update users with relative paths to full URLs
UPDATE Users
SET ProfileImagePath = 'https://testing-akshay-cm.s3.eu-south-1.amazonaws.com/' + ProfileImagePath
WHERE ProfileImagePath IS NOT NULL 
  AND ProfileImagePath <> ''
  AND ProfileImagePath NOT LIKE 'http%'
  AND LEN(ProfileImagePath) > 0;

-- Step 2: Verify the update
SELECT 
    COUNT(*) as TotalWithImages,
    COUNT(CASE WHEN ProfileImagePath LIKE 'http%' THEN 1 END) as FullURLs,
    COUNT(CASE WHEN ProfileImagePath NOT LIKE 'http%' THEN 1 END) as RelativePaths
FROM Users
WHERE ProfileImagePath IS NOT NULL AND ProfileImagePath <> '';

-- Step 3: Show sample results
SELECT TOP 5 Id, FirstName, ProfileImagePath 
FROM Users 
WHERE ProfileImagePath IS NOT NULL
ORDER BY CreatedAt DESC;
