using System;

namespace Mind_Mend.DTOs
{
    public class ChatPreviewDto
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string LastMessage { get; set; }
        public DateTime LastTimestamp { get; set; }
        public bool IsUnread { get; set; }
    }
}
