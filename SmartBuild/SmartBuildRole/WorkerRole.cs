using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure;
using TeamCitySharp;

namespace SmartBuildRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        private const string hubName = "smartbuildhub";
        NotificationHubClient hub = null;
        private string buildId = "0";

        readonly ManualResetEvent completedEvent = new ManualResetEvent(false);

        public override void Run()
        {
            Trace.TraceInformation("SmartBuildRole is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // Create the queue if it does not exist already
            string connectionString = CloudConfigurationManager.GetSetting("Microsoft.NotificationHub.ConnectionString");

            hub = NotificationHubClient.CreateClientFromConnectionString(connectionString, hubName);

            bool result = base.OnStart();
            Trace.TraceInformation("SmartBuildRole has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("SmartBuildRole is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("SmartBuildRole has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            var client = new TeamCityClient("52.169.222.26:8090");
            client.ConnectAsGuest();

            while (!cancellationToken.IsCancellationRequested)
            {
                var lastBuild = client.Builds.LastBuildByAgent("localhost");
                if (buildId != lastBuild.Id)
                {
                    buildId = lastBuild.Id;
                    string buildStatus = lastBuild.Status;

                    var toast = @"<toast><visual><binding template=""ToastText01""><text id=""1"">" + buildStatus + "</text></binding></visual></toast>";
                    await hub.SendWindowsNativeNotificationAsync(toast);

                    Trace.TraceInformation($"Build status: {buildStatus}");
                }

                await Task.Delay(10000);
            }
        }
    }
}
