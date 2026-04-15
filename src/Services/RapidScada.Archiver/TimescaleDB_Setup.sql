-- TimescaleDB Setup for RapidScada Historical Data
-- Run this script after applying EF Core migrations

-- Enable TimescaleDB extension
CREATE EXTENSION IF NOT EXISTS timescaledb;

-- Create tag_history table (if not exists)
CREATE TABLE IF NOT EXISTS tag_history (
    time TIMESTAMPTZ NOT NULL,
    tag_id INTEGER NOT NULL,
    value DOUBLE PRECISION,
    quality DOUBLE PRECISION,
    device_id INTEGER,
    FOREIGN KEY (tag_id) REFERENCES tags(id) ON DELETE CASCADE
);

-- Convert to hypertable (time-series optimized)
SELECT create_hypertable('tag_history', 'time', if_not_exists => TRUE);

-- Create indexes for better query performance
CREATE INDEX IF NOT EXISTS idx_tag_history_tag_time ON tag_history (tag_id, time DESC);
CREATE INDEX IF NOT EXISTS idx_tag_history_device_time ON tag_history (device_id, time DESC);

-- Create event_history table
CREATE TABLE IF NOT EXISTS event_history (
    event_id BIGSERIAL PRIMARY KEY,
    time TIMESTAMPTZ NOT NULL,
    event_type TEXT,
    tag_id INTEGER,
    device_id INTEGER,
    severity TEXT,
    message TEXT,
    acknowledged BOOLEAN DEFAULT FALSE,
    acknowledged_at TIMESTAMPTZ,
    acknowledged_by TEXT,
    FOREIGN KEY (tag_id) REFERENCES tags(id) ON DELETE SET NULL,
    FOREIGN KEY (device_id) REFERENCES devices(id) ON DELETE SET NULL
);

-- Create indexes for events
CREATE INDEX IF NOT EXISTS idx_event_history_time ON event_history (time DESC);
CREATE INDEX IF NOT EXISTS idx_event_history_tag ON event_history (tag_id, time DESC);
CREATE INDEX IF NOT EXISTS idx_event_history_device ON event_history (device_id, time DESC);
CREATE INDEX IF NOT EXISTS idx_event_history_severity ON event_history (severity, time DESC);

-- Enable compression on tag_history (compress data older than 7 days)
ALTER TABLE tag_history SET (
    timescaledb.compress,
    timescaledb.compress_segmentby = 'tag_id'
);

-- Add compression policy (compress chunks older than 7 days)
SELECT add_compression_policy('tag_history', INTERVAL '7 days', if_not_exists => TRUE);

-- Add retention policy (drop chunks older than specified in app config)
-- This will be managed by the application based on configuration

-- Create continuous aggregates for pre-computed statistics

-- 1-minute aggregates
CREATE MATERIALIZED VIEW IF NOT EXISTS tag_history_1min
WITH (timescaledb.continuous) AS
SELECT time_bucket('1 minute', time) AS time_bucket,
       tag_id,
       AVG(value) AS avg_value,
       MIN(value) AS min_value,
       MAX(value) AS max_value,
       COUNT(*) AS sample_count
FROM tag_history
GROUP BY time_bucket, tag_id
WITH NO DATA;

-- Refresh policy for 1-minute aggregates
SELECT add_continuous_aggregate_policy('tag_history_1min',
    start_offset => INTERVAL '3 hours',
    end_offset => INTERVAL '1 minute',
    schedule_interval => INTERVAL '1 minute',
    if_not_exists => TRUE);

-- 1-hour aggregates
CREATE MATERIALIZED VIEW IF NOT EXISTS tag_history_1hour
WITH (timescaledb.continuous) AS
SELECT time_bucket('1 hour', time) AS time_bucket,
       tag_id,
       AVG(value) AS avg_value,
       MIN(value) AS min_value,
       MAX(value) AS max_value,
       COUNT(*) AS sample_count
FROM tag_history
GROUP BY time_bucket, tag_id
WITH NO DATA;

-- Refresh policy for 1-hour aggregates
SELECT add_continuous_aggregate_policy('tag_history_1hour',
    start_offset => INTERVAL '7 days',
    end_offset => INTERVAL '1 hour',
    schedule_interval => INTERVAL '1 hour',
    if_not_exists => TRUE);

-- 1-day aggregates
CREATE MATERIALIZED VIEW IF NOT EXISTS tag_history_1day
WITH (timescaledb.continuous) AS
SELECT time_bucket('1 day', time) AS time_bucket,
       tag_id,
       AVG(value) AS avg_value,
       MIN(value) AS min_value,
       MAX(value) AS max_value,
       COUNT(*) AS sample_count
FROM tag_history
GROUP BY time_bucket, tag_id
WITH NO DATA;

-- Refresh policy for 1-day aggregates
SELECT add_continuous_aggregate_policy('tag_history_1day',
    start_offset => INTERVAL '30 days',
    end_offset => INTERVAL '1 day',
    schedule_interval => INTERVAL '1 day',
    if_not_exists => TRUE);

-- Grant permissions
GRANT SELECT, INSERT ON tag_history TO scada;
GRANT SELECT, INSERT, UPDATE ON event_history TO scada;
GRANT SELECT ON tag_history_1min TO scada;
GRANT SELECT ON tag_history_1hour TO scada;
GRANT SELECT ON tag_history_1day TO scada;
