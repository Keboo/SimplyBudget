﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SimplyBudgetShared.Data;

namespace SimplyBudgetShared.Migrations;

[DbContext(typeof(BudgetContext))]
[Migration("20210810062023_AddIgnoreBudget")]
partial class AddIgnoreBudget
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "5.0.6");

        modelBuilder.Entity("SimplyBudgetShared.Data.Account", b =>
            {
                b.Property<int>("ID")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER");

                b.Property<bool>("IsDefault")
                    .HasColumnType("INTEGER");

                b.Property<string>("Name")
                    .HasColumnType("TEXT");

                b.Property<DateTime>("ValidatedDate")
                    .HasColumnType("TEXT");

                b.HasKey("ID");

                b.HasIndex("IsDefault");

                b.ToTable("Account");
            });

        modelBuilder.Entity("SimplyBudgetShared.Data.ExpenseCategory", b =>
            {
                b.Property<int>("ID")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER");

                b.Property<int?>("AccountID")
                    .HasColumnType("INTEGER");

                b.Property<int>("BudgetedAmount")
                    .HasColumnType("INTEGER");

                b.Property<int>("BudgetedPercentage")
                    .HasColumnType("INTEGER");

                b.Property<int?>("Cap")
                    .HasColumnType("INTEGER");

                b.Property<string>("CategoryName")
                    .HasColumnType("TEXT");

                b.Property<int>("CurrentBalance")
                    .HasColumnType("INTEGER");

                b.Property<bool>("IsHidden")
                    .HasColumnType("INTEGER");

                b.Property<string>("Name")
                    .HasColumnType("TEXT");

                b.HasKey("ID");

                b.HasIndex("AccountID");

                b.HasIndex("CategoryName");

                b.ToTable("ExpenseCategory");
            });

        modelBuilder.Entity("SimplyBudgetShared.Data.ExpenseCategoryItem", b =>
            {
                b.Property<int>("ID")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER");

                b.Property<DateTime>("Date")
                    .HasColumnType("TEXT");

                b.Property<string>("Description")
                    .HasColumnType("TEXT");

                b.HasKey("ID");

                b.ToTable("ExpenseCategoryItem");
            });

        modelBuilder.Entity("SimplyBudgetShared.Data.ExpenseCategoryItemDetail", b =>
            {
                b.Property<int>("ID")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER");

                b.Property<int>("Amount")
                    .HasColumnType("INTEGER");

                b.Property<int>("ExpenseCategoryId")
                    .HasColumnType("INTEGER");

                b.Property<int>("ExpenseCategoryItemId")
                    .HasColumnType("INTEGER");

                b.Property<bool>("IgnoreBudget")
                    .HasColumnType("INTEGER");

                b.HasKey("ID");

                b.HasIndex("ExpenseCategoryId");

                b.HasIndex("ExpenseCategoryItemId");

                b.ToTable("ExpenseCategoryItemDetail");
            });

        modelBuilder.Entity("SimplyBudgetShared.Data.Metadata", b =>
            {
                b.Property<int>("ID")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER");

                b.Property<string>("Key")
                    .HasColumnType("TEXT");

                b.Property<string>("Value")
                    .HasColumnType("TEXT");

                b.HasKey("ID");

                b.ToTable("MetaData");
            });

        modelBuilder.Entity("SimplyBudgetShared.Data.ExpenseCategory", b =>
            {
                b.HasOne("SimplyBudgetShared.Data.Account", "Account")
                    .WithMany("ExpenseCategories")
                    .HasForeignKey("AccountID");

                b.Navigation("Account");
            });

        modelBuilder.Entity("SimplyBudgetShared.Data.ExpenseCategoryItemDetail", b =>
            {
                b.HasOne("SimplyBudgetShared.Data.ExpenseCategory", "ExpenseCategory")
                    .WithMany()
                    .HasForeignKey("ExpenseCategoryId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.HasOne("SimplyBudgetShared.Data.ExpenseCategoryItem", "ExpenseCategoryItem")
                    .WithMany("Details")
                    .HasForeignKey("ExpenseCategoryItemId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("ExpenseCategory");

                b.Navigation("ExpenseCategoryItem");
            });

        modelBuilder.Entity("SimplyBudgetShared.Data.Account", b =>
            {
                b.Navigation("ExpenseCategories");
            });

        modelBuilder.Entity("SimplyBudgetShared.Data.ExpenseCategoryItem", b =>
            {
                b.Navigation("Details");
            });
#pragma warning restore 612, 618
    }
}
