-- Create issue_logs table to track status changes and other modifications
CREATE TABLE IF NOT EXISTS issue_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    issue_id UUID NOT NULL REFERENCES issues(id) ON DELETE CASCADE,
    timestamp TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    user_name TEXT NOT NULL,
    field_changed TEXT NOT NULL DEFAULT '',
    old_value TEXT,
    new_value TEXT,
    action TEXT NOT NULL DEFAULT '',
    details TEXT NOT NULL DEFAULT ''
);

-- Create index for faster lookups by issue_id
CREATE INDEX IF NOT EXISTS idx_issue_logs_issue_id ON issue_logs(issue_id);
CREATE INDEX IF NOT EXISTS idx_issue_logs_timestamp ON issue_logs(timestamp DESC);

-- Enable Row Level Security
ALTER TABLE issue_logs ENABLE ROW LEVEL SECURITY;

-- Create policy to allow all authenticated users to read and write logs
CREATE POLICY "Allow all operations on issue_logs" ON issue_logs
    FOR ALL
    USING (true)
    WITH CHECK (true);
