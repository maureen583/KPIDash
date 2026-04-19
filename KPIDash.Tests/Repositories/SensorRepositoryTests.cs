using Dapper;
using KPIDash.Data.Repositories;
using KPIDash.Tests.Infrastructure;

namespace KPIDash.Tests.Repositories;

public class SensorRepositoryTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly SensorRepository _sut;

    private static readonly DateTime Base = new(2025, 3, 1, 10, 0, 0, DateTimeKind.Utc);

    public SensorRepositoryTests()
    {
        _sut = new SensorRepository(_db.Factory);
        SeedEquipmentAndSensors();
    }

    // --- GetByEquipmentIdAsync ---

    [Fact]
    public async Task GetByEquipmentIdAsync_ReturnsOnlySensorsForEquipment()
    {
        var results = (await _sut.GetByEquipmentIdAsync(1)).ToList();

        Assert.Equal(2, results.Count);
        Assert.All(results, s => Assert.Equal(1, s.EquipmentId));
    }

    [Fact]
    public async Task GetByEquipmentIdAsync_NoSensorsForEquipment_ReturnsEmpty()
    {
        var results = await _sut.GetByEquipmentIdAsync(99);

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetByEquipmentIdAsync_ReturnsSensorProperties()
    {
        var results = (await _sut.GetByEquipmentIdAsync(1)).ToList();
        var temp = results.Single(s => s.Name == "Temperature");

        Assert.Equal("°C", temp.Unit);
        Assert.Equal(60.0,  temp.MinNormal);
        Assert.Equal(120.0, temp.MaxNormal);
        Assert.True(temp.IsStatusSensor);
    }

    // --- GetLatestReadingAsync ---

    [Fact]
    public async Task GetLatestReadingAsync_ReturnsNewestReading()
    {
        SeedReadings(sensorId: 1,
            (Base,            85.0),
            (Base.AddHours(1), 90.0),
            (Base.AddHours(2), 95.0));

        var result = await _sut.GetLatestReadingAsync(1);

        Assert.NotNull(result);
        Assert.Equal(95.0, result.Value);
    }

    [Fact]
    public async Task GetLatestReadingAsync_NoReadings_ReturnsNull()
    {
        var result = await _sut.GetLatestReadingAsync(99);

        Assert.Null(result);
    }

    // --- GetReadingsAsync ---

    [Fact]
    public async Task GetReadingsAsync_ReturnsReadingsWithinRange()
    {
        SeedReadings(sensorId: 1,
            (Base.AddHours(-1), 70.0),
            (Base,              85.0),
            (Base.AddHours(1),  90.0),
            (Base.AddHours(5),  95.0));

        var results = (await _sut.GetReadingsAsync(1, Base, Base.AddHours(1))).ToList();

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Value == 85.0);
        Assert.Contains(results, r => r.Value == 90.0);
    }

    [Fact]
    public async Task GetReadingsAsync_OrderedByRecordedAtAscending()
    {
        SeedReadings(sensorId: 1,
            (Base,             85.0),
            (Base.AddHours(2), 95.0),
            (Base.AddHours(1), 90.0));

        var results = (await _sut.GetReadingsAsync(1, Base.AddHours(-1), Base.AddHours(3))).ToList();

        Assert.Equal(3, results.Count);
        Assert.True(results[0].RecordedAt <= results[1].RecordedAt);
        Assert.True(results[1].RecordedAt <= results[2].RecordedAt);
    }

    [Fact]
    public async Task GetReadingsAsync_NoReadingsInRange_ReturnsEmpty()
    {
        SeedReadings(sensorId: 1, (Base, 85.0));

        var results = await _sut.GetReadingsAsync(1, Base.AddDays(1), Base.AddDays(2));

        Assert.Empty(results);
    }

    private void SeedEquipmentAndSensors()
    {
        using var conn = _db.Factory.Create();
        conn.Execute("""
            INSERT INTO Equipment (EquipmentId, Name, Type, DisplayOrder) VALUES
            (1, 'Conveyor', 'Conveyor', 1),
            (2, 'Mill',     'Mill',     2)
            """);

        conn.Execute("""
            INSERT INTO Sensors (SensorId, EquipmentId, Name, Unit, MinNormal, MaxNormal, IsStatusSensor) VALUES
            (1, 1, 'Temperature', '°C',  60.0, 120.0, 1),
            (2, 1, 'Speed',       'RPM', 10.0,  50.0, 1),
            (3, 2, 'Pressure',    'bar',  1.0,   5.0, 0)
            """);
    }

    private void SeedReadings(int sensorId, params (DateTime RecordedAt, double Value)[] readings)
    {
        using var conn = _db.Factory.Create();
        foreach (var (recordedAt, value) in readings)
        {
            conn.Execute("""
                INSERT INTO SensorReadings (SensorId, RecordedAt, Value)
                VALUES (@SensorId, @RecordedAt, @Value)
                """,
                new { SensorId = sensorId, RecordedAt = recordedAt.ToString("o"), Value = value });
        }
    }

    public void Dispose() => _db.Dispose();
}
