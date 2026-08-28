using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Web.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BetaReadiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastRevisionComment",
                table: "Deals",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RevisionRequestedAt",
                table: "Deals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBlocked",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PrivacyPolicyAcceptedAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TermsAcceptedAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastRevisionComment",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "RevisionRequestedAt",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "IsBlocked",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PrivacyPolicyAcceptedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TermsAcceptedAt",
                table: "AspNetUsers");
        }
    }
}
