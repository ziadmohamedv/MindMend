using System.Collections.Concurrent;
using System.Collections.Generic;
using Mind_Mend.DTOs;

namespace Mind_Mend.Services
{
    public class CallService
    {
        private readonly ConcurrentDictionary<string, CallInfoResponse> _activeCalls = new();

        public string CreateCall(string callerId, string receiverId)
        {
            var roomId = Guid.NewGuid().ToString();

            _activeCalls[roomId] = new CallInfoResponse
            {
                RoomId = roomId,
                CallerId = callerId,
                ReceiverId = receiverId,
                Status = "active"
            };

            return roomId;
        }

        public IEnumerable<CallInfoResponse> GetActiveCalls()
        {
            return _activeCalls.Values;
        }

        public bool EndCall(string roomId)
        {
            return _activeCalls.TryRemove(roomId, out _);
        }
    }
}
