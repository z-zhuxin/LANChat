using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using LANChat.Core.Models;
using LANChat.Core.Services;
using LANChat.UI.Commands;
using Microsoft.Extensions.Configuration;

namespace LANChat.UI.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IChatService _chatService;
        private readonly IConfiguration _configuration;
        private bool _isConnected;
        private string _username = string.Empty;
        private string _messageText = string.Empty;

        public MainViewModel(IChatService chatService, IConfiguration configuration)
        {
            _chatService = chatService;
            _configuration = configuration;

            Messages = new ObservableCollection<Message>();
            OnlineUsers = new ObservableCollection<User>();

            ToggleConnectionCommand = new RelayCommand(ToggleConnectionAsync, () => !string.IsNullOrWhiteSpace(Username));
            SendMessageCommand = new RelayCommand(SendMessageAsync, () => IsConnected && !string.IsNullOrWhiteSpace(MessageText));

            _chatService.MessageReceived += OnMessageReceived;
            _chatService.UserJoined += OnUserJoined;
            _chatService.UserLeft += OnUserLeft;
        }

        public ObservableCollection<Message> Messages { get; }
        public ObservableCollection<User> OnlineUsers { get; }

        public ICommand ToggleConnectionCommand { get; }
        public ICommand SendMessageCommand { get; }

        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (SetProperty(ref _isConnected, value))
                {
                    OnPropertyChanged(nameof(IsNotConnected));
                    OnPropertyChanged(nameof(ConnectionButtonText));
                    ((RelayCommand)SendMessageCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsNotConnected => !IsConnected;

        public string ConnectionButtonText => IsConnected ? "断开连接" : "连接";

        public string Username
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                {
                    ((RelayCommand)ToggleConnectionCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public string MessageText
        {
            get => _messageText;
            set
            {
                if (SetProperty(ref _messageText, value))
                {
                    ((RelayCommand)SendMessageCommand).RaiseCanExecuteChanged();
                }
            }
        }

        private async void ToggleConnectionAsync()
        {
            if (IsConnected)
            {
                await _chatService.StopAsync();
                IsConnected = false;
                OnlineUsers.Clear();
                Messages.Clear();
            }
            else
            {
                var ipAddress = _configuration.GetValue<string>("Chat:User:DefaultIpAddress");
                var port = _configuration.GetValue<int>("Chat:User:DefaultPort");

                try
                {
                    await _chatService.StartAsync(Username, ipAddress, port);
                    IsConnected = true;

                    // 添加系统消息
                    Messages.Add(new Message
                    {
                        Content = "已连接到聊天室",
                        Type = MessageType.System
                    });
                }
                catch (Exception ex)
                {
                    Messages.Add(new Message
                    {
                        Content = $"连接失败: {ex.Message}",
                        Type = MessageType.System
                    });
                }
            }
        }

        private async void SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(MessageText)) return;

            try
            {
                await _chatService.SendMessageAsync(MessageText);
                MessageText = string.Empty;
            }
            catch (Exception ex)
            {
                Messages.Add(new Message
                {
                    Content = $"发送失败: {ex.Message}",
                    Type = MessageType.System
                });
            }
        }

        private void OnMessageReceived(object? sender, Message message)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                Messages.Add(message);
            });
        }

        private void OnUserJoined(object? sender, User user)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                if (!OnlineUsers.Any(u => u.Id == user.Id))
                {
                    OnlineUsers.Add(user);
                    Messages.Add(new Message
                    {
                        Content = $"{user.Name} 加入了聊天室",
                        Type = MessageType.UserJoined
                    });
                }
            });
        }

        private void OnUserLeft(object? sender, User user)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                var existingUser = OnlineUsers.FirstOrDefault(u => u.Id == user.Id);
                if (existingUser != null)
                {
                    OnlineUsers.Remove(existingUser);
                    Messages.Add(new Message
                    {
                        Content = $"{user.Name} 离开了聊天室",
                        Type = MessageType.UserLeft
                    });
                }
            });
        }
    }
}