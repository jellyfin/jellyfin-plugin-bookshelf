using System.Runtime.Serialization;

namespace Trakt.Api.DataContracts.BaseModel
{
    [DataContract]
    public class TraktUserSummary
    {
        [DataMember(Name = "username")]
        public string Username { get; set; }

        [DataMember(Name = "name")]
        public string FullName { get; set; }

        [DataMember(Name = "vip")]
        public bool IsVip { get; set; }

        [DataMember(Name = "private")]
        public bool IsPrivate { get; set; }
    }
}