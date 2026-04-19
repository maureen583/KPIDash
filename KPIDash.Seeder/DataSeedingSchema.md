# KPIDash Data Seeding Guide

## Seed Order (must follow due to foreign key dependencies)

1. Equipment
2. Employees
3. Sensors
4. TimeLog
5. ProductionSchedule
6. Batches
7. SensorReadings
8. EquipmentStatus
9. DowntimeEvents

---

## Table Relationships

Equipment (8 rows — Line 1 and Line 2 for each of 4 types)
└──< Sensors (~5 per equipment, ~20 rows total)
└──< SensorReadings (every 5 min, 7 days)
└──< EquipmentStatus (one row per status change event)
└──< DowntimeEvents (one row per down period)
Employees (~15 rows)
└──< TimeLog (clock in/out, 7 days of shifts)
ProductionSchedule (2-3 compound runs per shift per line, 7 days)
└──< Batches (each batch tied to one operator, one line, one compound)

---

## Equipment Seed Data (exact rows, fixed)

| EquipmentId | Name                  | Type          | DisplayOrder |
| ----------- | --------------------- | ------------- | ------------ |
| 1           | Line 1 Conveyor       | Conveyor      | 1            |
| 2           | Line 2 Conveyor       | Conveyor      | 2            |
| 3           | Line 1 Internal Mixer | InternalMixer | 3            |
| 4           | Line 2 Internal Mixer | InternalMixer | 4            |
| 5           | Line 1 Mill           | Mill          | 5            |
| 6           | Line 2 Mill           | Mill          | 6            |
| 7           | Line 1 Cooling        | CoolingLine   | 7            |
| 8           | Line 2 Cooling        | CoolingLine   | 8            |

---

## Sensors Seed Data (exact rows, fixed)

### Conveyor (EquipmentId = 1)

| Name             | Unit  | MinNormal | MaxNormal |
| ---------------- | ----- | --------- | --------- |
| BeltSpeed        | m/min | 0.5       | 3.0       |
| MotorCurrent     | A     | 5.0       | 45.0      |
| MaterialPresence | bool  | 0         | 1         |

### Internal Mixer (EquipmentId = 2)

| Name              | Unit | MinNormal | MaxNormal |
| ----------------- | ---- | --------- | --------- |
| RotorRPM          | RPM  | 20.0      | 80.0      |
| ChamberTemp       | C    | 60.0      | 160.0     |
| MotorCurrent      | A    | 50.0      | 400.0     |
| RamPressure       | bar  | 2.0       | 8.0       |
| HydraulicPressure | bar  | 80.0      | 160.0     |

### Mill (EquipmentId = 3)

| Name          | Unit | MinNormal | MaxNormal |
| ------------- | ---- | --------- | --------- |
| FrontRollRPM  | RPM  | 15.0      | 40.0      |
| BackRollRPM   | RPM  | 15.0      | 40.0      |
| FrontRollTemp | C    | 50.0      | 90.0      |
| BackRollTemp  | C    | 50.0      | 90.0      |
| MotorCurrent  | A    | 20.0      | 150.0     |

### Cooling Line (EquipmentId = 4)

| Name             | Unit  | MinNormal | MaxNormal |
| ---------------- | ----- | --------- | --------- |
| ConveyorSpeed    | m/min | 0.5       | 2.0       |
| WaterFlowRate    | L/min | 10.0      | 50.0      |
| WaterInletTemp   | C     | 10.0      | 25.0      |
| WaterOutletTemp  | C     | 20.0      | 45.0      |
| ExitCompoundTemp | C     | 40.0      | 80.0      |

---

## Employees Seed Data

Generate 15 fake employees using Bogus with these roles:

- 5 General Operator
- 3 Mixers
- 3 Mill Man
- 2 Supervisor
- 2 Maintenance

---

## TimeLog Rules

- Generate 7 days of shift history
- Three shifts per day: Day (06:00-14:00), Afternoon (14:00-22:00), Night (22:00-06:00)
- Assign 3-5 operators per shift
- ClockIn/ClockOut should have small random variance (±15 min) from shift start/end
- ShiftDate is always the date the shift STARTED

