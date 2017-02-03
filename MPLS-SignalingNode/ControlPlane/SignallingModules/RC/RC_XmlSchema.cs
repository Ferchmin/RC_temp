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

        [XmlElement("myAreaName")]
        public string XMP_myAreaName { get; set; }

        [XmlElement("DomainIpAddress")]
        public string XMP_DomainIpAddress { get; set; }

        public struct SN_1TODomain
        {
            [XmlElement("ID_SN_1")]
            public int ID_SN_1 { get; set; }
            [XmlElement("ID_Domain")]
            public int ID_Domain { get; set; }
        }

        [XmlElement("SN_1")]
        public SN_1TODomain[] Translate1 { get; set; }
        public struct SN_2TODomain
        {
            [XmlElement("ID_SN_2")]
            public int ID_SN_2 { get; set; }
            [XmlElement("ID_Domain")]
            public int ID_Domain { get; set; }
        }

        [XmlElement("SN_2")]
        public SN_2TODomain[] Translate2 { get; set; }
        public struct IPTOID
        {
            [XmlElement("IP")]
            public string IP { get; set; }
            [XmlElement("ID")]
            public int ID { get; set; }
        }

        [XmlElement("IPTOID")]
        public IPTOID[] Dictionary { get; set; }


        [XmlArray("LocalTopology")]
        [XmlArrayItem("Record", typeof(Topology))]
        public List<Topology> LocalTopology { get; set; }


        public RC_XmlSchame()
        {
            LocalTopology = new List<ControlPlane.Topology>();
        }

    }


    public class Topology
    {
        [XmlElement("ID")]
        public int ID { get; set; }
        [XmlElement("capacity")]
        public int capacity { get; set; }
        [XmlElement("weight")]
        public double weight { get; set; }


        [XmlArray("reachableID-List")]
        [XmlArrayItem("Record", typeof(int))]
        public List<int> reachableNodes { get; set; }

        [XmlElement("areaName")]
        public string areaName { get; set; }

        public Topology()
        {
            reachableNodes = new List<int>();
        }
    }
}
