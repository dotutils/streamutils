using DotUtils.StreamUtils;
using FluentAssertions;
using Moq;
using System;
using Xunit;

namespace DotUtils.StreamUtils.UnitTests;

/// <summary>
/// Unit tests for the <see cref="CleanupScope"/> struct.
/// </summary>
public class CleanupScopeTests
{
    /// <summary>
    /// Tests that the Dispose method invokes the dispose action exactly one time.
    /// </summary>
    [Fact]
    public void Dispose_WhenCalledOnce_InvokesDisposeAction()
    {
        // Arrange
        bool isDisposed = false;
        Action disposeAction = () => isDisposed = true;
        var cleanupScope = new CleanupScope(disposeAction);

        // Act
        cleanupScope.Dispose();

        // Assert
        isDisposed.Should().BeTrue("because the dispose action should have been invoked exactly once upon calling Dispose.");
    }

    /// <summary>
    /// Tests that calling Dispose multiple times invokes the dispose action each time.
    /// </summary>
    [Fact]
    public void Dispose_WhenCalledMultipleTimes_InvokesDisposeActionEachTime()
    {
        // Arrange
        int invocationCount = 0;
        Action disposeAction = () => invocationCount++;
        var cleanupScope = new CleanupScope(disposeAction);

        // Act
        cleanupScope.Dispose();
        cleanupScope.Dispose();

        // Assert
        invocationCount.Should().Be(2, "because the dispose action should be called each time Dispose is invoked.");
    }

    /// <summary>
    /// Tests that if the dispose action throws an exception, 
    /// the Dispose method propagates the exception.
    /// </summary>
    [Fact]
    public void Dispose_WhenDisposeActionThrows_PropagatesException()
    {
        // Arrange
        string exceptionMessage = "Dispose error";
        Action disposeAction = () => throw new InvalidOperationException(exceptionMessage);
        var cleanupScope = new CleanupScope(disposeAction);

        // Act
        Action act = () => cleanupScope.Dispose();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage(exceptionMessage);
    }
}
