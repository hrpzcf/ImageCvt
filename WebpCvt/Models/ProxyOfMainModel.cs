namespace WebpCvt
{
    public class ProxyOfMainModel : NotifiableModelBase
    {
        private MainWindowModel mainModelProxy = new MainWindowModel();

        public MainWindowModel MainModelProxy
        {
            get => this.mainModelProxy;
            set => this.SetPropNotify(ref this.mainModelProxy, value);
        }
    }
}
