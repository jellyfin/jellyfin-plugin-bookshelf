using MediaBrowser.Model.Dto;
using MediaBrowser.Theater.Interfaces.Presentation;
using MediaBrowser.Theater.Interfaces.Theming;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace MediaBrowser.Plugins.Phoenix
{
    public class Theme : ITheme
    {
        private readonly IThemeManager _themeManager;
        private readonly IPresentationManager _presentationManager;

        public Theme(IThemeManager themeManager, IPresentationManager presentationManager)
        {
            _themeManager = themeManager;
            _presentationManager = presentationManager;
        }

        /// <summary>
        /// Gets the item page.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="context">The context.</param>
        /// <returns>Page.</returns>
        public Page GetItemPage(BaseItemDto item, string context)
        {
            return null;
        }

        public string Name
        {
            get { return "Phoenix"; }
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

        /// <summary>
        /// Gets the global resources.
        /// </summary>
        /// <returns>IEnumerable{ResourceDictionary}.</returns>
        private IEnumerable<ResourceDictionary> GetGlobalResources()
        {
            var namespaceName = GetType().Namespace;

            return new[] { "Theme" }.Select(i => new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/" + namespaceName + ";component/Resources/" + i + ".xaml", UriKind.Absolute)
            });
        }

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

        public string DefaultHomePageName
        {
            get { return "Phoenix"; }
        }
    }
}
