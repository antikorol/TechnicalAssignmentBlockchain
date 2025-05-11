using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TechnicalAssignment.BlockchainCollector.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Blockchains",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    Hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Time = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    LatestUrl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PreviousHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PreviousUrl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PeerCount = table.Column<int>(type: "integer", nullable: false),
                    UnconfirmedCount = table.Column<int>(type: "integer", nullable: false),
                    HighFeePerKb = table.Column<int>(type: "integer", nullable: false),
                    MediumFeePerKb = table.Column<int>(type: "integer", nullable: false),
                    LowFeePerKb = table.Column<int>(type: "integer", nullable: false),
                    LastForkHeight = table.Column<int>(type: "integer", nullable: false),
                    LastForkHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blockchains", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Blockchains_Hash",
                table: "Blockchains",
                column: "Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Blockchains_Name_CreatedAt",
                table: "Blockchains",
                columns: new[] { "Name", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Blockchains");
        }
    }
}
