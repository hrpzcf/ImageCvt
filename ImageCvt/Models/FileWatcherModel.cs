using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;

namespace ImageCvt
{
    public class FileWatcherModel : NotifiableModelBase
    {
        private bool isEnabled = false;
        private string remark = string.Empty;
        private string targetDir = string.Empty;
        private string outputDir = string.Empty;
        private int quality = 90;
        private FileFmt outFormat = FileFmt.JPG;
        private CompMode compLevel = CompMode.Quality;
        private bool autoDeleteOriginOnSucceeded = false;
        private bool watchJpg = false;
        private bool watchPng = false;
        private bool watchWebp = true;
        private bool watchGif = false;
        private readonly Timer filesChangedTimer;
        private readonly List<ParamPackage> packagesForTimer;
        private readonly object packagesForTimerLock = new object();
        private readonly FileSystemWatcher watcher = new FileSystemWatcher();
        private ObservableCollection<ParamPackage> processedPictures =
            new ObservableCollection<ParamPackage>();
        private BlockingCollection<ParamPackage> paramPackageCollection;
        private int succeededCount = 0;
        private Window hostWindow = null;
        private string beingProcessedName = null;
        private ParamPackage selectedParamPackage = null;
        private RelayCommand enableWatcherCmd;
        private RelayCommand openLogWindowCmd;
        private RelayCommand selectTargetDirCmd;
        private RelayCommand selectOutputDirCmd;
        private RelayCommand clearLogItemsCmd;
        private RelayCommand deleteConvertedFilesCmd;
        private RelayCommand openInFileLocationCmd;
        private RelayCommand openOutFileLocationCmd;
        private RelayCommand deleteInFileCmd;
        private RelayCommand deleteOutFileCmd;
        private RelayCommand deleteConvertionLogCmd;

        private readonly List<string> excludedFileFullPaths = new List<string>();

        [DllImport(ConfigHelper.libImgCvt, CallingConvention = CallingConvention.Cdecl)]
        private static extern int ConvertImageFormat(string inFile, FileFmt inFmt, string outFile,
            FileFmt outFmt, int fit_dyn, CompMode level, int quality, byte[] error);

        public FileWatcherModel()
        {
            this.watcher.Error += new ErrorEventHandler(this.WatcherError);
            this.watcher.Created += new FileSystemEventHandler(this.OnFileCreated);
            this.watcher.Renamed += new RenamedEventHandler(this.OnFileRenamed);
            this.watcher.Changed += new FileSystemEventHandler(this.OnFileChanged);
            this.watcher.IncludeSubdirectories = true;
            this.watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
            this.packagesForTimer = new List<ParamPackage>();
            this.filesChangedTimer = new Timer(this.ChangedOrRenamedProc);
        }

        [XmlIgnore]
        public Window HostWindow
        {
            get => this.hostWindow ?? MainWindow.This;
            set => this.hostWindow = value;
        }

        [XmlIgnore]
        public bool IsEnabled
        {
            get => this.isEnabled;
            set => this.SetPropNotify(ref this.isEnabled, value);
        }

        public string Remark
        {
            get => this.remark;
            set => this.SetPropNotify(ref this.remark, value);
        }

        public string TargetDir
        {
            get
            {
                return this.targetDir;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    this.SetPropNotify(ref this.targetDir, null);
                }
                else
                {
                    this.SetPropNotify(ref this.targetDir, value.Trim());
                }
            }
        }

