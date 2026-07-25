using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Connect.Presentation.Hubs;

// Temporary integration boundary for T001-02.
// The v2 methods and event orchestration are introduced by T001-07.
[Authorize]
public sealed class PlayerHub : Hub;
