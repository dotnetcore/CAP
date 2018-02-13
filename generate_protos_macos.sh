PROTOC=~/.nuget/packages/google.protobuf.tools/3.5.1/tools/macosx_x64/protoc
PLUGIN=~/.nuget/packages/grpc.tools/1.9.0/tools/macosx_x64/grpc_csharp_plugin

dotnet restore

$PROTOC -I protos --csharp_out src/SkyWalking.NetworkProtocol  protos/ApplicationRegisterService.proto --grpc_out src/SkyWalking.NetworkProtocol --plugin=protoc-gen-grpc=$PLUGIN
$PROTOC -I protos --csharp_out src/SkyWalking.NetworkProtocol  protos/Common.proto --grpc_out src/SkyWalking.NetworkProtocol --plugin=protoc-gen-grpc=$PLUGIN
$PROTOC -I protos --csharp_out src/SkyWalking.NetworkProtocol  protos/DiscoveryService.proto --grpc_out src/SkyWalking.NetworkProtocol --plugin=protoc-gen-grpc=$PLUGIN
$PROTOC -I protos --csharp_out src/SkyWalking.NetworkProtocol  protos/Downstream.proto --grpc_out src/SkyWalking.NetworkProtocol --plugin=protoc-gen-grpc=$PLUGIN
$PROTOC -I protos --csharp_out src/SkyWalking.NetworkProtocol  protos/JVMMetricsService.proto --grpc_out src/SkyWalking.NetworkProtocol --plugin=protoc-gen-grpc=$PLUGIN
$PROTOC -I protos --csharp_out src/SkyWalking.NetworkProtocol  protos/KeyWithIntegerValue.proto --grpc_out src/SkyWalking.NetworkProtocol --plugin=protoc-gen-grpc=$PLUGIN
$PROTOC -I protos --csharp_out src/SkyWalking.NetworkProtocol  protos/KeyWithStringValue.proto --grpc_out src/SkyWalking.NetworkProtocol --plugin=protoc-gen-grpc=$PLUGIN
$PROTOC -I protos --csharp_out src/SkyWalking.NetworkProtocol  protos/NetworkAddressRegisterService.proto --grpc_out src/SkyWalking.NetworkProtocol --plugin=protoc-gen-grpc=$PLUGIN
$PROTOC -I protos --csharp_out src/SkyWalking.NetworkProtocol  protos/TraceSegmentService.proto --grpc_out src/SkyWalking.NetworkProtocol --plugin=protoc-gen-grpc=$PLUGIN
