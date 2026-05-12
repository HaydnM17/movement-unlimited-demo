using Microsoft.EntityFrameworkCore;
using MU5PrototypeProject.Models;
using System.Diagnostics;
using ModelAction = MU5PrototypeProject.Models.Action;
using Microsoft.Data.Sqlite;

namespace MU5PrototypeProject.Data
{
    public static class MUInitializer
    {
        private const string InitialMigrationId = "20260406052254_Initial";

        public static void Initialize(
            IServiceProvider serviceProvider,
            bool deleteDatabase = false,
            bool useMigrations = true,
            bool seedSampleData = true)
        {
            using var context = new MUContext(
                serviceProvider.GetRequiredService<DbContextOptions<MUContext>>());

            try
            {
                if (deleteDatabase || !context.Database.CanConnect())
                {
                    if (useMigrations)
                    {
                        BaselineInitialMigrationIfSchemaAlreadyExists(context);
                        context.Database.Migrate();
                    }
                    else
                    {
                        context.Database.EnsureCreated();
                    }
                }
                else if (useMigrations)
                {
                    BaselineInitialMigrationIfSchemaAlreadyExists(context);
                    context.Database.Migrate();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.GetBaseException().Message);
                throw;
            }

            if (!seedSampleData)
            {
                return;
            }

            using var transaction = context.Database.BeginTransaction();
            try
            {
                SeedClients(context);
                SeedTrainers(context);
                SeedApparatuses(context);
                SeedExercises(context);
                SeedProps(context);
                SeedSprings(context);
                SeedSessions(context);

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.WriteLine(ex.GetBaseException().Message);
                throw;
            }
        }

        private static void BaselineInitialMigrationIfSchemaAlreadyExists(MUContext context)
        {
            if (!context.Database.IsSqlite())
            {
                return;
            }

            var applied = context.Database.GetAppliedMigrations().ToList();
            if (applied.Contains(InitialMigrationId))
            {
                return;
            }

            var pending = context.Database.GetPendingMigrations().ToList();
            if (!pending.Contains(InitialMigrationId))
            {
                return;
            }

            if (!TableExists(context, "Apparatuses") || !TableExists(context, "__EFMigrationsHistory"))
            {
                return;
            }

            var productVersion = typeof(DbContext).Assembly.GetName().Version?.ToString() ?? "9.0.0";
            context.Database.ExecuteSqlRaw(
                "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ({0}, {1});",
                InitialMigrationId,
                productVersion);
        }

        private static bool TableExists(MUContext context, string tableName)
        {
            var connection = (SqliteConnection)context.Database.GetDbConnection();
            var wasClosed = connection.State != System.Data.ConnectionState.Open;

            if (wasClosed)
            {
                connection.Open();
            }

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=$name LIMIT 1;";
                command.Parameters.AddWithValue("$name", tableName);
                var result = command.ExecuteScalar();
                return result is not null;
            }
            finally
            {
                if (wasClosed)
                {
                    connection.Close();
                }
            }
        }

        private static void SeedClients(MUContext context)
        {
            if (context.Clients.Any()) return;

            context.Clients.AddRange(
                new Client { FirstName = "Ava", LastName = "Reed", DOB = new DateTime(1990, 5, 12), Phone = "2265550101", Email = "ava.reed@example.com", ClientFolderUrl = "https://files.movementunlimited.com/clients/ava-reed" },
                new Client { FirstName = "Noah", LastName = "Hughes", DOB = new DateTime(1985, 8, 23), Phone = "2895550102", Email = "noah.hughes@example.com", ClientFolderUrl = "https://files.movementunlimited.com/clients/noah-hughes" },
                new Client { FirstName = "Mia", LastName = "Turner", DOB = new DateTime(1992, 2, 14), Phone = "3435550103", Email = "mia.turner@example.com", ClientFolderUrl = "https://files.movementunlimited.com/clients/mia-turner" },
                new Client { FirstName = "Liam", LastName = "Baker", DOB = new DateTime(1988, 11, 2), Phone = "3655550104", Email = "liam.baker@example.com", ClientFolderUrl = "https://files.movementunlimited.com/clients/liam-baker" },
                new Client { FirstName = "Emma", LastName = "Cole", DOB = new DateTime(1995, 7, 7), Phone = "4165550105", Email = "emma.cole@example.com", ClientFolderUrl = "https://files.movementunlimited.com/clients/emma-cole" },
                new Client { FirstName = "Oliver", LastName = "Shaw", DOB = new DateTime(1983, 6, 9), Phone = "4375550106", Email = "oliver.shaw@example.com", ClientFolderUrl = "https://files.movementunlimited.com/clients/oliver-shaw" },
                new Client { FirstName = "Sophie", LastName = "Chen", DOB = new DateTime(1991, 3, 18), Phone = "5195550107", Email = "sophie.chen@example.com", ClientFolderUrl = "https://files.movementunlimited.com/clients/sophie-chen" },
                new Client { FirstName = "James", LastName = "Patel", DOB = new DateTime(1978, 12, 5), Phone = "6135550108", Email = "james.patel@example.com", ClientFolderUrl = "https://files.movementunlimited.com/clients/james-patel" },
                new Client { FirstName = "Chloe", LastName = "Martin", DOB = new DateTime(1999, 9, 22), Phone = "7055550109", Email = "chloe.martin@example.com", ClientFolderUrl = "https://files.movementunlimited.com/clients/chloe-martin" },
                new Client { FirstName = "Ethan", LastName = "Brooks", DOB = new DateTime(1986, 4, 30), Phone = "9055550110", Email = "ethan.brooks@example.com", ClientFolderUrl = "https://files.movementunlimited.com/clients/ethan-brooks" },
                new Client { FirstName = "Isabella", LastName = "Wong", DOB = new DateTime(1993, 1, 14), Phone = "6475550111", Email = "isabella.wong@example.com", ClientFolderUrl = "https://files.movementunlimited.com/clients/isabella-wong" },
                new Client { FirstName = "Lucas", LastName = "Silva", DOB = new DateTime(1980, 7, 19), Phone = "5145550112", Email = "lucas.silva@example.com", ClientFolderUrl = "https://files.movementunlimited.com/clients/lucas-silva" }
            );

            context.SaveChanges();
        }

        private static void SeedTrainers(MUContext context)
        {
            if (context.Trainers.Any()) return;

            context.Trainers.AddRange(
                new Trainer { FirstName = "Alex", LastName = "Green", Email = "alex@mu.com", Role = "Trainer", IsActive = true },
                new Trainer { FirstName = "Jordan", LastName = "Lee", Email = "jordan@mu.com", Role = "Admin", IsActive = true },
                new Trainer { FirstName = "Morgan", LastName = "Park", Email = "morgan@mu.com", Role = "Physio", IsActive = true },
                new Trainer { FirstName = "Taylor", LastName = "Kim", Email = "taylor@mu.com", Role = "Trainer", IsActive = true }
            );

            context.SaveChanges();
        }

