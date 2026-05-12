using MU5PrototypeProject.Models;
using System.Security.Claims;

namespace MU5PrototypeProject.Security
{
    public static class SessionAuthorizationHelper
    {
        public static bool CanStartLoggedStage(ClaimsPrincipal user, int? currentTrainerId)
        {
            return user.IsInRole(AppRoles.Owner)
                || (user.IsInRole(AppRoles.Trainer) && currentTrainerId.HasValue);
        }

        public static bool HasMixedLoggedStageOwners(Session session)
        {
            return GetExplicitLoggedStageOwnerIds(session).Count > 1;
        }

        public static int? ResolveLoggedStageOwnerTrainerId(Session session)
        {
            var explicitOwnerIds = GetExplicitLoggedStageOwnerIds(session);
            if (explicitOwnerIds.Count == 1)
            {
                return explicitOwnerIds[0];
            }

            if (explicitOwnerIds.Count > 1)
            {
                return null;
            }

            return session.Status is SessionStatus.Logged or SessionStatus.Completed
                ? session.TrainerID
                : null;
        }

        public static bool CanEditLoggedStage(Session session, ClaimsPrincipal user, int? currentTrainerId)
        {
            if (user.IsInRole(AppRoles.Owner))
            {
                return true;
            }

            if (!user.IsInRole(AppRoles.Trainer) || !currentTrainerId.HasValue)
            {
                return false;
            }

            if (HasMixedLoggedStageOwners(session))
            {
                return false;
            }

            return ResolveLoggedStageOwnerTrainerId(session) == currentTrainerId.Value;
        }

        public static bool CanEditCompletedStage(Session session, ClaimsPrincipal user)
        {
            if (user.IsInRole(AppRoles.Owner))
            {
                return true;
            }

            if (HasMixedLoggedStageOwners(session))
            {
                return false;
            }

            return user.IsInRole(AppRoles.Administration);
        }

        public static bool CanOpenEditSession(Session session, ClaimsPrincipal user, int? currentTrainerId)
        {
            return session.Status switch
            {
                SessionStatus.Opened => user.IsInRole(AppRoles.Owner)
                    || user.IsInRole(AppRoles.Administration)
                    || user.IsInRole(AppRoles.Trainer),
                SessionStatus.Logged => CanEditLoggedStage(session, user, currentTrainerId)
                    || CanEditCompletedStage(session, user),
                SessionStatus.Completed => user.IsInRole(AppRoles.Owner)
                    || CanEditCompletedStage(session, user),
                _ => false
            };
        }

        public static bool ShouldDefaultToCompletedStage(Session session, ClaimsPrincipal user, int? currentTrainerId)
        {
            return session.Status == SessionStatus.Logged
                && !CanEditLoggedStage(session, user, currentTrainerId)
                && CanEditCompletedStage(session, user);
        }

        public static bool IsCancelEligible(Session session, DateTime today)
        {
            return !session.IsArchived
                && !session.IsCanceled
                && session.Status == SessionStatus.Opened
                && session.SessionDate.Date > today.Date;
        }

        public static bool CanCancelSession(Session session, ClaimsPrincipal user, DateTime today)
        {
            return IsCancelEligible(session, today)
                && (user.IsInRole(AppRoles.Owner) || user.IsInRole(AppRoles.Administration));
        }

        public static bool CanRestoreCanceledSession(Session session, ClaimsPrincipal user)
        {
            return session.IsCanceled
                && session.IsArchived
                && (user.IsInRole(AppRoles.Owner) || user.IsInRole(AppRoles.Administration));
        }

        private static List<int> GetExplicitLoggedStageOwnerIds(Session session)
        {
            return session.OrderedSessionClients
                .Select(sc => sc.SessionNotes?.CompletedByTrainerID)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();
        }
    }
}
