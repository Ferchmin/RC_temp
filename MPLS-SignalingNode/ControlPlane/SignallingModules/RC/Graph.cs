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

        }
        
        //Dodaj metody addVertex i addEdge
    }
}
