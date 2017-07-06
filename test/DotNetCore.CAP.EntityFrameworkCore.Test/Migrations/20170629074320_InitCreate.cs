using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DotNetCore.CAP.EntityFrameworkCore.Test.Migrations
{
    public partial class InitCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CapReceivedMessages",
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
                constraints: table => { table.PrimaryKey("PK_CapReceivedMessages", x => x.Id); });

            migrationBuilder.CreateTable(
                name: "CapSentMessages",
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
                constraints: table => { table.PrimaryKey("PK_CapSentMessages", x => x.Id); });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CapReceivedMessages");

            migrationBuilder.DropTable(
                name: "CapSentMessages");
        }
    }
}