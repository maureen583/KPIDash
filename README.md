# KPIDash
Sample Manufacturing KPI Dashboard to demo Claude Code development container with all simulated data.

## Features

### Equipment Status Timeline
A color-coded horizontal bar chart (one chart per production line) showing the live status of each of the 4 equipment pieces over a rolling 4-hour window (3 hours of history + current moment).

| Color  | Status  | Meaning                         |
|--------|---------|----------------------------------|
| Green  | Running | Equipment producing normally     |
| Yellow | Idle    | Healthy but not producing        |
| Red    | Down    | Fault or parameter out of range  |
