using MediaBrowser.Model.Dto;
using MediaBrowser.Plugins.DummyTheme.Home;
using MediaBrowser.Plugins.DummyTheme.Pages;
using MediaBrowser.Theater.Interfaces.Presentation;
using MediaBrowser.Theater.Interfaces.Theming;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace MediaBrowser.Plugins.DummyTheme
{
    public class DummyTheme : ITheme
    {
        private readonly IThemeManager _themeManager;
        private readonly IPresentationManager _presentationManager;

        public DummyTheme(IThemeManager themeManager, IPresentationManager presentationManager)
        {
            _themeManager = themeManager;
            _presentationManager = presentationManager;
        }

        public IEnumerable<ResourceDictionary> GetGlobalResources()
        {
            var namespaceName = GetType().Namespace;

            return new[] { "Dictionary1" }.Select(i => new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/" + namespaceName + ";component/Resources/" + i + ".xaml", UriKind.Absolute)
            });
        }

        public Page GetItemPage(BaseItemDto item, string context)
        {
            return new Blankpage();
        }

        public Page GetLoginPage()
        {
            return new HomePage();
        }

        public string Name
        {
            get { return "Dummy Theme"; }
        }

        public void SetDefaultPageTitle()
        {
        }

        public void SetPageTitle(string title)
        {
        }

        public void ShowDefaultErrorMessage()
        {
            _themeManager.DefaultTheme.ShowDefaultErrorMessage();
        }

        public MessageBoxResult ShowMessage(MessageBoxInfo options)
        {
            return _themeManager.DefaultTheme.ShowMessage(options);
        }

        public void ShowNotification(string caption, string text, BitmapImage icon)
        {
            _themeManager.DefaultTheme.ShowNotification(caption, text, icon);
        }

        public void SetGlobalContentVisibility(bool visible)
        {
        }

        private List<ResourceDictionary> _globalResources;

        public void Load()
        {
            _globalResources = GetGlobalResources().ToList();

            foreach (var resource in _globalResources)
            {
                _presentationManager.AddResourceDictionary(resource);
            }
        }

        public void Unload()
        {
            foreach (var resource in _globalResources)
            {
                _presentationManager.RemoveResourceDictionary(resource);
            }

            foreach (var resource in _globalResources.OfType<IDisposable>())
            {
                resource.Dispose();
            }

            _globalResources.Clear();
        }
    }
}
