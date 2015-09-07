using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace Tuna.iCoder.Base.Imp
{
    [XmlRoot("configuration")]
    public class SqlConfigAttach
    {
        [XmlArray("tablemaps")]
        [XmlArrayItem("add")]
        public TableMaps TableMaps { get; set; }
    }

    public abstract class TableMaps : KeyedCollection<string, TableMap>
    {
        protected override string GetKeyForItem(TableMap item)
        {
            return item.Name;
        }
    }
    public class TableMap
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlElement("primarykeys")]
        public string PrimaryKeys { get; set; }

        [XmlAttribute("driver")]
        public string Driver { get; set; }

        [XmlArray("operations")]
        [XmlArrayItem("add")]
        public List<Operation> Operations { get; set; }
    }

    public abstract class Operations : KeyedCollection<string, Operation>
    {
        protected override string GetKeyForItem(Operation item)
        {
            return item.Name;
        }
    }
    public class Operation
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("type")]
        public string Type { get; set; }

        [XmlElement("fields")]
        public string Fields { get; set; }

        [XmlElement("keynames")]
        public string KeyNames { get; set; }

        [XmlElement("wherecondition")]
        public string Condition { get; set; }
    }
}
