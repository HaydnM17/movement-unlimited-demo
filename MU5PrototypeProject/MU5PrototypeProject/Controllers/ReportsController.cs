using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MU5PrototypeProject.CustomController;
using MU5PrototypeProject.Data;
using MU5PrototypeProject.Models;
using MU5PrototypeProject.Security;
using MU5PrototypeProject.ViewModels;
using OfficeOpenXml;
using System.Security.Claims;

namespace MU5PrototypeProject.Controllers
{
    [Authorize]
    public class ReportsController : CognizantController
    {
        private readonly MUContext _context;

        public ReportsController(MUContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? preset, DateTime? startDate, DateTime? endDate, string? activeTab)
        {
            var filter = ResolveDateRange(preset, startDate, endDate);
            ViewData["activeTab"] = activeTab;

            var currentTrainerId = await GetCurrentTrainerIdAsync();
            IQueryable<Session> sessionsQuery = _context.Sessions
                .AsNoTracking()
                .Where(s => !s.IsArchived
                    && s.SessionDate >= filter.StartDate
                    && s.SessionDate <= filter.EndDate)
                .Include(s => s.Trainer)
                .Include(s => s.SessionClients).ThenInclude(sc => sc.Client)
                .Include(s => s.SessionClients).ThenInclude(sc => sc.Actions)
                    .ThenInclude(a => a.Exercise!)
                    .ThenInclude(e => e.Apparatus)
                .Include(s => s.SessionClients).ThenInclude(sc => sc.Actions)
                    .ThenInclude(a => a.ExerciseProps)
                    .ThenInclude(ep => ep.Prop);

            sessionsQuery = ApplyReportAccessScope(sessionsQuery, currentTrainerId);
            var sessions = await sessionsQuery.ToListAsync();

            var vm = new ReportsViewModel
            {
                Filter = filter,
                SessionStats = BuildSessionStatistics(sessions),
                ClientAnalytics = BuildClientAnalytics(sessions),
                TrainerWorkload = BuildTrainerWorkload(sessions, currentTrainerId, User),
                ExercisePopularity = BuildExercisePopularity(sessions)
            };

            return View(vm);
        }

        public async Task<IActionResult> ExportReportToExcel(string? preset, DateTime? startDate, DateTime? endDate, string report)
        {
            var filter = ResolveDateRange(preset, startDate, endDate);

            var currentTrainerId = await GetCurrentTrainerIdAsync();
            IQueryable<Session> sessionsQuery = _context.Sessions
                .AsNoTracking()
                .Where(s => !s.IsArchived
                    && s.SessionDate >= filter.StartDate
                    && s.SessionDate <= filter.EndDate)
                .Include(s => s.Trainer)
                .Include(s => s.SessionClients).ThenInclude(sc => sc.Client)
                .Include(s => s.SessionClients).ThenInclude(sc => sc.Actions)
                    .ThenInclude(a => a.Exercise!)
                    .ThenInclude(e => e.Apparatus)
                .Include(s => s.SessionClients).ThenInclude(sc => sc.Actions)
                    .ThenInclude(a => a.ExerciseProps)
                    .ThenInclude(ep => ep.Prop);

            sessionsQuery = ApplyReportAccessScope(sessionsQuery, currentTrainerId);
            var sessions = await sessionsQuery.ToListAsync();

            using var package = new ExcelPackage();
            var rangeLabel = $"{filter.StartDate:yyyy-MM-dd}_to_{filter.EndDate:yyyy-MM-dd}";
            string sheetTitle;

            switch (report)
            {
                case "sessions":
                    sheetTitle = "Session_Statistics";
                    WriteSessionStatisticsSheet(package, BuildSessionStatistics(sessions), filter);
                    break;
                case "clients":
                    sheetTitle = "Client_Analytics";
                    WriteClientAnalyticsSheet(package, BuildClientAnalytics(sessions), filter);
                    break;
                case "trainers":
                    sheetTitle = "Trainer_Workload";
                    WriteTrainerWorkloadSheet(package, BuildTrainerWorkload(sessions, currentTrainerId, User), filter);
                    break;
                case "exercises":
                    sheetTitle = "Exercise_Popularity";
                    WriteExercisePopularitySheet(package, BuildExercisePopularity(sessions), filter);
                    break;
                default:
                    return BadRequest("Invalid report type.");
            }

            var bytes = package.GetAsByteArray();
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{sheetTitle}_{rangeLabel}.xlsx");
        }

