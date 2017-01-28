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

        public Path[] runAlgorithm(Graph graph_, Vertex begin, Vertex end)
        {
            graph = graph_;
            Path[] widestPath = new Path[graph.Vertices.Count];


            widestPath[0] = generatePath(begin, end);

            return widestPath;
        }


        private void initialize(Vertex begin)
        {

            for (int i = 0; i < graph.Vertices.Count; i++)
            {
                graph.Vertices[i].CumulatedWeight = 0;
                graph.Vertices[i].Prev = null;
            }

            graph.Vertices[begin.Id - 1].CumulatedWeight = infinity;

            begin.Prev = begin;
        }

        private Path generatePath(Vertex begin, Vertex end)
        {
            Path path = new Path(graph.Vertices.Count);
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
