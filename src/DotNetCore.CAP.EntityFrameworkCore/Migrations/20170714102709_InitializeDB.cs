using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DotNetCore.CAP.EntityFrameworkCore.Migrations
{
    public partial class InitializeDB : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cap");

            migrationBuilder.CreateTable(
                name: "CapQueue",
                schema: "cap",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    MessageId = table.Column<string>(nullable: true),
                    Type = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapQueue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CapReceivedMessages",
                schema: "cap",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Added = table.Column<DateTime>(nullable: false),
                    Content = table.Column<string>(nullable: true),
                    ExpiresAt = table.Column<DateTime>(nullable: true),
                    Group = table.Column<string>(nullable: true),
                    KeyName = table.Column<string>(nullable: true),
                    Retries = table.Column<int>(nullable: false),
                    StatusName = table.Column<string>(maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapReceivedMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CapSentMessages",
                schema: "cap",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Added = table.Column<DateTime>(nullable: false),
                    Content = table.Column<string>(nullable: true),
                    ExpiresAt = table.Column<DateTime>(nullable: true),
                    KeyName = table.Column<string>(nullable: true),
                    Retries = table.Column<int>(nullable: false),
                    StatusName = table.Column<string>(maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapSentMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CapReceivedMessages_StatusName",
                schema: "cap",
                table: "CapReceivedMessages",
                column: "StatusName");

            migrationBuilder.CreateIndex(
                name: "IX_CapSentMessages_StatusName",
                schema: "cap",
                table: "CapSentMessages",
                column: "StatusName");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CapQueue",
                schema: "cap");

            migrationBuilder.DropTable(
                name: "CapReceivedMessages",
                schema: "cap");

            migrationBuilder.DropTable(
                name: "CapSentMessages",
                schema: "cap");
        }
    }
}
