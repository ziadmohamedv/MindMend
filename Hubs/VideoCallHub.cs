using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class VideoCallHub : Hub
{
    // keep track of active rooms (for discovery/debugging)
    private static readonly HashSet<string> ActiveRooms = new HashSet<string>();

    public Task<string> CreateRoom()
    {
        string newRoomId = Guid.NewGuid().ToString();
        ActiveRooms.Add(newRoomId);
        return Task.FromResult(newRoomId);
    }

    public async Task JoinRoom(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        await Clients.Group(roomId).SendAsync("UserJoined", Context.ConnectionId);
    }

    public async Task SendOffer(string roomId, string offer, string toConnectionId)
    {
        await Clients.Client(toConnectionId).SendAsync("ReceiveOffer", Context.ConnectionId, offer);
    }

    public async Task SendAnswer(string roomId, string answer, string toConnectionId)
    {
        await Clients.Client(toConnectionId).SendAsync("ReceiveAnswer", Context.ConnectionId, answer);
    }

    public async Task SendCandidate(string roomId, string candidate, string toConnectionId)
    {
        await Clients.Client(toConnectionId).SendAsync("ReceiveCandidate", Context.ConnectionId, candidate);
    }

    public async Task LeaveRoom(string roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
        await Clients.Group(roomId).SendAsync("UserLeft", Context.ConnectionId);
    }
}
