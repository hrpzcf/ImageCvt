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
        private bool closeByTrayMenu = false;

        public static MainWindow This { get; private set; }

        public MainWindow()
        {
            This = this;
            this.Closed += this.MainWindowClosed;
            this.Closing += this.MainWindowClosing;
            this.InitializeComponent();
            this.InitializeNotifyIconAndWatchers();
        }

        private void DisplayWndClick(object sender, EventArgs e)
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

        private void ExitApplicationClick(object sender, EventArgs e)
        {
            this.closeByTrayMenu = true;
            this.Close();
        }

        private void SetupNotifyIcon()
        {
            this.trayIcon = new Forms.NotifyIcon();
            this.trayIcon.DoubleClick += this.DisplayWndClick;
            this.trayIcon.ContextMenuStrip = new Forms.ContextMenuStrip();
            this.trayIcon.Text = "ImageCvt";
            Forms.ToolStripMenuItem displayWnd = new Forms.ToolStripMenuItem();
            displayWnd.Text = "显示窗口";
            displayWnd.Click += this.DisplayWndClick;
            Forms.ToolStripMenuItem exitApplication = new Forms.ToolStripMenuItem();
            exitApplication.Text = "退出";
            exitApplication.Click += this.ExitApplicationClick;
            this.trayIcon.ContextMenuStrip.Items.Add(displayWnd);
            this.trayIcon.ContextMenuStrip.Items.Add(exitApplication);
            StreamResourceInfo streamInfo =
                Application.GetResourceStream(new Uri(@"\Images\icon.ico", UriKind.Relative));
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

        private void MainWindowClosed(object sender, EventArgs e)
        {
            this.RemoveNotifyIcon();
            foreach (FileWatcherModel model in ConfigHelper.Current.MainModelProxy.Watchers)
            {
                model.NotStoppedBeforeExiting = model.IsEnabled;
                model.EnableWatcher(false);
                model.DisposeFileWatcherModel();
            }
        }

        private void MainWindowClosing(object sender, CancelEventArgs e)
        {
            if (!this.closeByTrayMenu && ConfigHelper.Current.MainModelProxy.HideClosingWndToTray)
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