---

## ProductionSchedule Rules

Each shift on each line is divided into 2–3 consecutive compound runs. Every run gets its own row — the schedule is fully chronological with no gaps on Day and Afternoon shifts, and a 2-hour unscheduled window at the end of Night shifts.

### Schema

| Column           | Type    | Notes                                              |
| ---------------- | ------- | -------------------------------------------------- |
| ScheduleId       | INTEGER | PK AUTOINCREMENT                                   |
| ShiftDate        | TEXT    | YYYY-MM-DD — date the shift **started**            |
| Shift            | TEXT    | Day \| Afternoon \| Night                          |
| Line             | TEXT    | 'Line 1' \| 'Line 2'                               |
| CompoundCode     | TEXT    | e.g. 'NR-100'                                      |
| CompoundName     | TEXT    | e.g. 'Natural Rubber Base'                         |
| ScheduledStart   | TEXT    | ISO datetime — start of this compound run          |
| ScheduledEnd     | TEXT    | ISO datetime — end of this compound run            |
| TargetBatches    | INTEGER | duration\_minutes / 10 (avg cycle time), rounded   |
| PlannedOperators | INTEGER | Planned headcount for the full shift               |

### Compound library (rotate through these)

| CompoundCode | CompoundName              |
| ------------ | ------------------------- |
| NR-100       | Natural Rubber Base       |
| SBR-200      | Styrene Butadiene General |
| EPDM-300     | EPDM Weather Seal         |
| NBR-400      | Nitrile Oil Resistant     |
| CR-500       | Chloroprene Adhesive      |
| BR-600       | Butadiene High Resilience |

### Shift capacity rules

| Shift     | Shift window    | Scheduled production | Unscheduled |
| --------- | --------------- | -------------------- | ----------- |
| Day       | 06:00 – 14:00   | 06:00 – 14:00 (8 h)  | none        |
| Afternoon | 14:00 – 22:00   | 14:00 – 22:00 (8 h)  | none        |
| Night     | 22:00 – 06:00   | 22:00 – 04:00 (6 h)  | 04:00–06:00 |

- Day and Afternoon shifts are scheduled to 100% capacity — compound runs fill the full 8 hours with no gaps
- Night shift schedules 6 of 8 hours — the last 2 hours (04:00–06:00) are unscheduled (no rows)
- Generate 2 or 3 compound runs per shift per line (randomise; do not always split evenly)
- Run durations must sum exactly to the scheduled window
- Example 3-run Day split: 2.5 h / 3 h / 2.5 h → TargetBatches: 15 / 18 / 15
- Do not repeat the same compound consecutively on the same line within a shift

### TargetBatches calculation

`TargetBatches = ROUND(duration_minutes / 10)`

Average cycle time is 10 minutes (midpoint of the 8–12 min Banbury cycle).

### PlannedOperators per shift

| Shift     | PlannedOperators |
| --------- | ---------------- |
| Day       | 5                |
| Afternoon | 4                |
| Night     | 3                |

Same value for every row belonging to the same shift.

---

## Batch Rules

- One batch takes 8-12 minutes in the Banbury
- BatchNumber format: `B-YYYYMMDD-NNN` (e.g. `B-20260101-001`)
- TargetDumpTemp is always 120.0C
- DumpTemperature should be TargetDumpTemp ± 15C with normal distribution
- ~5% of batches should have Status = Rejected (DumpTemp out of range by >10C)
- ~2% of batches should have Status = InProgress (most recent batch of current shift)
- All others Status = Complete
- Each batch is assigned to one Operator from the active shift
- Each batch must have a `Line` value ('Line 1' or 'Line 2') — split roughly 50/50 per shift
- Each batch must have a `CompoundCode` matching the ProductionSchedule row whose
  ScheduledStart–ScheduledEnd window contains the batch's StartedAt timestamp

---

## SensorReadings Rules

