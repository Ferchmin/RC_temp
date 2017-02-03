using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlPlane
{
    class Vertex
    {
        private int id;
        public int Id
        {
            get { return id; }
            set { id = value; }
        }
        private string areaName;
        public string AreaName
        {
            get { return areaName; }
            set { areaName = value; }
        }
        private Vertex prev;
        public Vertex Prev
        {
            get { return prev; }
            set { prev = value; }
        }
        private int capacity;
        public int Capacity
        {
            get { return capacity; }
            set { capacity = value; }
        }
        private double cumulatedWeight;
        public double CumulatedWeight
        {
            get { return cumulatedWeight; }
            set { cumulatedWeight = value; }
        }

        private double weight;
        public double Weight
        {
            get
            {
                return weight;
            }
            set
            {
                weight = value;
            }
        }

        private List<Edge> edgesOut;
        public List<Edge> EdgesOut
        {
            get { return edgesOut; }
            set { edgesOut = value; }
        }

        public Vertex()
        {
            this.id = 0;
            this.edgesOut = new List<Edge>();
        }

        public Vertex(int id, int capacity, double weight, string areaName)
        {
            this.id = id;
            this.edgesOut = new List<Edge>();
            this.capacity = capacity;
            this.weight = weight;
            this.areaName = areaName;
        }

    }
}
