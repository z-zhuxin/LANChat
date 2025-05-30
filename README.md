# LANChat

一个基于WPF和.NET Core开发的局域网聊天应用程序。

## 功能特点

- 局域网内自动发现其他用户
- 实时消息收发
- 用户上线/下线通知
- 简洁现代的用户界面

## 技术栈

- .NET 6.0
- WPF (Windows Presentation Foundation)
- MVVM 架构模式
- TCP/IP 和 UDP 网络通信

## 快速开始

1. 克隆仓库
2. 打开解决方案文件
3. 配置 appsettings.json
4. 运行项目

## 测试

项目包含单元测试和集成测试，使用xUnit测试框架。

运行测试:
```bash
dotnet test
```

## CI/CD

项目使用GitHub Actions进行持续集成和部署。每次推送到main分支时:

1. 自动构建解决方案
2. 运行所有测试
3. 发布可执行文件

## 贡献指南

1. Fork项目
2. 创建特性分支
3. 提交变更
4. 推送分支
5. 创建Pull Request

## 许可证

MIT License - 详见LICENSE文件