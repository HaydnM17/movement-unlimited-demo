using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MU5PrototypeProject.CustomController;
using MU5PrototypeProject.Data;
using MU5PrototypeProject.Models;
using MU5PrototypeProject.Models.ViewModels;
using MU5PrototypeProject.Security;
using MU5PrototypeProject.Utilities;
using OfficeOpenXml;
using QuestPDF.Fluent;
using System.Globalization;
using System.Security.Claims;
using ModelAction = MU5PrototypeProject.Models.Action;

namespace MU5PrototypeProject.Controllers
{//random comment to test git pull request
    public class SessionController : CognizantController
    {
        private readonly MUContext _context;
        private const string CompletedOnlyAccessMessage =
            "Logged-stage notes and exercises were completed by another trainer. Only the Completed section is editable for your role.";
        private const string OpenedAddActionSessionClientTempDataKey = "OpenedAddActionSessionClientId";
        private bool _currentTrainerIdResolved;
        private int? _currentTrainerId;
        private static readonly string[] ImportHeaders =
{
            "SessionDate", "SessionType", "TrainerEmail",
            "PrimaryClientName", "PrimaryClientEmail", "PrimarySessionsPerWeek", "PrimaryGoals", "PrimaryGeneralComments", "PrimarySubjectiveReports", "PrimaryObjectiveFindings", "PrimaryPlan",
            "PrimaryNextAppointmentBooked", "PrimaryCommunicatedProgress", "PrimaryReadyToProgress", "PrimaryCourseCorrectionNeeded", "PrimaryTeamConsult", "PrimaryReferredExternally", "PrimaryReferredTo",
            "PrimaryIsPaid", "PrimaryAdminNotes", "PrimaryAdminInitials",
            "SecondaryClientName", "SecondaryClientEmail", "SecondarySessionsPerWeek", "SecondaryGoals", "SecondaryGeneralComments", "SecondarySubjectiveReports", "SecondaryObjectiveFindings", "SecondaryPlan",
            "SecondaryNextAppointmentBooked", "SecondaryCommunicatedProgress", "SecondaryReadyToProgress", "SecondaryCourseCorrectionNeeded", "SecondaryTeamConsult", "SecondaryReferredExternally", "SecondaryReferredTo",
            "SecondaryIsPaid", "SecondaryAdminNotes", "SecondaryAdminInitials",
            "AccessoriesHeadPad", "AccessoriesStrapsOrHandles", "AccessoriesGearBar", "AccessoriesStopperSettings", "AccessoriesRubberPads", "AccessoriesHeadRest", "AccessoriesTowel", "AccessoriesPosturePillow",
            "PhysioAssessment", "InsuranceCompany", "CoverageAmountPerYear", "AmountUsed", "CoverageResetsDate", "PhysiotherapistName", "CoverageShared", "CommunicatedWithPhysio",
            "Exercises"
        };

        public SessionController(MUContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string? SearchSessionDate,
            int? TrainerID,
            string SearchClient,
            SessionLifecycleFilter? SearchLifecycle,  
            string actionButton,
            int? page,
            int? pageSizeID,
            bool showArchived = false,
            bool showUpcoming = false,
            string sortDirection = "asc",
            string sortField = "Session")
        {
            string[] sortOptions = { "Client", "Trainer", "SessionDate" };
            var effectiveShowArchived = showArchived
                || SearchLifecycle == SessionLifecycleFilter.Canceled
                || showUpcoming;

            ViewData["Filtering"] = "btn-outline-secondary";
            int numberFilters = 0;

            PopulateDropDownLists();

            var sessions = _context.Sessions
                .Include(s => s.SessionClients)
                    .ThenInclude(sc => sc.Client)
                .Include(s => s.SessionClients)
                    .ThenInclude(sc => sc.SessionNotes)
                .Include(s => s.Trainer)
                .AsNoTracking()
                .AsQueryable();

            if (!effectiveShowArchived)
            {
                sessions = sessions.Where(s => !s.IsArchived);
            }
            else if (showArchived)
            {
                numberFilters++;
            }

            if (!string.IsNullOrWhiteSpace(SearchClient))
            {
                var search = SearchClient.ToUpper().Trim();

                sessions = sessions.Where(s =>
                    s.SessionClients.Any(sc =>
                        sc.Client != null &&
                        (
                            sc.Client.FirstName.ToUpper().Contains(search) ||
                            sc.Client.LastName.ToUpper().Contains(search) ||
                            (sc.Client.FirstName.ToUpper() + " " + sc.Client.LastName.ToUpper()).Contains(search)
                        )));

                numberFilters++;
            }

            if (TrainerID.HasValue)
            {
                sessions = sessions.Where(p => p.TrainerID == TrainerID);
                numberFilters++;
            }

            if (showUpcoming)
            {
                sessions = sessions.Where(s => s.SessionDate >= DateTime.Today);
                numberFilters++;
            }

            if (SearchLifecycle.HasValue)
            {
                sessions = SearchLifecycle.Value switch
                {
                    SessionLifecycleFilter.Canceled => sessions.Where(s => s.IsCanceled),
                    SessionLifecycleFilter.Opened => sessions.Where(s => !s.IsCanceled && s.Status == SessionStatus.Opened),
                    SessionLifecycleFilter.Logged => sessions.Where(s => !s.IsCanceled && s.Status == SessionStatus.Logged),
                    SessionLifecycleFilter.Completed => sessions.Where(s => !s.IsCanceled && s.Status == SessionStatus.Completed),
                    _ => sessions
                };
                numberFilters++;
            }

            if (numberFilters != 0)
            {
                ViewData["Filtering"] = " btn-danger";
                ViewData["numberFilters"] = "(" + numberFilters + " Filter" + (numberFilters > 1 ? "s" : "") + " Applied)";
                ViewData["ShowFilter"] = " show";
            }

            if (!string.IsNullOrEmpty(actionButton))
            {
                page = 1;

                if (sortOptions.Contains(actionButton))
                {
                    if (actionButton == sortField)
                    {
                        sortDirection = sortDirection == "asc" ? "desc" : "asc";
                    }

                    sortField = actionButton;
                }
            }

            if (sortField == "Client")
            {
                sessions = sortDirection == "asc"
                    ? sessions
                        .OrderBy(p => p.SessionClients
                            .Where(sc => sc.ParticipantOrder == 1)
                            .Select(sc => sc.Client!.FirstName)
                            .FirstOrDefault())
                        .ThenBy(p => p.SessionClients
                            .Where(sc => sc.ParticipantOrder == 1)
                            .Select(sc => sc.Client!.LastName)
                            .FirstOrDefault())
                    : sessions
                        .OrderByDescending(p => p.SessionClients
                            .Where(sc => sc.ParticipantOrder == 1)
                            .Select(sc => sc.Client!.FirstName)
                            .FirstOrDefault())
                        .ThenByDescending(p => p.SessionClients
                            .Where(sc => sc.ParticipantOrder == 1)
                            .Select(sc => sc.Client!.LastName)
                            .FirstOrDefault());
            }
            else if (sortField == "Trainer")
            {
                sessions = sortDirection == "asc"
                    ? sessions.OrderBy(p => p.Trainer!.LastName)
                    : sessions.OrderByDescending(p => p.Trainer!.LastName);
            }
            else
            {
                sessions = sortDirection == "asc"
                    ? sessions.OrderByDescending(p => p.SessionDate)
                        .ThenBy(p => p.SessionClients
                            .Where(sc => sc.ParticipantOrder == 1)
                            .Select(sc => sc.Client!.FirstName)
                            .FirstOrDefault())
                        .ThenBy(p => p.Trainer!.LastName)
                    : sessions.OrderBy(p => p.SessionDate)
                        .ThenBy(p => p.SessionClients
                            .Where(sc => sc.ParticipantOrder == 1)
                            .Select(sc => sc.Client!.FirstName)
                            .FirstOrDefault())
                        .ThenBy(p => p.Trainer!.LastName);
            }

            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;
            ViewData["showArchived"] = effectiveShowArchived;
            ViewData["SearchLifecycle"] = SearchLifecycle?.ToString() ?? string.Empty;
            ViewData["showUpcoming"] = showUpcoming;

            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, ControllerName());
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);

