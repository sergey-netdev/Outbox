namespace Outbox.Publisher.RabbitMQ.Tests;

using Microsoft.Extensions.Logging;
using Moq;
using System.Threading;

public class TimeConstrainedFactoryTests
{
    private const int TimeoutInMilliseconds = 350;
    private readonly TimeSpan _timeout = TimeSpan.FromMilliseconds(TimeoutInMilliseconds);
    private readonly TimeSpan _disposeVerifyTime = TimeSpan.FromMilliseconds(TimeoutInMilliseconds * 1.5); // to compensate Task/ThreadPool/Debugger/xUnit overhead; cannot be predicted, depends on Debug/Release, CPU load, etc.

    private readonly Mock<ILogger<TimeConstrainedFactory<IConnection>>> _mockLogger = new();
    private readonly Mock<IConnection> _mockConnection = new();
    private readonly Mock<IConnectionFactory> _mockConnectionFactory = new();
    private readonly TimeConstrainedFactory<IConnection> _factory;

    public TimeConstrainedFactoryTests()
    {
        _factory = new(_mockConnectionFactory.Object.CreateConnection, _mockLogger.Object);
    }

    [Theory]
    [InlineData(100)]
    public async Task CreateConnectionAsync_Throws_TimeoutException_When_SpecifiedTimeout_Is_Exceeded(int delayInMilliseconds)
    {
        // setup
        TimeSpan createTime = _timeout.Add(TimeSpan.FromMilliseconds(delayInMilliseconds));
        Setup(createTime);

        // act
        TimeoutException ex = await Assert.ThrowsAsync<TimeoutException>(
            () => _factory.CreateAsync(_timeout));

        // verify
        _mockConnectionFactory.Verify(p => p.CreateConnection(), Times.Once);

        string expectedMessage = string.Format(TimeoutException.IntervalExceededMessage, _timeout);
        Assert.Equal(expectedMessage, ex.Message);

        await Task.Delay(_disposeVerifyTime);
        _mockConnection.Verify(p => p.Dispose(), Times.Once);
    }

    [Theory]
    [InlineData(100)]
    public async Task CreateConnectionAsync_Returns_Connection_When_SpecifiedTimeout_Is_Not_Exceeded(int delayInMilliseconds)
    {
        // setup
        TimeSpan createTime = _timeout.Add(TimeSpan.FromMilliseconds(-delayInMilliseconds));
        Setup(createTime);

        // act
        IConnection connection = await _factory.CreateAsync(_timeout);

        // verify
        _mockConnectionFactory.Verify(p => p.CreateConnection(), Times.Once);
        Assert.Equal(_mockConnection.Object, connection);

        await Task.Delay(_disposeVerifyTime);
        _mockConnection.Verify(p => p.Dispose(), Times.Never); // nothing to dispose
    }

    [Fact]
    public async Task CreateConnectionAsync_Throws_Underneath_Exception()
    {
        // setup
        InvalidOperationException expectedException = new("Client exception.");
        Setup(TimeSpan.Zero, expectedException);

        // act
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _factory.CreateAsync(_timeout));

        // verify
        _mockConnectionFactory.Verify(p => p.CreateConnection(), Times.Once);
        Assert.Equal(expectedException, ex);

        await Task.Delay(_disposeVerifyTime);
        _mockConnection.Verify(p => p.Dispose(), Times.Never); // nothing to dispose
    }

    private void Setup(TimeSpan createTime, Exception? ex = null)
    {
        _mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(() =>
        {
            if (ex is not null)
            {
                throw ex;
            }

            Thread.Sleep(createTime);
            return _mockConnection.Object;
        });

        _mockConnection.Setup(x => x.Dispose()).Callback(() => { });
    }
}
