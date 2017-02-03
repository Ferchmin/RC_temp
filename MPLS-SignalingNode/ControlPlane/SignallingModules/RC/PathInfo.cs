using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DTO.ControlPlane;

namespace ControlPlane
{
    class PathInfo
    {
        public SignalMessage.Pair beginEnd = new SignalMessage.Pair();

      
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

        public PathInfo()
        {

        }

    }
}
