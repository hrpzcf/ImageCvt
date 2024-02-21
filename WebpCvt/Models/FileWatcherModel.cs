using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;

namespace WebpCvt
{
    public class FileWatcherModel : NotifiableModelBase
    {
        private bool isEnabled;
        private string remark;
        private string targetDir;
        private string outputDir;
        private OutType outType;
        private const string supportedExt = ".webp";
        private readonly Timer fileChangedTimer;
        private List<ParamPackage> packagesForTimer;
        private readonly object pathsForTimerLock = new object();
        private BlockingCollection<ParamPackage> paramPackageCollection;
        private readonly FileSystemWatcher watcher = new FileSystemWatcher();
        private ObservableCollection<ParamPackage> processedWebpFiles =
            new ObservableCollection<ParamPackage>();
        private int succeededCount = 0;
        private ParamPackage selectedParamPackage = null;
        private RelayCommand enableWatcherCmd;
        private RelayCommand openLogWindowCmd;
        private RelayCommand selectTargetDirCmd;
        private RelayCommand selectOutputDirCmd;
        private RelayCommand clearLogItemsCmd;
        private RelayCommand deleteConvertedFilesCmd;

        [DllImport(ConfigHelper.LibWebpCvt, CallingConvention = CallingConvention.Cdecl)]
        private static extern int WebpConvertTo(string inFile, string outFile, int outFormat, ref int reason);

        [DllImport(ConfigHelper.LibWebpCvt, CallingConvention = CallingConvention.Cdecl)]
        private static extern int WebpConvertToJpeg(string in_file, string out_file, int quality, ref int reason);

        public FileWatcherModel()
        {
            this.watcher.IncludeSubdirectories = true;
            this.watcher.Error += new ErrorEventHandler(this.WatcherError);
            this.watcher.Renamed += new RenamedEventHandler(this.OnFileRenamed);
            this.watcher.Changed += new FileSystemEventHandler(this.OnFileChanged);
            this.watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
            this.packagesForTimer = new List<ParamPackage>();
            this.fileChangedTimer = new Timer(this.ChangedOrRenamedProc);
        }

        [XmlIgnore]
        private static string[] Reasons { get; } = new string[]
        {
            string.Empty,
            "输出格式参数不正确",
            "输入或输出文件路径为空",
            "初始化 Webp 解码配置失败",
            "读取输入文件失败",
            "获取输入文件特征失败",
            "解码输入文件失败",
            "输出格式不匹配可选的格式",
            "保存输出文件失败",
            "输入或输出文件路径为空",
            "输出 JPEG 图像质量值不正确",
            "读取输入文件失败",
            "解码输入文件失败",
            "创建输出文件失败",
            "生成 JPEG 文件过程失败",
        };

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

        public OutType OutType
        {
            get => this.outType;
            set => this.SetPropNotify(ref this.outType, value);
        }

        public int JpegQuality { get; set; } = 80;

        [XmlIgnore]
        public ParamPackage SelectedParamPackage
        {
            get => this.selectedParamPackage;
            set => this.SetPropNotify(ref this.selectedParamPackage, value);
        }

        [XmlIgnore]
        public ObservableCollection<ParamPackage> ProcessedWebpFiles
        {
            get => this.processedWebpFiles;
            set => this.processedWebpFiles = value;
        }

        public FileWatcherModel CopyModel()
        {
            return new FileWatcherModel()
            {
                Remark = this.Remark,
                TargetDir = this.TargetDir,
                OutputDir = this.OutputDir,
                OutType = this.OutType,
                JpegQuality = this.JpegQuality,
                ProcessedWebpFiles = this.ProcessedWebpFiles,
                IsEnabled = false,
            };
        }

