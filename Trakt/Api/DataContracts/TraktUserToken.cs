using System.Runtime.Serialization;

namespace Trakt.Api.DataContracts
{
    [DataContract]
    public class TraktUserToken
    {
        [DataMember(Name = "token")]
        public string Token { get; set; }
    }
}