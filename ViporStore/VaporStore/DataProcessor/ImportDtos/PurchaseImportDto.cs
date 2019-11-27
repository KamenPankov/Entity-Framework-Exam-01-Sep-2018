using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace VaporStore.DataProcessor.ImportDtos
{
    [XmlType("Purchase")]
    public class PurchaseImportDto
    {
        [XmlAttribute("title")]
        public string GameName { get; set; }

        [XmlElement("Type")]
        public string Type { get; set; }

        [XmlElement("Key")]
        public string ProductKey { get; set; }

        [XmlElement("Card")]
        public string CardNumber { get; set; }

        [XmlElement("Date")]
        public string Date { get; set; }
    }
}
