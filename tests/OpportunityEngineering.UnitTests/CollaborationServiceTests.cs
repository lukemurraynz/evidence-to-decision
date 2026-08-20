using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Application.Services;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.UnitTests;

[TestClass]
public sealed class CollaborationServiceTests
{
    [TestMethod]
    public async Task CastVoteThenTallyThenPromoteFlowSucceeds()
    {
        // Set up a real journey step to promote against, exactly like the shipped
        // Persona -> JourneyMap -> manual shortlist flow.
        var graphStore = new InMemoryGraphStore(TestData.CreateGraph());
        var commands = new GraphCommandService(
            graphStore,
            new SequentialIdentifierFactory(),
            new FixedTimeProvider(),
            new GateEvaluator(new FixedTimeProvider()));
        var actor = TestData.Facilitator();

        var afterPersona = await commands.AddPersonaAsync(
            actor,
            "engagement-1",
            new Persona("persona-1", "Alex", "Claims advisor", [], [], []),
            graphStore.Graph.ObjectVersion,
            CancellationToken.None);
        var afterJourneyMap = await commands.AddJourneyMapAsync(
            actor,
            "engagement-1",
            new JourneyMap(
                "journey-map-1",
                "persona-1",
                "workflow-1",
                [new JourneyStep("step-1", 1, "Receive the claim", "Re-keying", "Automate", "Minutes")]),
            afterPersona.ObjectVersion,
            CancellationToken.None);

        // Two participants vote for the same card/step, and one of them also backs a second
        // card for that step. The session-level rebuild lets a step have multiple candidate
        // cards live at once, so the tally must carry both rather than being pinned to one.
        // Neither vote touches graphStore at all: the ETag-free staging path stays separate.
        var voteStore = new InMemoryLiveVoteStore();
        var votes = new LiveVoteService(voteStore, new SequentialIdentifierFactory(), new FixedTimeProvider());
        var participantA = new ParticipantContext("participant-a", "workspace-1", "engagement-1", "session-1", "Riley", "c1");
        var participantB = new ParticipantContext("participant-b", "workspace-1", "engagement-1", "session-1", "Sam", "c2");

        await votes.CastAsync(participantA, "navigation-and-control-automate-home-operations", "step-1", CancellationToken.None);
        await votes.CastAsync(participantB, "navigation-and-control-automate-home-operations", "step-1", CancellationToken.None);
        var tally = await votes.CastAsync(participantB, "data-and-predictive-analytics-forecast-demand", "step-1", CancellationToken.None);

        Assert.AreEqual(2, tally.Count);
        var runnerUp = tally.Single(item => item.DiscoveryCardId == "data-and-predictive-analytics-forecast-demand");
        Assert.AreEqual("step-1", runnerUp.JourneyStepId);
        Assert.AreEqual(1, runnerUp.Count);

        var winner = tally.Single(item => item.DiscoveryCardId == "navigation-and-control-automate-home-operations");
        Assert.AreEqual("step-1", winner.JourneyStepId);
        Assert.AreEqual(2, winner.Count);

        // The facilitator's promote action is the only thing that ever touches the
        // canonical graph. It reuses the exact same AddCardShortlistEntryAsync path the
        // manual shortlist flow already uses, just pre-filled from the tally.
        var afterPromote = await commands.AddCardShortlistEntryAsync(
            actor,
            "engagement-1",
            new CardShortlistEntry(
                "shortlist-1",
                winner.JourneyStepId,
                winner.DiscoveryCardId,
                "Two participants voted for this card during the live session.",
                1,
                FacilitatorSelected: true),
            afterJourneyMap.ObjectVersion,
            CancellationToken.None);

        var promoted = afterPromote.CardShortlist.Single(entry => entry.Id == "shortlist-1");
        Assert.IsTrue(promoted.FacilitatorSelected);
        Assert.AreEqual("navigation-and-control-automate-home-operations", promoted.DiscoveryCardId);
    }

    [TestMethod]
    public async Task RecastingAVoteForTheSameCardAndStepOverwritesRatherThanDuplicates()
    {
        var voteStore = new InMemoryLiveVoteStore();
        var votes = new LiveVoteService(voteStore, new SequentialIdentifierFactory(), new FixedTimeProvider());
        var participant = new ParticipantContext("participant-a", "workspace-1", "engagement-1", "session-1", "Riley", "c1");

        await votes.CastAsync(participant, "card-1", "step-1", CancellationToken.None);
        var tally = await votes.CastAsync(participant, "card-1", "step-1", CancellationToken.None);

        Assert.AreEqual(1, tally.Single().Count);
    }

