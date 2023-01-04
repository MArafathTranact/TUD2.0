using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using TUD2._0.Classes;

namespace TUD2._0
{
    public partial class Service1 : ServiceBase
    {
        #region Properties

        WebSocketListener listener;
        #endregion
        public Service1()
        {
            InitializeComponent();
           // ConnectSocketListener();
        }

        protected override void OnStart(string[] args)
        {
            Logger.LogWithNoLock($" Service Started ");
            Logger.LogWithNoLock($"-------- Maximum file size for the log is 100 MB --------");

            try
            {
                Task.Factory.StartNew(() =>
                {
                    ConnectSocketListener();
                });
            }
            catch (Exception)
            {

            }

            //var logsize = int.Parse(ServiceConfiguration.GetFileLocation("LogSize"));
        }

        protected override void OnStop()
        {
            try
            {
                Logger.LogWithNoLock($" Stoping Service..");

                listener.CloseWebSocket();
                Logger.LogWithNoLock($" Service stopped ");
                Task.Delay(1000);
                NLog.LogManager.Shutdown();


            }
            catch (Exception)
            {
                Logger.LogWithNoLock($" Service stopped ");
                Task.Delay(1000);
                NLog.LogManager.Shutdown();
            }
        }

        private async Task ConnectSocketListener()
        {
            try
            {
                listener = new WebSocketListener();
                listener.ConnectWebSocket();
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
