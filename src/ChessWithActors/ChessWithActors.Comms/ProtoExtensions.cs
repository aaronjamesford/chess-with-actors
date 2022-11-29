using Google.Protobuf.WellKnownTypes;
using Proto.Remote;
using Proto.Remote.GrpcNet;

namespace ChessWithActors.Comms;

public static class ProtoExtensions
{
    public static GrpcNetRemoteConfig WithChessMessages(this GrpcNetRemoteConfig config)
        => config.WithProtoMessages(EmptyReflection.Descriptor)
            .WithProtoMessages(MessagesReflection.Descriptor);
}