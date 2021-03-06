﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ControlPlane
{
    class Lrm
    {
        
        CC cc;
        private string areaName;
        private bool isActive;
        public System.Timers.Timer keepAliveTimer;
        public string AreaName
        {
            get { return areaName; }
            set { areaName = value; }
        }
        public bool IsActive
        {
            get { return isActive; }
        }

        public Lrm(string areaName, CC cc)
        {
            this.areaName = areaName;
            isActive = true;
            this.cc = cc;
            keepAliveTimer = new System.Timers.Timer();
            keepAliveTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimedEvent);
            keepAliveTimer.AutoReset = false;
            keepAliveTimer.Interval = 40000;
            keepAliveTimer.Enabled = true;
        }


        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            isActive = false;
            keepAliveTimer.Stop();
            keepAliveTimer.Close();
            cc.OnNodeFailure(areaName);
        }

    }
}
