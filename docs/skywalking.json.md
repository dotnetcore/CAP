# SkyWalking Configuration

# ApplicationCode

App name displayed.

# SpanLimitPerSegment

"Span" Limit Per Segment Max.

## Sampling 

Sample Configuration Section

1. SamplePer3Secs, Sample Per 3 Seconds

## Logging

SkyWalking Logging Configuration Section

1. Level, defalut:Information
2. FilePath, defalut:logs\\SkyWalking-{Date}.log

## Transport Section

Transport Configuration Section

1. Interval, Flush Interval Millisecond,(unit:Millisecond)
2. PendingSegmentLimit,  PendingSegmentLimit Count
3. PendingSegmentTimeout, Data queued beyond this time will be discarded,(unit:Millisecond)

### gRPC 

gRPC Configuration Section

1. Servers, gRPC Service address,Multiple addresses separated by commas (",")
2. Timeout, Timeout for creating a link,(unit:Millisecond)
3. ConnectTimeout, gRPC Connectioning timed out,(unit:Millisecond)

# skywalking.json sample
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
