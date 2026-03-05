namespace AiCFO.Infrastructure.Services;

using AiCFO.Application.Common;
using AiCFO.Domain.Entities;
using AiCFO.Domain.ValueObjects;
using AiCFO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Reconciliation service implementation.
/// Handles matching bank transactions with journal entries using various algorithms.
/// </summary>
public class ReconciliationService : IReconciliationService
{
    private readonly AppDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ReconciliationService> _logger;

    public ReconciliationService(
        AppDbContext context,
        ITenantContext tenantContext,
        ILogger<ReconciliationService> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Get a specific reconciliation match.
    /// </summary>
    public async Task<ReconciliationMatch?> GetReconciliationMatchAsync(
        Guid tenantId,
        Guid matchId,
        CancellationToken cancellationToken)
    {
        return await _context.ReconciliationMatches
            .Where(m => m.TenantId == tenantId && m.Id == matchId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Get reconciliation matches by status.
    /// </summary>
    public async Task<IReadOnlyList<ReconciliationMatch>> GetReconciliationMatchesByStatusAsync(
        Guid tenantId,
        ReconciliationStatus status,
        CancellationToken cancellationToken)
    {
        return await _context.ReconciliationMatches
            .Where(m => m.TenantId == tenantId && m.Status == status)
            .OrderByDescending(m => m.MatchedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get unconfirmed (proposed) matches.
    /// </summary>
    public async Task<IReadOnlyList<ReconciliationMatch>> GetUnconfirmedMatchesAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return await _context.ReconciliationMatches
            .Where(m => m.TenantId == tenantId && m.Status == ReconciliationStatus.Proposed)
            .OrderBy(m => m.MatchScore.ConfidencePercentage)  // Low confidence first
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Create a new reconciliation match.
    /// </summary>
    public async Task<ReconciliationMatch> CreateMatchAsync(
        Guid tenantId,
        Guid bankTransactionId,
        Guid journalEntryId,
        MatchScore matchScore,
        VarianceAmount varianceAmount,
        TimelineVariance timelineVariance,
        Guid createdBy,
        CancellationToken cancellationToken)
    {
        var match = new ReconciliationMatch(
            id: Guid.NewGuid(),
            tenantId: tenantId,
            bankTransactionId: bankTransactionId,
            journalEntryId: journalEntryId,
            matchScore: matchScore,
            varianceAmount: varianceAmount,
            timelineVariance: timelineVariance,
            createdBy: createdBy);

        _context.ReconciliationMatches.Add(match);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created reconciliation match {MatchId} for tenant {TenantId} with {Confidence}% confidence",
            match.Id, tenantId, matchScore.ConfidencePercentage);

        return match;
    }

    /// <summary>
    /// Confirm a reconciliation match.
    /// </summary>
    public async Task<bool> ConfirmMatchAsync(
        Guid tenantId,
        Guid matchId,
        Guid confirmedBy,
        string? notes,
        CancellationToken cancellationToken)
    {
        var match = await GetReconciliationMatchAsync(tenantId, matchId, cancellationToken);
        if (match is null)
            return false;

        try
        {
            match.Confirm(confirmedBy, notes);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Confirmed reconciliation match {MatchId}", matchId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm reconciliation match {MatchId}", matchId);
            return false;
        }
    }

    /// <summary>
    /// Reject a reconciliation match.
    /// </summary>
    public async Task<bool> RejectMatchAsync(
        Guid tenantId,
        Guid matchId,
        Guid rejectedBy,
        string reason,
        CancellationToken cancellationToken)
    {
        var match = await GetReconciliationMatchAsync(tenantId, matchId, cancellationToken);
        if (match is null)
            return false;

        try
        {
            match.Reject(rejectedBy, reason);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Rejected reconciliation match {MatchId}: {Reason}", matchId, reason);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reject reconciliation match {MatchId}", matchId);
            return false;
        }
    }

    /// <summary>
    /// Add notes to a reconciliation match.
    /// </summary>
    public async Task<bool> AddNotesAsync(
        Guid tenantId,
        Guid matchId,
        string notes,
        Guid addedBy,
        CancellationToken cancellationToken)
    {
        var match = await GetReconciliationMatchAsync(tenantId, matchId, cancellationToken);
        if (match is null)
            return false;

        try
        {
            match.AddNotes(notes, addedBy);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add notes to reconciliation match {MatchId}", matchId);
            return false;
        }
    }

    /// <summary>
    /// Find exact matches (same amount and date).
    /// </summary>
    public async Task<List<ReconciliationMatch>> FindExactMatchesAsync(
        Guid tenantId,
        IReadOnlyList<BankTransaction> bankTransactions,
        IReadOnlyList<JournalEntry> journalEntries,
        Guid createdBy,
        CancellationToken cancellationToken)
    {
        var matches = new List<ReconciliationMatch>();

        foreach (var bankTx in bankTransactions)
        {
            // Look for journal entries with exact amount match and same date
            var exactMatches = journalEntries.Where(je =>
                je.TotalDebits.Amount == bankTx.Amount.Amount &&
                je.EntryDate.Date == bankTx.TransactionDate.Date).ToList();

            foreach (var journalEntry in exactMatches)
            {
                var matchScore = MatchScore.CreateExactMatch("Exact amount and date match");
                var variance = new VarianceAmount(bankTx.Amount, journalEntry.TotalDebits);
                var timeline = new TimelineVariance(bankTx.TransactionDate, journalEntry.EntryDate);

                var match = await CreateMatchAsync(
                    tenantId,
                    bankTx.Id,
                    journalEntry.Id,
                    matchScore,
                    variance,
                    timeline,
                    createdBy,
                    cancellationToken);

                matches.Add(match);
            }
        }

        _logger.LogInformation("Found {Count} exact matches for tenant {TenantId}", matches.Count, tenantId);
        return matches;
    }

    /// <summary>
    /// Find partial matches (within variance threshold).
    /// </summary>
    public async Task<List<ReconciliationMatch>> FindPartialMatchesAsync(
        Guid tenantId,
        IReadOnlyList<BankTransaction> bankTransactions,
        IReadOnlyList<JournalEntry> journalEntries,
        decimal varianceThreshold,
        Guid createdBy,
        CancellationToken cancellationToken)
    {
        var matches = new List<ReconciliationMatch>();

        foreach (var bankTx in bankTransactions)
        {
            // Already matched transactions - skip
            var alreadyMatched = await _context.ReconciliationMatches
                .AnyAsync(m => m.BankTransactionId == bankTx.Id && m.TenantId == tenantId, cancellationToken);

            if (alreadyMatched)
                continue;

            // Look for journal entries within variance threshold
            var partialMatches = journalEntries.Where(je =>
                je.TotalDebits.Currency.Code == bankTx.Amount.Currency.Code &&
                Math.Abs(je.TotalDebits.Amount - bankTx.Amount.Amount) / bankTx.Amount.Amount <= (varianceThreshold / 100))
                .ToList();

            foreach (var journalEntry in partialMatches)
            {
                var variance = new VarianceAmount(bankTx.Amount, journalEntry.TotalDebits);

                // Calculate confidence based on variance percentage
                var confidence = 100 - Math.Min(variance.Percentage, 100);
                var matchScore = MatchScore.CreatePartialMatch(
                    confidence,
                    $"Partial match: {variance.Percentage:F2}% variance");

                var timeline = new TimelineVariance(bankTx.TransactionDate, journalEntry.EntryDate);

                var match = await CreateMatchAsync(
                    tenantId,
                    bankTx.Id,
                    journalEntry.Id,
                    matchScore,
                    variance,
                    timeline,
                    createdBy,
                    cancellationToken);

                matches.Add(match);
            }
        }

        _logger.LogInformation("Found {Count} partial matches for tenant {TenantId}", matches.Count, tenantId);
        return matches;
    }

    /// <summary>
    /// Find matches within a date range tolerance.
    /// </summary>
    public async Task<List<ReconciliationMatch>> FindDateRangeMatchesAsync(
        Guid tenantId,
        IReadOnlyList<BankTransaction> bankTransactions,
        IReadOnlyList<JournalEntry> journalEntries,
        int dayTolerance,
        Guid createdBy,
        CancellationToken cancellationToken)
    {
        var matches = new List<ReconciliationMatch>();

        foreach (var bankTx in bankTransactions)
        {
            // Already matched transactions - skip
            var alreadyMatched = await _context.ReconciliationMatches
                .AnyAsync(m => m.BankTransactionId == bankTx.Id && m.TenantId == tenantId, cancellationToken);

            if (alreadyMatched)
                continue;

            // Look for journal entries within date range and same amount
            var rangeMatches = journalEntries.Where(je =>
                je.TotalDebits.Amount == bankTx.Amount.Amount &&
                je.TotalDebits.Currency.Code == bankTx.Amount.Currency.Code &&
                Math.Abs((je.EntryDate - bankTx.TransactionDate).Days) <= dayTolerance)
                .ToList();

            foreach (var journalEntry in rangeMatches)
            {
                var variance = new VarianceAmount(bankTx.Amount, journalEntry.TotalDebits);
                var timeline = new TimelineVariance(bankTx.TransactionDate, journalEntry.EntryDate);

                // Confidence decreases with date difference
                var dateDifference = Math.Abs((bankTx.TransactionDate - journalEntry.EntryDate).Days);
                var dateConfidence = 100 - (dateDifference * 5); // 5% loss per day
                var confidence = Math.Max(dateConfidence, 50); // Minimum 50% confidence

                var matchScore = MatchScore.CreatePartialMatch(
                    confidence,
                    $"Date range match: {dateDifference} days difference");

                var match = await CreateMatchAsync(
                    tenantId,
                    bankTx.Id,
                    journalEntry.Id,
                    matchScore,
                    variance,
                    timeline,
                    createdBy,
                    cancellationToken);

                matches.Add(match);
            }
        }

        _logger.LogInformation("Found {Count} date range matches for tenant {TenantId}", matches.Count, tenantId);
        return matches;
    }

    /// <summary>
    /// Get unmatched bank transactions (not matched to journal entries).
    /// </summary>
    public async Task<IReadOnlyList<UnmatchedBankTransaction>> GetUnmatchedBankTransactionsAsync(
        Guid tenantId,
        int minDaysOld = 0,
        CancellationToken cancellationToken = default)
    {
        return await _context.UnmatchedBankTransactions
            .Where(u => u.TenantId == tenantId && u.DaysUnmatched >= minDaysOld)
            .OrderByDescending(u => u.DaysUnmatched)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get unmatched journal entries (not matched to bank transactions).
    /// </summary>
    public async Task<IReadOnlyList<UnmatchedJournalEntry>> GetUnmatchedJournalEntriesAsync(
        Guid tenantId,
        int minDaysOld = 0,
        CancellationToken cancellationToken = default)
    {
        return await _context.UnmatchedJournalEntries
            .Where(u => u.TenantId == tenantId && u.DaysUnmatched >= minDaysOld)
            .OrderByDescending(u => u.DaysUnmatched)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Update unmatched items list (calculate days unmatched).
    /// </summary>
    public async Task<bool> UpdateUnmatchedItemsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        try
        {
            var today = DateTime.UtcNow.Date;

            // Clear existing unmatched lists for this tenant
            var existingBankTransactions = await _context.UnmatchedBankTransactions
                .Where(u => u.TenantId == tenantId)
                .ToListAsync(cancellationToken);
            _context.UnmatchedBankTransactions.RemoveRange(existingBankTransactions);

            var existingJournalEntries = await _context.UnmatchedJournalEntries
                .Where(u => u.TenantId == tenantId)
                .ToListAsync(cancellationToken);
            _context.UnmatchedJournalEntries.RemoveRange(existingJournalEntries);

            // Find unmatched bank transactions
            var matchedBankTxIds = await _context.ReconciliationMatches
                .Where(m => m.TenantId == tenantId && m.Status == ReconciliationStatus.Confirmed)
                .Select(m => m.BankTransactionId)
                .ToListAsync(cancellationToken);

            var unmatchedBankTransactions = await _context.BankTransactions
                .Where(b => b.TenantId == tenantId && !matchedBankTxIds.Contains(b.Id))
                .ToListAsync(cancellationToken);

            foreach (var bankTx in unmatchedBankTransactions)
            {
                var daysUnmatched = (today - bankTx.TransactionDate.Date).Days;
                var unmatchedTx = new UnmatchedBankTransaction(
                    id: Guid.NewGuid(),
                    tenantId: tenantId,
                    bankTransactionId: bankTx.Id,
                    transactionReference: bankTx.Reference ?? bankTx.BankTransactionId,
                    amount: bankTx.Amount,
                    transactionDate: bankTx.TransactionDate,
                    description: bankTx.Description,
                    daysUnmatched: daysUnmatched);

                _context.UnmatchedBankTransactions.Add(unmatchedTx);
            }

            // Find unmatched journal entries
            var matchedJournalEntryIds = await _context.ReconciliationMatches
                .Where(m => m.TenantId == tenantId && m.Status == ReconciliationStatus.Confirmed)
                .Select(m => m.JournalEntryId)
                .ToListAsync(cancellationToken);

            var unmatchedJournalEntries = await _context.JournalEntries
                .Where(j => j.TenantId == tenantId && !matchedJournalEntryIds.Contains(j.Id))
                .ToListAsync(cancellationToken);

            foreach (var journalEntry in unmatchedJournalEntries)
            {
                var daysUnmatched = (today - journalEntry.EntryDate.Date).Days;
                var unmatchedEntry = new UnmatchedJournalEntry(
                    id: Guid.NewGuid(),
                    tenantId: tenantId,
                    journalEntryId: journalEntry.Id,
                    referenceNumber: journalEntry.ReferenceNumber,
                    amount: journalEntry.TotalDebits,
                    entryDate: journalEntry.EntryDate,
                    description: journalEntry.Description,
                    daysUnmatched: daysUnmatched);

                _context.UnmatchedJournalEntries.Add(unmatchedEntry);
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated unmatched items for tenant {TenantId}", tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update unmatched items for tenant {TenantId}", tenantId);
            return false;
        }
    }

    /// <summary>
    /// Create a reconciliation session.
    /// </summary>
    public async Task<ReconciliationSession> CreateSessionAsync(
        Guid tenantId,
        string sessionName,
        Guid createdBy,
        CurrencyCode currencyCode,
        CancellationToken cancellationToken)
    {
        var session = new ReconciliationSession(
            id: Guid.NewGuid(),
            tenantId: tenantId,
            sessionName: sessionName,
            createdBy: createdBy,
            currencyCode: currencyCode);

        _context.ReconciliationSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created reconciliation session {SessionId}: {SessionName}", session.Id, sessionName);
        return session;
    }

    /// <summary>
    /// Get a reconciliation session.
    /// </summary>
    public async Task<ReconciliationSession?> GetSessionAsync(
        Guid tenantId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        return await _context.ReconciliationSessions
            .Where(s => s.TenantId == tenantId && s.Id == sessionId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Get recent reconciliation sessions.
    /// </summary>
    public async Task<IReadOnlyList<ReconciliationSession>> GetRecentSessionsAsync(
        Guid tenantId,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        return await _context.ReconciliationSessions
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.SessionDate)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Complete a reconciliation session.
    /// </summary>
    public async Task<bool> CompleteSessionAsync(
        Guid tenantId,
        Guid sessionId,
        Guid completedBy,
        CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(tenantId, sessionId, cancellationToken);
        if (session is null)
            return false;

        try
        {
            session.Complete(completedBy);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Completed reconciliation session {SessionId}", sessionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete reconciliation session {SessionId}", sessionId);
            return false;
        }
    }

    /// <summary>
    /// Get reconciliation statistics.
    /// </summary>
    public async Task<ReconciliationStats> GetReconciliationStatsAsync(
        Guid tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ReconciliationMatches.Where(m => m.TenantId == tenantId);

        if (fromDate.HasValue)
            query = query.Where(m => m.MatchedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(m => m.MatchedAt <= toDate.Value);

        var matches = await query.ToListAsync(cancellationToken);

        var totalMatches = matches.Count;
        var confirmedMatches = matches.Count(m => m.Status == ReconciliationStatus.Confirmed);
        var pendingMatches = matches.Count(m => m.Status == ReconciliationStatus.Proposed);
        var rejectedMatches = matches.Count(m => m.Status == ReconciliationStatus.Rejected);

        var totalBankTransactions = await _context.BankTransactions
            .Where(b => b.TenantId == tenantId)
            .CountAsync(cancellationToken);

        var matchRate = totalBankTransactions > 0 ? (confirmedMatches * 100m) / totalBankTransactions : 0;

        var totalMatchedAmount = matches
            .Where(m => m.Status == ReconciliationStatus.Confirmed)
            .Select(m => m.VarianceAmount.Amount)
            .FirstOrDefault() ?? Money.Create(0, Currency.NGN.Code);

        var unmatchedBankTransactions = await GetUnmatchedBankTransactionsAsync(tenantId, 0, cancellationToken);
        var unmatchedJournalEntries = await GetUnmatchedJournalEntriesAsync(tenantId, 0, cancellationToken);

        var totalUnmatchedAmount = Money.Create(0, Currency.NGN.Code);
        foreach (var unmatched in unmatchedBankTransactions)
        {
            totalUnmatchedAmount = totalUnmatchedAmount.Add(unmatched.Amount);
        }

        var avgConfidence = matches.Count > 0
            ? matches.Average(m => m.MatchScore.ConfidencePercentage)
            : 0;

        return new ReconciliationStats
        {
            TotalMatches = totalMatches,
            ConfirmedMatches = confirmedMatches,
            PendingMatches = pendingMatches,
            RejectedMatches = rejectedMatches,
            MatchRate = matchRate,
            TotalMatchedAmount = totalMatchedAmount,
            TotalUnmatchedAmount = totalUnmatchedAmount,
            AverageMatchConfidence = (decimal)avgConfidence,
            UnmatchedBankTransactions = unmatchedBankTransactions.Count,
            UnmatchedJournalEntries = unmatchedJournalEntries.Count
        };
    }

    /// <summary>
    /// Get reconciliation health report.
    /// </summary>
    public async Task<ReconciliationHealthReport> GetReconciliationHealthAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var stats = await GetReconciliationStatsAsync(tenantId, null, null, cancellationToken);
        var unmatchedBankTransactions = await GetUnmatchedBankTransactionsAsync(tenantId, 30, cancellationToken);  // 30+ days old
        var unmatchedJournalEntries = await GetUnmatchedJournalEntriesAsync(tenantId, 30, cancellationToken);

        var agedUnmatchedAmount = Money.Create(0, Currency.NGN.Code);
        foreach (var unmatched in unmatchedBankTransactions)
        {
            agedUnmatchedAmount = agedUnmatchedAmount.Add(unmatched.Amount);
        }

        var lowConfidenceMatches = await _context.ReconciliationMatches
            .Where(m => m.TenantId == tenantId && m.MatchScore.ConfidencePercentage < 60)
            .CountAsync(cancellationToken);

        var allMatches = await _context.ReconciliationMatches
            .Where(m => m.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var totalMatches = allMatches.Count;
        var highVarianceCount = allMatches.Count(m => m.VarianceAmount.Percentage > 1);
        var highVariancePercentage = totalMatches > 0 ? (highVarianceCount * 100m) / totalMatches : 0;

        var healthStatus = stats.MatchRate switch
        {
            >= 95 => "Excellent",
            >= 80 => "Good",
            >= 60 => "Fair",
            >= 40 => "Poor",
            _ => "Critical"
        };

        var recommendations = new List<string>();

        if (stats.MatchRate < 80)
            recommendations.Add("Consider reviewing unmatched transactions for manual reconciliation");

        if (stats.AverageMatchConfidence < 70)
            recommendations.Add("Some matches have low confidence scores - review and adjust matching thresholds");

        if (unmatchedBankTransactions.Count > 100)
            recommendations.Add("Large number of aged unmatched bank transactions - prioritize reconciliation");

        if (unmatchedJournalEntries.Count > 50)
            recommendations.Add("Significant aged unmatched journal entries - verify data entry quality");

        if (stats.PendingMatches > 0)
            recommendations.Add($"{stats.PendingMatches} proposed matches awaiting confirmation");

        return new ReconciliationHealthReport
        {
            GeneratedAt = DateTime.UtcNow,
            AgedUnmatchedCount = unmatchedBankTransactions.Count + unmatchedJournalEntries.Count,
            AgedUnmatchedAmount = agedUnmatchedAmount,
            LowConfidenceMatchCount = lowConfidenceMatches,
            HighVariancePercentage = highVariancePercentage,
            HealthStatus = healthStatus,
            Recommendations = recommendations
        };
    }
}
