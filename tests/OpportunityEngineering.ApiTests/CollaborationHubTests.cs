using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using OpportunityEngineering.Api.Hubs;
using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Application.Services;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.ApiTests;

// Exercises CollaborationHub's own logic directly: the participant-claim-driven group
// resolution and the cast-vote-then-broadcast path, without going through a live SignalR
// transport. A real over-the-wire test would require Azure SignalR Service connectivity
// (the Default-mode hub dispatcher opens an outbound connection using DefaultAzureCredential
// at negotiate time, which has no local/CI equivalent), the same class of dependency Cosmos
// and Service Bus already aren't exercised live for in this suite.
[TestClass]
public sealed class CollaborationHubTests
{
    [TestMethod]
    public async Task JoinSessionForAParticipantConnectionJoinsTheGroupFromTheTokenClaimNotTheArgument()
    {
        var hub = CreateHub(out _, out var groups);
        hub.Context = new FakeHubCallerContext(CreateParticipantPrincipal("session-1", "participant-1"));

        await hub.JoinSession("some-other-session-id-the-client-tried-to-pass");

        Assert.AreEqual(("connection-1", "session:session-1"), groups.Added.Single());
    }

    [TestMethod]
    public async Task JoinSessionForAFacilitatorConnectionUsesTheRequestedSessionId()
    {
        var hub = CreateHub(out _, out var groups);
        hub.Context = new FakeHubCallerContext(new ClaimsPrincipal(new ClaimsIdentity([new Claim("oid", "facilitator-1")], "test")));

        await hub.JoinSession("session-1");

        Assert.AreEqual(("connection-1", "session:session-1"), groups.Added.Single());
    }

