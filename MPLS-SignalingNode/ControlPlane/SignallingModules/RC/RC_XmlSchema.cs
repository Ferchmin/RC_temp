using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ControlPlane
{
    [XmlRoot("RC_Configuration")]
    public class RC_XmlSchame
    {
        [XmlElement("myIPAddress")]
        public string XML_myIPAddress { get; set; }
        /*
        [XmlElement("myPortNumber")]
        public int XML_myPortNumber { get; set; }

    */

            [XmlElement("myAreaName")]
            public string XMP_myAreaName { get; set; }
        public struct IPTOID
        {
            [XmlElement("IP")]
            public string IP { get; set; }
            [XmlElement("ID")]
            public int ID { get; set; }
        }
        [XmlElement("IPTOID")]
        public IPTOID[] Dictionary { get; set; }
        public RC_XmlSchame()
        {
        }

    }
}
