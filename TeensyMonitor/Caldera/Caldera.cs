using Microsoft.Web.WebView2.Core;

namespace TeensyMonitor.Caldera
{
    public class Caldera
    {
        public AutoResetEvent InputReady = new(false);
        public AutoResetEvent OutputEvent = new(false);

        public CalderaControl Control;
        public CoreWebView2 WebView;

        public Caldera(CalderaControl control)
        {
            Control = control;
            WebView = control.CoreWebView2;

            WebView.WebMessageReceived += WebView_WebMessageReceived;
            WebView.NavigationCompleted += WebView_NavigationCompleted;
        }

        private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess) return;

            WebView.Settings.IsWebMessageEnabled = true;

            WebView.PostWebMessageAsJson("{\"type\":\"hostConfig\",\"postSettingsChanges\":true}");

        }

        private void WebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message = e.TryGetWebMessageAsString();
            if (message == "dataReady")
            {
                OutputEvent.Set();
            }
        }
    }
}