- Generate one reading per sensor every 5 minutes for 7 days
- Values must reflect the equipment's current state (Running, Idle, or Down)
- State transitions should be realistic — equipment doesn't flip state every reading
- equipment states need to make sense, if conveyor is down, equipment below it should stop within
  5 minutes and stay stopped for 5 minutes until it restarts. Follow equipment order for this pattern
- states should change at least once for at least 5 minutes per 4 hour window

### Value ranges by state:

**Running** — values within MinNormal to MaxNormal with small Gaussian noise (±3%)

**Idle** — motor/speed sensors drop to 0, temperature sensors drift slowly within range

- RPM sensors: 0
- Current sensors: 0-2A (residual)
- Temperature sensors: slowly drift toward ambient (20C) but stay in range
- Pressure sensors: drop to minimum or 0

**Down** — one or more sensors drift outside normal range

- The triggering sensor drifts beyond MinNormal or MaxNormal
- Other sensors follow suit over 2-5 readings (realistic degradation)
- Down periods last 15-120 minutes

---

## EquipmentStatus Rules

- Derived from SensorReadings — do not generate independently
- One status record per state change (not one per reading)
- Status transitions: Running → Idle → Running is normal between batches
- Status transitions: Running → Down or Idle → Down indicates a fault
- Reason mapping:
  - Running + all sensors normal = `NormalOperation`
  - Idle + all sensors healthy = `Idle`
  - Down + motor fault = `FaultTrip`
  - Down + temp out of range = `ParameterOutOfRange`
  - Down + instantaneous drop = `EmergencyStop`

---

## DowntimeEvents Rules

- Derived from EquipmentStatus — do not generate independently
  Use these exact string values for the Reason field in both DowntimeEvents and EquipmentStatus:

| Reason            | Category   | Trigger                                                 |
| ----------------- | ---------- | ------------------------------------------------------- |
| `BearingFailure`  | Mechanical | RPM drops while current spikes                          |
| `HydraulicLeak`   | Mechanical | HydraulicPressure drops out of range                    |
| `DriveFailure`    | Mechanical | RPM drops to 0 while current remains high               |
| `MotorOverload`   | Electrical | MotorCurrent spikes beyond MaxNormal                    |
| `PowerOutage`     | Electrical | All sensors drop to 0 simultaneously                    |
| `RubberStick`     | Process    | MotorCurrent spikes, RPM drops                          |
| `TempOutOfRange`  | Process    | Chamber/roll temp drifts beyond MinNormal or MaxNormal  |
| `CoolingFailure`  | Process    | WaterFlowRate drops below MinNormal                     |
| `EmergencyStop`   | Safety     | All motor sensors drop to 0 instantaneously (1 reading) |
| `SafetyGuardOpen` | Safety     | All motor sensors drop to 0 instantaneously (1 reading) |

- One row per continuous Down period per equipment
- StartedAt = timestamp of first Down status record
- EndedAt = timestamp of first non-Down status after recovery (NULL if still down)
- DurationMinutes = calculated from StartedAt and EndedAt
- Reason matches the EquipmentStatus Reason that triggered it
- Target ~3-5 downtime events per equipment over 7 days

---

## Realistic State Schedule (per day per equipment)

A typical 24-hour period should look roughly like:
06:00 - Running (Day shift starts, equipment warming up)
06:00 - 14:00 — Running with brief Idle gaps between batches (2-4 min each)
~10:00 — Optional: 1 Down event (15-45 min), then recovery
14:00 - 22:00 — Same pattern as Day shift
22:00 - 06:00 — Same pattern as Night shift
1-2x per shift — 1 Down event somewhere

---

## Key Constraints

- SensorReadings and EquipmentStatus must be internally consistent
  (a Down status must have at least one out-of-range sensor reading)
- Batches only occur when the Banbury is in Running state
- Batch StartedAt must fall within a ProductionSchedule ScheduledStart–ScheduledEnd window
  for the matching Line; batches must not be generated during unscheduled time (04:00–06:00 Night)
- TimeLog shift coverage must overlap with batch production times. Certain shifts should have more people on than others so we can show variance in calculations for Utilization
- DowntimeEvents must not overlap for the same equipment
