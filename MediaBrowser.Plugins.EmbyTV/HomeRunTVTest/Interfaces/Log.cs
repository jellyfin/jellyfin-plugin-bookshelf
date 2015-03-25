using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;

namespace HomeRunTVTest.Interfaces
{
    class Log: ILogger
    {
        public string lastLog;
        void ILogger.Debug(string message, params object[] paramList)
        {
            throw new NotImplementedException();
        }

        void ILogger.Error(string message, params object[] paramList)
        {
            lastLog = message;
        }

        void ILogger.ErrorException(string message, Exception exception, params object[] paramList)
        {
            throw new NotImplementedException();
        }

        void ILogger.Fatal(string message, params object[] paramList)
        {
            throw new NotImplementedException();
        }

        void ILogger.FatalException(string message, Exception exception, params object[] paramList)
        {
            throw new NotImplementedException();
        }

        void ILogger.Info(string message, params object[] paramList)
        {
            throw new NotImplementedException();
        }

        void ILogger.Log(LogSeverity severity, string message, params object[] paramList)
        {
            throw new NotImplementedException();
        }

        void ILogger.LogMultiline(string message, LogSeverity severity, StringBuilder additionalContent)
        {
            throw new NotImplementedException();
        }

        void ILogger.Warn(string message, params object[] paramList)
        {
            throw new NotImplementedException();
        }
    }
}