    [TestMethod]
    public async Task JoinSessionForAFacilitatorConnectionIgnoresTheTokensUnrelatedEntraSidClaim()
    {
        // Azure AD access tokens carry their own standard "sid" claim (the AAD session id),
        // which has nothing to do with our live-vote session. A facilitator must still land
        // in the group they actually asked to watch, not one keyed by that unrelated claim.
        var hub = CreateHub(out _, out var groups);
        hub.Context = new FakeHubCallerContext(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("oid", "facilitator-1"), new Claim("sid", "aad-session-id-unrelated-to-live-vote")],
            "test")));

        await hub.JoinSession("session-1");

        Assert.AreEqual(("connection-1", "session:session-1"), groups.Added.Single());
    }

    [TestMethod]
    public async Task JoinSessionForAParticipantConnectionBroadcastsAnIncrementedPresenceCount()
    {
        var hub = CreateHub(out var clients, out _);
        hub.Context = new FakeHubCallerContext(CreateParticipantPrincipal("session-1", "participant-1"));

        await hub.JoinSession("session-1");

        Assert.AreEqual(1, clients.LastGroupClient.LastPresenceCount);
    }

    [TestMethod]
    public async Task JoinSessionForAFacilitatorConnectionDoesNotCountTowardPresence()
    {
        var hub = CreateHub(out var clients, out _);
        hub.Context = new FakeHubCallerContext(new ClaimsPrincipal(new ClaimsIdentity([new Claim("oid", "facilitator-1")], "test")));

        await hub.JoinSession("session-1");

        Assert.IsNull(clients.LastGroupClient.LastPresenceCount);
    }

    [TestMethod]
    public async Task DisconnectingAJoinedParticipantBroadcastsADecrementedPresenceCount()
    {
        var hub = CreateHub(out var clients, out _);
        hub.Context = new FakeHubCallerContext(CreateParticipantPrincipal("session-1", "participant-1"));
        await hub.JoinSession("session-1");

        await hub.OnDisconnectedAsync(null);

        Assert.AreEqual(0, clients.LastGroupClient.LastPresenceCount);
    }

    [TestMethod]
    public async Task CastVoteResolvesTheParticipantFromClaimsAndBroadcastsTheTallyToTheirSessionGroup()
    {
        var hub = CreateHub(out var clients, out _);
        hub.Context = new FakeHubCallerContext(CreateParticipantPrincipal("session-1", "participant-1"));

        await hub.CastVote("card-1", "step-1");

        Assert.AreEqual("session:session-1", clients.LastGroupName);
        var tally = clients.LastGroupClient.LastTally.Single();
        Assert.AreEqual("card-1", tally.DiscoveryCardId);
        Assert.AreEqual("step-1", tally.JourneyStepId);
        Assert.AreEqual(1, tally.Count);
    }

    [TestMethod]
    public async Task CastVoteRejectsAConnectionAuthenticatedWithoutParticipantClaims()
    {
        var hub = CreateHub(out _, out _);
        hub.Context = new FakeHubCallerContext(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim("oid", "facilitator-1")], "test")));

        // A facilitator's own Entra token is accepted for the hub CONNECTION (so they can
        // watch the tally), but it carries no participant claims. CastVote must reject it
        // rather than silently resolving a bogus participant.
        await Assert.ThrowsExactlyAsync<DomainException>(() => hub.CastVote("card-1", "step-1"));
    }

    [TestMethod]
    public async Task SubmitIdeaResolvesTheParticipantFromClaimsAndBroadcastsTheBoardToTheirSessionGroup()
    {
        var hub = CreateHub(out var clients, out _);
        hub.Context = new FakeHubCallerContext(CreateParticipantPrincipal("session-1", "participant-1"));

        await hub.SubmitIdea("Skip the re-keying step entirely.");

        Assert.AreEqual("session:session-1", clients.LastGroupName);
        var note = clients.LastGroupClient.LastIdeationNotes.Single();
        Assert.AreEqual("Skip the re-keying step entirely.", note.Text);
        Assert.AreEqual("participant-1", note.ParticipantId);
    }

    [TestMethod]
    public async Task TogglePinResolvesTheParticipantAndReturnsPinnedStateAndBroadcastsTheTally()
    {
        var hub = CreateHub(out var clients, out _);
        hub.Context = new FakeHubCallerContext(CreateParticipantPrincipal("session-1", "participant-1"));

        var pinned = await hub.TogglePin("card-1", "step-1");

        Assert.IsTrue(pinned.Pinned);
        Assert.AreEqual(1, pinned.Tally.Single().Count);
        Assert.AreEqual("session:session-1", clients.LastGroupName);
        Assert.AreEqual(1, clients.LastGroupClient.LastPinTally.Single().Count);
    }

    [TestMethod]
    public async Task TogglingTheSamePinTwiceUnpinsIt()
    {
        var hub = CreateHub(out _, out _);
        hub.Context = new FakeHubCallerContext(CreateParticipantPrincipal("session-1", "participant-1"));
        await hub.TogglePin("card-1", "step-1");

        var result = await hub.TogglePin("card-1", "step-1");

        Assert.IsFalse(result.Pinned);
        Assert.HasCount(0, result.Tally);
    }

    [TestMethod]
    public async Task JoinSessionSendsTheAlreadyPlacedBoardToANewlyJoiningParticipant()
    {
        // A participant who joins after cards are already on the board must see them
        // immediately, otherwise the shared mural looks empty until someone else moves or
        // places a card, even though the board already has content.
        var hub = CreateHub(out var clients, out _);
        hub.Context = new FakeHubCallerContext(CreateParticipantPrincipal("session-1", "participant-1"));
        await hub.PlaceBoardCard("card-1", 0.2, 0.2, "Already here.");
        hub.Context = new FakeHubCallerContext(CreateParticipantPrincipal("session-1", "participant-2"));

        await hub.JoinSession("session-1");

        Assert.HasCount(1, clients.LastGroupClient.LastBoard);
        Assert.AreEqual("card-1", clients.LastGroupClient.LastBoard[0].DiscoveryCardId);
    }

    [TestMethod]
    public async Task JoinSessionSendsTheAlreadyPlacedBoardToAFacilitatorConnection()
    {
        var hub = CreateHub(out var clients, out _);
        hub.Context = new FakeHubCallerContext(CreateParticipantPrincipal("session-1", "participant-1"));
        await hub.PlaceBoardCard("card-1", 0.2, 0.2, "Already here.");
        hub.Context = new FakeHubCallerContext(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim("oid", "facilitator-1")], "test")));

        await hub.JoinSession("session-1", "workspace-1", "engagement-1");

        Assert.HasCount(1, clients.LastGroupClient.LastBoard);
        Assert.AreEqual("card-1", clients.LastGroupClient.LastBoard[0].DiscoveryCardId);
    }

    [TestMethod]
    public async Task PlaceBoardCardResolvesTheParticipantAndBroadcastsTheBoardToTheirSessionGroup()
    {
        var hub = CreateHub(out var clients, out _);
        hub.Context = new FakeHubCallerContext(CreateParticipantPrincipal("session-1", "participant-1"));

        var board = await hub.PlaceBoardCard("card-1", 0.2, 0.2, "Fits our persona's workflow.");

        Assert.HasCount(1, board);
        Assert.AreEqual("session:session-1", clients.LastGroupName);
        Assert.AreEqual(1, clients.LastGroupClient.LastBoard.Count);
        Assert.AreEqual("card-1", clients.LastGroupClient.LastBoard[0].DiscoveryCardId);
    }

    [TestMethod]
    public async Task PlacingTheSameCardTwiceCreatesTwoDistinctPlacements()
    {
        var hub = CreateHub(out _, out _);
        hub.Context = new FakeHubCallerContext(CreateParticipantPrincipal("session-1", "participant-1"));
        await hub.PlaceBoardCard("card-1", 0.2, 0.2, "First take.");

        var board = await hub.PlaceBoardCard("card-1", 0.3, 0.3, "Second take.");

        Assert.HasCount(2, board);
    }

    [TestMethod]
    public async Task MoveBoardCardByADifferentParticipantThanThePlacerSucceedsAndBroadcasts()
    {
        var hub = CreateHub(out var clients, out _);
        hub.Context = new FakeHubCallerContext(CreateParticipantPrincipal("session-1", "participant-1"));
        var placed = await hub.PlaceBoardCard("card-1", 0.2, 0.2, "Fits our persona.");
        hub.Context = new FakeHubCallerContext(CreateParticipantPrincipal("session-1", "participant-2"));

        var board = await hub.MoveBoardCard(placed[0].Id, 0.8, 0.6);

        Assert.AreEqual(0.8, board.Single().X);
        Assert.AreEqual(0.6, board.Single().Y);
        Assert.AreEqual("participant-1", board.Single().PlacedByParticipantId);
        Assert.AreEqual("session:session-1", clients.LastGroupName);
        Assert.AreEqual(0.8, clients.LastGroupClient.LastBoard.Single().X);
    }

    [TestMethod]
    public async Task PlaceBoardCardWorksFromAFacilitatorsOwnConnectionAfterJoinSessionRecordsItsContext()
    {
        // A facilitator's own DiscoveryCardsView tab connects with an Entra token, not a
        // Participant token. It must still be able to place cards on the shared board, the
        // whole point of "everyone can drag cards anywhere on the canvas" including the
        // facilitator.
        var hub = CreateHub(out var clients, out _);
        hub.Context = new FakeHubCallerContext(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim("oid", "facilitator-1")], "test")));
        await hub.JoinSession("session-1", "workspace-1", "engagement-1");

        var board = await hub.PlaceBoardCard("card-1", 0.2, 0.2, "Facilitator's own take.");

        Assert.HasCount(1, board);
        Assert.AreEqual("facilitator-1", board[0].PlacedByParticipantId);
        Assert.AreEqual("session:session-1", clients.LastGroupName);
    }

    [TestMethod]
    public async Task PlaceBoardCardStillRejectsAFacilitatorConnectionThatNeverJoinedASession()
    {
        var hub = CreateHub(out _, out _);
        hub.Context = new FakeHubCallerContext(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim("oid", "facilitator-1")], "test")));

        await Assert.ThrowsExactlyAsync<DomainException>(
            () => hub.PlaceBoardCard("card-1", 0.2, 0.2, "No JoinSession call first."));
    }

    [TestMethod]
    public async Task RemoveBoardCardBroadcastsTheUpdatedBoardAndReturnsIt()
    {
        var hub = CreateHub(out var clients, out _);
        hub.Context = new FakeHubCallerContext(CreateParticipantPrincipal("session-1", "participant-1"));
        var placed = await hub.PlaceBoardCard("card-1", 0.2, 0.2, "Fits our persona.");

        var board = await hub.RemoveBoardCard(placed[0].Id);

        Assert.HasCount(0, board);
        Assert.AreEqual("session:session-1", clients.LastGroupName);
        Assert.HasCount(0, clients.LastGroupClient.LastBoard);
    }

    [TestMethod]
    public async Task EditBoardCardUpdatesTheRationaleAndBroadcasts()
    {
        var hub = CreateHub(out var clients, out _);
        hub.Context = new FakeHubCallerContext(CreateParticipantPrincipal("session-1", "participant-1"));
        var placed = await hub.PlaceBoardCard("card-1", 0.2, 0.2, "First take.");

        var board = await hub.EditBoardCard(placed[0].Id, "Revised take.");

        Assert.AreEqual("Revised take.", board.Single().Rationale);
        Assert.AreEqual("session:session-1", clients.LastGroupName);
        Assert.AreEqual("Revised take.", clients.LastGroupClient.LastBoard.Single().Rationale);
    }

    [TestMethod]
    public async Task PlaceBoardCardWhilePrivateReturnsOnlyTheCallersOwnCardsAndPushesEverythingToFacilitators()
    {
        var hub = CreateHub(out var clients, out _, out var sessions);
        await sessions.CreateAsync(PrivateSession(), CancellationToken.None);
        hub.Context = new FakeHubCallerContext(CreateParticipantPrincipal("session-1", "participant-a"));
        await hub.PlaceBoardCard("card-1", 0.2, 0.2, "Participant A's take.");
        hub.Context = new FakeHubCallerContext(CreateParticipantPrincipal("session-1", "participant-b"));

        var board = await hub.PlaceBoardCard("card-2", 0.4, 0.4, "Participant B's take.");

        Assert.HasCount(1, board);
        Assert.AreEqual("card-2", board.Single().DiscoveryCardId);
        Assert.AreEqual("session:session-1:facilitators", clients.LastGroupExceptName);
        Assert.HasCount(2, clients.LastGroupExceptClient.LastBoard);
    }

    [TestMethod]
    public async Task JoinSessionSnapshotIsFilteredForAParticipantWhileTheBoardIsPrivate()
    {
        var hub = CreateHub(out var clients, out _, out var sessions);
        await sessions.CreateAsync(PrivateSession(), CancellationToken.None);
        hub.Context = new FakeHubCallerContext(CreateParticipantPrincipal("session-1", "participant-a"));
        await hub.PlaceBoardCard("card-1", 0.2, 0.2, "Participant A's take.");
        hub.Context = new FakeHubCallerContext(CreateParticipantPrincipal("session-1", "participant-b"));

        await hub.JoinSession("session-1");

        Assert.HasCount(0, clients.LastGroupClient.LastBoard);
        Assert.IsFalse(clients.LastGroupClient.LastBoardRevealed);
    }

    [TestMethod]
    public async Task JoinSessionSnapshotShowsAFacilitatorEverythingEvenWhileTheBoardIsPrivate()
    {
        var hub = CreateHub(out var clients, out _, out var sessions);
        await sessions.CreateAsync(PrivateSession(), CancellationToken.None);
        hub.Context = new FakeHubCallerContext(CreateParticipantPrincipal("session-1", "participant-a"));
        await hub.PlaceBoardCard("card-1", 0.2, 0.2, "Participant A's take.");
        hub.Context = new FakeHubCallerContext(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim("oid", "facilitator-1")], "test")));

        await hub.JoinSession("session-1", "workspace-1", "engagement-1");

        Assert.HasCount(1, clients.LastGroupClient.LastBoard);
    }

    [TestMethod]
    public async Task MoveCursorBroadcastsToOthersInTheSessionGroupOnly()
    {
        var hub = CreateHub(out var clients, out _);
        hub.Context = new FakeHubCallerContext(CreateParticipantPrincipal("session-1", "participant-1"));

        await hub.MoveCursor(0.4, 0.6);

        Assert.AreEqual("session:session-1", clients.LastOthersInGroupName);
        Assert.AreEqual("participant-1", clients.LastOthersInGroupClient.LastCursorParticipantId);
        Assert.AreEqual(0.4, clients.LastOthersInGroupClient.LastCursorX);
        Assert.AreEqual(0.6, clients.LastOthersInGroupClient.LastCursorY);
    }

    private static LiveSession PrivateSession() => new(
        "session-1", "workspace-1", "engagement-1", "step-1", "ABC123", "facilitator-1",
        DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(4), "active", BoardRevealed: false);

    private static CollaborationHub CreateHub(
        out FakeHubCallerClients clients,
        out FakeGroupManager groups) =>
        CreateHub(out clients, out groups, out _);

    private static CollaborationHub CreateHub(
        out FakeHubCallerClients clients,
        out FakeGroupManager groups,
        out InMemoryLiveSessionStore sessions)
    {
        var voteStore = new InMemoryLiveVoteStore();
        var votes = new LiveVoteService(voteStore, new GuidIdentifierFactory(), TimeProvider.System);
        var ideation = new LiveIdeationService(
            new InMemoryLiveIdeationNoteStore(), new GuidIdentifierFactory(), TimeProvider.System);
        var pins = new LivePinService(
            new InMemoryLivePinStore(), new GuidIdentifierFactory(), TimeProvider.System);
        var board = new LiveBoardService(
            new InMemoryLiveBoardCardStore(), new GuidIdentifierFactory(), TimeProvider.System);
        clients = new FakeHubCallerClients();
        groups = new FakeGroupManager();
        sessions = new InMemoryLiveSessionStore();
        return new CollaborationHub(
            votes,
            ideation,
            pins,
            board,
            new OpportunityEngineering.Api.Authorization.ParticipantContextResolver(),
            new LivePresenceTracker(),
            new FacilitatorConnectionTracker(),
            sessions)
        {
            Clients = clients,
            Groups = groups,
        };
    }

    private static ClaimsPrincipal CreateParticipantPrincipal(string joinSessionId, string participantId) =>
        new(new ClaimsIdentity(
            [
                new Claim("sub", participantId),
                new Claim("wid", "workspace-1"),
                new Claim("eid", "engagement-1"),
                new Claim("sid", joinSessionId),
                new Claim("name", "Riley"),
                new Claim("scope", "participant"),
            ],
            "test"));
}

