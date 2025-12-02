-- Migration: Add notified column to meetings table
-- Date: 2024-01-XX
-- Description: Add a boolean column to track whether notification emails have been sent for meetings

ALTER TABLE meetings 
ADD COLUMN notified BOOLEAN NOT NULL DEFAULT FALSE;

-- Add index for better query performance when filtering by notification status
CREATE INDEX idx_meetings_notified ON meetings(notified);

