using KPIDash.Data;
using KPIDash.Data.Repositories;
using KPIDash.API.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("Default")!;
builder.Services.AddSingleton(new DbConnectionFactory(connectionString));
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddSingleton<DataSeeder>();

builder.Services.AddScoped<IEquipmentRepository, EquipmentRepository>();
builder.Services.AddScoped<ISensorRepository, SensorRepository>();
builder.Services.AddScoped<IEquipmentStatusRepository, EquipmentStatusRepository>();
builder.Services.AddScoped<IDowntimeRepository, DowntimeRepository>();
builder.Services.AddScoped<IBatchRepository, BatchRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<ITimeLogRepository, TimeLogRepository>();

var app = builder.Build();

app.Services.GetRequiredService<DatabaseInitializer>().Initialize();
app.Services.GetRequiredService<DataSeeder>().Seed();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapEquipmentEndpoints();
app.MapSensorEndpoints();
app.MapEquipmentStatusEndpoints();
app.MapDowntimeEndpoints();
app.MapBatchEndpoints();
app.MapEmployeeEndpoints();
app.MapTimeLogEndpoints();

app.Run();
