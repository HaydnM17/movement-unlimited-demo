using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore.Storage;
using MU5PrototypeProject.CustomController;
using MU5PrototypeProject.Data;
using MU5PrototypeProject.Models;
using MU5PrototypeProject.Security;
using MU5PrototypeProject.Utilities;
using System.Security.Claims;

namespace MU5PrototypeProject.Controllers
{
    public class ClientController : CognizantController
    {
        private readonly MUContext _context;

        public ClientController(MUContext context)
        {
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

        // GET: Client
        public async Task<IActionResult> Index(
            string SearchName,
            string SearchPhone,
            string actionButton,
            int? page,

            int? pageSizeID,
            bool showArchived = false,
            string sortDirection = "asc",
            string sortField = "Client")

        {
            try
            {
                //List of sort options.
                //NOTE: make sure this array has matching values to the column headings
                string[] sortOptions = new[] { "Client" };

                //Count the number of filters applied - start by assuming no filters
                ViewData["Filtering"] = "btn-outline-secondary";
                int numberFilters = 0;
                var clients = _context.Clients
                        .AsNoTracking();

                // Apply archived filter first
                if (!showArchived)
                {
                    clients = clients.Where(c => !c.IsArchived);
                }
                else
                {
                    numberFilters++; // count as a filter when showing archived
                }

                if (!string.IsNullOrEmpty(SearchName))
                {
                    var search = SearchName.ToUpper().Trim();

                    clients = clients.Where(s =>
                        s.FirstName.ToUpper().Contains(search)
                        || s.LastName.ToUpper().Contains(search)
                        || (s.FirstName.ToUpper() + " " + s.LastName.ToUpper()).Contains(search));

                    numberFilters++;
                }

                if (!string.IsNullOrEmpty(SearchPhone))
                {
                    var phoneSearch = SearchPhone.ToUpper().Trim();
                    clients = clients.Where(s => s.Phone != null && s.Phone.ToUpper().Contains(phoneSearch));
                    numberFilters++;
                }

                //Give feedback about the state of the filters
                if (numberFilters != 0)
                {
                    //Toggle the Open/Closed state of the collapse depending on if we are filtering
                    ViewData["Filtering"] = " btn-danger";
                    //Show how many filters have been applied
                    ViewData["numberFilters"] = "(" + numberFilters.ToString()
                        + " Filter" + (numberFilters > 1 ? "s" : "") + " Applied)";
                    //Keep the Bootstrap collapse open
                    @ViewData["ShowFilter"] = " show";
                }

                //Before we sort, see if we have called for a change of filtering or sorting
                if (!String.IsNullOrEmpty(actionButton)) //Form Submitted!
                {
                    if (sortOptions.Contains(actionButton))//Change of sort is requested
                    {
                        page = 1; //Reset page to start when changing sort or filters
                        if (actionButton == sortField) //Reverse order on same field
                        {
                            sortDirection = sortDirection == "asc" ? "desc" : "asc";
                        }
                        sortField = actionButton;//Sort by the button clicked
                    }
                }

                if (sortField == "Client")
                {
                    if (sortDirection == "asc")
                    {
                        clients = clients
                            .OrderBy(c => c.FirstName)
                            .ThenBy(c => c.LastName);

                    }
                    else
                    {
                        clients = clients
                            .OrderByDescending(c => c.FirstName)
                            .ThenBy(c => c.LastName);
                    }
                }
                ViewData["sortDirection"] = sortDirection;
                ViewData["sortField"] = sortField;
                ViewData["showArchived"] = showArchived;

                int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, ControllerName());
                ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);

                var pagedData = await PaginatedList<Client>.CreateAsync(clients.AsNoTracking(), page ?? 1, pageSize);

                return View(pagedData);
            }
            catch (Exception)
            {
                return Problem("Unable to load clients. Please try again later.");
            }
        }


        // GET: Client/Details/5
        public async Task<IActionResult> Details(int? id, int? page, int? pageSizeID,
            string actionButton = "", string sessionSortDirection = "desc",
            string sessionSortField = "Date", string PostResult = "Details")
        {
            if (id == null) return NotFound();

            var client = await _context.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);

            if (client == null) return NotFound();

            string[] sortOptions = new[] { "Date", "Type", "Trainer", "Sessions/Week" };

            if (!string.IsNullOrEmpty(actionButton) && sortOptions.Contains(actionButton))
            {
                page = 1;
                if (actionButton == sessionSortField)
                    sessionSortDirection = sessionSortDirection == "asc" ? "desc" : "asc";
                sessionSortField = actionButton;
            }

            var sessionParticipations = _context.SessionClients
                .Where(sc => sc.ClientID == id)
                .Include(sc => sc.Session!)
                    .ThenInclude(s => s!.Trainer)
                .Include(sc => sc.Session!)
                    .ThenInclude(s => s!.SessionClients)
                        .ThenInclude(participant => participant.SessionNotes)
                .AsTracking();

