using Xunit;
using LANChat.UI.ViewModels;
using LANChat.Core.Services;
using LANChat.Core.Models;
using Moq;
using System.Collections.ObjectModel;

namespace LANChat.Tests
{
    public class ViewModelTests
    {
        private readonly Mock<IChatService> _mockChatService;
        private readonly Mock<IDiscoveryService> _mockDiscoveryService;
        private readonly Mock<ILogger> _mockLogger;
        private readonly MainViewModel _viewModel;

        public ViewModelTests()
        {
            _mockChatService = new Mock<IChatService>();
            _mockDiscoveryService = new Mock<IDiscoveryService>();
            _mockLogger = new Mock<ILogger>();

            _viewModel = new MainViewModel(
                _mockChatService.Object,
                _mockDiscoveryService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public void SendMessage_Command_AddsMessageToList()
        {
            // Arrange
            var testMessage = "Test Message";
            _viewModel.MessageText = testMessage;

            // Act
            _viewModel.SendMessageCommand.Execute(null);

            // Assert
            Assert.Contains(_viewModel.Messages, m => m.Content == testMessage);
            Assert.Empty(_viewModel.MessageText); // Should clear input
            _mockChatService.Verify(s => s.SendMessageAsync(
                It.Is<Message>(m => m.Content == testMessage),
                It.IsAny<string>(),
                It.IsAny<int>()
            ), Times.Once);
        }

        [Fact]
        public void Connect_Command_UpdatesConnectionState()
        {
            // Arrange
            _viewModel.IpAddress = "127.0.0.1";
            _viewModel.Port = 5000;
            _mockChatService.Setup(s => s.ValidateConnectionParameters(
                It.IsAny<string>(), It.IsAny<int>())
            ).Returns(true);

            // Act
            _viewModel.ConnectCommand.Execute(null);

            // Assert
            Assert.True(_viewModel.IsConnected);
            _mockDiscoveryService.Verify(s => s.StartDiscoveryAsync(
                It.IsAny<int>(),
                It.IsAny<Action<string>>()
            ), Times.Once);
        }

        [Fact]
        public void UserList_Updates_WhenUserDiscovered()
        {
            // Arrange
            var testUser = "TestUser@127.0.0.1:5000";

            // Act
            _viewModel.OnUserDiscovered(testUser);

            // Assert
            Assert.Contains(_viewModel.Users, u => u == testUser);
        }

        [Fact]
        public void MessageValidation_PreventsSending_EmptyMessage()
        {
            // Arrange
            _viewModel.MessageText = "";

            // Act & Assert
            Assert.False(_viewModel.SendMessageCommand.CanExecute(null));
        }

        [Fact]
        public void ConnectionValidation_PreventsBadParameters()
        {
            // Arrange
            _viewModel.IpAddress = "invalid-ip";
            _viewModel.Port = -1;
            _mockChatService.Setup(s => s.ValidateConnectionParameters(
                It.IsAny<string>(), It.IsAny<int>())
            ).Returns(false);

            // Act & Assert
            Assert.False(_viewModel.ConnectCommand.CanExecute(null));
        }

        [Fact]
        public void Disconnect_Command_ClearsState()
        {
            // Arrange
            _viewModel.IsConnected = true;
            _viewModel.Users.Add("TestUser");

            // Act
            _viewModel.DisconnectCommand.Execute(null);

            // Assert
            Assert.False(_viewModel.IsConnected);
            Assert.Empty(_viewModel.Users);
            _mockChatService.Verify(s => s.DisconnectAsync(), Times.Once);
        }
    }
}