using System;
using MPLS_SignalingNode;
using System.Collections.Generic;
using DTO.ControlPlane;

namespace ControlPlane
{
    public delegate void MyDelegate(string area);
    class RC
    {
        #region Variables
        private string _configurationFilePath;
        private string _localPcIpAddress;
        private string _myAreaName;
        private Dictionary<int, int> interdomainLinks = new Dictionary<int, int>();
        private Dictionary<string, int> IPTOIDDictionary = new Dictionary<string, int>();
        private Graph graph = new Graph();
        private List<Lrm> LRMs = new List<Lrm>();
        private PC _pc;
        #endregion

        public RC()
        {

        }
        #region Main_Methodes
        public RC(string configurationFilePath)
        {
            InitialiseVariables(configurationFilePath);
        }
        private void InitialiseVariables(string configurationFilePath)
        {
            _configurationFilePath = configurationFilePath;

            RC_XmlSchame tmp = new RC_XmlSchame();
            tmp = RC_LoadingXmlFile.Deserialization(_configurationFilePath);
            _localPcIpAddress = tmp.XML_myIPAddress;
            _myAreaName = tmp.XMP_myAreaName;
            foreach (var v in tmp.Dictionary)
            {
                IPTOIDDictionary.Add(v.IP, v.ID);
            }
        }
        #endregion


        #region Properties
        public PC LocalPC { set { _pc = value; } }
        #endregion


        #region PC_Cooperation_Methodes
        private void SendMessageToPC(SignalMessage message)
        {
            _pc.SendSignallingMessage(message);
            SignallingNodeDeviceClass.MakeSignallingLog("RC", "INFO - Signalling message send to PC module");
        }
        public void ReceiveMessageFromPC(SignalMessage message)
        {
            switch (message.General_SignalMessageType)
            {
                case SignalMessage.SignalType.RouteQuery:
                    if (message.CalledIpAddress != null)
                    {
                        RouteQuery(message.ConnnectionID, message.CallingIpAddress, message.CalledIpAddress, message.CallingCapacity); // Wewnatrzdomenowa wiad nr.1 
                    }else
                    {
                        RouteQuery(message.ConnnectionID, message.SnppIdPair, message.CallingCapacity); // wewnatrzdomenowa wiad nr 2
                    }
                       
                    break;

                case SignalMessage.SignalType.IsUp:
                    IsUp(message.IsUpKeepAlive_areaName);

                    break;

                case SignalMessage.SignalType.KeepAlive:
                    KeepAlive(message.IsUpKeepAlive_areaName);
                    break;




                case SignalMessage.SignalType.LocalTopology:
                    
                    if (_myAreaName.Equals("Domena_1"))
                    {
                        List<int> otherDomain = message.LocalTopology_reachableSnppIdList.FindAll(x => x > 199);
                        message.LocalTopology_reachableSnppIdList.RemoveAll(x => x > 199);
                        if (otherDomain != null)
                            NetworkTopology(message.LocalTopology_SnppID, otherDomain);
                        LocalTopology(message.LocalTopology_SnppID, message.LocalTopology_availibleCapacity, message.LocalTopology_reachableSnppIdList, message.LocalTopology_areaName);
                    }
                    else if (_myAreaName.Equals("Domena_2"))
                    {
                        List<int> otherDomain = message.LocalTopology_reachableSnppIdList.FindAll(x => x < 100);
                        message.LocalTopology_reachableSnppIdList.RemoveAll(x => x < 100);
                        if (otherDomain != null)
                            NetworkTopology(message.LocalTopology_SnppID, otherDomain);
                        LocalTopology(message.LocalTopology_SnppID, message.LocalTopology_availibleCapacity, message.LocalTopology_reachableSnppIdList, message.LocalTopology_areaName);
                    }
                    else
                                        
                        LocalTopology(message.LocalTopology_SnppID, message.LocalTopology_availibleCapacity, message.LocalTopology_reachableSnppIdList, message.LocalTopology_areaName);
                    break;
            }
        }


        #endregion



        #region Incomming_Methodes_From_Standardization
        //sourceIpadrees dodany w celach testowych!!!
        private void RouteQuery(int connectionID, string callingIpAddress, string calledIpAddress, int callingCapacity)
        {
            SignalMessage.Pair SNPPPair = new SignalMessage.Pair();
            SNPPPair.first = IPTOIDDictionary[callingIpAddress];
            SNPPPair.second = IPTOIDDictionary[calledIpAddress];

            SignalMessage signalMessage = new SignalMessage();
            signalMessage.SnppIdPair = SNPPPair;

            RouteQueryResponse(connectionID, SNPPPair, signalMessage.ConnnectionID);


            //Dijkstra dijkstra = new Dijkstra();
            
            //Musimy pobrac Vertex.id uzywajac callingIpAddress i calledIpAddress ze slownika, potem uruchomic funkcje dijkstra.runAlgorithm(graph, vertex1, vertex2, callingCapacity)


        }
       
