
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Xml.Serialization;

namespace SimplyBudget.Properties
{

    internal sealed partial class Settings {

    }

    [Serializable]
    public class DataGridSettingsManager
    {
        public DataGridSettings[]? GridSettings { get; set; }

        public static void SaveSettings(DataGrid dataGrid)
        {
            if (dataGrid is null) throw new ArgumentNullException("dataGrid");
            var manager = Settings.Default.DataGridSettingsManager ??
                          (Settings.Default.DataGridSettingsManager = new DataGridSettingsManager());

            var settings = new List<DataGridSettings>(manager.GridSettings ?? new DataGridSettings[0]);
            settings.RemoveAll(x => x.Name == dataGrid.Name);
            settings.Add(DataGridSettings.Create(dataGrid));
            manager.GridSettings = settings.ToArray();
            Settings.Default.Save();
        }

        public static DataGridSettings? GetSettings(DataGrid dataGrid)
        {
            if (dataGrid is null) throw new ArgumentNullException("dataGrid");
            var manager = Settings.Default.DataGridSettingsManager;
            if (manager != null && manager.GridSettings != null)
                return manager.GridSettings.FirstOrDefault(x => x.Name == dataGrid.Name);
            return null;
        }
    }

    [Serializable]
    public class DataGridSettings
    {
        public static DataGridSettings Create(DataGrid dataGrid)
        {
            if (dataGrid is null) throw new ArgumentNullException(nameof(dataGrid));
            if (string.IsNullOrWhiteSpace(dataGrid.Name)) throw new InvalidDataException("DataGrid must have a name");

            return new DataGridSettings
                       {
                           Name = dataGrid.Name,
                           Columns = dataGrid.Columns.Select(DataGridColumnSettings.Create).ToArray()
                       };
        }

        public void LoadSettings(DataGrid dataGrid)
        {
            if (dataGrid is null) throw new ArgumentNullException("dataGrid");
            if (dataGrid.Name != Name) throw new InvalidOperationException("Settings are not for this DataGrid");

            var columns = Columns;
            if (columns != null)
            {
                var columnsByHeader = Columns.ToDictionary(x => x.Header);
                foreach (var column in dataGrid.Columns.Where(x => x.Header != null))
                {
                    if (columnsByHeader.TryGetValue(column!.Header!.ToString() ?? "", out DataGridColumnSettings? settings))
                    {
                        column.MinWidth = settings.MinWidth;
                        column.MaxWidth = settings.MaxWidth;
                        column.DisplayIndex = settings.DisplayIndex;
                        column.SortDirection = settings.SortDirection;
                        column.Width = new DataGridLength(settings.WidthValue, settings.WidthType);
                    }
                }
            }
        }

        public DataGridColumnSettings[]? Columns { get; set; }

        [XmlElement]
        public string? Name { get; set; }
    }

    [Serializable]
    public class DataGridColumnSettings
    {
        public static DataGridColumnSettings Create(DataGridColumn column)
        {
            if (column is null) throw new ArgumentNullException("column");
            if (column.Header is null) throw new InvalidDataException("Column must have a header");
        
            return new DataGridColumnSettings
                       {
                           Header = column.Header!.ToString() ?? "",
                           MinWidth = column.MinWidth,
                           MaxWidth = column.MaxWidth,
                           DisplayIndex = column.DisplayIndex,
                           SortDirection = column.SortDirection,
                           WidthType = column.Width.UnitType,
                           WidthValue = column.Width.Value
                       };
        }

        public ListSortDirection? SortDirection { get; set; }

        public DataGridLengthUnitType WidthType { get; set; }
        public double WidthValue { get; set; }
        
        public double MinWidth { get; set; }
        public double MaxWidth { get; set; }

        public int DisplayIndex { get; set; }

        public string? Header { get; set; }
    }
}
