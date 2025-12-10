using System;
using System.Threading.Tasks;
using LoadTesting.EmailAnalysisService;

namespace LoadTesting.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Configuration
            const string baseUrl = "https://localhost:7182"; // Change to your service URL
            
            Console.WriteLine("\n╔════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║   EMAIL ANALYSIS SERVICE - LOAD TESTING CONSOLE                      ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════════╝\n");

            var runner = new LoadTestRunner(baseUrl);

            // Test 1: Health Check
            Console.WriteLine("Test 1: Health Check Endpoint");
            Console.WriteLine("─".PadRight(70, '─'));
            var healthResult = await runner.TestHealthCheckAsync();
            PrintTestResult(healthResult);
            await Task.Delay(1000);

            // Test 2: Light Load (5 concurrent, 10 total)
            Console.WriteLine("\n\nTest 2: Light Load Test");
            Console.WriteLine("─".PadRight(70, '─'));
            var lightLoadReport = await runner.RunLoadTestAsync(
                concurrentRequests: 5,
                totalRequests: 10,
                delayBetweenRequests: 100,
                verbose: true);
            await Task.Delay(2000);

            // Test 3: Medium Load (10 concurrent, 50 total)
            Console.WriteLine("\n\nTest 3: Medium Load Test");
            Console.WriteLine("─".PadRight(70, '─'));
            var mediumLoadReport = await runner.RunLoadTestAsync(
                concurrentRequests: 10,
                totalRequests: 50,
                delayBetweenRequests: 50,
                verbose: false);
            await Task.Delay(2000);

            // Test 4: Heavy Load (20 concurrent, 100 total)
            Console.WriteLine("\n\nTest 4: Heavy Load Test");
            Console.WriteLine("─".PadRight(70, '─'));
            var heavyLoadReport = await runner.RunLoadTestAsync(
                concurrentRequests: 20,
                totalRequests: 100,
                delayBetweenRequests: 0,
                verbose: false);
            await Task.Delay(2000);

            // Test 5: Stress Test (50 concurrent, 200 total)
            Console.WriteLine("\n\nTest 5: Stress Test");
            Console.WriteLine("─".PadRight(70, '─'));
            var stressTestReport = await runner.RunLoadTestAsync(
                concurrentRequests: 50,
                totalRequests: 200,
                delayBetweenRequests: 0,
                verbose: false);

            // Summary
            PrintSummary(lightLoadReport, mediumLoadReport, heavyLoadReport, stressTestReport);

            Console.WriteLine("\n\n✓ All load tests completed!");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static void PrintTestResult(LoadTesting.EmailAnalysisService.TestResult result)
        {
            var statusIcon = result.Success ? "✓" : "✗";
            Console.WriteLine($"{statusIcon} Status: {result.StatusCode}");
            Console.WriteLine($"  Response Time: {result.ResponseTime}ms");
            Console.WriteLine($"  Response Size: {result.ResponseSize} bytes");
            if (!string.IsNullOrEmpty(result.Error))
            {
                Console.WriteLine($"  Error: {result.Error}");
            }
        }

        private static void PrintSummary(
            LoadTesting.EmailAnalysisService.LoadTestReport light,
            LoadTesting.EmailAnalysisService.LoadTestReport medium,
            LoadTesting.EmailAnalysisService.LoadTestReport heavy,
            LoadTesting.EmailAnalysisService.LoadTestReport stress)
        {
            Console.WriteLine("\n╔════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║   SUMMARY OF ALL TESTS                                               ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════════╝\n");

            PrintTestSummary("Light Load (5 concurrent, 10 total)", light);
            PrintTestSummary("Medium Load (10 concurrent, 50 total)", medium);
            PrintTestSummary("Heavy Load (20 concurrent, 100 total)", heavy);
            PrintTestSummary("Stress Test (50 concurrent, 200 total)", stress);
        }

        private static void PrintTestSummary(
            string testName,
            LoadTesting.EmailAnalysisService.LoadTestReport report)
        {
            Console.WriteLine($"\n📊 {testName}");
            Console.WriteLine($"   ├─ Success Rate: {report.SuccessRate:F2}%");
            Console.WriteLine($"   ├─ Avg Response: {report.AverageResponseTime:F2}ms");
            Console.WriteLine($"   ├─ Min/Max: {report.MinResponseTime:F2}ms / {report.MaxResponseTime:F2}ms");
            Console.WriteLine($"   ├─ RPS: {report.RequestsPerSecond:F2}");
            Console.WriteLine($"   └─ Duration: {report.TotalDuration.TotalSeconds:F2}s");
        }
    }
}
