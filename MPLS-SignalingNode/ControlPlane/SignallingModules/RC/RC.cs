using System;
using MPLS_SignalingNode;
using System.Collections.Generic;
using DTO.ControlPlane;
using System.Threading;

namespace ControlPlane
{
    class RC
    {
        #region Variables
        private string _configurationFilePath;
        private string _localPcIpAddress;
        private string _myAreaName;
        private string _domainIpAddress;
        private Dictionary<int, int> interdomainLinks = new Dictionary<int, int>();
        private Dictionary<string, int> IPTOIDDictionary = new Dictionary<string, int>();
        private Dictionary<int, int> SN_1ToDomain = new Dictionary<int, int>();
        private Dictionary<int, int> SN_2ToDomain = new Dictionary<int, int>();
        private Graph graph = new Graph();
        public List<AbstractVertex> abstractVertices = new List<AbstractVertex>();
        private List<Lrm> LRMs = new List<Lrm>();
        private PC _pc;
        #endregion

        #region Properties
        public PC LocalPC { set { _pc = value; } }
        #endregion

        public RC()
        {

        }
        #region Main_Methodes

        public RC(string configurationFilePath)
        {
            InitialiseVariables(configurationFilePath);
            graph = createGraph(abstractVertices);
            Dijkstra dijkstra = new Dijkstra(_myAreaName);
            List<PathInfo> pathsInfo = new List<PathInfo>();

            if (!_myAreaName.Contains("Dom"))
            {
                pathsInfo = dijkstra.runAlgorithmForAll(graph);
                foreach (PathInfo pathInfo in pathsInfo)
                {
                    Thread.Sleep(20);
                    LocalTopology(pathInfo.beginEnd, pathInfo.Weight, pathInfo.Capacity, pathInfo.AreaName, _domainIpAddress);
                }
            }
            
        }

        private void InitialiseVariables(string configurationFilePath)
        {
            _configurationFilePath = configurationFilePath;

            RC_XmlSchame tmp = new RC_XmlSchame();
            Topology temp = new Topology();
            tmp = RC_LoadingXmlFile.Deserialization(_configurationFilePath);
            _localPcIpAddress = tmp.XML_myIPAddress;
            _myAreaName = tmp.XMP_myAreaName;
            _domainIpAddress = tmp.XMP_DomainIpAddress;
            if(_myAreaName.Equals("Dom1"))
            {
                if (tmp.Translate1 != null)
                {
                    foreach (var v in tmp.Translate1)
                    {
                        SN_1ToDomain.Add(v.ID_SN_1, v.ID_Domain);
                    }
                }
                if (tmp.Translate2 != null)
                {
                    foreach (var v in tmp.Translate2)
                    {
                        SN_1ToDomain.Add(v.ID_SN_2, v.ID_Domain);
                    }
                }
            }
            if (tmp.Dictionary != null)
            {
                foreach (var v in tmp.Dictionary)
                {
                    IPTOIDDictionary.Add(v.IP, v.ID);
                }
            }
            foreach (var v in tmp.LocalTopology)
            {
                AbstractVertex tmpAbstractVertex = new AbstractVertex(v.ID, v.capacity, v.weight, v.areaName, v.reachableNodes);
                Console.WriteLine("id: " + v.ID);
                foreach (int id in v.reachableNodes)
                {
                    Console.WriteLine(id);
                }
                abstractVertices.Add(tmpAbstractVertex);
            }
        }

        public Graph createGraph(List<AbstractVertex> abstractVertices)
        {

            Graph graph = new Graph();

            List<Vertex> vertices = new List<Vertex>();
            foreach (AbstractVertex v in abstractVertices)
            {

                Vertex tmpVertex = new Vertex(v.ID, v.Capacity, v.Weight, v.AreaName);
                vertices.Add(tmpVertex);

            }
            foreach (Vertex currentVertex in vertices)
            {
                AbstractVertex tmpAbstractVertex = abstractVertices.Find(x => x.ID == currentVertex.Id);
                if (tmpAbstractVertex != null)
                {
                    foreach (int id in tmpAbstractVertex.ReachableNodes)
                    {
                        Vertex end = vertices.Find(x => x.Id == id);
                        if (end != null)
                        {
                            Edge edge = new Edge(currentVertex, end, tmpAbstractVertex.Capacity, tmpAbstractVertex.Weight);
                            if (!edge.Begin.AreaName.Equals(_myAreaName))
                            {
                                if (edge.Begin.AreaName.Equals(edge.End.AreaName))
                                {
                                    edge.SubDomain = true;
                                }
                            }
                            currentVertex.addEdgeOut(edge);
                            graph.Edges.Add(edge);
                        }

                    }
                }
                graph.Vertices.Add(currentVertex);
            }

            return graph;
        }


