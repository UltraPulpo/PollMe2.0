using Microsoft.AspNetCore.SignalR;

namespace PollApp.Api.Hubs;

// A SignalR Hub — clients connect via WebSocket and join groups by poll ID.
// This hub only handles group management. Actual broadcasting happens from
// the controller using IHubContext<PollHub> (you don't need to be "inside"
// the hub to send messages — IHubContext lets you send from anywhere).
public class PollHub : Hub
{
    // Called by the client when it opens the results page.
    // Adds this connection to a group named by the poll ID.
    public async Task JoinPoll(string pollId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, pollId);

    // Called by the client when it leaves the results page.
    // (Also happens automatically if the client disconnects.)
    public async Task LeavePoll(string pollId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, pollId);
}
