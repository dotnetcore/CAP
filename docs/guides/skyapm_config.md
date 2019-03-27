# SkyAPM Config 

## ServiceName

Service name displayed.

## Sampling 

Sample Configuration Section

1. SamplePer3Secs, Sample Per 3 Seconds

## Logging

SkyAPM Logging Configuration Section

1. Level, defalut:Information
2. FilePath, defalut:logs\\SkyWalking-{Date}.log

## Transport Section

Transport Configuration Section

1. Interval, Flush Interval Millisecond,(unit:Millisecond)

### gRPC 

gRPC Configuration Section

1. Servers, gRPC Service address,Multiple addresses separated by commas (",")
2. Timeout, Timeout for creating a link,(unit:Millisecond)
3. ConnectTimeout, gRPC Connectioning timed out,(unit:Millisecond)

# skyapm.json sample
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
