using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prisma.Api.Migrations
{
    /// <inheritdoc />
    public partial class DiagnosesAndSectorAssessments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "diagnoses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    AssessmentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Context = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "Draft"),
                    ExecutiveSummary = table.Column<string>(type: "text", nullable: true),
                    GeneralRecommendations = table.Column<string>(type: "text", nullable: true),
                    NextSteps = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_diagnoses", x => x.Id);
                    table.UniqueConstraint("ak_diagnoses_id_client_id", x => new { x.Id, x.ClientId });
                    table.ForeignKey(
                        name: "fk_diagnoses_clients_client_id",
                        column: x => x.ClientId,
                        principalTable: "clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "diagnosis_objectives",
                columns: table => new
                {
                    DiagnosisId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CustomDescription = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_diagnosis_objectives", x => new { x.DiagnosisId, x.Code });
                    table.ForeignKey(
                        name: "fk_diagnosis_objectives_diagnoses_diagnosis_id",
                        column: x => x.DiagnosisId,
                        principalTable: "diagnoses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sector_assessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiagnosisId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientSectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    SectorNameSnapshot = table.Column<string>(type: "text", nullable: false),
                    Answer01 = table.Column<string>(type: "text", nullable: true),
                    Answer02 = table.Column<string>(type: "text", nullable: true),
                    Answer03 = table.Column<string>(type: "text", nullable: true),
                    Answer04 = table.Column<string>(type: "text", nullable: true),
                    Answer05 = table.Column<string>(type: "text", nullable: true),
                    Answer06 = table.Column<string>(type: "text", nullable: true),
                    Answer07 = table.Column<string>(type: "text", nullable: true),
                    Answer08 = table.Column<string>(type: "text", nullable: true),
                    Answer09 = table.Column<string>(type: "text", nullable: true),
                    Answer10 = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sector_assessments", x => x.Id);
                    table.UniqueConstraint("ak_sector_assessments_id_diagnosis_id", x => new { x.Id, x.DiagnosisId });
                    table.ForeignKey(
                        name: "fk_sector_assessments_client_sectors_sector_client_id",
                        columns: x => new { x.ClientSectorId, x.ClientId },
                        principalTable: "client_sectors",
                        principalColumns: new[] { "Id", "ClientId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sector_assessments_diagnoses_diagnosis_client_id",
                        columns: x => new { x.DiagnosisId, x.ClientId },
                        principalTable: "diagnoses",
                        principalColumns: new[] { "Id", "ClientId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_diagnoses_ClientId",
                table: "diagnoses",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "ux_diagnosis_objectives_one_primary_per_diagnosis",
                table: "diagnosis_objectives",
                column: "DiagnosisId",
                unique: true,
                filter: "\"IsPrimary\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_sector_assessments_ClientSectorId_ClientId",
                table: "sector_assessments",
                columns: new[] { "ClientSectorId", "ClientId" });

            migrationBuilder.CreateIndex(
                name: "IX_sector_assessments_DiagnosisId_ClientId",
                table: "sector_assessments",
                columns: new[] { "DiagnosisId", "ClientId" });

            migrationBuilder.CreateIndex(
                name: "ux_sector_assessments_diagnosis_client_sector",
                table: "sector_assessments",
                columns: new[] { "DiagnosisId", "ClientSectorId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "diagnosis_objectives");

            migrationBuilder.DropTable(
                name: "sector_assessments");

            migrationBuilder.DropTable(
                name: "diagnoses");
        }
    }
}
