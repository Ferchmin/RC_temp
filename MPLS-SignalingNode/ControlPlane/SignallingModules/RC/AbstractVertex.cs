using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlPlane
{

    class AbstractVertex
    {

        private int id;
        public int ID
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
            }
        }
        private int capacity;
        public int Capacity
        {
            get
            {
                return capacity;
            }
            set
            {
                capacity = value;
            }
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
        private string areaName;
        public string AreaName
        {
            get
            {
                return areaName;
            }
            set
            {
                areaName = value;
            }
        }
        private List<int> reachableNodes;
        public List<int> ReachableNodes
        {
            get
            {
                return reachableNodes;
            }
            set
            {
                reachableNodes = value;
            }
        }

        public AbstractVertex(int id, int capacity, double weight, string areaName, List<int> reachableNodes)
        {
            this.id = id;
            this.capacity = capacity;
            this.weight = weight;
            this.areaName = areaName;
            this.reachableNodes = reachableNodes;
        }


    }
}
