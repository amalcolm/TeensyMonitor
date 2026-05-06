using Microsoft.Web.WebView2.Core;

namespace TeensyMonitor.Caldera
{
    public partial class CalderaControl : UserControl
    {
        public CoreWebView2 CoreWebView2 => web.CoreWebView2;

        public Caldera Caldera { get => _caldera; }
        private bool _webInitStarted = false;
        private bool _disposedOrClosing = false;

        private readonly DevServer _devServer = new();
        private readonly Caldera _caldera;

        public CalderaControl()
        {
            InitializeComponent();
            _caldera = new Caldera(this);
        }

        protected override async void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (Program.IsRunning == false || _webInitStarted) return;

            await _devServer.EnsureViteRunningAsync();

            _webInitStarted = true;

            try 
            {
                await InitWebView();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize WebView2: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (!RecreatingHandle && !_disposedOrClosing)
            {
                _disposedOrClosing = true;
                _devServer.StopViteIfStartedByMe();
            }

            base.OnHandleDestroyed(e);
        }

        private async Task InitWebView()
        {
            
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TeensyMonitor",
                "WebView2");

            Directory.CreateDirectory(userDataFolder);

            var env = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: userDataFolder);


            await web.EnsureCoreWebView2Async(env);

            web.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "my.web",
                Path.Combine(BuildPaths.SolutionDir, "Caldera"),
                CoreWebView2HostResourceAccessKind.Allow);


            web.CoreWebView2.Navigate("https://my.web/index.html");

        }
    }
}
