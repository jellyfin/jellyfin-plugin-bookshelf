using System.Collections.Generic;

namespace MediaBrowser.Plugins.Dlna
{
    internal static class LoggingExtensions
    {
        //provide some json-esque string that can be used for Verbose logging purposed
        internal static string ToLogString(this Platinum.Action item)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,  
                " {{ Name:\"{0}\", Description:\"{1}\", Arguments:{2} }} ",
                item.Name, item.Description.ToLogString(), item.Arguments.ToLogString());

        }
        internal static string ToLogString(this IEnumerable<Platinum.ActionArgumentDescription> items)
        {
            var result = "[";
            foreach (var arg in items)
            {
                result += (" " + arg.ToLogString());
            }
            result += " ]";
            return result;
        }
        internal static string ToLogString(this Platinum.ActionArgumentDescription item)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, 
                " {{ Name:\"{0}\", Direction:{1}, HasReturnValue:{2}, RelatedStateVariable:{3} }} ",
                item.Name, item.Direction, item.HasReturnValue, item.RelatedStateVariable.ToLogString());

        }
        internal static string ToLogString(this Platinum.StateVariable item)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, 
                " {{ Name:\"{0}\", DataType:{1}, DataTypeString:\"{2}\", Value:{3}, ValueString:\"{4}\" }} ",
                item.Name, item.DataType, item.DataTypeString, item.Value, item.ValueString);
        }
        internal static string ToLogString(this Platinum.ActionDescription item)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, 
                " {{ Name:\"{0}\", Arguments:{1} }} ",
                item.Name, item.Arguments.ToLogString());
        }
        internal static string ToLogString(this Platinum.HttpRequestContext item)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, 
                " {{ LocalAddress:{0}, RemoteAddress:{1}, Request:\"{2}\", Signature:{3} }}",
                item.LocalAddress.ToLogString(), item.RemoteAddress.ToLogString(), item.Request.URI.ToString(), item.Signature);
        }
        internal static string ToLogString(this Platinum.HttpRequestContext.SocketAddress item)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, 
                "{{ IP:{0}, Port:{1} }}",
                item.ip, item.port);
        }
    }
}
