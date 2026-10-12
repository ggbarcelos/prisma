using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prisma.Api.Migrations
{
    /// <inheritdoc />
    public partial class PhasesAndDiagnosisPhaseLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "phases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiagnosisId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_phases", x => x.Id);
                    table.UniqueConstraint("ak_phases_id_diagnosis_id", x => new { x.Id, x.DiagnosisId });
                    table.CheckConstraint("ck_phases_sort_order_nonnegative", "\"SortOrder\" >= 0");
                    table.ForeignKey(
                        name: "fk_phases_diagnoses_diagnosis_id",
                        column: x => x.DiagnosisId,
                        principalTable: "diagnoses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_opportunities_PhaseId_DiagnosisId",
                table: "opportunities",
                columns: new[] { "PhaseId", "DiagnosisId" });

            migrationBuilder.CreateIndex(
                name: "ux_phases_diagnosis_id_sort_order",
                table: "phases",
                columns: new[] { "DiagnosisId", "SortOrder" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_opportunities_phases_phase_id_diagnosis_id",
                table: "opportunities",
                columns: new[] { "PhaseId", "DiagnosisId" },
                principalTable: "phases",
                principalColumns: new[] { "Id", "DiagnosisId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_opportunities_phases_phase_id_diagnosis_id",
                table: "opportunities");

            migrationBuilder.DropTable(
                name: "phases");

            migrationBuilder.DropIndex(
                name: "IX_opportunities_PhaseId_DiagnosisId",
                table: "opportunities");
        }
    }
}
