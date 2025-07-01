using Mind_Mend.DTOs;
using Mind_Mend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mind_Mend.Services
{
    public interface IChatService
    {
        Task SendMessageAsync(string senderId, SendMessageDto dto);
        Task<List<Message>> GetMessagesAsync(string user1Id, string user2Id);
        Task<List<ChatPreviewDto>> GetChatPreviewsAsync(string userId);
    }
}
