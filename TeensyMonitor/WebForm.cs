using System.Diagnostics;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;

namespace TeensyMonitor
{
    public partial class WebForm : Form
    {
        public WebForm()
        {
            InitializeComponent();
        }

        private async void WebForm_Load(object sender, EventArgs e)
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

            using JsonDocument doc = JsonDocument.Parse(e.WebMessageAsJson);
            JsonElement root = doc.RootElement;

            string? type = root.GetProperty("type").GetString();

            Debug.WriteLine($"Received message of type: {type}");
        }
    }
}
