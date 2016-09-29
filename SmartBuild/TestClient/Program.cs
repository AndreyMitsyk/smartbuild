using System;
using Microsoft.Azure;
using Microsoft.ServiceBus.Messaging;

namespace AzureService
{
    static class Program
    {
        private static void Main()
        {
            string connectionString =
                CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");
            QueueClient client =
                QueueClient.CreateFromConnectionString(connectionString, "build");
            ReadMessage(client);
            Console.WriteLine("Started!");
            Console.ReadLine();
            client.Close();
        }

        private static void ReadMessage(QueueClient client)
        {
            OnMessageOptions options = new OnMessageOptions
            {
                AutoComplete = false,
                AutoRenewTimeout = TimeSpan.FromMinutes(1)
            };

            client.OnMessage(message =>
            {
                try
                {
                    var mail = message.Properties["buildstatus"].ToString();
                    Console.WriteLine(mail);

                    message.Complete();
                }
                catch (Exception)
                {
                    message.Abandon();
                }
            }, options);
        }
    }
}
