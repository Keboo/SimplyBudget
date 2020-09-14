
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using JetBrains.Annotations;
using SimplyBudget.Properties;
using SimplyBudgetShared.Utilities;

namespace SimplyBudget.Controls
{
    /// <summary>
    /// Interaction logic for DateRangePicker.xaml
    /// </summary>
    public partial class DateRangePicker
    {
        public static readonly DependencyProperty StartProperty =
            DependencyProperty.Register("Start", typeof(DateTime), typeof(DateRangePicker),
                                        new FrameworkPropertyMetadata(default(DateTime), 
                                            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public DateTime Start
        {
            get { return (DateTime)GetValue(StartProperty); }
            set { SetValue(StartProperty, value); }
        }

        public static readonly DependencyProperty EndProperty =
            DependencyProperty.Register("End", typeof(DateTime), typeof(DateRangePicker),
                                        new FrameworkPropertyMetadata(default(DateTime), 
                                            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public DateTime End
        {
            get { return (DateTime)GetValue(EndProperty); }
            set { SetValue(EndProperty, value); }
        }

        public static readonly DependencyProperty SimpleModeProperty =
            DependencyProperty.Register("SimpleMode", typeof(bool), typeof(DateRangePicker), 
            new PropertyMetadata(true, (@do, args) =>
                                           {
                                               Settings.Default.DateRangeSimpleMode = (bool)args.NewValue;
                                               Settings.Default.Save();
                                           }));

        public bool SimpleMode
        {
            get { return (bool)GetValue(SimpleModeProperty); }
            set { SetValue(SimpleModeProperty, value); }
        }

        public IEnumerable<object> SimpleModeOptions
        {
            get
            {
                Func<DateRange> thisMonthRange = () => new DateRange(DateTime.Today.StartOfMonth(), DateTime.Today.EndOfMonth());

                Func<DateRange> previousMonthRange = () =>
                                                         {
                                                             var lastMonth = DateTime.Today.AddMonths(-1);
                                                             return new DateRange(lastMonth.StartOfMonth(), lastMonth.EndOfMonth());
                                                         };

                Func<int, DateRange> lastMonthsRange = numMonths =>
                                                           {
                                                               var end = DateTime.Today.EndOfMonth();
                                                               var month = DateTime.Today.AddMonths(-1 * (numMonths - 1));
                                                               return new DateRange(month.StartOfMonth(), end);
                                                           };

                return new[]
                           {
                               new SimpleModeOption("This Month", thisMonthRange),
                               new SimpleModeOption("Previous Month", previousMonthRange),
                               new SimpleModeOption("Last 2 Months", () => lastMonthsRange(2)),
                               new SimpleModeOption("Last 3 Months", () => lastMonthsRange(3)),
                               new SimpleModeOption("Last 4 Months", () => lastMonthsRange(4)),
                               new SimpleModeOption("Last 5 Months", () => lastMonthsRange(5)),
                               new SimpleModeOption("Last 6 Months", () => lastMonthsRange(6)),
                               new SimpleModeOption("Last Year", () => lastMonthsRange(12))
                           };
            }
        }

        public DateRangePicker()
        {
            InitializeComponent();
            SimpleRangeSelector.SelectedIndex = 0;
            //TODO: Decouple from the settings
            SetCurrentValue(SimpleModeProperty, Settings.Default.DateRangeSimpleMode);
        }

        private void SimpleRangeSelectorOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var option = (SimpleModeOption) SimpleRangeSelector.SelectedItem;
            var range = option.GetRange();
            SetCurrentValue(StartProperty, range.Start);
            SetCurrentValue(EndProperty, range.End);
        }

        private void MoreLinkOnClick(object sender, RoutedEventArgs e)
        {
            SetCurrentValue(SimpleModeProperty, false);
        }

        private void SimpleLinkOnClick(object sender, RoutedEventArgs e)
        {
            SetCurrentValue(SimpleModeProperty, true);
        }

        private class DateRange
        {
            private readonly DateTime _start;
            private readonly DateTime _end;

            public DateRange(DateTime start, DateTime end)
            {
                _start = start;
                _end = end;
            }

            public DateTime Start
            {
                get { return _start; }
            }

            public DateTime End
            {
                get { return _end; }
            }
        }

        private class SimpleModeOption
        {
            private readonly string _title;
            private readonly Func<DateRange> _getRange;

            public SimpleModeOption(string title, [NotNull] Func<DateRange> getRange)
            {
                if (getRange == null) throw new ArgumentNullException("getRange");
                _title = title;
                _getRange = getRange;
            }

            public override string ToString()
            {
                return _title;
            }

            public DateRange GetRange()
            {
                return _getRange();
            }
        }
    }
}
