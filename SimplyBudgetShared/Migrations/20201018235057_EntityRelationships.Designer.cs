﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SimplyBudgetShared.Data;

namespace SimplyBudgetShared.Migrations;

[DbContext(typeof(BudgetContext))]
[Migration("20201018235057_EntityRelationships")]
partial class EntityRelationships
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "3.1.8");

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

                b.Property<string>("CategoryName")
                    .HasColumnType("TEXT");

                b.Property<int>("CurrentBalance")
                    .HasColumnType("INTEGER");

                b.Property<string>("Name")
                    .HasColumnType("TEXT");

                b.HasKey("ID");

                b.HasIndex("AccountID");

                b.HasIndex("CategoryName");

                b.ToTable("ExpenseCategory");
            });

        modelBuilder.Entity("SimplyBudgetShared.Data.Income", b =>
            {
                b.Property<int>("ID")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER");

                b.Property<DateTime>("Date")
                    .HasColumnType("TEXT");

                b.Property<string>("Description")
                    .HasColumnType("TEXT");

                b.Property<int>("TotalAmount")
                    .HasColumnType("INTEGER");

                b.HasKey("ID");

                b.ToTable("Income");
            });

        modelBuilder.Entity("SimplyBudgetShared.Data.IncomeItem", b =>
            {
                b.Property<int>("ID")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER");

                b.Property<int>("Amount")
                    .HasColumnType("INTEGER");

                b.Property<string>("Description")
                    .HasColumnType("TEXT");

                b.Property<int>("ExpenseCategoryID")
                    .HasColumnType("INTEGER");

                b.Property<int>("IncomeID")
                    .HasColumnType("INTEGER");

                b.HasKey("ID");

                b.HasIndex("ExpenseCategoryID");

                b.HasIndex("IncomeID");

                b.ToTable("IncomeItem");
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

        modelBuilder.Entity("SimplyBudgetShared.Data.Transaction", b =>
            {
                b.Property<int>("ID")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER");

                b.Property<DateTime>("Date")
                    .HasColumnType("TEXT");

                b.Property<string>("Description")
                    .HasColumnType("TEXT");

                b.HasKey("ID");

                b.HasIndex("Date");

                b.ToTable("Transaction");
            });

        modelBuilder.Entity("SimplyBudgetShared.Data.TransactionItem", b =>
            {
                b.Property<int>("ID")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER");

                b.Property<int>("Amount")
                    .HasColumnType("INTEGER");

                b.Property<string>("Description")
                    .HasColumnType("TEXT");

                b.Property<int>("ExpenseCategoryID")
                    .HasColumnType("INTEGER");

                b.Property<int>("TransactionID")
                    .HasColumnType("INTEGER");

                b.HasKey("ID");

                b.HasIndex("ExpenseCategoryID");

                b.HasIndex("TransactionID");

                b.ToTable("TransactionItem");
            });

        modelBuilder.Entity("SimplyBudgetShared.Data.Transfer", b =>
            {
                b.Property<int>("ID")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER");

                b.Property<int>("Amount")
                    .HasColumnType("INTEGER");

                b.Property<DateTime>("Date")
                    .HasColumnType("TEXT");

                b.Property<string>("Description")
                    .HasColumnType("TEXT");

                b.Property<int>("FromExpenseCategoryID")
                    .HasColumnType("INTEGER");

                b.Property<int>("ToExpenseCategoryID")
                    .HasColumnType("INTEGER");

                b.HasKey("ID");

                b.HasIndex("Date");

                b.HasIndex("FromExpenseCategoryID");

                b.HasIndex("ToExpenseCategoryID");

                b.ToTable("Transfer");
            });

        modelBuilder.Entity("SimplyBudgetShared.Data.ExpenseCategory", b =>
            {
                b.HasOne("SimplyBudgetShared.Data.Account", "Account")
                    .WithMany("ExpenseCategories")
                    .HasForeignKey("AccountID");
            });

        modelBuilder.Entity("SimplyBudgetShared.Data.IncomeItem", b =>
            {
                b.HasOne("SimplyBudgetShared.Data.ExpenseCategory", "ExpenseCategory")
                    .WithMany()
                    .HasForeignKey("ExpenseCategoryID")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.HasOne("SimplyBudgetShared.Data.Income", "Income")
                    .WithMany("IncomeItems")
                    .HasForeignKey("IncomeID")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });

        modelBuilder.Entity("SimplyBudgetShared.Data.TransactionItem", b =>
            {
                b.HasOne("SimplyBudgetShared.Data.ExpenseCategory", "ExpenseCategory")
                    .WithMany()
                    .HasForeignKey("ExpenseCategoryID")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.HasOne("SimplyBudgetShared.Data.Transaction", "Transaction")
                    .WithMany("TransactionItems")
                    .HasForeignKey("TransactionID")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });

        modelBuilder.Entity("SimplyBudgetShared.Data.Transfer", b =>
            {
                b.HasOne("SimplyBudgetShared.Data.ExpenseCategory", "FromExpenseCategory")
                    .WithMany()
                    .HasForeignKey("FromExpenseCategoryID")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.HasOne("SimplyBudgetShared.Data.ExpenseCategory", "ToExpenseCategory")
                    .WithMany()
                    .HasForeignKey("ToExpenseCategoryID")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });
#pragma warning restore 612, 618
    }
}
