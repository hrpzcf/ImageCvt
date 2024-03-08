using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Resources;
using Forms = System.Windows.Forms;

namespace ImageCvt
{
    public partial class MainWindow : Window
    {
        private Forms.NotifyIcon trayIcon;
        private bool closeImmediately = false;

        public static MainWindow This { get; private set; }

        public MainWindow()
        {
            This = this;
            this.Closed += this.MainWindowClosed;
            this.Closing += this.MainWindowClosing;
            Application.Current.SessionEnding += this.ApplicationSessionEnding;
            this.InitializeComponent();
            this.InitializeNotifyIconAndWatchers();
        }

        private void DisplayWindowClick(object sender, EventArgs e)
        {
            if (!this.IsVisible)
            {
                this.Show();
            }
            if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
            }
            if (!this.IsActive)
            {
                this.Activate();
            }
        }

        private void EnableWatchersClick(object sender, EventArgs e)
        {
            foreach (FileWatcherModel model in ConfigHelper.Current.MainModelProxy.Watchers)
            {
                if (!model.IsEnabled)
                {
                    model.EnableWatcher(true);
                }
            }
        }

        private void DisableWatchersClick(object sender, EventArgs e)
        {
            foreach (FileWatcherModel model in ConfigHelper.Current.MainModelProxy.Watchers)
            {
                if (model.IsEnabled)
                {
                    model.EnableWatcher(false);
                }
            }
        }

        private void ExitApplicationClick(object sender, EventArgs e)
        {
            this.closeImmediately = true;
            this.Close();
        }

        private void SetupNotifyIcon()
        {
            this.trayIcon = new Forms.NotifyIcon();
            this.trayIcon.DoubleClick += this.DisplayWindowClick;
            this.trayIcon.ContextMenuStrip = new Forms.ContextMenuStrip();
            this.trayIcon.Text = "ImageCvt";

            var displayWindow = new Forms.ToolStripMenuItem();
            displayWindow.Text = "显示窗口";
            displayWindow.Click += this.DisplayWindowClick;
            this.trayIcon.ContextMenuStrip.Items.Add(displayWindow);
            this.trayIcon.ContextMenuStrip.Items.Add(new Forms.ToolStripSeparator());

            var enableWatchers = new Forms.ToolStripMenuItem();
            enableWatchers.Text = "启动所有任务";
            enableWatchers.Click += this.EnableWatchersClick;
            this.trayIcon.ContextMenuStrip.Items.Add(enableWatchers);
            this.trayIcon.ContextMenuStrip.Items.Add(new Forms.ToolStripSeparator());

            var disableWatchers = new Forms.ToolStripMenuItem();
            disableWatchers.Text = "停止所有任务";
            disableWatchers.Click += this.DisableWatchersClick;
            this.trayIcon.ContextMenuStrip.Items.Add(disableWatchers);
            this.trayIcon.ContextMenuStrip.Items.Add(new Forms.ToolStripSeparator());

            var exitApplication = new Forms.ToolStripMenuItem();
            exitApplication.Text = "退出软件";
            exitApplication.Click += this.ExitApplicationClick;
            this.trayIcon.ContextMenuStrip.Items.Add(exitApplication);

            StreamResourceInfo streamInfo = Application.GetResourceStream(
                new Uri(@"\Images\icon.ico", UriKind.Relative));
            using (streamInfo.Stream)
            {
                this.trayIcon.Icon = new Icon(streamInfo.Stream);
            }
            this.trayIcon.Visible = true;
        }

        private void RemoveNotifyIcon()
        {
            this.trayIcon?.Dispose();
        }

        private void Finalization()
        {
            foreach (FileWatcherModel model in ConfigHelper.Current.MainModelProxy.Watchers)
            {
                model.NotStoppedBeforeExiting = model.IsEnabled;
                model.EnableWatcher(false);
                model.DisposeFileWatcherModel();
            }
        }

        private void ApplicationSessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            e.Cancel = true;
            this.Finalization();
            ConfigHelper.SaveConfig();
        }

        private void MainWindowClosed(object sender, EventArgs e)
        {
            this.Finalization();
            this.RemoveNotifyIcon();
        }

        private void MainWindowClosing(object sender, CancelEventArgs e)
        {
            if (!this.closeImmediately && ConfigHelper.Current.MainModelProxy.HideClosingWndToTray)
            {
                this.Hide();
                e.Cancel = true;
            }
        }

        private void InitializeNotifyIconAndWatchers()
        {
            this.SetupNotifyIcon();
            foreach (FileWatcherModel model in ConfigHelper.Current.MainModelProxy.Watchers)
            {
                if (model.NotStoppedBeforeExiting && ConfigHelper.Current.MainModelProxy.AutoStartTasksNotStopped)
                {
                    model.EnableWatcher(true);
                }
                model.NotStoppedBeforeExiting = false;
            }
        }
    }
}
