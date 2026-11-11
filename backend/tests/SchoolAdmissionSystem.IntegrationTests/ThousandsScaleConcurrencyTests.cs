using System.Net;
using System.Net.Http.Json;
using SchoolAdmissionSystem.Api.Models.Dtos;
using Xunit;

namespace SchoolAdmissionSystem.IntegrationTests;

public class ThousandsScaleConcurrencyTests
{
    [Fact]
    public async Task ThousandConcurrentHoldRequests_NeverOversell_StrictlyCapAtCapacity()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        const int totalSeats = 25;
        const int concurrentRequests = 1000;

        // 1. Create class with exactly 25 seats
        var createRes = await client.PostAsJsonAsync("/classes", new CreateClassRequest
        {
            Name = "Grade 1 - Concurrency Stress Test",
            TotalSeats = totalSeats
        });
        var createdClass = await createRes.Content.ReadFromJsonAsync<ClassResponse>();
        Assert.NotNull(createdClass);
        var classId = createdClass.Id;

        // 2. Prepare 1,000 simultaneous requests synchronized with TaskCompletionSource
        var startSignal = new TaskCompletionSource();
        var tasks = Enumerable.Range(1, concurrentRequests).Select(async i =>
        {
            await startSignal.Task;
            var req = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classId}/holds");
            req.Headers.Add("X-Student-Id", $"2015269{i:D10}");
            return await client.SendAsync(req);
        }).ToArray();

        // 3. Release barrier to fire all 1,000 requests in parallel
        startSignal.SetResult();
        var responses = await Task.WhenAll(tasks);

        // 4. Invariant assertions: No overselling under any circumstances
        var createdCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        Assert.Equal(totalSeats, createdCount);
        Assert.Equal(concurrentRequests - totalSeats, conflictCount);
        Assert.All(responses, r =>
            Assert.True(r.StatusCode == HttpStatusCode.Created || r.StatusCode == HttpStatusCode.Conflict,
                $"Unexpected status code: {r.StatusCode}"));

        // 5. Verify final seat balance from the API
        var classRes = await client.GetAsync($"/classes/{classId}");
        var classStatus = await classRes.Content.ReadFromJsonAsync<ClassSeatStatusResponse>();
        Assert.NotNull(classStatus);
        Assert.Equal(totalSeats, classStatus.TotalSeats);
        Assert.Equal(totalSeats, classStatus.HeldSeats);
        Assert.Equal(0, classStatus.AvailableSeats);
        Assert.Equal(0, classStatus.ConfirmedSeats);
    }
}
