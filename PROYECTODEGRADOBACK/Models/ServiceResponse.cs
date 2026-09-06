namespace RR_Nueva_Naturaleza.Models
{
    public enum ServiceResponseType
    {
        Succeded,
        Failed
    }
    public class ServiceResponse
    {
        public ServiceResponseType Result { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string InformationMessage { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}