        private static void SeedApparatuses(MUContext context)
        {
            string[] presetApparatuses =
            {
                "Mat",
                "Reformer",
                "Cadillac (Trapeze Table)",
                "Chair",
                "Barrels"
            };

            var existingNames = context.Apparatuses
                .Select(a => a.ApparatusName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var apparatusesToAdd = presetApparatuses
                .Where(name => !existingNames.Contains(name))
                .Select(name => new Apparatus { ApparatusName = name })
                .ToList();

            if (apparatusesToAdd.Count == 0)
            {
                return;
            }

            context.Apparatuses.AddRange(apparatusesToAdd);
            context.SaveChanges();
        }

        private static void SeedExercises(MUContext context)
        {
            var apparatus = context.Apparatuses
                .ToDictionary(a => a.ApparatusName, a => a.ID, StringComparer.OrdinalIgnoreCase);

            var matId = TryGetApparatusId(apparatus, "Mat");
            var reformerId = TryGetApparatusId(apparatus, "Reformer");
            var cadillacId = TryGetApparatusId(apparatus, "Cadillac (Trapeze Table)", "Cadillac");
            var chairId = TryGetApparatusId(apparatus, "Chair");

            var presetExercises = new List<(string Name, int? ApparatusId)>
            {
                ("Pelvic Rocking", matId),
                ("Pelvic Clock", matId),
                ("Breathing Forward", matId),
                ("Supine Hip Folds", matId),
                ("Toe Taps", matId),
                ("Single Leg Extension", matId),
                ("Quadruped Hip Hinge", matId),
                ("Ab Prep", matId),
                ("Quadruped Abs", matId),
                ("Heel Squeeze Prone", matId),
                ("Spine Stretch Forward", matId),
                ("One Leg Circle", matId),
                ("Single Leg Stretch", matId),
                ("Double Leg Stretch", matId),
                ("Scissors", matId),
                ("Double Straight Leg Stretch", matId),
                ("Criss Cross", matId),
                ("One Leg Kick Prep", matId),
                ("Leg Pull Front Prep", matId),
                ("Side Kick Prep", matId),
                ("Half Roll Back", matId),

                ("Footwork", reformerId),
                ("Running / Prancing", reformerId),
                ("Feet in Straps", reformerId),
                ("Shoulder Bridge Prep", reformerId),
                ("Shoulder Bridge", reformerId),
                ("Back Rowing Preps", reformerId),
                ("Front Rowing Preps", reformerId),
                ("Supine Arm Series", reformerId),
                ("Hundred", reformerId),
                ("Lat Press", reformerId),
                ("Tricep Press", reformerId),
                ("Feet Pulling Straps (Hamstrings)", reformerId),
                ("Quadruped Abs Facing Front", reformerId),
                ("Elephant", reformerId),
                ("Knee Stretches", reformerId),
                ("Scooter", reformerId),
                ("Sleeper Series", reformerId),
                ("Side Split Series", reformerId),
                ("Front Splits Prep", reformerId),
                ("Hip Lift (Pelvic Lift)", reformerId),
                ("Arms Pulling Straps", reformerId),
                ("Side Arm Preps", reformerId),

                ("Push Through on Back", cadillacId),
                ("Push Through on Back with Roll Up and Bridge", cadillacId),
                ("Lat Pull", cadillacId),
                ("Prone Scapular Isolations", cadillacId),
                ("Swan Dive Prep", cadillacId),
                ("Breaststroke Preps", cadillacId),
                ("Supine Leg Springs", cadillacId),
                ("Roll Down with Bar", cadillacId),
                ("Airplane Prep", cadillacId),
                ("Assisted Bridge", cadillacId),
                ("Side Lying Leg Springs", cadillacId),
                ("Arm Springs Standing", cadillacId),
                ("Ballet Stretches", cadillacId),

                ("Ankle Exercise (Achilles Stretch)", chairId),
                ("Prone Chest Press", chairId),
                ("Prone on Chair", chairId),
                ("Hamstring Press (Hips Down)", chairId),
                ("Seated Hamstrings", chairId),
                ("Single Leg Sitting", chairId),
                ("Foot Press on Long Box", chairId),
                ("Cross Over Press", chairId),
                ("Single Leg Pump", chairId),
                ("Frog (Lying Flat)", chairId),
                ("Adductor Press", chairId),
                ("One Arm Push (Prep)", chairId),
                ("Triceps Press Sitting", chairId)
            };

            var existingExerciseNames = context.Exercises
                .Select(e => e.ExerciseName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var exercisesToAdd = new List<Exercise>();
            foreach (var exercise in presetExercises)
            {
                if (existingExerciseNames.Contains(exercise.Name))
                {
                    continue;
                }

                exercisesToAdd.Add(new Exercise
                {
                    ExerciseName = exercise.Name,
                    ApparatusID = exercise.ApparatusId
                });

                existingExerciseNames.Add(exercise.Name);
            }

            if (exercisesToAdd.Count == 0)
            {
                return;
            }

            context.Exercises.AddRange(exercisesToAdd);
            context.SaveChanges();
        }

        private static void SeedProps(MUContext context)
        {
            if (context.Props.Any()) return;

            context.Props.AddRange(
                new Prop { PropName = "Circle" },
                new Prop { PropName = "Foam Roller" },
                new Prop { PropName = "Long Box" },
                new Prop { PropName = "Mini Band" },
                new Prop { PropName = "Towel" }
            );

            context.SaveChanges();
        }

        private static void SeedSprings(MUContext context)
        {
            var apparatus = context.Apparatuses
                .ToDictionary(a => a.ApparatusName, a => a.ID, StringComparer.OrdinalIgnoreCase);

            var reformerId = TryGetApparatusId(apparatus, "Reformer");
            var matId = TryGetApparatusId(apparatus, "Mat");
            var cadillacId = TryGetApparatusId(apparatus, "Cadillac (Trapeze Table)", "Cadillac");

            var presetSprings = new List<(string Name, int? ApparatusId, string? TensionLevel, string? Color)>
            {
                ("Red", reformerId, "Heavy", "Red"),
                ("Blue", reformerId, "Medium", "Blue"),
                ("Mat Resistance Band Light", matId, "Light", "Yellow"),
                ("Cadillac Red", cadillacId, "Heavy", "Red"),

                ("2R", reformerId, null, null),
                ("1R", reformerId, null, null),
                ("1B", reformerId, null, null),
                ("1W", reformerId, null, null),
                ("2H", cadillacId, null, null),
                ("1H", cadillacId, null, null),
                ("L", cadillacId, null, null),
                ("L1", cadillacId, null, null),
                ("L2", cadillacId, null, null),
                ("2L", cadillacId, null, null),
                ("T", cadillacId, null, null)
            };

            var existingSpringNames = context.Springs
                .Select(s => s.SpringName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var springsToAdd = new List<Spring>();
            foreach (var spring in presetSprings)
            {
                if (!spring.ApparatusId.HasValue || existingSpringNames.Contains(spring.Name))
                {
                    continue;
                }

                springsToAdd.Add(new Spring
                {
                    ApparatusID = spring.ApparatusId.Value,
                    SpringName = spring.Name,
                    TensionLevel = spring.TensionLevel,
                    Color = spring.Color
                });

                existingSpringNames.Add(spring.Name);
            }

            if (springsToAdd.Count == 0)
            {
                return;
            }

            context.Springs.AddRange(springsToAdd);
            context.SaveChanges();
        }

        private static void SeedSessions(MUContext context)
        {
            if (context.Sessions.Any()) return;

            var clients = context.Clients.ToDictionary(c => c.Email!, c => c.ID);
            var trainers = context.Trainers.ToDictionary(t => t.Email!, t => t.ID);
            var exercises = context.Exercises.ToDictionary(e => e.ExerciseName, e => e.ID, StringComparer.OrdinalIgnoreCase);
            var springs = context.Springs.ToDictionary(s => s.SpringName, s => s.ID, StringComparer.OrdinalIgnoreCase);

            // Shorthand references
            int ava = clients["ava.reed@example.com"];
            int noah = clients["noah.hughes@example.com"];
            int mia = clients["mia.turner@example.com"];
            int liam = clients["liam.baker@example.com"];
            int emma = clients["emma.cole@example.com"];
            int oliver = clients["oliver.shaw@example.com"];

            int sophie = clients["sophie.chen@example.com"];
            int james = clients["james.patel@example.com"];
            int chloe = clients["chloe.martin@example.com"];
            int ethan = clients["ethan.brooks@example.com"];
            int isabella = clients["isabella.wong@example.com"];
            int lucas = clients["lucas.silva@example.com"];

            int alex = trainers["alex@mu.com"];
            int jordan = trainers["jordan@mu.com"];
            int morgan = trainers["morgan@mu.com"];
            int taylor = trainers["taylor@mu.com"];

            int footwork = GetExistingId(exercises, "Footwork", "Reformer Footwork");
            int hundred = GetExistingId(exercises, "Hundred");
            int bridge = GetExistingId(exercises, "Shoulder Bridge Prep", "Shoulder Bridge", "Bridge");
            int spineStretch = GetExistingId(exercises, "Spine Stretch Forward", "Spine Stretch");
            int chairPumping = GetExistingId(exercises, "Single Leg Pump", "Chair Pumping");
            int legSprings = GetExistingId(exercises, "Supine Leg Springs", "Leg Springs");

            int redSpring = GetExistingId(springs, "Red", "2R");
            int blueSpring = GetExistingId(springs, "Blue", "1R, 1B");
            int matBand = GetExistingId(springs, "Mat Resistance Band Light", "1B");
            int cadillacRed = GetExistingId(springs, "Cadillac Red", "2H, L");

            var today = DateTime.Today;
            // Helper: ensure seed dates always fall on a weekday (Mon–Fri).
            // Saturday nudges to Friday, Sunday nudges to Monday.
            DateTime Weekday(int daysFromToday)
            {
                var d = today.AddDays(daysFromToday);
                if (d.DayOfWeek == DayOfWeek.Saturday) d = d.AddDays(-1);
                else if (d.DayOfWeek == DayOfWeek.Sunday) d = d.AddDays(1);
                return d;
            }

            DateTime UpcomingWednesdayApril22()
            {
                var year = today.Year;
                while (true)
                {
                    var candidate = new DateTime(year, 4, 22);
                    if (candidate > today && candidate.DayOfWeek == DayOfWeek.Wednesday)
                    {
                        return candidate;
                    }

                    year++;
                }
            }

            var aprilBlockStart = UpcomingWednesdayApril22();

            var sessions = new List<Session>
            {
                // ═══════════════════════════════════════════════════════════
                // AVA REED — 6 private sessions with Alex over 6 weeks
                // Shoulder rehab progression
                // ═══════════════════════════════════════════════════════════
                BuildPrivateSession(
                    Weekday(-42), alex, ava, 2,
                    "Initial assessment — shoulder pain and reduced ROM.",
                    "New client intake. Explain apparatus safety and session flow.",
                    "5/10 shoulder pain after overhead reaching. Pain worsens at end of day.",
                    "Left shoulder flexion limited to 140 degrees. Mild scapular winging.",
                    "Begin with gentle mobility and core activation. Avoid overhead load.",
                    BuildAdmin(true, "Intake paperwork complete. Insurance info on file.", "AG"),
                    BuildAccessories(redSpring, HeadPadOption.Full, StrapOrHandleOption.Handles, 2, 3, true, true, false, false),
                    new[] {
                        BuildAction(footwork, "2R", "Light load. Focus on breath and alignment."),
                        BuildAction(bridge, "", "Supine only. No shoulder weight bearing.")
                    }, SessionStatus.Completed),

                BuildPrivateSession(
                    Weekday(-35), alex, ava, 2,
                    "Build scapular stability and continue mobility work.",
                    "Client reports less pain after first session.",
                    "4/10 shoulder pain. Sleeping better on left side.",
                    "Left shoulder flexion improved to 150 degrees. Less winging.",
                    "Add light resistance scapular exercises. Continue mobility.",
                    BuildAdmin(true, "Payment received.", "AG"),
                    BuildAccessories(redSpring, HeadPadOption.Full, StrapOrHandleOption.Handles, 2, 3, true, true, false, false),
                    new[] {
                        BuildAction(footwork, "2R", "Smooth tempo, 3 sets of 10."),
                        BuildAction(hundred, "1R1B", "Modified position. Short set with breathing emphasis."),
                        BuildAction(spineStretch, "", "Seated. Focus on thoracic extension.")
                    }, SessionStatus.Completed),

                BuildPrivateSession(
                    Weekday(-28), alex, ava, 2,
                    "Progress shoulder loading. Introduce reformer arm work.",
                    "Good compliance with home exercises.",
                    "3/10 shoulder pain only with heavy lifting. No night pain.",
                    "Left shoulder flexion 160 degrees. Scapular control improving.",
                    "Add arm springs at light resistance. Progress footwork load.",
                    BuildAdmin(true, "Paid.", "AG"),
                    BuildAccessories(blueSpring, HeadPadOption.Full, StrapOrHandleOption.Handles, 2, 4, true, true, false, false),
                    new[] {
                        BuildAction(footwork, "2R1B", "Increased spring. Monitor form."),
                        BuildAction(hundred, "1R1B", "Full set. Good breathing pattern."),
                        BuildAction(bridge, "", "Added marching variation.")
                    }, SessionStatus.Completed),

                BuildPrivateSession(
                    Weekday(-21), alex, ava, 2,
                    "Continue progressive loading. Test overhead tolerance.",
                    "Client feeling motivated. Returning to light gym work.",
                    "2/10 shoulder pain with overhead press only.",
                    "Near full ROM. Good scapulohumeral rhythm.",
                    "Begin overhead exercises at light load. Monitor for flare-ups.",
                    BuildAdmin(true, "Paid.", "AG"),
                    BuildAccessories(redSpring, HeadPadOption.Middle, StrapOrHandleOption.Handles, 2, 4, true, true, false, false),
                    new[] {
                        BuildAction(footwork, "2R1B", "Progressed to single leg variation."),
                        BuildAction(hundred, "2R", "Full hundred. Strong."),
                        BuildAction(spineStretch, "", "Added rotation component.")
                    }, SessionStatus.Completed),

                BuildPrivateSession(
                    Weekday(-14), alex, ava, 2,
                    "Functional movement integration. Prepare for discharge.",
                    "Almost pain free. Very pleased with progress.",
                    "1/10 shoulder pain with heavy overhead only.",
                    "Full ROM bilateral. Strength 4+/5 all planes.",
                    "Focus on functional patterns. Plan for independent program.",
                    BuildAdmin(true, "Paid.", "AG"),
                    BuildAccessories(redSpring, HeadPadOption.Middle, StrapOrHandleOption.Straps, 3, 5, true, true, false, true),
                    new[] {
                        BuildAction(footwork, "3R", "Full load. Excellent form."),
                        BuildAction(hundred, "2R", "Full set, no modifications needed."),
                        BuildAction(chairPumping, "", "New exercise. Good tolerance.")
                    }, SessionStatus.Completed),

                BuildPrivateSession(
                    Weekday(-7), alex, ava, 2,
                    "Final supervised session. Review home program.",
                    "Discharge session. Client will continue independently.",
                    "0/10 pain. Full function restored.",
                    "Full ROM and strength. All functional tests passed.",
                    "Transition to maintenance. Home program provided.",
                    BuildAdmin(true, "Final session paid. Package complete.", "AG"),
                    BuildAccessories(redSpring, HeadPadOption.Middle, StrapOrHandleOption.Straps, 3, 5, true, true, false, true),
                    new[] {
                        BuildAction(footwork, "3R", "Independent performance. Excellent."),
                        BuildAction(hundred, "2R", "Strong and controlled."),
                        BuildAction(chairPumping, "", "Good hip and knee control."),
                        BuildAction(bridge, "", "Single leg bridge. No issues.")
                    }, SessionStatus.Completed),

                // ═══════════════════════════════════════════════════════════
                // NOAH & MIA — 4 semi-private sessions with Jordan
                // Noah: knee rehab, Mia: posture and endurance
                // ═══════════════════════════════════════════════════════════
                BuildSemiPrivateSession(
                    Weekday(-40), jordan, noah, mia, 2, 1,
                    BuildNotes("Strengthen lower body post-knee surgery.", "First semi-private session with this pairing.", "Knee feels stiff in morning but loosens up.", "Good squat to parallel. Slight valgus on left.", "Focus on quad activation and proper tracking."),
                    BuildNotes("Improve posture and breathing capacity.", "First session. Referred by physiotherapist.", "Upper back tension from desk work.", "Forward head posture. Rounded shoulders.", "Begin with thoracic mobility and breathing drills."),
                    BuildAdmin(true, "Noah paid for 4-session package.", "JL"),
                    BuildAdmin(true, "Mia paid for 4-session package.", "JL"),
                    BuildAccessories(blueSpring, HeadPadOption.Down, StrapOrHandleOption.Straps, 1, 2, false, true, true, false),
                    new[] {
                        BuildAction(bridge, "", "Shared warm-up. Both clients."),
                        BuildAction(footwork, "1R1B", "Noah focus: quad activation. Mia focus: alignment."),
                        BuildAction(spineStretch, "", "Alternating cues for each client.")
                    }, SessionStatus.Completed),

                BuildSemiPrivateSession(
                    Weekday(-33), jordan, noah, mia, 2, 1,
                    BuildNotes("Progress knee strength. Add single leg work.", "Good rapport between clients.", "Knee feeling stronger. Less morning stiffness.", "Improved quad activation. Valgus correcting.", "Progress to single leg exercises next session."),
                    BuildNotes("Continue posture work. Add core endurance.", "Enjoying the sessions. More aware of posture at work.", "Less upper back tension this week.", "Slight improvement in head position.", "Add plank variations and breathing integration."),
                    BuildAdmin(true, "Package session 2/4.", "JL"),
                    BuildAdmin(true, "Package session 2/4.", "JL"),
                    BuildAccessories(blueSpring, HeadPadOption.Down, StrapOrHandleOption.Straps, 1, 3, false, true, true, false),
                    new[] {
                        BuildAction(bridge, "", "Single leg variation for Noah. Standard for Mia."),
                        BuildAction(footwork, "2R", "Increased load for both."),
                        BuildAction(hundred, "1R", "Core endurance focus.")
                    }, SessionStatus.Completed),

                BuildSemiPrivateSession(
                    Weekday(-26), jordan, noah, mia, 2, 1,
                    BuildNotes("Single leg strength and balance.", "Noah progressing faster than expected.", "No knee pain during single leg work.", "Single leg squat clean. Good balance.", "Continue progression. Consider step-ups next."),
                    BuildNotes("Core endurance and postural awareness.", "Mia more confident with exercises.", "Breathing patterns improved.", "Shoulders less rounded. Better mid-back extension.", "Add resistance to core work."),
                    BuildAdmin(true, "Package session 3/4.", "JL"),
                    BuildAdmin(true, "Package session 3/4.", "JL"),
                    BuildAccessories(redSpring, HeadPadOption.Down, StrapOrHandleOption.Straps, 2, 3, false, true, true, false),
                    new[] {
                        BuildAction(footwork, "2R1B", "Heavy day. Both clients tolerated well."),
                        BuildAction(chairPumping, "", "New for both. Focus on control."),
                        BuildAction(spineStretch, "", "Added rotation for Mia.")
                    }, SessionStatus.Completed),

                BuildSemiPrivateSession(
                    Weekday(-19), jordan, noah, mia, 2, 1,
                    BuildNotes("Final package session. Review progress.", "Excellent progress. Discussing renewal.", "Knee pain free for 2 weeks.", "Full depth squat with good mechanics. Single leg stable.", "Ready to progress to independent training with check-ins."),
                    BuildNotes("Posture and endurance review.", "Wants to continue. Booking next package.", "No upper back pain this week.", "Posture significantly improved. Head neutral.", "Maintain current program. Add variety."),
                    BuildAdmin(true, "Package session 4/4 complete. Renewal discussed.", "JL"),
                    BuildAdmin(true, "Package session 4/4 complete. Renewal booked.", "JL"),
                    BuildAccessories(redSpring, HeadPadOption.Middle, StrapOrHandleOption.Straps, 2, 3, true, true, true, false),
                    new[] {
                        BuildAction(footwork, "3R", "Max load test. Both strong."),
                        BuildAction(hundred, "2R", "Full set. Good endurance."),
                        BuildAction(bridge, "", "Single leg for both. Solid."),
                        BuildAction(chairPumping, "", "Good control from both clients.")
                    }, SessionStatus.Completed),

                // ═══════════════════════════════════════════════════════════
                // EMMA COLE — 4 physio sessions with Morgan
                // Rotator cuff rehab
                // ═══════════════════════════════════════════════════════════
                BuildPhysioSession(
                    Weekday(-33), morgan, emma, 3,
                    BuildNotes("Initial physio assessment. Rotator cuff strain.", "Referred by Dr. Patel. MVA 3 months ago.", "6/10 pain with reaching. Cannot sleep on right side.", "Reduced external rotation. Positive impingement tests.", "Begin gentle ROM and isometric strengthening."),
                    BuildAdmin(false, "Insurance claim submitted. Awaiting approval.", "MP"),
                    BuildAccessories(cadillacRed, HeadPadOption.Middle, StrapOrHandleOption.Handles, 0, 1, true, false, true, false),
                    new PhysioInfo
                    {
                        PhysioAssessment = "Right rotator cuff strain post-MVA. Reduced overhead tolerance and external rotation.",
                        InsuranceCompany = "Manulife",
                        CoverageAmountPerYear = 2000m,
                        AmountUsed = 0m,
                        CoverageResetsDate = new DateTime(today.Year + 1, 1, 1),
                        PhysiotherapistName = "Dr. Patel",
                        CoverageShared = true,
                        CommunicatedWithPhysio = true
                    },
                    new[] {
                        BuildAction(legSprings, "1R", "Gentle. Monitor pain response."),
                        BuildAction(bridge, "", "Supine core activation. No shoulder load.")
                    }, SessionStatus.Completed),

                BuildPhysioSession(
                    Weekday(-25), morgan, emma, 3,
                    BuildNotes("Progress ROM. Begin isotonic strengthening.", "Insurance approved. 12 sessions covered.", "5/10 pain. Sleeping slightly better.", "External rotation improved 10 degrees. Less guarding.", "Progress to light resistance band work."),
                    BuildAdmin(false, "Insurance approved. Claim processing.", "MP"),
                    BuildAccessories(cadillacRed, HeadPadOption.Middle, StrapOrHandleOption.Handles, 0, 1, true, false, true, false),
                    new PhysioInfo
                    {
                        PhysioAssessment = "Improving ROM. Pain decreasing. Ready for progressive loading.",
                        InsuranceCompany = "Manulife",
                        CoverageAmountPerYear = 2000m,
                        AmountUsed = 150m,
                        CoverageResetsDate = new DateTime(today.Year + 1, 1, 1),
                        PhysiotherapistName = "Dr. Patel",
                        CoverageShared = true,
                        CommunicatedWithPhysio = true
                    },
                    new[] {
                        BuildAction(legSprings, "1R", "Increased range. Good control."),
                        BuildAction(bridge, "", "Added arm reach variation."),
                        BuildAction(spineStretch, "", "Thoracic mobility to support shoulder.")
                    }, SessionStatus.Completed),

                BuildPhysioSession(
                    Weekday(-20), morgan, emma, 3,
                    BuildNotes("Continue strengthening. Add functional patterns.", "Dr. Patel pleased with progress at last check-in.", "4/10 pain with heavy lifting only. Daily function improved.", "External rotation near normal. Strength 4/5.", "Add overhead reach exercises at low load."),
                    BuildAdmin(false, "Third claim submitted.", "MP"),
                    BuildAccessories(cadillacRed, HeadPadOption.Middle, StrapOrHandleOption.Handles, 0, 2, true, false, true, false),
                    new PhysioInfo
                    {
                        PhysioAssessment = "Good progress. Transitioning to functional strengthening phase.",
                        InsuranceCompany = "Manulife",
                        CoverageAmountPerYear = 2000m,
                        AmountUsed = 300m,
                        CoverageResetsDate = new DateTime(today.Year + 1, 1, 1),
                        PhysiotherapistName = "Dr. Patel",
                        CoverageShared = true,
                        CommunicatedWithPhysio = false
                    },
                    new[] {
                        BuildAction(legSprings, "1R", "Full range now. Strong."),
                        BuildAction(hundred, "1R", "Modified arm position. Tolerated well."),
                        BuildAction(bridge, "", "Added resistance band.")
                    }, SessionStatus.Completed),

                BuildPhysioSession(
                    Weekday(-13), morgan, emma, 3,
                    BuildNotes("Functional integration and return-to-activity planning.", "Planning discharge in 2 more sessions.", "2/10 pain only with max effort overhead.", "Full ROM. Strength 4+/5. Functional tests passing.", "Continue progression. Begin discharge planning."),
                    BuildAdmin(false, "Fourth claim submitted.", "MP"),
                    BuildAccessories(cadillacRed, HeadPadOption.Full, StrapOrHandleOption.Handles, 1, 2, true, false, true, true),
                    new PhysioInfo
                    {
                        PhysioAssessment = "Near discharge. Functional goals mostly met. 2 sessions remaining.",
                        InsuranceCompany = "Manulife",
                        CoverageAmountPerYear = 2000m,
                        AmountUsed = 450m,
                        CoverageResetsDate = new DateTime(today.Year + 1, 1, 1),
                        PhysiotherapistName = "Dr. Patel",
                        CoverageShared = true,
                        CommunicatedWithPhysio = true
                    },
                    new[] {
                        BuildAction(legSprings, "1R", "Full program. Independent."),
                        BuildAction(hundred, "1R1B", "Progressed springs."),
                        BuildAction(chairPumping, "", "New exercise for functional transfer."),
                        BuildAction(bridge, "", "Single leg. Excellent.")
                    }, SessionStatus.Completed),

                // ═══════════════════════════════════════════════════════════
                // LIAM BAKER — 5 private sessions with Taylor
                // Desk worker: back stiffness and flexibility
                // ═══════════════════════════════════════════════════════════
                BuildPrivateSession(
                    Weekday(-32), taylor, liam, 1,
                    "Reduce back stiffness and improve flexibility.",
                    "Desk job 10+ hours/day. Very sedentary.",
                    "Constant dull ache in lower back. 4/10.",
                    "Tight hamstrings and hip flexors. Limited lumbar extension.",
                    "Begin with gentle mobility. Educate on posture and breaks.",
                    BuildAdmin(true, "Intake complete. Paid.", "TK"),
                    null,
                    new[] {
                        BuildAction(spineStretch, "", "Slow tempo. Focus on breathing."),
                        BuildAction(bridge, "", "Posterior chain activation.")
                    }, SessionStatus.Completed),

                BuildPrivateSession(
                    Weekday(-24), taylor, liam, 1,
                    "Continue mobility work. Add hip flexor stretching.",
                    "Taking more breaks at work. Using standing desk sometimes.",
                    "3/10 back ache. Better after walks.",
                    "Slight hamstring improvement. Hip flexors still tight.",
                    "Add hip flexor focused work. Continue mobility.",
                    BuildAdmin(true, "Paid.", "TK"),
                    BuildAccessories(blueSpring, HeadPadOption.Down, StrapOrHandleOption.Straps, 1, 2, false, false, true, false),
                    new[] {
                        BuildAction(spineStretch, "", "Improved range this week."),
                        BuildAction(bridge, "", "Added hip extension hold."),
                        BuildAction(footwork, "1R1B", "Light. Focus on hip mobility.")
                    }, SessionStatus.Completed),

                BuildPrivateSession(
                    Weekday(-17), taylor, liam, 1,
                    "Progress to light strengthening. Maintain mobility gains.",
                    "Bought a foam roller for home use.",
                    "2/10 back ache only after long sitting.",
                    "Hamstrings much improved. Hip flexors loosening.",
                    "Add reformer work for core and hip strength.",
                    BuildAdmin(true, "Paid.", "TK"),
                    BuildAccessories(blueSpring, HeadPadOption.Down, StrapOrHandleOption.Straps, 1, 3, false, false, true, false),
                    new[] {
                        BuildAction(footwork, "2R", "Good form. Increased load."),
                        BuildAction(spineStretch, "", "Excellent thoracic mobility now."),
                        BuildAction(bridge, "", "Single leg intro. Managed well.")
                    }, SessionStatus.Completed),

                BuildPrivateSession(
                    Weekday(-10), taylor, liam, 2,
                    "Build core endurance and functional hip strength.",
                    "Wants to increase to 2x/week. Feeling much better.",
                    "1/10 back ache rarely. Only after very long days.",
                    "Good flexibility gains. Core endurance improving.",
                    "Increase session frequency. Add more challenging exercises.",
                    BuildAdmin(true, "Paid. Upgraded to 2x/week package.", "TK"),
                    BuildAccessories(redSpring, HeadPadOption.Middle, StrapOrHandleOption.Handles, 2, 3, true, false, true, false),
                    new[] {
                        BuildAction(footwork, "2R1B", "Progressed again. Strong."),
                        BuildAction(hundred, "1R", "First time. Good breathing."),
                        BuildAction(bridge, "", "Single leg. Solid."),
                        BuildAction(chairPumping, "", "Introduced. Good hip control.")
                    }, SessionStatus.Completed),

                BuildPrivateSession(
                    Weekday(-5), taylor, liam, 2,
                    "Full program. Focus on endurance and independence.",
                    "Very consistent with home exercises.",
                    "No back pain this week.",
                    "Full flexibility. Good core strength. Posture improved.",
                    "Continue current program. Building toward independence.",
                    BuildAdmin(true, "Paid.", "TK"),
                    BuildAccessories(redSpring, HeadPadOption.Middle, StrapOrHandleOption.Handles, 2, 4, true, false, true, false),
                    new[] {
                        BuildAction(footwork, "3R", "Max spring. Excellent."),
                        BuildAction(hundred, "1R1B", "Full hundred. Strong."),
                        BuildAction(chairPumping, "", "Added tempo variation."),
                        BuildAction(spineStretch, "", "Maintenance. Full range.")
                    }, SessionStatus.Completed),

                // ═══════════════════════════════════════════════════════════
                // OLIVER SHAW — 3 private sessions with Alex
                // General fitness and stress relief
                // ═══════════════════════════════════════════════════════════
                BuildPrivateSession(
                    Weekday(-22), alex, oliver, 1,
                    "General fitness and stress management through movement.",
                    "High-stress job in finance. Looking for active recovery.",
                    "No specific pain. General muscle tension and fatigue.",
                    "Good baseline strength. Moderate flexibility. Shallow breathing pattern.",
                    "Full body movement with breathing focus. Keep it enjoyable.",
                    BuildAdmin(true, "Paid drop-in.", "AG"),
                    BuildAccessories(blueSpring, HeadPadOption.Down, StrapOrHandleOption.Straps, 1, 2, false, true, false, false),
                    new[] {
                        BuildAction(footwork, "2R", "Smooth flowing movement."),
                        BuildAction(hundred, "1R1B", "Breathing focus."),
                        BuildAction(spineStretch, "", "Really enjoyed this one.")
                    }, SessionStatus.Completed),

                BuildPrivateSession(
                    Weekday(-15), alex, oliver, 1,
                    "Continue full body work. Add variety.",
                    "Came in more relaxed this week. Said last session helped.",
                    "Less muscle tension. Slept better after last session.",
                    "Breathing pattern improved. Good engagement.",
                    "Add Cadillac work for variety. Continue stress relief focus.",
                    BuildAdmin(true, "Paid drop-in.", "AG"),
                    BuildAccessories(blueSpring, HeadPadOption.Middle, StrapOrHandleOption.Straps, 1, 3, false, true, false, false),
                    new[] {
                        BuildAction(footwork, "2R", "Warm-up. Relaxed pace."),
                        BuildAction(legSprings, "1R", "New for Oliver. Enjoyed it."),
                        BuildAction(bridge, "", "Cool-down stretch.")
                    }, SessionStatus.Completed),

                BuildPrivateSession(
                    Weekday(-8), alex, oliver, 2,
                    "Increasing engagement. Wants to come more often.",
                    "Wants to move to 2x/week. Feeling the benefits.",
                    "Noticeably less tense. Energy levels improved.",
                    "Good strength and flexibility progress. Breathing much improved.",
                    "Increase frequency. Mix reformer and mat for variety.",
                    BuildAdmin(true, "Paid. Booking weekly going forward.", "AG"),
                    BuildAccessories(redSpring, HeadPadOption.Middle, StrapOrHandleOption.Straps, 2, 3, true, true, false, false),
                    new[] {
                        BuildAction(footwork, "2R1B", "Progressed springs."),
                        BuildAction(hundred, "2R", "Full set. Controlled."),
                        BuildAction(legSprings, "1R", "Good pelvic control."),
                        BuildAction(spineStretch, "", "Favourite exercise.")
                    }, SessionStatus.Completed),

                // ═══════════════════════════════════════════════════════════
                // RECENT SESSIONS — Logged but not yet completed
                // (notes filled in, awaiting admin sign-off)
                // ═══════════════════════════════════════════════════════════
                BuildPrivateSession(
                    Weekday(-3), taylor, liam, 2,
                    "Core and hip strength progression.",
                    "Feeling great. No complaints.",
                    "Zero back pain. Full energy.",
                    "All exercises performed at full load. Excellent form.",
                    "Maintain current program. Add lateral work next.",
                    BuildAdmin(false, "Needs admin review.", ""),
                    BuildAccessories(redSpring, HeadPadOption.Middle, StrapOrHandleOption.Handles, 2, 4, true, false, true, false),
                    new[] {
                        BuildAction(footwork, "3R", "Full load. Perfect form."),
                        BuildAction(hundred, "2R", "Strong and controlled."),
                        BuildAction(chairPumping, "", "Added tempo variation."),
                        BuildAction(bridge, "", "Single leg with resistance band.")
                    }, SessionStatus.Logged),

                BuildPrivateSession(
                    Weekday(-2), alex, oliver, 2,
                    "Full body with emphasis on breathing and flow.",
                    "More relaxed than usual. Good session.",
                    "Feeling good. Less work stress this week.",
                    "Excellent movement quality. Breathing fully integrated.",
                    "Progress to more challenging variations.",
                    BuildAdmin(false, "Awaiting payment.", ""),
                    BuildAccessories(redSpring, HeadPadOption.Middle, StrapOrHandleOption.Straps, 2, 3, true, true, false, false),
                    new[] {
                        BuildAction(footwork, "2R1B", "Flowing tempo."),
                        BuildAction(hundred, "2R", "Full set. Breathing excellent."),
                        BuildAction(legSprings, "1R", "Good pelvic stability."),
                        BuildAction(spineStretch, "", "Deep stretch. Very relaxed.")
                    }, SessionStatus.Logged),

                // ═══════════════════════════════════════════════════════════
                // RECENT SESSIONS — Opened but not yet filled in
                // (session happened but trainer hasn't done notes yet)
                // ═══════════════════════════════════════════════════════════
                BuildPrivateSession(
                    Weekday(-1), alex, ava, 1,
                    "", "", "", "", "",
                    BuildAdmin(false, "", ""),
                    null,
                    Array.Empty<ModelAction>()),

                BuildSemiPrivateSession(
                    Weekday(-1), jordan, noah, mia, 2, 1,
                    BuildNotes("", "", "", "", ""),
                    BuildNotes("", "", "", "", ""),
                    BuildAdmin(false, "", ""),
                    BuildAdmin(false, "", ""),
                    null,
                    Array.Empty<ModelAction>()),

                // ═══════════════════════════════════════════════════════════
                // UPCOMING SESSIONS (future dates — booked, not started)
                // ═══════════════════════════════════════════════════════════
                BuildPrivateSession(
                    Weekday(1), alex, ava, 1,
                    "Maintenance check-in. Review home program.",
                    "Monthly follow-up after discharge.",
                    "", "", "Review and adjust home program as needed.",
                    BuildAdmin(false, "", ""),
                    BuildAccessories(redSpring, HeadPadOption.Middle, StrapOrHandleOption.Straps, 3, 5, true, true, false, true),
                    new[] {
                        BuildAction(footwork, "3R", ""),
                        BuildAction(hundred, "2R", "")
                    }),

                BuildSemiPrivateSession(
                    Weekday(2), jordan, noah, mia, 2, 1,
                    BuildNotes("New package session 1. Continue progression.", "", "", "", ""),
                    BuildNotes("New package session 1. Maintain gains.", "", "", "", ""),
                    BuildAdmin(false, "New 4-session package purchased.", "JL"),
                    BuildAdmin(false, "New 4-session package purchased.", "JL"),
                    BuildAccessories(redSpring, HeadPadOption.Middle, StrapOrHandleOption.Straps, 2, 3, true, true, true, false),
                    new[] {
                        BuildAction(footwork, "3R", ""),
                        BuildAction(hundred, "2R", ""),
                        BuildAction(chairPumping, "", "")
                    }),

                BuildPhysioSession(
                    Weekday(3), morgan, emma, 2,
                    BuildNotes("Discharge session. Final assessment and home exercise plan.", "Setup complete. Insurance verified for final session.", "", "", ""),
                    BuildAdmin(false, "Final session prepared. Discharge paperwork ready. Cadillac reserved.", "MP"),
                    BuildAccessories(cadillacRed, HeadPadOption.Full, StrapOrHandleOption.Handles, 1, 2, true, false, true, true),
                    new PhysioInfo
                    {
                        PhysioAssessment = "Ready for discharge. Functional goals met.",
                        InsuranceCompany = "Manulife",
                        CoverageAmountPerYear = 2000m,
                        AmountUsed = 600m,
                        CoverageResetsDate = new DateTime(today.Year + 1, 1, 1),
                        PhysiotherapistName = "Dr. Patel",
                        CoverageShared = true,
                        CommunicatedWithPhysio = true
                    },
                    new[] {
                        BuildAction(legSprings, "1R", ""),
                        BuildAction(hundred, "1R1B", ""),
                        BuildAction(bridge, "", "")
                    }),

                BuildPrivateSession(
                    Weekday(4), alex, ava, 2,
                    "Maintain shoulder strength and continue progressive loading.",
                    "Session setup complete. Equipment and paperwork prepared.",
                    "Pain managed well with current program.",
                    "Left shoulder flexion 170 degrees. Strong and stable.",
                    "Continue current program. Monitor for any regressions.",
                    BuildAdmin(true, "Setup complete. Insurance verified. Equipment ready: Red spring, handles.", "AG"),
                    BuildAccessories(redSpring, HeadPadOption.Full, StrapOrHandleOption.Handles, 2, 3, true, true, false, false),
                    new[] {
                        BuildAction(footwork, "2R", ""),
                        BuildAction(hundred, "1R1B", ""),
                        BuildAction(bridge, "", "")
                    }, SessionStatus.Opened),

                BuildSemiPrivateSession(
                    Weekday(5), jordan, noah, mia, 2, 1,
                    BuildNotes("Review progress and adjust programming.", "Setup scheduled. Both clients confirmed.", "", "", ""),
                    BuildNotes("Posture maintenance and continued progression.", "Setup scheduled. Both clients confirmed.", "", "", ""),
                    BuildAdmin(true, "Renewal packages paid for both clients. Session 1/4 prepared.", "JL"),
                    BuildAdmin(true, "Renewal packages paid for both clients. Session 1/4 prepared.", "JL"),
                    BuildAccessories(blueSpring, HeadPadOption.Down, StrapOrHandleOption.Straps, 1, 2, false, true, true, false),
                    new[] {
                        BuildAction(bridge, "", ""),
                        BuildAction(footwork, "2R", ""),
                        BuildAction(hundred, "1R", "")
                    }, SessionStatus.Opened),

                BuildPhysioSession(
                    Weekday(8), morgan, emma, 3,
                    BuildNotes("Discharge session. Final assessment and home exercise plan.", "Setup complete. Insurance verified for final session.", "", "", ""),
                    BuildAdmin(false, "Final session prepared. Discharge paperwork ready. Cadillac reserved.", "MP"),
                    BuildAccessories(cadillacRed, HeadPadOption.Full, StrapOrHandleOption.Handles, 1, 2, true, false, true, true),
                    new PhysioInfo
                    {
                        PhysioAssessment = "Ready for discharge. Functional goals met.",
                        InsuranceCompany = "Manulife",
                        CoverageAmountPerYear = 2000m,
                        AmountUsed = 600m,
                        CoverageResetsDate = new DateTime(today.Year + 1, 1, 1),
                        PhysiotherapistName = "Dr. Patel",
                        CoverageShared = true,
                        CommunicatedWithPhysio = true
                    },
                    new[] {
                        BuildAction(legSprings, "1R", ""),
                        BuildAction(hundred, "1R1B", ""),
                        BuildAction(bridge, "", "")
                    }, SessionStatus.Opened),

                // ═══════════════════════════════════════════════════════════
                // UPCOMING APRIL BLOCK — 3 sessions per day (all opened)
                // Wednesday April 22, Thursday April 23, Friday April 24
                // ═══════════════════════════════════════════════════════════
                BuildPrivateSession(
                    aprilBlockStart, alex, ava, 1,
                    "Maintenance and movement quality check-in.",
                    "Future booking.",
                    "", "", "",
                    BuildAdmin(false, "", ""),
                    BuildAccessories(redSpring, HeadPadOption.Middle, StrapOrHandleOption.Straps, 2, 4, true, true, false, false),
                    new[] {
                        BuildAction(footwork, "2R", ""),
                        BuildAction(hundred, "1R", "")
                    }, SessionStatus.Opened),

                BuildSemiPrivateSession(
                    aprilBlockStart, jordan, noah, mia, 2, 1,
                    BuildNotes("Continue package progression.", "Future booking.", "", "", ""),
                    BuildNotes("Posture and endurance progression.", "Future booking.", "", "", ""),
                    BuildAdmin(false, "", ""),
                    BuildAdmin(false, "", ""),
                    BuildAccessories(blueSpring, HeadPadOption.Down, StrapOrHandleOption.Straps, 1, 2, false, true, true, false),
                    new[] {
                        BuildAction(bridge, "", ""),
                        BuildAction(spineStretch, "", ""),
                        BuildAction(footwork, "2R", "")
                    }, SessionStatus.Opened),

                BuildPhysioSession(
                    aprilBlockStart, morgan, emma, 2,
                    BuildNotes("Follow-up rehab session.", "Future booking.", "", "", ""),
                    BuildAdmin(false, "", ""),
                    BuildAccessories(cadillacRed, HeadPadOption.Middle, StrapOrHandleOption.Handles, 1, 2, true, false, true, false),
                    new PhysioInfo
                    {
                        PhysioAssessment = "Ongoing rehab progression.",
                        InsuranceCompany = "Manulife",
                        CoverageAmountPerYear = 2000m,
                        AmountUsed = 750m,
                        CoverageResetsDate = new DateTime(aprilBlockStart.Year + 1, 1, 1),
                        PhysiotherapistName = "Dr. Patel",
                        CoverageShared = true,
                        CommunicatedWithPhysio = true
                    },
                    new[] {
                        BuildAction(legSprings, "1R", ""),
                        BuildAction(bridge, "", "")
                    }, SessionStatus.Opened),

                BuildPrivateSession(
                    aprilBlockStart.AddDays(1), taylor, liam, 2,
                    "Core and hip progression.",
                    "Future booking.",
                    "", "", "",
                    BuildAdmin(false, "", ""),
                    BuildAccessories(redSpring, HeadPadOption.Middle, StrapOrHandleOption.Handles, 2, 4, true, false, true, false),
                    new[] {
                        BuildAction(footwork, "2R1B", ""),
                        BuildAction(hundred, "1R", ""),
                        BuildAction(chairPumping, "", "")
                    }, SessionStatus.Opened),

                BuildSemiPrivateSession(
                    aprilBlockStart.AddDays(1), alex, chloe, ethan, 1, 1,
                    BuildNotes("Partner progression session.", "Future booking.", "", "", ""),
                    BuildNotes("Partner progression session.", "Future booking.", "", "", ""),
                    BuildAdmin(false, "", ""),
                    BuildAdmin(false, "", ""),
                    BuildAccessories(redSpring, HeadPadOption.Full, StrapOrHandleOption.Handles, 2, 4, true, true, false, true),
                    new[] {
                        BuildAction(footwork, "2R", ""),
                        BuildAction(hundred, "1R", ""),
                        BuildAction(bridge, "", "")
                    }, SessionStatus.Opened),

                BuildPhysioSession(
                    aprilBlockStart.AddDays(1), morgan, liam, 1,
                    BuildNotes("Post-op follow-up progression.", "Future booking.", "", "", ""),
                    BuildAdmin(false, "", ""),
                    BuildAccessories(cadillacRed, HeadPadOption.Full, StrapOrHandleOption.Handles, 1, 2, true, true, true, true),
                    new PhysioInfo
                    {
                        PhysioAssessment = "Post-op strength and ROM progression.",
                        InsuranceCompany = "Sun Life",
                        CoverageAmountPerYear = 1500m,
                        AmountUsed = 450m,
                        CoverageResetsDate = new DateTime(aprilBlockStart.Year + 1, 1, 1),
                        PhysiotherapistName = "Dr. Nguyen",
                        CoverageShared = false,
                        CommunicatedWithPhysio = true
                    },
                    new[] {
                        BuildAction(legSprings, "1R", ""),
                        BuildAction(spineStretch, "", "")
                    }, SessionStatus.Opened),

                BuildPrivateSession(
                    aprilBlockStart.AddDays(2), alex, sophie, 2,
                    "Initial baseline and movement profile.",
                    "Future booking.",
                    "", "", "",
                    BuildAdmin(false, "", ""),
                    BuildAccessories(redSpring, HeadPadOption.Middle, StrapOrHandleOption.Straps, 2, 4, true, true, false, false),
                    new[] {
                        BuildAction(footwork, "2R", ""),
                        BuildAction(hundred, "1R", "")
                    }, SessionStatus.Opened),

                BuildSemiPrivateSession(
                    aprilBlockStart.AddDays(2), jordan, noah, mia, 2, 1,
                    BuildNotes("Package progression session.", "Future booking.", "", "", ""),
                    BuildNotes("Package progression session.", "Future booking.", "", "", ""),
                    BuildAdmin(false, "", ""),
                    BuildAdmin(false, "", ""),
                    BuildAccessories(blueSpring, HeadPadOption.Down, StrapOrHandleOption.Handles, 2, 3, false, true, true, false),
                    new[] {
                        BuildAction(bridge, "", ""),
                        BuildAction(spineStretch, "", ""),
                        BuildAction(footwork, "2R", "")
                    }, SessionStatus.Opened),

                BuildPhysioSession(
                    aprilBlockStart.AddDays(2), morgan, emma, 2,
                    BuildNotes("Final progression before discharge planning.", "Future booking.", "", "", ""),
                    BuildAdmin(false, "", ""),
                    BuildAccessories(cadillacRed, HeadPadOption.Full, StrapOrHandleOption.Handles, 1, 2, true, false, true, true),
                    new PhysioInfo
                    {
                        PhysioAssessment = "Near discharge with strong functional outcomes.",
                        InsuranceCompany = "Manulife",
                        CoverageAmountPerYear = 2000m,
                        AmountUsed = 900m,
                        CoverageResetsDate = new DateTime(aprilBlockStart.Year + 1, 1, 1),
                        PhysiotherapistName = "Dr. Patel",
                        CoverageShared = true,
                        CommunicatedWithPhysio = true
                    },
                    new[] {
                        BuildAction(legSprings, "1R", ""),
                        BuildAction(hundred, "1R1B", ""),
                        BuildAction(bridge, "", "")
                    }, SessionStatus.Opened),

                // ═══════════════════════════════════════════════════════════
                // UPCOMING SESSIONS — 5+ weeks out
                // ═══════════════════════════════════════════════════════════
                BuildPrivateSession(
                    Weekday(35), alex, sophie, 2,
                    "Initial assessment. Build baseline strength profile.",
                    "New client intake — first session.",
                    "", "", "",
                    BuildAdmin(false, "", ""),
                    BuildAccessories(redSpring, HeadPadOption.Middle, StrapOrHandleOption.Straps, 2, 4, true, true, false, false),
                    new[] {
                        BuildAction(footwork, "2R", ""),
                        BuildAction(hundred, "1R", "")
                    }),

                BuildSemiPrivateSession(
                    Weekday(38), jordan, noah, mia, 2, 1,
                    BuildNotes("Package session 3. Progressive overload focus.", "", "", "", ""),
                    BuildNotes("Package session 3. Flexibility maintenance.", "", "", "", ""),
                    BuildAdmin(false, "Session 3/4 of current package.", "JL"),
                    BuildAdmin(false, "Session 3/4 of current package.", "JL"),
                    BuildAccessories(blueSpring, HeadPadOption.Down, StrapOrHandleOption.Handles, 2, 3, false, true, true, false),
                    new[] {
                        BuildAction(bridge, "", ""),
                        BuildAction(spineStretch, "", ""),
                        BuildAction(footwork, "2R", "")
                    }),

                BuildPhysioSession(
                    Weekday(42), morgan, liam, 1,
                    BuildNotes("Post-surgical follow-up. Assess ROM and strength recovery.", "Insurance pre-authorized. Equipment reserved.", "", "", ""),
                    BuildAdmin(false, "Surgical clearance letter on file. Cadillac booked.", "MP"),
                    BuildAccessories(cadillacRed, HeadPadOption.Full, StrapOrHandleOption.Handles, 1, 2, true, true, true, true),
                    new PhysioInfo
                    {
                        PhysioAssessment = "6-week post-op review. Cleared for progressive loading.",
                        InsuranceCompany = "Sun Life",
                        CoverageAmountPerYear = 1500m,
                        AmountUsed = 300m,
                        CoverageResetsDate = new DateTime(today.Year + 1, 1, 1),
                        PhysiotherapistName = "Dr. Nguyen",
                        CoverageShared = false,
                        CommunicatedWithPhysio = true
                    },
                    new[] {
                        BuildAction(legSprings, "1R", ""),
                        BuildAction(bridge, "", "")
                    }),

                BuildPrivateSession(
                    Weekday(45), taylor, james, 2,
                    "Core stability and posture correction program.",
                    "Referred by physiotherapist for ongoing strengthening.",
                    "", "", "",
                    BuildAdmin(false, "", ""),
                    BuildAccessories(blueSpring, HeadPadOption.Middle, StrapOrHandleOption.Straps, 3, 5, true, false, true, false),
                    new[] {
                        BuildAction(hundred, "2R", ""),
                        BuildAction(spineStretch, "", ""),
                        BuildAction(bridge, "", "")
                    }),

                BuildSemiPrivateSession(
                    Weekday(50), alex, chloe, ethan, 1, 1,
                    BuildNotes("Partner session 1. Establish movement baselines.", "", "", "", ""),
                    BuildNotes("Partner session 1. Focus on coordination and balance.", "", "", "", ""),
                    BuildAdmin(false, "New couple package — session 1/6.", "AG"),
                    BuildAdmin(false, "New couple package — session 1/6.", "AG"),
                    BuildAccessories(redSpring, HeadPadOption.Full, StrapOrHandleOption.Handles, 2, 4, true, true, false, true),
                    new[] {
                        BuildAction(footwork, "2R", ""),
                        BuildAction(hundred, "1R", ""),
                        BuildAction(chairPumping, "", "")
                    }),

                BuildPrivateSession(
                    Weekday(55), jordan, isabella, 2,
                    "Advanced reformer series. Challenge stability and control.",
                    "Long-term client — continuing advanced program.",
                    "", "", "",
                    BuildAdmin(false, "Monthly package renewal processed.", "JL"),
                    BuildAccessories(redSpring, HeadPadOption.Down, StrapOrHandleOption.Straps, 3, 5, false, true, true, false),
                    new[] {
                        BuildAction(footwork, "3R", ""),
                        BuildAction(hundred, "2R", ""),
                        BuildAction(spineStretch, "", ""),
                        BuildAction(bridge, "", "")
                    })
            };

            context.Sessions.AddRange(sessions);
            context.SaveChanges();
        }

        private static Session BuildPrivateSession(DateTime date, int trainerId, int clientId, int sessionsPerWeek, string goals, string? comments, string subjective, string objective, string plan, AdminComplete admin, Accessories? accessories, IEnumerable<ModelAction> actions, SessionStatus status = SessionStatus.Opened)
        {
            var session = new Session
            {
                SessionDate = date,
                SessionType = SessionType.Private,
                Status = status,
                TrainerID = trainerId
            };

            session.SessionClients.Add(new SessionClient
            {
                ClientID = clientId,
                ParticipantOrder = 1,
                SessionsPerWeekRecommended = sessionsPerWeek,
                SessionNotes = BuildNotes(goals, comments, subjective, objective, plan),
                NextSteps = BuildNextSteps(true, false, false, false, false, false, null),
                AdminComplete = admin,
                Accessories = accessories
            });

            AddActionsToParticipants(session, actions);
            return session;
        }

        private static Session BuildSemiPrivateSession(DateTime date, int trainerId, int client1Id, int client2Id, int client1Sessions, int client2Sessions, SessionNotes notes1, SessionNotes notes2, AdminComplete admin1, AdminComplete admin2, Accessories? accessories, IEnumerable<ModelAction> actions, SessionStatus status = SessionStatus.Opened)
        {
            var session = new Session
            {
                SessionDate = date,
                SessionType = SessionType.SemiPrivate,
                Status = status,
                TrainerID = trainerId
            };

            session.SessionClients.Add(new SessionClient { ClientID = client1Id, ParticipantOrder = 1, SessionsPerWeekRecommended = client1Sessions, SessionNotes = notes1, NextSteps = BuildNextSteps(true, true, true, false, false, false, null), AdminComplete = admin1, Accessories = accessories == null ? null : BuildAccessories(accessories.SpringID, accessories.HeadPad, accessories.StrapsOrHandles, accessories.GearBar, accessories.StopperSettings, accessories.RubberPads, accessories.HeadRest, accessories.Towel, accessories.PosturePillow) });
            session.SessionClients.Add(new SessionClient { ClientID = client2Id, ParticipantOrder = 2, SessionsPerWeekRecommended = client2Sessions, SessionNotes = notes2, NextSteps = BuildNextSteps(true, true, false, false, false, false, null), AdminComplete = admin2, Accessories = accessories == null ? null : BuildAccessories(accessories.SpringID, accessories.HeadPad, accessories.StrapsOrHandles, accessories.GearBar, accessories.StopperSettings, accessories.RubberPads, accessories.HeadRest, accessories.Towel, accessories.PosturePillow) });

            AddActionsToParticipants(session, actions);
            return session;
        }

        private static Session BuildPhysioSession(DateTime date, int trainerId, int clientId, int sessionsPerWeek, SessionNotes notes, AdminComplete admin, Accessories? accessories, PhysioInfo physioInfo, IEnumerable<ModelAction> actions, SessionStatus status = SessionStatus.Opened)
        {
            var session = new Session
            {
                SessionDate = date,
                SessionType = SessionType.Physio,
                Status = status,
                TrainerID = trainerId,
                PhysioInfo = physioInfo
            };

            session.SessionClients.Add(new SessionClient
            {
                ClientID = clientId,
                ParticipantOrder = 1,
                SessionsPerWeekRecommended = sessionsPerWeek,
                SessionNotes = notes,
                NextSteps = BuildNextSteps(true, true, false, true, true, false, null),
                AdminComplete = admin,
                Accessories = accessories
            });

            AddActionsToParticipants(session, actions);
            return session;
        }

        private static SessionNotes BuildNotes(string goals, string? comments, string subjective, string objective, string plan) =>
            new() { Goals = goals, GeneralComments = comments, SubjectiveReports = subjective, ObjectiveFindings = objective, Plan = plan };

        private static NextSteps BuildNextSteps(bool booked, bool progress, bool ready, bool correction, bool consult, bool referred, string? referredTo) =>
            new() { NextAppointmentBooked = booked, CommunicatedProgress = progress, ReadyToProgress = ready, CourseCorrectionNeeded = correction, TeamConsult = consult, ReferredExternally = referred, ReferredTo = referredTo };

        private static AdminComplete BuildAdmin(bool isPaid, string? notes, string? initials) =>
            new() { IsPaid = isPaid, AdminNotes = notes, AdminInitials = initials };

        private static Accessories BuildAccessories(int? springId, HeadPadOption headPad, StrapOrHandleOption strapsOrHandles, int gearBar, int stopperSettings, bool rubberPads, bool headRest, bool towel, bool posturePillow) =>
            new() { SpringID = springId, HeadPad = headPad, StrapsOrHandles = strapsOrHandles, GearBar = gearBar, StopperSettings = stopperSettings, RubberPads = rubberPads, HeadRest = headRest, Towel = towel, PosturePillow = posturePillow };

        private static ModelAction BuildAction(int exerciseId, string? springs, string? notes) =>
            new() { ExerciseID = exerciseId, Springs = springs ?? string.Empty, Notes = notes ?? string.Empty };

        private static int? TryGetApparatusId(IReadOnlyDictionary<string, int> apparatusMap, params string[] apparatusNames)
        {
            foreach (var name in apparatusNames)
            {
                if (apparatusMap.TryGetValue(name, out var id))
                {
                    return id;
                }
            }

            return null;
        }

        private static int GetExistingId(IReadOnlyDictionary<string, int> map, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (map.TryGetValue(key, out var id))
                {
                    return id;
                }
            }

            throw new InvalidOperationException($"None of the expected keys were found: {string.Join(", ", keys)}.");
        }

        private static void AddActionsToParticipants(Session session, IEnumerable<ModelAction> actions)
        {
            var actionTemplates = actions.ToList();
            if (actionTemplates.Count == 0)
            {
                return;
            }

            var actionOwners = session.SessionClients
                .Where(sc => sc.ParticipantOrder == 1 || session.SessionType == SessionType.SemiPrivate)
                .ToList();

            foreach (var actionOwner in actionOwners)
            {
                foreach (var action in actionTemplates)
                {
                    actionOwner.Actions.Add(new ModelAction
                    {
                        ExerciseID = action.ExerciseID,
                        ActionType = action.ActionType,
                        Springs = action.Springs,
                        Notes = action.Notes
                    });
                }
            }
        }
    }
}
