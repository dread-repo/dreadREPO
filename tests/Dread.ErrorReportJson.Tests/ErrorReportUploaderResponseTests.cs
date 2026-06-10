using System.Collections.Generic;
using Dread.Systems;
using Xunit;

namespace Dread.ErrorReportJson.Tests
{
    /// <summary>
    /// Worker response parsing in <see cref="ErrorReportUploader"/>. Bodies mirror
    /// workers/error-reporter results: {hash, status: created|skipped|error}.
    /// </summary>
    public class ErrorReportUploaderResponseTests
    {
        private static ErrorReport Report(string hash) => new ErrorReport { Hash = hash };

        private const string PartialSuccessBody =
            "{\"processed\":3,\"results\":["
            + "{\"hash\":\"aaa\",\"issueNumber\":12,\"status\":\"created\"},"
            + "{\"hash\":\"bbb\",\"status\":\"skipped\",\"reason\":\"duplicate\"},"
            + "{\"hash\":null,\"status\":\"error\",\"error\":\"Report missing Hash\"}]}";

        [Fact]
        public void CollectFailedReports_ReturnsOnlyHashesMarkedError()
        {
            var body =
                "{\"results\":[{\"hash\":\"aaa\",\"status\":\"created\"},"
                + "{\"hash\":\"bbb\",\"status\":\"error\",\"error\":\"boom\"}]}";
            var batch = new List<ErrorReport> { Report("aaa"), Report("bbb") };

            var failed = ErrorReportUploader.CollectFailedReports(body, batch);

            Assert.Single(failed);
            Assert.Equal("bbb", failed[0].Hash);
        }

        [Fact]
        public void HasUnmappedWorkerErrors_DetectsErrorStatusAnywhere()
        {
            Assert.True(ErrorReportUploader.HasUnmappedWorkerErrors(PartialSuccessBody));
            Assert.False(ErrorReportUploader.HasUnmappedWorkerErrors(
                "{\"results\":[{\"hash\":\"aaa\",\"status\":\"created\"}]}"));
        }

        [Fact]
        public void CollectUnsettledReports_ExcludesCreatedAndSkippedHashes()
        {
            var batch = new List<ErrorReport> { Report("aaa"), Report("bbb"), Report("ccc") };

            var unsettled = ErrorReportUploader.CollectUnsettledReports(PartialSuccessBody, batch);

            Assert.Single(unsettled);
            Assert.Equal("ccc", unsettled[0].Hash);
        }

        [Fact]
        public void CollectUnsettledReports_AllSettled_ReturnsEmpty()
        {
            var batch = new List<ErrorReport> { Report("aaa"), Report("bbb") };

            var unsettled = ErrorReportUploader.CollectUnsettledReports(PartialSuccessBody, batch);

            Assert.Empty(unsettled);
        }

        [Fact]
        public void IsReportSettledInResponse_ErrorStatusIsNotSettled()
        {
            var body = "{\"results\":[{\"hash\":\"aaa\",\"status\":\"error\",\"error\":\"boom\"}]}";

            Assert.False(ErrorReportUploader.IsReportSettledInResponse(body, "aaa"));
        }

        [Fact]
        public void IsReportSettledInResponse_HandlesSpacedJson()
        {
            var body = "{\"results\":[{\"hash\":\"aaa\", \"status\": \"created\"}]}";

            Assert.True(ErrorReportUploader.IsReportSettledInResponse(body, "aaa"));
        }

        [Fact]
        public void IsReportFailedInResponse_MissingHash_ReturnsFalse()
        {
            Assert.False(ErrorReportUploader.IsReportFailedInResponse(PartialSuccessBody, "zzz"));
            Assert.False(ErrorReportUploader.IsReportFailedInResponse(PartialSuccessBody, ""));
        }

        [Fact]
        public void IsReportFailedInResponse_DoesNotBleedIntoNeighborResult()
        {
            // Regression: a succeeded result within 256 chars of a failed neighbor
            // must not inherit the neighbor's error status.
            var body =
                "{\"results\":[{\"hash\":\"aaa\",\"status\":\"created\"},"
                + "{\"hash\":\"bbb\",\"status\":\"error\",\"error\":\"boom\"}]}";

            Assert.False(ErrorReportUploader.IsReportFailedInResponse(body, "aaa"));
            Assert.True(ErrorReportUploader.IsReportFailedInResponse(body, "bbb"));
        }
    }
}
