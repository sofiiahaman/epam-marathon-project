namespace Restaurant_Aqa_Framework.Api.Dtos.Groups;

public sealed record GroupParticipantsResponse(
    IReadOnlyList<GroupParticipant> Participants,
    decimal Total);
