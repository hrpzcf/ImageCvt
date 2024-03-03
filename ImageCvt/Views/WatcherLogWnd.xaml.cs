using System.Windows;

namespace ImageCvt
{
    public partial class WatcherLogWnd : Window
    {
        public WatcherLogWnd(FileWatcherModel model)
        {
            this.DataContext = model;
            this.InitializeComponent();
        }
    }
}
