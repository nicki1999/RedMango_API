using System.Net;

namespace RedMango_API.Model
{
    public class ApiResponse
    {
        public ApiResponse()
        {
            ErrorMessages = new List<string>();
        }

        public HttpStatusCode statusCode { get; set; }
        public bool isSuccess { get; set; } = true; 
        public List<string> ErrorMessages { get; set; }
        public object Result { get; set; }
    }
}