    [TestMethod]
    public async Task CreateAsyncRejectsReviewerStartingALiveSession()
    {
        var sessionStore = new InMemoryLiveSessionStore();
        var graphStore = new InMemoryGraphStore(TestData.CreateGraph());
        var service = new LiveSessionService(
            sessionStore, graphStore, new SequentialIdentifierFactory(), new FixedTimeProvider());

        var exception = await Assert.ThrowsExactlyAsync<DomainException>(() =>
            service.CreateAsync(TestData.Reviewer(), "engagement-1", "step-1", false, CancellationToken.None));

        Assert.AreEqual("authorization.live_session_denied", exception.Code);
    }

    [TestMethod]
    public async Task GetActiveByStepAsyncFindsTheSessionAFacilitatorAlreadyStarted()
    {
        var sessionStore = new InMemoryLiveSessionStore();
        var graphStore = new InMemoryGraphStore(TestData.CreateGraph());
        var service = new LiveSessionService(
            sessionStore, graphStore, new SequentialIdentifierFactory(), new FixedTimeProvider());
        var created = await service.CreateAsync(TestData.Facilitator(), "engagement-1", "step-1", false, CancellationToken.None);

        var found = await service.GetActiveByStepAsync(TestData.Facilitator(), "engagement-1", "step-1", CancellationToken.None);

        Assert.AreEqual(created.Id, found?.Id);
    }

    [TestMethod]
    public async Task GetActiveByStepAsyncRejectsReviewers()
    {
        var sessionStore = new InMemoryLiveSessionStore();
        var graphStore = new InMemoryGraphStore(TestData.CreateGraph());
        var service = new LiveSessionService(
            sessionStore, graphStore, new SequentialIdentifierFactory(), new FixedTimeProvider());

        var exception = await Assert.ThrowsExactlyAsync<DomainException>(() =>
            service.GetActiveByStepAsync(TestData.Reviewer(), "engagement-1", "step-1", CancellationToken.None));

        Assert.AreEqual("authorization.live_session_denied", exception.Code);
    }

    [TestMethod]
    public async Task CreateAsyncDefaultsToARevealedBoardWhenNotStartedPrivately()
    {
        var sessionStore = new InMemoryLiveSessionStore();
        var graphStore = new InMemoryGraphStore(TestData.CreateGraph());
        var service = new LiveSessionService(
            sessionStore, graphStore, new SequentialIdentifierFactory(), new FixedTimeProvider());

        var session = await service.CreateAsync(TestData.Facilitator(), "engagement-1", "step-1", false, CancellationToken.None);

        Assert.IsTrue(session.BoardRevealed);
    }

    [TestMethod]
    public async Task CreateAsyncStartsThePrivateBoardUnrevealed()
    {
        var sessionStore = new InMemoryLiveSessionStore();
        var graphStore = new InMemoryGraphStore(TestData.CreateGraph());
        var service = new LiveSessionService(
            sessionStore, graphStore, new SequentialIdentifierFactory(), new FixedTimeProvider());

        var session = await service.CreateAsync(TestData.Facilitator(), "engagement-1", "step-1", startPrivate: true, CancellationToken.None);

        Assert.IsFalse(session.BoardRevealed);
    }

    [TestMethod]
    public async Task RevealBoardAsyncFlipsAPrivateSessionToRevealed()
    {
        var sessionStore = new InMemoryLiveSessionStore();
        var graphStore = new InMemoryGraphStore(TestData.CreateGraph());
        var service = new LiveSessionService(
            sessionStore, graphStore, new SequentialIdentifierFactory(), new FixedTimeProvider());
        var session = await service.CreateAsync(TestData.Facilitator(), "engagement-1", "step-1", startPrivate: true, CancellationToken.None);

        var revealed = await service.RevealBoardAsync(TestData.Facilitator(), session.Id, CancellationToken.None);

        Assert.IsTrue(revealed.BoardRevealed);
    }

