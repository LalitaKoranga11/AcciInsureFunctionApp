using Newtonsoft.Json;
using System;

namespace AcciInsureFunctionApp
{
     class CosmosDBDocument
    {
        [JsonProperty(PropertyName = "timestamp")]
        private string latestTimestamp { get; set; }

        [JsonProperty(PropertyName = "message")]
        private string messageBody { get; set; }

        [JsonProperty(PropertyName = "hashedMessage")]
        private string hashedMessageBody { get; set; }

        [JsonProperty(PropertyName = "id")]
        public Guid Id { get; set; }

        public CosmosDBDocument(string time, string message, string hashedString)
        {
            latestTimestamp = time;
            messageBody = message;
            hashedMessageBody = hashedString;
        }
    }
}