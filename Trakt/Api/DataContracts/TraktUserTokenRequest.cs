using System.Runtime.Serialization;

namespace Trakt.Api.DataContracts
{
    [DataContract]
    public class TraktUserTokenRequest
    {
        [DataMember(Name = "login")]
        public string Login { get; set; }

        [DataMember(Name = "password")]
        public string Password { get; set; }
    }
}