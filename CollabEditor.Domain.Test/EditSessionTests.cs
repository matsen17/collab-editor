using CollabEditor.Domain.Aggregates.EditSessionAggregate;
using CollabEditor.Domain.Common;
using CollabEditor.Domain.Events;
using CollabEditor.Domain.Exceptions;
using CollabEditor.Domain.ValueObjects;
using FluentAssertions;
using InvalidOperationException = CollabEditor.Domain.Exceptions.InvalidOperationException;

namespace CollabEditor.Domain.Test;

public class EditSessionTests
{
    [Fact]
    public void Create_ShouldCreateEmptySession()
    {
        // Arrange
        var sessionId = SessionId.Create();
        
        // Act
        var session = EditSession.Create(sessionId);
        
        // Assert
        session.Id.Should().Be(sessionId);
        session.Participants.Should().BeEmpty();
        session.CurrentVersion.Should().Be(0);
        session.IsClosed.Should().BeFalse();
        session.CurrentContent.Text.Should().BeEmpty();
        session.DomainEvents.Should().BeEmpty();
    }
    
    [Fact]
    public void Create_WithInitialContent_ShouldCreateSessionWithContent()
    {
        // Arrange
        var sessionId = SessionId.Create();
        var initialContent = DocumentContent.From("Hello World");
        
        // Act
        var session = EditSession.Create(sessionId, initialContent);
        
        // Assert
        session.CurrentContent.Should().Be(initialContent);
        session.CurrentContent.Text.Should().Be("Hello World");
    }
    
    [Fact]
    public void AddParticipant_ShouldAddParticipantAndRaiseEvent()
    {
        // Arrange
        var session = EditSession.Create(SessionId.Create());
        var participantId = ParticipantId.Create();
        var participantName = "Alice";
        
        // Act
        session.AddParticipant(participantId, participantName);
        
        // Assert - Participant added
        session.Participants.Should().ContainSingle()
            .Which.Should().Match<Participant>(p =>
                p.Id == participantId &&
                p.Name == participantName &&
                p.IsActive);
        
        // Assert - Event raised with correct data
        session.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ParticipantJoinedEvent>()
            .Which.Should().Match<ParticipantJoinedEvent>(e =>
                e.SessionId == session.Id &&
                e.ParticipantId == participantId &&
                e.ParticipantName == participantName &&
                e.CurrentContent == session.CurrentContent);
    }
    
    [Fact]
    public void AddParticipant_WhenAlreadyExists_ShouldThrowException()
    {
        // Arrange
        var session = EditSession.Create(SessionId.Create());
        var participantId = ParticipantId.Create();
        session.AddParticipant(participantId, "Alice");
        session.ClearDomainEvents();
        
        // Act
        var act = () => session.AddParticipant(participantId, "Alice Again");
        
        // Assert
        act.Should().Throw<ParticipantAlreadyJoinedException>()
            .Which.Should().Match<ParticipantAlreadyJoinedException>(ex =>
                ex.ParticipantId == participantId &&
                ex.SessionId == session.Id &&
                ex.ErrorCode == "PARTICIPANT_ALREADY_JOINED");
        
        session.DomainEvents.Should().BeEmpty("no events should be raised on failure");
    }
    
    [Fact]
    public void AddParticipant_WhenSessionClosed_ShouldThrowException()
    {
        // Arrange
        var session = EditSession.Create(SessionId.Create());
        session.Close();
        session.ClearDomainEvents();
        
        // Act
        var act = () => session.AddParticipant(ParticipantId.Create(), "Bob");
        
        // Assert
        act.Should().Throw<SessionClosedException>()
            .Which.Should().Match<SessionClosedException>(ex =>
                ex.SessionId == session.Id &&
                ex.ErrorCode == "SESSION_CLOSED");
        
        session.DomainEvents.Should().BeEmpty();
    }
    
    [Fact]
    public void AddParticipant_WithNullName_ShouldThrowException()
    {
        // Arrange
        var session = EditSession.Create(SessionId.Create());
        
        // Act
        var act = () => session.AddParticipant(ParticipantId.Create(), null!);
        
        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*name*")
            .WithParameterName("name");
    }
    
    [Fact]
    public void AddParticipant_WithEmptyName_ShouldThrowException()
    {
        // Arrange
        var session = EditSession.Create(SessionId.Create());
        
        // Act
        var act = () => session.AddParticipant(ParticipantId.Create(), "");
        
        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }
    
    [Fact]
    public void AddParticipant_MultipleParticipants_ShouldRaiseMultipleEvents()
    {
        // Arrange
        var session = EditSession.Create(SessionId.Create());
        var participant1 = ParticipantId.Create();
        var participant2 = ParticipantId.Create();
        
        // Act
        session.AddParticipant(participant1, "Alice");
        session.AddParticipant(participant2, "Bob");
        
        // Assert
        session.Participants.Should().HaveCount(2);
        session.DomainEvents.Should().HaveCount(2)
            .And.AllBeOfType<ParticipantJoinedEvent>();
        
        var events = session.DomainEvents.Cast<ParticipantJoinedEvent>().ToList();
        events[0].ParticipantName.Should().Be("Alice");
        events[1].ParticipantName.Should().Be("Bob");
    }
    
