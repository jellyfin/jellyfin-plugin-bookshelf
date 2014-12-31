using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Trakt.Api.DataContracts
{
    [DataContract]
    public class TraktPeopleDataContract
    {
        [DataMember(Name = "directors")]
        public List<TraktDirectorDataContract> Directors { get; set; }

        [DataMember(Name = "writers")]
        public List<TraktWriterDataContract> Writers { get; set; }

        [DataMember(Name = "producers")]
        public List<TraktProducerDataContract> Producers { get; set; }

        [DataMember(Name = "actors")]
        public List<TraktActorDataContract> Actors { get; set; }

    }

    [DataContract]
    public class TraktDirectorDataContract
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
    }

    [DataContract]
    public class TraktWriterDataContract
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "job")]
        public string Job { get; set; }
    }

    [DataContract]
    public class TraktProducerDataContract
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "executive")]
        public bool Executive { get; set; }
    }

    [DataContract]
    public class TraktActorDataContract
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "character")]
        public string Character { get; set; }
    }
}
