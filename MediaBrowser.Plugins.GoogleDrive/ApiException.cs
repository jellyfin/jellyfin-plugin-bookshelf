using System;

namespace MediaBrowser.Plugins.GoogleDrive
{
    public class ApiException : Exception
    {
        public ApiException(string message)
            : base(message)
        { }
    }
}
