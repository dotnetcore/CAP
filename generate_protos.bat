@rem Generate the C# code for .proto files

@rem enter this directory
cd /d %~dp0

set PROTOC=%UserProfile%\.nuget\packages\google.protobuf.tools\3.5.1\tools\windows_x64\protoc.exe
set PLUGIN=%UserProfile%\.nuget\packages\grpc.tools\1.9.0\tools\windows_x64\grpc_csharp_plugin.exe

dotnet restore

%PROTOC% -I protos --csharp_out src/SkyWalking.NetworkProtocol  protos/ApplicationRegisterService.proto --grpc_out src/SkyWalking.NetworkProtocol --plugin=protoc-gen-grpc=%PLUGIN%
%PROTOC% -I protos --csharp_out src/SkyWalking.NetworkProtocol  protos/Common.proto --grpc_out src/SkyWalking.NetworkProtocol --plugin=protoc-gen-grpc=%PLUGIN%
%PROTOC% -I protos --csharp_out src/SkyWalking.NetworkProtocol  protos/DiscoveryService.proto --grpc_out src/SkyWalking.NetworkProtocol --plugin=protoc-gen-grpc=%PLUGIN%
%PROTOC% -I protos --csharp_out src/SkyWalking.NetworkProtocol  protos/Downstream.proto --grpc_out src/SkyWalking.NetworkProtocol --plugin=protoc-gen-grpc=%PLUGIN%
%PROTOC% -I protos --csharp_out src/SkyWalking.NetworkProtocol  protos/JVMMetricsService.proto --grpc_out src/SkyWalking.NetworkProtocol --plugin=protoc-gen-grpc=%PLUGIN%
%PROTOC% -I protos --csharp_out src/SkyWalking.NetworkProtocol  protos/KeyWithIntegerValue.proto --grpc_out src/SkyWalking.NetworkProtocol --plugin=protoc-gen-grpc=%PLUGIN%
%PROTOC% -I protos --csharp_out src/SkyWalking.NetworkProtocol  protos/KeyWithStringValue.proto --grpc_out src/SkyWalking.NetworkProtocol --plugin=protoc-gen-grpc=%PLUGIN%
%PROTOC% -I protos --csharp_out src/SkyWalking.NetworkProtocol  protos/NetworkAddressRegisterService.proto --grpc_out src/SkyWalking.NetworkProtocol --plugin=protoc-gen-grpc=%PLUGIN%
%PROTOC% -I protos --csharp_out src/SkyWalking.NetworkProtocol  protos/TraceSegmentService.proto --grpc_out src/SkyWalking.NetworkProtocol --plugin=protoc-gen-grpc=%PLUGIN%