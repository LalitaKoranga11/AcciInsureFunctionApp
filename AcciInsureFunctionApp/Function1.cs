using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using FunctionApp1;

namespace AcciInsureFunctionApp
{
    public static class Function1
    {
        /*
         * 
         Resources to Create:
        1. Eventhub Namespace(ConnectionString) > Eventhub(Name)
        2. Storage Account (Container) - For EventhubConsumer, (Queue) - for Storage Queues
        3. CosmosDB -> Cosmos Database -> Cosmoso Container inside DB
        4. Service Bus Namespace(ConnectionString) > Queue(ServiceBusQueue)
         */
        [FunctionName("Function1")]
        public static async Task Run([EventHubTrigger("myeventhub", Connection = "EventhubConnectionString")] EventData[] events, [Queue("deadletterqueue")] IAsyncCollector<string> deadLetterMessages, ILogger log)
        {
            var exceptions = new List<Exception>();
            
            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                    string hashedMessage = convertToSHA256(messageBody);
                    string timeStamp = GetTimestamp(DateTime.Now);

                    CosmosClient cosmos = new CosmosClient("AccountEndpoint=https://democosmosdbacc.documents.azure.com:443/;AccountKey=UMEwfXnsBRWul65UZrn7LHHZ0COAc1Svk67RAwq7F6wmDb3lPNJRh0sDb6fgc3xM162JdKBBY3daRvycmCtYFg==;");
                    var container = cosmos.GetContainer("demoDB", "demoContainerId");// databaseId, containerId
                    //var result = JsonConvert.DeserializeObject<CosmosDBDocument>(hashedMessage); //Session is a Class for Incoming data
                    var result = new CosmosDBDocument(timeStamp, messageBody, hashedMessage);
                    result.Id = Guid.NewGuid();
                    await container.CreateItemAsync(result);

                    log.LogInformation("Saved the event successfully in cosmosDB");
                    log.LogInformation($"C# Event Hub trigger function processed a message: {messageBody}");
                    await deadLetterMessages.AddAsync(Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count));
                    log.LogInformation("Saved Messages in Storage Queue");

                    //sending alerts to ServiceBusQueue
                    // using service bus queue which triggers the logic App
                    string serviceBusConString = "Endpoint=sb://lalitaservicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=Umu/rUijug2RdQfrxnEgmJM2teEYm1yo6z7cW2UBVGI=";
                    string queueName = "alert";
                    // fetch these alert model details from api exposed to interact with DB 
                    //AlertModel alertModel = dbService.getALertDetails("deviceId");
                    AlertModel alertModel = new AlertModel("lalitakoranga@gmail.com", "Test Alert", " This is the testAlert", "condition");
                    ServiceBusClient client = new ServiceBusClient(serviceBusConString);
                    ServiceBusSender sender = client.CreateSender(queueName);

                    if(ConditionVoilated(eventData)== true)
                    {
                        sendAlerts(sender, client, alertModel);
                        log.LogInformation("Trigger Condition Voilated, Sent Alerts to Service bus queue");
                    }

                    // sending alerts just to check if alerting working properly.
                    log.LogInformation("Successfully Sent data to Service Bus Queue");
                    sendAlerts(sender, client, alertModel);

                    await Task.Yield();
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    await deadLetterMessages.AddAsync(Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count));
                    exceptions.Add(e);
                }
            }


            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.

            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();
        }

        private static bool ConditionVoilated(EventData eventData)
        {
            string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
            MessageModel result = JsonConvert.DeserializeObject<MessageModel>(messageBody);
            bool trigger = result.condition;
            if (trigger == true) return true;

            return false;
        }

        public static String convertToSHA256(String message)
        {
            StringBuilder Sb = new StringBuilder();

            using (SHA256 hash = SHA256Managed.Create())
            {
                Encoding enc = Encoding.UTF8;
                Byte[] result = hash.ComputeHash(enc.GetBytes(message));

                foreach (Byte b in result)
                    Sb.Append(b.ToString("x2"));
            }
            return Sb.ToString();
        }
        public static String GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssffff");
        }

        public static async void sendAlerts(ServiceBusSender sender, ServiceBusClient client, AlertModel message)
        {
            using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();
            // try adding a message to the batch
            var jsonStringMessage = Newtonsoft.Json.JsonConvert.SerializeObject(message);
           //// messageBatch.TryAddMessage(new ServiceBusMessage(jsonStringMessage));
            try
            {
                // Use the producer client to send the batch of messages to the Service Bus queue
                //await sender.SendMessagesAsync(messageBatch);
                await sender.SendMessageAsync(new ServiceBusMessage(jsonStringMessage));
            }
            finally
            {
                // Calling DisposeAsync on client types is required to ensure that network
                // resources and other unmanaged objects are properly cleaned up.
                await sender.DisposeAsync();
                await client.DisposeAsync();
            }
        }

    }
}