internal sealed class GuidIdentifierFactory : IIdentifierFactory
{
    public string Create() => Guid.NewGuid().ToString();
}

internal sealed class FakeHubCallerContext(ClaimsPrincipal user) : HubCallerContext
{
    public override string ConnectionId { get; } = "connection-1";
    public override string? UserIdentifier => null;
    public override ClaimsPrincipal? User { get; } = user;
    public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
    public override IFeatureCollection Features { get; } = new FeatureCollection();
    public override CancellationToken ConnectionAborted => CancellationToken.None;
    public override void Abort()
    {
    }
}

internal sealed class FakeCollaborationClient : ICollaborationClient
{
    public IReadOnlyList<CardVoteTally> LastTally { get; private set; } = [];
    public IReadOnlyList<string> LastShortlistedDiscoveryCardIds { get; private set; } = [];
    public int SessionClosedCallCount { get; private set; }

    public Task VoteTallyUpdated(IReadOnlyList<CardVoteTally> tally)
    {
        LastTally = tally;
        return Task.CompletedTask;
    }

    public Task ShortlistUpdated(IReadOnlyList<string> discoveryCardIds)
    {
        LastShortlistedDiscoveryCardIds = discoveryCardIds;
        return Task.CompletedTask;
    }

    public Task SessionClosed()
    {
        SessionClosedCallCount++;
        return Task.CompletedTask;
    }

