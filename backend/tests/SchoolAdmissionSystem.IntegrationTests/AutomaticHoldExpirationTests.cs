using System.Net;
using System.Net.Http.Json;
using SchoolAdmissionSystem.Api.Models.Dtos;
using Xunit;

namespace SchoolAdmissionSystem.IntegrationTests;

public class AutomaticHoldExpirationTests
{
    [Fact]
    public async Task HoldAutomaticallyExpires_SeatBecomesAvailable_WithZeroApiCalls()
    {
        // 1. Configure short hold duration (2 seconds)
        await using var factory = new CustomWebApplicationFactory(holdDurationSeconds: 2);
        var client = factory.CreateClient();

        // 2. Create class with 1 seat
        var createClassRes = await client.PostAsJsonAsync("/classes", new CreateClassRequest
        {
            Name = "Grade 3 - Section A",
            TotalSeats = 1
        });
        var createdClass = await createClassRes.Content.ReadFromJsonAsync<ClassResponse>();
        Assert.NotNull(createdClass);
        var classId = createdClass.Id;

        // 3. Student A holds the only seat
        const string studentA = "20152691234567801";
        var holdReqA = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classId}/holds");
        holdReqA.Headers.Add("X-Student-Id", studentA);
        var holdResA = await client.SendAsync(holdReqA);
        Assert.Equal(HttpStatusCode.Created, holdResA.StatusCode);
        var holdA = await holdResA.Content.ReadFromJsonAsync<HoldResponse>();
        Assert.NotNull(holdA);

        // 4. Verify class is full (0 available)
        var classRes1 = await client.GetAsync($"/classes/{classId}");
        var status1 = await classRes1.Content.ReadFromJsonAsync<ClassSeatStatusResponse>();
        Assert.NotNull(status1);
        Assert.Equal(0, status1.AvailableSeats);
        Assert.Equal(1, status1.HeldSeats);

        // 5. Wait 2.5 seconds (past the 2-second hold duration)
        await Task.Delay(2500);

        // 6. Zero API call verification: query GET /classes/{id} without any prior release or cleanup call
        var classRes2 = await client.GetAsync($"/classes/{classId}");
        var status2 = await classRes2.Content.ReadFromJsonAsync<ClassSeatStatusResponse>();
        Assert.NotNull(status2);
        Assert.Equal(1, status2.AvailableSeats);
        Assert.Equal(0, status2.HeldSeats);

        // 7. Student A attempts to pay/confirm -> 400 Bad Request (expired)
        var confirmReq = new HttpRequestMessage(HttpMethod.Post, $"/holds/{holdA.HoldId}/confirm");
        confirmReq.Headers.Add("X-Student-Id", studentA);
        var confirmRes = await client.SendAsync(confirmReq);
        Assert.Equal(HttpStatusCode.BadRequest, confirmRes.StatusCode);

        // 8. Student B can immediately claim the released seat
        const string studentB = "20152691234567802";
        var holdReqB = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classId}/holds");
        holdReqB.Headers.Add("X-Student-Id", studentB);
        var holdResB = await client.SendAsync(holdReqB);
        Assert.Equal(HttpStatusCode.Created, holdResB.StatusCode);
    }
}
