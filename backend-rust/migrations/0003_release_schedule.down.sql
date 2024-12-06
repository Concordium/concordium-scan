-- Down migration script for removing the scheduled release table.

-- Drop the scheduled_releases table.
DROP TABLE IF EXISTS scheduled_releases;