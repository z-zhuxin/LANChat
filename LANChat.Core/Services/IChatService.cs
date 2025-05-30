using LANChat.Core.Models;

namespace LANChat.Core.Services
{
    public interface IChatService
    {
        event EventHandler<Message> MessageReceived;
        event EventHandler<User> UserJoined;
        event EventHandler<User> UserLeft;

        Task StartAsync(string username, string ipAddress, int port);
        Task StopAsync();
        Task SendMessageAsync(string content);
        Task BroadcastPresenceAsync();
        IEnumerable<User> GetOnlineUsers();
    }
}