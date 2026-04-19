Create schedule page. This will display fake compounds scheduled for a current shift. It will allow us to start calculating KPIs

     Plan: Schedule Page with MudBlazor + Shared Shift Selector

     Context

     The app needs a Schedule page so operators can see which compound runs are planned for each line in a given shift. The equipment status timeline already
     has a working shift selector; extracting it as a shared component lets the Schedule page reuse it without duplicating logic. MudBlazor's MudGrid/MudCard
     gives a polished table-style layout that fits the Blazor stack. The demo Counter and Weather pages add noise and should be removed from the nav.

     ---
     Files to Create / Modify

     ┌─────────────────────────────────────────────────────┬─────────────────────────────────────────────────┐
     │                        File                         │                     Action                      │
     ├─────────────────────────────────────────────────────┼─────────────────────────────────────────────────┤
     │ KPIDash.UI/KPIDash.UI.csproj                        │ Add MudBlazor NuGet reference                   │
     ├─────────────────────────────────────────────────────┼─────────────────────────────────────────────────┤
     │ KPIDash.UI/wwwroot/index.html                       │ Add MudBlazor CSS + JS references               │
     ├─────────────────────────────────────────────────────┼─────────────────────────────────────────────────┤
     │ KPIDash.UI/Program.cs                               │ Register builder.Services.AddMudServices()      │
     ├─────────────────────────────────────────────────────┼─────────────────────────────────────────────────┤
     │ KPIDash.UI/_Imports.razor                           │ Add @using MudBlazor                            │
     ├─────────────────────────────────────────────────────┼─────────────────────────────────────────────────┤
     │ KPIDash.UI/Models/ProductionSchedule.cs             │ New — UI model mirroring API response           │
     ├─────────────────────────────────────────────────────┼─────────────────────────────────────────────────┤
     │ KPIDash.UI/Components/ShiftSelector.razor           │ New — extracted shift navigation component      │
     ├─────────────────────────────────────────────────────┼─────────────────────────────────────────────────┤
     │ KPIDash.UI/Components/EquipmentStatusTimeline.razor │ Replace inline shift logic with <ShiftSelector> │
     ├─────────────────────────────────────────────────────┼─────────────────────────────────────────────────┤
     │ KPIDash.UI/Pages/Schedule.razor                     │ New — schedule page                             │
     ├─────────────────────────────────────────────────────┼─────────────────────────────────────────────────┤
     │ KPIDash.UI/Layout/NavMenu.razor                     │ Add Schedule link, remove Counter + Weather     │
     ├─────────────────────────────────────────────────────┼─────────────────────────────────────────────────┤
     │ KPIDash.UI/Layout/MainLayout.razor                  │ Add MudBlazor providers                         │
     ├─────────────────────────────────────────────────────┼─────────────────────────────────────────────────┤
     │ KPIDash.UI/wwwroot/css/app.css                      │ Add calendar nav icon entry                     │
     └─────────────────────────────────────────────────────┴─────────────────────────────────────────────────┘

     ---
     Implementation

     1. Install MudBlazor

     Add to KPIDash.UI.csproj:
     <PackageReference Include="MudBlazor" Version="8.*" />

     Add to wwwroot/index.html <head>:
     <link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
     <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />

     Add before closing </body>:
     <script src="_content/MudBlazor/MudBlazor.min.js"></script>

     Add to Program.cs:
     builder.Services.AddMudServices();

     Add to _Imports.razor:
     @using MudBlazor

     Add to MainLayout.razor (inside the layout, alongside existing content):
     <MudThemeProvider />
     <MudDialogProvider />
     <MudSnackbarProvider />

     2. KPIDash.UI/Models/ProductionSchedule.cs (new)

     namespace KPIDash.UI.Models;

     public class ProductionSchedule
     {
         public int ScheduleId { get; set; }
         public string ShiftDate { get; set; } = "";
         public string Shift { get; set; } = "";
         public string Line { get; set; } = "";
         public string CompoundCode { get; set; } = "";
         public string CompoundName { get; set; } = "";
         public string ScheduledStart { get; set; } = "";
         public string ScheduledEnd { get; set; } = "";
         public int TargetBatches { get; set; }
         public int PlannedOperators { get; set; }
     }

     3. KPIDash.UI/Components/ShiftSelector.razor (new)

     Extract shift logic from EquipmentStatusTimeline.razor into a reusable component with EventCallback<Shift> for parent notification:

     @using KPIDash.UI.Models

     <div class="d-flex align-items-center gap-2">
         <button class="btn btn-sm btn-outline-secondary" @onclick="PrevShift">← Shift</button>
         <span class="text-muted small px-2">
             @DisplayedShift.Name · @DisplayedShift.Start.ToString("MMM d, HH:mm") – @DisplayedShift.End.ToString("HH:mm") UTC
         </span>
         <button class="btn btn-sm btn-outline-secondary" @onclick="NextShift" disabled="@IsCurrentShift">Shift →</button>
         <button class="btn btn-sm btn-secondary" @onclick="JumpToNow" disabled="@IsCurrentShift">Now</button>
     </div>

     @code {
         [Parameter] public EventCallback<Shift> OnShiftChanged { get; set; }

         public Shift DisplayedShift { get; private set; } = GetCurrentShift();
         private bool IsCurrentShift => DisplayedShift.Start == GetCurrentShift().Start;

         public static Shift GetCurrentShift() => GetShiftAt(DateTime.UtcNow);

         public static Shift GetShiftAt(DateTime t)
         {
             var d = t.Date;
             return t.Hour switch
             {
                 >= 6 and < 14  => new("Day",       d.AddHours(6),             d.AddHours(14)),
                 >= 14 and < 22 => new("Afternoon", d.AddHours(14),            d.AddHours(22)),
                 >= 22          => new("Night",     d.AddHours(22),             d.AddDays(1).AddHours(6)),
                 _              => new("Night",     d.AddDays(-1).AddHours(22), d.AddHours(6)),
             };
         }

         private async Task PrevShift()
         {
             DisplayedShift = GetShiftAt(DisplayedShift.Start.AddMinutes(-1));
             await OnShiftChanged.InvokeAsync(DisplayedShift);
         }

         private async Task NextShift()
         {
             if (!IsCurrentShift)
             {
                 DisplayedShift = GetShiftAt(DisplayedShift.End.AddMinutes(1));
                 await OnShiftChanged.InvokeAsync(DisplayedShift);
             }
         }

         private async Task JumpToNow()
         {
             DisplayedShift = GetCurrentShift();
             await OnShiftChanged.InvokeAsync(DisplayedShift);
         }
     }

     4. EquipmentStatusTimeline.razor — Refactor to use ShiftSelector

     Replace the header div + all shift code with:

     <div class="d-flex align-items-center justify-content-between mb-3">
         <h4 class="mb-0">Equipment Status Timeline</h4>
         <ShiftSelector OnShiftChanged="OnShiftChanged" />
     </div>

     private Shift displayedShift = ShiftSelector.GetCurrentShift();
     private bool IsCurrentShift => displayedShift.Start == ShiftSelector.GetCurrentShift().Start;
     private DateTime DataCutoff => IsCurrentShift ? DateTime.UtcNow : displayedShift.End;

     private async Task OnShiftChanged(Shift shift)
     {
         displayedShift = shift;
         await LoadStatusData();
     }

     Remove the old GetShiftAt, GetCurrentShift, PrevShift, NextShift, JumpToNow methods from this file.

     5. KPIDash.UI/Pages/Schedule.razor (new)

     @page "/schedule"
     @using KPIDash.UI.Models
     @inject HttpClient Http

     <div class="d-flex align-items-center justify-content-between mb-3">
         <h4 class="mb-0">Production Schedule</h4>
         <ShiftSelector OnShiftChanged="OnShiftChanged" />
     </div>

     @if (isLoading)
     {
         <MudProgressCircular Indeterminate="true" />
     }
     else if (!scheduleItems.Any())
     {
         <MudAlert Severity="Severity.Info">No schedule data for this shift.</MudAlert>
     }
     else
     {
         @foreach (var line in scheduleItems.GroupBy(s => s.Line).OrderBy(g => g.Key))
         {
             <MudText Typo="Typo.h6" Class="mt-3 mb-2">@line.Key</MudText>
             <MudGrid>
                 @foreach (var run in line.OrderBy(r => r.ScheduledStart))
                 {
                     <MudItem xs="12" sm="6" md="4">
                         <MudCard Outlined="true">
                             <MudCardContent>
                                 <MudText Typo="Typo.subtitle1"><b>@run.CompoundCode</b> — @run.CompoundName</MudText>
                                 <MudText Typo="Typo.body2">@FormatTime(run.ScheduledStart) – @FormatTime(run.ScheduledEnd)</MudText>
                                 <MudText Typo="Typo.body2">Target: @run.TargetBatches batches</MudText>
                                 <MudText Typo="Typo.body2">Operators: @run.PlannedOperators</MudText>
                             </MudCardContent>
                         </MudCard>
                     </MudItem>
                 }
             </MudGrid>
         }
     }

     @code {
         private List<ProductionSchedule> scheduleItems = new();
         private bool isLoading = true;
         private Shift currentShift = ShiftSelector.GetCurrentShift();

         protected override async Task OnInitializedAsync() => await LoadSchedule();

         private async Task OnShiftChanged(Shift shift)
         {
             currentShift = shift;
             await LoadSchedule();
         }

         private async Task LoadSchedule()
         {
             isLoading = true;
             StateHasChanged();
             var url = $"/api/schedule/{currentShift.Start:yyyy-MM-dd}/{currentShift.Name}";
             scheduleItems = await Http.GetFromJsonAsync<List<ProductionSchedule>>(url) ?? new();
             isLoading = false;
         }

         private static string FormatTime(string iso) =>
             DateTime.TryParse(iso, out var dt) ? dt.ToString("HH:mm") : iso;
     }

     Note on Night shift date: URL uses currentShift.Start.ToString("yyyy-MM-dd") which matches the seeder's ShiftDate convention (the date the shift starts).

     6. NavMenu.razor — Update navigation

     Replace Counter + Weather links with a Schedule link:

     <div class="nav-item px-3">
         <NavLink class="nav-link" href="" Match="NavLinkMatch.All">
             <span class="bi bi-house-door-fill-nav-menu" aria-hidden="true"></span> Home
         </NavLink>
     </div>
     <div class="nav-item px-3">
         <NavLink class="nav-link" href="schedule">
             <span class="bi bi-calendar3-nav-menu" aria-hidden="true"></span> Schedule
         </NavLink>
     </div>

     Add to app.css (matching existing .bi-*-nav-menu icon pattern):
     .bi-calendar3-nav-menu::before {
         background-image: url("data:image/svg+xml,..."); /* calendar icon SVG */
     }

     The ui-designer agent should check app.css for the existing icon pattern and generate the matching calendar3 SVG entry.

     ---
     Delegation to ui-designer

     The ui-designer agent handles all Blazor + MudBlazor file creation and editing:
     - ShiftSelector.razor
     - Schedule.razor
     - NavMenu.razor
     - MainLayout.razor (MudBlazor providers)
     - app.css (calendar icon)
     - EquipmentStatusTimeline.razor refactor
     - MudBlazor wiring in index.html, Program.cs, _Imports.razor, .csproj

     Provide the agent with the full plan above and paths to:
     - KPIDash.UI/Components/EquipmentStatusTimeline.razor (existing shift logic to extract)
     - KPIDash.UI/Layout/NavMenu.razor (existing nav structure)
     - KPIDash.UI/Layout/MainLayout.razor (for MudBlazor providers placement)
     - KPIDash.UI/wwwroot/index.html
     - KPIDash.UI/wwwroot/css/app.css

     ---
     Verification

     1. dotnet build KPIDash.slnx — no errors
     2. dotnet run --project KPIDash.API
     3. Open app, confirm:
       - Nav shows "Home" and "Schedule" (Counter/Weather removed)
       - Schedule page loads, defaults to current shift
       - Cards display compound runs grouped by Line 1 / Line 2
       - Shift navigation (← Shift / Shift →) works and reloads data
       - Equipment timeline on Home page still works (ShiftSelector refactor didn't break it)
     4. Kill dotnet run process after testing (free port 5250)
     5. dotnet test KPIDash.Tests/KPIDash.Tests.csproj — all tests pass
