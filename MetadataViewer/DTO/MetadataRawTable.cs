using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Providers;
using System.Collections.Generic;

namespace MetadataViewer.DTO
{
    public class MetadataRawTable
    {
        public List<string> Headers { get; set; }

        public List<MetadataRawRow> Rows { get; set; }

        public MetadataRawTable()
        {
            Headers = new List<string>();
            Rows = new List<MetadataRawRow>();
        }

        public class MetadataRawRow
        {
            public string Caption { get; set; }

            public List<object> Values { get; set; }

            public MetadataRawRow()
            {
                Values = new List<object>();
            }
        }
    }
}