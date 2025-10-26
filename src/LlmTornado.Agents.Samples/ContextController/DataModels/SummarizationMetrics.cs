namespace LlmTornado.Agents.Samples.ContextController;

/// <summary>
/// Metrics for tracking summarization performance
/// </summary>
public class SummarizationMetrics
{
    private long _totalSummarizations;
    private long _largeMessageCompressions;
    private long _uncompressedCompressions;
    private long _recompressions;
    private long _totalMessagesBefore;
    private long _totalMessagesAfter;
    private long _totalTokensBefore;
    private long _totalTokensAfter;
    private long _totalDurationMs;
    private readonly object _lock = new();

    public long TotalSummarizations
    {
        get { lock (_lock) return _totalSummarizations; }
    }

    public long LargeMessageCompressions
    {
        get { lock (_lock) return _largeMessageCompressions; }
    }

    public long UncompressedCompressions
    {
        get { lock (_lock) return _uncompressedCompressions; }
    }

    public long Recompressions
    {
        get { lock (_lock) return _recompressions; }
    }

    public long TotalMessagesBefore
    {
        get { lock (_lock) return _totalMessagesBefore; }
    }

    public long TotalMessagesAfter
    {
        get { lock (_lock) return _totalMessagesAfter; }
    }

    public long TotalTokensBefore
    {
        get { lock (_lock) return _totalTokensBefore; }
    }

    public long TotalTokensAfter
    {
        get { lock (_lock) return _totalTokensAfter; }
    }

    public long TotalDurationMs
    {
        get { lock (_lock) return _totalDurationMs; }
    }

    public double AverageDurationMs
    {
        get
        {
            lock (_lock)
            {
                return _totalSummarizations > 0
                    ? (double)_totalDurationMs / _totalSummarizations
                    : 0;
            }
        }
    }

    public double AverageCompressionRatio
    {
        get
        {
            lock (_lock)
            {
                return _totalTokensBefore > 0
                    ? (double)_totalTokensAfter / _totalTokensBefore
                    : 0;
            }
        }
    }

    public long TotalTokensSaved
    {
        get { lock (_lock) return _totalTokensBefore - _totalTokensAfter; }
    }

    internal void RecordSummarization(
        int messagesBefore,
        int messagesAfter,
        int tokensBefore,
        int tokensAfter,
        long durationMs,
        string type)
    {
        lock (_lock)
        {
            _totalSummarizations++;
            _totalMessagesBefore += messagesBefore;
            _totalMessagesAfter += messagesAfter;
            _totalTokensBefore += tokensBefore;
            _totalTokensAfter += tokensAfter;
            _totalDurationMs += durationMs;

            switch (type)
            {
                case "large":
                    _largeMessageCompressions++;
                    break;
                case "uncompressed":
                    _uncompressedCompressions++;
                    break;
                case "recompressed":
                    _recompressions++;
                    break;
            }
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _totalSummarizations = 0;
            _largeMessageCompressions = 0;
            _uncompressedCompressions = 0;
            _recompressions = 0;
            _totalMessagesBefore = 0;
            _totalMessagesAfter = 0;
            _totalTokensBefore = 0;
            _totalTokensAfter = 0;
            _totalDurationMs = 0;
        }
    }

    public override string ToString()
    {
        return $@"Summarization Metrics:
- Total Summarizations: {TotalSummarizations:N0}
  - Large Messages: {LargeMessageCompressions:N0}
  - Uncompressed: {UncompressedCompressions:N0}
  - Re-compressions: {Recompressions:N0}
- Messages: {TotalMessagesBefore:N0} ? {TotalMessagesAfter:N0}
- Tokens: {TotalTokensBefore:N0} ? {TotalTokensAfter:N0} (saved {TotalTokensSaved:N0})
- Compression Ratio: {AverageCompressionRatio:P1}
- Avg Duration: {AverageDurationMs:F2}ms
- Total Duration: {TotalDurationMs:N0}ms";
    }
}
