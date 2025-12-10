using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace LoadTesting.EmailAnalysisService
{
    /// <summary>
    /// Load testing utility for Email Analysis Service
    /// Tests concurrent requests to batch email processing endpoint
    /// </summary>
    public class LoadTestRunner
    {
        private readonly string _baseUrl;
        private readonly HttpClient _httpClient;
        private readonly List<TestResult> _results = new();
        private readonly object _lockObj = new();

        public LoadTestRunner(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        }

        /// <summary>
        /// Run load test with specified parameters
        /// </summary>
        public async Task<LoadTestReport> RunLoadTestAsync(
            int concurrentRequests,
            int totalRequests,
            int delayBetweenRequests = 0,
            bool verbose = true)
        {
            var stopwatch = Stopwatch.StartNew();
            _results.Clear();

            Console.WriteLine($"\n{'='*80}");
            Console.WriteLine($"EMAIL ANALYSIS SERVICE - LOAD TEST");
            Console.WriteLine($"{'='*80}");
            Console.WriteLine($"Base URL: {_baseUrl}");
            Console.WriteLine($"Concurrent Requests: {concurrentRequests}");
            Console.WriteLine($"Total Requests: {totalRequests}");
            Console.WriteLine($"Delay Between Requests: {delayBetweenRequests}ms");
            Console.WriteLine($"Start Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff UTC}");
            Console.WriteLine($"{'='*80}\n");

            // Create request queue
            var requestTasks = new List<Task>();

            for (int i = 0; i < totalRequests; i++)
            {
                // Wait for semaphore slot if at concurrency limit
                while (requestTasks.Count(t => !t.IsCompleted) >= concurrentRequests)
                {
                    await Task.Delay(10);
                }

                int requestNumber = i + 1;
                var task = Task.Run(async () =>
                {
                    if (delayBetweenRequests > 0 && requestNumber > 1)
                    {
                        await Task.Delay(delayBetweenRequests);
                    }
                    await RunSingleRequestAsync(requestNumber);
                });

                requestTasks.Add(task);

                if (verbose && requestNumber % 10 == 0)
                {
                    Console.WriteLine($"[{requestNumber:D4}] Requests queued...");
                }
            }

            // Wait for all requests to complete
            await Task.WhenAll(requestTasks);
            stopwatch.Stop();

            // Generate report
            return GenerateReport(stopwatch.Elapsed);
        }

        /// <summary>
        /// Test health check endpoint
        /// </summary>
        public async Task<TestResult> TestHealthCheckAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new TestResult { TestName = "Health Check", RequestNumber = 1 };

            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/EmailAnalysis/health");
                stopwatch.Stop();

                result.StatusCode = (int)response.StatusCode;
                result.ResponseTime = stopwatch.ElapsedMilliseconds;
                result.Success = response.IsSuccessStatusCode;
                result.ResponseSize = response.Content.Headers.ContentLength ?? 0;

                if (!response.IsSuccessStatusCode)
                {
                    result.Error = $"HTTP {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.StatusCode = 0;
                result.ResponseTime = stopwatch.ElapsedMilliseconds;
                result.Success = false;
                result.Error = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Test file analysis endpoint
        /// </summary>
        public async Task<TestResult> TestFileAnalysisAsync(string filePath, int requestNumber)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new TestResult { TestName = "File Analysis", RequestNumber = requestNumber };

            try
            {
                var request = new
                {
                    filePath = filePath,
                    subject = $"Test Email {requestNumber}",
                    fromEmail = $"test{requestNumber}@example.com"
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_baseUrl}/api/EmailAnalysis/file/analyze",
                    content);

                stopwatch.Stop();

                result.StatusCode = (int)response.StatusCode;
                result.ResponseTime = stopwatch.ElapsedMilliseconds;
                result.Success = response.IsSuccessStatusCode;
                result.ResponseSize = response.Content.Headers.ContentLength ?? 0;

                if (!response.IsSuccessStatusCode)
                {
                    result.Error = $"HTTP {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.StatusCode = 0;
                result.ResponseTime = stopwatch.ElapsedMilliseconds;
                result.Success = false;
                result.Error = ex.Message;
            }

            lock (_lockObj)
            {
                _results.Add(result);
            }

            return result;
        }

        /// <summary>
        /// Test batch directory processing endpoint
        /// </summary>
        public async Task<TestResult> TestBatchDirectoryAsync(string directoryPath, int requestNumber)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new TestResult { TestName = "Batch Directory", RequestNumber = requestNumber };

            try
            {
                var request = new
                {
                    type = "directory",
                    source = directoryPath
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_baseUrl}/api/EmailAnalysis/emails/incoming",
                    content);

                stopwatch.Stop();

                result.StatusCode = (int)response.StatusCode;
                result.ResponseTime = stopwatch.ElapsedMilliseconds;
                result.Success = response.IsSuccessStatusCode;
                result.ResponseSize = response.Content.Headers.ContentLength ?? 0;

                if (!response.IsSuccessStatusCode)
                {
                    result.Error = $"HTTP {response.StatusCode}";
                }

                // Try to parse response for details
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(responseContent);
                        if (jsonDoc.RootElement.TryGetProperty("processingDurationSeconds", out var duration))
                        {
                            result.ProcessingDuration = duration.GetDouble();
                        }
                    }
                    catch { /* Ignore parsing errors */ }
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.StatusCode = 0;
                result.ResponseTime = stopwatch.ElapsedMilliseconds;
                result.Success = false;
                result.Error = ex.Message;
            }

            lock (_lockObj)
            {
                _results.Add(result);
            }

            return result;
        }

        private async Task RunSingleRequestAsync(int requestNumber)
        {
            // Default: test batch directory endpoint
            // You can modify this to test different endpoints
            await TestBatchDirectoryAsync("C:\\EmailFiles", requestNumber);
        }

        private LoadTestReport GenerateReport(TimeSpan totalDuration)
        {
            var successCount = _results.Count(r => r.Success);
            var failureCount = _results.Count(r => !r.Success);
            var avgResponseTime = _results.Count > 0 ? _results.Average(r => r.ResponseTime) : 0;
            var minResponseTime = _results.Count > 0 ? _results.Min(r => r.ResponseTime) : 0;
            var maxResponseTime = _results.Count > 0 ? _results.Max(r => r.ResponseTime) : 0;
            var totalDataTransferred = _results.Sum(r => r.ResponseSize);
            var successRate = _results.Count > 0 ? (double)successCount / _results.Count * 100 : 0;

            var report = new LoadTestReport
            {
                TotalRequests = _results.Count,
                SuccessfulRequests = successCount,
                FailedRequests = failureCount,
                SuccessRate = successRate,
                TotalDuration = totalDuration,
                AverageResponseTime = avgResponseTime,
                MinResponseTime = minResponseTime,
                MaxResponseTime = maxResponseTime,
                RequestsPerSecond = _results.Count / totalDuration.TotalSeconds,
                TotalDataTransferred = totalDataTransferred,
                EndTime = DateTime.UtcNow,
                Results = _results.ToList()
            };

            PrintReport(report);
            return report;
        }

        private void PrintReport(LoadTestReport report)
        {
            Console.WriteLine($"\n{'='*80}");
            Console.WriteLine($"LOAD TEST RESULTS");
            Console.WriteLine($"{'='*80}");
            Console.WriteLine($"Total Duration: {report.TotalDuration.TotalSeconds:F2} seconds");
            Console.WriteLine($"Total Requests: {report.TotalRequests}");
            Console.WriteLine($"Successful: {report.SuccessfulRequests} ({report.SuccessRate:F2}%)");
            Console.WriteLine($"Failed: {report.FailedRequests}");
            Console.WriteLine($"Requests/sec: {report.RequestsPerSecond:F2}");
            Console.WriteLine($"\nResponse Times (ms):");
            Console.WriteLine($"  Average: {report.AverageResponseTime:F2}");
            Console.WriteLine($"  Min: {report.MinResponseTime:F2}");
            Console.WriteLine($"  Max: {report.MaxResponseTime:F2}");
            Console.WriteLine($"\nData Transfer:");
            Console.WriteLine($"  Total: {FormatBytes(report.TotalDataTransferred)}");
            Console.WriteLine($"  Average per request: {FormatBytes((long)(report.TotalDataTransferred / report.TotalRequests))}");
            Console.WriteLine($"\nEnd Time: {report.EndTime:yyyy-MM-dd HH:mm:ss.fff UTC}");
            Console.WriteLine($"{'='*80}\n");
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:F2} {sizes[order]}";
        }
    }

    /// <summary>
    /// Results of a single test request
    /// </summary>
    public class TestResult
    {
        public string TestName { get; set; }
        public int RequestNumber { get; set; }
        public int StatusCode { get; set; }
        public long ResponseTime { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
        public long ResponseSize { get; set; }
        public double ProcessingDuration { get; set; }
    }

    /// <summary>
    /// Complete load test report
    /// </summary>
    public class LoadTestReport
    {
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public double SuccessRate { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public double AverageResponseTime { get; set; }
        public double MinResponseTime { get; set; }
        public double MaxResponseTime { get; set; }
        public double RequestsPerSecond { get; set; }
        public long TotalDataTransferred { get; set; }
        public DateTime EndTime { get; set; }
        public List<TestResult> Results { get; set; } = new();
    }
}
