﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace MYPHandler
{
    public static class MypHandlerConfig
    {
        #region Attributes

        private static string maxOperationThread = "MaxOperationThread";
        private static string multithreadedExtraction = "MultiThreadedExtraction";

        #endregion

        #region Properties
        /// <summary>
        /// Might be used in the future for multithreading the processing of the GetFileTable function
        /// Otherwise not used at the moment (July 7th, 2009, Chryzo)
        /// </summary>
        public static int MaxOperationThread
        {
            get
            {
                if (ConfigurationManager.AppSettings[maxOperationThread] == null)
                {
                    return 2;
                }
                return Convert.ToInt32(ConfigurationManager.AppSettings[maxOperationThread]);
            }
            set
            {
                UpdateConfiguration(maxOperationThread, value.ToString());
            }
        }

        /// <summary>
        /// Option to multithread operations
        /// Automatically set to false if the reading disk is the same as the writting disk
        /// </summary>
        public static bool MultithreadedExtraction
        {
            get
            {
                if (ConfigurationManager.AppSettings[multithreadedExtraction] == null)
                {
                    return true;
                }
                return Convert.ToBoolean(ConfigurationManager.AppSettings[multithreadedExtraction]);
            }
            set
            {
                UpdateConfiguration(multithreadedExtraction, value.ToString());
            }
        }

        #endregion

        #region Functions

        static void UpdateConfiguration(string key, string value)
        {

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            // Check to see if the key already exists, if so we remove it
            if (ConfigurationManager.AppSettings[key] != null)
            {
                config.AppSettings.Settings.Remove(key);
            }
            // Add the new key value
            config.AppSettings.Settings.Add(key, value);
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        static void RemoveConfigurationKey(string key)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            // Check to see if the key already exists, if so we remove it
            for (int i = 0; i < ConfigurationManager.AppSettings.Keys.Count; i++)
            {
                if (ConfigurationManager.AppSettings.Keys.Get(i) == key)
                {
                    config.AppSettings.Settings.Remove(key);
                    break;
                }
            }
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        #endregion
    }
}
