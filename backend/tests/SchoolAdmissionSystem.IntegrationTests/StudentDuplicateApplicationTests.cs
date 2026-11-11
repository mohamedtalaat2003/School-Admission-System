using System.Net;
using System.Net.Http.Json;
using SchoolAdmissionSystem.Api.Models.Dtos;
using Xunit;

namespace SchoolAdmissionSystem.IntegrationTests;

public class StudentDuplicateApplicationTests
{
    [Fact]
    public async Task StudentCannotHoldMultipleSeats_InSameClass_WhenActiveOrConfirmed()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        // 1. Create Class 1 with 5 seats
        var class1Res = await client.PostAsJsonAsync("/classes", new CreateClassRequest
        {
            Name = "Grade 1 - Section A",
            TotalSeats = 5
        });
        var class1 = await class1Res.Content.ReadFromJsonAsync<ClassResponse>();
        Assert.NotNull(class1);

        // 2. Student holds a seat in Class 1
        const string studentId = "20152691234567890";
        var holdReq1 = new HttpRequestMessage(HttpMethod.Post, $"/classes/{class1.Id}/holds");
        holdReq1.Headers.Add("X-Student-Id", studentId);
        var holdRes1 = await client.SendAsync(holdReq1);
        Assert.Equal(HttpStatusCode.Created, holdRes1.StatusCode);

        // 3. Same student attempts duplicate hold in Class 1 while active -> 409 Conflict
        var holdReqDuplicate = new HttpRequestMessage(HttpMethod.Post, $"/classes/{class1.Id}/holds");
        holdReqDuplicate.Headers.Add("X-Student-Id", studentId);
        var holdResDuplicate = await client.SendAsync(holdReqDuplicate);
        Assert.Equal(HttpStatusCode.Conflict, holdResDuplicate.StatusCode);

        // 4. Confirm the first hold
        var hold1 = await holdRes1.Content.ReadFromJsonAsync<HoldResponse>();
        Assert.NotNull(hold1);
        var confirmReq = new HttpRequestMessage(HttpMethod.Post, $"/holds/{hold1.HoldId}/confirm");
        confirmReq.Headers.Add("X-Student-Id", studentId);
        var confirmRes = await client.SendAsync(confirmReq);
        Assert.Equal(HttpStatusCode.OK, confirmRes.StatusCode);

        // 5. Same student attempts hold in Class 1 after confirmation -> 409 Conflict
        var holdReqAfterConfirm = new HttpRequestMessage(HttpMethod.Post, $"/classes/{class1.Id}/holds");
        holdReqAfterConfirm.Headers.Add("X-Student-Id", studentId);
        var holdResAfterConfirm = await client.SendAsync(holdReqAfterConfirm);
        Assert.Equal(HttpStatusCode.Conflict, holdResAfterConfirm.StatusCode);
    }

    [Fact]
    public async Task StudentCanApplyAgain_AfterCancellingPreviousHold()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        // 1. Create Class with 3 seats
        var classRes = await client.PostAsJsonAsync("/classes", new CreateClassRequest
        {
            Name = "Grade 2 - Section B",
            TotalSeats = 3
        });
        var createdClass = await classRes.Content.ReadFromJsonAsync<ClassResponse>();
        Assert.NotNull(createdClass);

        // 2. Student holds a seat
        const string studentId = "20152691234567895";
        var holdReq = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass.Id}/holds");
        holdReq.Headers.Add("X-Student-Id", studentId);
        var holdRes = await client.SendAsync(holdReq);
        Assert.Equal(HttpStatusCode.Created, holdRes.StatusCode);
        var hold = await holdRes.Content.ReadFromJsonAsync<HoldResponse>();
        Assert.NotNull(hold);

        // 3. Student cancels the hold
        var cancelReq = new HttpRequestMessage(HttpMethod.Delete, $"/holds/{hold.HoldId}");
        cancelReq.Headers.Add("X-Student-Id", studentId);
        var cancelRes = await client.SendAsync(cancelReq);
        Assert.Equal(HttpStatusCode.NoContent, cancelRes.StatusCode);

        // 4. Student can now re-apply to the same class
        var reapplyReq = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass.Id}/holds");
        reapplyReq.Headers.Add("X-Student-Id", studentId);
        var reapplyRes = await client.SendAsync(reapplyReq);
        Assert.Equal(HttpStatusCode.Created, reapplyRes.StatusCode);
    }
}
