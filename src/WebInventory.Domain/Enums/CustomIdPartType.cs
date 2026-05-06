namespace WebInventory.Domain.Enums;

public enum CustomIdPartType
{
    FixedText = 0,
    Random20Bit = 1,
    Random32Bit = 2,
    Random6Digit = 3,
    Random9Digit = 4,
    Guid = 5,
    DateTime = 6,
    Sequence = 7
}
