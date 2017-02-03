using DTO.ControlPlane;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlPlane
{
    class Dijkstra
    {
        private const double infinity = double.MaxValue;
        private Graph graph;
        private string _myAreaName;

        public Dijkstra(string myAreaName)
        {
            _myAreaName = myAreaName;
        }

        /*
        public Path runAlgorithm(Graph graph_, Vertex begin, Vertex end)
        {
            graph = graph_;

            Path shortestPath = new Path();
            this.initialize(begin);

            List<Vertex> unvisitedVertices = graph.Vertices;
            unvisitedVertices.Sort((x, y) => x.CumulatedWeight.CompareTo(y.CumulatedWeight));

            while (unvisitedVertices.Count != 0)
            {
                Vertex currentVertex = unvisitedVertices.First();
                unvisitedVertices.Remove(currentVertex);
                if (currentVertex.CumulatedWeight == infinity)
                {
                    break;
                }

                foreach (Edge e in currentVertex.EdgesOut)
                {
                    Vertex neighbor = e.End;

                    if (neighbor.CumulatedWeight > currentVertex.CumulatedWeight + e.Weight)
                    {
                        neighbor.CumulatedWeight = currentVertex.CumulatedWeight + e.Weight;
                        neighbor.Prev = currentVertex;
                    }


                }

                unvisitedVertices.Sort((x, y) => x.CumulatedWeight.CompareTo(y.CumulatedWeight));
            }

            shortestPath = generatePath(begin, end);
            return shortestPath;
        }
        */


        public List<SignalMessage.Pair> runAlgorithm(Graph graph_, Vertex begin, Vertex end, int capacity)
        {
            graph = new Graph(graph_);

            List<SignalMessage.Pair> pairsOfVertices = new List<SignalMessage.Pair>();
            this.initialize(begin);

            List<Vertex> unvisitedVertices = graph.Vertices;
            unvisitedVertices.Sort((x, y) => x.CumulatedWeight.CompareTo(y.CumulatedWeight));

            while (unvisitedVertices.Count != 0)
            {
                Vertex currentVertex = unvisitedVertices.First();
                unvisitedVertices.Remove(currentVertex);
                if (currentVertex.CumulatedWeight == infinity)
                {
                    break;
                }

                foreach (Edge e in currentVertex.EdgesOut)
                {

                    Vertex neighbor = e.End;

                    if (neighbor.Capacity >= capacity)
                    {
                        if (neighbor.CumulatedWeight > currentVertex.CumulatedWeight + e.Weight)
                        {
                            neighbor.CumulatedWeight = currentVertex.CumulatedWeight + e.Weight;
                            neighbor.Prev = currentVertex;
                        }
                    }
                    else
                    {
                        neighbor.CumulatedWeight = infinity;
                    }


                }

                unvisitedVertices.Sort((x, y) => x.CumulatedWeight.CompareTo(y.CumulatedWeight));
            }

            pairsOfVertices = generatePairs(begin, end);
            return pairsOfVertices;
        }

        public PathInfo runAlgorithm(Graph graph_, Vertex begin, Vertex end)
        {
            graph = new Graph(graph_);
            PathInfo pathInfo = new PathInfo();

            List<SignalMessage.Pair> pairsOfVertices = new List<SignalMessage.Pair>();
            this.initialize(begin);

            List<Vertex> unvisitedVertices = graph.Vertices;
            unvisitedVertices.Sort((x, y) => x.CumulatedWeight.CompareTo(y.CumulatedWeight));

            while (unvisitedVertices.Count != 0)
            {
                Vertex currentVertex = unvisitedVertices.First();
                unvisitedVertices.Remove(currentVertex);
                if (currentVertex.CumulatedWeight == infinity)
                {
                    break;
                }

                foreach (Edge e in currentVertex.EdgesOut)
                {
                    Vertex neighbor = e.End;
                    if (neighbor.CumulatedWeight > currentVertex.CumulatedWeight + e.Weight)
                    {
                        neighbor.CumulatedWeight = currentVertex.CumulatedWeight + e.Weight;
                        pathInfo.Capacity = Math.Min(currentVertex.Capacity, currentVertex.Prev.Capacity);
                        neighbor.Prev = currentVertex;
                    }

                }

                unvisitedVertices.Sort((x, y) => x.CumulatedWeight.CompareTo(y.CumulatedWeight));
            }

            Vertex vertexForPath = end;
            bool didFindPath = true;
            while (vertexForPath != begin)
            {
                if (vertexForPath.Prev == null)
                {
                    didFindPath = false;
                }
                else
                {
                    vertexForPath = vertexForPath.Prev;
                }
            }

            pathInfo.Weight = end.CumulatedWeight;
            pathInfo.beginEnd.first = begin.Id;
            pathInfo.beginEnd.second = end.Id;
            pathInfo.AreaName = _myAreaName;

            if (didFindPath)
            {
                return pathInfo;
            }
            else
            {
                return null;
            }
        }

        public List<PathInfo> runAlgorithmForAll(Graph graph)
        {
            List<PathInfo> pathsInfo = new List<PathInfo>();
            foreach (Vertex begin in graph.Vertices)
            {
                if (begin.AreaName.Equals(_myAreaName))
                {
                    foreach (Vertex end in graph.Vertices)
                    {
                        if (end.AreaName.Equals(_myAreaName))
                        {
                            if (end.Id != begin.Id)
                            {
                                PathInfo pathInfo = runAlgorithm(graph, begin, end);
                                if(pathInfo != null)
                                {
                                    pathsInfo.Add(pathInfo);
                                }
                            }
                        }
                    }
                }
            }
            return pathsInfo;
        }


        private List<SignalMessage.Pair> generatePairs(Vertex begin, Vertex end)
        {
            List<SignalMessage.Pair> list = new List<SignalMessage.Pair>();
            Vertex currentVertex = end;

            while (currentVertex != begin)
            {
                if (currentVertex == null)
                {
                    return null;
                }

                String edgeId = Edge.CreateName(currentVertex.Prev, currentVertex);
                Edge tmpEdge = graph.Edges.Find(x => x.Id == edgeId);
                if (!tmpEdge.SubDomain)
                {
                    SignalMessage.Pair pair = new SignalMessage.Pair() { first = currentVertex.Prev.Id, second = currentVertex.Id };
                    list.Insert(0, pair);
                }
                currentVertex = currentVertex.Prev;
            }
            return list;
        }

        //Initializer, sets Wieght of every vertex to infinity, and the Weight of the begining node to 0
        private void initialize(Vertex begin)
        {

            for (int i = 0; i < graph.Vertices.Count; i++)
            {
                graph.Vertices[i].CumulatedWeight = infinity;
                graph.Vertices[i].Prev = null;
            }
            begin.CumulatedWeight = 0;
            begin.Prev = begin;

        }

        private List<Vertex> generateVerticesList(Vertex begin, Vertex end)
        {
            List<Vertex> listOfVertices = new List<Vertex>();
            Vertex currentVertex = end;

            while(currentVertex != begin)
            {
                if (currentVertex == null)
                {
                    return null;
                }
                listOfVertices.Add(currentVertex);
                currentVertex = currentVertex.Prev;
            }

            listOfVertices.Add(begin);
            return listOfVertices;
        }

        private Path generatePath(Vertex begin, Vertex end)
        {
            Path path = new Path();
            Vertex currentVertex = end;

            while (currentVertex != begin)
            {
                if (currentVertex == null)
                {
                    return null;
                }
                path.push(currentVertex);
                currentVertex = currentVertex.Prev;
            }
            path.push(begin);

            return path;
        }

    }
}
