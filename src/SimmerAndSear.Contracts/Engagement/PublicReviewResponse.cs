namespace SimmerAndSear.Contracts.Engagement;

public sealed record PublicReviewResponse(
    string Id,
    string ProductId,
    string ProductName,
    string ClientName,
    int Rating,
    string Title,
    string Body,
    DateTime CreatedAt
);
