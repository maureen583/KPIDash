CREATE TABLE IF NOT EXISTS ProductionSchedule (
    ScheduleId       INTEGER PRIMARY KEY AUTOINCREMENT,
    ShiftDate        TEXT NOT NULL,
    Shift            TEXT NOT NULL,
    Line             TEXT NOT NULL,
    CompoundCode     TEXT NOT NULL,
    CompoundName     TEXT NOT NULL,
    ScheduledStart   TEXT NOT NULL,
    ScheduledEnd     TEXT NOT NULL,
    TargetBatches    INTEGER NOT NULL,
    PlannedOperators INTEGER NOT NULL
);

ALTER TABLE Batches ADD COLUMN Line TEXT NOT NULL DEFAULT '';
ALTER TABLE Batches ADD COLUMN CompoundCode TEXT NOT NULL DEFAULT '';