        #endregion


        #region PC_Cooperation_Methodes
        private void SendMessageToPC(SignalMessage message)
        {
            _pc.SendSignallingMessage(message);
            //SignallingNodeDeviceClass.MakeSignallingLog("RC", "INFO - Signalling message send to PC module");
        }
        public void ReceiveMessageFromPC(SignalMessage message)
        {
            switch (message.General_SignalMessageType)
            {
                case SignalMessage.SignalType.RouteQuery:
                    if (message.CallingIpAddress != null)
                    {
                        RouteQuery(message.ConnnectionID, message.CallingIpAddress, message.CalledIpAddress, message.CallingCapacity); // Wewnatrzdomenowa wiad nr 1 i miedzydomenowa nr 1
                    }
                    else if (message.CalledIpAddress != null)
                    {
                        RouteQuery(message.ConnnectionID, message.SnppInId, message.CalledIpAddress, message.CallingCapacity); // Miedzydomenowa wiad nr 3
                    }
                    else
                    {
                        RouteQuery(message.ConnnectionID, message.SnppIdPair, message.CallingCapacity); // wewnatrzdomenowa wiad nr 2 i miedzydomenowa nr 2
                    }

                    break;
/*
                case SignalMessage.SignalType.IsUp:
                    IsUp(message.IsUpKeepAlive_areaName);

                    break;

                case SignalMessage.SignalType.KeepAlive:
                    KeepAlive(message.IsUpKeepAlive_areaName);
                    break;
*/

                
                case SignalMessage.SignalType.LocalTopology:
                    LocalTopology(message.SnppIdPair, message.LocalTopology_weight, message.LocalTopology_availibleCapacity, message.AreaName);
                    break;
                    
            }
        }


        #endregion



        #region Incomming_Methodes_From_Standardization

        private void RouteQuery(int connectionID, string callingIpAddress, string calledIpAddress, int callingCapacity)
        {
            SignalMessage.Pair SNPPPair = new SignalMessage.Pair();
            SNPPPair.first = IPTOIDDictionary[callingIpAddress];
            SNPPPair.second = IPTOIDDictionary[calledIpAddress];

            Vertex begin = graph.Vertices.Find(x => x.Id == SNPPPair.first);
            Vertex end = graph.Vertices.Find(x => x.Id == SNPPPair.second);

            List<string> areaNames = new List<string>();
            List<SignalMessage.Pair> snppPairs = new List<SignalMessage.Pair>();

            SNPPPair.second = interdomainLinks[end.Id];

            if (begin.AreaName.Equals(end.AreaName))
            {
                areaNames = null;
                snppPairs.Add(SNPPPair);
                RouteQueryResponse(connectionID, snppPairs, areaNames);
            }
            else
            {
                areaNames.Add(end.AreaName);
                snppPairs.Add(SNPPPair);
                SignalMessage.Pair interdomainPair = new SignalMessage.Pair();
                interdomainPair.first = interdomainLinks[end.Id];
                interdomainPair.second = end.Id;
                snppPairs.Add(interdomainPair);
                RouteQueryResponse(connectionID, snppPairs, areaNames);
            }
        }

        public void RouteQuery(int connectionID, int snppInId, string calledIpAddress, int callingCapacity)
        {
            SignalMessage.Pair SNPPPair = new SignalMessage.Pair();
            SNPPPair.first = snppInId;
            SNPPPair.second = IPTOIDDictionary[calledIpAddress];

            List<SignalMessage.Pair> localSnppPairs = new List<SignalMessage.Pair>();
            localSnppPairs.Add(SNPPPair);

            Vertex begin = graph.Vertices.Find(x => x.Id == SNPPPair.first);
            Vertex end = graph.Vertices.Find(x => x.Id == SNPPPair.second);

            List<String> areaNames = new List<String>();
            areaNames.Add(end.AreaName);

            if (begin.AreaName.Equals(end.AreaName))
            {
                RouteQueryResponse(connectionID, localSnppPairs, null);
            }
            else
            {
                RouteQueryResponse(connectionID, localSnppPairs, areaNames);
            }
        }


