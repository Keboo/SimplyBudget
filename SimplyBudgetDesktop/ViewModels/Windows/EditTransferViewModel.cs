﻿using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;
using JetBrains.Annotations;
using SimplyBudget.Collections;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;

namespace SimplyBudget.ViewModels.Windows
{
    internal class EditTransferViewModel : ViewEditViewModel<Transfer>, IRequestClose
    {
        public event EventHandler<EventArgs> RequestClose;

        private Transfer _existingsTransfer;

        public EditTransferViewModel()
        {
            ExpenseCategories = CollectionViewSource.GetDefaultView(ExpenseCategoryCollection.Instance);
            
            ExpenseCategories.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            _date = DateTime.Today;
        }

        public ICollectionView ExpenseCategories { get; }

        private int _amount;
        public int Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        private string _amountError;
        public string AmountError
        {
            get => _amountError;
            set => SetProperty(ref _amountError, value);
        }

        private int _fromExpenseCategoryID;
        public int FromExpenseCategoryID
        {
            get => _fromExpenseCategoryID;
            set => SetProperty(ref _fromExpenseCategoryID, value);
        }

        private string _fromError;
        public string FromError
        {
            get => _fromError;
            set => SetProperty(ref _fromError, value);
        }

        private int _toExpenseCategoryID;
        public int ToExpenseCategoryID
        {
            get => _toExpenseCategoryID;
            set => SetProperty(ref _toExpenseCategoryID, value);
        }

        private string _toError;
        public string ToError
        {
            get => _toError;
            set => SetProperty(ref _toError, value);
        }

        private string _description;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private DateTime _date;
        public DateTime Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        protected override async Task CreateAsync()
        {
            if (HasErrors()) return;

            var transfer = new Transfer
                               {
                                   Amount = Amount,
                                   Date = Date,
                                   Description = Description,
                                   FromExpenseCategoryID = FromExpenseCategoryID,
                                   ToExpenseCategoryID = ToExpenseCategoryID
                               };
            await transfer.Save();

            RequestClose.Raise(this, EventArgs.Empty);
        }

        protected override async Task SaveAsync()
        {
            if (HasErrors()) return;

            _existingsTransfer.Amount = Amount;
            _existingsTransfer.Date = Date;
            _existingsTransfer.Description = Description;
            _existingsTransfer.FromExpenseCategoryID = FromExpenseCategoryID;
            _existingsTransfer.ToExpenseCategoryID = ToExpenseCategoryID;


            await _existingsTransfer.Save();

            RequestClose.Raise(this, EventArgs.Empty);
        }

        private bool HasErrors()
        {
            bool rv = false;

            if (FromExpenseCategoryID <= 0)
            {
                FromError = "An expense category to transfer from is required";
                rv = true;
            }

            if (ToExpenseCategoryID <= 0)
            {
                ToError = "An expense category to transfer to is required";
                rv = true;
            }

            if (Amount <= 0.0)
            {
                AmountError = "An amount is required";
                rv = true;
            }

            return rv;
        }

        protected override Task SetPropertiesToEditAsync(Transfer transfer)
        {
            if (transfer is null) throw new ArgumentNullException("transfer");
            Amount = transfer.Amount;
            Date = transfer.Date;
            Description = transfer.Description;
            FromExpenseCategoryID = transfer.FromExpenseCategoryID;
            ToExpenseCategoryID = transfer.ToExpenseCategoryID;

            _existingsTransfer = transfer;
            
            return Task.FromResult<object>(null);
        }
    }
}