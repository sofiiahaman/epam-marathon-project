using Restaurant_Aqa_Framework.Api.Dtos.Orders;

namespace Restaurant_Aqa_Framework.Api.Dtos.Groups;

public enum GroupParticipantType
{
    Owner,
    Guest,
}

public sealed record GroupParticipant(
    string? Id,
    GroupParticipantType Type,
    string Name,
    IReadOnlyList<OrderItemResponse> Items,
    decimal Total,
    bool IsSubmitted);
