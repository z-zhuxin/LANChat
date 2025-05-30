namespace LANChat.Core.Models
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool IsOnline { get; set; }
        public DateTime LastSeen { get; set; } = DateTime.Now;
    }
}