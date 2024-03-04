using System;
using System.IO;
using System.Windows;

namespace ImageCvt
{
    public partial class WatcherEditor : Window
    {
        private readonly FileWatcherModel viewModel;
        private readonly Action<FileWatcherModel> callback;

        public WatcherEditor(Action<FileWatcherModel> action)
        {
            this.callback = action;
            this.viewModel = new FileWatcherModel();
            this.DataContext = this.viewModel;
            this.InitializeComponent();
        }

        public WatcherEditor(Action<FileWatcherModel> action, FileWatcherModel model)
        {
            this.callback = action;
            this.viewModel = model == null ? new FileWatcherModel() : model.CopyModel();
            this.DataContext = this.viewModel;
            this.InitializeComponent();
        }

        private void OnButtonCancelClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OnButtonConfirmClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(this.viewModel.TargetDir) &&
                Path.IsPathRooted(this.viewModel.TargetDir))
            {
                this.Close();
                this.callback(this.viewModel);
            }
            else
            {
                MessageBox.Show(this, "请使用完整路径来表示要监视的目录！", "提示", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }
}
