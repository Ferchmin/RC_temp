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
            set { vertices = value; }
        }

        private List<Edge> edges;
        public List<Edge> Edges
        {
            get { return edges; }
            set { edges = value; }
        }

        public Graph()
        {
            vertices = new List<Vertex>();
            edges = new List<Edge>();
        }

        public Graph(Graph graph)
        {
            Edge[] tmpEdges = new Edge[graph.Edges.Count];
            graph.Edges.CopyTo(tmpEdges);
            edges = new List<Edge>();
            foreach (Edge edge in tmpEdges)
            {
                edges.Add(edge);
            }

            Vertex[] tmpVertices = new Vertex[graph.Vertices.Count];
            graph.Vertices.CopyTo(tmpVertices);
            vertices = new List<Vertex>();
            foreach (Vertex vertex in tmpVertices)
            {
                vertices.Add(vertex);
            }
        }

    }
}
