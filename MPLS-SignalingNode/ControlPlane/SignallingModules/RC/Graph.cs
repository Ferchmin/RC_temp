using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlPlane
{
    class Graph
    {
        private List<Vertex> vertices;
        public List<Vertex> Vertices
        {
            get { return vertices; }
        }

        private List<Edge> edges;
        public List<Edge> Edges
        {
            get { return edges; }
        }

        public Graph()
        {
            vertices = new List<Vertex>();
            edges = new List<Edge>();
        }

        public Graph Copy(Graph graph)
        {
            Graph tmpGraph = new Graph();
            tmpGraph.edges = graph.Edges;
            tmpGraph.vertices = graph.Vertices;
            return tmpGraph;
        }

    }
}