    [Fact]
    public void RemoveParticipant_ShouldRemoveParticipantAndRaiseEvent()
    {
        // Arrange
        var session = EditSession.Create(SessionId.Create());
        var participantId = ParticipantId.Create();
        session.AddParticipant(participantId, "Alice");
        session.ClearDomainEvents();
        
        // Act
        session.RemoveParticipant(participantId);
        
        // Assert
        session.Participants.Should().BeEmpty();
        
        session.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ParticipantLeftEvent>()
            .Which.Should().Match<ParticipantLeftEvent>(e =>
                e.SessionId == session.Id &&
                e.ParticipantId == participantId &&
                e.RemainingParticipantCount == 0);
    }
    
    [Fact]
    public void RemoveParticipant_WhenLastParticipant_ShouldCloseSession()
    {
        // Arrange
        var session = EditSession.Create(SessionId.Create());
        var participantId = ParticipantId.Create();
        session.AddParticipant(participantId, "Alice");
        session.ClearDomainEvents();
        
        // Act
        session.RemoveParticipant(participantId);
        
        // Assert
        session.Participants.Should().BeEmpty();
        session.IsClosed.Should().BeTrue("session should auto-close when last participant leaves");
        
        session.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ParticipantLeftEvent>();
    }
    
    [Fact]
    public void RemoveParticipant_WhenNotLastParticipant_ShouldNotCloseSession()
    {
        // Arrange
        var session = EditSession.Create(SessionId.Create());
        var participant1 = ParticipantId.Create();
        var participant2 = ParticipantId.Create();
        session.AddParticipant(participant1, "Alice");
        session.AddParticipant(participant2, "Bob");
        session.ClearDomainEvents();
        
        // Act
        session.RemoveParticipant(participant1);
        
        // Assert
        session.Participants.Should().ContainSingle()
            .Which.Name.Should().Be("Bob");
        session.IsClosed.Should().BeFalse();
        
        session.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ParticipantLeftEvent>()
            .Which.RemainingParticipantCount.Should().Be(1);
    }
    
    [Fact]
    public void Close_ShouldMarkSessionAsClosed()
    {
        // Arrange
        var session = EditSession.Create(SessionId.Create());
        var participant = ParticipantId.Create();
        session.AddParticipant(participant, "Alice");
        
        // Act
        session.Close();
        
        // Assert
        session.IsClosed.Should().BeTrue();
        session.Participants.Should().ContainSingle()
            .Which.IsActive.Should().BeFalse("participants should be marked inactive");
    }
    
    [Fact]
    public void Close_WhenAlreadyClosed_ShouldBeIdempotent()
    {
        // Arrange
        var session = EditSession.Create(SessionId.Create());
        session.Close();
        var firstClosedTime = session.LastModifiedAt;
        
        // Act
        session.Close();
        
        // Assert
        session.IsClosed.Should().BeTrue();
        session.LastModifiedAt.Should().Be(firstClosedTime);
    }
    
    [Fact]
    public void Reopen_ShouldAllowSessionToAcceptParticipants()
    {
        // Arrange
        var session = EditSession.Create(SessionId.Create());
        session.Close();
        
        // Act
        session.Reopen();
        session.AddParticipant(ParticipantId.Create(), "Alice");
        
        // Assert
        session.IsClosed.Should().BeFalse();
        session.Participants.Should().ContainSingle();
    }
    
    [Fact]
    public void UpdateParticipantActivity_ShouldUpdateTimestamp()
    {
        // Arrange
        var session = EditSession.Create(SessionId.Create());
        var participantId = ParticipantId.Create();
        session.AddParticipant(participantId, "Alice");
        var participant = session.Participants.First();
        var initialActivity = participant.LastActiveAt;
        
        // Wait a tiny bit to ensure timestamp changes
        Thread.Sleep(10);
        
        // Act
        session.UpdateParticipantActivity(participantId);
        
        // Assert
        participant.LastActiveAt.Should().BeAfter(initialActivity);
        participant.IsActive.Should().BeTrue();
    }
    
    [Fact]
    public void UpdateParticipantActivity_WhenParticipantNotFound_ShouldThrowException()
    {
        // Arrange
        var session = EditSession.Create(SessionId.Create());
        var nonExistentParticipant = ParticipantId.Create();
        
        // Act
        var act = () => session.UpdateParticipantActivity(nonExistentParticipant);
        
        // Assert
        act.Should().Throw<InvalidOperationException>()
            .Which.ErrorCode.Should().Be("PARTICIPANT_NOT_FOUND");
    }
    
    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var session = EditSession.Create(SessionId.Create());
        session.AddParticipant(ParticipantId.Create(), "Alice");
        session.AddParticipant(ParticipantId.Create(), "Bob");
        session.DomainEvents.Should().HaveCount(2);
        
        // Act
        session.ClearDomainEvents();
        
        // Assert
        session.DomainEvents.Should().BeEmpty();
    }
    
    [Fact]
    public void DomainEvents_ShouldBeReadOnlyCollection()
    {
        // Arrange
        var session = EditSession.Create(SessionId.Create());
        session.AddParticipant(ParticipantId.Create(), "Alice");
        
        // Act
        var events = session.DomainEvents;
        
        // Assert
        events.Should().BeAssignableTo<IReadOnlyCollection<DomainEvent>>();
        events.Should().ContainSingle();
    }
}