    [TestMethod]
    public async Task RevealBoardAsyncRejectsReviewers()
    {
        var sessionStore = new InMemoryLiveSessionStore();
        var graphStore = new InMemoryGraphStore(TestData.CreateGraph());
        var service = new LiveSessionService(
            sessionStore, graphStore, new SequentialIdentifierFactory(), new FixedTimeProvider());
        var session = await service.CreateAsync(TestData.Facilitator(), "engagement-1", "step-1", startPrivate: true, CancellationToken.None);

        var exception = await Assert.ThrowsExactlyAsync<DomainException>(() =>
            service.RevealBoardAsync(TestData.Reviewer(), session.Id, CancellationToken.None));

        Assert.AreEqual("authorization.live_session_denied", exception.Code);
    }

    [TestMethod]
    public async Task SetBoardPrivateAsyncFlipsARevealedSessionBackToUnrevealed()
    {
        var sessionStore = new InMemoryLiveSessionStore();
        var graphStore = new InMemoryGraphStore(TestData.CreateGraph());
        var service = new LiveSessionService(
            sessionStore, graphStore, new SequentialIdentifierFactory(), new FixedTimeProvider());
        var session = await service.CreateAsync(TestData.Facilitator(), "engagement-1", "step-1", false, CancellationToken.None);
        Assert.IsTrue(session.BoardRevealed);

        var madePrivate = await service.SetBoardPrivateAsync(TestData.Facilitator(), session.Id, CancellationToken.None);

        Assert.IsFalse(madePrivate.BoardRevealed);
    }

    [TestMethod]
    public async Task SetBoardPrivateAsyncRejectsReviewers()
    {
        var sessionStore = new InMemoryLiveSessionStore();
        var graphStore = new InMemoryGraphStore(TestData.CreateGraph());
        var service = new LiveSessionService(
            sessionStore, graphStore, new SequentialIdentifierFactory(), new FixedTimeProvider());
        var session = await service.CreateAsync(TestData.Facilitator(), "engagement-1", "step-1", false, CancellationToken.None);

        var exception = await Assert.ThrowsExactlyAsync<DomainException>(() =>
            service.SetBoardPrivateAsync(TestData.Reviewer(), session.Id, CancellationToken.None));

        Assert.AreEqual("authorization.live_session_denied", exception.Code);
    }

    [TestMethod]
    public async Task CloseAsyncMarksTheSessionClosedAndRejectsFurtherJoins()
    {
        var sessionStore = new InMemoryLiveSessionStore();
        var graphStore = new InMemoryGraphStore(TestData.CreateGraph());
        var service = new LiveSessionService(
            sessionStore, graphStore, new SequentialIdentifierFactory(), new FixedTimeProvider());
        var session = await service.CreateAsync(TestData.Facilitator(), "engagement-1", "step-1", false, CancellationToken.None);

        var closed = await service.CloseAsync(TestData.Facilitator(), session.Id, CancellationToken.None);

        Assert.AreEqual("closed", closed.Status);
        var exception = await Assert.ThrowsExactlyAsync<DomainException>(() =>
            service.RedeemJoinCodeAsync(session.JoinCode, CancellationToken.None));
        Assert.AreEqual("live_session.expired", exception.Code);
    }

    [TestMethod]
    public async Task CloseAsyncRejectsReviewerClosingALiveSession()
    {
        var sessionStore = new InMemoryLiveSessionStore();
        var graphStore = new InMemoryGraphStore(TestData.CreateGraph());
        var service = new LiveSessionService(
            sessionStore, graphStore, new SequentialIdentifierFactory(), new FixedTimeProvider());
        var session = await service.CreateAsync(TestData.Facilitator(), "engagement-1", "step-1", false, CancellationToken.None);

        var exception = await Assert.ThrowsExactlyAsync<DomainException>(() =>
            service.CloseAsync(TestData.Reviewer(), session.Id, CancellationToken.None));

        Assert.AreEqual("authorization.live_session_denied", exception.Code);
    }