        private static void WriteSessionStatisticsSheet(ExcelPackage package, SessionStatistics stats, DateRangeFilter filter)
        {
            var ws = package.Workbook.Worksheets.Add("Session Statistics");
            int row = 1;

            ws.Cells[row, 1].Value = $"Session Statistics — {filter.StartDate:yyyy-MM-dd} to {filter.EndDate:yyyy-MM-dd}";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.Font.Size = 14;
            row += 2;

            ws.Cells[row, 1].Value = "Total Sessions"; ws.Cells[row, 2].Value = stats.TotalSessions; row++;
            ws.Cells[row, 1].Value = "Private"; ws.Cells[row, 2].Value = stats.PrivateCount; row++;
            ws.Cells[row, 1].Value = "Semi-Private"; ws.Cells[row, 2].Value = stats.SemiPrivateCount; row++;
            ws.Cells[row, 1].Value = "Physio"; ws.Cells[row, 2].Value = stats.PhysioCount; row++;
            row++;

            ws.Cells[row, 1].Value = "Day"; ws.Cells[row, 2].Value = "Total";
            ws.Cells[row, 3].Value = "Private"; ws.Cells[row, 4].Value = "Semi-Private"; ws.Cells[row, 5].Value = "Physio";
            using (var hr = ws.Cells[row, 1, row, 5])
            {
                hr.Style.Font.Bold = true;
                hr.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                hr.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
            }
            row++;

            foreach (var d in stats.ByDayOfWeek)
            {
                ws.Cells[row, 1].Value = d.DayOfWeek;
                ws.Cells[row, 2].Value = d.Total;
                ws.Cells[row, 3].Value = d.Private;
                ws.Cells[row, 4].Value = d.SemiPrivate;
                ws.Cells[row, 5].Value = d.Physio;
                row++;
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();
        }

        private static void WriteClientAnalyticsSheet(ExcelPackage package, ClientAnalytics analytics, DateRangeFilter filter)
        {
            var ws = package.Workbook.Worksheets.Add("Client Analytics");
            int row = 1;

            ws.Cells[row, 1].Value = $"Client Analytics — {filter.StartDate:yyyy-MM-dd} to {filter.EndDate:yyyy-MM-dd}";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.Font.Size = 14;
            row += 2;

            ws.Cells[row, 1].Value = "Active Clients"; ws.Cells[row, 2].Value = analytics.TotalActiveClients; row++;
            ws.Cells[row, 1].Value = "Avg Sessions / Client"; ws.Cells[row, 2].Value = analytics.AvgSessionsPerClient; row++;
            row++;

            ws.Cells[row, 1].Value = "Client"; ws.Cells[row, 2].Value = "Sessions";
            ws.Cells[row, 3].Value = "Last Session"; ws.Cells[row, 4].Value = "Most Frequent Type";
            using (var hr = ws.Cells[row, 1, row, 4])
            {
                hr.Style.Font.Bold = true;
                hr.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                hr.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
            }
            row++;

            foreach (var c in analytics.MostActiveClients)
            {
                ws.Cells[row, 1].Value = c.ClientName;
                ws.Cells[row, 2].Value = c.SessionCount;
                ws.Cells[row, 3].Value = c.LastSessionDate?.ToString("yyyy-MM-dd") ?? "N/A";
                ws.Cells[row, 4].Value = c.MostFrequentType;
                row++;
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();
        }

        private static void WriteTrainerWorkloadSheet(ExcelPackage package, TrainerWorkload workload, DateRangeFilter filter)
        {
            var ws = package.Workbook.Worksheets.Add("Trainer Workload");
            int row = 1;

            ws.Cells[row, 1].Value = $"Trainer Workload — {filter.StartDate:yyyy-MM-dd} to {filter.EndDate:yyyy-MM-dd}";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.Font.Size = 14;
            row += 2;

            ws.Cells[row, 1].Value = "Trainer"; ws.Cells[row, 2].Value = "Total";
            ws.Cells[row, 3].Value = "Private"; ws.Cells[row, 4].Value = "Semi-Private";
            ws.Cells[row, 5].Value = "Physio"; ws.Cells[row, 6].Value = "Unique Clients";
            using (var hr = ws.Cells[row, 1, row, 6])
            {
                hr.Style.Font.Bold = true;
                hr.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                hr.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
            }
            row++;

            foreach (var t in workload.Trainers)
            {
                ws.Cells[row, 1].Value = t.TrainerName;
                ws.Cells[row, 2].Value = t.TotalSessions;
                ws.Cells[row, 3].Value = t.PrivateCount;
                ws.Cells[row, 4].Value = t.SemiPrivateCount;
                ws.Cells[row, 5].Value = t.PhysioCount;
                ws.Cells[row, 6].Value = t.UniqueClients;
                row++;
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();
        }

        private static void WriteExercisePopularitySheet(ExcelPackage package, ExercisePopularity popularity, DateRangeFilter filter)
        {
            var ws = package.Workbook.Worksheets.Add("Exercise Popularity");
            int row = 1;

            ws.Cells[row, 1].Value = $"Exercise Popularity — {filter.StartDate:yyyy-MM-dd} to {filter.EndDate:yyyy-MM-dd}";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.Font.Size = 14;
            row += 2;

            // Top Exercises
            ws.Cells[row, 1].Value = "Top Exercises";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.Font.Size = 12;
            row++;

            ws.Cells[row, 1].Value = "Exercise"; ws.Cells[row, 2].Value = "Apparatus";
            ws.Cells[row, 3].Value = "Times Used"; ws.Cells[row, 4].Value = "Unique Clients";
            using (var hr = ws.Cells[row, 1, row, 4])
            {
                hr.Style.Font.Bold = true;
                hr.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                hr.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
            }
            row++;

            foreach (var e in popularity.TopExercises)
            {
                ws.Cells[row, 1].Value = e.ExerciseName;
                ws.Cells[row, 2].Value = e.ApparatusName;
                ws.Cells[row, 3].Value = e.TimesUsed;
                ws.Cells[row, 4].Value = e.UniqueClients;
                row++;
            }
            row++;

            // Apparatus Usage
            ws.Cells[row, 1].Value = "Apparatus Usage";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.Font.Size = 12;
            row++;

            ws.Cells[row, 1].Value = "Apparatus"; ws.Cells[row, 2].Value = "Exercises"; ws.Cells[row, 3].Value = "Total Usages";
            using (var hr = ws.Cells[row, 1, row, 3])
            {
                hr.Style.Font.Bold = true;
                hr.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                hr.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
            }
            row++;

            foreach (var a in popularity.ApparatusUsage)
            {
                ws.Cells[row, 1].Value = a.ApparatusName;
                ws.Cells[row, 2].Value = a.ExerciseCount;
                ws.Cells[row, 3].Value = a.TotalUsages;
                row++;
            }
            row++;

            // Prop Usage
            ws.Cells[row, 1].Value = "Prop Usage";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.Font.Size = 12;
            row++;

            ws.Cells[row, 1].Value = "Prop"; ws.Cells[row, 2].Value = "Times Used";
            using (var hr = ws.Cells[row, 1, row, 2])
            {
                hr.Style.Font.Bold = true;
                hr.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                hr.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
            }
            row++;

            foreach (var p in popularity.PropUsage)
            {
                ws.Cells[row, 1].Value = p.PropName;
                ws.Cells[row, 2].Value = p.TimesUsed;
                row++;
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();
        }

        private static DateRangeFilter ResolveDateRange(string? preset, DateTime? startDate, DateTime? endDate)
        {
            var today = DateTime.Today;
            preset ??= "ThisMonth";

            var filter = new DateRangeFilter { Preset = preset };

            switch (preset)
            {
                case "Last7Days":
                    filter.StartDate = today.AddDays(-7);
                    filter.EndDate = today;
                    break;
                case "Last3Months":
                    filter.StartDate = today.AddMonths(-3);
                    filter.EndDate = today;
                    break;
                case "ThisYear":
                    filter.StartDate = new DateTime(today.Year, 1, 1);
                    filter.EndDate = today;
                    break;
                case "Custom":
                    filter.StartDate = startDate ?? new DateTime(today.Year, today.Month, 1);
                    filter.EndDate = endDate ?? today;
                    break;
                default: // ThisMonth
                    filter.Preset = "ThisMonth";
                    filter.StartDate = new DateTime(today.Year, today.Month, 1);
                    filter.EndDate = today;
                    break;
            }

            return filter;
        }

        private IQueryable<Session> ApplyReportAccessScope(IQueryable<Session> sessions, int? currentTrainerId)
        {
            if (User.IsInRole(AppRoles.Trainer)
                && !User.IsInRole(AppRoles.Owner)
                && !User.IsInRole(AppRoles.Administration))
            {
                return currentTrainerId.HasValue
                    ? sessions.Where(s => s.TrainerID == currentTrainerId.Value)
                    : sessions.Where(s => false);
            }

            return sessions;
        }

        private static SessionStatistics BuildSessionStatistics(List<Session> sessions)
        {
            var stats = new SessionStatistics
            {
                TotalSessions = sessions.Count,
                PrivateCount = sessions.Count(s => s.SessionType == SessionType.Private),
                SemiPrivateCount = sessions.Count(s => s.SessionType == SessionType.SemiPrivate),
                PhysioCount = sessions.Count(s => s.SessionType == SessionType.Physio)
            };

            var dayOrder = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };

            stats.ByDayOfWeek = dayOrder
                .Select(day =>
                {
                    var daySessions = sessions.Where(s => s.SessionDate.DayOfWeek == day).ToList();
                    return new SessionsByDayRow
                    {
                        DayOfWeek = day.ToString(),
                        Total = daySessions.Count,
                        Private = daySessions.Count(s => s.SessionType == SessionType.Private),
                        SemiPrivate = daySessions.Count(s => s.SessionType == SessionType.SemiPrivate),
                        Physio = daySessions.Count(s => s.SessionType == SessionType.Physio)
                    };
                })
                .Where(r => r.Total > 0)
                .ToList();

            return stats;
        }

        private static ClientAnalytics BuildClientAnalytics(List<Session> sessions)
        {
            var participations = sessions
                .SelectMany(s => s.SessionClients.Select(sc => new
                {
                    sc.ClientID,
                    ClientName = sc.Client?.ClientName ?? "Unknown",
                    s.SessionDate,
                    s.SessionType
                }))
                .ToList();

            var clientGroups = participations
                .GroupBy(p => p.ClientID)
                .Select(g =>
                {
                    var mostFrequent = g
                        .GroupBy(p => p.SessionType)
                        .OrderByDescending(tg => tg.Count())
                        .First().Key;

                    return new ClientActivityRow
                    {
                        ClientName = g.First().ClientName,
                        SessionCount = g.Count(),
                        LastSessionDate = g.Max(p => p.SessionDate),
                        MostFrequentType = mostFrequent.ToString()
                    };
                })
                .OrderByDescending(c => c.SessionCount)
                .ToList();

            return new ClientAnalytics
            {
                TotalActiveClients = clientGroups.Count,
                AvgSessionsPerClient = clientGroups.Count > 0
                    ? Math.Round(clientGroups.Average(c => c.SessionCount), 1)
                    : 0,
                MostActiveClients = clientGroups.Take(15).ToList()
            };
        }

        private static TrainerWorkload BuildTrainerWorkload(List<Session> sessions, int? currentTrainerId, System.Security.Claims.ClaimsPrincipal user)
        {
            if (user.IsInRole(AppRoles.Trainer) && !user.IsInRole(AppRoles.Owner) && !user.IsInRole(AppRoles.Administration))
                sessions = sessions.Where(s => s.TrainerID == currentTrainerId).ToList();

            var trainers = sessions
                .GroupBy(s => s.TrainerID)
                .Select(g =>
                {
                    var uniqueClients = g
                        .SelectMany(s => s.SessionClients.Select(sc => sc.ClientID))
                        .Distinct()
                        .Count();

                    return new TrainerWorkloadRow
                    {
                        TrainerName = g.First().Trainer?.TrainerName ?? "Unknown",
                        TotalSessions = g.Count(),
                        UniqueClients = uniqueClients,
                        PrivateCount = g.Count(s => s.SessionType == SessionType.Private),
                        SemiPrivateCount = g.Count(s => s.SessionType == SessionType.SemiPrivate),
                        PhysioCount = g.Count(s => s.SessionType == SessionType.Physio)
                    };
                })
                .OrderByDescending(t => t.TotalSessions)
                .ToList();

            return new TrainerWorkload { Trainers = trainers };
        }

        private static ExercisePopularity BuildExercisePopularity(List<Session> sessions)
        {
            var actions = sessions
                .SelectMany(s => s.SessionClients.SelectMany(sc =>
                    sc.Actions.Select(a => new
                    {
                        a.ExerciseID,
                        ExerciseName = a.Exercise?.ExerciseName ?? "Unknown",
                        ApparatusName = a.Exercise?.Apparatus?.ApparatusName ?? "N/A",
                        ApparatusID = a.Exercise?.ApparatusID,
                        sc.ClientID,
                        ExerciseProps = a.ExerciseProps
                    })))
                .ToList();

            var topExercises = actions
                .GroupBy(a => a.ExerciseID)
                .Select(g => new ExercisePopularityRow
                {
                    ExerciseName = g.First().ExerciseName,
                    ApparatusName = g.First().ApparatusName,
                    TimesUsed = g.Count(),
                    UniqueClients = g.Select(a => a.ClientID).Distinct().Count()
                })
                .OrderByDescending(e => e.TimesUsed)
                .Take(15)
                .ToList();

            var apparatusUsage = actions
                .Where(a => a.ApparatusID != null)
                .GroupBy(a => a.ApparatusID)
                .Select(g => new ApparatusUsageRow
                {
                    ApparatusName = g.First().ApparatusName,
                    ExerciseCount = g.Select(a => a.ExerciseID).Distinct().Count(),
                    TotalUsages = g.Count()
                })
                .OrderByDescending(a => a.TotalUsages)
                .ToList();

            var propUsage = actions
                .SelectMany(a => a.ExerciseProps.Select(ep => new
                {
                    PropName = ep.Prop?.PropName ?? "Unknown"
                }))
                .GroupBy(p => p.PropName)
                .Select(g => new PropUsageRow
                {
                    PropName = g.Key,
                    TimesUsed = g.Count()
                })
                .OrderByDescending(p => p.TimesUsed)
                .Take(15)
                .ToList();

            return new ExercisePopularity
            {
                TopExercises = topExercises,
                ApparatusUsage = apparatusUsage,
                PropUsage = propUsage
            };
        }

        private async Task<int?> GetCurrentTrainerIdAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return null;
            return await _context.Trainers
                .AsNoTracking()
                .Where(t => t.IsActive && t.ApplicationUserId == userId)
                .Select(t => (int?)t.ID)
                .SingleOrDefaultAsync();
        }
    }
}
