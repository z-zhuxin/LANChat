using Xunit;
using LANChat.Core.Services;
using LANChat.Core.Models;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Moq;

namespace LANChat.Tests
{
    public class NetworkTests : IDisposable
    {
        private readonly UdpClient _discoveryClient;
        private readonly int _testPort = 5555;
        private readonly string _testMessage = "TestMessage";

        public NetworkTests()
        {
            _discoveryClient = new UdpClient(_testPort);
        }

        public void Dispose()
        {
            _discoveryClient?.Dispose();
        }

        [Fact]
        public async Task UdpBroadcast_SendsMessageSuccessfully()
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();
            var discoveryService = new DiscoveryService(mockLogger.Object);
            var receivedData = new TaskCompletionSource<string>();

            // Start listening for broadcast
            _ = Task.Run(async () =>
            {
                var result = await _discoveryClient.ReceiveAsync();
                var message = Encoding.UTF8.GetString(result.Buffer);
                receivedData.SetResult(message);
            });

            // Act
            await discoveryService.BroadcastPresenceAsync(_testPort);

            // Assert
            var received = await receivedData.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Contains("LANCHAT_PRESENCE", received);
        }

        [Fact]
        public async Task TcpConnection_TransfersDataSuccessfully()
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();
            var server = new TcpListener(IPAddress.Loopback, _testPort);
            server.Start();

            var chatService = new ChatService(mockLogger.Object);
            var receivedData = new TaskCompletionSource<string>();

            // Start server
            _ = Task.Run(async () =>
            {
                using var client = await server.AcceptTcpClientAsync();
                using var stream = client.GetStream();
                var buffer = new byte[1024];
                var bytesRead = await stream.ReadAsync(buffer);
                var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                receivedData.SetResult(message);
            });

            // Act
            await chatService.SendMessageAsync(
                new Message
                {
                    Content = _testMessage,
                    Sender = "TestUser",
                    MessageType = MessageType.UserMessage
                },
                IPAddress.Loopback.ToString(),
                _testPort
            );

            // Assert
            var received = await receivedData.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Contains(_testMessage, received);

            // Cleanup
            server.Stop();
        }

        [Theory]
        [InlineData("127.0.0.1", 1234, true)]
        [InlineData("invalid-ip", 1234, false)]
        [InlineData("127.0.0.1", -1, false)]
        public void ValidateConnectionParameters_ReturnsExpectedResult(
            string ip, int port, bool expectedResult)
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();
            var chatService = new ChatService(mockLogger.Object);

            // Act
            var result = chatService.ValidateConnectionParameters(ip, port);

            // Assert
            Assert.Equal(expectedResult, result);
        }
    }
}