    [TestMethod]
    public async Task RedeemJoinCodeAsyncRejectsAnExpiredSession()
    {
        var sessionStore = new InMemoryLiveSessionStore();
        var graphStore = new InMemoryGraphStore(TestData.CreateGraph());
        var timeProvider = new FixedTimeProvider();
        var expired = new LiveSession(
            Id: "session-1",
            WorkspaceId: "workspace-1",
            EngagementId: "engagement-1",
            JourneyStepId: "step-1",
            JoinCode: "ABC123",
            CreatedBy: "actor-1",
            CreatedAt: timeProvider.GetUtcNow().AddHours(-5),
            ExpiresAt: timeProvider.GetUtcNow().AddHours(-1),
            Status: "active");
        await sessionStore.CreateAsync(expired, CancellationToken.None);
        var service = new LiveSessionService(sessionStore, graphStore, new SequentialIdentifierFactory(), timeProvider);

        var exception = await Assert.ThrowsExactlyAsync<DomainException>(() =>
            service.RedeemJoinCodeAsync("ABC123", CancellationToken.None));

        Assert.AreEqual("live_session.expired", exception.Code);
    }

    [TestMethod]
    public async Task RedeemJoinCodeAsyncResolvesTheJourneyStepForTheJoiningParticipant()
    {
        var sessionStore = new InMemoryLiveSessionStore();
        var graphStore = new InMemoryGraphStore(TestData.CreateGraph());
        var commands = new GraphCommandService(
            graphStore,
            new SequentialIdentifierFactory(),
            new FixedTimeProvider(),
            new GateEvaluator(new FixedTimeProvider()));
        var actor = TestData.Facilitator();
        await commands.AddPersonaAsync(
            actor,
            "engagement-1",
            new Persona("persona-1", "Alex", "Claims advisor", [], [], []),
            graphStore.Graph.ObjectVersion,
            CancellationToken.None);
        await commands.AddJourneyMapAsync(
            actor,
            "engagement-1",
            new JourneyMap(
                "journey-map-1",
                "persona-1",
                "workflow-1",
                [new JourneyStep("step-1", 1, "Receive the claim", "Re-keying", "Automate", "Minutes")]),
            graphStore.Graph.ObjectVersion,
            CancellationToken.None);

        var timeProvider = new FixedTimeProvider();
        var service = new LiveSessionService(sessionStore, graphStore, new SequentialIdentifierFactory(), timeProvider);
        var session = await service.CreateAsync(actor, "engagement-1", "step-1", false, CancellationToken.None);

        var (_, _, step, _) = await service.RedeemJoinCodeAsync(session.JoinCode, CancellationToken.None);

        Assert.IsNotNull(step);
        Assert.AreEqual("Receive the claim", step.Name);
        Assert.AreEqual("Re-keying", step.PainPoint);
    }

    [TestMethod]
    public async Task SubmitThenCurateFlowMovesAnIdeaFromEphemeralToCanonical()
    {
        var noteStore = new InMemoryLiveIdeationNoteStore();
        var ideation = new LiveIdeationService(noteStore, new SequentialIdentifierFactory(), new FixedTimeProvider());
        var participant = new ParticipantContext("participant-a", "workspace-1", "engagement-1", "session-1", "Riley", "c1");

        var notes = await ideation.SubmitAsync(participant, "Skip the re-keying step entirely.", CancellationToken.None);
        Assert.AreEqual("Skip the re-keying step entirely.", notes.Single().Text);

        var graphStore = new InMemoryGraphStore(TestData.CreateGraph());
        var commands = new GraphCommandService(
            graphStore,
            new SequentialIdentifierFactory(),
            new FixedTimeProvider(),
            new GateEvaluator(new FixedTimeProvider()));
        var note = notes.Single();
        // Curation credits the display name captured at submission time, not the opaque
        // per-join ParticipantId, the same field the real /ideation-notes/curate endpoint reads.
        var afterCurate = await commands.AddIdeationNoteAsync(
            TestData.Facilitator(),
            "engagement-1",
            new IdeationNote("ideation-note-1", note.Text, note.DisplayName, TestData.Now),
            graphStore.Graph.ObjectVersion,
            CancellationToken.None);

        var curated = afterCurate.IdeationNotes.Single();
        Assert.AreEqual("Skip the re-keying step entirely.", curated.Text);
        Assert.AreEqual("Riley", curated.SubmittedBy);
    }

