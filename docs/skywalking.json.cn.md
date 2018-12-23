# SkyWalking Config 配置说明

# ApplicationCode

应用名称

# SpanLimitPerSegment

每段限制

## Sampling 

采样配置节点

1. SamplePer3Secs 每3秒采样数

## Logging

SkyWalking日志配置节点

1. Level  日志级别
2. FilePath 日志保存路径

## Transport

传输配置节点

1. Interval 每多少毫秒刷新
2. PendingSegmentLimit  排队限制
3. PendingSegmentTimeout 排队超时毫秒，超时时排队的数据会被丢弃

### gRPC

gRPC配置节点

1. Servers gRPC地址，多个用逗号","
2. Timeout 创建gRPC链接的超时时间，毫秒
3. ConnectTimeout gRPC最长链接时间，毫秒

# skywalking.json 示例
```
{
  "SkyWalking": {
    "ApplicationCode": "app",
    "SpanLimitPerSegment": 300,
    "Sampling": {
      "SamplePer3Secs": -1
    },
    "Logging": {
      "Level": "Information",
      "FilePath": "logs\\SkyWalking-{Date}.log"
    },
    "Transport": {
      "Interval": 3000,
      "PendingSegmentLimit": 30000,
      "PendingSegmentTimeout": 1000,
      "gRPC": {
        "Servers": "localhost:11800",
        "Timeout": 2000,
        "ConnectTimeout": 10000
      }
    }
  }
}
```