            sessionParticipations = sessionSortField switch
            {
                "Type" => sessionSortDirection == "asc"
                    ? sessionParticipations.OrderBy(sc => sc.Session!.SessionType)
                    : sessionParticipations.OrderByDescending(sc => sc.Session!.SessionType),
                "Trainer" => sessionSortDirection == "asc"
                    ? sessionParticipations.OrderBy(sc => sc.Session!.Trainer!.LastName)
                    : sessionParticipations.OrderByDescending(sc => sc.Session!.Trainer!.LastName),
                "Sessions/Week" => sessionSortDirection == "asc"
                    ? sessionParticipations.OrderBy(sc => sc.SessionsPerWeekRecommended)
                    : sessionParticipations.OrderByDescending(sc => sc.SessionsPerWeekRecommended),
                _ => sessionSortDirection == "asc"
                    ? sessionParticipations.OrderBy(sc => sc.Session!.SessionDate)
                    : sessionParticipations.OrderByDescending(sc => sc.Session!.SessionDate)
            };

            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, "ClientSessions");
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            ViewData["currentPageSize"] = pageSize.ToString();
            ViewData["sessionSortField"] = sessionSortField;
            ViewData["sessionSortDirection"] = sessionSortDirection;
            ViewData["PostResult"] = PostResult;

            int pageNumber = page ?? 1;
            int totalSessions = await sessionParticipations.CountAsync();
            int totalPages = (int)Math.Ceiling(totalSessions / (double)pageSize);
            pageNumber = Math.Max(1, Math.Min(pageNumber, totalPages == 0 ? 1 : totalPages));

            ViewData["sessionPage"] = pageNumber;
            ViewData["sessionTotalPages"] = totalPages;
            var pagedParticipations = await sessionParticipations
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var currentTrainerId = await GetCurrentTrainerIdAsync();
            ViewData["EditableSessionIds"] = pagedParticipations
                .Select(participation => participation.Session)
                .Where(session => session != null
                    && !session.IsArchived
                    && SessionAuthorizationHelper.CanOpenEditSession(session, User, currentTrainerId))
                .Select(session => session!.ID)
                .ToHashSet();

            ViewBag.PagedSessionParticipations = pagedParticipations;

            return View(client);
        }

        // GET: Client/Create
        [Authorize(Policy = AuthorizationPolicies.OwnerOrAdministration)]
        public IActionResult Create()
        {
            DateTime today = DateTime.Today;
            DateTime maxDate = today.AddYears(-7);   // Must be at least 7
            DateTime minDate = today.AddYears(-120);

            ViewData["MaxDOB"] = maxDate.ToString("yyyy-MM-dd");
            ViewData["MinDOB"] = minDate.ToString("yyyy-MM-dd");
            ViewData["DefaultDOB"] = maxDate.ToString("yyyy-MM-dd");

            return View();
        }

        // POST: Client/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.OwnerOrAdministration)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,FirstName,LastName,DOB,Phone,Email,ClientFolderUrl,IsArchived")] Client client)
        {
            try
            {
                if (ModelState.IsValid)
                {

                    //client.CreatedAt = DateTime.Now; // causing error deleting for now

                    _context.Add(client);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Details), new { client.ID, PostResult = "Create" });
                }
            }
            catch (RetryLimitExceededException)
            {
                ModelState.AddModelError("", "Unable to save changes after multiple attempts. " +
                    "Try again, and if the problem persists, see your system administrator.");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
            }

            return View(client);
        }

        // GET: Client/Edit/5
        [Authorize(Policy = AuthorizationPolicies.OwnerOrAdministration)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients.FindAsync(id);
            if (client == null)
            {
                return NotFound();
            }
            return View(client);
        }

        // POST: Client/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.OwnerOrAdministration)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id)
        {
            //Go get the client to update from the database
            var clientToUpdate = await _context.Clients.FirstOrDefaultAsync(c => c.ID == id);

            //Check if the client exists
            if (clientToUpdate == null)
            {
                return NotFound();
            }

            if (await TryUpdateModelAsync(clientToUpdate, "",
                c => c.FirstName,
                c => c.LastName,
                c => c.DOB,
                c => c.Phone,
                c => c.Email,
                c => c.ClientFolderUrl,
                c => c.IsArchived))  // CreatedAt removed here
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Details), new { id, PostResult = "Edit" });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClientExists(clientToUpdate.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (RetryLimitExceededException)
                {
                    ModelState.AddModelError("", "Unable to save changes after multiple attempts. " +
                        "Try again, and if the problem persists, see your system administrator.");
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                }
            }

            return View(clientToUpdate);
        }

        // GET: Client/Delete/5
        [Authorize(Policy = AuthorizationPolicies.OwnerOrAdministration)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);
            if (client == null)
            {
                return NotFound();
            }

            return View(client);
        }

        // POST: Client/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Policy = AuthorizationPolicies.OwnerOrAdministration)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var client = await _context.Clients
                .FirstOrDefaultAsync(m => m.ID == id);
            try
            {
                if (client != null)
                {
                    _context.Clients.Remove(client);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                //Log the error (uncomment ex variable name and write a log.)   
                ModelState.AddModelError("", "Unable to delete client. Try again, and if the problem persists see your system administrator.");
            }
            return View(client);
        }

        // POST: Client/Archive/5
        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.OwnerOrAdministration)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id)
        {
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.ID == id);
            if (client == null)
            {
                return NotFound();
            }

            client.IsArchived = true;

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to archive client. Try again, and if the problem persists see your system administrator.");
                return View("Details", client);
            }
        }

        // POST: Client/Unarchive/5
        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.OwnerOrAdministration)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unarchive(int id)
        {
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.ID == id);
            if (client == null)
            {
                return NotFound();
            }

            client.IsArchived = false;

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to unarchive client. Try again, and if the problem persists see your system administrator.");
                return View("Details", client);
            }
        }

        private bool ClientExists(int id)
        {
            return _context.Clients.Any(e => e.ID == id);
        }
    }
}
