//using Microsoft.AspNetCore.SignalR;
//using System.Threading.Tasks;

//namespace Mind_Mend.Hubs
//{
//    public class ChatHub : Hub
//    {
//        public async Task SendMessage(string senderId, string receiverId, string message)
//        {
//            // Convert string IDs to int if needed (assuming validation elsewhere)
//            if (!int.TryParse(senderId, out _) || !int.TryParse(receiverId, out _))
//                throw new ArgumentException("Invalid user ID format.");

//            // Send message to receiver
//            await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, message);
//        }

//        public override Task OnConnectedAsync()
//        {
//            string userId = Context.UserIdentifier;
//            return base.OnConnectedAsync();
//        }
//    }
//}
///////////////////////////////////////////////////////
//  using Microsoft.AspNetCore.SignalR;
//  using System.Threading.Tasks;

// namespace Mind_Mend.Hubs
// {
//     public class ChatHub : Hub
//     {
       
//         public async Task SendMessage(int senderId, int receiverId, string message)
//         {
            
//             // Send message to receiver
//             await Clients.User(receiverId.ToString()).SendAsync("ReceiveMessage", senderId.ToString(), message);
            
//         }

//         public override Task OnConnectedAsync()
//         {
//             string userId = Context.UserIdentifier;
//             return base.OnConnectedAsync();
//         }
//     }
// }

 using Microsoft.AspNetCore.SignalR;
 using System.Threading.Tasks;

namespace Mind_Mend.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(string senderId, string receiverId, string message)
        {
            await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, message);
        }

        public override Task OnConnectedAsync()
        {
            string userId = Context.UserIdentifier;
            return base.OnConnectedAsync();
        }
    }
}