    [TestMethod]
    public async Task SubmitRejectsAnEmptyIdea()
    {
        var noteStore = new InMemoryLiveIdeationNoteStore();
        var ideation = new LiveIdeationService(noteStore, new SequentialIdentifierFactory(), new FixedTimeProvider());
        var participant = new ParticipantContext("participant-a", "workspace-1", "engagement-1", "session-1", "Riley", "c1");

        var exception = await Assert.ThrowsExactlyAsync<DomainException>(() =>
            ideation.SubmitAsync(participant, "   ", CancellationToken.None));

        Assert.AreEqual("live_ideation_note.text_required", exception.Code);
    }

    [TestMethod]
    public async Task SubmitRejectsAnIdeaOverTheLengthCap()
    {
        var noteStore = new InMemoryLiveIdeationNoteStore();
        var ideation = new LiveIdeationService(noteStore, new SequentialIdentifierFactory(), new FixedTimeProvider());
        var participant = new ParticipantContext("participant-a", "workspace-1", "engagement-1", "session-1", "Riley", "c1");

        var exception = await Assert.ThrowsExactlyAsync<DomainException>(() =>
            ideation.SubmitAsync(participant, new string('a', 501), CancellationToken.None));

        Assert.AreEqual("live_ideation_note.text_too_long", exception.Code);
    }

    [TestMethod]
    public async Task TogglingAPinOnThenOffRemovesItFromTheTally()
    {
        var pinStore = new InMemoryLivePinStore();
        var pins = new LivePinService(pinStore, new SequentialIdentifierFactory(), new FixedTimeProvider());
        var participant = new ParticipantContext("participant-a", "workspace-1", "engagement-1", "session-1", "Riley", "c1");

        var (pinnedOn, tallyOn) = await pins.ToggleAsync(participant, "card-1", "step-1", CancellationToken.None);
        Assert.IsTrue(pinnedOn);
        Assert.AreEqual(1, tallyOn.Single().Count);

        var (pinnedOff, tallyOff) = await pins.ToggleAsync(participant, "card-1", "step-1", CancellationToken.None);
        Assert.IsFalse(pinnedOff);
        Assert.HasCount(0, tallyOff);
    }

    [TestMethod]
    public async Task PinsFromDifferentParticipantsForTheSameCardAccumulateInTheTally()
    {
        var pinStore = new InMemoryLivePinStore();
        var pins = new LivePinService(pinStore, new SequentialIdentifierFactory(), new FixedTimeProvider());
        var participantA = new ParticipantContext("participant-a", "workspace-1", "engagement-1", "session-1", "Riley", "c1");
        var participantB = new ParticipantContext("participant-b", "workspace-1", "engagement-1", "session-1", "Sam", "c2");

        await pins.ToggleAsync(participantA, "card-1", "step-1", CancellationToken.None);
        var (_, tally) = await pins.ToggleAsync(participantB, "card-1", "step-1", CancellationToken.None);

        Assert.AreEqual(2, tally.Single().Count);
    }

