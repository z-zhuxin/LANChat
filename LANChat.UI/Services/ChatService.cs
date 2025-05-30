using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LANChat.Core.Models;
using LANChat.Core.Services;
using Microsoft.Extensions.Configuration;

namespace LANChat.UI.Services
{
    public class ChatService : IChatService
    {
        private readonly IConfiguration _configuration;
        private UdpClient? _discoveryClient;
        private TcpListener? _messageListener;
        private bool _isRunning;
        private string _userId = Guid.NewGuid().ToString();
        private string _username = string.Empty;
        private readonly Dictionary<string, User> _onlineUsers = new();

        public event EventHandler<Message>? MessageReceived;
        public event EventHandler<User>? UserJoined;
        public event EventHandler<User>? UserLeft;

        public ChatService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task StartAsync(string username, string ipAddress, int port)
        {
            if (_isRunning) return;

            _username = username;
            _isRunning = true;

            var udpPort = _configuration.GetValue<int>("Chat:Discovery:UdpPort");
            var broadcastAddress = _configuration.GetValue<string>("Chat:Discovery:BroadcastAddress");

            // 启动用户发现服务
            _discoveryClient = new UdpClient();
            _discoveryClient.EnableBroadcast = true;

            // 启动消息监听服务
            _messageListener = new TcpListener(IPAddress.Parse(ipAddress), port);
            _messageListener.Start();

            // 开始监听消息
            _ = Task.Run(ListenForMessagesAsync);

            // 开始监听用户发现广播
            _ = Task.Run(ListenForDiscoveryAsync);

            // 广播自己的存在
            await BroadcastPresenceAsync();
        }

        public async Task StopAsync()
        {
            if (!_isRunning) return;

            _isRunning = false;

            // 广播离开消息
            if (_discoveryClient != null)
            {
                var leaveMessage = new
                {
                    Type = "Leave",
                    UserId = _userId
                };

                var json = JsonSerializer.Serialize(leaveMessage);
                var bytes = Encoding.UTF8.GetBytes(json);
                var broadcastAddress = _configuration.GetValue<string>("Chat:Discovery:BroadcastAddress");
                var udpPort = _configuration.GetValue<int>("Chat:Discovery:UdpPort");

                await _discoveryClient.SendAsync(bytes, bytes.Length, broadcastAddress, udpPort);
            }

            _discoveryClient?.Close();
            _messageListener?.Stop();

            _discoveryClient = null;
            _messageListener = null;
        }

        public async Task SendMessageAsync(string content)
        {
            var message = new Message
            {
                SenderId = _userId,
                SenderName = _username,
                Content = content,
                Type = MessageType.Text
            };

            // 向所有在线用户发送消息
            foreach (var user in _onlineUsers.Values.Where(u => u.IsOnline))
            {
                try
                {
                    using var client = new TcpClient();
                    await client.ConnectAsync(user.IpAddress, user.Port);

                    var json = JsonSerializer.Serialize(message);
                    var bytes = Encoding.UTF8.GetBytes(json);

                    using var stream = client.GetStream();
                    await stream.WriteAsync(bytes);
                }
                catch (Exception)
                {
                    // 发送失败，可能用户已离线
                    user.IsOnline = false;
                }
            }

            // 触发本地消息接收事件
            MessageReceived?.Invoke(this, message);
        }

        public async Task BroadcastPresenceAsync()
        {
            if (_discoveryClient == null || !_isRunning) return;

            var presenceMessage = new
            {
                Type = "Join",
                UserId = _userId,
                Username = _username,
                IpAddress = _configuration.GetValue<string>("Chat:User:DefaultIpAddress"),
                Port = _configuration.GetValue<int>("Chat:User:DefaultPort")
            };

            var json = JsonSerializer.Serialize(presenceMessage);
            var bytes = Encoding.UTF8.GetBytes(json);
            var broadcastAddress = _configuration.GetValue<string>("Chat:Discovery:BroadcastAddress");
            var udpPort = _configuration.GetValue<int>("Chat:Discovery:UdpPort");

            await _discoveryClient.SendAsync(bytes, bytes.Length, broadcastAddress, udpPort);
        }

        public IEnumerable<User> GetOnlineUsers()
        {
            return _onlineUsers.Values.Where(u => u.IsOnline).ToList();
        }

        private async Task ListenForMessagesAsync()
        {
            while (_isRunning && _messageListener != null)
            {
                try
                {
                    var client = await _messageListener.AcceptTcpClientAsync();
                    _ = HandleMessageClientAsync(client);
                }
                catch (Exception) when (!_isRunning)
                {
                    // 正常停止，忽略异常
                }
                catch (Exception)
                {
                    // 处理其他异常
                }
            }
        }

        private async Task HandleMessageClientAsync(TcpClient client)
        {
            try
            {
                using var stream = client.GetStream();
                var buffer = new byte[4096];
                var count = await stream.ReadAsync(buffer);
                var json = Encoding.UTF8.GetString(buffer, 0, count);
                var message = JsonSerializer.Deserialize<Message>(json);

                if (message != null)
                {
                    MessageReceived?.Invoke(this, message);
                }
            }
            finally
            {
                client.Dispose();
            }
        }

        private async Task ListenForDiscoveryAsync()
        {
            if (_discoveryClient == null) return;

            var udpPort = _configuration.GetValue<int>("Chat:Discovery:UdpPort");
            var endpoint = new IPEndPoint(IPAddress.Any, udpPort);

            while (_isRunning)
            {
                try
                {
                    var result = await _discoveryClient.ReceiveAsync();
                    var json = Encoding.UTF8.GetString(result.Buffer);
                    var message = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                    if (message != null && message.TryGetValue("Type", out var type))
                    {
                        switch (type)
                        {
                            case "Join":
                                HandleUserJoin(message);
                                break;
                            case "Leave":
                                HandleUserLeave(message);
                                break;
                        }
                    }
                }
                catch (Exception) when (!_isRunning)
                {
                    // 正常停止，忽略异常
                }
                catch (Exception)
                {
                    // 处理其他异常
                }
            }
        }

        private void HandleUserJoin(Dictionary<string, string> message)
        {
            if (!message.TryGetValue("UserId", out var userId) ||
                !message.TryGetValue("Username", out var username) ||
                !message.TryGetValue("IpAddress", out var ipAddress) ||
                !message.TryGetValue("Port", out var portStr) ||
                !int.TryParse(portStr, out var port))
            {
                return;
            }

            var user = new User
            {
                Id = userId,
                Name = username,
                IpAddress = ipAddress,
                Port = port,
                IsOnline = true,
                LastSeen = DateTime.Now
            };

            _onlineUsers[userId] = user;
            UserJoined?.Invoke(this, user);
        }

        private void HandleUserLeave(Dictionary<string, string> message)
        {
            if (!message.TryGetValue("UserId", out var userId) ||
                !_onlineUsers.TryGetValue(userId, out var user))
            {
                return;
            }

            user.IsOnline = false;
            UserLeft?.Invoke(this, user);
        }
    }
}