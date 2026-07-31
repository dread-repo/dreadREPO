using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Dread.Systems
{
    internal static class ErrorReportLogQueue
    {
        internal const int MaxPendingLogs = 100;
        internal const int MaxProcessPerFrame = 3;
        private const float DedupeCooldownSeconds = 60f;
        private const int HashPrefixLength = 16;
        // Prune trigger for RecentHashes (ERR-6): expired entries are dropped once
        // the map grows past this, so varied long sessions stay bounded.
        private const int MaxRecentHashes = 256;

        private static readonly Queue<RawLogEntry> PendingLogs = new Queue<RawLogEntry>(32);
        private static readonly object LogsLock = new object();
        private static readonly Dictionary<string, float> RecentHashes = new Dictionary<string, float>();
        private static bool _queueFullWarned;

        internal sealed class RawLogEntry
        {
            public string Message = string.Empty;
            public string StackTrace = string.Empty;
            public LogType Type;
        }

        internal static void EnqueueLog(string logString, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error)
                return;

            if (!ErrorReportingConsent.IsReportingAllowed())
                return;

            if (IsIgnoredSpam(logString, stackTrace))
                return;

            var hash = ComputeHash(stackTrace, logString);
            var now = Time.realtimeSinceStartup;
            var droppedAtCapacity = false;
            lock (LogsLock)
            {
                if (RecentHashes.TryGetValue(hash, out var last) && now - last < DedupeCooldownSeconds)
                    return;

                RecentHashes[hash] = now;
                if (RecentHashes.Count > MaxRecentHashes)
                    PruneExpiredHashes(now);

                if (PendingLogs.Count >= MaxPendingLogs)
                {
                    droppedAtCapacity = true;
                }
                else
                {
                    PendingLogs.Enqueue(new RawLogEntry
                    {
                        Message = logString,
                        StackTrace = stackTrace,
                        Type = type
                    });
                }
            }

            // ERR-5 backpressure: error spam must not silently lose signal. Warn
            // once per session (outside the lock; warnings do not re-enter here).
            if (droppedAtCapacity && !_queueFullWarned)
            {
                _queueFullWarned = true;
                LoggingService.LogWarning(
                    $"[ErrorReporter] Pending log queue is full ({MaxPendingLogs}); new errors are being dropped");
            }
        }

        // Caller holds LogsLock. Entries past the dedupe cooldown can never block
        // an enqueue again, so removing them does not change dedupe behavior.
        private static void PruneExpiredHashes(float now)
        {
            List<string>? expired = null;
            foreach (var entry in RecentHashes)
            {
                if (now - entry.Value >= DedupeCooldownSeconds)
                    (expired ??= new List<string>()).Add(entry.Key);
            }

            if (expired == null)
                return;

            foreach (var key in expired)
                RecentHashes.Remove(key);
        }

        internal static bool HasPending()
        {
            lock (LogsLock)
            {
                return PendingLogs.Count > 0;
            }
        }

        internal static bool TryDequeueBatch(out RawLogEntry[] batch)
        {
            return TryDequeueBatch(out batch, MaxProcessPerFrame);
        }

        internal static bool TryDequeueBatch(out RawLogEntry[] batch, int maxCount)
        {
            lock (LogsLock)
            {
                if (PendingLogs.Count == 0)
                {
                    batch = Array.Empty<RawLogEntry>();
                    return false;
                }

                var count = Math.Min(maxCount, PendingLogs.Count);
                batch = new RawLogEntry[count];
                for (var i = 0; i < count; i++)
                    batch[i] = PendingLogs.Dequeue();
                return true;
            }
        }

        internal static bool IsIgnoredSpam(string message, string stackTrace)
        {
            if (message.IndexOf("[Dread TestCrash]", StringComparison.Ordinal) >= 0
                || stackTrace.IndexOf("TestCrashSystem", StringComparison.Ordinal) >= 0)
                return true;
            if (message.IndexOf("DebugConsoleUI", StringComparison.Ordinal) >= 0
                || stackTrace.IndexOf("DebugConsoleUI", StringComparison.Ordinal) >= 0)
                return true;
            if (message.IndexOf("DebugTester", StringComparison.Ordinal) >= 0
                || stackTrace.IndexOf("SemiFunc.DebugTester", StringComparison.Ordinal) >= 0
                || stackTrace.IndexOf("DebugTester", StringComparison.Ordinal) >= 0)
                return true;
            return false;
        }

        internal static string ComputeHash(string stackTrace, string message)
        {
            using var sha = SHA256.Create();
            var input = $"{stackTrace}\n{message}";
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder();
            foreach (var b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString().Substring(0, HashPrefixLength);
        }
    }
}
