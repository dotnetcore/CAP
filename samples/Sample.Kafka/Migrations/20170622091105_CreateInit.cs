using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sample.Kafka.Migrations
{
    public partial class CreateInit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Payload = table.Column<string>(nullable: true),
                    RowVersion = table.Column<byte[]>(nullable: true),
                    SendTime = table.Column<DateTime>(nullable: false),
                    Status = table.Column<int>(nullable: false),
                    Topic = table.Column<string>(nullable: true),
                    UpdateTime = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Messages");
        }
    }
}
