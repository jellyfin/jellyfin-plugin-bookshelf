using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmbyTV.EPGProvider.Responses
{
    class XMLTV
    {

        /// <remarks/>
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
        public partial class tv
        {

            private tvChannel[] channelField;

            private tvProgramme[] programmeField;

            private string sourceinfourlField;

            private string sourceinfonameField;

            private string generatorinfonameField;

            private string generatorinfourlField;

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute("channel")]
            public tvChannel[] channel
            {
                get
                {
                    return this.channelField;
                }
                set
                {
                    this.channelField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute("programme")]
            public tvProgramme[] programme
            {
                get
                {
                    return this.programmeField;
                }
                set
                {
                    this.programmeField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute("source-info-url")]
            public string sourceinfourl
            {
                get
                {
                    return this.sourceinfourlField;
                }
                set
                {
                    this.sourceinfourlField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute("source-info-name")]
            public string sourceinfoname
            {
                get
                {
                    return this.sourceinfonameField;
                }
                set
                {
                    this.sourceinfonameField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute("generator-info-name")]
            public string generatorinfoname
            {
                get
                {
                    return this.generatorinfonameField;
                }
                set
                {
                    this.generatorinfonameField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute("generator-info-url")]
            public string generatorinfourl
            {
                get
                {
                    return this.generatorinfourlField;
                }
                set
                {
                    this.generatorinfourlField = value;
                }
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class tvChannel
        {

            private string[] displaynameField;

            private tvChannelIcon iconField;

            private string idField;

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute("display-name")]
            public string[] displayname
            {
                get
                {
                    return this.displaynameField;
                }
                set
                {
                    this.displaynameField = value;
                }
            }

            /// <remarks/>
            public tvChannelIcon icon
            {
                get
                {
                    return this.iconField;
                }
                set
                {
                    this.iconField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string id
            {
                get
                {
                    return this.idField;
                }
                set
                {
                    this.idField = value;
                }
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class tvChannelIcon
        {

            private string srcField;

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string src
            {
                get
                {
                    return this.srcField;
                }
                set
                {
                    this.srcField = value;
                }
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class tvProgramme
        {

            private tvProgrammeTitle titleField;

            private tvProgrammeSubtitle subtitleField;

            private tvProgrammeDesc descField;

            private tvProgrammeCredits creditsField;

            private uint dateField;

            private bool dateFieldSpecified;

            private tvProgrammeCategory[] categoryField;

            private tvProgrammeEpisodenum[] episodenumField;

            private tvProgrammeAudio audioField;

            private tvProgrammePreviouslyshown previouslyshownField;

            private tvProgrammeSubtitles subtitlesField;

            private tvProgrammeRating ratingField;

            private string startField;

            private string stopField;

            private string channelField;

            /// <remarks/>
            public tvProgrammeTitle title
            {
                get
                {
                    return this.titleField;
                }
                set
                {
                    this.titleField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute("sub-title")]
            public tvProgrammeSubtitle subtitle
            {
                get
                {
                    return this.subtitleField;
                }
                set
                {
                    this.subtitleField = value;
                }
            }

            /// <remarks/>
            public tvProgrammeDesc desc
            {
                get
                {
                    return this.descField;
                }
                set
                {
                    this.descField = value;
                }
            }

            /// <remarks/>
            public tvProgrammeCredits credits
            {
                get
                {
                    return this.creditsField;
                }
                set
                {
                    this.creditsField = value;
                }
            }

            /// <remarks/>
            public uint date
            {
                get
                {
                    return this.dateField;
                }
                set
                {
                    this.dateField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlIgnoreAttribute()]
            public bool dateSpecified
            {
                get
                {
                    return this.dateFieldSpecified;
                }
                set
                {
                    this.dateFieldSpecified = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute("category")]
            public tvProgrammeCategory[] category
            {
                get
                {
                    return this.categoryField;
                }
                set
                {
                    this.categoryField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute("episode-num")]
            public tvProgrammeEpisodenum[] episodenum
            {
                get
                {
                    return this.episodenumField;
                }
                set
                {
                    this.episodenumField = value;
                }
            }

            /// <remarks/>
            public tvProgrammeAudio audio
            {
                get
                {
                    return this.audioField;
                }
                set
                {
                    this.audioField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute("previously-shown")]
            public tvProgrammePreviouslyshown previouslyshown
            {
                get
                {
                    return this.previouslyshownField;
                }
                set
                {
                    this.previouslyshownField = value;
                }
            }

            /// <remarks/>
            public tvProgrammeSubtitles subtitles
            {
                get
                {
                    return this.subtitlesField;
                }
                set
                {
                    this.subtitlesField = value;
                }
            }

            /// <remarks/>
            public tvProgrammeRating rating
            {
                get
                {
                    return this.ratingField;
                }
                set
                {
                    this.ratingField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string start
            {
                get
                {
                    return this.startField;
                }
                set
                {
                    this.startField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string stop
            {
                get
                {
                    return this.stopField;
                }
                set
                {
                    this.stopField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string channel
            {
                get
                {
                    return this.channelField;
                }
                set
                {
                    this.channelField = value;
                }
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class tvProgrammeTitle
        {

            private string langField;

            private string valueField;

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string lang
            {
                get
                {
                    return this.langField;
                }
                set
                {
                    this.langField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlTextAttribute()]
            public string Value
            {
                get
                {
                    return this.valueField;
                }
                set
                {
                    this.valueField = value;
                }
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class tvProgrammeSubtitle
        {

            private string langField;

            private string valueField;

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string lang
            {
                get
                {
                    return this.langField;
                }
                set
                {
                    this.langField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlTextAttribute()]
            public string Value
            {
                get
                {
                    return this.valueField;
                }
                set
                {
                    this.valueField = value;
                }
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class tvProgrammeDesc
        {

            private string langField;

            private string valueField;

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string lang
            {
                get
                {
                    return this.langField;
                }
                set
                {
                    this.langField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlTextAttribute()]
            public string Value
            {
                get
                {
                    return this.valueField;
                }
                set
                {
                    this.valueField = value;
                }
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class tvProgrammeCredits
        {

            private string[] actorField;

            private string directorField;

            private string producerField;

            private string presenterField;

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute("actor")]
            public string[] actor
            {
                get
                {
                    return this.actorField;
                }
                set
                {
                    this.actorField = value;
                }
            }

            /// <remarks/>
            public string director
            {
                get
                {
                    return this.directorField;
                }
                set
                {
                    this.directorField = value;
                }
            }

            /// <remarks/>
            public string producer
            {
                get
                {
                    return this.producerField;
                }
                set
                {
                    this.producerField = value;
                }
            }

            /// <remarks/>
            public string presenter
            {
                get
                {
                    return this.presenterField;
                }
                set
                {
                    this.presenterField = value;
                }
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class tvProgrammeCategory
        {

            private string langField;

            private string valueField;

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string lang
            {
                get
                {
                    return this.langField;
                }
                set
                {
                    this.langField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlTextAttribute()]
            public string Value
            {
                get
                {
                    return this.valueField;
                }
                set
                {
                    this.valueField = value;
                }
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class tvProgrammeEpisodenum
        {

            private string systemField;

            private string valueField;

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string system
            {
                get
                {
                    return this.systemField;
                }
                set
                {
                    this.systemField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlTextAttribute()]
            public string Value
            {
                get
                {
                    return this.valueField;
                }
                set
                {
                    this.valueField = value;
                }
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class tvProgrammeAudio
        {

            private string stereoField;

            /// <remarks/>
            public string stereo
            {
                get
                {
                    return this.stereoField;
                }
                set
                {
                    this.stereoField = value;
                }
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class tvProgrammePreviouslyshown
        {

            private ulong startField;

            private bool startFieldSpecified;

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public ulong start
            {
                get
                {
                    return this.startField;
                }
                set
                {
                    this.startField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlIgnoreAttribute()]
            public bool startSpecified
            {
                get
                {
                    return this.startFieldSpecified;
                }
                set
                {
                    this.startFieldSpecified = value;
                }
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class tvProgrammeSubtitles
        {

            private string typeField;

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string type
            {
                get
                {
                    return this.typeField;
                }
                set
                {
                    this.typeField = value;
                }
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class tvProgrammeRating
        {

            private string valueField;

            private string systemField;

            /// <remarks/>
            public string value
            {
                get
                {
                    return this.valueField;
                }
                set
                {
                    this.valueField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string system
            {
                get
                {
                    return this.systemField;
                }
                set
                {
                    this.systemField = value;
                }
            }
        }


    }
}