    public int? LastPresenceCount { get; private set; }

    public Task PresenceUpdated(int participantCount)
    {
        LastPresenceCount = participantCount;
        return Task.CompletedTask;
    }

    public IReadOnlyList<LiveIdeationNote> LastIdeationNotes { get; private set; } = [];

    public Task IdeationBoardUpdated(IReadOnlyList<LiveIdeationNote> notes)
    {
        LastIdeationNotes = notes;
        return Task.CompletedTask;
    }

    public IReadOnlyList<CardPinTally> LastPinTally { get; private set; } = [];

    public Task PinTallyUpdated(IReadOnlyList<CardPinTally> tally)
    {
        LastPinTally = tally;
        return Task.CompletedTask;
    }

    public IReadOnlyList<LiveBoardCard> LastBoard { get; private set; } = [];
    public bool? LastBoardRevealed { get; private set; }

    public Task BoardUpdated(IReadOnlyList<LiveBoardCard> cards, bool revealed)
    {
        LastBoard = cards;
        LastBoardRevealed = revealed;
        return Task.CompletedTask;
    }

    public string? LastCursorParticipantId { get; private set; }
    public double LastCursorX { get; private set; }
    public double LastCursorY { get; private set; }

    public Task CursorMoved(string participantId, string displayName, double x, double y)
    {
        LastCursorParticipantId = participantId;
        LastCursorX = x;
        LastCursorY = y;
        return Task.CompletedTask;
    }
}

