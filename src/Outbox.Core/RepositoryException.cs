namespace Outbox.Core;

/// <summary>
/// A base class for any exceptions thrown by repository implementations.
/// Gateways/repositories must hide any underlaying exceptions like <see cref="System.IO.FileNotFoundException"/>
/// or <see cref="System.Net.Sockets.SocketException"/> and throw this instead.
/// </summary>
[Serializable]
public class RepositoryException : Exception
{
    public RepositoryException()
    {
    }

    public RepositoryException(string message)
        : base(message)
    {
    }

    public RepositoryException(string message, Exception inner)
        : base(message, inner)
    {
    }
}