        private void RouteQuery(int connectionID, SignalMessage.Pair snppIdPair, int callingCapacity)
        {

            Dijkstra dijkstra = new Dijkstra();
            Vertex begin = graph.Vertices.Find(x => x.Id == snppIdPair.first);
            Vertex end = graph.Vertices.Find(x => x.Id == snppIdPair.second);

            Vertex currentVertex = end;
            List<Vertex> listOfVertices = dijkstra.runAlgorithm(graph, begin, end, callingCapacity);
            listOfVertices.Reverse();

            List<SignalMessage.Pair> snppIdPairs = new List<SignalMessage.Pair>();
            List<string> areaNames = new List<string>();

            while(currentVertex.Prev != begin)
            {
                SignalMessage.Pair pair = new SignalMessage.Pair();
                pair.first = currentVertex.Id;
                pair.second = currentVertex.Prev.Id;
                snppIdPairs.Add(pair);
                areaNames.Add(currentVertex.AreaName);
                currentVertex = currentVertex.Prev.Prev;
            }
            SignalMessage.Pair lastPair = new SignalMessage.Pair();
            lastPair.first = currentVertex.Id;
            lastPair.second = currentVertex.Prev.Id;
            snppIdPairs.Add(lastPair);
            areaNames.Add(currentVertex.AreaName);


            RouteQueryResponse(connectionID, snppIdPairs, areaNames);


        }
        //klasa, ktora tworzy graf sieci
        //RC wykorzystuje graf do wyznaczania sciezek dla polaczen
        //graf jest aktualizowany z kazda informacja od LRM
        public void IsUp(string areaName)
        {
            var lrm = LRMs.Find(x => x.AreaName.Equals(areaName));
            if (lrm == null)
            {
                Lrm l = new Lrm(areaName);
                LRMs.Add(l);
            }
            else
                KeepAlive(areaName);
        }
        public void KeepAlive(string areaName)
        {
            var item = LRMs.Find(x => x.AreaName.Equals(areaName));
            if(item != null)
            {
                LRMs.Find(x => x.AreaName.Equals(areaName)).keepAliveTimer.Stop();
                LRMs.Find(x => x.AreaName.Equals(areaName)).keepAliveTimer.Start();
            }

        }
        public void LocalTopology(int snppId, int availibleCapacity, List<int> reachableSnppIdList, string areaName)
        {
            Console.WriteLine(availibleCapacity);
            var item = graph.Vertices.Find(x => x.Id == snppId);
            //przypadek kiedy wierzcholek jeszcze nie istnieje
            if (item == null)
            {
                Vertex v = new Vertex(snppId, availibleCapacity, areaName);
                foreach (var point in reachableSnppIdList)
                {
                    var res = graph.Vertices.Find(x => x.Id == point);
                    if(res == null)
                    {
                        Vertex uncompleteVertex = new Vertex(point, 0, "unreachable");
                        graph.Vertices.Add(uncompleteVertex);
                        double weight = double.MaxValue;
                        Edge uncompleteEdge = new Edge(v, uncompleteVertex, 0,  weight);
                        v.addEdgeOut(uncompleteEdge);
                        graph.Edges.Add(uncompleteEdge);
                    }
                    else
                    {
                        double weight;
                        int capacity;
                        if (res.AreaName.Equals(areaName))
                        {
                            weight = 0;
                            capacity = int.MaxValue;
                        }
                        else
                        {
                            weight = 1;
                            capacity = Math.Min(v.Capacity, res.Capacity);
                        }
                        Edge edge = new Edge(v, res, capacity, weight);
                        graph.Vertices.Find(x => x.Id == point).addEdgeOut(edge);
                    }
                }
                graph.Vertices.Add(v);
            }
            //wierzcholek juz istnieje
            else
            {
                graph.Vertices.Find(x => x.Id == snppId).Capacity = availibleCapacity;
                graph.Vertices.Find(x => x.Id == snppId).AreaName = areaName;
                foreach (var point in reachableSnppIdList)
                {
                    var res = graph.Vertices.Find(x => x.Id == point);
                    if (res == null)
                    {
                        Vertex uncompleteVertex = new Vertex(point, 0, "unreachable");
                        graph.Vertices.Add(uncompleteVertex);
                        double weight = double.MaxValue;
                        Edge uncompleteEdge = new Edge(graph.Vertices.Find(x => x.Id == snppId), uncompleteVertex, 0, weight);
                        graph.Vertices.Find(x => x.Id == snppId).addEdgeOut(uncompleteEdge);
                        graph.Edges.Add(uncompleteEdge);
                    }
                    else
                    {
                        Edge existingEdge = graph.Edges.Find(x => x.Id.Equals(Edge.CreateName(snppId, res.Id)));
                        if ((existingEdge != null) && !(res.AreaName.Equals("unreachable")))
                        {
                            if (existingEdge.Weight == double.MaxValue)
                            {
                            }
                            else
                            {
                            }
                        }
                        else if (existingEdge == null)
                        {

                            double weight;
                            int capacity;
                            if (res.AreaName.Equals(areaName))
                            {
                                weight = 0;
                                capacity = int.MaxValue;
                            }
                            else
                            {
                                weight = 1;
                                capacity = Math.Min(item.Capacity, res.Capacity);
                            }
                            Edge edge = new Edge(graph.Vertices.Find(x=> x.Id == snppId), graph.Vertices.Find(x=> x.Id == res.Id), capacity, weight);
                            graph.Vertices.Find(x => x.Id == snppId).addEdgeOut(edge);
                            graph.Edges.Add(edge);
                        }
                        else
                        {
                            double weight;
                            int capacity;
                            if (res.AreaName.Equals(areaName))
                            {
                                weight = 0;
                                capacity = int.MaxValue;
                            }
                            else
                            {
                                weight = 1;
                                capacity = Math.Min(item.Capacity, res.Capacity);
                            }
                            graph.Edges.Find(x => x.Id.Equals(Edge.CreateName(snppId, res.Id))).Capacity = capacity;
                            graph.Edges.Find(x => x.Id.Equals(Edge.CreateName(snppId, res.Id))).Weight = weight;

                        }
                    }
                }
            }
            //tworzenie krawedzi pomiedzy wierzcholkami z tego samego SN
            if (!areaName.Equals(_myAreaName))
            {
                List<Vertex> area = new List<Vertex>();
                area = graph.Vertices.FindAll(x => x.AreaName.Equals(areaName));
                if (area != null)
                {
                    foreach (var v in area)
                    {
                        string edgeID = Edge.CreateName(graph.Vertices.Find(x => x.Id == snppId), v);
                        var res = graph.Edges.Find(x => x.Id.Equals(edgeID));
                        if ((res == null) && (v.Id != snppId))
                        {
                            int capacity = int.MaxValue;
                            double weight = 0;
                            Edge edge = new Edge(graph.Vertices.Find(x => x.Id == snppId), graph.Vertices.Find(x => x.Id == v.Id), capacity, weight);
                            graph.Vertices.Find(x => x.Id == snppId).addEdgeOut(edge);
                            graph.Edges.Add(edge);
                        }
                        edgeID = Edge.CreateName(v, graph.Vertices.Find(x => x.Id == snppId));
                        res = graph.Edges.Find(x => x.Id.Equals(edgeID));
                        if ((res == null) && (v.Id != snppId))
                        {
                            int capacity = int.MaxValue;
                            double weight = 0;
                            Edge edge = new Edge(graph.Vertices.Find(x => x.Id == v.Id), graph.Vertices.Find(x => x.Id == snppId), capacity, weight);
                            graph.Vertices.Find(x => x.Id == v.Id).addEdgeOut(edge);
                            graph.Edges.Add(edge);
                        }
                    }
                }
            }
            //sprawdzam czy w kierunku SNPP, ktorego rozgloszenie wlasnie otrzymalismy, nie sa skierowane jakies sciezki widma
            if (item != null)
            {
                foreach (var v in graph.Vertices)
                {
                    string edgeID = Edge.CreateName(v.Id, snppId);
                    var res = graph.Edges.Find(x => x.Id.Equals(edgeID));
                    if (res != null)
                    {
                        if (res.Weight == double.MaxValue)
                        {
                            double weight;
                            int capacity;
                            if (v.AreaName.Equals(areaName))
                            {
                                weight = 0;
                                capacity = int.MaxValue;
                            }
                            else
                            {
                                weight = 1;
                                capacity = Math.Min(graph.Vertices.Find(x => x.Id == snppId).Capacity, res.Capacity);
                            }
                            graph.Edges.Find(x => x.Id.Equals(edgeID)).Capacity = capacity;
                            graph.Edges.Find(x => x.Id.Equals(edgeID)).Weight = weight;
                        }
                    }
                }
            }
        }


        public void NetworkTopology (int snppId, List<int> reachableSnppIdList)
        {
            foreach (var id in reachableSnppIdList)
                interdomainLinks.Add(snppId, id);
        }
        #endregion



        #region Outcomming_Methodes_From_Standardization

        private void RouteQueryResponse(int connectionID, List<SignalMessage.Pair> includedSnppIdPairs, List<string> includedAreaNames)
        {
            SignalMessage message = new SignalMessage()
            {
                General_SignalMessageType = SignalMessage.SignalType.RouteQueryResponse,
                General_SourceIpAddress = _localPcIpAddress,
                General_DestinationIpAddress = _localPcIpAddress,
                General_SourceModule = "RC",
                General_DestinationModule = "CC",

                ConnnectionID = connectionID,
                IncludedSnppIdPairs = includedSnppIdPairs,
                IncludedAreaNames = includedAreaNames
            };

            _pc.SendSignallingMessage(message);
        }

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
