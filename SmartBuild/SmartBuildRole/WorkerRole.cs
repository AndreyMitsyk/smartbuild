using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Azure;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus;

namespace SmartBuildRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        private const string queueName = "build";
        private const string messageField = "buildstatus";
        QueueClient client = null;

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
            string connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");
            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
            if (!namespaceManager.QueueExists(queueName))
            {
                namespaceManager.CreateQueue(queueName);
            }

            // Initialize the connection to Service Bus Queue
            client = QueueClient.CreateFromConnectionString(connectionString, queueName);

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
            while (!cancellationToken.IsCancellationRequested)
            {
                BrokeredMessage message = new BrokeredMessage();

                // TODO: replace this with a real logic!
                string buildStatus = "broken";

                message.Properties[messageField] = buildStatus;

                await client.SendAsync(message);

                Trace.TraceInformation($"Build status: {buildStatus}");
                await Task.Delay(30000);
            }
        }
    }
}
