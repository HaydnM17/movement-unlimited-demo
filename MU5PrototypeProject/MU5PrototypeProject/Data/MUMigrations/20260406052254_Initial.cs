using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MU5PrototypeProject.Data.MUMigrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Apparatuses",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApparatusName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Apparatuses", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DOB = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    ClientFolderUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Props",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PropName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Props", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Trainers",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Role = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ApplicationUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trainers", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Exercises",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExerciseName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ApparatusID = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exercises", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Exercises_Apparatuses_ApparatusID",
                        column: x => x.ApparatusID,
                        principalTable: "Apparatuses",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Springs",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApparatusID = table.Column<int>(type: "INTEGER", nullable: false),
                    SpringName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TensionLevel = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Color = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Springs", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Springs_Apparatuses_ApparatusID",
                        column: x => x.ApparatusID,
                        principalTable: "Apparatuses",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    SessionType = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    TrainerID = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    UpdatedOn = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Sessions_Trainers_TrainerID",
                        column: x => x.TrainerID,
                        principalTable: "Trainers",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhysioInfos",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionID = table.Column<int>(type: "INTEGER", nullable: false),
                    PhysioAssessment = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    InsuranceCompany = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CoverageAmountPerYear = table.Column<decimal>(type: "TEXT", nullable: true),
                    AmountUsed = table.Column<decimal>(type: "TEXT", nullable: true),
                    CoverageResetsDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PhysiotherapistName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CoverageShared = table.Column<bool>(type: "INTEGER", nullable: false),
                    CommunicatedWithPhysio = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhysioInfos", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PhysioInfos_Sessions_SessionID",
                        column: x => x.SessionID,
                        principalTable: "Sessions",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionClients",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionID = table.Column<int>(type: "INTEGER", nullable: false),
                    ClientID = table.Column<int>(type: "INTEGER", nullable: false),
                    ParticipantOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    SessionsPerWeekRecommended = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionClients", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SessionClients_Clients_ClientID",
                        column: x => x.ClientID,
                        principalTable: "Clients",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionClients_Sessions_SessionID",
                        column: x => x.SessionID,
                        principalTable: "Sessions",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Accessories",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionClientID = table.Column<int>(type: "INTEGER", nullable: false),
                    HeadPad = table.Column<int>(type: "INTEGER", nullable: false),
                    StrapsOrHandles = table.Column<int>(type: "INTEGER", nullable: false),
                    GearBar = table.Column<int>(type: "INTEGER", nullable: false),
                    StopperSettings = table.Column<int>(type: "INTEGER", nullable: false),
                    RubberPads = table.Column<bool>(type: "INTEGER", nullable: false),
                    HeadRest = table.Column<bool>(type: "INTEGER", nullable: false),
                    Towel = table.Column<bool>(type: "INTEGER", nullable: false),
                    PosturePillow = table.Column<bool>(type: "INTEGER", nullable: false),
                    SpringID = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accessories", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Accessories_SessionClients_SessionClientID",
                        column: x => x.SessionClientID,
                        principalTable: "SessionClients",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Accessories_Springs_SpringID",
                        column: x => x.SpringID,
                        principalTable: "Springs",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "AdminCompletes",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionClientID = table.Column<int>(type: "INTEGER", nullable: false),
                    IsPaid = table.Column<bool>(type: "INTEGER", nullable: true),
                    AdminNotes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    AdminInitials = table.Column<string>(type: "TEXT", maxLength: 5, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminCompletes", x => x.ID);
                    table.ForeignKey(
                        name: "FK_AdminCompletes_SessionClients_SessionClientID",
                        column: x => x.SessionClientID,
                        principalTable: "SessionClients",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NextSteps",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionClientID = table.Column<int>(type: "INTEGER", nullable: false),
                    NextAppointmentBooked = table.Column<bool>(type: "INTEGER", nullable: false),
                    CommunicatedProgress = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReadyToProgress = table.Column<bool>(type: "INTEGER", nullable: false),
                    CourseCorrectionNeeded = table.Column<bool>(type: "INTEGER", nullable: false),
                    TeamConsult = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReferredExternally = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReferredTo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NextSteps", x => x.ID);
                    table.ForeignKey(
                        name: "FK_NextSteps_SessionClients_SessionClientID",
                        column: x => x.SessionClientID,
                        principalTable: "SessionClients",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionExercises",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionClientID = table.Column<int>(type: "INTEGER", nullable: false),
                    ExerciseID = table.Column<int>(type: "INTEGER", nullable: false),
                    ActionType = table.Column<int>(type: "INTEGER", nullable: false),
                    Springs = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionExercises", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SessionExercises_Exercises_ExerciseID",
                        column: x => x.ExerciseID,
                        principalTable: "Exercises",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionExercises_SessionClients_SessionClientID",
                        column: x => x.SessionClientID,
                        principalTable: "SessionClients",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionNotes",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionClientID = table.Column<int>(type: "INTEGER", nullable: false),
                    Goals = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    GeneralComments = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    SubjectiveReports = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ObjectiveFindings = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Plan = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CompletedByTrainerID = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionNotes", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SessionNotes_SessionClients_SessionClientID",
                        column: x => x.SessionClientID,
                        principalTable: "SessionClients",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SessionNotes_Trainers_CompletedByTrainerID",
                        column: x => x.CompletedByTrainerID,
                        principalTable: "Trainers",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "ExerciseProps",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActionID = table.Column<int>(type: "INTEGER", nullable: false),
                    PropID = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseProps", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ExerciseProps_Props_PropID",
                        column: x => x.PropID,
                        principalTable: "Props",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExerciseProps_SessionExercises_ActionID",
                        column: x => x.ActionID,
                        principalTable: "SessionExercises",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accessories_SessionClientID",
                table: "Accessories",
                column: "SessionClientID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accessories_SpringID",
                table: "Accessories",
                column: "SpringID");

            migrationBuilder.CreateIndex(
                name: "IX_AdminCompletes_SessionClientID",
                table: "AdminCompletes",
                column: "SessionClientID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseProps_ActionID",
                table: "ExerciseProps",
                column: "ActionID");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseProps_PropID",
                table: "ExerciseProps",
                column: "PropID");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_ApparatusID",
                table: "Exercises",
                column: "ApparatusID");

            migrationBuilder.CreateIndex(
                name: "IX_NextSteps_SessionClientID",
                table: "NextSteps",
                column: "SessionClientID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhysioInfos_SessionID",
                table: "PhysioInfos",
                column: "SessionID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionClients_ClientID",
                table: "SessionClients",
                column: "ClientID");

            migrationBuilder.CreateIndex(
                name: "IX_SessionClients_SessionID_ClientID",
                table: "SessionClients",
                columns: new[] { "SessionID", "ClientID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionClients_SessionID_ParticipantOrder",
                table: "SessionClients",
                columns: new[] { "SessionID", "ParticipantOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionExercises_ExerciseID",
                table: "SessionExercises",
                column: "ExerciseID");

            migrationBuilder.CreateIndex(
                name: "IX_SessionExercises_SessionClientID",
                table: "SessionExercises",
                column: "SessionClientID");

            migrationBuilder.CreateIndex(
                name: "IX_SessionNotes_CompletedByTrainerID",
                table: "SessionNotes",
                column: "CompletedByTrainerID");

            migrationBuilder.CreateIndex(
                name: "IX_SessionNotes_SessionClientID",
                table: "SessionNotes",
                column: "SessionClientID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_TrainerID",
                table: "Sessions",
                column: "TrainerID");

            migrationBuilder.CreateIndex(
                name: "IX_Springs_ApparatusID",
                table: "Springs",
                column: "ApparatusID");

            migrationBuilder.CreateIndex(
                name: "IX_Trainers_ApplicationUserId",
                table: "Trainers",
                column: "ApplicationUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accessories");

            migrationBuilder.DropTable(
                name: "AdminCompletes");

            migrationBuilder.DropTable(
                name: "ExerciseProps");

            migrationBuilder.DropTable(
                name: "NextSteps");

            migrationBuilder.DropTable(
                name: "PhysioInfos");

            migrationBuilder.DropTable(
                name: "SessionNotes");

            migrationBuilder.DropTable(
                name: "Springs");

            migrationBuilder.DropTable(
                name: "Props");

            migrationBuilder.DropTable(
                name: "SessionExercises");

            migrationBuilder.DropTable(
                name: "Exercises");

            migrationBuilder.DropTable(
                name: "SessionClients");

            migrationBuilder.DropTable(
                name: "Apparatuses");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "Trainers");
        }
    }
}
