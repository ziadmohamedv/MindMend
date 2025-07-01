namespace Mind_Mend.DTOs
{
    public class CreateCallRequest
    {
        public string CallerId { get; set; }
        public string ReceiverId { get; set; }
    }

    public class EndCallRequest
    {
        public string RoomId { get; set; }
    }

    public class CallInfoResponse
    {
        public string RoomId { get; set; }
        public string CallerId { get; set; }
        public string ReceiverId { get; set; }
        public string Status { get; set; } // "active", "ended", etc.
    }
}
