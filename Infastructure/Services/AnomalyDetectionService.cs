namespace AiCFO.Infastructure.Services;

/// <summary>
/// Service implementation for detecting financial anomalies.
/// Uses statistical analysis and pattern matching to identify unusual transactions.
/// </summary>
public class AnomalyDetectionService : IAnomalyDetectionService
{
    private readonly AppDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<AnomalyDetectionService> _logger;

    public AnomalyDetectionService(
        AppDbContext context,
        ITenantContext tenantContext,
        ILogger<AnomalyDetectionService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<AnomalyDetectionResult>> AnalyzeJournalEntryAsync(
        Guid journalEntryId,
        Guid accountId,
        Money amount,
        AccountType accountType,
        DateTime transactionDate)
    {
        try
        {
            _logger.LogInformation(
                "Analyzing journal entry {JournalEntryId} for account {AccountId}, amount {Amount}, type {AccountType}",
                journalEntryId, accountId, amount.Amount, accountType);

            var anomalies = new List<AnomalyScore>();
            var riskScore = 0m;

            // Analyze amount for outliers
            var amountAnomalies = await DetectUnusualAmountsAsync(accountId, 90);
            if (amountAnomalies.IsSuccess && amountAnomalies.Value != null)
            {
                var significantAmountAnomalies = amountAnomalies.Value
                    .Where(a => a.IsSignificant && a.ConfidencePercentage >= 75)
                    .ToList();
                anomalies.AddRange(significantAmountAnomalies);
                riskScore += significantAmountAnomalies.Any() ? 30m : 0m;
            }

            // Analyze frequency
            var frequencyAnomalies = await DetectUnusualFrequencyAsync(accountId, 90);
            if (frequencyAnomalies.IsSuccess && frequencyAnomalies.Value != null)
            {
                var significantFrequencyAnomalies = frequencyAnomalies.Value
                    .Where(a => a.IsSignificant && a.ConfidencePercentage >= 75)
                    .ToList();
                anomalies.AddRange(significantFrequencyAnomalies);
                riskScore += significantFrequencyAnomalies.Any() ? 25m : 0m;
            }

            // Check for duplicate patterns
            var duplicateAnomalies = await DetectDuplicatePatternsAsync(accountId, 90);
            if (duplicateAnomalies.IsSuccess && duplicateAnomalies.Value != null)
            {
                var significantDuplicates = duplicateAnomalies.Value
                    .Where(a => a.IsSignificant && a.ConfidencePercentage >= 70)
                    .ToList();
                anomalies.AddRange(significantDuplicates);
                riskScore += significantDuplicates.Any() ? 20m : 0m;
            }

            var hasAnomalies = anomalies.Any();
            var summary = hasAnomalies
                ? $"Detected {anomalies.Count} anomaly(ies) with risk score {Math.Min(riskScore, 100m):F0}%"
                : "No significant anomalies detected";

            _logger.LogInformation(
                "Journal entry analysis complete: {Summary}",
                summary);

            return Result<AnomalyDetectionResult>.Ok(new AnomalyDetectionResult(
                hasAnomalies,
                anomalies,
                Math.Min(riskScore, 100m),
                summary));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing journal entry {JournalEntryId}", journalEntryId);
            return Result<AnomalyDetectionResult>.Fail($"Error analyzing journal entry: {ex.Message}");
        }
    }

    public async Task<Result<AnomalyDetectionResult>> AnalyzeBankTransactionAsync(
        Guid bankTransactionId,
        Money amount,
        string? description,
        DateTime transactionDate)
    {
        try
        {
            _logger.LogInformation(
                "Analyzing bank transaction {BankTransactionId}, amount {Amount}, description: {Description}",
                bankTransactionId, amount.Amount, description ?? "N/A");

            var anomalies = new List<AnomalyScore>();
            var riskScore = 0m;

            // Check for unusual amount
            var recentTransactions = await _context.BankTransactions
                .Where(bt => bt.TenantId == _tenantContext.TenantId &&
                       bt.TransactionDate >= DateTime.UtcNow.AddDays(-90))
                .Select(bt => bt.Amount.Amount)
                .ToListAsync();

            if (recentTransactions.Any())
            {
                var mean = recentTransactions.Average();
                var stdDev = CalculateStandardDeviation(recentTransactions, mean);

                if (stdDev > 0)
                {
                    var zScore = Math.Abs((amount.Amount - mean) / stdDev);
                    if (zScore > 3) // 3 standard deviations = outlier
                    {
                        var anomaly = AnomalyScore.Create(
                            Math.Min(70m + (zScore - 3) * 10, 99m),
                            AnomalySeverity.High,
                            AnomalyType.OutlierAmount,
                            $"Amount {amount.Amount} is {zScore:F2} standard deviations from mean {mean:F2}",
                            new Dictionary<string, object>
                            {
                                { "Mean", mean },
                                { "StdDev", stdDev },
                                { "ZScore", zScore }
                            });
                        anomalies.Add(anomaly);
                        riskScore += 35m;
                    }
                }
            }

            // Check for duplicate description
            if (!string.IsNullOrEmpty(description))
            {
                var duplicateCount = await _context.BankTransactions
                    .Where(bt => bt.TenantId == _tenantContext.TenantId &&
                           bt.Description == description &&
                           bt.TransactionDate >= DateTime.UtcNow.AddDays(-7))
                    .CountAsync();

                if (duplicateCount > 3)
                {
                    var anomaly = AnomalyScore.Create(
                        Math.Min(60m + (duplicateCount * 5), 95m),
                        AnomalySeverity.Medium,
                        AnomalyType.DuplicatePattern,
                        $"Description '{description}' repeated {duplicateCount} times in 7 days",
                        new Dictionary<string, object> { { "DuplicateCount", duplicateCount } });
                    anomalies.Add(anomaly);
                    riskScore += 25m;
                }
            }

            var hasAnomalies = anomalies.Any();
            var summary = hasAnomalies
                ? $"Detected {anomalies.Count} anomaly(ies) with risk score {Math.Min(riskScore, 100m):F0}%"
                : "No significant anomalies detected";

            return Result<AnomalyDetectionResult>.Ok(new AnomalyDetectionResult(hasAnomalies, anomalies, Math.Min(riskScore, 100m), summary));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing bank transaction {BankTransactionId}", bankTransactionId);
            return Result<AnomalyDetectionResult>.Fail($"Error analyzing bank transaction: {ex.Message}");
        }
    }

    public async Task<Result<List<AnomalyScore>>> DetectUnusualAmountsAsync(
        Guid accountId,
        int lookbackDays = 90)
    {
        try
        {
            _logger.LogInformation("Detecting unusual amounts for account {AccountId}", accountId);

            var cutoffDate = DateTime.UtcNow.AddDays(-lookbackDays);
            var journalLines = await _context.JournalLines
                .Where(jl => jl.JournalEntry.Id == accountId &&
                       jl.JournalEntry.TenantId == _tenantContext.TenantId &&
                       jl.CreatedAt >= cutoffDate)
                .Select(jl => jl.Amount.Amount)
                .ToListAsync();

            if (journalLines.Count < 5)
                return Result<List<AnomalyScore>>.Ok(new List<AnomalyScore>());

            var mean = journalLines.Average();
            var stdDev = CalculateStandardDeviation(journalLines, mean);
            var anomalies = new List<AnomalyScore>();

            if (stdDev > 0)
            {
                foreach (var amount in journalLines)
                {
                    var zScore = Math.Abs((amount - mean) / stdDev);
                    if (zScore > 2.5m) // 2.5 standard deviations
                    {
                        var confidence = Math.Min(70m + ((zScore - 2.5m) * 15), 99m);
                        var anomaly = AnomalyScore.Create(
                            confidence,
                            zScore > 3 ? AnomalySeverity.High : AnomalySeverity.Medium,
                            AnomalyType.OutlierAmount,
                            $"Amount {amount:F2} deviates {zScore:F2} standard deviations from mean {mean:F2}",
                            new Dictionary<string, object>
                            {
                                { "Amount", amount },
                                { "Mean", mean },
                                { "StdDev", stdDev },
                                { "ZScore", zScore }
                            });
                        anomalies.Add(anomaly);
                    }
                }
            }

            _logger.LogInformation("Detected {AnomalyCount} unusual amounts", anomalies.Count);
            return Result<List<AnomalyScore>>.Ok(anomalies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting unusual amounts");
            return Result<List<AnomalyScore>>.Fail($"Error detecting unusual amounts: {ex.Message}");
        }
    }

    public async Task<Result<List<AnomalyScore>>> DetectUnusualFrequencyAsync(
        Guid accountId,
        int lookbackDays = 90)
    {
        try
        {
            _logger.LogInformation("Detecting unusual frequency for account {AccountId}", accountId);

            var cutoffDate = DateTime.UtcNow.AddDays(-lookbackDays);
            var dailyCounts = await _context.JournalLines
                .Where(jl => jl.JournalEntry.Id == accountId &&
                       jl.JournalEntry.TenantId == _tenantContext.TenantId &&
                       jl.CreatedAt >= cutoffDate)
                .GroupBy(jl => jl.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            if (dailyCounts.Count < 5)
                return Result<List<AnomalyScore>>.Ok(new List<AnomalyScore>());

            var counts = dailyCounts.Select(dc => (decimal)dc.Count).ToList();
            var mean = counts.Average();
            var stdDev = CalculateStandardDeviation(counts, mean);
            var anomalies = new List<AnomalyScore>();

            if (stdDev > 0)
            {
                foreach (var dc in dailyCounts)
                {
                    var zScore = Math.Abs((dc.Count - mean) / stdDev);
                    if (zScore > 2)
                    {
                        var confidence = Math.Min(65m + ((zScore - 2) * 12), 98m);
                        var anomaly = AnomalyScore.Create(
                            confidence,
                            zScore > 2.5m ? AnomalySeverity.High : AnomalySeverity.Medium,
                            AnomalyType.UnusualFrequency,
                            $"Frequency {dc.Count} transactions on {dc.Date:yyyy-MM-dd} deviates {zScore:F2} std devs from mean {mean:F2}",
                            new Dictionary<string, object>
                            {
                                { "Date", dc.Date },
                                { "TransactionCount", dc.Count },
                                { "Mean", mean },
                                { "StdDev", stdDev }
                            });
                        anomalies.Add(anomaly);
                    }
                }
            }

            _logger.LogInformation("Detected {AnomalyCount} unusual frequency patterns", anomalies.Count);
            return Result<List<AnomalyScore>>.Ok(anomalies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting unusual frequency");
            return Result<List<AnomalyScore>>.Fail($"Error detecting unusual frequency: {ex.Message}");
        }
    }

    public async Task<Result<List<AnomalyScore>>> DetectDuplicatePatternsAsync(
        Guid accountId,
        int lookbackDays = 90)
    {
        try
        {
            _logger.LogInformation("Detecting duplicate patterns for account {AccountId}", accountId);

            var cutoffDate = DateTime.UtcNow.AddDays(-lookbackDays);
            var patterns = await _context.JournalLines
                .Where(jl => jl.JournalEntry.Id == accountId &&
                       jl.JournalEntry.TenantId == _tenantContext.TenantId &&
                       jl.CreatedAt >= cutoffDate)
                .GroupBy(jl => new { jl.Amount, jl.Description })
                .Where(g => g.Count() > 2)
                .Select(g => new
                {
                    g.Key.Amount,
                    g.Key.Description,
                    Count = g.Count(),
                    Dates = g.Select(jl => jl.CreatedAt).OrderByDescending(d => d).ToList()
                })
                .ToListAsync();

            var anomalies = new List<AnomalyScore>();

            foreach (var pattern in patterns)
            {
                var confidence = Math.Min(60m + (pattern.Count * 8), 95m);
                var anomaly = AnomalyScore.Create(
                    confidence,
                    pattern.Count > 5 ? AnomalySeverity.High : AnomalySeverity.Medium,
                    AnomalyType.DuplicatePattern,
                    $"Pattern repeated {pattern.Count} times: {pattern.Amount.Amount} - {pattern.Description}",
                    new Dictionary<string, object>
                    {
                        { "Amount", pattern.Amount.Amount },
                        { "Description", pattern.Description ?? "N/A" },
                        { "RepeatCount", pattern.Count },
                        { "LatestDate", pattern.Dates.First() }
                    });
                anomalies.Add(anomaly);
            }

            _logger.LogInformation("Detected {AnomalyCount} duplicate patterns", anomalies.Count);
            return Result<List<AnomalyScore>>.Ok(anomalies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting duplicate patterns");
            return Result<List<AnomalyScore>>.Fail($"Error detecting duplicate patterns: {ex.Message}");
        }
    }

    public async Task<Result<List<AnomalyScore>>> DetectTimeSeriesAnomaliesAsync(
        Guid accountId,
        int lookbackDays = 90)
    {
        try
        {
            _logger.LogInformation("Detecting time-series anomalies for account {AccountId}", accountId);

            var cutoffDate = DateTime.UtcNow.AddDays(-lookbackDays);
            var timeSeries = await _context.JournalLines
                .Where(jl => jl.JournalEntry.Id == accountId &&
                       jl.JournalEntry.TenantId == _tenantContext.TenantId &&
                       jl.CreatedAt >= cutoffDate)
                .OrderBy(jl => jl.CreatedAt)
                .Select(jl => new { jl.CreatedAt, jl.Amount.Amount })
                .ToListAsync();

            if (timeSeries.Count < 10)
                return Result<List<AnomalyScore>>.Ok(new List<AnomalyScore>());

            var amounts = timeSeries.Select(ts => ts.Amount).ToList();
            var movingAvg = CalculateMovingAverage(amounts, 7);
            var anomalies = new List<AnomalyScore>();

            for (int i = 7; i < timeSeries.Count; i++)
            {
                var actualValue = amounts[i];
                var predictedValue = movingAvg[i - 7];
                var deviation = Math.Abs(actualValue - predictedValue) / (predictedValue > 0 ? predictedValue : 1);

                if (deviation > 0.5m) // 50% deviation from trend
                {
                    var confidence = Math.Min(70m + (decimal)(deviation * 50), 95m);
                    var anomaly = AnomalyScore.Create(
                        confidence,
                        deviation > 1 ? AnomalySeverity.High : AnomalySeverity.Medium,
                        AnomalyType.TimeSeriesDeviation,
                        $"Value {actualValue:F2} deviates {(deviation * 100):F0}% from trend {predictedValue:F2}",
                        new Dictionary<string, object>
                        {
                            { "Date", timeSeries[i].CreatedAt },
                            { "ActualValue", actualValue },
                            { "TrendValue", predictedValue },
                            { "DevationPercent", deviation * 100 }
                        });
                    anomalies.Add(anomaly);
                }
            }

            _logger.LogInformation("Detected {AnomalyCount} time-series anomalies", anomalies.Count);
            return Result<List<AnomalyScore>>.Ok(anomalies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting time-series anomalies");
            return Result<List<AnomalyScore>>.Fail($"Error detecting time-series anomalies: {ex.Message}");
        }
    }

    public async Task<Result<List<AnomalyDto>>> ScanUnmatchedTransactionsAsync(
        AnomalySeverity minSeverity = AnomalySeverity.Medium)
    {
        try
        {
            _logger.LogInformation("Scanning unmatched transactions for anomalies");

            var unmatchedBankTransactions = await _context.UnmatchedBankTransactions
                .Where(ubt => ubt.TenantId == _tenantContext.TenantId)
                .ToListAsync();

            var anomalyDtos = new List<AnomalyDto>();

            foreach (var unmatchedTx in unmatchedBankTransactions)
            {
                var result = await AnalyzeBankTransactionAsync(
                    unmatchedTx.BankTransactionId,
                    unmatchedTx.Amount,
                    unmatchedTx.Description,
                    unmatchedTx.TransactionDate);

                if (result.IsSuccess && result.Value?.HasAnomalies == true)
                {
                    foreach (var anomaly in result.Value.Anomalies.Where(a => a.Severity >= minSeverity))
                    {
                        anomalyDtos.Add(new AnomalyDto(
                            Guid.NewGuid(),
                            anomaly,
                            unmatchedTx.BankTransactionId,
                            "BankTransaction",
                            unmatchedTx.Amount,
                            DateTime.UtcNow,
                            null,
                            null));
                    }
                }
            }

            return Result<List<AnomalyDto>>.Ok(anomalyDtos.OrderByDescending(a => a.Score.Severity).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning unmatched transactions");
            return Result<List<AnomalyDto>>.Fail($"Error scanning unmatched transactions: {ex.Message}");
        }
    }

    public async Task<Result<List<AnomalyDto>>> GetRecentAnomaliesAsync(
        int days = 30,
        AnomalySeverity? severityFilter = null)
    {
        try
        {
            _logger.LogInformation("Retrieving recent anomalies from last {Days} days", days);

            var cutoffDate = DateTime.UtcNow.AddDays(-days);

            // In a real implementation, this would query from a stored anomalies table
            // For now, we'll scan recent transactions
            var recentTransactions = await _context.BankTransactions
                .Where(bt => bt.TenantId == _tenantContext.TenantId && bt.TransactionDate >= cutoffDate)
                .Take(100)
                .ToListAsync();

            var anomalies = new List<AnomalyDto>();

            foreach (var tx in recentTransactions)
            {
                var result = await AnalyzeBankTransactionAsync(
                    tx.Id,
                    tx.Amount,
                    tx.Description,
                    tx.TransactionDate);

                if (result.IsSuccess && result.Value?.HasAnomalies == true)
                {
                    foreach (var anomaly in result.Value.Anomalies)
                    {
                        if (severityFilter == null || anomaly.Severity >= severityFilter)
                        {
                            anomalies.Add(new AnomalyDto(
                                Guid.NewGuid(),
                                anomaly,
                                tx.Id,
                                "BankTransaction",
                                tx.Amount,
                                DateTime.UtcNow,
                                null,
                                null));
                        }
                    }
                }
            }

            return Result<List<AnomalyDto>>.Ok(anomalies.OrderByDescending(a => a.Score.ConfidencePercentage).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent anomalies");
            return Result<List<AnomalyDto>>.Fail($"Error retrieving recent anomalies: {ex.Message}");
        }
    }

    public async Task<Result<bool>> DismissAnomalyAsync(
        Guid anomalyId,
        string? notes = null)
    {
        try
        {
            _logger.LogInformation("Dismissing anomaly {AnomalyId}", anomalyId);
            // In a real implementation, this would update an anomalies table
            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dismissing anomaly");
            return Result<bool>.Fail($"Error dismissing anomaly: {ex.Message}");
        }
    }

    public async Task<Result<bool>> FlagFalsePositiveAsync(
        Guid anomalyId,
        string reason)
    {
        try
        {
            _logger.LogInformation("Flagging anomaly {AnomalyId} as false positive: {Reason}", anomalyId, reason);
            // This would help retrain the ML models
            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flagging false positive");
            return Result<bool>.Fail($"Error flagging false positive: {ex.Message}");
        }
    }

    public async Task<Result<AnomalyStatsDto>> GetAnomalyStatsAsync()
    {
        try
        {
            _logger.LogInformation("Calculating anomaly statistics");

            var stats = new AnomalyStatsDto(
                TotalAnomalies: 0,
                CriticalCount: 0,
                HighCount: 0,
                MediumCount: 0,
                LowCount: 0,
                AverageConfidence: 0m,
                Trends: new(),
                CalculatedAt: DateTime.UtcNow);

            return Result<AnomalyStatsDto>.Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating anomaly statistics");
            return Result<AnomalyStatsDto>.Fail($"Error calculating anomaly statistics: {ex.Message}");
        }
    }

    // Helper methods
    private decimal CalculateStandardDeviation(List<decimal> values, decimal mean)
    {
        if (values.Count < 2)
            return 0m;

        var variance = values.Sum(v => (v - mean) * (v - mean)) / values.Count;
        return (decimal)Math.Sqrt((double)variance);
    }

    private List<decimal> CalculateMovingAverage(List<decimal> values, int windowSize)
    {
        var result = new List<decimal>();
        for (int i = 0; i < values.Count; i++)
        {
            var start = Math.Max(0, i - windowSize + 1);
            var window = values.Skip(start).Take(i - start + 1);
            result.Add(window.Average());
        }
        return result;
    }
}
