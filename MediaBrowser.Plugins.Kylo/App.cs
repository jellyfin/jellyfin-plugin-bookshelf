using MediaBrowser.Theater.Interfaces.Presentation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MediaBrowser.Plugins.Kylo
{
    public class App : IApp
    {
        private readonly IImageManager _imageManager;

        public App(IImageManager imageManager)
        {
            _imageManager = imageManager;
        }

        public FrameworkElement GetThumbImage()
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
                    FileName = GetKyloPath(),
                    WindowStyle = ProcessWindowStyle.Hidden,
                    ErrorDialog = false,
                    UseShellExecute = false
                }
            };

            process.Exited += process_Exited;

            process.Start();
        }

        private string GetKyloPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Hillcrest Labs\\Kylo\\Kylo.exe");
        }

        void process_Exited(object sender, EventArgs e)
        {
            var process = (Process)sender;

            process.Dispose();
        }

        public string Name
        {
            get { return "Kylo"; }
        }

        public void Dispose()
        {
        }
    }

    public class AppFactory : IAppFactory
    {
        private readonly IImageManager _imageManager;

        public AppFactory(IImageManager imageManager)
        {
            _imageManager = imageManager;
        }

        public IEnumerable<IApp> GetApps()
        {
            return new[] { new App(_imageManager) };
        }
    }
}
