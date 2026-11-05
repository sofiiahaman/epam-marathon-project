namespace Restaurant_Aqa_Framework.Api.Dtos.Groups;

public sealed record UpdateGuestSessionDishRequest(
    string DishId,
    int Quantity);