        private void RouteQuery(int connectionID, SignalMessage.Pair snppIdPair, int callingCapacity)
        {

            Dijkstra dijkstra = new Dijkstra(_myAreaName);
            Vertex begin = graph.Vertices.Find(x => x.Id == snppIdPair.first);
            Vertex end = graph.Vertices.Find(x => x.Id == snppIdPair.second);

            List<SignalMessage.Pair> snppIdPairs = dijkstra.runAlgorithm(graph, begin, end, callingCapacity);
            List<string> areaNames = new List<string>();

            if(snppIdPairs == null)
            {
                RouteQueryResponse(connectionID, snppIdPairs, areaNames);
                return;
            }


            foreach (SignalMessage.Pair pair in snppIdPairs)
            {
                //Vertex firstVertex = graph.Vertices.Find(x => x.Id == pair.first);
                Vertex secondVertex = graph.Vertices.Find(x => x.Id == pair.second);

                if (!secondVertex.AreaName.Equals(_myAreaName))
                {
                    areaNames.Add(secondVertex.AreaName);
                }
            }

            RouteQueryResponse(connectionID, snppIdPairs, areaNames);

        }

        private void LocalTopology(SignalMessage.Pair snppIdPair, double weight, int avaibleCapacity, string areaName)
        {
            int first = SN_1ToDomain[snppIdPair.first];
            int second = SN_1ToDomain[snppIdPair.second];
            if (((graph.Vertices.Find(x => x.Id == first)) != null) && ((graph.Vertices.Find(x => x.Id == second)) != null))
            {
                Edge edge = new Edge(graph.Vertices.Find(x => x.Id == first), graph.Vertices.Find(x => x.Id == second), avaibleCapacity, weight);
                graph.Vertices.Find(x => x.Id == first).addEdgeOut(edge);
                graph.Edges.Add(edge);
            }
        }
        public void IsUp(string areaName)
        {
            var lrm = LRMs.Find(x => x.AreaName.Equals(areaName));
            if (lrm == null)
            {
               // Lrm l = new Lrm(areaName, this);
              //  LRMs.Add(l);
            }
            else
                KeepAlive(areaName);
        }
        public void KeepAlive(string areaName)
        {
            var item = LRMs.Find(x => x.AreaName.Equals(areaName));
            if (item != null)
            {
                LRMs.Find(x => x.AreaName.Equals(areaName)).keepAliveTimer.Stop();
                LRMs.Find(x => x.AreaName.Equals(areaName)).keepAliveTimer.Start();
            }

        }
        


        public void NetworkTopology(int snppId, List<int> reachableSnppIdList)
        {
            foreach (var id in reachableSnppIdList)
                interdomainLinks.Add(id, snppId);
        }
        #endregion



        #region Outcomming_Methodes_From_Standardization



        private void RouteQueryResponse(int connectionID, SignalMessage.Pair snppPair, int callingCapacity)
        {
            SignalMessage message = new SignalMessage()
            {
                General_SignalMessageType = SignalMessage.SignalType.RouteQueryResponse,
                General_SourceIpAddress = _localPcIpAddress,
                General_DestinationIpAddress = _localPcIpAddress,
                General_SourceModule = "RC",
                General_DestinationModule = "CC",

                ConnnectionID = connectionID,
                SnppIdPair = snppPair

            };

            _pc.SendSignallingMessage(message);
        }
        private void LocalTopology(SignalMessage.Pair snppIdPair, double weight, int avaibleCapacity, string areaName, string domainIpAddress)
        {
            SignalMessage message = new SignalMessage()
            {
                General_SignalMessageType = SignalMessage.SignalType.LocalTopology,
                General_SourceIpAddress = _localPcIpAddress,
                General_DestinationIpAddress = domainIpAddress,
                General_SourceModule = "RC",
                General_DestinationModule = "RC",

                SnppIdPair = snppIdPair,
                LocalTopology_weight = weight,
                LocalTopology_availibleCapacity = avaibleCapacity,
                LocalTopology_areaName = areaName
            };

            _pc.SendSignallingMessage(message);
        }
        private void RouteQueryResponse(int connectionID, List<SignalMessage.Pair> snppPair, List<string> areaName)
        {
            SignalMessage message = new SignalMessage()
            {
                General_SignalMessageType = SignalMessage.SignalType.RouteQueryResponse,
                General_SourceIpAddress = _localPcIpAddress,
                General_DestinationIpAddress = _localPcIpAddress,
                General_SourceModule = "RC",
                General_DestinationModule = "CC",

                ConnnectionID = connectionID,
                IncludedSnppIdPairs = snppPair,
                IncludedAreaNames = areaName

            };

            SendMessageToPC(message);
        }



        #endregion

        #region Other
        public void OnNodeFailure(string areaName)
        {
            List<Vertex> v = graph.Vertices.FindAll(x => x.AreaName.Equals(areaName));
            foreach (var ver in v)
            {
                graph.Edges.RemoveAll(x => x.Begin.Id == ver.Id);
                graph.Edges.RemoveAll(x => x.End.Id == ver.Id);
            }
            graph.Vertices.RemoveAll(x => x.AreaName.Equals(areaName));
        }
        #endregion
    }
}
