namespace AcciInsureFunctionApp
{
    //Class represnt incoming message format
    public class MessageModel
    {
        public string deviceId { get; set; }
        public string message { get; set; }
        public bool condition { get; set; }

        public MessageModel(string d, string m, bool c)
        {
            deviceId = d;
            message = m;
            condition = c;
        }

    }
}