internal sealed class FakeHubCallerClients : IHubCallerClients<ICollaborationClient>
{
    public string? LastGroupName { get; private set; }
    public FakeCollaborationClient LastGroupClient { get; } = new();

    public ICollaborationClient Caller => LastGroupClient;
    public ICollaborationClient Others => LastGroupClient;
    public ICollaborationClient All => LastGroupClient;

    public ICollaborationClient AllExcept(IReadOnlyList<string> excludedConnectionIds) => LastGroupClient;
    public ICollaborationClient Client(string connectionId) => LastGroupClient;
    public ICollaborationClient Clients(IReadOnlyList<string> connectionIds) => LastGroupClient;

    public ICollaborationClient Group(string groupName)
    {
        LastGroupName = groupName;
        return LastGroupClient;
    }

    // Separate tracked instances from LastGroupClient (rather than aliasing it, like Caller/Group
    // above): no test predating the private-mode mural exercised these two targets, so keeping
    // them distinct lets a test assert what a facilitator group received without it being
    // clobbered by a same-call caller-targeted push, or vice versa.
    public string? LastGroupExceptName { get; private set; }
    public FakeCollaborationClient LastGroupExceptClient { get; } = new();
    public string? LastOthersInGroupName { get; private set; }
    public FakeCollaborationClient LastOthersInGroupClient { get; } = new();

