-- Create votes table if it does not exist
CREATE TABLE IF NOT EXISTS votes (
    id VARCHAR(255) PRIMARY KEY,
    vote VARCHAR(255) NOT NULL
);
