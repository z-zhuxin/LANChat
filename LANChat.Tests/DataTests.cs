using Xunit;
using LANChat.Data;
using LANChat.Core.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;

namespace LANChat.Tests
{
    public class DataTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ChatDbContext _context;

        public DataTests()
        {
            // 使用内存数据库进行测试
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<ChatDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new ChatDbContext(options);
            _context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        [Fact]
        public async Task SaveMessage_StoresMessageCorrectly()
        {
            // Arrange
            var message = new Message
            {
                Sender = "TestUser",
                Content = "Test Message",
                Timestamp = DateTime.UtcNow,
                MessageType = MessageType.UserMessage
            };

            // Act
            await _context.Messages.AddAsync(message);
            await _context.SaveChangesAsync();

            // Assert
            var savedMessage = await _context.Messages.FirstOrDefaultAsync(m =>
                m.Sender == "TestUser" && m.Content == "Test Message");

            Assert.NotNull(savedMessage);
            Assert.Equal(message.Content, savedMessage.Content);
            Assert.Equal(message.Sender, savedMessage.Sender);
            Assert.Equal(message.MessageType, savedMessage.MessageType);
        }

        [Fact]
        public async Task GetMessageHistory_ReturnsMessagesInOrder()
        {
            // Arrange
            var messages = new[]
            {
                new Message
                {
                    Sender = "User1",
                    Content = "Message 1",
                    Timestamp = DateTime.UtcNow.AddMinutes(-2),
                    MessageType = MessageType.UserMessage
                },
                new Message
                {
                    Sender = "User2",
                    Content = "Message 2",
                    Timestamp = DateTime.UtcNow.AddMinutes(-1),
                    MessageType = MessageType.UserMessage
                },
                new Message
                {
                    Sender = "User1",
                    Content = "Message 3",
                    Timestamp = DateTime.UtcNow,
                    MessageType = MessageType.UserMessage
                }
            };

            await _context.Messages.AddRangeAsync(messages);
            await _context.SaveChangesAsync();

            // Act
            var history = await _context.Messages
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            // Assert
            Assert.Equal(3, history.Count);
            Assert.Equal("Message 1", history[0].Content);
            Assert.Equal("Message 2", history[1].Content);
            Assert.Equal("Message 3", history[2].Content);
        }

        [Fact]
        public async Task DeleteMessage_RemovesMessageFromDatabase()
        {
            // Arrange
            var message = new Message
            {
                Sender = "TestUser",
                Content = "Delete Me",
                Timestamp = DateTime.UtcNow,
                MessageType = MessageType.UserMessage
            };

            await _context.Messages.AddAsync(message);
            await _context.SaveChangesAsync();

            // Act
            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            // Assert
            var deletedMessage = await _context.Messages
                .FirstOrDefaultAsync(m => m.Content == "Delete Me");
            Assert.Null(deletedMessage);
        }

        [Fact]
        public async Task UpdateMessage_ModifiesExistingMessage()
        {
            // Arrange
            var message = new Message
            {
                Sender = "TestUser",
                Content = "Original Content",
                Timestamp = DateTime.UtcNow,
                MessageType = MessageType.UserMessage
            };

            await _context.Messages.AddAsync(message);
            await _context.SaveChangesAsync();

            // Act
            message.Content = "Updated Content";
            await _context.SaveChangesAsync();

            // Assert
            var updatedMessage = await _context.Messages
                .FirstOrDefaultAsync(m => m.Sender == "TestUser");
            Assert.Equal("Updated Content", updatedMessage.Content);
        }
    }
}