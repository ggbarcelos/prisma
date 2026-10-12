using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prisma.Api.Migrations
{
    /// <inheritdoc />
    public partial class OpportunitiesAndBottleneckLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "opportunities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiagnosisId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false),
                    SolutionDescription = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: false),
                    ExpectedBenefit = table.Column<string>(type: "text", nullable: false),
                    Impact = table.Column<int>(type: "integer", nullable: false),
                    Effort = table.Column<int>(type: "integer", nullable: false),
                    Urgency = table.Column<int>(type: "integer", nullable: false),
                    Dependencies = table.Column<string>(type: "text", nullable: true),
                    AiDataNeeds = table.Column<string>(type: "text", nullable: true),
                    NeedsHumanReview = table.Column<bool>(type: "boolean", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_opportunities", x => x.Id);
                    table.UniqueConstraint("ak_opportunities_id_diagnosis_id", x => new { x.Id, x.DiagnosisId });
                    table.CheckConstraint("ck_opportunities_effort_range", "\"Effort\" BETWEEN 1 AND 5");
                    table.CheckConstraint("ck_opportunities_impact_range", "\"Impact\" BETWEEN 1 AND 5");
                    table.CheckConstraint("ck_opportunities_urgency_range", "\"Urgency\" BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "fk_opportunities_diagnoses_diagnosis_id",
                        column: x => x.DiagnosisId,
                        principalTable: "diagnoses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "opportunity_bottlenecks",
                columns: table => new
                {
                    OpportunityId = table.Column<Guid>(type: "uuid", nullable: false),
                    BottleneckId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiagnosisId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_opportunity_bottlenecks", x => new { x.OpportunityId, x.BottleneckId });
                    table.ForeignKey(
                        name: "fk_opportunity_bottlenecks_bottlenecks_bottleneck_diagnosis_id",
                        columns: x => new { x.BottleneckId, x.DiagnosisId },
                        principalTable: "bottlenecks",
                        principalColumns: new[] { "Id", "DiagnosisId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_opportunity_bottlenecks_opportunities_opportunity_diagnosis_id",
                        columns: x => new { x.OpportunityId, x.DiagnosisId },
                        principalTable: "opportunities",
                        principalColumns: new[] { "Id", "DiagnosisId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_opportunities_DiagnosisId",
                table: "opportunities",
                column: "DiagnosisId");

            migrationBuilder.CreateIndex(
                name: "IX_opportunity_bottlenecks_BottleneckId_DiagnosisId",
                table: "opportunity_bottlenecks",
                columns: new[] { "BottleneckId", "DiagnosisId" });

            migrationBuilder.CreateIndex(
                name: "IX_opportunity_bottlenecks_OpportunityId_DiagnosisId",
                table: "opportunity_bottlenecks",
                columns: new[] { "OpportunityId", "DiagnosisId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "opportunity_bottlenecks");

            migrationBuilder.DropTable(
                name: "opportunities");
        }
    }
}
