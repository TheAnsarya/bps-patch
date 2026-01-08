namespace bps_patch.Tests;

/// <summary>
/// Test timeout configuration constants and helpers.
/// </summary>
public static class TestConfiguration {
	/// <summary>
	/// Default timeout for quick unit tests (5 seconds).
	/// </summary>
	public const int QuickTestTimeoutMs = 5_000;

	/// <summary>
	/// Timeout for medium-length tests (30 seconds).
	/// </summary>
	public const int MediumTestTimeoutMs = 30_000;

	/// <summary>
	/// Timeout for long-running integration tests (60 seconds).
	/// </summary>
	public const int LongTestTimeoutMs = 60_000;

	/// <summary>
	/// Maximum timeout for large file tests (5 minutes).
	/// </summary>
	public const int LargeFileTestTimeoutMs = 300_000;

	/// <summary>
	/// Creates a CancellationToken that cancels after the specified timeout.
	/// Use this in async tests for timeout support.
	/// </summary>
	public static CancellationToken CreateTimeout(int milliseconds = MediumTestTimeoutMs) {
		return new CancellationTokenSource(milliseconds).Token;
	}

	/// <summary>
	/// Creates a CancellationTokenSource for timeout management.
	/// Dispose after use to avoid leaks.
	/// </summary>
	public static CancellationTokenSource CreateTimeoutSource(int milliseconds = MediumTestTimeoutMs) {
		return new CancellationTokenSource(milliseconds);
	}

	/// <summary>
	/// Runs an action with a timeout. Throws OperationCanceledException if timeout exceeded.
	/// </summary>
	public static void RunWithTimeout(Action action, int timeoutMs = MediumTestTimeoutMs) {
		using var cts = new CancellationTokenSource(timeoutMs);
		var task = Task.Run(action, cts.Token);

		if (!task.Wait(timeoutMs)) {
			cts.Cancel();
			throw new TimeoutException($"Test exceeded timeout of {timeoutMs}ms");
		}

		// Propagate any exceptions from the task
		if (task.IsFaulted && task.Exception != null) {
			throw task.Exception.InnerException ?? task.Exception;
		}
	}

	/// <summary>
	/// Runs an async function with a timeout. Throws TimeoutException if timeout exceeded.
	/// </summary>
	public static async Task RunWithTimeoutAsync(Func<Task> action, int timeoutMs = MediumTestTimeoutMs) {
		using var cts = new CancellationTokenSource(timeoutMs);
		var task = action();
		var completedTask = await Task.WhenAny(task, Task.Delay(timeoutMs, cts.Token));

		if (completedTask != task) {
			throw new TimeoutException($"Test exceeded timeout of {timeoutMs}ms");
		}

		await task; // Propagate any exceptions
	}
}