    [TestMethod]
    public async Task RedeemJoinCodeAsyncReturnsCardsShortlistedForThisStepButNotOtherSteps()
    {
        var sessionStore = new InMemoryLiveSessionStore();
        var graphStore = new InMemoryGraphStore(TestData.CreateGraph());
        var commands = new GraphCommandService(
            graphStore,
            new SequentialIdentifierFactory(),
            new FixedTimeProvider(),
            new GateEvaluator(new FixedTimeProvider()));
        var actor = TestData.Facilitator();
        await commands.AddPersonaAsync(
            actor,
            "engagement-1",
            new Persona("persona-1", "Alex", "Claims advisor", [], [], []),
            graphStore.Graph.ObjectVersion,
            CancellationToken.None);
        await commands.AddJourneyMapAsync(
            actor,
            "engagement-1",
            new JourneyMap(
                "journey-map-1",
                "persona-1",
                "workflow-1",
                [
                    new JourneyStep("step-1", 1, "Receive the claim", "Re-keying", "Automate", "Minutes"),
                    new JourneyStep("step-2", 2, "Assess the claim", "Manual review", "Automate", "Minutes"),
                ]),
            graphStore.Graph.ObjectVersion,
            CancellationToken.None);
        await commands.AddCardShortlistEntryAsync(
            actor,
            "engagement-1",
            new CardShortlistEntry("shortlist-1", "step-1", "card-a", "Discussed previously.", 1, FacilitatorSelected: false),
            graphStore.Graph.ObjectVersion,
            CancellationToken.None);
        await commands.AddCardShortlistEntryAsync(
            actor,
            "engagement-1",
            new CardShortlistEntry("shortlist-2", "step-1", "card-b", "Also discussed.", 2, FacilitatorSelected: true),
            graphStore.Graph.ObjectVersion,
            CancellationToken.None);
        await commands.AddCardShortlistEntryAsync(
            actor,
            "engagement-1",
            new CardShortlistEntry("shortlist-3", "step-2", "card-c", "Different step entirely.", 1, FacilitatorSelected: false),
            graphStore.Graph.ObjectVersion,
            CancellationToken.None);

        var service = new LiveSessionService(sessionStore, graphStore, new SequentialIdentifierFactory(), new FixedTimeProvider());
        var session = await service.CreateAsync(actor, "engagement-1", "step-1", false, CancellationToken.None);

        var (_, _, _, shortlistedDiscoveryCardIds) =
            await service.RedeemJoinCodeAsync(session.JoinCode, CancellationToken.None);

        Assert.AreEqual(2, shortlistedDiscoveryCardIds.Count);
        Assert.IsTrue(shortlistedDiscoveryCardIds.Contains("card-a"));
        Assert.IsTrue(shortlistedDiscoveryCardIds.Contains("card-b"));
        Assert.IsFalse(shortlistedDiscoveryCardIds.Contains("card-c"));
    }

    [TestMethod]
    public async Task PlacingTheSameCardTwiceCreatesTwoIndependentPlacements()
    {
        var board = new LiveBoardService(new InMemoryLiveBoardCardStore(), new SequentialIdentifierFactory(), new FixedTimeProvider());
        var participant = new ParticipantContext("participant-a", "workspace-1", "engagement-1", "session-1", "Riley", "c1");

        await board.PlaceAsync(participant, "card-1", 0.2, 0.2, "First take.", CancellationToken.None);
        var placements = await board.PlaceAsync(participant, "card-1", 0.3, 0.3, "Second take.", CancellationToken.None);

        Assert.HasCount(2, placements);
        Assert.AreNotEqual(placements[0].Id, placements[1].Id);
    }

    [TestMethod]
    public async Task MovingAPlacementByADifferentParticipantThanThePlacerSucceedsAndPreservesOwnership()
    {
        var board = new LiveBoardService(new InMemoryLiveBoardCardStore(), new SequentialIdentifierFactory(), new FixedTimeProvider());
        var placer = new ParticipantContext("participant-a", "workspace-1", "engagement-1", "session-1", "Riley", "c1");
        var mover = new ParticipantContext("participant-b", "workspace-1", "engagement-1", "session-1", "Sam", "c2");
        var placed = await board.PlaceAsync(placer, "card-1", 0.2, 0.2, "Fits our persona.", CancellationToken.None);

        var moved = await board.MoveAsync(mover, placed.Single().Id, 0.8, 0.6, CancellationToken.None);

        Assert.AreEqual(0.8, moved.Single().X);
        Assert.AreEqual(0.6, moved.Single().Y);
        Assert.AreEqual("participant-a", moved.Single().PlacedByParticipantId);
        Assert.AreEqual("Fits our persona.", moved.Single().Rationale);
    }

    [TestMethod]
    public async Task PlaceAsyncClampsOutOfRangeCoordinatesToTheCanvasBounds()
    {
        var board = new LiveBoardService(new InMemoryLiveBoardCardStore(), new SequentialIdentifierFactory(), new FixedTimeProvider());
        var participant = new ParticipantContext("participant-a", "workspace-1", "engagement-1", "session-1", "Riley", "c1");

        var placements = await board.PlaceAsync(participant, "card-1", -0.5, 1.7, "Rationale.", CancellationToken.None);

        Assert.AreEqual(0.0, placements.Single().X);
        Assert.AreEqual(1.0, placements.Single().Y);
    }

