//using Microsoft.EntityFrameworkCore;
//using Mind_Mend.Data;
//using Mind_Mend.DTOs;
//using Mind_Mend.Models;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Mind_Mend.Services
//{
//    public class ChatService : IChatService
//    {
//        private readonly ApplicationDbContext _context;

//        public ChatService(ApplicationDbContext context)
//        {
//            _context = context;
//        }

//        public async Task SendMessageAsync(int senderId, SendMessageDto dto)
//        {
//            var message = new Message
//            {
//                SenderId = senderId,
//                ReceiverId = dto.ReceiverId,
//                Content = dto.Content,
//                Timestamp = DateTime.UtcNow
//            };

//            _context.Messages.Add(message);
//            await _context.SaveChangesAsync();
//        }

//        public async Task<List<Message>> GetMessagesAsync(int user1Id, int user2Id)
//        {
//            return await _context.Messages
//                .Where(m => (m.SenderId == user1Id && m.ReceiverId == user2Id)
//                         || (m.SenderId == user2Id && m.ReceiverId == user1Id))
//                .OrderBy(m => m.Timestamp)
//                .ToListAsync();
//        }

//        public async Task<List<ChatPreviewDto>> GetChatPreviewsAsync(int userId)
//        {
//            var messages = await _context.Messages
//                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
//                .OrderByDescending(m => m.Timestamp)
//                .ToListAsync();

//            var previews = messages
//                .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
//                .Select(g =>
//                {
//                    var last = g.First();
//                    return new ChatPreviewDto
//                    {
//                        UserId = last.SenderId == userId ? last.ReceiverId : last.SenderId,
//                        UserName = "", // Optional: fetch from _context.Users if needed
//                        LastMessage = last.Content,
//                        LastTimestamp = last.Timestamp,
//                        IsUnread = !last.IsRead && last.ReceiverId == userId
//                    };
//                })
//                .ToList();

//            return previews;
//        }
//    }
//}
using Microsoft.EntityFrameworkCore;
using Mind_Mend.Data;
using Mind_Mend.DTOs;
using Mind_Mend.Models;
using Mind_Mend.Models.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mind_Mend.Services
{
    public class ChatService : IChatService
    {
        private readonly MindMendDbContext _context;

        public ChatService(MindMendDbContext context)
        {
            _context = context;
        }

        public async Task SendMessageAsync(string senderId, SendMessageDto dto)
        {
            Console.WriteLine($"Attempting to send message. SenderId={senderId}, ReceiverId={dto.ReceiverId}, Content={dto.Content}");

            var sender = await _context.Users.FirstOrDefaultAsync(u => u.Id == senderId);
            var receiver = await _context.Users.FirstOrDefaultAsync(u => u.Id == dto.ReceiverId);

            Console.WriteLine($"Sender raw data: {Newtonsoft.Json.JsonConvert.SerializeObject(sender)}");
            Console.WriteLine($"Receiver raw data: {Newtonsoft.Json.JsonConvert.SerializeObject(receiver)}");
            Console.WriteLine($"Sender exists? {sender != null}, Receiver exists? {receiver != null}");

            if (sender == null || receiver == null)
                throw new ArgumentException("Sender or Receiver does not exist.");

            // Role-based validation
            if (sender.Role == Roles.Patient && receiver.Role != Roles.Therapist)
                throw new ArgumentException("Patients can only message Therapists.");

            if (sender.Role == Roles.Therapist && receiver.Role != Roles.Patient)
                throw new ArgumentException("Therapists can only message Patients.");

            if (sender.Role == Roles.Doctor && receiver.Role != Roles.Patient)
                throw new ArgumentException("Doctors can only message Patients.");

            Console.WriteLine($"Inserting message: SenderId={senderId}, ReceiverId={dto.ReceiverId}, Content={dto.Content}");
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Check or create ChatThread
                var chatThread = await _context.ChatThreads
                    .FirstOrDefaultAsync(ct => (ct.User1Id == senderId && ct.User2Id == dto.ReceiverId) ||
                                             (ct.User1Id == dto.ReceiverId && ct.User2Id == senderId));
                if (chatThread == null)
                {
                    chatThread = new ChatThread
                    {
                        User1Id = senderId,
                        User2Id = dto.ReceiverId
                        // Remove CreatedAt since it's not defined; add other properties if needed
                    };
                    _context.ChatThreads.Add(chatThread);
                    await _context.SaveChangesAsync(); // Save to get ChatThreadId
                }

                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = dto.ReceiverId,
                    Content = dto.Content,
                    Timestamp = DateTime.UtcNow,
                    IsRead = false,
                    ChatThreadId = chatThread.Id // Associate with the chat thread
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                Console.WriteLine("Message saved successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error saving message: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Message>> GetMessagesAsync(string user1Id, string user2Id)
        {
            var user1Exists = await _context.Users.AnyAsync(u => u.Id == user1Id);
            var user2Exists = await _context.Users.AnyAsync(u => u.Id == user2Id);
            if (!user1Exists || !user2Exists)
                throw new ArgumentException("One or both users do not exist.");

            return await _context.Messages
                .Where(m => (m.SenderId == user1Id && m.ReceiverId == user2Id) ||
                         (m.SenderId == user2Id && m.ReceiverId == user1Id))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }

        public async Task<List<ChatPreviewDto>> GetChatPreviewsAsync(string userId)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
                throw new ArgumentException("User does not exist.");

            var messages = await _context.Messages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync();

            var otherUserIds = messages
                .Select(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToList();

            var otherUsers = await _context.Users
                .Where(u => otherUserIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UserName);

            var previews = messages
                .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                .Select(g =>
                {
                    var last = g.First();
                    var otherUserId = last.SenderId == userId ? last.ReceiverId : last.SenderId;

                    return new ChatPreviewDto
                    {
                        UserId = otherUserId,
                        UserName = otherUsers.ContainsKey(otherUserId) ? otherUsers[otherUserId] : "Unknown",
                        LastMessage = last.Content,
                        LastTimestamp = last.Timestamp,
                        IsUnread = !last.IsRead && last.ReceiverId == userId
                    };
                })
                .ToList();

            return previews;
        }
    }
}