using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prisma.Api.Migrations
{
    /// <inheritdoc />
    public partial class Bottlenecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bottlenecks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiagnosisId = table.Column<Guid>(type: "uuid", nullable: false),
                    SectorAssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Impact = table.Column<int>(type: "integer", nullable: false),
                    Frequency = table.Column<string>(type: "text", nullable: true),
                    Evidence = table.Column<string>(type: "text", nullable: true),
                    ProbableCause = table.Column<string>(type: "text", nullable: true),
                    EstimatedMonthlyHours = table.Column<decimal>(type: "numeric", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bottlenecks", x => x.Id);
                    table.UniqueConstraint("ak_bottlenecks_id_diagnosis_id", x => new { x.Id, x.DiagnosisId });
                    table.CheckConstraint("ck_bottlenecks_estimated_monthly_hours_non_negative", "\"EstimatedMonthlyHours\" IS NULL OR \"EstimatedMonthlyHours\" >= 0");
                    table.CheckConstraint("ck_bottlenecks_impact_range", "\"Impact\" BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "fk_bottlenecks_diagnoses_diagnosis_id",
                        column: x => x.DiagnosisId,
                        principalTable: "diagnoses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bottlenecks_sector_assessments_assessment_diagnosis_id",
                        columns: x => new { x.SectorAssessmentId, x.DiagnosisId },
                        principalTable: "sector_assessments",
                        principalColumns: new[] { "Id", "DiagnosisId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bottlenecks_DiagnosisId",
                table: "bottlenecks",
                column: "DiagnosisId");

            migrationBuilder.CreateIndex(
                name: "IX_bottlenecks_SectorAssessmentId_DiagnosisId",
                table: "bottlenecks",
                columns: new[] { "SectorAssessmentId", "DiagnosisId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bottlenecks");
        }
    }
}
