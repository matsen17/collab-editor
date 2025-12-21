namespace CollabEditor.Application.Interfaces;

public interface IMessage
{
    static abstract string RoutingKey { get; }
}