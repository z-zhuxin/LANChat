using Xunit;
using LANChat.Core.Models;
using System;

namespace LANChat.Tests
{
    public class MessageTests
    {
        [Fact]
        public void Message_Creation_SetsPropertiesCorrectly()
        {
            // Arrange
            var sender = "TestUser";
            var content = "Hello, World!";
            var timestamp = DateTime.Now;

            // Act
            var message = new Message
            {
                Sender = sender,
                Content = content,
                Timestamp = timestamp,
                MessageType = MessageType.UserMessage
            };

            // Assert
            Assert.Equal(sender, message.Sender);
            Assert.Equal(content, message.Content);
            Assert.Equal(timestamp, message.Timestamp);
            Assert.Equal(MessageType.UserMessage, message.MessageType);
        }

        [Theory]
        [InlineData("", "Content", "Sender cannot be empty")]
        [InlineData("Sender", "", "Content cannot be empty")]
        public void Message_Validation_ThrowsException_ForInvalidData(string sender, string content, string expectedError)
        {
            // Arrange & Act
            var exception = Record.Exception(() => new Message
            {
                Sender = sender,
                Content = content,
                Timestamp = DateTime.Now,
                MessageType = MessageType.UserMessage
            }.Validate());

            // Assert
            Assert.NotNull(exception);
            Assert.Contains(expectedError, exception.Message);
        }

        [Fact]
        public void SystemMessage_Creation_SetsCorrectMessageType()
        {
            // Arrange & Act
            var message = Message.CreateSystemMessage("User joined the chat");

            // Assert
            Assert.Equal(MessageType.SystemMessage, message.MessageType);
            Assert.Equal("System", message.Sender);
            Assert.NotEqual(default(DateTime), message.Timestamp);
        }
    }
}