    [TestMethod]
    public async Task PlaceAsyncAllowsAStickyNoteWithNoCatalogCardWhenTextIsGiven()
    {
        var board = new LiveBoardService(new InMemoryLiveBoardCardStore(), new SequentialIdentifierFactory(), new FixedTimeProvider());
        var participant = new ParticipantContext("participant-a", "workspace-1", "engagement-1", "session-1", "Riley", "c1");

        var placements = await board.PlaceAsync(participant, null, 0.2, 0.2, "Something the catalog doesn't cover.", CancellationToken.None);

        Assert.IsNull(placements.Single().DiscoveryCardId);
        Assert.AreEqual("Something the catalog doesn't cover.", placements.Single().Rationale);
    }

    [TestMethod]
    public async Task PlaceAsyncRejectsAnEmptyStickyNote()
    {
        var board = new LiveBoardService(new InMemoryLiveBoardCardStore(), new SequentialIdentifierFactory(), new FixedTimeProvider());
        var participant = new ParticipantContext("participant-a", "workspace-1", "engagement-1", "session-1", "Riley", "c1");

        var exception = await Assert.ThrowsExactlyAsync<DomainException>(() =>
            board.PlaceAsync(participant, null, 0.2, 0.2, "   ", CancellationToken.None));

        Assert.AreEqual("live_board_card.note_text_required", exception.Code);
    }

    [TestMethod]
    public async Task RemoveAsyncByADifferentParticipantThanThePlacerRemovesTheCard()
    {
        var board = new LiveBoardService(new InMemoryLiveBoardCardStore(), new SequentialIdentifierFactory(), new FixedTimeProvider());
        var placer = new ParticipantContext("participant-a", "workspace-1", "engagement-1", "session-1", "Riley", "c1");
        var remover = new ParticipantContext("participant-b", "workspace-1", "engagement-1", "session-1", "Sam", "c2");
        var placed = await board.PlaceAsync(placer, "card-1", 0.2, 0.2, "Fits our persona.", CancellationToken.None);

        var board2 = await board.RemoveAsync(remover, placed.Single().Id, CancellationToken.None);

        Assert.HasCount(0, board2);
    }

    [TestMethod]
    public async Task RemoveAsyncOfAnAlreadyMissingPlacementIsANoOp()
    {
        var board = new LiveBoardService(new InMemoryLiveBoardCardStore(), new SequentialIdentifierFactory(), new FixedTimeProvider());
        var participant = new ParticipantContext("participant-a", "workspace-1", "engagement-1", "session-1", "Riley", "c1");

        var result = await board.RemoveAsync(participant, "missing-placement", CancellationToken.None);

        Assert.HasCount(0, result);
    }

    [TestMethod]
    public async Task EditAsyncByADifferentParticipantThanThePlacerUpdatesTheRationale()
    {
        var board = new LiveBoardService(new InMemoryLiveBoardCardStore(), new SequentialIdentifierFactory(), new FixedTimeProvider());
        var placer = new ParticipantContext("participant-a", "workspace-1", "engagement-1", "session-1", "Riley", "c1");
        var editor = new ParticipantContext("participant-b", "workspace-1", "engagement-1", "session-1", "Sam", "c2");
        var placed = await board.PlaceAsync(placer, "card-1", 0.2, 0.2, "First take.", CancellationToken.None);

        var edited = await board.EditAsync(editor, placed.Single().Id, "Revised take.", CancellationToken.None);

        Assert.AreEqual("Revised take.", edited.Single().Rationale);
        Assert.AreEqual("participant-a", edited.Single().PlacedByParticipantId);
    }

    [TestMethod]
    public async Task EditAsyncRejectsBlankingOutAStickyNote()
    {
        var board = new LiveBoardService(new InMemoryLiveBoardCardStore(), new SequentialIdentifierFactory(), new FixedTimeProvider());
        var participant = new ParticipantContext("participant-a", "workspace-1", "engagement-1", "session-1", "Riley", "c1");
        var placed = await board.PlaceAsync(participant, null, 0.2, 0.2, "A note.", CancellationToken.None);

        var exception = await Assert.ThrowsExactlyAsync<DomainException>(() =>
            board.EditAsync(participant, placed.Single().Id, "   ", CancellationToken.None));

        Assert.AreEqual("live_board_card.note_text_required", exception.Code);
    }

