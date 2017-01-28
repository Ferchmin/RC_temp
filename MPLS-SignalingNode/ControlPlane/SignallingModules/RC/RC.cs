using System;
using MPLS_SignalingNode;
using System.Collections.Generic;
using DTO.ControlPlane;

namespace ControlPlane
{
    class RC
    {
        #region Variables
        private string _configurationFilePath;
        private string _localPcIpAddress;

        private Graph graph = new Graph();
        
        private PC _pc;
        #endregion


        #region Main_Methodes
        public RC(string configurationFilePath)
        {
            //InitialiseVariables(configurationFilePath);
        }
        private void InitialiseVariables(string configurationFilePath)
        {
            _configurationFilePath = configurationFilePath;

            RC_XmlSchame tmp = new RC_XmlSchame();
            tmp = RC_LoadingXmlFile.Deserialization(_configurationFilePath);

            //miejsce na przypisanie zmiennych
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
                        RouteQuery(message.ConnnectionID, message.CallingIpAddress, message.CalledIpAddress, message.CallingCapacity); // Wewnatrzdomenowa wiad nr.1 
                    else
                        RouteQuery(message.ConnnectionID, message.SnppIdPair, message.CallingCapacity); // wewnatrzdomenowa wiad nr 2
                    break;
                        
                 




                case SignalMessage.SignalType.LocalTopology:
                    LocalTopology(message.LocalTopology_SnppID, message.LocalTopology_availibleCapacity, message.LocalTopology_reachableSnppIdList, message.LocalTopology_areaName);
                    break;
            }
        }


        #endregion



        #region Incomming_Methodes_From_Standardization
        //sourceIpadrees dodany w celach testowych!!!
        private void RouteQuery(int connectionID, string callingIpAddress, string calledIpAddress, int callingCapacity)
        {
            /*
            if (connectionID == 111 && callingIpAddress == "127.0.1.101" && calledIpAddress == "127.0.1.102" && callingCapacity == 1000)
                RouteQueryResponse(
                    111,
                    new List<SignalMessage.Pair>()
                    {
                        new SignalMessage.Pair() { first = 1, second = 2 }
                    },
                    null);
            */


            Dijkstra dijkstra = new Dijkstra();
            
            //Musimy pobrac Vertex.id uzywajac callingIpAddress i calledIpAddress ze slownika, potem uruchomic funkcje dijkstra.runAlgorithm(graph, vertex1, vertex2, callingCapacity)


        }
       
        private void RouteQuery(int connectionID, SignalMessage.Pair snppIdPair, int callingCapacity)
        {

            Dijkstra dijkstra = new Dijkstra();
            //Musimy pobrac Vertex.id z snppIdPair, potem uruchomic dijkstra.runAlgorithm(graph,vertex1,vertex2,callingCapacity)
            //Kris powiedzial ze otrzymaujemy wiadomosc bez sourceIpAddress



/*
            

            if (connectionID == 111 && snppIdPair.first == 1 && snppIdPair.second == 2 && callingCapacity == 1000 && sourceIpAddress == "127.0.1.201")
                RouteQueryResponse(
                    111,
                    new List<SignalMessage.Pair>()
                    {
                        new SignalMessage.Pair() { first = 1, second = 11 },
                        new SignalMessage.Pair() { first = 2, second = 12 }
                    },
                    new List<string>()
                    {
                        "SN_1"
                    });
            if (connectionID == 111 && snppIdPair.first == 1 && snppIdPair.second == 2 && callingCapacity == 1000 && sourceIpAddress == "127.0.1.202")
                RouteQueryResponse(
                    111,
                    new List<SignalMessage.Pair>()
                    {
                        new SignalMessage.Pair() { first = 1, second = 11 },
                        new SignalMessage.Pair() { first = 13, second = 31 },
                        new SignalMessage.Pair() { first = 32, second = 22 },
                        new SignalMessage.Pair() { first = 21, second = 2 }
                    },
                    new List<string>()
                    {
                        "SN_1_1",
                        "SN_1_2",
                        "SN_1_3"
                    });

            */

        }
        //klasa, ktora tworzy graf sieci
        //RC wykorzystuje graf do wyznaczania sciezek dla polaczen
        //graf jest aktualizowany z kazda informacja od LRM
        public void LocalTopology(int snppId, int availibleCapacity, List<int> reachableSnppIdList, string areaName)
        {
            var item = graph.Vertices.Find(x => x.Id == snppId);
            //przypadek kiedy wierzcholek jeszcze nie istnieje
            if (item == null)
            {
                Vertex v = new Vertex(snppId, availibleCapacity);
                foreach (var point in reachableSnppIdList)
                {
                    var res = graph.Vertices.Find(x => x.Id == point);
                    if(res == null)
                    {
                        Vertex uncompleteVertex = new Vertex(point, 0);
                        graph.Vertices.Add(uncompleteVertex);
                        double weight = double.MaxValue;
                        Edge uncompleteEdge = new Edge(v, uncompleteVertex, 0,  weight);
                        v.addEdgeOut(uncompleteEdge);
                        uncompleteVertex.addEdgeOut(uncompleteEdge);
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
                foreach (var point in reachableSnppIdList)
                {
                    var res = graph.Vertices.Find(x => x.Id == point);
                    if (res == null)
                    {
                        Vertex uncompleteVertex = new Vertex(point, 0);
                        graph.Vertices.Add(uncompleteVertex);
                        double weight = double.MaxValue;
                        Edge uncompleteEdge = new Edge(graph.Vertices.Find(x => x.Id == snppId), uncompleteVertex, 0, weight);
                        graph.Vertices.Find(x => x.Id == snppId).addEdgeOut(uncompleteEdge);
                        uncompleteVertex.addEdgeOut(uncompleteEdge);
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
                            capacity = Math.Min(graph.Vertices.Find(x => x.Id == snppId).Capacity, res.Capacity);
                        }
                        string edgeID = Edge.CreateName(graph.Vertices.Find(x => x.Id == snppId), res);
                        graph.Edges.Find(x => x.Id.Equals(edgeID)).Capacity = capacity;
                        graph.Edges.Find(x => x.Id.Equals(edgeID)).Weight = weight;
                    }
                }
            }
            //tworzenie krawedzi pomiedzy wierzcholkami z tego samego SN
            List<Vertex> area = new List<Vertex>();
            area = graph.Vertices.FindAll(x => x.AreaName.Equals(areaName));
            if(area != null)
            {
                foreach (var v in area)
                {
                    string edgeID = Edge.CreateName(graph.Vertices.Find(x => x.Id == snppId), v);
                    var res = graph.Edges.Find(x => x.Id.Equals(edgeID));
                    if((res == null) && !(v.Id.Equals(snppId.ToString())))
                    {
                        int capacity = int.MaxValue;
                        double weight = 0;
                        Edge edge = new Edge(graph.Vertices.Find(x => x.Id == snppId), graph.Vertices.Find(x => x.Id == v.Id), capacity, weight);
                        graph.Vertices.Find(x => x.Id == v.Id).addEdgeOut(edge);
                        graph.Vertices.Find(x => x.Id == snppId).addEdgeOut(edge);
                        graph.Edges.Add(edge);
                    }
                }
            }
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

            //wysyłamy żądanie do RC
            _pc.SendSignallingMessage(message);
        }



        #endregion
    }
}
