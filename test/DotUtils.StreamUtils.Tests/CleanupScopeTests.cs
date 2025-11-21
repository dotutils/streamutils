using DotUtils.StreamUtils;
using Xunit;

namespace DotUtils.StreamUtils.UnitTests
{
    /// <summary>
    /// Unit tests for the <see cref="CleanupScope"/> struct.
    /// </summary>
    public class CleanupScopeTests
    {
        /// <summary>
        /// Tests that the Dispose method calls the provided action exactly once.
        /// Arrange: Initialize a counter and a delegate that increments the counter.
        /// Act: Invoke Dispose on a CleanupScope instance created with the delegate.
        /// Assert: Verify that the counter is incremented exactly once.
        /// </summary>
        [Fact]
        public void Dispose_WhenCalled_InvokesActionOnce()
        {
            // Arrange
            int callCount = 0;
            Action action = () => callCount++;
            var cleanupScope = new CleanupScope(action);

            // Act
            cleanupScope.Dispose();

            // Assert
            Assert.Equal(1, callCount);
        }
        
        /// <summary>
        /// Tests that the Dispose method propagates exceptions thrown by the provided action.
        /// Arrange: Create a delegate that throws an InvalidOperationException.
        /// Act & Assert: Ensure that calling Dispose results in the same exception.
        /// </summary>
        [Fact]
        public void Dispose_WhenActionThrowsException_PropagatesException()
        {
            // Arrange
            Action action = () => throw new InvalidOperationException("Test exception");
            var cleanupScope = new CleanupScope(action);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => cleanupScope.Dispose());
            Assert.Equal("Test exception", exception.Message);
        }
        
        /// <summary>
        /// Tests that creating a CleanupScope with a null action results in a NullReferenceException upon disposal.
        /// Arrange: Create a CleanupScope instance with a null action.
        /// Act & Assert: Expect a NullReferenceException when Dispose is called.
        /// </summary>
        [Fact]
        public void Dispose_WhenCreatedWithNullAction_ThrowsNullReferenceException()
        {
            // Arrange
            var cleanupScope = new CleanupScope(null);

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => cleanupScope.Dispose());
        }
        
        /// <summary>
        /// Tests that calling Dispose twice results in the action being invoked twice.
        /// Arrange: Initialize a counter and a delegate that increments the counter.
        /// Act: Call Dispose twice.
        /// Assert: Verify that the counter equals two.
        /// </summary>
        [Fact]
        public void Dispose_CalledTwice_InvokesActionTwice()
        {
            // Arrange
            int callCount = 0;
            Action action = () => callCount++;
            var cleanupScope = new CleanupScope(action);

            // Act
            cleanupScope.Dispose();
            cleanupScope.Dispose();

            // Assert
            Assert.Equal(2, callCount);
        }
    }
}
