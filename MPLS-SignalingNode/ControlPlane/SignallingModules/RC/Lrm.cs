using System;
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

        public Lrm(string areaName)
        {
            this.areaName = areaName;
            isActive = true;

            keepAliveTimer = new System.Timers.Timer();
            keepAliveTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimedEvent);
            keepAliveTimer.AutoReset = false;
            keepAliveTimer.Interval = 40000;
            keepAliveTimer.Enabled = true;
        }


        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            RC myClass1 = new RC();

            MyDelegate del = new MyDelegate(myClass1.OnNodeFailure);

            IAsyncResult async = del.BeginInvoke(areaName, new AsyncCallback(MyCallBack), "A message from the main thread");
            isActive = false;
            keepAliveTimer.Stop();
            keepAliveTimer.Close();
        }
        static void MyCallBack(IAsyncResult async)

        {

            AsyncResult ar = (AsyncResult)async;

            MyDelegate del = (MyDelegate)ar.AsyncDelegate;

            del.EndInvoke(async);
        }
    }
}
