using System.Windows;

namespace ImageCvt
{
    public partial class WatcherLogWnd : Window
    {
        public WatcherLogWnd(FileWatcherModel model)
        {
            model.HostWindow = this;
            this.DataContext = model;
            this.InitializeComponent();
        }
    }
}
