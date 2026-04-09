CREATE TABLE IF NOT EXISTS Migrations (
    MigrationId   INTEGER PRIMARY KEY AUTOINCREMENT,
    FileName      TEXT NOT NULL UNIQUE,
    AppliedAt     TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Equipment (
    EquipmentId     INTEGER PRIMARY KEY AUTOINCREMENT,
    Name            TEXT NOT NULL,
    Type            TEXT NOT NULL,      -- Conveyor, InternalMixer, Mill, CoolingLine
    DisplayOrder    INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS Sensors (
    SensorId        INTEGER PRIMARY KEY AUTOINCREMENT,
    EquipmentId     INTEGER NOT NULL REFERENCES Equipment(EquipmentId),
    Name            TEXT NOT NULL,
    Unit            TEXT NOT NULL,
    MinNormal       REAL NOT NULL,
    MaxNormal       REAL NOT NULL,
    IsStatusSensor  INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS SensorReadings (
    ReadingId       INTEGER PRIMARY KEY AUTOINCREMENT,
    SensorId        INTEGER NOT NULL REFERENCES Sensors(SensorId),
    RecordedAt      TEXT NOT NULL,
    Value           REAL NOT NULL
);

CREATE TABLE IF NOT EXISTS EquipmentStatus (
    StatusId        INTEGER PRIMARY KEY AUTOINCREMENT,
    EquipmentId     INTEGER NOT NULL REFERENCES Equipment(EquipmentId),
    RecordedAt      TEXT NOT NULL,
    Status          TEXT NOT NULL,      -- Running, Idle, Down
    Reason          TEXT NOT NULL       -- NormalOperation, PlannedIdle, FaultTrip,
                                        -- ParameterOutOfRange, EmergencyStop
);

CREATE TABLE IF NOT EXISTS DowntimeEvents (
    DowntimeId      INTEGER PRIMARY KEY AUTOINCREMENT,
    EquipmentId     INTEGER NOT NULL REFERENCES Equipment(EquipmentId),
    StartedAt       TEXT NOT NULL,
    EndedAt         TEXT,
    DurationMinutes REAL,
    Reason          TEXT NOT NULL,
    Notes           TEXT
);

CREATE TABLE IF NOT EXISTS Employees (
    EmployeeId      INTEGER PRIMARY KEY AUTOINCREMENT,
    FirstName       TEXT NOT NULL,
    LastName        TEXT NOT NULL,
    Role            TEXT NOT NULL       -- Operator, Supervisor, Maintenance
);

CREATE TABLE IF NOT EXISTS Batches (
    BatchId         INTEGER PRIMARY KEY AUTOINCREMENT,
    BatchNumber     TEXT NOT NULL UNIQUE,
    StartedAt       TEXT NOT NULL,
    CompletedAt     TEXT,
    DumpTemperature REAL,
    TargetDumpTemp  REAL NOT NULL,
    Status          TEXT NOT NULL,      -- InProgress, Complete, Rejected
    OperatorId      INTEGER NOT NULL REFERENCES Employees(EmployeeId)
);

CREATE TABLE IF NOT EXISTS TimeLog (
    TimeLogId       INTEGER PRIMARY KEY AUTOINCREMENT,
    EmployeeId      INTEGER NOT NULL REFERENCES Employees(EmployeeId),
    ClockIn         TEXT NOT NULL,
    ClockOut        TEXT,
    ShiftDate       TEXT NOT NULL,      -- YYYY-MM-DD
    Shift           TEXT NOT NULL       -- Day, Afternoon, Night
);