    public ICollaborationClient GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds)
    {
        LastGroupExceptName = groupName;
        return LastGroupExceptClient;
    }

    public ICollaborationClient Groups(IReadOnlyList<string> groupNames) => LastGroupClient;

    public ICollaborationClient OthersInGroup(string groupName)
    {
        LastOthersInGroupName = groupName;
        return LastOthersInGroupClient;
    }

    public ICollaborationClient User(string userId) => LastGroupClient;
    public ICollaborationClient Users(IReadOnlyList<string> userIds) => LastGroupClient;
}

internal sealed class FakeGroupManager : IGroupManager
{
    public List<(string ConnectionId, string GroupName)> Added { get; } = [];

    public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        Added.Add((connectionId, groupName));
        return Task.CompletedTask;
    }

    public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

internal sealed class InMemoryLiveVoteStore : ILiveVoteStore
{
    private readonly Dictionary<(string, string, string, string), LiveVote> votes = [];

    public Task CastAsync(LiveVote vote, CancellationToken cancellationToken)
    {
        votes[(vote.JoinSessionId, vote.ParticipantId, vote.DiscoveryCardId, vote.JourneyStepId)] = vote;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<LiveVote>> QueryTallyAsync(
        string workspaceId,
        string joinSessionId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<LiveVote>>(
            [.. votes.Values.Where(vote => vote.WorkspaceId == workspaceId && vote.JoinSessionId == joinSessionId)]);
}

internal sealed class InMemoryLiveIdeationNoteStore : ILiveIdeationNoteStore
{
    private readonly List<LiveIdeationNote> notes = [];

    public Task<LiveIdeationNote> SubmitAsync(LiveIdeationNote note, CancellationToken cancellationToken)
    {
        notes.Add(note);
        return Task.FromResult(note);
    }

    public Task<IReadOnlyList<LiveIdeationNote>> QueryBySessionAsync(
        string workspaceId, string joinSessionId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<LiveIdeationNote>>(
            [.. notes.Where(note => note.WorkspaceId == workspaceId && note.JoinSessionId == joinSessionId)]);

    public Task<LiveIdeationNote?> GetAsync(
        string workspaceId, string joinSessionId, string noteId, CancellationToken cancellationToken) =>
        Task.FromResult(notes.SingleOrDefault(
            note => note.WorkspaceId == workspaceId && note.JoinSessionId == joinSessionId && note.Id == noteId));
}

internal sealed class InMemoryLivePinStore : ILivePinStore
{
    private readonly Dictionary<(string, string, string, string), LivePin> pins = [];

    public Task<bool> ToggleAsync(LivePin pin, CancellationToken cancellationToken)
    {
        var key = (pin.JoinSessionId, pin.ParticipantId, pin.DiscoveryCardId, pin.JourneyStepId);
        if (pins.Remove(key))
        {
            return Task.FromResult(false);
        }

        pins[key] = pin;
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<LivePin>> QueryTallyAsync(
        string workspaceId, string joinSessionId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<LivePin>>(
            [.. pins.Values.Where(pin => pin.WorkspaceId == workspaceId && pin.JoinSessionId == joinSessionId)]);
}

internal sealed class InMemoryLiveBoardCardStore : ILiveBoardCardStore
{
    private readonly Dictionary<string, LiveBoardCard> cards = [];

    public Task<LiveBoardCard> PlaceAsync(LiveBoardCard card, CancellationToken cancellationToken)
    {
        cards[card.Id] = card;
        return Task.FromResult(card);
    }

    public Task<LiveBoardCard> MoveAsync(LiveBoardCard card, CancellationToken cancellationToken)
    {
        cards[card.Id] = card;
        return Task.FromResult(card);
    }

    public Task RemoveAsync(string workspaceId, string placementId, CancellationToken cancellationToken)
    {
        cards.Remove(placementId);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<LiveBoardCard>> QueryBySessionAsync(
        string workspaceId, string joinSessionId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<LiveBoardCard>>(
            [.. cards.Values.Where(card => card.WorkspaceId == workspaceId && card.JoinSessionId == joinSessionId)]);
}

/// <summary>Absent from this store, a session's board is treated as revealed. See
/// CollaborationHub.IsBoardRevealedAsync, so tests that never seed a session here keep
/// exercising the always-public default behavior unchanged.</summary>
internal sealed class InMemoryLiveSessionStore : ILiveSessionStore
{
    private readonly Dictionary<string, LiveSession> sessions = [];

    public Task<LiveSession> CreateAsync(LiveSession session, CancellationToken cancellationToken)
    {
        sessions[session.Id] = session;
        return Task.FromResult(session);
    }

    public Task<LiveSession> UpdateStatusAsync(LiveSession session, CancellationToken cancellationToken)
    {
        sessions[session.Id] = session;
        return Task.FromResult(session);
    }

    public Task<LiveSession?> GetByJoinCodeAsync(string joinCode, CancellationToken cancellationToken) =>
        Task.FromResult(sessions.Values.FirstOrDefault(session => session.JoinCode == joinCode));

    public Task<LiveSession?> GetAsync(string workspaceId, string sessionId, CancellationToken cancellationToken) =>
        Task.FromResult(sessions.TryGetValue(sessionId, out var session) && session.WorkspaceId == workspaceId
            ? session
            : null);

    public Task<LiveSession?> GetActiveByStepAsync(
        string workspaceId, string engagementId, string journeyStepId, CancellationToken cancellationToken) =>
        Task.FromResult(sessions.Values.FirstOrDefault(session =>
            session.WorkspaceId == workspaceId &&
            session.EngagementId == engagementId &&
            session.JourneyStepId == journeyStepId &&
            session.Status == "active"));
}

