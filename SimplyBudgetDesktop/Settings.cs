﻿using System;
using System.IO;

namespace SimplyBudget.Properties;



// This class allows you to handle specific events on the settings class:
//  The SettingChanging event is raised before a setting's value is changed.
//  The PropertyChanged event is raised after a setting's value is changed.
//  The SettingsLoaded event is raised after the setting values are loaded.
//  The SettingsSaving event is raised before the setting values are saved.
internal sealed partial class Settings {
    
    public Settings() {
        // // To add event handlers for saving and changing settings, uncomment the lines below:
        //
        // this.SettingChanging += this.SettingChangingEventHandler;
        //
        // this.SettingsSaving += this.SettingsSavingEventHandler;
        //
    }

    public static string GetStorageDirectory() 
        => Path.GetFullPath(Environment.ExpandEnvironmentVariables(Default.StorageLocation));

    public static string GetDatabasePath()
        => Path.Combine(GetStorageDirectory(), "data.db");

    public static string GetDatabaseConnectionString()
        => $"Data Source='{GetDatabasePath()}'";

    private void SettingChangingEventHandler(object sender, System.Configuration.SettingChangingEventArgs e) {
        // Add code to handle the SettingChangingEvent event here.
    }
    
    private void SettingsSavingEventHandler(object sender, System.ComponentModel.CancelEventArgs e) {
        // Add code to handle the SettingsSaving event here.
    }
}