        public void DisposeFileWatcherModel()
        {
            this.watcher.Dispose();
            this.fileChangedTimer?.Dispose();
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

        private void ChangedOrRenamedProc(object state)
        {
            List<ParamPackage> paramPackages;
            lock (this.pathsForTimerLock)
            {
                paramPackages = this.packagesForTimer;
                this.packagesForTimer = new List<ParamPackage>();
            }
            try
            {
                foreach (ParamPackage package in paramPackages)
                {
                    string ext = Path.GetExtension(package.Name);
                    if (ext.Equals(supportedExt, StringComparison.OrdinalIgnoreCase) &&
                        CheckFileAccess(package.FullPath))
                    {
                        package.TargetDir = this.TargetDir;
                        package.OutputDir = this.OutputDir;
                        package.OutType = this.OutType;
                        package.JpegQuality = this.JpegQuality;
                        this.paramPackageCollection.Add(package);
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // 此异常也是 ObjectDisposedException 的父类
            }
        }

        private void GenericForFileChanged(string name, string fullPath)
        {
            lock (this.pathsForTimerLock)
            {
                if (!this.packagesForTimer.ContainsPath(fullPath))
                {
                    this.packagesForTimer.Add(new ParamPackage(name, fullPath));
                }
            }
            this.fileChangedTimer.Change(1000, Timeout.Infinite);
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
                MessageBox.Show(MainWindow.This, $"监视任务{this.Remark}出现异常：{e.GetException().Message}",
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
                return $"要监视的目录不存在，请选择有效的目录！";
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

        private static bool ConvertToOutType(ParamPackage paramPackage)
        {
            try
            {
                string newExt;
                int intFormat = 0;
                switch (paramPackage.OutType)
                {
                    default:
                    case OutType.JPG:
                        newExt = ".jpg";
                        break;
                    case OutType.PNG:
                        newExt = ".png";
                        break;
                    case OutType.BMP:
                        intFormat = 4;
                        newExt = ".bmp";
                        break;
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
                        $"{nameNoExt}{newExt}" : $"{nameNoExt}_{duplicate}{newExt}";
                    paramPackage.NewFullPath = Path.Combine(outdir, paramPackage.NewName);
                } while (File.Exists(paramPackage.NewFullPath));
                int reason = 0;
                if (paramPackage.OutType != OutType.JPG)
                {
                    paramPackage.Result = WebpConvertTo(paramPackage.FullPath, paramPackage.NewFullPath,
                        intFormat, ref reason) != 0;
                }
                else
                {
                    paramPackage.Result = WebpConvertToJpeg(paramPackage.FullPath, paramPackage.NewFullPath,
                        paramPackage.JpegQuality, ref reason) != 0;
                }
                if (!paramPackage.Result)
                {
                    try
                    {
                        File.Delete(paramPackage.NewFullPath);
                        paramPackage.ReasonForFailure = Reasons[reason];
                    }
                    catch (Exception)
                    {
                    }
                    paramPackage.NewName = paramPackage.NewFullPath = string.Empty;
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
                        bool succeeded = ConvertToOutType(param);
                        param.FinishTime = DateTime.Now;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (succeeded) { ++this.SucceededCount; }
                            this.ProcessedWebpFiles.Add(param);
                        });
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
                this.IsEnabled = enabled;
                this.watcher.EnableRaisingEvents = enabled;
                this.paramPackageCollection?.CompleteAdding();
                this.paramPackageCollection = null;
            }
            else
            {
                string reasonForFailure = this.CheckWatcherParams();
                if (string.IsNullOrEmpty(reasonForFailure))
                {
                    this.IsEnabled = enabled;
                    this.paramPackageCollection = new BlockingCollection<ParamPackage>();
                    this.StartConsumer(this.paramPackageCollection);
                    this.watcher.Path = this.TargetDir;
                    this.watcher.EnableRaisingEvents = enabled;
                }
                else
                {
                    MessageBox.Show(MainWindow.This, reasonForFailure, "提示", MessageBoxButton.OK,
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
            this.ProcessedWebpFiles.Clear();
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
            if (MessageBox.Show(MainWindow.This, "确定要删除所有已经转换成功的 .webp 原文件吗？",
                "提示", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                // 对 ProcessedWebpFiles 的操作都在主线程，不用加锁
                foreach (ParamPackage package in this.ProcessedWebpFiles.Where(i => i.Result))
                {
                    try
                    {
                        File.Delete(package.FullPath);
                        package.OriginFileDeleted = true;
                    }
                    catch (Exception)
                    {
                    }
                }
                MessageBox.Show(MainWindow.This, "转换成功的原文件已经被删除！", "提示", MessageBoxButton.OK,
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
    }
}
