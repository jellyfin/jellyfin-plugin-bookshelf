using MediaBrowser.Model.Dlna;

namespace RokuMetadata
{
    public class RokuDeviceProfile : DeviceProfile
    {
        public RokuDeviceProfile()
        {
            Name = "Roku";

            MaxStreamingBitrate = 20000000;

            DirectPlayProfiles = new[]
            {
                new DirectPlayProfile
                {
                    Container = "mkv",
                    VideoCodec = "h264,mpeg4",
                    AudioCodec = "ac3,dca,aac,mp3",
                    Type = DlnaProfileType.Video
                },
                new DirectPlayProfile
                {
                    Container = "mp4,mov,m4v",
                    VideoCodec = "h264,mpeg4",
                    AudioCodec = "ac3,aac",
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
        }
    }
}
