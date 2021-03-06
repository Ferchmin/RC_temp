﻿using System;
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
        private Dictionary<int, int> interdomainLinksDictionary = new Dictionary<int, int>();
        private Dictionary<string, int> IPTOIDDictionary = new Dictionary<string, int>();
        private Dictionary<int, int> SN_1ToDomain = new Dictionary<int, int>();
        private Dictionary<int, int> SN_2ToDomain = new Dictionary<int, int>();
        private Graph graph = new Graph();
        public List<AbstractVertex> abstractVertices = new List<AbstractVertex>();

        private PC _pc;
        public PC LocalPC
        {
            get
            {
                return _pc;
            }
            set
            {
                _pc = value;
            }
        }
        #endregion

        public RC()
        {

        }
        #region Main_Methodes

        public RC(string configurationFilePath, PC modulePC)
        {
            _pc = modulePC;


            InitialiseVariables(configurationFilePath);
            graph = createGraph(abstractVertices);
            Dijkstra dijkstra = new Dijkstra(_myAreaName);
            List<PathInfo> pathsInfo = new List<PathInfo>();

            if (!_myAreaName.Contains("Dom"))
            {
                pathsInfo = dijkstra.runAlgorithmForAll(graph);
                foreach (PathInfo pathInfo in pathsInfo)
                {
                    Thread.Sleep(100);
                    LocalTopology(pathInfo.beginEnd, pathInfo.Weight, pathInfo.Capacity, pathInfo.AreaName, _domainIpAddress);
                }
            }

        }

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
                    Thread.Sleep(100);
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
            if (_myAreaName.Equals("Dom_1"))
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
                        SN_2ToDomain.Add(v.ID_SN_2, v.ID_Domain);
                    }
                }
            }
            if (tmp.iptoidDictionary != null)
            {
                foreach (var v in tmp.iptoidDictionary)
                {
                    IPTOIDDictionary.Add(v.IP, v.ID);
                }
            }
            if (tmp.interdomainDictionary != null)
            {
                foreach (var v in tmp.interdomainDictionary)
                {
                    interdomainLinksDictionary.Add(v.id1, v.id2);
                }
            }
            foreach (var v in tmp.LocalTopology)
            {
                AbstractVertex tmpAbstractVertex = new AbstractVertex(v.ID, v.capacity, v.weight, v.areaName, v.reachableNodes);
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
                            currentVertex.EdgesOut.Add(edge);
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
                case SignalMessage.SignalType.RemoteTopologyStatus:
                    RemoteTopologyStatus(message.AreaName);
                    break;
                case SignalMessage.SignalType.Topology:
                    Topology(message.snppids, message.capacities, message.areaNames, message.reachableSNPPs);
                    break;
                case SignalMessage.SignalType.LocalTopology:
                    LocalTopology(message.SnppIdPair, message.LocalTopology_weight, message.LocalTopology_availibleCapacity, message.AreaName);
                    break;

            }
        }


        #endregion


        #region Incomming_Methodes_From_Standardization
        private void Topology(List<int> snppids, List<int> capacities, List<string> areaNames, List<List<int>> reachableSNPPs)
        {
            for(int i = 0; i<snppids.Count; i++)
            {
                var res = abstractVertices.Find(x => x.ID == snppids[i]);
                if(res != null)
                {
                    abstractVertices.Find(x => x.ID == snppids[i]).Capacity = capacities[i];
                    abstractVertices.Find(x => x.ID == snppids[i]).AreaName = areaNames[i];
                    abstractVertices.Find(x => x.ID == snppids[i]).ReachableNodes = reachableSNPPs[i];
                }
                else
                {
                    AbstractVertex tmpAbstractVertex = new AbstractVertex(snppids[i], capacities[i], 5, areaNames[i], reachableSNPPs[i]);
                    Console.WriteLine("id: " + snppids[i]);
                    foreach (int id in reachableSNPPs[i])
                    {
                        Console.WriteLine(id);
                    }
                    abstractVertices.Add(tmpAbstractVertex);
                }
            }
            graph = createGraph(abstractVertices);
            if (!_myAreaName.Contains("Dom"))
            {
                Dijkstra dijkstra = new Dijkstra(_myAreaName);
                var pathsInfo = dijkstra.runAlgorithmForAll(graph);
                foreach (PathInfo pathInfo in pathsInfo)
                {
                    Thread.Sleep(100);
                    LocalTopology(pathInfo.beginEnd, pathInfo.Weight, pathInfo.Capacity, pathInfo.AreaName, _domainIpAddress);
                }
            }
        }
        private void RouteQuery(int connectionID, string callingIpAddress, string calledIpAddress, int callingCapacity)
        {
            SignalMessage.Pair SNPPPair = new SignalMessage.Pair();
            SNPPPair.first = IPTOIDDictionary[callingIpAddress];
            SNPPPair.second = IPTOIDDictionary[calledIpAddress];

            Vertex begin = graph.Vertices.Find(x => x.Id == SNPPPair.first);
            Vertex end = graph.Vertices.Find(x => x.Id == SNPPPair.second);

            List<string> areaNames = new List<string>();
            List<SignalMessage.Pair> snppPairs = new List<SignalMessage.Pair>();


            if (begin.AreaName.Equals(end.AreaName))
            {
                areaNames = null;
                snppPairs.Add(SNPPPair);
                RouteQueryResponse(connectionID, snppPairs, areaNames);
            }
            else
            {
                //czy kris chce areaNames.Add(begin.AreaName); ? Wtedy areaNames to bedzie {Dom_1,Dom_2}
                areaNames.Add(end.AreaName);

                //Z ta linijka jest pairs = ({1,3},{3,201})
                SNPPPair.second = interdomainLinksDictionary[end.Id]; //Kris czy ma byc pairs = ({1,3},{3,201}) CZY pairs = ({1,201},{3,201})
                snppPairs.Add(SNPPPair);

                SignalMessage.Pair interdomainPair = new SignalMessage.Pair();
                interdomainPair.first = interdomainLinksDictionary[end.Id];
                interdomainPair.second = end.Id;
                snppPairs.Add(interdomainPair);
                RouteQueryResponse(connectionID, snppPairs, areaNames);
            }
        }

        public void RouteQuery(int connectionID, int snppInId, string calledIpAddress, int callingCapacity)
        {

            //tak samo jak dwa ip
            SignalMessage.Pair SNPPPair = new SignalMessage.Pair();
            SNPPPair.first = snppInId;
            SNPPPair.second = IPTOIDDictionary[calledIpAddress];

            Vertex begin = graph.Vertices.Find(x => x.Id == SNPPPair.first);
            Vertex end = graph.Vertices.Find(x => x.Id == SNPPPair.second);

            List<string> areaNames = new List<string>();
            List<SignalMessage.Pair> snppPairs = new List<SignalMessage.Pair>();


            if (begin.AreaName.Equals(end.AreaName))
            {
                areaNames = null;
                snppPairs.Add(SNPPPair);
                RouteQueryResponse(connectionID, snppPairs, areaNames);
            }
            else
            {
                //czy kris chce areaNames.Add(begin.AreaName); ? Wtedy areaNames to bedzie {Dom_1,Dom_2}
                areaNames.Add(end.AreaName);

                //Z ta linijka jest pairs = ({1,3},{3,201})
                SNPPPair.second = interdomainLinksDictionary[end.Id]; //Kris czy ma byc pairs = ({1,3},{3,201}) CZY pairs = ({1,201},{3,201})
                snppPairs.Add(SNPPPair);

                SignalMessage.Pair interdomainPair = new SignalMessage.Pair();
                interdomainPair.first = interdomainLinksDictionary[end.Id];
                interdomainPair.second = end.Id;
                snppPairs.Add(interdomainPair);
                RouteQueryResponse(connectionID, snppPairs, areaNames);
            }
        }




        private void RouteQuery(int connectionID, SignalMessage.Pair snppIdPair, int callingCapacity)
        {

            Dijkstra dijkstra = new Dijkstra(_myAreaName);
            Vertex begin = graph.Vertices.Find(x => x.Id == snppIdPair.first);
            Vertex end = graph.Vertices.Find(x => x.Id == snppIdPair.second);

            List<SignalMessage.Pair> snppIdPairs = dijkstra.runAlgorithm(graph, begin, end, callingCapacity);

            List<string> areaNames = new List<string>();

            foreach (SignalMessage.Pair pair in snppIdPairs)
            {
                //Vertex firstVertex = graph.Vertices.Find(x => x.Id == pair.first);
                Vertex secondVertex = graph.Vertices.Find(x => x.Id == pair.second);

                if (!secondVertex.AreaName.Equals(_myAreaName))
                {
                    String tmp = areaNames.Find(x => x == secondVertex.AreaName);
                    if (tmp == null)
                    {
                        areaNames.Add(secondVertex.AreaName);
                    }
                }
            }

            RouteQueryResponse(connectionID, snppIdPairs, areaNames);

        }
        private void RemoteTopologyStatus(string areaName)
        {
            foreach (var vertex in graph.Vertices)
            {
                vertex.EdgesOut.RemoveAll(x => x.End.AreaName.Equals(areaName));
            }
            graph.Vertices.RemoveAll(x => x.AreaName.Equals(areaName));
            graph.Edges.RemoveAll(x => x.Begin.AreaName.Equals(areaName));
            graph.Edges.RemoveAll(x => x.End.AreaName.Equals(areaName));
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
        private void LocalTopology(SignalMessage.Pair snppIdPair, double weight, int avaibleCapacity, string areaName)
        {
            int first = SN_1ToDomain[snppIdPair.first];
            int second = SN_1ToDomain[snppIdPair.second];
            if (((graph.Vertices.Find(x => x.Id == first)) != null) && ((graph.Vertices.Find(x => x.Id == second)) != null))
            {
                Edge edge = new Edge(graph.Vertices.Find(x => x.Id == first), graph.Vertices.Find(x => x.Id == second), avaibleCapacity, weight);
                graph.Vertices.Find(x => x.Id == first).EdgesOut.Add(edge);
                graph.Edges.Add(edge);
            }
        }
        public void IsUp(string areaName)
        {

        }
        public void NetworkTopology(int snppId, List<int> reachableSnppIdList)
        {
            foreach (var id in reachableSnppIdList)
                interdomainLinksDictionary.Add(id, snppId);
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
