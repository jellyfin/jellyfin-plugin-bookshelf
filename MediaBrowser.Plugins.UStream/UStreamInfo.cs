using System.Collections.Generic;

namespace MediaBrowser.Plugins.UStream
{
    public class PageMeta
    {
        public string activeMain { get; set; }
        public string activeSub { get; set; }
        public string requestPath { get; set; }
        public int infinite { get; set; }
        public int isGrid { get; set; }
        public int isList { get; set; }
        public string title { get; set; }
    }

    public class RootObject
    {
        public bool success { get; set; }
        public PageMeta pageMeta { get; set; }
        public string pageContent { get; set; }
        public string quantcastLabel { get; set; }
    }
}
