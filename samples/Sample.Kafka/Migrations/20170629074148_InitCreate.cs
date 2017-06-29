using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sample.Kafka.Migrations
{
    public partial class InitCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReceivedMessages",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Added = table.Column<DateTime>(nullable: false),
                    Content = table.Column<string>(nullable: true),
                    KeyName = table.Column<string>(nullable: true),
                    LastRun = table.Column<DateTime>(nullable: false),
                    Retries = table.Column<int>(nullable: false),
                    StatusName = table.Column<string>(maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceivedMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SentMessages",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Added = table.Column<DateTime>(nullable: false),
                    Content = table.Column<string>(nullable: true),
                    KeyName = table.Column<string>(nullable: true),
                    LastRun = table.Column<DateTime>(nullable: false),
                    Retries = table.Column<int>(nullable: false),
                    StatusName = table.Column<string>(maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SentMessages", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceivedMessages");

            migrationBuilder.DropTable(
                name: "SentMessages");
        }
    }
}
