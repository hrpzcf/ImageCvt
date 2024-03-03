using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;

namespace ImageCvt
{
    public class MainWindowModel : NotifiableModelBase
    {
        private double mainWidth = 800.0;
        private double mainHeight = 500.0;
        private double editorWidth = 500.0;
        private double editorHeight = 300.0;
        private double logWndWidth = 600.0;
        private double logWndHeight = 700.0;
        private bool hideToTrayWhenWindowClosed = true;
        private FileWatcherModel selectedWatcher;
        private RelayCommand addWhatcherCmd;
        private RelayCommand editWhatcherCmd;
        private RelayCommand moveWhatcherUpCmd;
        private RelayCommand moveWhatcherDownCmd;
        private RelayCommand removeWatcherCmd;

        public string PreviousVer { get; set; }

        public double MainWidth
        {
            get => this.mainWidth;
            set => this.SetPropNotify(ref this.mainWidth, value);
        }

        public double MainHeight
        {
            get => this.mainHeight;
            set => this.SetPropNotify(ref this.mainHeight, value);
        }

        public double EditorWidth
        {
            get => this.editorWidth;
            set => this.SetPropNotify(ref this.editorWidth, value);
        }

        public double EditorHeight
        {
            get => this.editorHeight;
            set => this.SetPropNotify(ref this.editorHeight, value);
        }

        public double LogWndWidth
        {
            get => this.logWndWidth;
            set => this.SetPropNotify(ref this.logWndWidth, value);
        }

        public double LogWndHeight
        {
            get => this.logWndHeight;
            set => this.SetPropNotify(ref this.logWndHeight, value);
        }

        public bool HideClosingWndToTray
        {
            get => this.hideToTrayWhenWindowClosed;
            set => this.SetPropNotify(ref this.hideToTrayWhenWindowClosed, value);
        }

        [XmlIgnore]
        public FileWatcherModel SelectedWatcher
        {
            get => this.selectedWatcher;
            set => this.SetPropNotify(ref this.selectedWatcher, value);
        }

        public ObservableCollection<FileWatcherModel> Watchers { get; set; } =
            new ObservableCollection<FileWatcherModel>();

        private bool WatcherTargetDirExists(FileWatcherModel newWatcher, FileWatcherModel skipOld)
        {
            foreach (FileWatcherModel watcher in this.Watchers)
            {
                if (watcher != skipOld &&
                    newWatcher.TargetDir.Equals(watcher.TargetDir, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private void AddWhatcherAction(object param)
        {
            Action<FileWatcherModel> AddWatcher = watcher =>
            {
                if (!this.WatcherTargetDirExists(watcher, null))
                {
                    this.Watchers.Add(watcher);
                    this.SelectedWatcher = watcher;
                }
                else
                {
                    MessageBox.Show(MainWindow.This, "列表中已存在相同的监视目录！", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            };
            WatcherEditor watcherEditor = new WatcherEditor(AddWatcher)
            {
                Owner = MainWindow.This,
            };
            watcherEditor.ShowDialog();
        }

        [XmlIgnore]
        public ICommand AddWhatcherCmd
        {
            get
            {
                if (this.addWhatcherCmd == null)
                {
                    this.addWhatcherCmd = new RelayCommand(this.AddWhatcherAction);
                }
                return this.addWhatcherCmd;
            }
        }

        private void EditWhatcherAction(object param)
        {
            int index;
            if (this.SelectedWatcher != null && (index = this.Watchers.IndexOf(this.SelectedWatcher)) != -1)
            {
                FileWatcherModel oldWatcher = this.Watchers[index];
                bool oldWatcherEnabled = oldWatcher.IsEnabled;
                Action<FileWatcherModel> ReplaceWatcher = watcher =>
                {
                    if (!this.WatcherTargetDirExists(watcher, oldWatcher))
                    {
                        this.Watchers[index] = watcher;
                        this.SelectedWatcher = watcher;
                        if (oldWatcherEnabled)
                        {
                            oldWatcher.EnableWatcher(false);
                            watcher.EnableWatcher(oldWatcherEnabled);
                        }
                    }
                    else
                    {
                        MessageBox.Show(MainWindow.This, "列表中已存在相同的监视目录！", "提示",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                };
                WatcherEditor watcherEditor = new WatcherEditor(ReplaceWatcher, this.SelectedWatcher)
                {
                    Owner = MainWindow.This,
                };
                watcherEditor.ShowDialog();
            }
            else
            {
                MessageBox.Show(MainWindow.This, "没有选中任何行！", "提示", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        [XmlIgnore]
        public ICommand EditWhatcherCmd
        {
            get
            {
                if (this.editWhatcherCmd == null)
                {
                    this.editWhatcherCmd = new RelayCommand(this.EditWhatcherAction);
                }
                return this.editWhatcherCmd;
            }
        }

        private void MoveWhatcherUpAction(object param)
        {
            int index;
            if (this.SelectedWatcher != null &&
                (index = this.Watchers.IndexOf(this.SelectedWatcher)) != -1 && index > 0)
            {
                (this.Watchers[index], this.Watchers[index - 1]) = (this.Watchers[index - 1], this.Watchers[index]);
                this.SelectedWatcher = this.Watchers[index - 1];
            }
        }

        [XmlIgnore]
        public ICommand MoveWhatcherUpCmd
        {
            get
            {
                if (this.moveWhatcherUpCmd == null)
                {
                    this.moveWhatcherUpCmd = new RelayCommand(this.MoveWhatcherUpAction);
                }
                return this.moveWhatcherUpCmd;
            }
        }

        private void MoveWhatcherDownAction(object param)
        {
            int index;
            if (this.SelectedWatcher != null &&
                (index = this.Watchers.IndexOf(this.SelectedWatcher)) != -1 && index < this.Watchers.Count - 1)
            {
                (this.Watchers[index], this.Watchers[index + 1]) = (this.Watchers[index + 1], this.Watchers[index]);
                this.SelectedWatcher = this.Watchers[index + 1];
            }
        }

        [XmlIgnore]
        public ICommand MoveWhatcherDownCmd
        {
            get
            {
                if (this.moveWhatcherDownCmd == null)
                {
                    this.moveWhatcherDownCmd = new RelayCommand(this.MoveWhatcherDownAction);
                }
                return this.moveWhatcherDownCmd;
            }
        }

        private void RemoveWatcherAction(object param)
        {
            int index;
            if ((index = this.Watchers.IndexOf(this.SelectedWatcher)) != -1)
            {
                this.Watchers[index].EnableWatcher(false);
                this.Watchers.RemoveAt(index);
                if (index < this.Watchers.Count)
                {
                    this.SelectedWatcher = this.Watchers[index];
                }
                else if (index > 0)
                {
                    this.SelectedWatcher = this.Watchers[index - 1];
                }
                else
                {
                    this.SelectedWatcher = null;
                }
            }
            else
            {
                MessageBox.Show(MainWindow.This, "没有选中任何行！", "提示", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        [XmlIgnore]
        public ICommand RemoveWatcherCmd
        {
            get
            {
                if (this.removeWatcherCmd == null)
                {
                    this.removeWatcherCmd = new RelayCommand(this.RemoveWatcherAction);
                }
                return this.removeWatcherCmd;
            }
        }
    }
}