            var pagedData = await PaginatedList<Session>.CreateAsync(sessions.AsNoTracking(), page ?? 1, pageSize);
            await PopulateCompletedSessionCountsAsync(pagedData);
            ViewData["EditableSessionIds"] = await GetEditableSessionIdsAsync(pagedData);
            return View(pagedData);
        }

        private async Task PopulateCompletedSessionCountsAsync(IEnumerable<Session> sessions)
        {
            var clientIds = sessions
                .SelectMany(s => s.OrderedSessionClients)
                .Select(sc => sc.ClientID)
                .Distinct()
                .ToList();

            if (clientIds.Count == 0)
            {
                return;
            }

            var completedSessionCountsByClientId = await _context.SessionClients
                .Where(sc => clientIds.Contains(sc.ClientID)
                    && sc.Session != null
                    && !sc.Session.IsCanceled
                    && sc.Session.Status == SessionStatus.Completed)
                .GroupBy(sc => sc.ClientID)
                .Select(g => new
                {
                    ClientId = g.Key,
                    TotalCompletedSessions = g.Count()
                })
                .ToDictionaryAsync(x => x.ClientId, x => x.TotalCompletedSessions);

            foreach (var session in sessions)
            {
                if (session.ClientID is int primaryClientId
                    && completedSessionCountsByClientId.TryGetValue(primaryClientId, out var primaryCount))
                {
                    session.PrimaryCompletedSessionsCount = primaryCount;
                }

                if (session.Client2ID is int secondaryClientId
                    && completedSessionCountsByClientId.TryGetValue(secondaryClientId, out var secondaryCount))
                {
                    session.SecondaryCompletedSessionsCount = secondaryCount;
                }
            }
        }

        public async Task<IActionResult> Details(int? id, string PostResult = "Details")
        {
            if (id == null)
            {
                return NotFound();
            }

            var session = await SessionDetailsQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ID == id);

            if (session == null)
            {
                return NotFound();
            }

            ViewData["CanEditSession"] = CanCurrentUserOpenEditSession(session, await GetCurrentTrainerIdAsync());
            ViewData["CanCancelSession"] = SessionAuthorizationHelper.CanCancelSession(session, User, DateTime.Today);
            ViewData["IsCancelEligibleSession"] = SessionAuthorizationHelper.IsCancelEligible(session, DateTime.Today);
            ViewData["CanRestoreCanceledSession"] = SessionAuthorizationHelper.CanRestoreCanceledSession(session, User);
            ViewData["PostResult"] = PostResult;
            return View(session);
        }

        public IActionResult Create(int? clientId)
        {
            DateTime today = DateTime.Today;

            var vm = new SessionUpsertVM
            {
                SessionDate = today,
                ClientID = clientId,
                CurrentStatus = SessionStatus.Opened,
                Status = SessionStatus.Opened
            };

            EnsureUpsertViewModelChildren(vm);
            PopulateDropDownLists(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SessionUpsertVM vm)
        {
            PrepareUpsertViewModel(vm);
            if (!HasPostedFormValues(nameof(SessionUpsertVM.Accessories)))
            {
                RemoveModelStatePrefix(nameof(SessionUpsertVM.Accessories));
            }

            vm.CurrentStatus = SessionStatus.Opened;
            var targetStatus = SessionStatus.Opened;
            vm.Status = SessionStatus.Opened;
            EnforceRoleBasedWorkflowAccess(vm, targetStatus);

            if (!CanTransition(SessionStatus.Opened, targetStatus))
            {
                ModelState.AddModelError(nameof(SessionUpsertVM.Status),
                    $"Cannot change session status from {SessionStatus.Opened} to {targetStatus}.");
            }

            ValidateForStatus(vm, targetStatus);

            try
            {
                if (ModelState.IsValid)
                {
                    var session = new Session();
                    ApplySessionValues(session, vm);

                    foreach (var participant in BuildSessionClients(vm))
                    {
                        session.SessionClients.Add(participant);
                    }

                    _context.Sessions.Add(session);
                    await _context.SaveChangesAsync();

                    await CreateSelectedActions(session.ID, vm.SelectedExerciseIDs);

                    var workflowEntryStatus = ResolveDefaultWorkflowEntryStatus(SessionStatus.Opened);
                    if (workflowEntryStatus != SessionStatus.Opened)
                    {
                        return RedirectToAction(nameof(Edit), new { id = session.ID, workflowStatus = workflowEntryStatus });
                    }

                    return RedirectToAction(nameof(Details), new { id = session.ID, PostResult = "Create" });
                }
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
            }

            EnsureUpsertViewModelChildren(vm);
            PopulateDropDownLists(vm);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ClientSessionHistory(
            int clientId,
            int offset = 0,
            int? currentSessionId = null,
            DateTime? referenceDate = null)
        {
            var query = BuildPreviousSessionsForClientQuery(clientId, referenceDate, currentSessionId);

            var totalCount = await query.CountAsync();
            if (totalCount == 0 || offset < 0 || offset >= totalCount)
                return Json(new { totalCount, found = false });

            var session = await query
                .Include(s => s.Trainer)
                .Include(s => s.SessionClients)
                    .ThenInclude(sc => sc.Client)
                .Include(s => s.SessionClients.Where(sc => sc.ClientID == clientId))
                    .ThenInclude(sc => sc.SessionNotes)
                .Include(s => s.SessionClients.Where(sc => sc.ClientID == clientId))
                    .ThenInclude(sc => sc.NextSteps)
                .Include(s => s.SessionClients.Where(sc => sc.ClientID == clientId))
                    .ThenInclude(sc => sc.AdminComplete)
                .Include(s => s.SessionClients.Where(sc => sc.ClientID == clientId))
                    .ThenInclude(sc => sc.Accessories)
                .Include(s => s.SessionClients.Where(sc => sc.ClientID == clientId))
                    .ThenInclude(sc => sc.Actions)
                        .ThenInclude(a => a.Exercise)
                            .ThenInclude(e => e!.Apparatus)
                .Include(s => s.SessionClients.Where(sc => sc.ClientID == clientId))
                    .ThenInclude(sc => sc.Actions)
                        .ThenInclude(a => a.ExerciseProps)
                            .ThenInclude(ep => ep.Prop)
                .Include(s => s.PhysioInfo)
                .AsSplitQuery()
                .AsNoTracking()
                .Skip(offset)
                .FirstOrDefaultAsync();

            if (session == null)
                return Json(new { totalCount, found = false });

            var sc = session.SessionClients.FirstOrDefault(c => c.ClientID == clientId);
            var primaryParticipant = session.SessionClients
                .FirstOrDefault(c => c.ParticipantOrder == 1);
            var secondaryParticipant = session.SessionClients
                .FirstOrDefault(c => c.ParticipantOrder == 2);
            var dto = new SessionHistoryDto
            {
                SessionId = session.ID,
                SessionDate = session.SessionDate.ToString("yyyy-MM-dd"),
                Offset = offset,
                TotalCount = totalCount,
                SessionTypeValue = (int)session.SessionType,
                SessionTypeText = session.SessionType.ToString(),
                TrainerId = session.TrainerID,
                TrainerName = session.Trainer?.TrainerName ?? "Not assigned",
                ClientId = primaryParticipant?.ClientID,
                ClientName = primaryParticipant?.Client?.FullName ?? "Not assigned",
                Client2Id = secondaryParticipant?.ClientID,
                Client2Name = secondaryParticipant?.Client?.FullName ?? "Not assigned",
                Actions = sc?.Actions
                    .OrderBy(a => a.ID)
                    .Select(a => new SessionHistoryActionDto
                    {
                        Id = a.ID,
                        ExerciseName = a.Exercise?.ExerciseName ?? "Excercise",
                        ApparatusName = a.Exercise?.Apparatus?.ApparatusName,
                        Springs = a.Springs,
                        PropNames = a.ExerciseProps
                            .Select(ep => ep.Prop?.PropName)
                            .Where(name => !string.IsNullOrWhiteSpace(name))
                            .Select(name => name!)
                            .ToList(),
                        Notes = a.Notes
                    })
                    .ToList() ?? new List<SessionHistoryActionDto>(),
                SessionsPerWeekRecommended = sc?.SessionsPerWeekRecommended,
                // SessionNotes
                Goals = sc?.SessionNotes?.Goals,
                GeneralComments = sc?.SessionNotes?.GeneralComments,
                SubjectiveReports = sc?.SessionNotes?.SubjectiveReports,
                ObjectiveFindings = sc?.SessionNotes?.ObjectiveFindings,
                Plan = sc?.SessionNotes?.Plan,
                // NextSteps
                NextAppointmentBooked = sc?.NextSteps?.NextAppointmentBooked ?? false,
                CommunicatedProgress = sc?.NextSteps?.CommunicatedProgress ?? false,
                ReadyToProgress = sc?.NextSteps?.ReadyToProgress ?? false,
                CourseCorrectionNeeded = sc?.NextSteps?.CourseCorrectionNeeded ?? false,
                TeamConsult = sc?.NextSteps?.TeamConsult ?? false,
                ReferredExternally = sc?.NextSteps?.ReferredExternally ?? false,
                ReferredTo = sc?.NextSteps?.ReferredTo,
                // AdminComplete
                IsPaid = sc?.AdminComplete?.IsPaid,
                AdminNotes = sc?.AdminComplete?.AdminNotes,
                AdminInitials = sc?.AdminComplete?.AdminInitials,
                // Accessories
                HeadPad = sc?.Accessories?.HeadPad.ToString(),
                StrapsOrHandles = sc?.Accessories?.StrapsOrHandles.ToString(),
                GearBar = sc?.Accessories?.GearBar,
                StopperSettings = sc?.Accessories?.StopperSettings,
                RubberPads = sc?.Accessories?.RubberPads,
                HeadRest = sc?.Accessories?.HeadRest,
                Towel = sc?.Accessories?.Towel,
                PosturePillow = sc?.Accessories?.PosturePillow,
                // PhysioInfo
                PhysioAssessment = session.PhysioInfo?.PhysioAssessment,
                InsuranceCompany = session.PhysioInfo?.InsuranceCompany,
                CoverageAmountPerYear = session.PhysioInfo?.CoverageAmountPerYear,
                AmountUsed = session.PhysioInfo?.AmountUsed,
                CoverageResetsDate = session.PhysioInfo?.CoverageResetsDate?.ToString("yyyy-MM-dd"),
                PhysiotherapistName = session.PhysioInfo?.PhysiotherapistName,
                CoverageShared = session.PhysioInfo?.CoverageShared,
                CommunicatedWithPhysio = session.PhysioInfo?.CommunicatedWithPhysio
            };

            return Json(dto);
        }

        public async Task<IActionResult> Edit(int? id, string? openSection, SessionStatus? workflowStatus)
        {
            if (id == null)
            {
                return NotFound();
            }

            var session = await SessionDetailsQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ID == id);

            if (session == null)
            {
                return NotFound();
            }

            var currentTrainerId = await GetCurrentTrainerIdAsync();
            if (!CanCurrentUserOpenEditSession(session, currentTrainerId))
            {
                return Forbid();
            }

            var vm = BuildSessionUpsertViewModel(session);
            vm.Status = ResolveWorkflowEntryStatus(vm.CurrentStatus, workflowStatus);

            var canStartLoggedStage = SessionAuthorizationHelper.CanStartLoggedStage(User, currentTrainerId);
            if (vm.CurrentStatus == SessionStatus.Opened && vm.Status == SessionStatus.Logged && !canStartLoggedStage)
            {
                vm.Status = SessionStatus.Opened;
            }

            if (SessionAuthorizationHelper.ShouldDefaultToCompletedStage(session, User, currentTrainerId))
            {
                vm.Status = SessionStatus.Completed;
            }

            Session? previousSession = null;
            if (vm.ClientID.HasValue)
            {
                previousSession = await GetPreviousSessionForClientAsync(vm.ClientID.Value, session.SessionDate, session.ID);
            }

            Session? previousSecondarySession = null;
            if (vm.Client2ID.HasValue
                && (vm.Status == SessionStatus.Logged
                    || (vm.Status == SessionStatus.Completed && vm.SessionType == SessionType.Physio)))
            {
                previousSecondarySession = await GetPreviousSessionForClientAsync(vm.Client2ID.Value, session.SessionDate, session.ID);
            }

            ApplyCarryForwardValuesForEdit(vm, session, previousSession, previousSecondarySession);

            ViewData["PreviousActions"] = previousSession?.Actions
                .OrderBy(a => a.ID)
                .ToList() ?? new List<ModelAction>();
            ViewData["PreviousActionsSessionDate"] = previousSession?.SessionDate;

            PopulateDropDownLists(vm);
            PopulateEditViewAccessState(session, currentTrainerId);
            ViewData["OpenSection"] = openSection;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SessionUpsertVM vm, int? addActionSessionClientId, bool continueToAddAction = false)
        {
            if (id != vm.ID)
            {
                return NotFound();
            }

            var sessionToUpdate = await SessionDetailsQuery()
                .FirstOrDefaultAsync(s => s.ID == id);

            if (sessionToUpdate == null)
            {
                return NotFound();
            }

            var currentTrainerId = await GetCurrentTrainerIdAsync();
            var currentStatus = sessionToUpdate.Status;
            vm.CurrentStatus = currentStatus;

            if (continueToAddAction && currentStatus == SessionStatus.Opened)
            {
                if (!CanEditTeacherSection()
                    || !SessionAuthorizationHelper.CanStartLoggedStage(User, currentTrainerId)
                    || sessionToUpdate.SessionDate.Date > DateTime.Today)
                {
                    return Forbid();
                }

                var requestedSessionClientId = addActionSessionClientId.GetValueOrDefault();
                var targetSessionClient = sessionToUpdate.SessionClients
                    .FirstOrDefault(sc => sc.ID == requestedSessionClientId);

                if (requestedSessionClientId <= 0 || targetSessionClient == null)
                {
                    return NotFound();
                }

                vm.SessionNotes ??= new SessionNotes();
                vm.NextSteps ??= new NextSteps();
                NormalizeSessionNotes(vm.SessionNotes);
                NormalizeNextSteps(vm.NextSteps);
                if (sessionToUpdate.PrimarySessionClient != null)
                {
                    sessionToUpdate.PrimarySessionClient.SessionsPerWeekRecommended =
                        vm.SessionsPerWeekRecommended ?? sessionToUpdate.PrimarySessionClient.SessionsPerWeekRecommended;
                }
                ApplySessionNotesForOpenedAddActionNavigation(sessionToUpdate.PrimarySessionClient, vm.SessionNotes);
                ApplyNextStepsForOpenedAddActionNavigation(sessionToUpdate.PrimarySessionClient, vm.NextSteps);
                NormalizeAccessories(vm.Accessories);
                ApplyAccessoriesForOpenedAddActionNavigation(sessionToUpdate.PrimarySessionClient, vm.Accessories);

                if (sessionToUpdate.SessionType == SessionType.SemiPrivate)
                {
                    vm.Client2SessionNotes ??= new SessionNotes();
                    NormalizeSessionNotes(vm.Client2SessionNotes);
                    if (sessionToUpdate.SecondarySessionClient != null)
                    {
                        sessionToUpdate.SecondarySessionClient.SessionsPerWeekRecommended =
                            vm.Client2SessionsPerWeekRecommended ?? sessionToUpdate.SecondarySessionClient.SessionsPerWeekRecommended;
                    }
                    ApplySessionNotesForOpenedAddActionNavigation(sessionToUpdate.SecondarySessionClient, vm.Client2SessionNotes);
                    ApplyNextStepsForOpenedAddActionNavigation(sessionToUpdate.SecondarySessionClient, vm.NextSteps);
                    NormalizeAccessories(vm.Client2Accessories);
                    ApplyAccessoriesForOpenedAddActionNavigation(sessionToUpdate.SecondarySessionClient, vm.Client2Accessories);
                }

                await _context.SaveChangesAsync();
                TempData[OpenedAddActionSessionClientTempDataKey] = requestedSessionClientId.ToString(CultureInfo.InvariantCulture);

                return RedirectToAction(nameof(AddAction), new { sessionClientId = requestedSessionClientId });
            }

            var targetStatus = ResolveTargetStatus(vm, currentStatus);
            vm.Status = targetStatus;

            if (!CanCurrentUserEditTargetStage(sessionToUpdate, targetStatus, currentTrainerId))
            {
                return Forbid();
            }

            if (targetStatus != SessionStatus.Opened)
            {
                RestoreReadOnlyEditGeneralValues(
                    vm,
                    sessionToUpdate,
                    includeSessionsPerWeek: targetStatus != SessionStatus.Logged);
            }

            PrepareUpsertViewModel(vm);
            RestoreReadOnlyWorkflowStageValues(vm, sessionToUpdate, targetStatus);
            EnforceRoleBasedWorkflowAccess(vm, targetStatus, sessionToUpdate);

            if (!CanTransition(currentStatus, targetStatus))
            {
                ModelState.AddModelError(nameof(SessionUpsertVM.Status),
                    $"Cannot change session status from {currentStatus} to {targetStatus}.");
            }

            if (currentStatus == SessionStatus.Opened && targetStatus == SessionStatus.Opened)
            {
                ModelState.AddModelError(nameof(SessionUpsertVM.Status),
                    "Opened stage is locked after the initial save. Use the workflow action to continue in Logged.");
            }

            var continueToAddActionViaLoggedFlow = addActionSessionClientId.HasValue;
            ValidateForStatus(vm, targetStatus, sessionToUpdate, continueToAddActionViaLoggedFlow);

            try
            {
                if (ModelState.IsValid)
                {
                    var loggedStageOwnerTrainerId = ResolveLoggedStageOwnerTrainerIdForSave(
                        sessionToUpdate,
                        currentStatus,
                        targetStatus,
                        currentTrainerId);

                    ApplySessionValues(sessionToUpdate, vm);

                    if (continueToAddActionViaLoggedFlow)
                    {
                        ApplySessionClientValuesForAddActionNavigation(sessionToUpdate, vm, loggedStageOwnerTrainerId);
                        await _context.SaveChangesAsync();

                        var requestedSessionClientId = addActionSessionClientId.GetValueOrDefault();
                        var targetSessionClient = sessionToUpdate.SessionClients
                            .FirstOrDefault(sc => sc.ID == requestedSessionClientId);

                        if (targetSessionClient != null)
                        {
                            return RedirectToAction(nameof(AddAction), new { sessionClientId = targetSessionClient.ID });
                        }

                        ModelState.AddModelError(string.Empty,
                            "Unable to open Add Action because the selected participant could not be found for this session.");
                        vm.ExistingActions = sessionToUpdate.Actions
                            .OrderBy(a => a.SessionClient?.ParticipantOrder)
                            .ThenBy(a => a.ID)
                            .ToList();
                        EnsureUpsertViewModelChildren(vm);
                        PopulateDropDownLists(vm);
                        PopulateEditViewAccessState(sessionToUpdate, currentTrainerId);
                        return View(vm);
                    }
                    else
                    {
                        await using var transaction = await _context.Database.BeginTransactionAsync();

                        var existingParticipantsByOrder = sessionToUpdate.SessionClients
                            .ToDictionary(sc => sc.ParticipantOrder);

                        if (sessionToUpdate.SessionClients.Any())
                        {
                            _context.SessionClients.RemoveRange(sessionToUpdate.SessionClients.ToList());
                            await _context.SaveChangesAsync();
                            sessionToUpdate.SessionClients.Clear();
                        }

                        foreach (var participant in BuildSessionClients(vm, existingParticipantsByOrder, loggedStageOwnerTrainerId))
                        {
                            sessionToUpdate.SessionClients.Add(participant);
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }

                    if (targetStatus == SessionStatus.Logged)
                    {
                        var workflowEntryStatus = ResolveDefaultWorkflowEntryStatus(targetStatus);
                        if (workflowEntryStatus != targetStatus)
                        {
                            return RedirectToAction(nameof(Edit), new { id = sessionToUpdate.ID, workflowStatus = workflowEntryStatus });
                        }
                    }

                    if (targetStatus == SessionStatus.Completed)
                    {
                        return RedirectToAction(nameof(Details), new { id = sessionToUpdate.ID });
                    }

                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
            }

            vm.ExistingActions = sessionToUpdate.Actions
                .OrderBy(a => a.SessionClient?.ParticipantOrder)
                .ThenBy(a => a.ID)
                .ToList();
            EnsureUpsertViewModelChildren(vm);
            PopulateDropDownLists(vm);
            PopulateEditViewAccessState(sessionToUpdate, currentTrainerId);
            return View(vm);
        }

        [Authorize(Policy = AuthorizationPolicies.OwnerOrAdministration)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sessionExists = await _context.Sessions.AnyAsync(s => s.ID == id);
            if (!sessionExists)
            {
                return NotFound();
            }

            TempData["ErrorMessage"] = "Sessions can no longer be permanently deleted. Use Archive or Cancel Session instead.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Policy = AuthorizationPolicies.OwnerOrAdministration)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sessionExists = await _context.Sessions.AnyAsync(s => s.ID == id);
            if (!sessionExists)
            {
                return NotFound();
            }

            TempData["ErrorMessage"] = "Sessions can no longer be permanently deleted. Use Archive or Cancel Session instead.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(SessionCancelRequest request)
        {
            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.ID == request.Id);
            if (session == null)
            {
                return NotFound();
            }

            if (!User.IsInRole(AppRoles.Owner) && !User.IsInRole(AppRoles.Administration))
            {
                return Forbid();
            }

            if (!SessionAuthorizationHelper.IsCancelEligible(session, DateTime.Today))
            {
                TempData["ErrorMessage"] = "Only future Opened sessions can be canceled.";
                return RedirectToAction(nameof(Details), new { id = request.Id });
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(error => error.ErrorMessage)
                    .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message))
                    ?? "A cancellation reason is required.";
                return RedirectToAction(nameof(Details), new { id = request.Id });
            }

            session.IsCanceled = true;
            session.IsArchived = true;
            session.CancellationReason = request.CancellationReason.Trim();
            session.CanceledOn = DateTime.UtcNow;
            session.CanceledBy = _context.UserName;

            try
            {
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = "Session canceled and archived.";
                return RedirectToAction(nameof(Details), new { id = request.Id });
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "Unable to cancel session. Try again, and if the problem persists see your system administrator.";
                return RedirectToAction(nameof(Details), new { id = request.Id });
            }
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.OwnerOrAdministration)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id)
        {
            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.ID == id);
            if (session == null)
            {
                return NotFound();
            }

            if (SessionAuthorizationHelper.IsCancelEligible(session, DateTime.Today))
            {
                if (!User.IsInRole(AppRoles.Owner) && !User.IsInRole(AppRoles.Administration))
                {
                    return Forbid();
                }

                TempData["ErrorMessage"] = "Future Opened sessions must be canceled instead of archived.";
                return RedirectToAction(nameof(Details), new { id });
            }

            session.IsArchived = true;

            try
            {
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = "Session archived.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "Unable to archive session. Try again, and if the problem persists see your system administrator.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.OwnerOrAdministration)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unarchive(int id)
        {
            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.ID == id);
            if (session == null)
            {
                return NotFound();
            }

            if (session.IsCanceled)
            {
                if (!SessionAuthorizationHelper.CanRestoreCanceledSession(session, User))
                {
                    return Forbid();
                }

                session.IsArchived = false;
                session.IsCanceled = false;
                session.CancellationReason = null;
                session.CanceledOn = null;
                session.CanceledBy = null;

                try
                {
                    await _context.SaveChangesAsync();
                    TempData["StatusMessage"] = "Canceled session restored.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                catch (DbUpdateException)
                {
                    TempData["ErrorMessage"] = "Unable to restore session. Try again, and if the problem persists see your system administrator.";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            session.IsArchived = false;

            try
            {
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = "Session unarchived.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "Unable to unarchive session. Try again, and if the problem persists see your system administrator.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        public async Task<IActionResult> AddAction(int sessionClientId)
        {
            if (!CanEditTeacherSection())
            {
                return Forbid();
            }

            var currentTrainerId = await GetCurrentTrainerIdAsync();
            var sessionClient = await _context.SessionClients
                .Include(sc => sc.Session)
                    .ThenInclude(s => s!.SessionClients)
                        .ThenInclude(participant => participant.SessionNotes)
                .FirstOrDefaultAsync(sc => sc.ID == sessionClientId);

            if (sessionClient?.Session == null)
            {
                return NotFound();
            }

            var canContinueOpenedAddAction = sessionClient.Session.Status == SessionStatus.Opened
                && SessionAuthorizationHelper.CanStartLoggedStage(User, currentTrainerId)
                && sessionClient.Session.SessionDate.Date <= DateTime.Today
                && TempData.Peek(OpenedAddActionSessionClientTempDataKey) is string openedAddActionSessionClientIdValue
                && int.TryParse(openedAddActionSessionClientIdValue, out var openedAddActionSessionClientId)
                && openedAddActionSessionClientId == sessionClientId;

            if (!canContinueOpenedAddAction
                && (sessionClient.Session.Status != SessionStatus.Logged
                    || !SessionAuthorizationHelper.CanEditLoggedStage(sessionClient.Session, User, currentTrainerId)))
            {
                return Forbid();
            }

            var exercises = await _context.Exercises
                .OrderBy(e => e.ExerciseName)
                .Select(e => new { e.ID, Text = e.ExerciseName })
                .ToListAsync();

            var allSprings = await _context.Springs
                .OrderBy(s => s.SpringName)
                .Select(s => new { s.ApparatusID, s.SpringName })
                .ToListAsync();

            var vm = new ActionCreateVM
            {
                SessionID = sessionClient.SessionID,
                SessionClientID = sessionClientId,
                ExerciseList = new SelectList(exercises, "ID", "Text"),
                PropList = new MultiSelectList(
                    await _context.Props.OrderBy(p => p.PropName).ToListAsync(),
                    "ID",
                    "PropName")
            };

            ViewBag.ApparatusID = new SelectList(
                await _context.Apparatuses
                    .Where(a => a.Exercises.Any())
                    .OrderBy(a => a.ApparatusName)
                    .ToListAsync(),
                "ID",
                "ApparatusName");
            ViewBag.SpringsJson = System.Text.Json.JsonSerializer.Serialize(allSprings);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAction(ActionCreateVM vm)
        {
            if (!CanEditTeacherSection())
            {
                return Forbid();
            }

            var currentTrainerId = await GetCurrentTrainerIdAsync();
            var sessionClient = await _context.SessionClients
                .Include(sc => sc.Session)
                    .ThenInclude(s => s!.SessionClients)
                        .ThenInclude(participant => participant.SessionNotes)
                .FirstOrDefaultAsync(sc => sc.ID == vm.SessionClientID);

            if (sessionClient == null || sessionClient.SessionID != vm.SessionID)
            {
                return NotFound();
            }

            var canContinueOpenedAddAction = sessionClient.Session?.Status == SessionStatus.Opened
                && SessionAuthorizationHelper.CanStartLoggedStage(User, currentTrainerId)
                && sessionClient.Session.SessionDate.Date <= DateTime.Today
                && TempData.Peek(OpenedAddActionSessionClientTempDataKey) is string openedAddActionSessionClientIdValue
                && int.TryParse(openedAddActionSessionClientIdValue, out var openedAddActionSessionClientId)
                && openedAddActionSessionClientId == vm.SessionClientID;

            if (sessionClient.Session == null
                || (!canContinueOpenedAddAction
                    && (sessionClient.Session.Status != SessionStatus.Logged
                        || !SessionAuthorizationHelper.CanEditLoggedStage(sessionClient.Session, User, currentTrainerId))))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                vm.ExerciseList = new SelectList(
                    await _context.Exercises.OrderBy(e => e.ExerciseName)
                        .Select(e => new { e.ID, Text = e.ExerciseName }).ToListAsync(),
                    "ID",
                    "Text",
                    vm.ExerciseID);

                vm.PropList = new MultiSelectList(
                    await _context.Props.OrderBy(p => p.PropName).ToListAsync(),
                    "ID",
                    "PropName",
                    vm.SelectedPropIDs);

                ViewBag.ApparatusID = new SelectList(
                    await _context.Apparatuses.Where(a => a.Exercises.Any()).OrderBy(a => a.ApparatusName).ToListAsync(),
                    "ID",
                    "ApparatusName");
                ViewBag.SpringsJson = System.Text.Json.JsonSerializer.Serialize(
                    await _context.Springs.OrderBy(s => s.SpringName)
                        .Select(s => new { s.ApparatusID, s.SpringName }).ToListAsync());

                return View(vm);
            }

            var action = new ModelAction
            {
                SessionClientID = vm.SessionClientID,
                ExerciseID = vm.ExerciseID,
                ActionType = vm.ActionType,
                Springs = vm.Springs ?? string.Empty,
                Notes = vm.Notes ?? string.Empty
            };

            _context.Actions.Add(action);
            await _context.SaveChangesAsync();

            if (vm.SelectedPropIDs != null && vm.SelectedPropIDs.Count > 0)
            {
                foreach (var propId in vm.SelectedPropIDs.Distinct())
                {
                    _context.ExerciseProps.Add(new ExerciseProp
                    {
                        ActionID = action.ID,
                        PropID = propId
                    });
                }

                await _context.SaveChangesAsync();
            }

            if (canContinueOpenedAddAction)
            {
                TempData.Remove(OpenedAddActionSessionClientTempDataKey);
            }

            return RedirectToAction(nameof(Edit), new { id = vm.SessionID });
        }

        public async Task<IActionResult> EditAction(int id)
        {
            if (!CanEditTeacherSection())
            {
                return Forbid();
            }

            var action = await _context.Actions
                .Include(a => a.SessionClient)
                    .ThenInclude(sc => sc!.Session)
                        .ThenInclude(s => s!.SessionClients)
                            .ThenInclude(participant => participant.SessionNotes)
                .Include(a => a.Exercise)
                    .ThenInclude(e => e!.Apparatus)
                .Include(a => a.ExerciseProps)
                    .ThenInclude(ep => ep.Prop)
                .FirstOrDefaultAsync(a => a.ID == id);

            if (action == null)
            {
                return NotFound();
            }

            var session = action.SessionClient?.Session;
            var currentTrainerId = await GetCurrentTrainerIdAsync();
            if (session == null
                || session.Status != SessionStatus.Logged
                || !SessionAuthorizationHelper.CanEditLoggedStage(session, User, currentTrainerId))
            {
                return Forbid();
            }

            var vm = new ActionEditVM
            {
                ActionID = action.ID,
                SessionID = action.SessionClient?.SessionID ?? 0,
                SessionClientID = action.SessionClientID,
                ExerciseID = action.ExerciseID,
                ActionType = action.ActionType,
                Springs = action.Springs,
                Notes = action.Notes,
                SelectedPropIDs = action.ExerciseProps.Select(ep => ep.PropID).ToList(),
                ApparatusFilterID = action.Exercise?.ApparatusID
            };

            vm.ExerciseList = new SelectList(
                await _context.Exercises.OrderBy(e => e.ExerciseName)
                    .Select(e => new { e.ID, Text = e.ExerciseName }).ToListAsync(),
                "ID",
                "Text",
                vm.ExerciseID);

            vm.PropList = new MultiSelectList(
                await _context.Props.OrderBy(p => p.PropName).ToListAsync(),
                "ID",
                "PropName",
                vm.SelectedPropIDs);

            ViewBag.ApparatusID = new SelectList(
                await _context.Apparatuses.Where(a => a.Exercises.Any()).OrderBy(a => a.ApparatusName).ToListAsync(),
                "ID",
                "ApparatusName");
            ViewBag.SpringsJson = System.Text.Json.JsonSerializer.Serialize(
                await _context.Springs.OrderBy(s => s.SpringName)
                    .Select(s => new { s.ApparatusID, s.SpringName }).ToListAsync());

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAction(ActionEditVM vm)
        {
            if (!CanEditTeacherSection())
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                vm.ExerciseList = new SelectList(
                    await _context.Exercises.OrderBy(e => e.ExerciseName)
                        .Select(e => new { e.ID, Text = e.ExerciseName }).ToListAsync(),
                    "ID",
                    "Text",
                    vm.ExerciseID);

                vm.PropList = new MultiSelectList(
                    await _context.Props.OrderBy(p => p.PropName).ToListAsync(),
                    "ID",
                    "PropName",
                    vm.SelectedPropIDs);

                ViewBag.ApparatusID = new SelectList(
                    await _context.Apparatuses.Where(a => a.Exercises.Any()).OrderBy(a => a.ApparatusName).ToListAsync(),
                    "ID",
                    "ApparatusName");
                ViewBag.SpringsJson = System.Text.Json.JsonSerializer.Serialize(
                    await _context.Springs.OrderBy(s => s.SpringName)
                        .Select(s => new { s.ApparatusID, s.SpringName }).ToListAsync());

                return View(vm);
            }

            var action = await _context.Actions
                .Include(a => a.SessionClient)
                    .ThenInclude(sc => sc!.Session)
                        .ThenInclude(s => s!.SessionClients)
                            .ThenInclude(participant => participant.SessionNotes)
                .Include(a => a.ExerciseProps)
                .FirstOrDefaultAsync(a => a.ID == vm.ActionID);

            if (action == null)
            {
                return NotFound();
            }

            var session = action.SessionClient?.Session;
            var currentTrainerId = await GetCurrentTrainerIdAsync();
            if (session == null
                || session.Status != SessionStatus.Logged
                || !SessionAuthorizationHelper.CanEditLoggedStage(session, User, currentTrainerId))
            {
                return Forbid();
            }

            if (action.SessionClientID != vm.SessionClientID
                || action.SessionClient?.SessionID != vm.SessionID)
            {
                return BadRequest();
            }

            action.ExerciseID = vm.ExerciseID;
            action.ActionType = vm.ActionType;
            action.Springs = vm.Springs ?? string.Empty;
            action.Notes = vm.Notes ?? string.Empty;

            _context.ExerciseProps.RemoveRange(action.ExerciseProps);

            if (vm.SelectedPropIDs != null && vm.SelectedPropIDs.Count > 0)
            {
                foreach (var propId in vm.SelectedPropIDs.Distinct())
                {
                    _context.ExerciseProps.Add(new ExerciseProp
                    {
                        ActionID = action.ID,
                        PropID = propId
                    });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Edit), new { id = action.SessionClient?.SessionID ?? vm.SessionID });
        }

        public async Task<IActionResult> DeleteAction(int id)
        {
            if (!CanEditTeacherSection())
            {
                return Forbid();
            }

            var action = await _context.Actions
                .Include(a => a.SessionClient)
                    .ThenInclude(sc => sc!.Session)
                        .ThenInclude(s => s!.SessionClients)
                            .ThenInclude(participant => participant.SessionNotes)
                .FirstOrDefaultAsync(a => a.ID == id);

            if (action == null)
            {
                return NotFound();
            }

            var session = action.SessionClient?.Session;
            var currentTrainerId = await GetCurrentTrainerIdAsync();
            if (session == null
                || session.Status != SessionStatus.Logged
                || !SessionAuthorizationHelper.CanEditLoggedStage(session, User, currentTrainerId))
            {
                return Forbid();
            }

            ViewData["SessionID"] = action.SessionClient?.SessionID;
            return View(action);
        }

        [HttpPost, ActionName("DeleteAction")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteActionConfirmed(int id)
        {
            if (!CanEditTeacherSection())
            {
                return Forbid();
            }

            var action = await _context.Actions
                .Include(a => a.SessionClient)
                    .ThenInclude(sc => sc!.Session)
                        .ThenInclude(s => s!.SessionClients)
                            .ThenInclude(participant => participant.SessionNotes)
                .Include(a => a.ExerciseProps)
                .FirstOrDefaultAsync(a => a.ID == id);

            if (action == null)
            {
                return NotFound();
            }

            var session = action.SessionClient?.Session;
            var currentTrainerId = await GetCurrentTrainerIdAsync();
            if (session == null
                || session.Status != SessionStatus.Logged
                || !SessionAuthorizationHelper.CanEditLoggedStage(session, User, currentTrainerId))
            {
                return Forbid();
            }

            int sessionId = action.SessionClient?.SessionID ?? 0;

            if (action.ExerciseProps.Any())
            {
                _context.ExerciseProps.RemoveRange(action.ExerciseProps);
            }

            _context.Actions.Remove(action);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Edit), new { id = sessionId });
        }

        public IActionResult DownloadImportTemplate()
        {
            using var package = new ExcelPackage();

            // ── Sheet 1: Template ──
            var dataSheet = package.Workbook.Worksheets.Add("Import Template");
            WriteImportHeaders(dataSheet);
            WriteTemplateExampleRow(dataSheet);
            dataSheet.Cells[dataSheet.Dimension.Address].AutoFitColumns();

            // ── Sheet 2: Instructions ──
            var guide = package.Workbook.Worksheets.Add("Instructions");

            var titleFont = guide.Cells["A1"].Style.Font;
            guide.Cells["A1"].Value = "Session Import Guide";
            guide.Cells["A1"].Style.Font.Size = 20;
            guide.Cells["A1"].Style.Font.Bold = true;
            guide.Cells["A1"].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(31, 56, 100));
            guide.Cells["A1:C1"].Merge = true;

            var headerColor = System.Drawing.Color.FromArgb(46, 117, 182);
            var yellowColor = System.Drawing.Color.FromArgb(255, 242, 204);
            var lightGray = System.Drawing.Color.FromArgb(242, 242, 242);

            void SectionTitle(int row, string text)
            {
                guide.Cells[row, 1].Value = text;
                guide.Cells[row, 1].Style.Font.Bold = true;
                guide.Cells[row, 1].Style.Font.Size = 13;
                guide.Cells[row, 1].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(46, 117, 182));
                guide.Cells[row, 1, row, 3].Merge = true;
            }

            void TableHeader(int row, string col1, string col2, string col3)
            {
                guide.Cells[row, 1].Value = col1;
                guide.Cells[row, 2].Value = col2;
                guide.Cells[row, 3].Value = col3;
                for (var c = 1; c <= 3; c++)
                {
                    guide.Cells[row, c].Style.Font.Bold = true;
                    guide.Cells[row, c].Style.Font.Color.SetColor(System.Drawing.Color.White);
                    guide.Cells[row, c].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    guide.Cells[row, c].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(46, 117, 182));
                }
            }

            void DataRow(int row, string col1, string col2, string col3, bool highlight = false)
            {
                guide.Cells[row, 1].Value = col1;
                guide.Cells[row, 2].Value = col2;
                guide.Cells[row, 3].Value = col3;
                if (highlight)
                {
                    for (var c = 1; c <= 3; c++)
                    {
                        guide.Cells[row, c].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        guide.Cells[row, c].Style.Fill.BackgroundColor.SetColor(yellowColor);
                    }
                }
                else if (row % 2 == 0)
                {
                    for (var c = 1; c <= 3; c++)
                    {
                        guide.Cells[row, c].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        guide.Cells[row, c].Style.Fill.BackgroundColor.SetColor(lightGray);
                    }
                }
            }

            void BorderRange(int fromRow, int toRow)
            {
                var range = guide.Cells[fromRow, 1, toRow, 3];
                range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
            }

            int r = 3;

            // Getting Started
            SectionTitle(r++, "1. Getting Started");
            var steps = new[]
            {
                "Download this file (Session_Import_Template.xlsx) from the Sessions page.",
                "Enter your session data on the 'Import Template' sheet starting from Row 2.",
                "Row 1 contains the column headers — do not delete or rename them.",
                "Save the file and return to the Sessions page to upload it.",
                "A feedback message will confirm how many sessions were imported or skipped."
            };
            foreach (var step in steps)
            {
                guide.Cells[r, 1].Value = $"  {Array.IndexOf(steps, step) + 1}.  {step}";
                guide.Cells[r, 1, r, 3].Merge = true;
                r++;
            }
            r++;

            // Important Rules
            SectionTitle(r++, "2. Important Rules");
            var rules = new[]
            {
                "Clients and trainers are identified by email — the email must already exist in the system.",
                "Client names (PrimaryClientName / SecondaryClientName) must match the system record exactly.",
                "Dates must be in YYYY-MM-DD format (e.g. 2026-05-01).",
                "Boolean fields accept: TRUE, FALSE, YES, NO, Y, N, 1, or 0.",
                "Exercises are pipe-separated (e.g. Reformer Footwork|Hundred) and must exist in the system.",
                "For SemiPrivate sessions all Secondary columns are required.",
                "Duplicate rows (same date + trainer + primary client) are skipped automatically.",
            };
            foreach (var rule in rules)
            {
                guide.Cells[r, 1].Value = $"  •  {rule}";
                guide.Cells[r, 1, r, 3].Merge = true;
                r++;
            }
            r++;

            // Session Types
            SectionTitle(r++, "3. Session Types");
            TableHeader(r, "SessionType Value", "Description", "Secondary Fields Required?");
            BorderRange(r, r + 3);
            r++;
            DataRow(r++, "Private", "One client, one trainer", "No — leave Secondary columns blank");
            DataRow(r++, "SemiPrivate", "Two clients, one trainer", "Yes — all Secondary fields required", false);
            DataRow(r++, "Physio", "Physiotherapy session", "No — fill Physio columns instead");
            r++;

            // Column Reference
            SectionTitle(r++, "4. Column Reference  (yellow rows = required for all session types)");
            TableHeader(r, "Column", "Required?", "Accepted Values / Notes");
            int colRefStart = r;
            r++;
            var columns = new[]
            {
                ("SessionDate",                  "Required",          "Date in YYYY-MM-DD format"),
                ("SessionType",                  "Required",          "Private, SemiPrivate, or Physio"),
                ("TrainerEmail",                 "Required",          "Must match a trainer email in the system"),
                ("PrimaryClientName",            "Required",          "Full name (First Last) — must match system record"),
                ("PrimaryClientEmail",           "Required",          "Must match a client email in the system"),
                ("PrimarySessionsPerWeek",       "Required",          "Whole number (e.g. 2)"),
                ("PrimaryGoals",                 "Required",          "Session goals text"),
                ("PrimaryGeneralComments",       "Optional",          "General comments"),
                ("PrimarySubjectiveReports",     "Required",          "Subjective report text"),
                ("PrimaryObjectiveFindings",     "Required",          "Objective findings text"),
                ("PrimaryPlan",                  "Required",          "Plan text"),
                ("PrimaryNextAppointmentBooked", "Optional",          "TRUE or FALSE"),
                ("PrimaryCommunicatedProgress",  "Optional",          "TRUE or FALSE"),
                ("PrimaryReadyToProgress",       "Optional",          "TRUE or FALSE"),
                ("PrimaryCourseCorrectionNeeded","Optional",          "TRUE or FALSE"),
                ("PrimaryTeamConsult",           "Optional",          "TRUE or FALSE"),
                ("PrimaryReferredExternally",    "Optional",          "TRUE or FALSE"),
                ("PrimaryReferredTo",            "Optional",          "Name of referral if referred externally"),
                ("PrimaryIsPaid",                "Optional",          "TRUE or FALSE"),
                ("PrimaryAdminNotes",            "Optional",          "Admin notes text"),
                ("PrimaryAdminInitials",         "Optional",          "Initials (e.g. AG)"),
                ("SecondaryClientName",          "SemiPrivate only",  "Full name — must match system record"),
                ("SecondaryClientEmail",         "SemiPrivate only",  "Must match a client email in the system"),
                ("SecondarySessionsPerWeek",     "SemiPrivate only",  "Whole number"),
                ("SecondaryGoals",               "SemiPrivate only",  "Session goals text"),
                ("SecondaryGeneralComments",     "Optional",          "General comments"),
                ("SecondarySubjectiveReports",   "SemiPrivate only",  "Subjective report text"),
                ("SecondaryObjectiveFindings",   "SemiPrivate only",  "Objective findings text"),
                ("SecondaryPlan",                "SemiPrivate only",  "Plan text"),
                ("SecondaryNextAppointmentBooked","Optional",         "TRUE or FALSE"),
                ("SecondaryCommunicatedProgress","Optional",          "TRUE or FALSE"),
                ("SecondaryReadyToProgress",     "Optional",          "TRUE or FALSE"),
                ("SecondaryCourseCorrectionNeeded","Optional",        "TRUE or FALSE"),
                ("SecondaryTeamConsult",         "Optional",          "TRUE or FALSE"),
                ("SecondaryReferredExternally",  "Optional",          "TRUE or FALSE"),
                ("SecondaryReferredTo",          "Optional",          "Name of referral if referred externally"),
                ("SecondaryIsPaid",              "Optional",          "TRUE or FALSE"),
                ("SecondaryAdminNotes",          "Optional",          "Admin notes text"),
                ("SecondaryAdminInitials",       "Optional",          "Initials"),
                ("AccessoriesHeadPad",           "Optional",          "Down, Middle, Full, OneExtraCushion, TwoExtraCushion, PosturePillow"),
                ("AccessoriesStrapsOrHandles",   "Optional",          "Straps or Handles"),
                ("AccessoriesGearBar",           "Optional",          "Number 0–3"),
                ("AccessoriesStopperSettings",   "Optional",          "Number 0–6"),
                ("AccessoriesRubberPads",        "Optional",          "TRUE or FALSE"),
                ("AccessoriesHeadRest",          "Optional",          "TRUE or FALSE"),
                ("AccessoriesTowel",             "Optional",          "TRUE or FALSE"),
                ("AccessoriesPosturePillow",     "Optional",          "TRUE or FALSE"),
                ("PhysioAssessment",             "Physio only",       "Assessment text"),
                ("InsuranceCompany",             "Physio only",       "Insurance company name"),
                ("CoverageAmountPerYear",        "Physio only",       "Decimal number (e.g. 1000.00)"),
                ("AmountUsed",                   "Physio only",       "Decimal number"),
                ("CoverageResetsDate",           "Physio only",       "Date in YYYY-MM-DD format"),
                ("PhysiotherapistName",          "Physio only",       "Name of physiotherapist"),
                ("CoverageShared",               "Physio only",       "TRUE or FALSE"),
                ("CommunicatedWithPhysio",       "Physio only",       "TRUE or FALSE"),
                ("Exercises",                    "Optional",          "Pipe-separated exercise names e.g. Reformer Footwork|Hundred"),
            };

            foreach (var (col, req, desc) in columns)
            {
                DataRow(r, col, req, desc, req == "Required");
                r++;
            }
            BorderRange(colRefStart, r - 1);
            r++;

            // Common Errors
            SectionTitle(r++, "5. Common Errors");
            TableHeader(r, "Error Message", "Cause", "How to Fix");
            int errStart = r;
            r++;
            var errors = new[]
            {
                ("trainer '...' was not found",          "Email not in system",             "Check the trainer email exists in the system"),
                ("client '...' was not found",           "Email not in system",             "Check the client email exists in the system"),
                ("ClientName does not match",            "Name mismatch",                   "Ensure name matches the system record exactly"),
                ("exercise '...' was not found",         "Exercise not in system",          "Check the exercise name exists in the system"),
                ("SessionDate must be a valid date",     "Wrong date format",               "Use YYYY-MM-DD format (e.g. 2026-05-01)"),
                ("semi-private requires SecondaryClientEmail", "Missing secondary email",   "Add SecondaryClientEmail for SemiPrivate rows"),
                ("All rows were duplicates",             "Sessions already exist",          "These sessions are already in the system"),
            };
            foreach (var (msg, cause, fix) in errors)
            {
                DataRow(r, msg, cause, fix);
                r++;
            }
            BorderRange(errStart, r - 1);

            // Formatting
            guide.Column(1).Width = 40;
            guide.Column(2).Width = 20;
            guide.Column(3).Width = 55;
            guide.Cells[1, 1, r, 3].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
            guide.Cells[1, 1, r, 3].Style.WrapText = true;

            var bytes = package.GetAsByteArray();
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Session_Import_Template.xlsx");
        }

        //public async Task<IActionResult> ExportToExcel(int id)
        //{
        //    var session = await SessionDetailsQuery()
        //        .AsNoTracking()
        //        .FirstOrDefaultAsync(s => s.ID == id);

        //    if (session == null) return NotFound();

        //    using var package = new ExcelPackage();
        //    var ws = package.Workbook.Worksheets.Add("Session");
        //    WriteVerticalSessionReport(ws, session);

        //    var bytes = package.GetAsByteArray();
        //    return File(bytes,
        //        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        //        $"session-{session.ID}-{session.SessionDate:yyyyMMdd}.xlsx");
        //}

        private static void WriteVerticalSessionReport(ExcelWorksheet ws, Session session)
        {
            var lightGray = System.Drawing.Color.LightGray;
            var lightBlue = System.Drawing.Color.LightBlue;

            void SectionHeader(int row, string title, int colSpan = 2)
            {
                ws.Cells[row, 1].Value = title;
                using var r = ws.Cells[row, 1, row, colSpan];
                r.Merge = true;
                r.Style.Font.Bold = true;
                r.Style.Font.Size = 14;
                r.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                r.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                r.Style.Fill.BackgroundColor.SetColor(lightGray);
            }

            void FieldHeaderRow(int row, int cols)
            {
                using var r = ws.Cells[row, 1, row, cols];
                r.Style.Font.Bold = true;
                r.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                r.Style.Fill.BackgroundColor.SetColor(lightBlue);
            }

            var primary = session.PrimarySessionClient;
            var secondary = session.SecondarySessionClient;

            // ── Title ──
            var clientName = primary?.Client?.FullName ?? "-";
            ws.Cells[1, 1].Value = $"Session Report – {clientName} – {session.SessionDate:yyyy-MM-dd}";
            using (var title = ws.Cells[1, 1, 1, 5])
            {
                title.Merge = true;
                title.Style.Font.Bold = true;
                title.Style.Font.Size = 18;
                title.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }

            // ── Timestamp ──
            var esZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, esZone);
            ws.Cells[2, 5].Value = $"Created: {localNow:t} on {localNow:d}";
            ws.Cells[2, 5].Style.Font.Bold = true;
            ws.Cells[2, 5].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;

            // ── General Info ──
            SectionHeader(3, "General Info");
            var generalInfo = new[]
            {
                new { Field = "Session Date",   Value = session.SessionDate.ToString("yyyy-MM-dd") },
                new { Field = "Session Type",   Value = session.SessionType.ToString() },
                new { Field = "Trainer",        Value = session.Trainer?.TrainerName ?? "-" },
                new { Field = "Client",         Value = clientName },
                new { Field = "Sessions/Week",  Value = primary?.SessionsPerWeekRecommended?.ToString() ?? "-" },
            };
            ws.Cells[4, 1].LoadFromCollection(generalInfo, true);
            FieldHeaderRow(4, 2);

            // ── Session Notes ──
            int notesRow = 4 + generalInfo.Length + 2;
            SectionHeader(notesRow, "Session Notes");
            var notes = primary?.SessionNotes;
            var notesData = new[]
            {
                new { Field = "Goals",               Value = notes?.Goals ?? "-" },
                new { Field = "General Comments",    Value = notes?.GeneralComments ?? "-" },
                new { Field = "Subjective Reports",  Value = notes?.SubjectiveReports ?? "-" },
                new { Field = "Objective Findings",  Value = notes?.ObjectiveFindings ?? "-" },
                new { Field = "Plan",                Value = notes?.Plan ?? "-" },
            };
            ws.Cells[notesRow + 1, 1].LoadFromCollection(notesData, true);
            FieldHeaderRow(notesRow + 1, 2);

            // ── Client 2 Notes (SemiPrivate) ──
            int actionsRow;
            if (session.SessionType == SessionType.SemiPrivate && secondary != null)
            {
                int client2NotesRow = notesRow + notesData.Length + 3;
                SectionHeader(client2NotesRow, $"Session Notes – {secondary.Client?.FullName ?? "Client 2"}");
                var notes2 = secondary.SessionNotes;
                var notesData2 = new[]
                {
                    new { Field = "Goals",               Value = notes2?.Goals ?? "-" },
                    new { Field = "General Comments",    Value = notes2?.GeneralComments ?? "-" },
                    new { Field = "Subjective Reports",  Value = notes2?.SubjectiveReports ?? "-" },
                    new { Field = "Objective Findings",  Value = notes2?.ObjectiveFindings ?? "-" },
                    new { Field = "Plan",                Value = notes2?.Plan ?? "-" },
                };
                ws.Cells[client2NotesRow + 1, 1].LoadFromCollection(notesData2, true);
                FieldHeaderRow(client2NotesRow + 1, 2);
                actionsRow = client2NotesRow + notesData2.Length + 3;
            }
            else
            {
                actionsRow = notesRow + notesData.Length + 3;
            }

            // ── Actions ──
            SectionHeader(actionsRow, "Actions", 5);
            int actionsEndRow = actionsRow + 2;
            if (session.Actions.Any())
            {
                var actionsData = session.Actions.OrderBy(a => a.ID).Select(a => new
                {
                    Exercise = a.Exercise?.ExerciseName ?? "-",
                    Apparatus = a.Exercise?.Apparatus?.ApparatusName ?? "-",
                    Springs = a.Springs ?? "-",
                    Props = a.ExerciseProps.Any()
                        ? string.Join(", ", a.ExerciseProps.Select(ep => ep.Prop?.PropName).Where(n => n != null))
                        : "-",
                    Notes = a.Notes ?? "-"
                }).ToList();

                ws.Cells[actionsRow + 1, 1].LoadFromCollection(actionsData, true);
                FieldHeaderRow(actionsRow + 1, 5);
                actionsEndRow = actionsRow + 1 + actionsData.Count;
                ws.Cells[actionsRow + 2, 1, actionsEndRow, 1].Style.Font.Bold = true;
            }

            // ── Accessories ──
            int accessoriesRow = actionsEndRow + 2;
            SectionHeader(accessoriesRow, "Accessories");
            if (primary?.Accessories != null)
            {
                var acc = primary.Accessories;
                var accData = new[]
                {
                    new { Field = "Head Pad",         Value = acc.HeadPad.ToString() },
                    new { Field = "Straps/Handles",   Value = acc.StrapsOrHandles.ToString() },
                    new { Field = "Gear Bar",         Value = acc.GearBar.ToString() },
                    new { Field = "Stopper Settings", Value = acc.StopperSettings.ToString() },
                    new { Field = "Rubber Pads",      Value = acc.RubberPads ? "Yes" : "No" },
                    new { Field = "Head Rest",        Value = acc.HeadRest ? "Yes" : "No" },
                    new { Field = "Towel",            Value = acc.Towel ? "Yes" : "No" },
                    new { Field = "Posture Pillow",   Value = acc.PosturePillow ? "Yes" : "No" },
                };
                ws.Cells[accessoriesRow + 1, 1].LoadFromCollection(accData, true);
                FieldHeaderRow(accessoriesRow + 1, 2);
            }

            // ── Next Steps ──
            int nextStepsRow = accessoriesRow + 12;
            SectionHeader(nextStepsRow, "Next Steps");
            var ns = primary?.NextSteps;
            if (ns != null)
            {
                var nsData = new[]
                {
                    new { Field = "Next Appointment Booked",  Value = ns.NextAppointmentBooked ? "Yes" : "No" },
                    new { Field = "Communicated Progress",    Value = ns.CommunicatedProgress ? "Yes" : "No" },
                    new { Field = "Ready to Progress",        Value = ns.ReadyToProgress ? "Yes" : "No" },
                    new { Field = "Course Correction Needed", Value = ns.CourseCorrectionNeeded ? "Yes" : "No" },
                    new { Field = "Team Consult",             Value = ns.TeamConsult ? "Yes" : "No" },
                    new { Field = "Referred Externally",      Value = ns.ReferredExternally ? "Yes" : "No" },
                    new { Field = "Referred To",              Value = ns.ReferredTo ?? "-" },
                };
                ws.Cells[nextStepsRow + 1, 1].LoadFromCollection(nsData, true);
                FieldHeaderRow(nextStepsRow + 1, 2);
            }

            // ── Admin Completion ──
            int adminRow = nextStepsRow + 11;
            SectionHeader(adminRow, "Admin Completion");
            var admin = primary?.AdminComplete;
            if (admin != null)
            {
                var adminData = new[]
                {
                    new { Field = "Paid",        Value = (admin.IsPaid == true) ? "Yes" : "No" },
                    new { Field = "Admin Notes", Value = admin.AdminNotes ?? "-" },
                    new { Field = "Initials",    Value = admin.AdminInitials ?? "-" },
                };
                ws.Cells[adminRow + 1, 1].LoadFromCollection(adminData, true);
                FieldHeaderRow(adminRow + 1, 2);
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();
        }

        // GET: Session/ExportRangeToExcel
        //public async Task<IActionResult> ExportRangeToExcel(DateTime? startDate, DateTime? endDate)
        //{
        //    if (startDate == null || endDate == null)
        //    {
        //        TempData["Feedback"] = "Please provide both a start and end date for the export.";
        //        return RedirectToAction(nameof(Index));
        //    }

        //    if (endDate < startDate)
        //    {
        //        TempData["Feedback"] = "End date must be on or after the start date.";
        //        return RedirectToAction(nameof(Index));
        //    }

        //    var sessions = await SessionDetailsQuery()
        //        .AsNoTracking()
        //        .Where(s => s.SessionDate >= startDate.Value.Date && s.SessionDate <= endDate.Value.Date)
        //        .OrderBy(s => s.SessionDate)
        //        .ThenBy(s => s.SessionClients
        //            .Where(sc => sc.ParticipantOrder == 1)
        //            .Select(sc => sc.Client!.LastName)
        //            .FirstOrDefault())
        //        .ToListAsync();

        //    if (!sessions.Any())
        //    {
        //        TempData["Feedback"] = $"No sessions found between {startDate:yyyy-MM-dd} and {endDate:yyyy-MM-dd}.";
        //        return RedirectToAction(nameof(Index));
        //    }

        //    using var package = new ExcelPackage();

        //    foreach (var session in sessions)
        //    {
        //        var primary = session.PrimarySessionClient;
        //        var clientLastName = primary?.Client?.LastName ?? "Unknown";
        //        var sheetName = $"{session.SessionDate:yyyyMMdd}_{clientLastName}";
        //        if (sheetName.Length > 31)
        //            sheetName = sheetName.Substring(0, 31);

        //        var baseName = sheetName;
        //        int suffix = 1;
        //        while (package.Workbook.Worksheets.Any(ws => ws.Name == sheetName))
        //        {
        //            sheetName = $"{baseName}_{suffix++}";
        //            if (sheetName.Length > 31)
        //                sheetName = sheetName.Substring(0, 28) + $"_{suffix}";
        //        }

        //        var worksheet = package.Workbook.Worksheets.Add(sheetName);
        //        WriteVerticalSessionReport(worksheet, session);
        //    }

        //    var bytes = package.GetAsByteArray();
        //    var filename = $"Sessions_{startDate:yyyyMMdd}_to_{endDate:yyyyMMdd}.xlsx";
        //    return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
        //}

        // GET: Session/ExportRangeToPdf
        public async Task<IActionResult> ExportRangeToPdf(DateTime? startDate, DateTime? endDate)
        {
            if (startDate == null || endDate == null)
            {
                TempData["ExportFeedback"] = "Please provide both a start and end date for the export.";
                return RedirectToAction(nameof(Index));
            }

            if (endDate < startDate)
            {
                TempData["ExportFeedback"] = "End date must be on or after the start date.";
                return RedirectToAction(nameof(Index));
            }

            var sessions = await SessionDetailsQuery()
                .AsNoTracking()
                .Where(s => s.SessionDate >= startDate.Value.Date && s.SessionDate <= endDate.Value.Date)
                .OrderBy(s => s.SessionDate)
                .ThenBy(s => s.SessionClients
                    .Where(sc => sc.ParticipantOrder == 1)
                    .Select(sc => sc.Client!.LastName)
                    .FirstOrDefault())
                .ToListAsync();

            if (!sessions.Any())
            {
                TempData["ExportFeedback"] = $"No sessions found between {startDate:yyyy-MM-dd} and {endDate:yyyy-MM-dd}.";
                return RedirectToAction(nameof(Index));
            }

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var pdf = QuestPDF.Fluent.Document.Create(container =>
            {
                foreach (var session in sessions)
                {
                    var primary = session.PrimarySessionClient;
                    var secondary = session.SecondarySessionClient;
                    var clientName = primary?.Client?.FullName ?? "-";
                    var titleName = session.SessionType == SessionType.SemiPrivate && secondary != null
                                    ? $"{clientName} / {secondary.Client?.FullName ?? "-"}"
                                    : clientName;

                    container.Page(page =>
                    {
                        page.Size(QuestPDF.Helpers.PageSizes.A4);
                        page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header().Column(col =>
                        {
                            col.Item().Text($"Session Report – {clientName} – {session.SessionDate:yyyy-MM-dd}")
                                .Bold().FontSize(16);
                            col.Item().Text($"Trainer: {session.Trainer?.TrainerName ?? "-"}  |  Type: {session.SessionType}  |  Sessions/Week: {primary?.SessionsPerWeekRecommended?.ToString() ?? "-"}")
                                .FontSize(9).FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                            col.Item().PaddingTop(4).LineHorizontal(1).LineColor(QuestPDF.Helpers.Colors.Grey.Lighten1);
                        });

                        page.Content().PaddingTop(12).Column(col =>
                        {
                            // ── Session Notes ──
                            var notes = primary?.SessionNotes;
                            col.Item().Text($"Session Notes – {primary?.Client?.FullName ?? "Client 1"}").Bold().FontSize(12);
                            col.Item().PaddingBottom(6).Table(table =>
                            {
                                table.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(2); });
                                void Row(string label, string? value)
                                {
                                    table.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text(label).Bold();
                                    table.Cell().Padding(4).Text(value ?? "-");
                                }
                                Row("Goals", notes?.Goals);
                                Row("General Comments", notes?.GeneralComments);
                                Row("Subjective Reports", notes?.SubjectiveReports);
                                Row("Objective Findings", notes?.ObjectiveFindings);
                                Row("Plan", notes?.Plan);
                            });

                            // ── Client 2 Notes ──
                            if (session.SessionType == SessionType.SemiPrivate && secondary != null)
                            {
                                var notes2 = secondary.SessionNotes;
                                col.Item().PaddingTop(8).Text($"Session Notes – {secondary.Client?.FullName ?? "Client 2"}").Bold().FontSize(12);
                                col.Item().PaddingBottom(6).Table(table =>
                                {
                                    table.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(2); });
                                    void Row(string label, string? value)
                                    {
                                        table.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text(label).Bold();
                                        table.Cell().Padding(4).Text(value ?? "-");
                                    }
                                    Row("Goals", notes2?.Goals);
                                    Row("General Comments", notes2?.GeneralComments);
                                    Row("Subjective Reports", notes2?.SubjectiveReports);
                                    Row("Objective Findings", notes2?.ObjectiveFindings);
                                    Row("Plan", notes2?.Plan);
                                });
                            }

                            // ── Actions ──
                            if (session.Actions.Any())
                            {
                                col.Item().PaddingTop(8).Text("Actions").Bold().FontSize(12);
                                col.Item().PaddingBottom(6).Table(table =>
                                {
                                    table.ColumnsDefinition(c =>
                                    {
                                        c.RelativeColumn(2); c.RelativeColumn(2);
                                        c.RelativeColumn(1); c.RelativeColumn(2); c.RelativeColumn(2);
                                    });
                                    table.Header(h =>
                                    {
                                        foreach (var header in new[] { "Exercise", "Apparatus", "Springs", "Props", "Notes" })
                                            h.Cell().Background(QuestPDF.Helpers.Colors.Blue.Lighten4).Padding(4).Text(header).Bold();
                                    });
                                    foreach (var action in session.Actions.OrderBy(a => a.ID))
                                    {
                                        table.Cell().Padding(4).Text(action.Exercise?.ExerciseName ?? "-");
                                        table.Cell().Padding(4).Text(action.Exercise?.Apparatus?.ApparatusName ?? "-");
                                        table.Cell().Padding(4).Text(action.Springs ?? "-");
                                        table.Cell().Padding(4).Text(action.ExerciseProps.Any()
                                            ? string.Join(", ", action.ExerciseProps.Select(ep => ep.Prop?.PropName).Where(n => n != null))
                                            : "-");
                                        table.Cell().Padding(4).Text(action.Notes ?? "-");
                                    }
                                });
                            }
                           
                            // ── Accessories ──
                            var acc = primary?.Accessories;
                            if (acc != null)
                            {
                                col.Item().PaddingTop(8).Text("Accessories").Bold().FontSize(12);
                                col.Item().PaddingBottom(6).Table(table =>
                                {
                                    table.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(1); c.RelativeColumn(1); c.RelativeColumn(1); });

                                    void Cell(string label, string value)
                                    {
                                        table.Cell().Border(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(4).Column(c =>
                                        {
                                            c.Item().Text(label).FontSize(8).FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                                            c.Item().Text(value).Bold();
                                        });
                                    }

                                    Cell("Head Pad", acc.HeadPad.ToString());
                                    Cell("Straps/Handles", acc.StrapsOrHandles.ToString());
                                    Cell("Gear Bar", acc.GearBar.ToString());
                                    Cell("Stopper Settings", acc.StopperSettings.ToString());
                                    Cell("Rubber Pads", acc.RubberPads ? "Yes" : "No");
                                    Cell("Head Rest", acc.HeadRest ? "Yes" : "No");
                                    Cell("Towel", acc.Towel ? "Yes" : "No");
                                    Cell("Posture Pillow", acc.PosturePillow ? "Yes" : "No");
                                });
                            }

                            // ── Next Steps ──
                            var ns = primary?.NextSteps;
                            if (ns != null)
                            {
                                col.Item().PaddingTop(8).Text("Next Steps").Bold().FontSize(12);
                                col.Item().PaddingBottom(6).Table(table =>
                                {
                                    table.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(1); c.RelativeColumn(1); c.RelativeColumn(1); });
                                    void Cell(string label, string value)
                                    {
                                        table.Cell().Border(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(4).Column(c =>
                                        {
                                            c.Item().Text(label).FontSize(8).FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                                            c.Item().Text(value).Bold();
                                        });
                                    }
                                    Cell("Next Appt Booked", ns.NextAppointmentBooked ? "Yes" : "No");
                                    Cell("Communicated Progress", ns.CommunicatedProgress ? "Yes" : "No");
                                    Cell("Ready to Progress", ns.ReadyToProgress ? "Yes" : "No");
                                    Cell("Course Correction", ns.CourseCorrectionNeeded ? "Yes" : "No");
                                    Cell("Team Consult", ns.TeamConsult ? "Yes" : "No");
                                    Cell("Referred Externally", ns.ReferredExternally ? "Yes" : "No");
                                    Cell("Referred To", ns.ReferredTo ?? "-");
                                });
                            }

                            // ── Admin ──
                            var admin = primary?.AdminComplete;
                            if (admin != null)
                            {
                                col.Item().PaddingTop(8).Text("Admin Completion").Bold().FontSize(12);
                                col.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(2); });
                                    void Row(string label, string? value)
                                    {
                                        table.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text(label).Bold();
                                        table.Cell().Padding(4).Text(value ?? "-");
                                    }
                                    Row("Paid", (admin.IsPaid == true) ? "Yes" : "No");
                                    Row("Admin Notes", admin.AdminNotes);
                                    Row("Initials", admin.AdminInitials);
                                });
                            }
                        });

                        page.Footer().AlignRight().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                    });
                }
            });

            var bytes = pdf.GeneratePdf();
            var filename = $"Sessions_{startDate:yyyyMMdd}_to_{endDate:yyyyMMdd}.pdf";
            return File(bytes, "application/pdf", filename);
        }

        public async Task<IActionResult> ExportToPdf(int id)
        {
            var session = await SessionDetailsQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ID == id);

            if (session == null) return NotFound();

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var primary = session.PrimarySessionClient;
            var secondary = session.SecondarySessionClient;
            var clientName = primary?.Client?.FullName ?? "-";
            var titleName = session.SessionType == SessionType.SemiPrivate && secondary != null
                ? $"{clientName} / {secondary.Client?.FullName ?? "-"}"
                : clientName;

            var pdf = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(QuestPDF.Helpers.PageSizes.A4);
                    page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text($"Session Report – {titleName} – {session.SessionDate:yyyy-MM-dd}")
                            .Bold().FontSize(16);
                        col.Item().Text($"Trainer: {session.Trainer?.TrainerName ?? "-"}  |  Type: {session.SessionType}  |  Sessions/Week: {primary?.SessionsPerWeekRecommended?.ToString() ?? "-"}")
                            .FontSize(9).FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                        col.Item().PaddingTop(4).LineHorizontal(1).LineColor(QuestPDF.Helpers.Colors.Grey.Lighten1);
                    });

                    page.Content().PaddingTop(12).Column(col =>
                    {
                        // ── Session Notes ──
                        var notes = primary?.SessionNotes;
                        col.Item().Text($"Session Notes – {primary?.Client?.FullName ?? "Client 1"}").Bold().FontSize(12);
                        col.Item().PaddingBottom(6).Table(table =>
                        {
                            table.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(2); });
                            void Row(string label, string? value)
                            {
                                table.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text(label).Bold();
                                table.Cell().Padding(4).Text(value ?? "-");
                            }
                            Row("Goals", notes?.Goals);
                            Row("General Comments", notes?.GeneralComments);
                            Row("Subjective Reports", notes?.SubjectiveReports);
                            Row("Objective Findings", notes?.ObjectiveFindings);
                            Row("Plan", notes?.Plan);
                        });

                        // ── Client 2 Notes ──
                        if (session.SessionType == SessionType.SemiPrivate && secondary != null)
                        {
                            var notes2 = secondary.SessionNotes;
                            col.Item().PaddingTop(8).Text($"Session Notes – {secondary.Client?.FullName ?? "Client 2"}").Bold().FontSize(12);
                            col.Item().PaddingBottom(6).Table(table =>
                            {
                                table.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(2); });
                                void Row(string label, string? value)
                                {
                                    table.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text(label).Bold();
                                    table.Cell().Padding(4).Text(value ?? "-");
                                }
                                Row("Goals", notes2?.Goals);
                                Row("General Comments", notes2?.GeneralComments);
                                Row("Subjective Reports", notes2?.SubjectiveReports);
                                Row("Objective Findings", notes2?.ObjectiveFindings);
                                Row("Plan", notes2?.Plan);
                            });
                        }

                        // ── Actions ──
                        if (session.Actions.Any())
                        {
                            col.Item().PaddingTop(8).Text("Actions").Bold().FontSize(12);
                            col.Item().PaddingBottom(6).Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(2); c.RelativeColumn(2);
                                    c.RelativeColumn(1); c.RelativeColumn(2); c.RelativeColumn(2);
                                });
                                table.Header(h =>
                                {
                                    foreach (var header in new[] { "Exercise", "Apparatus", "Springs", "Props", "Notes" })
                                        h.Cell().Background(QuestPDF.Helpers.Colors.Blue.Lighten4).Padding(4).Text(header).Bold();
                                });
                                foreach (var action in session.Actions.OrderBy(a => a.ID))
                                {
                                    table.Cell().Padding(4).Text(action.Exercise?.ExerciseName ?? "-");
                                    table.Cell().Padding(4).Text(action.Exercise?.Apparatus?.ApparatusName ?? "-");
                                    table.Cell().Padding(4).Text(action.Springs ?? "-");
                                    table.Cell().Padding(4).Text(action.ExerciseProps.Any()
                                        ? string.Join(", ", action.ExerciseProps.Select(ep => ep.Prop?.PropName).Where(n => n != null))
                                        : "-");
                                    table.Cell().Padding(4).Text(action.Notes ?? "-");
                                }
                            });
                        }

                        // ── Accessories ──
                        var acc = primary?.Accessories;
                        if (acc != null)
                        {
                            col.Item().PaddingTop(8).Text("Accessories").Bold().FontSize(12);
                            col.Item().PaddingBottom(6).Table(table =>
                            {
                                table.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(1); c.RelativeColumn(1); c.RelativeColumn(1); });
                                void Cell(string label, string value)
                                {
                                    table.Cell().Border(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(4).Column(c =>
                                    {
                                        c.Item().Text(label).FontSize(8).FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                                        c.Item().Text(value).Bold();
                                    });
                                }
                                Cell("Head Pad", acc.HeadPad.ToString());
                                Cell("Straps/Handles", acc.StrapsOrHandles.ToString());
                                Cell("Gear Bar", acc.GearBar.ToString());
                                Cell("Stopper Settings", acc.StopperSettings.ToString());
                                Cell("Rubber Pads", acc.RubberPads ? "Yes" : "No");
                                Cell("Head Rest", acc.HeadRest ? "Yes" : "No");
                                Cell("Towel", acc.Towel ? "Yes" : "No");
                                Cell("Posture Pillow", acc.PosturePillow ? "Yes" : "No");
                            });
                        }

                        // ── Next Steps ──
                        var ns = primary?.NextSteps;
                        if (ns != null)
                        {
                            col.Item().PaddingTop(8).Text("Next Steps").Bold().FontSize(12);
                            col.Item().PaddingBottom(6).Table(table =>
                            {
                                table.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(1); c.RelativeColumn(1); c.RelativeColumn(1); });
                                void Cell(string label, string value)
                                {
                                    table.Cell().Border(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(4).Column(c =>
                                    {
                                        c.Item().Text(label).FontSize(8).FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                                        c.Item().Text(value).Bold();
                                    });
                                }
                                Cell("Next Appt Booked", ns.NextAppointmentBooked ? "Yes" : "No");
                                Cell("Communicated Progress", ns.CommunicatedProgress ? "Yes" : "No");
                                Cell("Ready to Progress", ns.ReadyToProgress ? "Yes" : "No");
                                Cell("Course Correction", ns.CourseCorrectionNeeded ? "Yes" : "No");
                                Cell("Team Consult", ns.TeamConsult ? "Yes" : "No");
                                Cell("Referred Externally", ns.ReferredExternally ? "Yes" : "No");
                                Cell("Referred To", ns.ReferredTo ?? "-");
                            });
                        }

                        // ── Admin ──
                        var admin = primary?.AdminComplete;
                        if (admin != null)
                        {
                            col.Item().PaddingTop(8).Text("Admin Completion").Bold().FontSize(12);
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(2); });
                                void Row(string label, string? value)
                                {
                                    table.Cell().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text(label).Bold();
                                    table.Cell().Padding(4).Text(value ?? "-");
                                }
                                Row("Paid", (admin.IsPaid == true) ? "Yes" : "No");
                                Row("Admin Notes", admin.AdminNotes);
                                Row("Initials", admin.AdminInitials);
                            });
                        }
                    });

                    page.Footer().AlignRight().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            });

            var bytes = pdf.GeneratePdf();
            return File(bytes, "application/pdf",
                $"session-{session.ID}-{session.SessionDate:yyyyMMdd}.pdf");
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.OwnerOrAdministration)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportExcel(IFormFile theExcel)
        {
            if (theExcel == null || theExcel.Length == 0)
            {
                TempData["Feedback"] = "Select an Excel file to upload.";
                return RedirectToAction(nameof(Index));
            }

            var trainerIds = await _context.Trainers
                .Where(t => !string.IsNullOrWhiteSpace(t.Email))
                .ToDictionaryAsync(t => t.Email!.Trim().ToUpperInvariant(), t => t.ID);

            var clientIds = await _context.Clients
                .Where(c => !string.IsNullOrWhiteSpace(c.Email))
                .ToDictionaryAsync(c => c.Email!.Trim().ToUpperInvariant(), c => c.ID);

            var exerciseIds = await _context.Exercises
                .ToDictionaryAsync(e => e.ExerciseName.Trim().ToUpperInvariant(), e => e.ID);

            try
            {
                using var stream = new MemoryStream();
                await theExcel.CopyToAsync(stream);
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                {
                    TempData["Feedback"] = "The uploaded workbook did not contain any worksheets.";
                    return RedirectToAction(nameof(Index));
                }

                var headerMap = ReadHeaderMap(worksheet);
                ValidateImportHeaders(headerMap);

                var sessions = new List<Session>();
                var lastRow = worksheet.Dimension?.End.Row ?? 0;

                for (var row = 2; row <= lastRow; row++)
                {
                    if (string.IsNullOrWhiteSpace(GetCellText(worksheet, row, headerMap, "SessionDate")))
                    {
                        continue;
                    }

                    sessions.Add(BuildImportedSession(worksheet, row, headerMap, trainerIds, clientIds, exerciseIds));
                }

                if (sessions.Count == 0)
                {
                    TempData["Feedback"] = "No session rows were found in the uploaded workbook.";
                    return RedirectToAction(nameof(Index));
                }

                // ── Duplicate check ──
                // Build a set of (SessionDate, TrainerID, PrimaryClientID) already in the database
                var incomingDates = sessions.Select(s => s.SessionDate.Date).Distinct().ToList();

                var existingKeys = await _context.Sessions
                    .Where(s => incomingDates.Contains(s.SessionDate))
                    .Include(s => s.SessionClients)
                    .Select(s => new
                    {
                        s.SessionDate,
                        s.TrainerID,
                        s.SessionType,
                        PrimaryClientID = s.SessionClients
                            .Where(sc => sc.ParticipantOrder == 1)
                            .Select(sc => (int?)sc.ClientID)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                var existingKeySet = existingKeys
                    .Where(k => k.PrimaryClientID.HasValue)
                    .Select(k => (k.SessionDate.Date, k.TrainerID, k.PrimaryClientID!.Value, k.SessionType))
                    .ToHashSet();

                var toImport = new List<Session>();
                var skippedRows = new List<int>();

                for (var i = 0; i < sessions.Count; i++)
                {
                    var s = sessions[i];
                    var primaryClientId = s.SessionClients
                        .FirstOrDefault(sc => sc.ParticipantOrder == 1)?.ClientID;

                    if (primaryClientId.HasValue &&
                        existingKeySet.Contains((s.SessionDate.Date, s.TrainerID, primaryClientId.Value, s.SessionType)))
                    {
                        skippedRows.Add(i + 2); 
                    }
                    else
                    {
                        toImport.Add(s);
                    }
                }

                if (toImport.Count == 0)
                {
                    TempData["Feedback"] = "All rows were duplicates — nothing was imported.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Sessions.AddRange(toImport);
                await _context.SaveChangesAsync();

                var feedback = $"Imported {toImport.Count} session(s).";
                if (skippedRows.Any())
                    feedback += $" Skipped {skippedRows.Count} duplicate(s) on row(s): {string.Join(", ", skippedRows)}.";

                TempData["Feedback"] = feedback;
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.InnerException?.Message
                          ?? ex.InnerException?.Message
                          ?? ex.Message;
                TempData["Feedback"] = $"Error: {inner}";
            }

            return RedirectToAction(nameof(Index));
        }

        private IQueryable<Session> SessionDetailsQuery()
        {
            return _context.Sessions
                .Include(s => s.Trainer)
                .Include(s => s.SessionClients)
                    .ThenInclude(sc => sc.Client)
                .Include(s => s.SessionClients)
                    .ThenInclude(sc => sc.SessionNotes)
                .Include(s => s.SessionClients)
                    .ThenInclude(sc => sc.NextSteps)
                .Include(s => s.SessionClients)
                    .ThenInclude(sc => sc.AdminComplete)
                .Include(s => s.SessionClients)
                    .ThenInclude(sc => sc.Accessories)
                .Include(s => s.SessionClients)
                    .ThenInclude(sc => sc.Actions)
                        .ThenInclude(a => a.Exercise)
                            .ThenInclude(e => e!.Apparatus)
                .Include(s => s.SessionClients)
                    .ThenInclude(sc => sc.Actions)
                        .ThenInclude(a => a.ExerciseProps)
                            .ThenInclude(ep => ep.Prop)
                .Include(s => s.PhysioInfo)
                .AsSplitQuery();
        }

        private IQueryable<Session> BuildPreviousSessionsForClientQuery(
            int clientId,
            DateTime? referenceDate,
            int? currentSessionId = null)
        {
            var query = _context.Sessions
                .Where(s => !s.IsArchived
                    && s.SessionClients.Any(sc => sc.ClientID == clientId));

            if (currentSessionId.HasValue && referenceDate.HasValue)
            {
                var normalizedReferenceDate = referenceDate.Value.Date;

                query = query.Where(s => s.ID != currentSessionId.Value
                    && (s.SessionDate < normalizedReferenceDate
                        || (s.SessionDate == normalizedReferenceDate && s.ID < currentSessionId.Value)));
            }
            else if (currentSessionId.HasValue)
            {
                query = query.Where(s => s.ID != currentSessionId.Value);
            }
            else if (referenceDate.HasValue)
            {
                var normalizedReferenceDate = referenceDate.Value.Date;
                query = query.Where(s => s.SessionDate <= normalizedReferenceDate);
            }

            return query
                .OrderByDescending(s => s.SessionDate)
                .ThenByDescending(s => s.ID);
        }

        private async Task<Session?> GetPreviousSessionForClientAsync(
            int clientId,
            DateTime referenceDate,
            int? currentSessionId = null)
        {
            return await BuildPreviousSessionsForClientQuery(clientId, referenceDate, currentSessionId)
                .Include(s => s.SessionClients.Where(sc => sc.ClientID == clientId))
                    .ThenInclude(sc => sc.SessionNotes)
                .Include(s => s.SessionClients.Where(sc => sc.ClientID == clientId))
                    .ThenInclude(sc => sc.Accessories)
                .Include(s => s.SessionClients.Where(sc => sc.ClientID == clientId))
                    .ThenInclude(sc => sc.Actions)
                        .ThenInclude(a => a.ExerciseProps)
                            .ThenInclude(ep => ep.Prop)
                .Include(s => s.SessionClients.Where(sc => sc.ClientID == clientId))
                    .ThenInclude(sc => sc.Actions)
                        .ThenInclude(a => a.Exercise!)
                            .ThenInclude(e => e.Apparatus)
                .Include(s => s.PhysioInfo)
                .OrderByDescending(s => s.SessionDate)
                .ThenByDescending(s => s.ID)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        private async Task<int?> GetCurrentTrainerIdAsync()
        {
            if (_currentTrainerIdResolved)
            {
                return _currentTrainerId;
            }

            _currentTrainerIdResolved = true;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            _currentTrainerId = await _context.Trainers
                .AsNoTracking()
                .Where(t => t.IsActive && t.ApplicationUserId == userId)
                .Select(t => (int?)t.ID)
                .SingleOrDefaultAsync();

            return _currentTrainerId;
        }

        private async Task<HashSet<int>> GetEditableSessionIdsAsync(IEnumerable<Session> sessions)
        {
            var currentTrainerId = await GetCurrentTrainerIdAsync();

            return sessions
                .Where(session => CanCurrentUserOpenEditSession(session, currentTrainerId))
                .Select(session => session.ID)
                .ToHashSet();
        }

        private bool CanCurrentUserOpenEditSession(Session session, int? currentTrainerId)
        {
            return !session.IsArchived
                && SessionAuthorizationHelper.CanOpenEditSession(session, User, currentTrainerId);
        }

        private void PopulateEditViewAccessState(Session session, int? currentTrainerId)
        {
            ViewData["CanStartLoggedStage"] = SessionAuthorizationHelper.CanStartLoggedStage(User, currentTrainerId);

            if (SessionAuthorizationHelper.ShouldDefaultToCompletedStage(session, User, currentTrainerId))
            {
                ViewData["CompletedOnlyAccessMessage"] = CompletedOnlyAccessMessage;
            }
        }

        private bool CanCurrentUserEditTargetStage(Session session, SessionStatus targetStatus, int? currentTrainerId)
        {
            return session.Status switch
            {
                SessionStatus.Opened when targetStatus == SessionStatus.Opened => CanOpenSessionWorkflow(),
                SessionStatus.Opened when targetStatus == SessionStatus.Logged =>
                    SessionAuthorizationHelper.CanStartLoggedStage(User, currentTrainerId),
                SessionStatus.Logged when targetStatus == SessionStatus.Logged =>
                    SessionAuthorizationHelper.CanEditLoggedStage(session, User, currentTrainerId),
                SessionStatus.Logged when targetStatus == SessionStatus.Completed =>
                    SessionAuthorizationHelper.CanEditCompletedStage(session, User),
                SessionStatus.Completed when targetStatus == SessionStatus.Completed =>
                    SessionAuthorizationHelper.CanEditCompletedStage(session, User),
                _ => false
            };
        }

        private int? ResolveLoggedStageOwnerTrainerIdForSave(
            Session session,
            SessionStatus currentStatus,
            SessionStatus targetStatus,
            int? currentTrainerId)
        {
            if (targetStatus == SessionStatus.Opened)
            {
                return null;
            }

            if (currentStatus == SessionStatus.Opened && targetStatus == SessionStatus.Logged)
            {
                return currentTrainerId ?? session.TrainerID;
            }

            return SessionAuthorizationHelper.ResolveLoggedStageOwnerTrainerId(session);
        }

        private async Task CreateSelectedActions(int sessionId, ICollection<int>? selectedExerciseIds)
        {
            if (selectedExerciseIds == null || selectedExerciseIds.Count == 0)
            {
                return;
            }

            var validExerciseIds = await _context.Exercises
                .Where(e => selectedExerciseIds.Contains(e.ID))
                .Select(e => e.ID)
                .ToListAsync();

            if (validExerciseIds.Count == 0)
            {
                return;
            }

            var sessionClients = await _context.SessionClients
                .Where(sc => sc.SessionID == sessionId)
                .OrderBy(sc => sc.ParticipantOrder)
                .ToListAsync();

            if (sessionClients.Count == 0)
            {
                return;
            }

            var actionOwners = sessionClients
                .Where(sc => sc.ParticipantOrder == 1 || sessionClients.Count > 1)
                .ToList();

            foreach (var sessionClient in actionOwners)
            {
                foreach (var exerciseId in validExerciseIds)
                {
                    _context.Actions.Add(new ModelAction
                    {
                        SessionClientID = sessionClient.ID,
                        ExerciseID = exerciseId,
                        Springs = string.Empty,
                        Notes = string.Empty
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        private SelectList ClientSelectList(int? selectedId)
        {
            return new SelectList(_context.Clients
                .OrderBy(d => d.FirstName)
                .ThenBy(d => d.LastName), "ID", "FullName", selectedId);
        }

        private SelectList TrainerList(int? selectedId)
        {
            return new SelectList(_context.Trainers
                .OrderBy(d => d.LastName)
                .ThenBy(d => d.FirstName), "ID", "TrainerName", selectedId);
        }

        private SelectList ExerciseList()
        {
            return new SelectList(_context.Exercises
                .OrderBy(e => e.ExerciseName), "ID", "ExerciseName");
        }

        private void PopulateDropDownLists(SessionUpsertVM? vm = null)
        {
            ViewData["ClientID"] = ClientSelectList(vm?.ClientID);
            ViewData["TrainerID"] = TrainerList(vm?.TrainerID);
            ViewData["ExerciseID"] = ExerciseList();
        }

        private static void EnsureUpsertViewModelChildren(SessionUpsertVM vm)
        {
            vm.SessionNotes ??= new SessionNotes();
            vm.NextSteps ??= new NextSteps();
            vm.AdminComplete ??= new AdminComplete();
            vm.Accessories ??= new Accessories();

            if (vm.SessionType == SessionType.SemiPrivate)
            {
                vm.Client2SessionNotes ??= new SessionNotes();
                vm.Client2NextSteps ??= new NextSteps();
                vm.Client2AdminComplete ??= new AdminComplete();
                vm.Client2Accessories ??= new Accessories();
            }
        }

        private void PrepareUpsertViewModel(SessionUpsertVM vm)
        {
            EnsureUpsertViewModelChildren(vm);

            if (!HasPostedFormValues(nameof(SessionUpsertVM.Accessories)))
            {
                vm.Accessories = null;
            }

            NormalizeSessionNotes(vm.SessionNotes);
            NormalizeNextSteps(vm.NextSteps);
            NormalizeAdminComplete(vm.AdminComplete);

            if (vm.SessionType == SessionType.SemiPrivate)
            {
                vm.Client2SessionNotes ??= new SessionNotes();

                if (!HasPostedFormValues(nameof(SessionUpsertVM.Client2Accessories)))
                {
                    vm.Client2Accessories = null;
                }
                else
                {
                    vm.Client2Accessories ??= new Accessories();
                }

                NormalizeSessionNotes(vm.Client2SessionNotes);
                vm.Client2NextSteps = CopyNextSteps(vm.NextSteps);
                vm.Client2AdminComplete = CopyAdminComplete(vm.AdminComplete);
                NormalizeAccessories(vm.Client2Accessories);
            }
            else
            {
                vm.Client2ID = null;
                vm.Client2SessionsPerWeekRecommended = null;
                vm.Client2SessionNotes = null;
                vm.Client2NextSteps = null;
                vm.Client2AdminComplete = null;
                vm.Client2Accessories = null;

                RemoveModelStatePrefix(nameof(SessionUpsertVM.Client2ID));
                RemoveModelStatePrefix(nameof(SessionUpsertVM.Client2SessionsPerWeekRecommended));
                RemoveModelStatePrefix(nameof(SessionUpsertVM.Client2SessionNotes));
                RemoveModelStatePrefix(nameof(SessionUpsertVM.Client2NextSteps));
                RemoveModelStatePrefix(nameof(SessionUpsertVM.Client2AdminComplete));
                RemoveModelStatePrefix(nameof(SessionUpsertVM.Client2Accessories));
            }

            NormalizeAccessories(vm.Accessories);
            NormalizePhysioInfo(vm);
        }

        private SessionStatus ResolveTargetStatus(SessionUpsertVM vm, SessionStatus? currentStatus = null)
        {
            if (Request.HasFormContentType
                && Request.Form.TryGetValue(nameof(SessionUpsertVM.Status), out var postedStatusValues))
            {
                var postedStatus = postedStatusValues.ToString();
                if (Enum.TryParse<SessionStatus>(postedStatus, true, out var parsedStatus)
                    && Enum.IsDefined(typeof(SessionStatus), parsedStatus))
                {
                    return parsedStatus;
                }

                ModelState.AddModelError(nameof(SessionUpsertVM.Status), "The selected session status is not valid.");
            }

            return currentStatus ?? SessionStatus.Opened;
        }

        private static bool CanTransition(SessionStatus currentStatus, SessionStatus targetStatus)
        {
            return currentStatus switch
            {
                SessionStatus.Opened => targetStatus is SessionStatus.Opened or SessionStatus.Logged,
                SessionStatus.Logged => targetStatus is SessionStatus.Logged or SessionStatus.Completed,
                SessionStatus.Completed => targetStatus == SessionStatus.Completed,
                _ => false
            };
        }

        private SessionStatus ResolveWorkflowEntryStatus(SessionStatus currentStatus, SessionStatus? requestedStatus)
        {
            if (currentStatus == SessionStatus.Opened
                && (!requestedStatus.HasValue || requestedStatus.Value == SessionStatus.Opened)
                && CanLogSessionWorkflow())
            {
                return SessionStatus.Logged;
            }

            if (currentStatus == SessionStatus.Logged
                && (!requestedStatus.HasValue || requestedStatus.Value == SessionStatus.Logged)
                && CanCompleteSessionWorkflow())
            {
                return SessionStatus.Completed;
            }

            if (!requestedStatus.HasValue)
            {
                return currentStatus;
            }

            var targetStatus = requestedStatus.Value;
            if (!CanTransition(currentStatus, targetStatus))
            {
                return currentStatus;
            }

            return CanUseWorkflowAction(targetStatus)
                ? targetStatus
                : currentStatus;
        }

        private SessionStatus ResolveDefaultWorkflowEntryStatus(SessionStatus currentStatus)
        {
            if (currentStatus == SessionStatus.Opened && CanLogSessionWorkflow())
            {
                return SessionStatus.Logged;
            }

            if (currentStatus == SessionStatus.Logged && CanCompleteSessionWorkflow())
            {
                return SessionStatus.Completed;
            }

            return currentStatus;
        }

        private bool CanOpenSessionWorkflow()
        {
            return User.IsInRole(AppRoles.Owner)
                || User.IsInRole(AppRoles.Administration)
                || User.IsInRole(AppRoles.Trainer);
        }

        private bool CanLogSessionWorkflow()
        {
            return User.IsInRole(AppRoles.Owner)
                || User.IsInRole(AppRoles.Trainer);
        }

        private bool CanCompleteSessionWorkflow()
        {
            return User.IsInRole(AppRoles.Owner)
                || User.IsInRole(AppRoles.Administration);
        }

        private bool CanEditGeneralSection()
        {
            return CanOpenSessionWorkflow();
        }

        private bool CanEditTeacherSection()
        {
            return User.IsInRole(AppRoles.Owner)
                || User.IsInRole(AppRoles.Trainer);
        }

        private bool CanEditAdminSection()
        {
            return User.IsInRole(AppRoles.Owner)
                || User.IsInRole(AppRoles.Administration);
        }

        private bool CanUseWorkflowAction(SessionStatus targetStatus)
        {
            return targetStatus switch
            {
                SessionStatus.Opened => CanOpenSessionWorkflow(),
                SessionStatus.Logged => CanLogSessionWorkflow(),
                SessionStatus.Completed => CanCompleteSessionWorkflow(),
                _ => false
            };
        }

        private void EnforceRoleBasedWorkflowAccess(
            SessionUpsertVM vm,
            SessionStatus targetStatus,
            Session? existingSession = null)
        {
            if (!CanUseWorkflowAction(targetStatus))
            {
                ModelState.AddModelError(nameof(SessionUpsertVM.Status),
                    $"You do not have permission to save a session as {targetStatus}.");
            }

            if (!CanEditGeneralSection())
            {
                ModelState.AddModelError(string.Empty,
                    "You do not have permission to edit the General/Open section.");
            }

            if (!CanEditTeacherSection())
            {
                RestoreTeacherSectionValues(vm, existingSession);
            }

            if (!CanEditAdminSection())
            {
                RestoreAdminSectionValues(vm, existingSession);
            }
        }

        private void ValidateForStatus(
            SessionUpsertVM vm,
            SessionStatus targetStatus,
            Session? existingSession = null,
            bool skipActionRequirement = false)
        {
            ValidateDateForStatus(vm, targetStatus);
            ValidateParticipantBlocksForStatus(vm, targetStatus, existingSession);
            ValidateActionsForStatus(vm, targetStatus, existingSession, skipActionRequirement);
            ValidateAdminForStatus(vm, targetStatus);
        }

        private void ValidateDateForStatus(SessionUpsertVM vm, SessionStatus targetStatus)
        {
            if (ModelState.TryGetValue(nameof(SessionUpsertVM.SessionDate), out var sessionDateState)
                && sessionDateState.Errors.Count > 0)
            {
                return;
            }

            var sessionDate = vm.SessionDate.Date;
            var today = DateTime.Today;

            if (targetStatus == SessionStatus.Opened && sessionDate < today)
            {
                ModelState.AddModelError(nameof(SessionUpsertVM.SessionDate),
                    "Opened sessions must have a session date of today or later.");
            }

            if (targetStatus is SessionStatus.Logged or SessionStatus.Completed && sessionDate > today)
            {
                ModelState.AddModelError(nameof(SessionUpsertVM.SessionDate),
                    "Logged and completed sessions must have a session date of today or earlier.");
            }
        }

        private void ValidateParticipantBlocksForStatus(
            SessionUpsertVM vm,
            SessionStatus targetStatus,
            Session? existingSession = null)
        {
            if (!vm.TrainerID.HasValue)
            {
                ModelState.AddModelError(nameof(SessionUpsertVM.TrainerID),
                    "You must select a trainer to add to the session.");
            }

            if (!vm.ClientID.HasValue)
            {
                ModelState.AddModelError(nameof(SessionUpsertVM.ClientID),
                    "You must select a client to add to the session.");
            }

            if (vm.SessionType == SessionType.SemiPrivate)
            {
                if (!vm.Client2ID.HasValue)
                {
                    ModelState.AddModelError(nameof(SessionUpsertVM.Client2ID),
                        "You must select a second client for a semi-private session.");
                }
            }

            if (targetStatus == SessionStatus.Opened)
            {
                return;
            }

            var existingPrimarySessionsPerWeek = existingSession?.PrimarySessionClient?.SessionsPerWeekRecommended;
            if (!vm.SessionsPerWeekRecommended.HasValue && !existingPrimarySessionsPerWeek.HasValue)
            {
                ModelState.AddModelError(nameof(SessionUpsertVM.SessionsPerWeekRecommended),
                    "You must select a recommended session count per week.");
            }

            ValidateParticipantNotes(
                vm.SessionNotes,
                nameof(SessionUpsertVM.SessionNotes));

            if (vm.SessionType == SessionType.SemiPrivate)
            {
                ValidateParticipantNotes(
                    vm.Client2SessionNotes,
                    nameof(SessionUpsertVM.Client2SessionNotes));
            }
        }

        private void ValidateActionsForStatus(
            SessionUpsertVM vm,
            SessionStatus targetStatus,
            Session? existingSession = null,
            bool skipActionRequirement = false)
        {
            if (skipActionRequirement || targetStatus is not SessionStatus.Logged and not SessionStatus.Completed)
            {
                return;
            }

            if (!WillParticipantHaveAtLeastOneAction(existingSession, vm, 1))
            {
                ModelState.AddModelError(nameof(SessionUpsertVM.SelectedExerciseIDs),
                    "Logged and completed sessions must include at least one action for Client 1.");
            }

            if (vm.SessionType == SessionType.SemiPrivate
                && !WillParticipantHaveAtLeastOneAction(existingSession, vm, 2))
            {
                ModelState.AddModelError(nameof(SessionUpsertVM.SelectedExerciseIDs),
                    "Logged and completed semi-private sessions must include at least one action for Client 2.");
            }
        }

        private void ValidateAdminForStatus(SessionUpsertVM vm, SessionStatus targetStatus)
        {
            if (targetStatus != SessionStatus.Completed)
            {
                return;
            }

            ValidateParticipantAdmin(
                vm.AdminComplete,
                nameof(SessionUpsertVM.AdminComplete));

            if (vm.SessionType == SessionType.SemiPrivate)
            {
                ValidateParticipantAdmin(
                    vm.Client2AdminComplete,
                    nameof(SessionUpsertVM.Client2AdminComplete));
            }
        }

        private bool WillParticipantHaveAtLeastOneAction(Session? existingSession, SessionUpsertVM vm, int participantOrder)
        {
            if (existingSession?.SessionClients.Any(sc =>
                    sc.ParticipantOrder == participantOrder
                    && sc.Actions.Any()) == true)
            {
                return true;
            }

            if (participantOrder != 1)
            {
                return existingSession == null
                    && vm.SessionType == SessionType.SemiPrivate
                    && (vm.SelectedExerciseIDs?.Any(id => id > 0) ?? false);
            }

            return existingSession == null
                && (vm.SelectedExerciseIDs?.Any(id => id > 0) ?? false);
        }

        private void ValidateParticipantNotes(SessionNotes? notes, string prefix)
        {
            if (notes == null)
            {
                ModelState.AddModelError($"{prefix}.{nameof(SessionNotes.Goals)}",
                    "You must enter a goal for the session.");
                ModelState.AddModelError($"{prefix}.{nameof(SessionNotes.SubjectiveReports)}",
                    "You must enter a subjective report for the session.");
                ModelState.AddModelError($"{prefix}.{nameof(SessionNotes.ObjectiveFindings)}",
                    "You must enter an objective finding for the session.");
                ModelState.AddModelError($"{prefix}.{nameof(SessionNotes.Plan)}",
                    "You must enter a plan for the session.");
                return;
            }

            if (string.IsNullOrWhiteSpace(notes.Goals))
            {
                ModelState.AddModelError($"{prefix}.{nameof(SessionNotes.Goals)}",
                    "You must enter a goal for the session.");
            }

            if (string.IsNullOrWhiteSpace(notes.SubjectiveReports))
            {
                ModelState.AddModelError($"{prefix}.{nameof(SessionNotes.SubjectiveReports)}",
                    "You must enter a subjective report for the session.");
            }

            if (string.IsNullOrWhiteSpace(notes.ObjectiveFindings))
            {
                ModelState.AddModelError($"{prefix}.{nameof(SessionNotes.ObjectiveFindings)}",
                    "You must enter an objective finding for the session.");
            }

            if (string.IsNullOrWhiteSpace(notes.Plan))
            {
                ModelState.AddModelError($"{prefix}.{nameof(SessionNotes.Plan)}",
                    "You must enter a plan for the session.");
            }
        }

        private void ValidateParticipantAdmin(AdminComplete? adminComplete, string prefix)
        {
            if (adminComplete == null)
            {
                ModelState.AddModelError($"{prefix}.{nameof(AdminComplete.AdminInitials)}",
                    "Admin initials are required when completing a session.");
                ModelState.AddModelError($"{prefix}.{nameof(AdminComplete.IsPaid)}",
                    "You must explicitly review the Is Paid value when completing a session.");
                return;
            }

            if (string.IsNullOrWhiteSpace(adminComplete.AdminInitials))
            {
                ModelState.AddModelError($"{prefix}.{nameof(AdminComplete.AdminInitials)}",
                    "Admin initials are required when completing a session.");
            }

            if (!adminComplete.IsPaid.HasValue)
            {
                ModelState.AddModelError($"{prefix}.{nameof(AdminComplete.IsPaid)}",
                    "You must explicitly review the Is Paid value when completing a session.");
            }
        }

        private void NormalizePhysioInfo(SessionUpsertVM vm)
        {
            if (vm.PhysioInfo == null)
            {
                return;
            }

            vm.PhysioInfo.PhysioAssessment = NullIfWhiteSpace(vm.PhysioInfo.PhysioAssessment);
            vm.PhysioInfo.InsuranceCompany = NullIfWhiteSpace(vm.PhysioInfo.InsuranceCompany);
            vm.PhysioInfo.PhysiotherapistName = NullIfWhiteSpace(vm.PhysioInfo.PhysiotherapistName);

            if (vm.SessionType == SessionType.Physio && HasMeaningfulPhysioInfo(vm.PhysioInfo))
            {
                return;
            }

            vm.PhysioInfo = null;
            RemoveModelStatePrefix(nameof(SessionUpsertVM.PhysioInfo));
        }

        private void ApplySessionValues(Session session, SessionUpsertVM vm)
        {
            session.SessionDate = vm.SessionDate.Date;
            session.IsArchived = vm.IsArchived;
            session.SessionType = vm.SessionType;
            session.Status = vm.Status;
            session.TrainerID = vm.TrainerID!.Value;

            ApplyPhysioInfo(session, vm.PhysioInfo);
        }

        private void ApplySessionNotesForOpenedAddActionNavigation(SessionClient? participant, SessionNotes notes)
        {
            if (participant == null)
            {
                return;
            }

            participant.SessionNotes ??= new SessionNotes();
            participant.SessionNotes.Goals = notes.Goals?.Trim();
            participant.SessionNotes.GeneralComments = NullIfWhiteSpace(notes.GeneralComments);
            participant.SessionNotes.SubjectiveReports = notes.SubjectiveReports?.Trim();
            participant.SessionNotes.ObjectiveFindings = notes.ObjectiveFindings?.Trim();
            participant.SessionNotes.Plan = notes.Plan?.Trim();
        }

        private void ApplyNextStepsForOpenedAddActionNavigation(SessionClient? participant, NextSteps nextSteps)
        {
            if (participant == null)
            {
                return;
            }

            participant.NextSteps ??= new NextSteps();
            participant.NextSteps.NextAppointmentBooked = nextSteps.NextAppointmentBooked;
            participant.NextSteps.CommunicatedProgress = nextSteps.CommunicatedProgress;
            participant.NextSteps.ReadyToProgress = nextSteps.ReadyToProgress;
            participant.NextSteps.CourseCorrectionNeeded = nextSteps.CourseCorrectionNeeded;
            participant.NextSteps.TeamConsult = nextSteps.TeamConsult;
            participant.NextSteps.ReferredExternally = nextSteps.ReferredExternally;
            participant.NextSteps.ReferredTo = NullIfWhiteSpace(nextSteps.ReferredTo);
        }

        private void ApplyAccessoriesForOpenedAddActionNavigation(SessionClient? participant, Accessories? accessories)
        {
            if (participant == null || accessories == null)
            {
                return;
            }

            participant.Accessories ??= new Accessories();
            participant.Accessories.HeadPad = accessories.HeadPad;
            participant.Accessories.StrapsOrHandles = accessories.StrapsOrHandles;
            participant.Accessories.GearBar = accessories.GearBar;
            participant.Accessories.StopperSettings = accessories.StopperSettings;
            participant.Accessories.RubberPads = accessories.RubberPads;
            participant.Accessories.HeadRest = accessories.HeadRest;
            participant.Accessories.Towel = accessories.Towel;
            participant.Accessories.PosturePillow = accessories.PosturePillow;
            participant.Accessories.SpringID = accessories.SpringID;
        }

        private void ApplySessionClientValuesForAddActionNavigation(Session session, SessionUpsertVM vm, int? loggedStageOwnerTrainerId)
        {
            var primaryParticipant = session.PrimarySessionClient;
            if (primaryParticipant != null)
            {
                ApplySessionClientValues(
                    primaryParticipant,
                    vm.SessionsPerWeekRecommended ?? primaryParticipant.SessionsPerWeekRecommended,
                    vm.SessionNotes,
                    vm.NextSteps,
                    vm.AdminComplete,
                    vm.Accessories,
                    loggedStageOwnerTrainerId);
            }

            if (vm.SessionType == SessionType.SemiPrivate)
            {
                var secondaryParticipant = session.SecondarySessionClient;
                if (secondaryParticipant != null)
                {
                    ApplySessionClientValues(
                        secondaryParticipant,
                        vm.Client2SessionsPerWeekRecommended ?? secondaryParticipant.SessionsPerWeekRecommended,
                        vm.Client2SessionNotes!,
                        vm.Client2NextSteps!,
                        vm.Client2AdminComplete!,
                        vm.Client2Accessories,
                        loggedStageOwnerTrainerId);
                }
            }
        }

        private void ApplySessionClientValues(
            SessionClient participant,
            int? sessionsPerWeek,
            SessionNotes notes,
            NextSteps nextSteps,
            AdminComplete adminComplete,
            Accessories? accessories,
            int? loggedStageOwnerTrainerId)
        {
            participant.SessionsPerWeekRecommended = sessionsPerWeek;
            participant.SessionNotes ??= new SessionNotes();
            participant.SessionNotes.Goals = notes.Goals?.Trim();
            participant.SessionNotes.GeneralComments = NullIfWhiteSpace(notes.GeneralComments);
            participant.SessionNotes.SubjectiveReports = notes.SubjectiveReports?.Trim();
            participant.SessionNotes.ObjectiveFindings = notes.ObjectiveFindings?.Trim();
            participant.SessionNotes.Plan = notes.Plan?.Trim();
            participant.SessionNotes.CompletedByTrainerID = loggedStageOwnerTrainerId ?? participant.SessionNotes.CompletedByTrainerID;

            participant.NextSteps ??= new NextSteps();
            participant.NextSteps.NextAppointmentBooked = nextSteps.NextAppointmentBooked;
            participant.NextSteps.CommunicatedProgress = nextSteps.CommunicatedProgress;
            participant.NextSteps.ReadyToProgress = nextSteps.ReadyToProgress;
            participant.NextSteps.CourseCorrectionNeeded = nextSteps.CourseCorrectionNeeded;
            participant.NextSteps.TeamConsult = nextSteps.TeamConsult;
            participant.NextSteps.ReferredExternally = nextSteps.ReferredExternally;
            participant.NextSteps.ReferredTo = NullIfWhiteSpace(nextSteps.ReferredTo);

            participant.AdminComplete ??= new AdminComplete();
            participant.AdminComplete.IsPaid = adminComplete.IsPaid ?? false;
            participant.AdminComplete.AdminNotes = NullIfWhiteSpace(adminComplete.AdminNotes);
            participant.AdminComplete.AdminInitials = NullIfWhiteSpace(adminComplete.AdminInitials);

            if (accessories == null)
            {
                if (participant.Accessories != null)
                {
                    _context.Accessories.Remove(participant.Accessories);
                    participant.Accessories = null;
                }

                return;
            }

            participant.Accessories ??= new Accessories();
            participant.Accessories.HeadPad = accessories.HeadPad;
            participant.Accessories.StrapsOrHandles = accessories.StrapsOrHandles;
            participant.Accessories.GearBar = accessories.GearBar;
            participant.Accessories.StopperSettings = accessories.StopperSettings;
            participant.Accessories.RubberPads = accessories.RubberPads;
            participant.Accessories.HeadRest = accessories.HeadRest;
            participant.Accessories.Towel = accessories.Towel;
            participant.Accessories.PosturePillow = accessories.PosturePillow;
            participant.Accessories.SpringID = accessories.SpringID;
        }

        private void ApplyPhysioInfo(Session session, PhysioInfo? source)
        {
            if (source == null)
            {
                if (session.PhysioInfo != null)
                {
                    _context.PhysioInfos.Remove(session.PhysioInfo);
                    session.PhysioInfo = null;
                }

                return;
            }

            session.PhysioInfo ??= new PhysioInfo();
            session.PhysioInfo.PhysioAssessment = source.PhysioAssessment;
            session.PhysioInfo.InsuranceCompany = source.InsuranceCompany;
            session.PhysioInfo.CoverageAmountPerYear = source.CoverageAmountPerYear;
            session.PhysioInfo.AmountUsed = source.AmountUsed;
            session.PhysioInfo.CoverageResetsDate = source.CoverageResetsDate;
            session.PhysioInfo.PhysiotherapistName = source.PhysiotherapistName;
            session.PhysioInfo.CoverageShared = source.CoverageShared;
            session.PhysioInfo.CommunicatedWithPhysio = source.CommunicatedWithPhysio;
        }

        private IEnumerable<SessionClient> BuildSessionClients(
            SessionUpsertVM vm,
            IReadOnlyDictionary<int, SessionClient>? existingParticipantsByOrder = null,
            int? loggedStageOwnerTrainerId = null)
        {
            existingParticipantsByOrder ??= new Dictionary<int, SessionClient>();
            var participants = new List<SessionClient>
            {
                BuildSessionClient(
                    clientId: vm.ClientID!.Value,
                    participantOrder: 1,
                    sessionsPerWeek: vm.SessionsPerWeekRecommended
                        ?? (existingParticipantsByOrder.TryGetValue(1, out var primary) ? primary.SessionsPerWeekRecommended : null),
                    notes: vm.SessionNotes,
                    nextSteps: vm.NextSteps,
                    adminComplete: vm.AdminComplete,
                    accessories: vm.Accessories,
                    loggedStageOwnerTrainerId: loggedStageOwnerTrainerId,
                    previousParticipant: existingParticipantsByOrder.TryGetValue(1, out var primaryParticipant)
                        ? primaryParticipant
                        : null)
            };

            if (vm.SessionType == SessionType.SemiPrivate)
            {
                participants.Add(BuildSessionClient(
                    clientId: vm.Client2ID!.Value,
                    participantOrder: 2,
                    sessionsPerWeek: vm.Client2SessionsPerWeekRecommended
                        ?? (existingParticipantsByOrder.TryGetValue(2, out var secondary) ? secondary.SessionsPerWeekRecommended : null),
                    notes: vm.Client2SessionNotes!,
                    nextSteps: vm.Client2NextSteps!,
                    adminComplete: vm.Client2AdminComplete!,
                    accessories: vm.Client2Accessories,
                    loggedStageOwnerTrainerId: loggedStageOwnerTrainerId,
                    previousParticipant: existingParticipantsByOrder.TryGetValue(2, out var secondaryParticipant)
                        ? secondaryParticipant
                        : null));
            }

            return participants;
        }

        private void RestoreReadOnlyEditGeneralValues(
            SessionUpsertVM vm,
            Session existingSession,
            bool includeSessionsPerWeek = true)
        {
            var primarySessionClient = existingSession.PrimarySessionClient;
            var secondarySessionClient = existingSession.SecondarySessionClient;

            vm.IsArchived = existingSession.IsArchived;
            vm.SessionDate = existingSession.SessionDate;
            vm.SessionType = existingSession.SessionType;
            vm.TrainerID = existingSession.TrainerID;
            vm.ClientID = primarySessionClient?.ClientID;
            vm.Client2ID = secondarySessionClient?.ClientID;

            if (includeSessionsPerWeek)
            {
                vm.SessionsPerWeekRecommended = primarySessionClient?.SessionsPerWeekRecommended;
                vm.Client2SessionsPerWeekRecommended = existingSession.SessionType == SessionType.SemiPrivate
                    ? secondarySessionClient?.SessionsPerWeekRecommended
                    : null;
            }

            RemoveModelStatePrefix(nameof(SessionUpsertVM.IsArchived));
            RemoveModelStatePrefix(nameof(SessionUpsertVM.SessionDate));
            RemoveModelStatePrefix(nameof(SessionUpsertVM.SessionType));
            RemoveModelStatePrefix(nameof(SessionUpsertVM.TrainerID));
            RemoveModelStatePrefix(nameof(SessionUpsertVM.ClientID));
            RemoveModelStatePrefix(nameof(SessionUpsertVM.Client2ID));

            if (includeSessionsPerWeek)
            {
                RemoveModelStatePrefix(nameof(SessionUpsertVM.SessionsPerWeekRecommended));
                RemoveModelStatePrefix(nameof(SessionUpsertVM.Client2SessionsPerWeekRecommended));
            }
        }

        private static SessionClient BuildSessionClient(
            int clientId,
            int participantOrder,
            int? sessionsPerWeek,
            SessionNotes notes,
            NextSteps nextSteps,
            AdminComplete adminComplete,
            Accessories? accessories,
            int? loggedStageOwnerTrainerId,
            SessionClient? previousParticipant)
        {
            return new SessionClient
            {
                ClientID = clientId,
                ParticipantOrder = participantOrder,
                SessionsPerWeekRecommended = sessionsPerWeek,
                SessionNotes = new SessionNotes
                {
                    Goals = notes.Goals?.Trim(),
                    GeneralComments = NullIfWhiteSpace(notes.GeneralComments),
                    SubjectiveReports = notes.SubjectiveReports?.Trim(),
                    ObjectiveFindings = notes.ObjectiveFindings?.Trim(),
                    Plan = notes.Plan?.Trim(),
                    CompletedByTrainerID = loggedStageOwnerTrainerId ?? previousParticipant?.SessionNotes?.CompletedByTrainerID
                },
                NextSteps = new NextSteps
                {
                    NextAppointmentBooked = nextSteps.NextAppointmentBooked,
                    CommunicatedProgress = nextSteps.CommunicatedProgress,
                    ReadyToProgress = nextSteps.ReadyToProgress,
                    CourseCorrectionNeeded = nextSteps.CourseCorrectionNeeded,
                    TeamConsult = nextSteps.TeamConsult,
                    ReferredExternally = nextSteps.ReferredExternally,
                    ReferredTo = NullIfWhiteSpace(nextSteps.ReferredTo)
                },
                AdminComplete = new AdminComplete
                {
                    IsPaid = adminComplete.IsPaid ?? false,
                    AdminNotes = NullIfWhiteSpace(adminComplete.AdminNotes),
                    AdminInitials = NullIfWhiteSpace(adminComplete.AdminInitials)
                },
                Accessories = accessories == null
                    ? null
                    : CopyAccessories(accessories),
                Actions = CopyActions(previousParticipant?.Actions)
            };
        }

        private void RestoreReadOnlyWorkflowStageValues(
            SessionUpsertVM vm,
            Session existingSession,
            SessionStatus activeStage)
        {
            if (activeStage != SessionStatus.Opened)
            {
                RestoreReadOnlyEditGeneralValues(
                    vm,
                    existingSession,
                    includeSessionsPerWeek: activeStage != SessionStatus.Logged);
            }

            if (activeStage != SessionStatus.Logged)
            {
                RestoreTeacherSectionValues(vm, existingSession, addModelError: false);
            }

            if (activeStage != SessionStatus.Completed)
            {
                RestoreAdminSectionValues(vm, existingSession, addModelError: false);
            }
        }

        private void RestoreTeacherSectionValues(SessionUpsertVM vm, Session? existingSession, bool addModelError = true)
        {
            var primarySessionClient = existingSession?.PrimarySessionClient;
            var secondarySessionClient = existingSession?.SecondarySessionClient;
            var teacherSectionChanged =
                !SessionNotesMatch(vm.SessionNotes, primarySessionClient?.SessionNotes) ||
                !NextStepsMatch(vm.NextSteps, primarySessionClient?.NextSteps) ||
                !AccessoriesMatch(vm.Accessories, primarySessionClient?.Accessories) ||
                (vm.SessionType == SessionType.SemiPrivate &&
                    (!SessionNotesMatch(vm.Client2SessionNotes, secondarySessionClient?.SessionNotes) ||
                     !NextStepsMatch(vm.Client2NextSteps, secondarySessionClient?.NextSteps) ||
                     !AccessoriesMatch(vm.Client2Accessories, secondarySessionClient?.Accessories))) ||
                (existingSession == null && HasPostedParticipantActionSelections(vm));

            vm.SessionNotes = CopySessionNotes(primarySessionClient?.SessionNotes);
            vm.NextSteps = CopyNextSteps(primarySessionClient?.NextSteps);
            vm.Accessories = CopyAccessories(primarySessionClient?.Accessories);
            vm.Client2SessionNotes = vm.SessionType == SessionType.SemiPrivate
                ? CopySessionNotes(secondarySessionClient?.SessionNotes)
                : null;
            vm.Client2NextSteps = vm.SessionType == SessionType.SemiPrivate
                ? CopyNextSteps(secondarySessionClient?.NextSteps)
                : null;
            vm.Client2Accessories = vm.SessionType == SessionType.SemiPrivate
                ? CopyAccessories(secondarySessionClient?.Accessories)
                : null;

            if (existingSession == null)
            {
                vm.SelectedExerciseIDs = new List<int>();
                RemoveModelStatePrefix(nameof(SessionUpsertVM.SelectedExerciseIDs));
            }

            RemoveModelStatePrefix(nameof(SessionUpsertVM.SessionNotes));
            RemoveModelStatePrefix(nameof(SessionUpsertVM.NextSteps));
            RemoveModelStatePrefix(nameof(SessionUpsertVM.Client2SessionNotes));
            RemoveModelStatePrefix(nameof(SessionUpsertVM.Client2NextSteps));
            RemoveModelStatePrefix(nameof(SessionUpsertVM.Accessories));
            RemoveModelStatePrefix(nameof(SessionUpsertVM.Client2Accessories));

            if (teacherSectionChanged && addModelError)
            {
                ModelState.AddModelError(string.Empty,
                    "You do not have permission to edit the Teacher/Log section.");
            }
        }

        private void RestoreAdminSectionValues(SessionUpsertVM vm, Session? existingSession, bool addModelError = true)
        {
            var primarySessionClient = existingSession?.PrimarySessionClient;
            var secondarySessionClient = existingSession?.SecondarySessionClient;
            var adminSectionChanged =
                !AdminCompleteMatch(vm.AdminComplete, primarySessionClient?.AdminComplete) ||
                (vm.SessionType == SessionType.SemiPrivate &&
                    !AdminCompleteMatch(vm.Client2AdminComplete, secondarySessionClient?.AdminComplete));

            vm.AdminComplete = CopyAdminComplete(primarySessionClient?.AdminComplete);
            vm.Client2AdminComplete = vm.SessionType == SessionType.SemiPrivate
                ? CopyAdminComplete(secondarySessionClient?.AdminComplete)
                : null;

            RemoveModelStatePrefix(nameof(SessionUpsertVM.AdminComplete));
            RemoveModelStatePrefix(nameof(SessionUpsertVM.Client2AdminComplete));

            if (adminSectionChanged && addModelError)
            {
                ModelState.AddModelError(string.Empty,
                    "You do not have permission to edit the Admin/Complete section.");
            }
        }

        private static SessionNotes CopySessionNotes(SessionNotes? source)
        {
            return new SessionNotes
            {
                Goals = source?.Goals,
                GeneralComments = source?.GeneralComments,
                SubjectiveReports = source?.SubjectiveReports,
                ObjectiveFindings = source?.ObjectiveFindings,
                Plan = source?.Plan,
                CompletedByTrainerID = source?.CompletedByTrainerID
            };
        }

        private static NextSteps CopyNextSteps(NextSteps? source)
        {
            return new NextSteps
            {
                NextAppointmentBooked = source?.NextAppointmentBooked ?? false,
                CommunicatedProgress = source?.CommunicatedProgress ?? false,
                ReadyToProgress = source?.ReadyToProgress ?? false,
                CourseCorrectionNeeded = source?.CourseCorrectionNeeded ?? false,
                TeamConsult = source?.TeamConsult ?? false,
                ReferredExternally = source?.ReferredExternally ?? false,
                ReferredTo = source?.ReferredTo
            };
        }

        private static AdminComplete CopyAdminComplete(AdminComplete? source)
        {
            return new AdminComplete
            {
                IsPaid = source?.IsPaid,
                AdminNotes = source?.AdminNotes,
                AdminInitials = source?.AdminInitials
            };
        }

        private static Accessories CopyAccessories(Accessories? source)
        {
            return new Accessories
            {
                HeadPad = source?.HeadPad ?? default,
                StrapsOrHandles = source?.StrapsOrHandles ?? default,
                GearBar = source?.GearBar ?? default,
                StopperSettings = source?.StopperSettings ?? default,
                RubberPads = source?.RubberPads ?? false,
                HeadRest = source?.HeadRest ?? false,
                Towel = source?.Towel ?? false,
                PosturePillow = source?.PosturePillow ?? false,
                SpringID = source?.SpringID
            };
        }

        private static SessionClient? GetParticipantForClient(Session? session, int? clientId)
        {
            if (session == null || !clientId.HasValue)
            {
                return null;
            }

            return session.SessionClients.FirstOrDefault(sc => sc.ClientID == clientId.Value);
        }

        private static void ApplyCarryForwardText(string? previousValue, Action<string> assignValue, string? currentValue)
        {
            if (!string.IsNullOrWhiteSpace(currentValue) || string.IsNullOrWhiteSpace(previousValue))
            {
                return;
            }

            assignValue(previousValue.Trim());
        }

        private static void ApplyCarryForwardValuesForEdit(
            SessionUpsertVM vm,
            Session currentSession,
            Session? previousPrimarySession,
            Session? previousSecondarySession)
        {
            if (vm.Status == SessionStatus.Logged)
            {
                var currentPrimaryParticipant = currentSession.PrimarySessionClient;
                var previousPrimaryParticipant = GetParticipantForClient(previousPrimarySession, currentPrimaryParticipant?.ClientID);
                ApplyCarryForwardText(
                    previousPrimaryParticipant?.SessionNotes?.Goals,
                    value => vm.SessionNotes.Goals = value,
                    currentPrimaryParticipant?.SessionNotes?.Goals);
                ApplyCarryForwardText(
                    previousPrimaryParticipant?.SessionNotes?.GeneralComments,
                    value => vm.SessionNotes.GeneralComments = value,
                    currentPrimaryParticipant?.SessionNotes?.GeneralComments);

                if (currentPrimaryParticipant?.Accessories == null && previousPrimaryParticipant?.Accessories != null)
                {
                    vm.Accessories = CopyAccessories(previousPrimaryParticipant.Accessories);
                }

                if (vm.SessionType == SessionType.SemiPrivate)
                {
                    var currentSecondaryParticipant = currentSession.SecondarySessionClient;
                    var previousSecondaryParticipant = GetParticipantForClient(previousSecondarySession, currentSecondaryParticipant?.ClientID);

                    vm.Client2SessionNotes ??= new SessionNotes();
                    vm.Client2Accessories ??= new Accessories();

                    ApplyCarryForwardText(
                        previousSecondaryParticipant?.SessionNotes?.Goals,
                        value => vm.Client2SessionNotes.Goals = value,
                        currentSecondaryParticipant?.SessionNotes?.Goals);
                    ApplyCarryForwardText(
                        previousSecondaryParticipant?.SessionNotes?.GeneralComments,
                        value => vm.Client2SessionNotes.GeneralComments = value,
                        currentSecondaryParticipant?.SessionNotes?.GeneralComments);

                    if (currentSecondaryParticipant?.Accessories == null && previousSecondaryParticipant?.Accessories != null)
                    {
                        vm.Client2Accessories = CopyAccessories(previousSecondaryParticipant.Accessories);
                    }
                }
            }

            if (vm.Status == SessionStatus.Completed
                && vm.SessionType == SessionType.Physio
                && string.IsNullOrWhiteSpace(currentSession.PhysioInfo?.PhysioAssessment)
                && !string.IsNullOrWhiteSpace(previousPrimarySession?.PhysioInfo?.PhysioAssessment))
            {
                vm.PhysioInfo ??= new PhysioInfo();
                vm.PhysioInfo.PhysioAssessment = previousPrimarySession.PhysioInfo.PhysioAssessment!.Trim();
            }
        }

        private static ICollection<ModelAction> CopyActions(IEnumerable<ModelAction>? source)
        {
            return source?
                .Select(action => new ModelAction
                {
                    ExerciseID = action.ExerciseID,
                    ActionType = action.ActionType,
                    Springs = action.Springs,
                    Notes = action.Notes,
                    ExerciseProps = action.ExerciseProps
                        .Select(prop => new ExerciseProp
                        {
                            PropID = prop.PropID
                        })
                        .ToList()
                })
                .ToList() ?? new List<ModelAction>();
        }

        private static bool HasPostedParticipantActionSelections(SessionUpsertVM vm)
        {
            if (!(vm.SelectedExerciseIDs?.Any(id => id > 0) ?? false))
            {
                return false;
            }

            return true;
        }

        private static bool SessionNotesMatch(SessionNotes? posted, SessionNotes? current)
        {
            return PostedTextMatchesCurrent(posted?.Goals, current?.Goals)
                && PostedTextMatchesCurrent(posted?.GeneralComments, current?.GeneralComments)
                && PostedTextMatchesCurrent(posted?.SubjectiveReports, current?.SubjectiveReports)
                && PostedTextMatchesCurrent(posted?.ObjectiveFindings, current?.ObjectiveFindings)
                && PostedTextMatchesCurrent(posted?.Plan, current?.Plan);
        }

        private static bool NextStepsMatch(NextSteps? posted, NextSteps? current)
        {
            return (posted?.NextAppointmentBooked ?? false) == (current?.NextAppointmentBooked ?? false)
                && (posted?.CommunicatedProgress ?? false) == (current?.CommunicatedProgress ?? false)
                && (posted?.ReadyToProgress ?? false) == (current?.ReadyToProgress ?? false)
                && (posted?.CourseCorrectionNeeded ?? false) == (current?.CourseCorrectionNeeded ?? false)
                && (posted?.TeamConsult ?? false) == (current?.TeamConsult ?? false)
                && (posted?.ReferredExternally ?? false) == (current?.ReferredExternally ?? false)
                && PostedTextMatchesCurrent(posted?.ReferredTo, current?.ReferredTo);
        }

        private static bool AdminCompleteMatch(AdminComplete? posted, AdminComplete? current)
        {
            return posted?.IsPaid == current?.IsPaid
                && PostedTextMatchesCurrent(posted?.AdminNotes, current?.AdminNotes)
                && PostedTextMatchesCurrent(posted?.AdminInitials, current?.AdminInitials);
        }

        private static bool AccessoriesMatch(Accessories? posted, Accessories? current)
        {
            return (posted?.HeadPad ?? default) == (current?.HeadPad ?? default)
                && (posted?.StrapsOrHandles ?? default) == (current?.StrapsOrHandles ?? default)
                && (posted?.GearBar ?? default) == (current?.GearBar ?? default)
                && (posted?.StopperSettings ?? default) == (current?.StopperSettings ?? default)
                && (posted?.RubberPads ?? false) == (current?.RubberPads ?? false)
                && (posted?.HeadRest ?? false) == (current?.HeadRest ?? false)
                && (posted?.Towel ?? false) == (current?.Towel ?? false)
                && (posted?.PosturePillow ?? false) == (current?.PosturePillow ?? false)
                && posted?.SpringID == current?.SpringID;
        }

        private static bool PostedTextMatchesCurrent(string? postedValue, string? currentValue)
        {
            return string.Equals(
                postedValue?.Trim() ?? string.Empty,
                currentValue?.Trim() ?? string.Empty,
                StringComparison.Ordinal);
        }

        private static void NormalizeSessionNotes(SessionNotes notes)
        {
            notes.Goals = notes.Goals?.Trim();
            notes.GeneralComments = NullIfWhiteSpace(notes.GeneralComments);
            notes.SubjectiveReports = notes.SubjectiveReports?.Trim();
            notes.ObjectiveFindings = notes.ObjectiveFindings?.Trim();
            notes.Plan = notes.Plan?.Trim();
        }

        private static void NormalizeNextSteps(NextSteps nextSteps)
        {
            nextSteps.ReferredTo = NullIfWhiteSpace(nextSteps.ReferredTo);
        }

        private static void NormalizeAdminComplete(AdminComplete adminComplete)
        {
            adminComplete.AdminNotes = NullIfWhiteSpace(adminComplete.AdminNotes);
            adminComplete.AdminInitials = NullIfWhiteSpace(adminComplete.AdminInitials);
        }

        private static void NormalizeAccessories(Accessories? accessories)
        {
            if (accessories != null && accessories.SpringID <= 0)
            {
                accessories.SpringID = null;
            }
        }

        private void RemoveModelStatePrefix(string prefix)
        {
            var matchingKeys = ModelState.Keys
                .Where(k => k.Equals(prefix, StringComparison.Ordinal)
                    || k.StartsWith(prefix + ".", StringComparison.Ordinal))
                .ToList();

            foreach (var key in matchingKeys)
            {
                ModelState.Remove(key);
            }
        }

        private bool HasPostedFormValues(string prefix)
        {
            return Request.HasFormContentType
                && Request.Form.Keys.Any(k => k.StartsWith(prefix + ".", StringComparison.Ordinal));
        }

        private static bool HasMeaningfulPhysioInfo(PhysioInfo physioInfo)
        {
            return !string.IsNullOrWhiteSpace(physioInfo.PhysioAssessment)
                || !string.IsNullOrWhiteSpace(physioInfo.InsuranceCompany)
                || physioInfo.CoverageAmountPerYear.HasValue
                || physioInfo.AmountUsed.HasValue
                || physioInfo.CoverageResetsDate.HasValue
                || !string.IsNullOrWhiteSpace(physioInfo.PhysiotherapistName)
                || physioInfo.CoverageShared
                || physioInfo.CommunicatedWithPhysio;
        }

        private bool SessionExists(int id)
        {
            return _context.Sessions.Any(e => e.ID == id);
        }

        private SessionUpsertVM BuildSessionUpsertViewModel(Session session)
        {
            var primary = session.PrimarySessionClient;
            var secondary = session.SecondarySessionClient;

            return new SessionUpsertVM
            {
                ID = session.ID,
                SessionDate = session.SessionDate,
                IsArchived = session.IsArchived,
                SessionType = session.SessionType,
                CurrentStatus = session.Status,
                Status = session.Status,
                TrainerID = session.TrainerID,
                PrimarySessionClientID = primary?.ID,
                SecondarySessionClientID = secondary?.ID,
                ClientID = primary?.ClientID,
                SessionsPerWeekRecommended = primary?.SessionsPerWeekRecommended,
                SessionNotes = primary?.SessionNotes ?? new SessionNotes(),
                NextSteps = primary?.NextSteps ?? new NextSteps(),
                AdminComplete = primary?.AdminComplete ?? new AdminComplete(),
                Client2ID = secondary?.ClientID,
                Client2SessionsPerWeekRecommended = secondary?.SessionsPerWeekRecommended,
                Client2SessionNotes = secondary?.SessionNotes,
                Client2NextSteps = secondary?.NextSteps,
                Client2AdminComplete = secondary?.AdminComplete,
                Client2Accessories = session.SessionType == SessionType.SemiPrivate
                    ? secondary?.Accessories ?? new Accessories()
                    : null,
                PhysioInfo = session.PhysioInfo,
                Accessories = primary?.Accessories ?? new Accessories(),
                SelectedExerciseIDs = (primary?.Actions ?? Enumerable.Empty<ModelAction>())
                    .Select(a => a.ExerciseID)
                    .ToList(),
                ExistingActions = session.Actions
                    .OrderBy(a => a.SessionClient?.ParticipantOrder)
                    .ThenBy(a => a.ID)
                    .ToList()
            };
        }

        private static void WriteImportHeaders(ExcelWorksheet worksheet)
        {
            for (var column = 0; column < ImportHeaders.Length; column++)
            {
                worksheet.Cells[1, column + 1].Value = ImportHeaders[column];
            }
        }

        private static void WriteTemplateExampleRow(ExcelWorksheet worksheet)
        {
            var sampleValues = new Dictionary<string, object?>
            {
                ["SessionDate"] = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"),
                ["SessionType"] = SessionType.SemiPrivate.ToString(),
                ["TrainerEmail"] = "alex@mu.com",
                ["PrimaryClientName"] = "Ava Reed",
                ["PrimaryClientEmail"] = "ava.reed@example.com",
                ["PrimarySessionsPerWeek"] = 2,
                ["PrimaryGoals"] = "Improve core strength.",
                ["PrimaryGeneralComments"] = "Morning session preferred.",
                ["PrimarySubjectiveReports"] = "No pain today.",
                ["PrimaryObjectiveFindings"] = "Improved trunk control.",
                ["PrimaryPlan"] = "Progress reformer series.",
                ["PrimaryNextAppointmentBooked"] = true,
                ["PrimaryCommunicatedProgress"] = true,
                ["PrimaryReadyToProgress"] = true,
                ["PrimaryCourseCorrectionNeeded"] = false,
                ["PrimaryTeamConsult"] = false,
                ["PrimaryReferredExternally"] = false,
                ["PrimaryIsPaid"] = true,
                ["PrimaryAdminNotes"] = "Paid at front desk.",
                ["PrimaryAdminInitials"] = "AG",
                ["SecondaryClientName"] = "Noah Hughes",
                ["SecondaryClientEmail"] = "noah.hughes@example.com",
                ["SecondarySessionsPerWeek"] = 1,
                ["SecondaryGoals"] = "Build lower-body stability.",
                ["SecondarySubjectiveReports"] = "Knee feels stable.",
                ["SecondaryObjectiveFindings"] = "Good balance response.",
                ["SecondaryPlan"] = "Add progressive loading.",
                ["AccessoriesHeadPad"] = HeadPadOption.Full.ToString(),
                ["AccessoriesStrapsOrHandles"] = StrapOrHandleOption.Handles.ToString(),
                ["AccessoriesGearBar"] = 2,
                ["AccessoriesStopperSettings"] = 3,
                ["AccessoriesRubberPads"] = true,
                ["AccessoriesHeadRest"] = true,
                ["AccessoriesTowel"] = false,
                ["AccessoriesPosturePillow"] = false,
                ["Exercises"] = "Reformer Footwork|Hundred"
            };

            for (var column = 0; column < ImportHeaders.Length; column++)
            {
                sampleValues.TryGetValue(ImportHeaders[column], out var value);
                worksheet.Cells[2, column + 1].Value = value;
            }
        }

        private static void WriteSessionRow(ExcelWorksheet worksheet, int row, Session session)
        {
            var primary = session.PrimarySessionClient;
            var secondary = session.SecondarySessionClient;
            var values = new Dictionary<string, object?>
            {
                ["SessionDate"] = session.SessionDate.ToString("yyyy-MM-dd"),
                ["SessionType"] = session.SessionType.ToString(),
                ["TrainerEmail"] = session.Trainer?.Email,
                ["PrimaryClientEmail"] = primary?.Client?.Email,
                ["PrimarySessionsPerWeek"] = primary?.SessionsPerWeekRecommended,
                ["PrimaryGoals"] = primary?.SessionNotes?.Goals,
                ["PrimaryGeneralComments"] = primary?.SessionNotes?.GeneralComments,
                ["PrimarySubjectiveReports"] = primary?.SessionNotes?.SubjectiveReports,
                ["PrimaryObjectiveFindings"] = primary?.SessionNotes?.ObjectiveFindings,
                ["PrimaryPlan"] = primary?.SessionNotes?.Plan,
                ["PrimaryNextAppointmentBooked"] = primary?.NextSteps?.NextAppointmentBooked,
                ["PrimaryCommunicatedProgress"] = primary?.NextSteps?.CommunicatedProgress,
                ["PrimaryReadyToProgress"] = primary?.NextSteps?.ReadyToProgress,
                ["PrimaryCourseCorrectionNeeded"] = primary?.NextSteps?.CourseCorrectionNeeded,
                ["PrimaryTeamConsult"] = primary?.NextSteps?.TeamConsult,
                ["PrimaryReferredExternally"] = primary?.NextSteps?.ReferredExternally,
                ["PrimaryReferredTo"] = primary?.NextSteps?.ReferredTo,
                ["PrimaryIsPaid"] = primary?.AdminComplete?.IsPaid,
                ["PrimaryAdminNotes"] = primary?.AdminComplete?.AdminNotes,
                ["PrimaryAdminInitials"] = primary?.AdminComplete?.AdminInitials,
                ["SecondaryClientEmail"] = secondary?.Client?.Email,
                ["SecondarySessionsPerWeek"] = secondary?.SessionsPerWeekRecommended,
                ["SecondaryGoals"] = secondary?.SessionNotes?.Goals,
                ["SecondaryGeneralComments"] = secondary?.SessionNotes?.GeneralComments,
                ["SecondarySubjectiveReports"] = secondary?.SessionNotes?.SubjectiveReports,
                ["SecondaryObjectiveFindings"] = secondary?.SessionNotes?.ObjectiveFindings,
                ["SecondaryPlan"] = secondary?.SessionNotes?.Plan,
                ["SecondaryNextAppointmentBooked"] = secondary?.NextSteps?.NextAppointmentBooked,
                ["SecondaryCommunicatedProgress"] = secondary?.NextSteps?.CommunicatedProgress,
                ["SecondaryReadyToProgress"] = secondary?.NextSteps?.ReadyToProgress,
                ["SecondaryCourseCorrectionNeeded"] = secondary?.NextSteps?.CourseCorrectionNeeded,
                ["SecondaryTeamConsult"] = secondary?.NextSteps?.TeamConsult,
                ["SecondaryReferredExternally"] = secondary?.NextSteps?.ReferredExternally,
                ["SecondaryReferredTo"] = secondary?.NextSteps?.ReferredTo,
                ["SecondaryIsPaid"] = secondary?.AdminComplete?.IsPaid,
                ["SecondaryAdminNotes"] = secondary?.AdminComplete?.AdminNotes,
                ["SecondaryAdminInitials"] = secondary?.AdminComplete?.AdminInitials,
                ["AccessoriesHeadPad"] = session.PrimarySessionClient?.Accessories?.HeadPad.ToString(),
                ["AccessoriesStrapsOrHandles"] = session.PrimarySessionClient?.Accessories?.StrapsOrHandles.ToString(),
                ["AccessoriesGearBar"] = session.PrimarySessionClient?.Accessories?.GearBar,
                ["AccessoriesStopperSettings"] = session.PrimarySessionClient?.Accessories?.StopperSettings,
                ["AccessoriesRubberPads"] = session.PrimarySessionClient?.Accessories?.RubberPads,
                ["AccessoriesHeadRest"] = session.PrimarySessionClient?.Accessories?.HeadRest,
                ["AccessoriesTowel"] = session.PrimarySessionClient?.Accessories?.Towel,
                ["AccessoriesPosturePillow"] = session.PrimarySessionClient?.Accessories?.PosturePillow,
                ["PhysioAssessment"] = session.PhysioInfo?.PhysioAssessment,
                ["InsuranceCompany"] = session.PhysioInfo?.InsuranceCompany,
                ["CoverageAmountPerYear"] = session.PhysioInfo?.CoverageAmountPerYear,
                ["AmountUsed"] = session.PhysioInfo?.AmountUsed,
                ["CoverageResetsDate"] = session.PhysioInfo?.CoverageResetsDate?.ToString("yyyy-MM-dd"),
                ["PhysiotherapistName"] = session.PhysioInfo?.PhysiotherapistName,
                ["CoverageShared"] = session.PhysioInfo?.CoverageShared,
                ["CommunicatedWithPhysio"] = session.PhysioInfo?.CommunicatedWithPhysio,
                ["Exercises"] = string.Join("|", session.Actions.OrderBy(a => a.ID).Select(a => a.Exercise?.ExerciseName).Where(name => !string.IsNullOrWhiteSpace(name)))
            };

            for (var column = 0; column < ImportHeaders.Length; column++)
            {
                values.TryGetValue(ImportHeaders[column], out var value);
                worksheet.Cells[row, column + 1].Value = value;
            }
        }

        private static Dictionary<string, int> ReadHeaderMap(ExcelWorksheet worksheet)
        {
            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var lastColumn = worksheet.Dimension?.End.Column ?? 0;

            for (var column = 1; column <= lastColumn; column++)
            {
                var header = worksheet.Cells[1, column].Text?.Trim();
                if (!string.IsNullOrWhiteSpace(header))
                {
                    headerMap[header] = column;
                }
            }

            return headerMap;
        }

        private static void ValidateImportHeaders(IReadOnlyDictionary<string, int> headerMap)
        {
            var missingHeaders = ImportHeaders.Where(header => !headerMap.ContainsKey(header)).ToList();
            if (missingHeaders.Any())
            {
                throw new InvalidOperationException($"Missing required column(s): {string.Join(", ", missingHeaders)}.");
            }
        }

        private static string GetCellText(ExcelWorksheet worksheet, int row, IReadOnlyDictionary<string, int> headerMap, string header)
        {
            return headerMap.TryGetValue(header, out var column)
                ? worksheet.Cells[row, column].Text?.Trim() ?? string.Empty
                : string.Empty;
        }

        private Session BuildImportedSession(
            ExcelWorksheet worksheet,
            int row,
            IReadOnlyDictionary<string, int> headerMap,
            IReadOnlyDictionary<string, int> trainerIds,
            IReadOnlyDictionary<string, int> clientIds,
            IReadOnlyDictionary<string, int> exerciseIds)
        {
            var sessionType = ParseEnum<SessionType>(GetCellText(worksheet, row, headerMap, "SessionType"), "SessionType", row);
            var trainerEmail = GetRequiredCell(worksheet, row, headerMap, "TrainerEmail");
            var trainerId = ResolveLookup(trainerIds, trainerEmail, "trainer", row);

            var session = new Session
            {
                SessionDate = ParseDate(GetRequiredCell(worksheet, row, headerMap, "SessionDate"), "SessionDate", row),
                SessionType = sessionType,
                TrainerID = trainerId,
                PhysioInfo = BuildImportedPhysioInfo(worksheet, row, headerMap)
            };

            var importedAccessories = BuildImportedAccessories(worksheet, row, headerMap);

            session.SessionClients.Add(BuildImportedParticipant(
                worksheet, row, headerMap, clientIds, "Primary", 1, importedAccessories));

            var secondaryClientEmail = GetCellText(worksheet, row, headerMap, "SecondaryClientEmail");
            if (sessionType == SessionType.SemiPrivate)
            {
                if (string.IsNullOrWhiteSpace(secondaryClientEmail))
                {
                    throw new InvalidOperationException($"Row {row}: semi-private sessions require SecondaryClientEmail.");
                }

                session.SessionClients.Add(BuildImportedParticipant(
                    worksheet, row, headerMap, clientIds, "Secondary", 2, CopyAccessories(importedAccessories)));
            }

            var exerciseNames = GetCellText(worksheet, row, headerMap, "Exercises")
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var actionOwners = session.SessionClients
                .Where(sc => sc.ParticipantOrder == 1 || sessionType == SessionType.SemiPrivate)
                .ToList();

            foreach (var exerciseName in exerciseNames.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var normalized = exerciseName.Trim().ToUpperInvariant();
                if (!exerciseIds.TryGetValue(normalized, out var exerciseId))
                {
                    throw new InvalidOperationException($"Row {row}: exercise '{exerciseName}' was not found.");
                }

                foreach (var actionOwner in actionOwners)
                {
                    actionOwner.Actions.Add(new ModelAction
                    {
                        ExerciseID = exerciseId,
                        Springs = string.Empty,
                        Notes = string.Empty
                    });
                }
            }

            return session;
        }

        private SessionClient BuildImportedParticipant(
            ExcelWorksheet worksheet,
            int row,
            IReadOnlyDictionary<string, int> headerMap,
            IReadOnlyDictionary<string, int> clientIds,
            string prefix,
            int order,
            Accessories? accessories)
        {
            var clientName = GetRequiredCell(worksheet, row, headerMap, $"{prefix}ClientName");
            var clientEmail = GetRequiredCell(worksheet, row, headerMap, $"{prefix}ClientEmail");
            var clientId = ResolveLookup(clientIds, clientEmail, "client", row);

            // For data integrity, we verify that the provided client name matches the name on record for the given email. 
            var clientInDb = _context.Clients
                .Where(c => c.ID == clientId)
                .Select(c => new { c.FirstName, c.LastName })
                .FirstOrDefault();

            if (clientInDb != null)
            {
                var expectedFullName = $"{clientInDb.FirstName} {clientInDb.LastName}".Trim();
                var providedName = clientName.Trim();
                if (!string.Equals(expectedFullName, providedName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Row {row}: {prefix}ClientName '{providedName}' does not match the name on record for '{clientEmail}' (expected '{expectedFullName}').");
                }
            }

            return new SessionClient
            {
                ClientID = clientId,
                ParticipantOrder = order,
                SessionsPerWeekRecommended = ParseInt(GetRequiredCell(worksheet, row, headerMap, $"{prefix}SessionsPerWeek"), $"{prefix}SessionsPerWeek", row),
                SessionNotes = new SessionNotes
                {
                    Goals = GetRequiredCell(worksheet, row, headerMap, $"{prefix}Goals"),
                    GeneralComments = NullIfWhiteSpace(GetCellText(worksheet, row, headerMap, $"{prefix}GeneralComments")),
                    SubjectiveReports = GetRequiredCell(worksheet, row, headerMap, $"{prefix}SubjectiveReports"),
                    ObjectiveFindings = GetRequiredCell(worksheet, row, headerMap, $"{prefix}ObjectiveFindings"),
                    Plan = GetRequiredCell(worksheet, row, headerMap, $"{prefix}Plan")
                },
                NextSteps = new NextSteps
                {
                    NextAppointmentBooked = ParseBool(GetCellText(worksheet, row, headerMap, $"{prefix}NextAppointmentBooked")),
                    CommunicatedProgress = ParseBool(GetCellText(worksheet, row, headerMap, $"{prefix}CommunicatedProgress")),
                    ReadyToProgress = ParseBool(GetCellText(worksheet, row, headerMap, $"{prefix}ReadyToProgress")),
                    CourseCorrectionNeeded = ParseBool(GetCellText(worksheet, row, headerMap, $"{prefix}CourseCorrectionNeeded")),
                    TeamConsult = ParseBool(GetCellText(worksheet, row, headerMap, $"{prefix}TeamConsult")),
                    ReferredExternally = ParseBool(GetCellText(worksheet, row, headerMap, $"{prefix}ReferredExternally")),
                    ReferredTo = NullIfWhiteSpace(GetCellText(worksheet, row, headerMap, $"{prefix}ReferredTo"))
                },
                AdminComplete = new AdminComplete
                {
                    IsPaid = ParseBool(GetCellText(worksheet, row, headerMap, $"{prefix}IsPaid")),
                    AdminNotes = NullIfWhiteSpace(GetCellText(worksheet, row, headerMap, $"{prefix}AdminNotes")),
                    AdminInitials = NullIfWhiteSpace(GetCellText(worksheet, row, headerMap, $"{prefix}AdminInitials"))
                },
                Accessories = accessories
            };
        }

        private Accessories BuildImportedAccessories(ExcelWorksheet worksheet, int row, IReadOnlyDictionary<string, int> headerMap)
        {
            return new Accessories
            {
                HeadPad = ParseOptionalEnum(GetCellText(worksheet, row, headerMap, "AccessoriesHeadPad"), HeadPadOption.Down),
                StrapsOrHandles = ParseOptionalEnum(GetCellText(worksheet, row, headerMap, "AccessoriesStrapsOrHandles"), StrapOrHandleOption.Straps),
                GearBar = ParseOptionalInt(GetCellText(worksheet, row, headerMap, "AccessoriesGearBar")) ?? 0,
                StopperSettings = ParseOptionalInt(GetCellText(worksheet, row, headerMap, "AccessoriesStopperSettings")) ?? 0,
                RubberPads = ParseBool(GetCellText(worksheet, row, headerMap, "AccessoriesRubberPads")),
                HeadRest = ParseBool(GetCellText(worksheet, row, headerMap, "AccessoriesHeadRest")),
                Towel = ParseBool(GetCellText(worksheet, row, headerMap, "AccessoriesTowel")),
                PosturePillow = ParseBool(GetCellText(worksheet, row, headerMap, "AccessoriesPosturePillow"))
            };
        }

        private PhysioInfo? BuildImportedPhysioInfo(ExcelWorksheet worksheet, int row, IReadOnlyDictionary<string, int> headerMap)
        {
            var physioInfo = new PhysioInfo
            {
                PhysioAssessment = NullIfWhiteSpace(GetCellText(worksheet, row, headerMap, "PhysioAssessment")),
                InsuranceCompany = NullIfWhiteSpace(GetCellText(worksheet, row, headerMap, "InsuranceCompany")),
                CoverageAmountPerYear = ParseOptionalDecimal(GetCellText(worksheet, row, headerMap, "CoverageAmountPerYear")),
                AmountUsed = ParseOptionalDecimal(GetCellText(worksheet, row, headerMap, "AmountUsed")),
                CoverageResetsDate = ParseOptionalDate(GetCellText(worksheet, row, headerMap, "CoverageResetsDate")),
                PhysiotherapistName = NullIfWhiteSpace(GetCellText(worksheet, row, headerMap, "PhysiotherapistName")),
                CoverageShared = ParseBool(GetCellText(worksheet, row, headerMap, "CoverageShared")),
                CommunicatedWithPhysio = ParseBool(GetCellText(worksheet, row, headerMap, "CommunicatedWithPhysio"))
            };

            return HasMeaningfulPhysioInfo(physioInfo) ? physioInfo : null;
        }

        private static string GetRequiredCell(ExcelWorksheet worksheet, int row, IReadOnlyDictionary<string, int> headerMap, string header)
        {
            var value = GetCellText(worksheet, row, headerMap, header);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Row {row}: {header} is required.");
            }

            return value;
        }

        private static int ResolveLookup(IReadOnlyDictionary<string, int> lookup, string key, string label, int row)
        {
            var normalized = key.Trim().ToUpperInvariant();
            if (!lookup.TryGetValue(normalized, out var id))
            {
                throw new InvalidOperationException($"Row {row}: {label} '{key}' was not found.");
            }

            return id;
        }

        private static DateTime ParseDate(string value, string header, int row)
        {
            if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date))
            {
                throw new InvalidOperationException($"Row {row}: {header} must be a valid date.");
            }

            return date.Date;
        }

        private static DateTime? ParseOptionalDate(string value)
        {
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date)
                ? date.Date
                : null;
        }

        private static int ParseInt(string value, string header, int row)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                throw new InvalidOperationException($"Row {row}: {header} must be a whole number.");
            }

            return parsed;
        }

        private static int? ParseOptionalInt(string value)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
        }

        private static decimal? ParseOptionalDecimal(string value)
        {
            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
        }

        private static bool ParseBool(string value)
        {
            return value.Trim().ToUpperInvariant() switch
            {
                "TRUE" or "YES" or "Y" or "1" => true,
                _ => false
            };
        }

        private static TEnum ParseEnum<TEnum>(string value, string header, int row) where TEnum : struct
        {
            if (!Enum.TryParse<TEnum>(value, true, out var parsed))
            {
                throw new InvalidOperationException($"Row {row}: {header} value '{value}' is not valid.");
            }

            return parsed;
        }

        private static TEnum ParseOptionalEnum<TEnum>(string value, TEnum defaultValue) where TEnum : struct
        {
            return Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : defaultValue;
        }


        private static string? NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        [HttpGet]
        public async Task<IActionResult> GetExercisesByApparatus(int? apparatusId)
        {
            var query = _context.Exercises.OrderBy(e => e.ExerciseName).AsQueryable();
            if (apparatusId.HasValue)
                query = query.Where(e => e.ApparatusID == apparatusId);
            var list = await query.Select(e => new { id = e.ID, text = e.ExerciseName }).ToListAsync();
            return Json(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateExerciseAjax(string exerciseName)
        {
            if (string.IsNullOrWhiteSpace(exerciseName))
            {
                return BadRequest("Exercise name is required");
            }

            var normalizedName = exerciseName.Trim();
            var existingExercise = await _context.Exercises
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.ExerciseName.ToLower() == normalizedName.ToLower());

            if (existingExercise != null)
            {
                return Json(new { id = existingExercise.ID, name = existingExercise.ExerciseName, displayName = existingExercise.ExerciseName });
            }

            var exercise = new Exercise
            {
                ExerciseName = normalizedName,
                ApparatusID = null
            };

            _context.Exercises.Add(exercise);
            await _context.SaveChangesAsync();

            return Json(new { id = exercise.ID, name = exercise.ExerciseName, displayName = exercise.ExerciseName });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateApparatusAjax(string apparatusName)
        {
            if (string.IsNullOrWhiteSpace(apparatusName))
            {
                return BadRequest("Apparatus name is required");
            }

            var apparatus = new Apparatus
            {
                ApparatusName = apparatusName.Trim()
            };

            _context.Apparatuses.Add(apparatus);
            await _context.SaveChangesAsync();

            return Json(new { id = apparatus.ID, name = apparatus.ApparatusName });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePropAjax(string propName)
        {
            if (string.IsNullOrWhiteSpace(propName))
            {
                return BadRequest("Prop name is required");
            }

            var prop = new Prop
            {
                PropName = propName.Trim()
            };

            _context.Props.Add(prop);
            await _context.SaveChangesAsync();

            return Json(new { id = prop.ID, name = prop.PropName });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSpringAjax(string springName)
        {
            if (string.IsNullOrWhiteSpace(springName))
            {
                return BadRequest("Spring name is required");
            }

            var normalizedName = springName.Trim();
            var existingSpring = await _context.Springs
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SpringName.ToLower() == normalizedName.ToLower());

            if (existingSpring != null)
            {
                return Json(new { id = existingSpring.ID, name = existingSpring.SpringName, apparatusID = existingSpring.ApparatusID });
            }

            const string springApparatusName = "Springs (all unique settings)";
            var springApparatus = await _context.Apparatuses
                .FirstOrDefaultAsync(a => a.ApparatusName.ToLower() == springApparatusName.ToLower());

            if (springApparatus == null)
            {
                springApparatus = new Apparatus { ApparatusName = springApparatusName };
                _context.Apparatuses.Add(springApparatus);
                await _context.SaveChangesAsync();
            }

            var spring = new Spring
            {
                SpringName = normalizedName,
                ApparatusID = springApparatus.ID
            };

            _context.Springs.Add(spring);
            await _context.SaveChangesAsync();

            return Json(new { id = spring.ID, name = spring.SpringName, apparatusID = spring.ApparatusID });
        }
    }
}
