using Microsoft.Web.WebView2.Core;

namespace TeensyMonitor.Caldera
{
    public partial class CalderaControl : UserControl
    {
        private bool _webInitStarted = false;
        private bool _webInitCompleted = false;

        public CalderaControl()
        {
            InitializeComponent();
        }

        protected override async void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (Program.IsRunning == false || _webInitStarted) return;

            _webInitStarted = true;

            try 
            {
                await InitWebView();
                _webInitCompleted = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize WebView2: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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


            await web.EnsureCoreWebView2Async();

            web.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "my.web",
                Path.Combine(BuildPaths.SolutionDir, "Caldera"),
                CoreWebView2HostResourceAccessKind.Allow);


            web.CoreWebView2.Navigate("https://my.web/index.html");

        }
        private void web_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {

        }

        private void web_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {

        }
    }
}
