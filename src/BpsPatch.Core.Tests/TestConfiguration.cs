// ========================================================================================================
// Test Configuration Constants
// ========================================================================================================
// Defines timeout constants and utilities for test execution.
// Note: xUnit requires async tests for per-test Timeout support.
// Use the runner timeout configuration or CancellationToken patterns for long-running tests.
// ========================================================================================================

namespace BpsPatch.Core.Tests;

/// <summary>
/// Global test configuration and constants.
/// </summary>
public static class TestConfiguration
{
    /// <summary>
    /// Default timeout in milliseconds for unit tests (5 seconds).
    /// </summary>
    public const int UnitTestTimeout = 5_000;

    /// <summary>
    /// Timeout in milliseconds for integration tests (30 seconds).
    /// </summary>
    public const int IntegrationTestTimeout = 30_000;

    /// <summary>
    /// Timeout in milliseconds for performance/stress tests (60 seconds).
    /// </summary>
    public const int PerformanceTestTimeout = 60_000;

    /// <summary>
    /// Maximum file size for test data in bytes (1 MB).
    /// </summary>
    public const int MaxTestFileSize = 1024 * 1024;

    /// <summary>
    /// Creates a CancellationToken that cancels after the specified timeout.
    /// Use this for long-running operations that should respect timeouts.
    /// </summary>
    public static CancellationToken CreateTimeout(int timeoutMs = UnitTestTimeout)
        => new CancellationTokenSource(timeoutMs).Token;
}
