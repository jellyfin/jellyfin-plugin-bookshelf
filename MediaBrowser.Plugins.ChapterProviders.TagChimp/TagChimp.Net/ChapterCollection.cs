using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

namespace TagChimp
{
    public class ChapterCollection
    {
        public ChapterCollection()
        {
            RawChapters = new ObservableCollection<ChapterPart>();
            ((ObservableCollection<ChapterPart>)RawChapters).CollectionChanged += new NotifyCollectionChangedEventHandler(ChaptersChanged);
            Items = new List<Chapter>();
        }

        void ChaptersChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ParseRawChapters();
        }

        /// <summary>
        /// Takes the chapter information from the form returned by the web
        /// service and organizes it into individual chapter objects
        /// </summary>
        private void ParseRawChapters()
        {
            Items.Clear();
            if (RawChapters.Count == 0)
                return;
            Queue<ChapterPart> queue = new Queue<ChapterPart>();
            foreach (var c in RawChapters)
                queue.Enqueue(c);

            while (queue.Count > 0)
            {
                Chapter c = new Chapter();
                if (queue.Count > 0 && queue.Peek().GetType() == typeof(ChapterNumber))
                {
                    int val;
                    var item = queue.Dequeue();
                    if (Int32.TryParse(item.Value, out val))
                    {
                        c.Index = val;
                    }
                }

                if (queue.Count > 0 && queue.Peek().GetType() == typeof(ChapterTitle))
                {
                    c.Title = queue.Dequeue().Value;
                }

                if (queue.Count > 0 && queue.Peek().GetType() == typeof(ChapterTime))
                {
                    var item = queue.Dequeue();
                    c.StartTime = ParseTime(item.Value);
                }
                Items.Add(c);
            }
        }

        private TimeSpan ParseTime(string time)
        {
            string pattern = "\\A(\\d\\d):(\\d\\d):(\\d\\d):(\\d\\d\\d)\\z";
            Regex r = new Regex(pattern);
            var match = r.Match(time);
            if (match != null && match.Success)
            {
                string s = String.Format("{0}:{1}:{2}", match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value);
                TimeSpan val;
                if (TimeSpan.TryParse(s, out val))
                {
                    return val;
                }
            }
            return TimeSpan.Zero;
        }

        [XmlIgnore]
        public IList<Chapter> Items { get; set; }

        [XmlElement("totalChapters")]
        public int TotalChapters { get; set; }

        [XmlArray("chapter")]
        [XmlArrayItem(ElementName = "chapterNumber", Type = typeof(ChapterNumber))]
        [XmlArrayItem(ElementName = "chapterTitle", Type = typeof(ChapterTitle))]
        [XmlArrayItem(ElementName = "chapterTime", Type = typeof(ChapterTime))]
        public Collection<ChapterPart> RawChapters { get; set; }
    }
}