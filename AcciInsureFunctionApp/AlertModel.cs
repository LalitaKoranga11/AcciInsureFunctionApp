namespace AcciInsureFunctionApp
{
    public class AlertModel
    {
        public string recipient { get; set; }
        public string alertName { get; set; }
        public string alertMessage { get; set; }
        public string condition { get; set; }
        public AlertModel(string recipientMail, string alertName1, string alertMessage1, string condition1)
        {
            recipient = recipientMail;
            alertName = alertName1;
            alertMessage = alertMessage1;
            condition = condition1;
        }

    }
}
