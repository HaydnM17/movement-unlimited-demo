using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MU5PrototypeProject.Data;
using MU5PrototypeProject.Models;
using MU5PrototypeProject.Security;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

namespace MU5PrototypeProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MUContext _context;

        public HomeController(ILogger<HomeController> logger, MUContext context)
        {
            _logger = logger;
            _context = context;
        }

        private async Task<int?> GetCurrentTrainerIdAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            return await _context.Trainers
                .AsNoTracking()
                .Where(t => t.IsActive && t.ApplicationUserId == userId)
                .Select(t => (int?)t.ID)
                .SingleOrDefaultAsync();
        }

        public async Task<IActionResult> Index()
        {
            var sessions = _context.Sessions
                .Include(s => s.SessionClients)
                    .ThenInclude(sc => sc.Client)
                .Include(s => s.SessionClients)
                    .ThenInclude(sc => sc.SessionNotes)
                .Include(s => s.Trainer)
                .AsNoTracking()
                .AsQueryable();

            sessions = sessions.Where(s => !s.IsArchived);

            // Show opened upcoming sessions so scheduled future work is visible.
            sessions = sessions.Where(s => s.SessionDate > DateTime.Today && s.Status == SessionStatus.Opened);

            // Order by session date (earliest first)
            sessions = sessions.OrderBy(s => s.SessionDate);

            var sessionList = await sessions
                .Take(5)
                .ToListAsync();

            var currentTrainerId = await GetCurrentTrainerIdAsync();
            ViewData["EditableSessionIds"] = sessionList
                .Where(session => !session.IsArchived
                    && SessionAuthorizationHelper.CanOpenEditSession(session, User, currentTrainerId))
                .Select(session => session.ID)
                .ToHashSet();

            return View(sessionList);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}
