using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using DotNetCore.CAP.EntityFrameworkCore;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.EntityFrameworkCore.Migrations
{
    [DbContext(typeof(CapDbContext))]
    [Migration("20170714102709_InitializeDB")]
    partial class InitializeDB
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasDefaultSchema("cap")
                .HasAnnotation("ProductVersion", "1.1.2")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("DotNetCore.CAP.Models.CapQueue", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("MessageId");

                    b.Property<int>("Type");

                    b.HasKey("Id");

                    b.ToTable("CapQueue");
                });

            modelBuilder.Entity("DotNetCore.CAP.Models.CapReceivedMessage", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("Added");

                    b.Property<string>("Content");

                    b.Property<DateTime?>("ExpiresAt");

                    b.Property<string>("Group");

                    b.Property<string>("KeyName");

                    b.Property<int>("Retries");

                    b.Property<string>("StatusName")
                        .IsRequired()
                        .HasMaxLength(50);

                    b.HasKey("Id");

                    b.HasIndex("StatusName");

                    b.ToTable("CapReceivedMessages");
                });

            modelBuilder.Entity("DotNetCore.CAP.Models.CapSentMessage", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("Added");

                    b.Property<string>("Content");

                    b.Property<DateTime?>("ExpiresAt");

                    b.Property<string>("KeyName");

                    b.Property<int>("Retries");

                    b.Property<string>("StatusName")
                        .IsRequired()
                        .HasMaxLength(50);

                    b.HasKey("Id");

                    b.HasIndex("StatusName");

                    b.ToTable("CapSentMessages");
                });
        }
    }
}
