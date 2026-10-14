namespace RaceDay.Api.Dtos;

public record UserProfileResponse(int UserId, string FullName, string Email, string? PhoneNumber, string Role, DateTime CreatedAt);

public record EventResponse(int EventId, int OrganiserId, string OrganiserName, string EventName, string? Description,
    DateOnly EventDate, string Location, string EventType, decimal DistanceKm);

public record CategoryResponse(int CategoryId, int EventId, string CategoryName, decimal DistanceKm, decimal EntryFee,
    int MaxParticipants, int SpotsTaken);

public record EnrolmentResponse(int EnrolmentId, int ParticipantId, string ParticipantName, int EventId, string EventName,
    DateOnly EventDate, int CategoryId, string CategoryName, DateTime EnrolmentDate, string Status);

public record ResultResponse(int ResultId, int EnrolmentId, int ParticipantId, string ParticipantName, int EventId,
    string EventName, DateOnly EventDate, string CategoryName, TimeSpan FinishTime, int? Position, DateTime RecordedAt);
