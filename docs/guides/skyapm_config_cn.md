# SkyAPM Config 配置说明

## ServiceName

服务名称

## Sampling 

采样配置节点

1. SamplePer3Secs 每3秒采样数

2. Percentage 采样百分比，例如10%采样则配置为`10`

## Logging

日志配置节点

1. Level  日志级别
2. FilePath 日志保存路径

## Transport

传输配置节点

1. Interval 每多少毫秒刷新

### gRPC

gRPC配置节点

1. Servers gRPC地址，多个用逗号","
2. Timeout 创建gRPC链接的超时时间，毫秒
3. ConnectTimeout gRPC最长链接时间，毫秒

# skyapm.json 示例
```
{
  "SkyWalking": {
    "ServiceName": "your_service_name",
    "Namespace": "",
    "HeaderVersions": [
      "sw6"
    ],
    "Sampling": {
      "SamplePer3Secs": -1,
      "Percentage": -1.0
    },
    "Logging": {
      "Level": "Information",
      "FilePath": "logs/skyapm-{Date}.log"
    },
    "Transport": {
      "Interval": 3000,
      "ProtocolVersion": "v6",
      "QueueSize": 30000,
      "BatchSize": 3000,
      "gRPC": {
        "Servers": "localhost:11800",
        "Timeout": 10000,
        "ConnectTimeout": 10000,
        "ReportTimeout": 600000
      }
    }
  }
}
```
