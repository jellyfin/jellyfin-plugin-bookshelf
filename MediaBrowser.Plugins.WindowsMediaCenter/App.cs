using MediaBrowser.Theater.Interfaces.Presentation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MediaBrowser.Plugins.WindowsMediaCenter
{
    public class App : IApp
    {
        private readonly IImageManager _imageManager;

        public App(IImageManager imageManager)
        {
            _imageManager = imageManager;
        }

        public FrameworkElement GetTileImage()
        {
            var image = new Image
            {
                Source = _imageManager.GetBitmapImage(Plugin.GetThumbUri())
            };

            return image;
        }

        public Task Launch()
        {
            return Task.Run(() => LaunchProcess());
        }

        private void LaunchProcess()
        {
            var process = new Process
            {
                EnableRaisingEvents = true,

                StartInfo = new ProcessStartInfo
                {
                    FileName = GetPath(),
                    WindowStyle = ProcessWindowStyle.Hidden,
                    ErrorDialog = false,
                    UseShellExecute = false
                }
            };

            process.Exited += process_Exited;

            process.Start();
        }

        private string GetPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "ehome\\eshell.exe");
        }

        void process_Exited(object sender, EventArgs e)
        {
            var process = (Process)sender;

            process.Dispose();
        }

        public string Name
        {
            get { return "Windows Media Center"; }
        }

        public void Dispose()
        {
        }
    }

    public class AppFactory : IAppFactory
    {
        public IEnumerable<Type> AppTypes
        {
            get { return new[] { typeof(App) }; }
        }
    }
}
