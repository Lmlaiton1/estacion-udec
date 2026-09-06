namespace RR_Nueva_Naturaleza.Models.Dto
{
    public class EventNotifyDto
    {
        public Guid Id { get; set; }
        public string Date { get; set; }
        public string Notification { get; set; } = string.Empty;
        public bool Visto { get; set; } = false;
    }
}
