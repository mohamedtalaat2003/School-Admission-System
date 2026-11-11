using System.Net;
using System.Net.Http.Json;
using SchoolAdmissionSystem.Api.Models.Dtos;
using Xunit;

namespace SchoolAdmissionSystem.IntegrationTests;

public class AdmissionLifecycleTests
{
    [Fact]
    public async Task CompleteAdmissionLifecycle_SucceedsEndToEnd()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        // 1. Admin creates class with 2 seats
        var createClassReq = new CreateClassRequest
        {
            Name = "Class 6 - Morning Shift",
            TotalSeats = 2
        };
        var createClassRes = await client.PostAsJsonAsync("/classes", createClassReq);
        Assert.Equal(HttpStatusCode.Created, createClassRes.StatusCode);
        var createdClass = await createClassRes.Content.ReadFromJsonAsync<ClassResponse>();
        Assert.NotNull(createdClass);
        var classId = createdClass.Id;

        // 2. GET /classes lists the new class with 2 available seats
        var listRes = await client.GetAsync("/classes");
        Assert.Equal(HttpStatusCode.OK, listRes.StatusCode);
        var classesList = await listRes.Content.ReadFromJsonAsync<List<ClassSeatStatusResponse>>();
        Assert.NotNull(classesList);
        var classStatus = classesList.Single(c => c.Id == classId);
        Assert.Equal(2, classStatus.TotalSeats);
        Assert.Equal(0, classStatus.HeldSeats);
        Assert.Equal(0, classStatus.ConfirmedSeats);
        Assert.Equal(2, classStatus.AvailableSeats);

        // 3. Student A holds a seat
        const string studentA = "20152691234567890";
        var holdReqA = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classId}/holds");
        holdReqA.Headers.Add("X-Student-Id", studentA);
        var holdResA = await client.SendAsync(holdReqA);
        Assert.Equal(HttpStatusCode.Created, holdResA.StatusCode);
        var holdA = await holdResA.Content.ReadFromJsonAsync<HoldResponse>();
        Assert.NotNull(holdA);
        Assert.Equal("HELD", holdA.Status);
        var holdIdA = holdA.HoldId;

        // 4. Verify seat status updated (held: 1, available: 1)
        var classDetailRes1 = await client.GetAsync($"/classes/{classId}");
        var classDetail1 = await classDetailRes1.Content.ReadFromJsonAsync<ClassSeatStatusResponse>();
        Assert.NotNull(classDetail1);
        Assert.Equal(1, classDetail1.HeldSeats);
        Assert.Equal(0, classDetail1.ConfirmedSeats);
        Assert.Equal(1, classDetail1.AvailableSeats);

        // 5. Student A confirms admission fee
        var confirmReqA = new HttpRequestMessage(HttpMethod.Post, $"/holds/{holdIdA}/confirm");
        confirmReqA.Headers.Add("X-Student-Id", studentA);
        var confirmResA = await client.SendAsync(confirmReqA);
        Assert.Equal(HttpStatusCode.OK, confirmResA.StatusCode);
        var confirmResultA = await confirmResA.Content.ReadFromJsonAsync<ConfirmHoldResponse>();
        Assert.NotNull(confirmResultA);
        Assert.Equal("CONFIRMED", confirmResultA.Status);

        // 6. Verify seat status updated (confirmed: 1, held: 0, available: 1)
        var classDetailRes2 = await client.GetAsync($"/classes/{classId}");
        var classDetail2 = await classDetailRes2.Content.ReadFromJsonAsync<ClassSeatStatusResponse>();
        Assert.NotNull(classDetail2);
        Assert.Equal(0, classDetail2.HeldSeats);
        Assert.Equal(1, classDetail2.ConfirmedSeats);
        Assert.Equal(1, classDetail2.AvailableSeats);

        // 7. Student B holds the second seat
        const string studentB = "20152691234567891";
        var holdReqB = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classId}/holds");
        holdReqB.Headers.Add("X-Student-Id", studentB);
        var holdResB = await client.SendAsync(holdReqB);
        Assert.Equal(HttpStatusCode.Created, holdResB.StatusCode);

        // 8. Verify seat status is now full (available: 0)
        var classDetailRes3 = await client.GetAsync($"/classes/{classId}");
        var classDetail3 = await classDetailRes3.Content.ReadFromJsonAsync<ClassSeatStatusResponse>();
        Assert.NotNull(classDetail3);
        Assert.Equal(1, classDetail3.HeldSeats);
        Assert.Equal(1, classDetail3.ConfirmedSeats);
        Assert.Equal(0, classDetail3.AvailableSeats);

        // 9. Student C attempts to hold a seat -> 409 Conflict
        const string studentC = "20152691234567892";
        var holdReqC = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classId}/holds");
        holdReqC.Headers.Add("X-Student-Id", studentC);
        var holdResC = await client.SendAsync(holdReqC);
        Assert.Equal(HttpStatusCode.Conflict, holdResC.StatusCode);

        // 10. Student A checks /me/holds
        var myHoldsReqA = new HttpRequestMessage(HttpMethod.Get, "/me/holds");
        myHoldsReqA.Headers.Add("X-Student-Id", studentA);
        var myHoldsResA = await client.SendAsync(myHoldsReqA);
        Assert.Equal(HttpStatusCode.OK, myHoldsResA.StatusCode);
        var myHoldsA = await myHoldsResA.Content.ReadFromJsonAsync<List<StudentHoldResponse>>();
        Assert.NotNull(myHoldsA);
        var studentAHold = myHoldsA.Single(h => h.HoldId == holdIdA);
        Assert.Equal("CONFIRMED", studentAHold.Status);
        Assert.Equal("Class 6 - Morning Shift", studentAHold.ClassName);
    }
}
