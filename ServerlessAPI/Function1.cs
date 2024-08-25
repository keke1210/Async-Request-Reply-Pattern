using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace ServerlessAPI
{
    public class Function1
    {
        private static readonly ConcurrentDictionary<string, Status> _fakeDataStore = new();

        private readonly ILogger<Function1> _logger;
        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function("SubmitRequest")]
        public IActionResult SubmitRequest([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            string correlationId = Guid.NewGuid().ToString();

            // Simulate asynchronous/background processing
            _ = Task.Run(async () =>
            {
                _logger.LogInformation("Processing Started");
                _fakeDataStore.TryAdd(correlationId, Status.Processing);

                await Task.Delay(5 * 1000); // Simulate a long-running task

                _fakeDataStore[correlationId] = Status.Completed;
                _logger.LogInformation("Processing Finished");
            });

            req.HttpContext.Response.Headers.Append("Access-Control-Allow-Origin", "*");
            req.HttpContext.Response.Headers.Append("Access-Control-Expose-Headers", "Location, Retry-After");
            req.HttpContext.Response.Headers.Append("Retry-After", "5");
            return new AcceptedResult(location: $"http://localhost:7279/api/CheckResult/{correlationId}", value: null);
        }

        [Function("CheckResult")]
        public IActionResult CheckResult([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "CheckResult/{correlationId}")] HttpRequest req, string correlationId)
        {
            // Check the status of the request in the fake store
            if (!_fakeDataStore.TryGetValue(correlationId, out var status))
            {
                return new NotFoundObjectResult(new { Message = "Correlation ID not found" });
            }

            if (status == Status.Completed)
            {
                req.HttpContext.Response.Headers.Append("Location", $"http://localhost:7279/api/GetResult/{correlationId}");
                return new StatusCodeResult(StatusCodes.Status302Found);
            }

            return new OkResult();
        }

        [Function("GetResult")]
        public IActionResult GetResult([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetResult/{correlationId}")] HttpRequest req, string correlationId)
        {
            return new OkObjectResult($"Resource with correlation ID: {correlationId} has been successfully retrieved.");
        }

        enum Status
        {
            None,
            Processing,
            Completed
        }
    }
}
