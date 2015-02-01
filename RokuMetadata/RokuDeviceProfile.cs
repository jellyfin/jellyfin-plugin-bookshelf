using MediaBrowser.Model.Dlna;

namespace RokuMetadata
{
    public class RokuDeviceProfile : DeviceProfile
    {
        public RokuDeviceProfile(bool supportsAc3, bool supportsDca)
        {
            Name = "Roku";

            MaxStreamingBitrate = 20000000;

            var mkvAudio = "aac,mp3";
            var mp4Audio = "aac";

            if (supportsAc3)
            {
                mkvAudio += ",ac3";
                mp4Audio += ",ac3";
            }

            if (supportsDca)
            {
                mkvAudio += ",dca";
            }

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile
                {
                    Container = "mkv",
                    VideoCodec = "h264,mpeg4",
                    AudioCodec = mkvAudio,
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "mp4,mov,m4v",
                    VideoCodec = "h264,mpeg4",
                    AudioCodec = mp4Audio,
                    Type = DlnaProfileType.Video
                }
            };

            ContainerProfiles = new ContainerProfile[] { };

            CodecProfiles = new[]
            {
                new CodecProfile
                {
                    Type = CodecType.Video,
                    Conditions = new []
                    {
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.VideoBitDepth,
                            Value = "8",
                            IsRequired = false
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.Height,
                            Value = "1080",
                            IsRequired = false
                        },
                        new ProfileCondition
                        {
                            Condition = ProfileConditionType.LessThanEqual,
                            Property = ProfileConditionValue.RefFrames,
                            Value = "12",
                            IsRequired = false
                        }
                    }
                }
            };

            SubtitleProfiles = new[]
            {
                new SubtitleProfile
                {
                    Format = "srt",
                    Method = SubtitleDeliveryMethod.External
                }
            };

            TranscodingProfiles = new[]
            {
                new TranscodingProfile
                {
                    Container = "mp3",
                    AudioCodec = "mp3",
                    Type = DlnaProfileType.Audio
                },

                new TranscodingProfile
                {
                    Container = "ts",
                    Type = DlnaProfileType.Video,
                    AudioCodec = "aac",
                    VideoCodec = "h264"
                },

                new TranscodingProfile
                {
                    Container = "jpeg",
                    Type = DlnaProfileType.Photo
                }
            };

        }
    }
}
