using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlPlane;

namespace MPLS_SignalingNode
{
    class Program
    {
        static void Main(string[] args)
        {
            string end = null;
            RC rc = new RC("rc_config.xml");
            PC pc = new PC("pc_config.xml", rc);
            do
            {
                end = Console.ReadLine();
            }
            while (end != "end");
            //   DeviceClass device = new DeviceClass();
            //  device.StartWorking();
        }
    }
}
