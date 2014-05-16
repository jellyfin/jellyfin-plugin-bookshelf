using System.Xml.Serialization;

namespace TagChimp
{
    public abstract class ChapterPart
    {
        [XmlText]
        public string Value { get; set; }
    }

    public class ChapterNumber : ChapterPart
    {
    }

    public class ChapterTitle : ChapterPart
    {
    }

    public class ChapterTime : ChapterPart
    {
    }
}