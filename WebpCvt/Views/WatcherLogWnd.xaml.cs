using System.Windows;

namespace WebpCvt
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