        public string OutputDir
        {
            get
            {
                return this.outputDir;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    this.SetPropNotify(ref this.outputDir, null);
                }
                else
                {
                    this.SetPropNotify(ref this.outputDir, value.Trim());
                }
            }
        }

        public bool WatchJpg
        {
            get => this.watchJpg;
            set => this.SetPropNotify(ref this.watchJpg, value);
        }

        public bool WatchPng
        {
            get => this.watchPng;
            set => this.SetPropNotify(ref this.watchPng, value);
        }

        public bool WatchWebp
        {
            get => this.watchWebp;
            set => this.SetPropNotify(ref this.watchWebp, value);
        }

        public bool WatchGif
        {
            get => this.watchGif;
            set => this.SetPropNotify(ref this.watchGif, value);
        }

        public int Quality
        {
            get => this.quality;
            set
            {
                if (value < 1)
                {
                    value = 1;
                }
                else if (value > 100)
                {
                    value = 100;
                }
                this.SetPropNotify(ref this.quality, value);
            }
        }

        public FileFmt OutFormat
        {
            get => this.outFormat;
            set => this.SetPropNotify(ref this.outFormat, value);
        }

        public CompMode CompLevel
        {
            get => this.compLevel;
            set => this.SetPropNotify(ref this.compLevel, value);
        }

        public bool NotStoppedBeforeExiting { get; set; }

        public bool AutoDeleteOriginOnSucceeded
        {
            get => this.autoDeleteOriginOnSucceeded;
            set => this.SetPropNotify(ref this.autoDeleteOriginOnSucceeded, value);
        }

        [XmlIgnore]
        public string BeingProcessedName
        {
            get => this.beingProcessedName;
            set => this.SetPropNotify(ref this.beingProcessedName, value);
        }

        [XmlIgnore]
        public ParamPackage SelectedParamPackage
        {
            get => this.selectedParamPackage;
            set => this.SetPropNotify(ref this.selectedParamPackage, value);
        }

        [XmlIgnore]
        public ObservableCollection<ParamPackage> ProcessedPictures
        {
            get => this.processedPictures;
            set => this.processedPictures = value;
        }

        public FileWatcherModel CopyModel()
        {
            return new FileWatcherModel()
            {
                Remark = this.Remark,
                TargetDir = this.TargetDir,
                OutputDir = this.OutputDir,
                WatchJpg = this.WatchJpg,
                WatchPng = this.WatchPng,
                WatchWebp = this.WatchWebp,
                WatchGif = this.WatchGif,
                Quality = this.Quality,
                OutFormat = this.OutFormat,
                CompLevel = this.CompLevel,
                ProcessedPictures = this.ProcessedPictures,
                AutoDeleteOriginOnSucceeded = this.AutoDeleteOriginOnSucceeded,
                IsEnabled = false,
            };
        }

        public void DisposeFileWatcherModel()
        {
            this.watcher.Dispose();
            this.filesChangedTimer?.Dispose();
        }

        private static bool CheckFileAccess(string path)
        {
            try
            {
                using (FileStream fs = new FileStream(
                    path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    return true;
                }
            }
            catch (Exception)
            {
                return default(bool);
            }
        }

        private static bool OpenFolderAndSelectItem(string path)
        {
            if (string.IsNullOrEmpty(path) || !Path.IsPathRooted(path))
            {
                return false;
            }
            path = path.Replace("/", "\\");
            SHELL32.SHParseDisplayName(
                path, IntPtr.Zero, out IntPtr nativePath, 0U, out _);
            if (nativePath == IntPtr.Zero)
            {
                return false;
            }
            int intResult = SHELL32.SHOpenFolderAndSelectItems(nativePath, 0U, null, 0U);
            Marshal.FreeCoTaskMem(nativePath);
            return 0 == intResult;
        }

        private void ChangedOrRenamedProc(object state)
        {
            lock (this.packagesForTimerLock)
            {
                try
                {
                    foreach (ParamPackage package in this.packagesForTimer)
                    {
                        string ext = Path.GetExtension(package.Name);
                        package.TargetDir = this.TargetDir;
                        package.OutputDir = this.OutputDir;
                        package.Quality = this.Quality;
                        package.OutFormat = this.OutFormat;
                        package.CompLevel = this.CompLevel;
                        package.AutoDeleteOriginOnSucceeded = this.AutoDeleteOriginOnSucceeded;
                        bool matchedExtension = false;
                        if (this.WatchJpg && (
                            ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                            ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)))
                        {
                            matchedExtension = true;
                            package.InFormat = FileFmt.JPG;
                        }
                        else if (this.WatchPng && ext.Equals(".png", StringComparison.OrdinalIgnoreCase))
                        {
                            matchedExtension = true;
                            package.InFormat = FileFmt.PNG;
                        }
                        else if (this.WatchWebp && ext.Equals(".webp", StringComparison.OrdinalIgnoreCase))
                        {
                            matchedExtension = true;
                            package.InFormat = FileFmt.WEBP;
                        }
                        else if (this.WatchGif && ext.Equals(".gif", StringComparison.OrdinalIgnoreCase))
                        {
                            matchedExtension = true;
                            package.InFormat = FileFmt.GIF;
                        }
                        if (matchedExtension && CheckFileAccess(package.FullPath))
                        {
                            this.paramPackageCollection?.Add(package);
                        }
                    }
                }
                catch (InvalidOperationException)
                {
                    // 此异常是 ObjectDisposedException 的父类
                }
                finally
                {
                    this.packagesForTimer.Clear();
                }
            }
        }

        private void GenericForFileChanged(string name, string fullPath)
        {
            lock (this.packagesForTimerLock)
            {
                if (!this.packagesForTimer.ContainsPath(fullPath))
                {
                    bool excludedFile = false;
                    foreach (string excluded in this.excludedFileFullPaths)
                    {
                        if (excluded.Equals(fullPath, StringComparison.OrdinalIgnoreCase))
                        {
                            excludedFile = true;
                            break;
                        }
                    }
                    if (!excludedFile)
                    {
                        this.packagesForTimer.Add(new ParamPackage(name, fullPath));
                        this.filesChangedTimer.Change(1000, Timeout.Infinite);
                    }
                }
            }
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            this.GenericForFileChanged(e.Name, e.FullPath);
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            this.GenericForFileChanged(e.Name, e.FullPath);
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            this.GenericForFileChanged(e.Name, e.FullPath);
        }

        private void WatcherError(object sender, ErrorEventArgs e)
        {
            this.EnableWatcher(false);
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(this.HostWindow, $"监视任务{this.Remark}出现异常：{e.GetException().Message}",
                    "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        private string CheckWatcherParams()
        {
            if (string.IsNullOrWhiteSpace(this.TargetDir))
            {
                return "要监视的目录路径不能为空！";
            }
            else if (!Path.IsPathRooted(this.TargetDir))
            {
                return "要监视的目录路径需要是一个完整路径！";
            }
            else if (!Directory.Exists(this.TargetDir))
            {
                return "要监视的目录不存在，请选择有效的目录！";
            }
            if (!(this.WatchJpg || this.WatchPng || this.WatchWebp || this.WatchGif))
            {
                return "没有选择任何监视类型，请选择监视类型！";
            }
            if (!string.IsNullOrWhiteSpace(this.OutputDir))
            {
                if (!Path.IsPathRooted(this.OutputDir))
                {
                    return "保存输出文件的目录路径需要是一个完整路径！";
                }
                else if (!Directory.Exists(this.OutputDir))
                {
                    return $"保存输出文件的目录不存在，请选择有效的目录！";
                }
            }
            return default(string);
        }

        private bool ConvertToFormat(ParamPackage paramPackage)
        {
            try
            {
                string outFileExt;
                switch (paramPackage.OutFormat)
                {
                    case FileFmt.JPG:
                        outFileExt = ".jpg";
                        break;
                    case FileFmt.PNG:
                        outFileExt = ".png";
                        break;
                    case FileFmt.WEBP:
                        outFileExt = ".webp";
                        break;
                    case FileFmt.GIF:
                        outFileExt = ".gif";
                        break;
                    default:
                        paramPackage.ReasonForFailure = "未知输出格式";
                        return false;
                }
                string nameNoExt = Path.GetFileNameWithoutExtension(paramPackage.Name);
                string outdir = string.IsNullOrWhiteSpace(paramPackage.OutputDir) ?
                    Path.GetDirectoryName(paramPackage.FullPath) : paramPackage.OutputDir;
                if (!Directory.Exists(outdir))
                {
                    Directory.CreateDirectory(outdir);
                }
                int duplicate = -1;
                do
                {
                    paramPackage.NewName = ++duplicate == 0 ?
                        $"{nameNoExt}{outFileExt}" : $"{nameNoExt}_{duplicate}{outFileExt}";
                    paramPackage.NewFullPath = Path.Combine(outdir, paramPackage.NewName);
                } while (File.Exists(paramPackage.NewFullPath));
                lock (this.packagesForTimerLock)
                {
                    this.excludedFileFullPaths.Add(paramPackage.NewFullPath);
                }
                byte[] error = new byte[256];
                paramPackage.Result = ConvertImageFormat(paramPackage.FullPath, paramPackage.InFormat,
                    paramPackage.NewFullPath, paramPackage.OutFormat, fit_dyn: 0, paramPackage.CompLevel,
                    paramPackage.Quality, error) != 0;
                if (!paramPackage.Result)
                {
                    if (File.Exists(paramPackage.NewFullPath))
                    {
                        try
                        {
                            File.Delete(paramPackage.NewFullPath);
                        }
                        catch (Exception) { }
                    }
                    try
                    {
                        paramPackage.ReasonForFailure = Encoding.UTF8.GetString(error);
                    }
                    catch (Exception) { }
                    paramPackage.NewName = paramPackage.NewFullPath = null;
                }
            }
            catch (Exception ex)
            {
                paramPackage.Result = false;
                paramPackage.ReasonForFailure = ex.Message;
            }
            return paramPackage.Result == true;
        }

        private void StartConsumer(BlockingCollection<ParamPackage> paramPackageCollection)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    foreach (ParamPackage param in paramPackageCollection.GetConsumingEnumerable())
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            this.BeingProcessedName = param.Name;
                        });
                        bool succeeded = this.ConvertToFormat(param);
                        param.FinishTime = DateTime.Now;
                        void PictureProcessed()
                        {
                            this.BeingProcessedName = null;
                            this.ProcessedPictures.Add(param);
                            if (succeeded)
                            {
                                if (param.AutoDeleteOriginOnSucceeded && File.Exists(param.FullPath))
                                {
                                    try
                                    {
                                        File.Delete(param.FullPath);
                                        param.InFileDeleted = true;
                                    }
                                    catch (Exception) { }
                                }
                                ++this.SucceededCount;
                            }
                        };
                        Application.Current.Dispatcher.Invoke(PictureProcessed);
                    }
                }
                catch (ObjectDisposedException)
                {
                }
                finally
                {
                    paramPackageCollection.Dispose();
                }
            });
        }

        public void EnableWatcher(bool enabled)
        {
            if (!enabled)
            {
                this.IsEnabled = false;
                this.watcher.EnableRaisingEvents = false;
                this.paramPackageCollection?.CompleteAdding();
                this.paramPackageCollection = null;
                lock (this.packagesForTimerLock)
                {
                    this.excludedFileFullPaths.Clear();
                }
            }
            else
            {
                string reasonForFailure = this.CheckWatcherParams();
                if (string.IsNullOrEmpty(reasonForFailure))
                {
                    this.IsEnabled = true;
                    this.paramPackageCollection = new BlockingCollection<ParamPackage>();
                    this.StartConsumer(this.paramPackageCollection);
                    this.watcher.Path = this.TargetDir;
                    this.watcher.EnableRaisingEvents = true;
                }
                else
                {
                    this.IsEnabled = false;
                    MessageBox.Show(this.HostWindow, reasonForFailure, "提示", MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
        }

        [XmlIgnore]
        public int SucceededCount
        {
            get => this.succeededCount;
            set => this.SetPropNotify(ref this.succeededCount, value);
        }

        [XmlIgnore]
        public ICommand EnableWatcherCmd
        {
            get
            {
                if (this.enableWatcherCmd == null)
                {
                    this.enableWatcherCmd = new RelayCommand(o => { this.EnableWatcher(this.IsEnabled); });
                }
                return this.enableWatcherCmd;
            }
        }

        private void OpenLogWindowAction(object param)
        {
            WatcherLogWnd logWindow = new WatcherLogWnd(this)
            {
                Owner = MainWindow.This,
            };
            logWindow.ShowDialog();
        }

        [XmlIgnore]
        public ICommand OpenLogWindowCmd
        {
            get
            {
                if (this.openLogWindowCmd == null)
                {
                    this.openLogWindowCmd = new RelayCommand(this.OpenLogWindowAction);
                }
                return this.openLogWindowCmd;
            }
        }

        private void SelectTargetDirAction(object param)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog =
                new System.Windows.Forms.FolderBrowserDialog()
                {
                    Description = "选择要监视的目录。",
                    ShowNewFolderButton = true,
                    SelectedPath = this.TargetDir ?? string.Empty,
                };
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.TargetDir = folderBrowserDialog.SelectedPath;
            }
        }

        [XmlIgnore]
        public ICommand SelectTargetDirCmd
        {
            get
            {
                if (this.selectTargetDirCmd == null)
                {
                    this.selectTargetDirCmd = new RelayCommand(this.SelectTargetDirAction);
                }
                return this.selectTargetDirCmd;
            }
        }

        private void SelectOutputDirAction(object param)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog =
                new System.Windows.Forms.FolderBrowserDialog()
                {
                    Description = "选择转换后的文件的保存目录。",
                    ShowNewFolderButton = true,
                    SelectedPath = this.OutputDir ?? this.TargetDir ?? string.Empty,
                };
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.OutputDir = folderBrowserDialog.SelectedPath;
            }
        }

        [XmlIgnore]
        public ICommand SelectOutputDirCmd
        {
            get
            {
                if (this.selectOutputDirCmd == null)
                {
                    this.selectOutputDirCmd = new RelayCommand(this.SelectOutputDirAction);
                }
                return this.selectOutputDirCmd;
            }
        }

        private void ClearLogItemsAction(object param)
        {
            this.SucceededCount = 0;
            this.ProcessedPictures.Clear();
        }

        [XmlIgnore]
        public ICommand ClearLogItemsCmd
        {
            get
            {
                if (this.clearLogItemsCmd == null)
                {
                    this.clearLogItemsCmd = new RelayCommand(this.ClearLogItemsAction);
                }
                return this.clearLogItemsCmd;
            }
        }

        private void DeleteConvertedFilesAction(object param)
        {
            if (MessageBox.Show(this.HostWindow, "确定要删除所有已经转换成功的原文件吗？", "提示",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                // 对 ProcessedPictures 的操作都在主线程，不用加锁
                foreach (ParamPackage package in this.ProcessedPictures.Where(i => i.Result))
                {
                    try
                    {
                        File.Delete(package.FullPath);
                        package.InFileDeleted = true;
                    }
                    catch (Exception)
                    {
                    }
                }
                MessageBox.Show(this.HostWindow, "转换成功的原文件已经被删除！", "提示", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        [XmlIgnore]
        public ICommand DeleteConvertedFilesCmd
        {
            get
            {
                if (this.deleteConvertedFilesCmd == null)
                {
                    this.deleteConvertedFilesCmd = new RelayCommand(this.DeleteConvertedFilesAction);
                }
                return this.deleteConvertedFilesCmd;
            }
        }

        private void OpenInFileLocationAction(object param)
        {
            if (this.SelectedParamPackage != null)
            {
                OpenFolderAndSelectItem(this.SelectedParamPackage.FullPath);
            }
        }

        [XmlIgnore]
        public ICommand OpenInFileLocationCmd
        {
            get
            {
                if (this.openInFileLocationCmd == null)
                {
                    this.openInFileLocationCmd = new RelayCommand(this.OpenInFileLocationAction);
                }
                return this.openInFileLocationCmd;
            }
        }

        private void OpenOutFileLocationAction(object param)
        {
            if (this.SelectedParamPackage != null)
            {
                OpenFolderAndSelectItem(this.SelectedParamPackage.NewFullPath);
            }
        }

        [XmlIgnore]
        public ICommand OpenOutFileLocationCmd
        {
            get
            {
                if (this.openOutFileLocationCmd == null)
                {
                    this.openOutFileLocationCmd = new RelayCommand(this.OpenOutFileLocationAction);
                }
                return this.openOutFileLocationCmd;
            }
        }

        private void DeleteInFileAction(object param)
        {
            if (this.SelectedParamPackage != null &&
                MessageBox.Show(this.HostWindow,
                $"确定要删除所选记录的【原文件】吗？\n{this.SelectedParamPackage.FullPath}", "提示",
                MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                try
                {
                    File.Delete(this.SelectedParamPackage.FullPath);
                    this.SelectedParamPackage.InFileDeleted = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this.HostWindow, $"文件删除失败：\n{ex.Message}", "提示", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        [XmlIgnore]
        public ICommand DeleteInFileCmd
        {
            get
            {
                if (this.deleteInFileCmd == null)
                {
                    this.deleteInFileCmd = new RelayCommand(this.DeleteInFileAction);
                }
                return this.deleteInFileCmd;
            }
        }

        private void DeleteOutFileAction(object param)
        {
            if (this.SelectedParamPackage != null &&
                MessageBox.Show(this.HostWindow,
                $"确定要删除所选记录的【新文件】吗？\n{this.SelectedParamPackage.NewFullPath}", "提示",
                MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                try
                {
                    File.Delete(this.SelectedParamPackage.NewFullPath);
                    this.SelectedParamPackage.OutFileDeleted = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this.HostWindow, $"文件删除失败：\n{ex.Message}", "提示", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        [XmlIgnore]
        public ICommand DeleteOutFileCmd
        {
            get
            {
                if (this.deleteOutFileCmd == null)
                {
                    this.deleteOutFileCmd = new RelayCommand(this.DeleteOutFileAction);
                }
                return this.deleteOutFileCmd;
            }
        }

        private void DeleteConvertionLogAction(object param)
        {
            if (this.SelectedParamPackage != null)
            {
                this.ProcessedPictures.Remove(this.SelectedParamPackage);
            }
        }

        [XmlIgnore]
        public ICommand DeleteConvertionLogCmd
        {
            get
            {
                if (this.deleteConvertionLogCmd == null)
                {
                    this.deleteConvertionLogCmd = new RelayCommand(this.DeleteConvertionLogAction);
                }
                return this.deleteConvertionLogCmd;
            }
        }
    }
}
