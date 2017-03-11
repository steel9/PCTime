﻿/*
CSTime - a program which keeps track of your computer time
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
using System.Windows.Forms;
using System.Resources;
using Microsoft.Win32;
using System.Diagnostics;

namespace CSTime
{
    public partial class CSTime : Form
    {
        private const string className = "CSTime.cs"; //Used for logging

        public bool startMinimized = false;
        public bool instantExit = false;

        public int maximumSeconds = 0;
        public int maximumSecondsOvertime = 0;
        public int secondsElapsed = 0;

        public bool overtimeY = false; //yesterday
        public bool overtimeC = false; //current session

        private bool timesOutEvent = true;
        private bool timesOutOvertimeEvent = true;
        private bool pauseResumeTimerOnComputerLockUnlock = false;

        private string timeElapsed = null;
        private string timeRemaining = null;

        public DateTime startDateTime = default(DateTime);

        public CSTime()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            if (startMinimized)
            {
                WindowState = FormWindowState.Minimized;
            }
            else
            {
                WindowState = FormWindowState.Normal;
            }

            base.OnLoad(e);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Program.configureForm = new Configure();
            Program.configureForm.ShowDialog();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && !instantExit)
            {
                e.Cancel = true;
                if (Properties.Settings.Default.closeNoticeFirstTime)
                {
                    if (timer.Enabled)
                    {
                        MessageBox.Show(Program.rm_Messages.GetString("closeNotice"), "CSTime", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show(Program.rm_Messages.GetString("closeNotice_v2"), "CSTime", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    Properties.Settings.Default.closeNoticeFirstTime = false;
                    Properties.Settings.Default.Save();
                }
                WindowState = FormWindowState.Minimized;
            }
            else
            {

                Program.logger.Log(className, "executing CSTime closing event", Logger.LogType.Info);

                Program.logger.Log(className, "Quitting CSTime", Logger.LogType.Info);
                Program.logger.Log(className, "Performing quit actions...", Logger.LogType.Info);

                Properties.Settings.Default.secondsElapsed = secondsElapsed;
                if (overtimeC)
                {
                    Properties.Settings.Default.overtimeMinutes = (decimal)(secondsElapsed - maximumSeconds) / 60;
                }

                Properties.Settings.Default.Save();
                if (Properties.Settings.Default.pauseResumeTimerOnComputerLockUnlock && Properties.Settings.Default.timerEnabled)
                {
                    SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
                }
                Program.notifyIcon1.Visible = false;
                Program.logger.Log(className, "Done", Logger.LogType.Info);
            }
        }

        private void CSTime_Load(object sender, EventArgs e)
        {
            GUI_Load();
        }

        public void GUI_Load()
        {
            if (Program.rm_GUI == null)
            {
                Program.rm_GUI = new ResourceManager(string.Format("CSTime.Languages.{0}.GUI", Program.selectedLanguage), Program.assembly);
            }

            label_configRequired.Text = Program.rm_GUI.GetString("label_configRequired_text");
            label1.Text = Program.rm_GUI.GetString("label1_text");
            label3.Text = Program.rm_GUI.GetString("label3_text");
            button_quit.Text = Program.rm_GUI.GetString("quit");
            button_about.Text = Program.rm_GUI.GetString("button_about_text");
            toolTip1.SetToolTip(button1, Program.rm_GUI.GetString("button1_toolTip_text"));
            toolTip1.SetToolTip(button_fwrdRwndTime, Program.rm_GUI.GetString("button2_toolTip_text"));
            toolTip1.SetToolTip(l_exclM, Program.rm_GUI.GetString("exclM_toolTip_text"));

            label_version.Text = string.Format("v{0}", Application.ProductVersion);
            if (Program.debug)
            {
                this.Text = "CSTime DEBUG";
                label_version.Text += " DEBUGGING MODE";
            }

            if (Properties.Settings.Default.configured)
            {
                panel_home.Show();
            }

            if (Properties.Settings.Default.timerEnabled)
            {
                button3.Enabled = true;
            }

            if (timer.Enabled)
            {
                button3.Image = Properties.Resources.pause_97625_640;
                toolTip1.SetToolTip(button3, Program.rm_GUI.GetString("button3_toolTip_pause_text"));
            }
            else
            {
                button3.Image = Properties.Resources.play_97626_640;
                toolTip1.SetToolTip(button3, Program.rm_GUI.GetString("button3_toolTip_resume_text"));
            }

            if (Properties.Settings.Default.configured)
            {
                string timeElapsed = TimeSpan.FromSeconds(secondsElapsed).ToString(@"hh\:mm\:ss");
                string timeRemaining;

                if (!overtimeY)
                {
                    timeRemaining = TimeSpan.FromSeconds(maximumSeconds - secondsElapsed).ToString(@"hh\:mm\:ss");
                }
                else if (maximumSeconds + maximumSecondsOvertime >= secondsElapsed)
                {
                    timeRemaining = TimeSpan.FromSeconds((maximumSeconds + maximumSecondsOvertime) -
                        secondsElapsed).ToString(@"hh\:mm\:ss");
                }
                else
                {
                    timeRemaining = "-" + TimeSpan.FromSeconds((maximumSeconds + maximumSecondsOvertime) -
                        secondsElapsed).ToString(@"hh\:mm\:ss");
                }

                label_timeElapsed.Text = timeElapsed;
                label_timeRemaining.Text = timeRemaining;

                if (!Program.debug)
                {
                    Program.notifyIcon1.Text = string.Format(Program.rm_Messages.GetString("notifyIcon_text"), Program.rm_Messages.GetString("NOT_RUNNING"),
                        timeRemaining);
                }
                else
                {
                    Program.notifyIcon1.Text = string.Format(Program.rm_Messages.GetString("notifyIcon_text_debug"),
                        Program.rm_Messages.GetString("NOT_RUNNING"), timeRemaining);
                }
            }

            TopMost = Properties.Settings.Default.guiTopMost;

            if (maximumSeconds <= 0 && !overtimeY && !overtimeC)
            {
                label_timeElapsed.Text = label_timeRemaining.Text = "--:--:--";
                button3.Enabled = false;
            }
        }

        public void LoadApplySettings()
        {
            if (Properties.Settings.Default.configured)
            {
                switch (DateTime.Now.DayOfWeek)
                {
                    case DayOfWeek.Monday:
                        maximumSeconds = (int)(Properties.Settings.Default.maximumHoursMon * 3600);
                        break;

                    case DayOfWeek.Tuesday:
                        maximumSeconds = (int)(Properties.Settings.Default.maximumHoursTue * 3600);
                        break;

                    case DayOfWeek.Wednesday:
                        maximumSeconds = (int)(Properties.Settings.Default.maximumHoursWed * 3600);
                        break;

                    case DayOfWeek.Thursday:
                        maximumSeconds = (int)(Properties.Settings.Default.maximumHoursThu * 3600);
                        break;

                    case DayOfWeek.Friday:
                        maximumSeconds = (int)(Properties.Settings.Default.maximumHoursFri * 3600);
                        break;

                    case DayOfWeek.Saturday:
                        maximumSeconds = (int)(Properties.Settings.Default.maximumHoursSat * 3600);
                        break;

                    case DayOfWeek.Sunday:
                        maximumSeconds = (int)(Properties.Settings.Default.maximumHoursSun * 3600);
                        break;
                }

                if (Properties.Settings.Default.secondsElapsedDateTime.Date == DateTime.Now.Date)
                {
                    secondsElapsed = (int)(Properties.Settings.Default.secondsElapsed);
                }
                else
                {
                    Properties.Settings.Default.secondsElapsed = 0;
                    Properties.Settings.Default.secondsElapsedDateTime = DateTime.Now;
                    Properties.Settings.Default.Save();
                }

                DateTime overtimeDateTime = Properties.Settings.Default.overtimeDateTime;
                if (overtimeDateTime.Date == DateTime.Now.Date)
                {
                    maximumSecondsOvertime = (int)(Properties.Settings.Default.maximumMinutesOvertime * 60);
                }
                else if (overtimeDateTime.Date == DateTime.Now.Date.AddDays(-1))
                {
                    decimal overtimeMinutes = Properties.Settings.Default.overtimeMinutes;
                    if (maximumSeconds >= (int)(overtimeMinutes * 60))
                    {
                        maximumSeconds -= (int)(overtimeMinutes * 60);
                    }
                    else
                    {
                        maximumSeconds = 0;
                        overtimeY = true;
                    }
                }
                else
                {
                    Properties.Settings.Default.overtimeMinutes = 0;
                    Properties.Settings.Default.maximumMinutesOvertime = 0;
                    Properties.Settings.Default.overtimeDateTime = DateTime.Now;
                    Properties.Settings.Default.Save();
                }


                bool settings_timerEnabled = Properties.Settings.Default.timerEnabled;
                bool settings_timerPaused = Properties.Settings.Default.timerPaused;

                pauseResumeTimerOnComputerLockUnlock = Properties.Settings.Default.pauseResumeTimerOnComputerLockUnlock;
                if (pauseResumeTimerOnComputerLockUnlock && settings_timerEnabled)
                {
                    SystemEvents.SessionSwitch += new SessionSwitchEventHandler(SystemEvents_SessionSwitch);
                }

                timer.Enabled = (settings_timerEnabled && !settings_timerPaused && (maximumSeconds > 0 || overtimeC || overtimeY));

                if (settings_timerPaused)
                {
                    PauseTimer(true);

                    Properties.Settings.Default.timerPaused = false;
                    Properties.Settings.Default.Save();
                }

                if (maximumSeconds > 0)
                {
                    if (Properties.Settings.Default.timerEnabled)
                    {
                        Program.logger.Log(className, "Starting timer", Logger.LogType.Info);
                        Program.contextMenu1.MenuItems[0].Text = Program.rm_GUI.GetString("pause");
                    }
                    else
                    {
                        Program.logger.Log(className, "Not starting timer, the timer is disabled in the settings", Logger.LogType.Info);
                    }
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (timer.Enabled)
            {
                PauseTimer(true);
                Program.notifyIcon1.ShowBalloonTip(2500, "CSTime", Program.rm_Messages.GetString("timerPausedNotification_text"), ToolTipIcon.Info);
            }
            else
            {
                ResumeTimer(true);
                Program.notifyIcon1.ShowBalloonTip(2500, "CSTime", Program.rm_Messages.GetString("timerResumedNotification_text"), ToolTipIcon.Info);
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            About about = new About();
            about.TopMost = this.TopMost;
            about.ShowDialog();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            secondsElapsed++;

            timeElapsed = TimeSpan.FromSeconds(secondsElapsed).ToString(@"hh\:mm\:ss");

            if (!overtimeC && !overtimeY)
            {
                timeRemaining = TimeSpan.FromSeconds(maximumSeconds - secondsElapsed).ToString(@"hh\:mm\:ss");
            }
            else if (maximumSeconds + maximumSecondsOvertime >= secondsElapsed)
            {
                timeRemaining = TimeSpan.FromSeconds((maximumSeconds + maximumSecondsOvertime) - secondsElapsed).ToString(@"hh\:mm\:ss");
            }
            else
            {
                timeRemaining = "-" + TimeSpan.FromSeconds((maximumSeconds + maximumSecondsOvertime) - secondsElapsed).ToString(@"hh\:mm\:ss");
                l_exclM.Visible = true;
            }
            

            Program.CSTimeForm.label_timeElapsed.Text = timeElapsed;
            Program.CSTimeForm.label_timeRemaining.Text = timeRemaining;

            if (!Program.debug)
            {
                Program.notifyIcon1.Text = string.Format(Program.rm_Messages.GetString("notifyIcon_text"), Program.rm_Messages.GetString("RUNNING"),
                    timeRemaining);
            }
            else
            {
                Program.notifyIcon1.Text = string.Format(Program.rm_Messages.GetString("notifyIcon_text_debug"), Program.rm_Messages.GetString("RUNNING"),
                    timeRemaining);
            }

            if (secondsElapsed == maximumSeconds - 600)
            {
                Program.notifyIcon1.ShowBalloonTip(10000, "CSTime", Program.rm_Messages.GetString("tenMinutesRemainingNotification_text"),
                        ToolTipIcon.Info);
            }

            if (!overtimeC && !overtimeY && secondsElapsed >= maximumSeconds)
            {
                if (timesOutEvent)
                {
                    Program.logger.Log(className, "The computer time is out", Logger.LogType.Info);

                    Program.balloonClickAction = Program.BalloonClickActions.SetOvertime;
                    Program.notifyIcon1.ShowBalloonTip(10000, "CSTime", Program.rm_Messages.GetString("timeIsOutNotification_text"),
                        ToolTipIcon.Info);
                    timesOutEvent = false;
                    overtimeC = true;
                }
                else if (((decimal)secondsElapsed / 60) % 1 == 0)
                {
                    Program.balloonClickAction = Program.BalloonClickActions.SetOvertime;
                    Program.notifyIcon1.ShowBalloonTip(10000, "CSTime", Program.rm_Messages.GetString("timeIsOutNotification_text"),
                        ToolTipIcon.Info);
                }
            }

            if (overtimeC && maximumSecondsOvertime > 0 && secondsElapsed >= maximumSeconds + maximumSecondsOvertime)
            {
                if (timesOutOvertimeEvent)
                {
                    Program.logger.Log(className, "The overtime of the computer time is out", Logger.LogType.Info);

                    Program.notifyIcon1.ShowBalloonTip(10000, "CSTime", Program.rm_Messages.GetString("overtimeIsOutNotification_text"),
                        ToolTipIcon.Info);

                    timesOutOvertimeEvent = false;
                }
                else if (((decimal)secondsElapsed / 60) % 1 == 0)
                {
                    Program.notifyIcon1.ShowBalloonTip(10000, "CSTime", Program.rm_Messages.GetString("overtimeIsOutNotification_text"),
                        ToolTipIcon.Info);
                }
            }
        }

        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                Program.logger.Log(className, "Computer locked, pausing timer", Logger.LogType.Info);

                PauseTimer(true, false);
            }
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                Program.logger.Log(className, "Computer unlocked, resuming timer", Logger.LogType.Info);

                ResumeTimer(true, false);
            }
        }

        public void PauseTimer(bool updateGUI, bool log = true)
        {
            if (log)
                Program.logger.Log(className, "Pausing timer", Logger.LogType.Info);

            timer.Stop();
            if (!Program.debug)
            {
                Program.notifyIcon1.Text = string.Format(Program.rm_Messages.GetString("notifyIcon_text"), Program.rm_Messages.GetString("PAUSED"),
                    timeRemaining);
            }
            else
            {
                Program.notifyIcon1.Text = string.Format(Program.rm_Messages.GetString("notifyIcon_text_debug"), Program.rm_Messages.GetString("PAUSED"),
                    timeRemaining);
            }

            Program.contextMenu1.MenuItems[0].Text = Program.rm_GUI.GetString("resume");
            if (updateGUI)
            {
                button3.Image = Properties.Resources.play_97626_640;
                toolTip1.SetToolTip(button3, Program.rm_GUI.GetString("resume"));
            }
        }

        public void ResumeTimer(bool updateGUI, bool log = true)
        {
            if (log)
                Program.logger.Log(className, "Resuming timer", Logger.LogType.Info);

            timer.Start();
            if (!Program.debug)
            {
                Program.notifyIcon1.Text = string.Format(Program.rm_Messages.GetString("notifyIcon_text"), Program.rm_Messages.GetString("RUNNING"),
                    timeRemaining);
            }
            else
            {
                Program.notifyIcon1.Text = string.Format(Program.rm_Messages.GetString("notifyIcon_text_debug"), Program.rm_Messages.GetString("RUNNING"),
                    timeRemaining);
            }
            Program.contextMenu1.MenuItems[0].Text = Program.rm_GUI.GetString("pause");
            if (updateGUI)
            {
                button3.Image = Properties.Resources.pause_97625_640;
                toolTip1.SetToolTip(button3, Program.rm_GUI.GetString("pause"));
            }
        }

        private void reloadEveryDayTimer_Tick(object sender, EventArgs e)
        {
            if (startDateTime.Date != DateTime.Now.Date)
            {
                Program.logger.Log(className, "Auto-reload running", Logger.LogType.Info);
                if (Properties.Settings.Default.timerEnabled)
                {
                    Properties.Settings.Default.timerPaused = !timer.Enabled;
                }
                Properties.Settings.Default.Save();

                Program.Reload();
            }
        }

        private void button_quit_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(Program.rm_Messages.GetString("quitConfirmationDialog_text"), "CSTime",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void button_fwrdRwndTime_Click(object sender, EventArgs e)
        {
            if (Program.addSubtractTime == null)
            {
                Program.addSubtractTime = new AddSubtractTime();
            }

            Program.addSubtractTime.ShowDialog();
        }

        private void CSTime_HelpButtonClicked(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            if (MessageBox.Show("Open GitHub page?", "CSTime", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                == DialogResult.Yes)
            {
                Process.Start("https://github.com/steel9/CSTime");
            }
        }
    }
}
