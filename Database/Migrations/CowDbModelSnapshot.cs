﻿// <auto-generated />
using System;
using Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Database.Migrations
{
    [DbContext(typeof(CowDb))]
    partial class CowDbModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .UseCollation("case_insensitive")
                .HasAnnotation("Npgsql:CollationDefinition:case_insensitive", "und@colStrength=secondary,und@colStrength=secondary,icu,False")
                .HasAnnotation("ProductVersion", "7.0.5")
                .HasAnnotation("Proxies:ChangeTracking", false)
                .HasAnnotation("Proxies:CheckEquality", false)
                .HasAnnotation("Proxies:LazyLoading", true)
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Database.Models.Camera", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Address")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("CameraState")
                        .HasColumnType("integer");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("boolean");

                    b.HasKey("Id");

                    b.HasIndex("Address")
                        .IsUnique();

                    b.ToTable("Cameras");
                });

            modelBuilder.Entity("Database.Models.CameraAnalysis", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<int>("CalfCount")
                        .HasColumnType("integer");

                    b.Property<Guid>("CameraId")
                        .HasColumnType("uuid");

                    b.Property<int>("CowCount")
                        .HasColumnType("integer");

                    b.Property<DateTime>("DateTime")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("CameraId");

                    b.ToTable("CamerasAnalyses");
                });

            modelBuilder.Entity("Database.Models.Notification", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreateTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<bool>("IsSent")
                        .HasColumnType("boolean");

                    b.HasKey("Id");

                    b.HasIndex("IsSent");

                    b.ToTable("Notifications");

                    b.HasDiscriminator<string>("Discriminator").HasValue("Notification");

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("Database.Models.TelegramUser", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<int>("Status")
                        .HasColumnType("integer");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("Username")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Database.Models.NewUserNotification", b =>
                {
                    b.HasBaseType("Database.Models.Notification");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.HasIndex("UserId");

                    b.HasDiscriminator().HasValue("NewUserNotification");
                });

            modelBuilder.Entity("Database.Models.UserStatusChangeNotification", b =>
                {
                    b.HasBaseType("Database.Models.Notification");

                    b.Property<int>("NewStatus")
                        .HasColumnType("integer");

                    b.Property<int>("PreviousStatus")
                        .HasColumnType("integer");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.HasIndex("UserId");

                    b.ToTable("Notifications", t =>
                        {
                            t.Property("UserId")
                                .HasColumnName("UserStatusChangeNotification_UserId");
                        });

                    b.HasDiscriminator().HasValue("UserStatusChangeNotification");
                });

            modelBuilder.Entity("Database.Models.CameraAnalysis", b =>
                {
                    b.HasOne("Database.Models.Camera", "Camera")
                        .WithMany()
                        .HasForeignKey("CameraId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Camera");
                });

            modelBuilder.Entity("Database.Models.NewUserNotification", b =>
                {
                    b.HasOne("Database.Models.TelegramUser", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Database.Models.UserStatusChangeNotification", b =>
                {
                    b.HasOne("Database.Models.TelegramUser", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });
#pragma warning restore 612, 618
        }
    }
}
