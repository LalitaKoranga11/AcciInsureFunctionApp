namespace AcciInsureFunctionApp
{
    public class AlertModel
    {
        private string recipient { get; set; }
        private string alertName { get; set; }
        private string alertMessage { get; set; }
        private string condition { get; set; }
        public AlertModel(string recipientMail, string alertName1, string alertMessage1, string condition1)
        {
            recipient = recipientMail;
            alertName = alertName1;
            alertMessage = alertMessage1;
            condition = condition1;
        }

    }
}