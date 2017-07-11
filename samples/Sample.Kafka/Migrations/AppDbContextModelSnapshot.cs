using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Sample.Kafka;

namespace Sample.Kafka.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.2")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("DotNetCore.CAP.Infrastructure.CapReceivedMessage", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("Added");

                    b.Property<string>("Content");

                    b.Property<string>("Group");

                    b.Property<string>("KeyName");

                    b.Property<DateTime>("LastRun");

                    b.Property<int>("Retries");

                    b.Property<string>("StatusName")
                        .HasMaxLength(50);

                    b.HasKey("Id");

                    b.ToTable("ReceivedMessages");
                });

            modelBuilder.Entity("DotNetCore.CAP.Infrastructure.CapSentMessage", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("Added");

                    b.Property<string>("Content");

                    b.Property<string>("KeyName");

                    b.Property<DateTime>("LastRun");

                    b.Property<int>("Retries");

                    b.Property<string>("StatusName")
                        .HasMaxLength(50);

                    b.HasKey("Id");

                    b.ToTable("SentMessages");
                });
        }
    }
}
