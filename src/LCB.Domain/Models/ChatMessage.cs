namespace LCB.Domain.Models;

public record ChatMessage(
    string User, 
    string Text, 
    string Platform, 
    DateTime CreatedAt
);