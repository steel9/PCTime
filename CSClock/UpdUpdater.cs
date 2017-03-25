﻿/*
CSClock - a program which keeps track of your computer time
Copyright (C) 2017  Viktor J

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace CSClock
{
    static class UpdUpdater
    {
        private const string className = "UpdUpdater.cs";

        static string installFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CSClock");
        static string exePath = Path.Combine(installFolder, "AppUpdater.exe");
        static string tempExePath = Path.Combine(Path.GetTempPath(), "AppUpdater.exe");

        static string logPath = Path.Combine(installFolder, "updupdaterlog.txt");

        static Logger logger = null;

        public static bool CheckForInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                {
                    using (var stream = client.OpenRead("http://www.google.com"))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        public static void UpdUpdate()
        {
            logger = new Logger("CSClock App Updater Updater", logPath, Logger.LogTimeDateOptions.YearMonthDayHourMinuteSecond, true);

            logger.Log(className, "--UPDUPDATE (UPDATER UPDATE)--", Logger.LogType.Info);
            if (Program.dev)
            {
                logger.Log(className, "-DEV-", Logger.LogType.Info);
            }

            //Check for Internet connection
            if (!CheckForInternetConnection())
            {
                logger.Log(className, "No internet connection available, aborting update", Logger.LogType.Info);
                return;
            }

            //Check if update is needed
            logger.Log(className, "Initializing WebClient", Logger.LogType.Info);
            WebClient webClient = new WebClient();
            logger.Log(className, "Current version without dots --> int", Logger.LogType.Info);
            FileVersionInfo currVer = FileVersionInfo.GetVersionInfo(exePath);
            int currentVersion = int.Parse(string.Format("{0}{1}{2}", currVer.FileMajorPart.ToString(), currVer.FileMinorPart.ToString(),
                currVer.FileBuildPart.ToString()));
            logger.Log(className, "Downloading VERSION file from master branch", Logger.LogType.Info);
            string latestVersionText = null;
            try
            {
                if (!Program.dev)
                {
                    latestVersionText = webClient.DownloadString("https://raw.githubusercontent.com/steel9/CSClock/master/VERSION2");
                }
                else
                {
                    latestVersionText = webClient.DownloadString("https://raw.githubusercontent.com/steel9/CSClock/development/VERSION2");
                }
            }
            catch (Exception ex)
            {
                logger.Log(className, "Error while downloading VERSION file from master branch, aborting update. Error: " + ex.ToString(), Logger.LogType.Error);
                return;
            }
            logger.Log(className, "Parsing version", Logger.LogType.Info);
            string latestVersion_s = latestVersionText.Split(',', '#')[1];
            logger.Log(className, "Current version is: " + currentVersion, Logger.LogType.Info);
            logger.Log(className, "Latest version is: " + latestVersion_s, Logger.LogType.Info);
            int latestVersion = int.Parse(latestVersion_s);

            if (currentVersion >= latestVersion)
            {
                //Update is not needed
                logger.Log(className, "Update is NOT needed. Current version: '" + currentVersion.ToString() + "', latest version: '" +
                    latestVersion.ToString() + "'", Logger.LogType.Info);
                return;
            }
            else if (Program.dev)
            {
                string ver = string.Empty;
                foreach (char c in latestVersion.ToString())
                {
                    ver += c + ".";
                }
                ver.Remove(6, 1);
                logger.Log(className, "Updater update is available: " + ver + "\r\nAutomatic updates are not available in development builds", Logger.LogType.Info);
                MessageBox.Show("Updater update is available: " + ver + "\r\nAutomatic updates are not available in development builds", "CSClock",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            //Close CSClock
            logger.Log("Closing updater", className, Logger.LogType.Info);
            Process[] processes;
            string procName = "Updater";
            processes = Process.GetProcessesByName(procName);
            try
            {
                foreach (Process proc in processes)
                {
                    proc.CloseMainWindow();
                    proc.WaitForExit();
                }
            }
            catch (NullReferenceException)
            {
                logger.Log(className, "NullReferenceException occurred", Logger.LogType.Warning);
            }
            catch (Exception ex)
            {
                logger.Log(className, "Error when closing app updater, aborting updater update. Error: " + ex.ToString(), Logger.LogType.Error);
            }


            //Update CSClock
            logger.Log(className, "Downloading latest Install.exe", Logger.LogType.Info);
            try
            {
                webClient.DownloadFile("https://github.com/steel9/CSClock/raw/master/Install.exe", tempExePath);
            }
            catch (Exception ex)
            {
                logger.Log(className, "Error when downloading CSClock, aborting update. Error: " + ex.ToString(), Logger.LogType.Info);
            }

            //Install CSClock
            logger.Log(className, "Installing UpdUpdater", Logger.LogType.Info);
            try
            {
                File.Copy(tempExePath, exePath, true);
                File.Delete(tempExePath);
            }
            catch (Exception ex)
            {
                logger.Log(className, "UpdUpdater installation error: " + ex.ToString(), Logger.LogType.Error);
                MessageBox.Show("Error when installing UpdUpdater: " + ex.Message, "CSClock Installer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
