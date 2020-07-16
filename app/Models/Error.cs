namespace app.Models
{
    public class Error
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public Error(string message)
        {
            this.Message = message;
        }
        
        public Error(string message, int statusCode)
        {
            this.StatusCode = statusCode;
            this.Message = message;
        }
    }
}
