namespace LCB.Domain.Models;

public record ChatMessageModel(
    string User,
    string Text,
    string Platform,
    DateTime CreatedAt,
    string InsertedByUser
);