    [TestMethod]
    public async Task EditAsyncRejectsAMissingPlacement()
    {
        var board = new LiveBoardService(new InMemoryLiveBoardCardStore(), new SequentialIdentifierFactory(), new FixedTimeProvider());
        var participant = new ParticipantContext("participant-a", "workspace-1", "engagement-1", "session-1", "Riley", "c1");

        var exception = await Assert.ThrowsExactlyAsync<DomainException>(() =>
            board.EditAsync(participant, "missing-placement", "Text.", CancellationToken.None));

        Assert.AreEqual("live_board_card.not_found", exception.Code);
    }

    [TestMethod]
    public async Task MoveAsyncRejectsAPlacementThatNoLongerExists()
    {
        var board = new LiveBoardService(new InMemoryLiveBoardCardStore(), new SequentialIdentifierFactory(), new FixedTimeProvider());
        var participant = new ParticipantContext("participant-a", "workspace-1", "engagement-1", "session-1", "Riley", "c1");

        var exception = await Assert.ThrowsExactlyAsync<DomainException>(() =>
            board.MoveAsync(participant, "missing-placement", 0.5, 0.5, CancellationToken.None));

        Assert.AreEqual("live_board_card.not_found", exception.Code);
    }

    [TestMethod]
    public async Task ClearAsyncRemovesEveryPlacementOnTheBoard()
    {
        var board = new LiveBoardService(new InMemoryLiveBoardCardStore(), new SequentialIdentifierFactory(), new FixedTimeProvider());
        var participantA = new ParticipantContext("participant-a", "workspace-1", "engagement-1", "session-1", "Riley", "c1");
        var participantB = new ParticipantContext("participant-b", "workspace-1", "engagement-1", "session-1", "Sam", "c2");
        await board.PlaceAsync(participantA, "card-1", 0.2, 0.2, "First take.", CancellationToken.None);
        await board.PlaceAsync(participantB, "card-2", 0.4, 0.4, "Second take.", CancellationToken.None);

        var cleared = await board.ClearAsync("workspace-1", "session-1", CancellationToken.None);

        Assert.HasCount(0, cleared);
    }

    [TestMethod]
    public async Task ClearAsyncOnlyClearsTheRequestedSessionsBoard()
    {
        var board = new LiveBoardService(new InMemoryLiveBoardCardStore(), new SequentialIdentifierFactory(), new FixedTimeProvider());
        var participant = new ParticipantContext("participant-a", "workspace-1", "engagement-1", "session-1", "Riley", "c1");
        var otherSession = participant with { JoinSessionId = "session-2" };
        await board.PlaceAsync(participant, "card-1", 0.2, 0.2, "Session 1's card.", CancellationToken.None);
        await board.PlaceAsync(otherSession, "card-2", 0.4, 0.4, "Session 2's card.", CancellationToken.None);

        await board.ClearAsync("workspace-1", "session-1", CancellationToken.None);
        var untouched = await board.GetBoardAsync("workspace-1", "session-2", CancellationToken.None);

        Assert.HasCount(1, untouched);
    }
}

internal sealed class InMemoryLiveSessionStore : ILiveSessionStore
{
    private readonly List<LiveSession> sessions = [];

    public Task<LiveSession> CreateAsync(LiveSession session, CancellationToken cancellationToken)
    {
        sessions.Add(session);
        return Task.FromResult(session);
    }

    public Task<LiveSession> UpdateStatusAsync(LiveSession session, CancellationToken cancellationToken)
    {
        var index = sessions.FindIndex(existing => existing.Id == session.Id);
        if (index >= 0)
        {
            sessions[index] = session;
        }

        return Task.FromResult(session);
    }

    public Task<LiveSession?> GetByJoinCodeAsync(string joinCode, CancellationToken cancellationToken) =>
        Task.FromResult(sessions.SingleOrDefault(session => session.JoinCode == joinCode));

    public Task<LiveSession?> GetAsync(string workspaceId, string sessionId, CancellationToken cancellationToken) =>
        Task.FromResult(sessions.SingleOrDefault(
            session => session.WorkspaceId == workspaceId && session.Id == sessionId));

    public Task<LiveSession?> GetActiveByStepAsync(
        string workspaceId, string engagementId, string journeyStepId, CancellationToken cancellationToken) =>
        Task.FromResult(sessions.FirstOrDefault(
            session => session.WorkspaceId == workspaceId
                && session.EngagementId == engagementId
                && session.JourneyStepId == journeyStepId
                && session.Status == "active"));
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
