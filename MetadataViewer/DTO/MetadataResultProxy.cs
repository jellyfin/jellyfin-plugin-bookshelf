using MediaBrowser.Controller.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetadataViewer.DTO
{
    class MetadataResultProxy
    {
        private BaseItem item;
        private List<PersonInfo> persons;

        public MetadataResultProxy(BaseItem item, List<PersonInfo> persons)
        {
            this.item = item;
            this.persons = persons;
        }

        public BaseItem Item
        {
            get
            {
                return this.item;
            }
        }

        public List<PersonInfo> Persons
        {
            get
            {
                return this.persons;
            }
        }
    }
}
