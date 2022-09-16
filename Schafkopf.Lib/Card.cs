using System.Diagnostics.CodeAnalysis;

namespace Schafkopf.Lib;

public enum CardType
{
    Sieben,
    Acht,
    Neun,
    Unter,
    Ober,
    Koenig,
    Zehn,
    Sau
}

public enum CardColor
{
    Schell,
    Herz,
    Gras,
    Eichel
}

public readonly struct Card
{
    public const byte EXISTING_FLAG = 0x20;
    public const byte TRUMPF_FLAG = 0x40;
    public const byte ORIG_CARD_MASK = 0x1F;
    public const byte CARD_MASK_WITH_META = 0x7F;

    public Card(CardType type, CardColor color)
    {
        Id = (byte)(((byte)type << 2) | (byte)color);
    }

    // info: this constructor just exists to outline the memory allocation;
    //       the meta-data is instanciated by the Hand struct with bitwise ops
    public Card(CardType type, CardColor color, bool exists, bool isTrumpf)
    {
        Id = (byte)((byte)color
            | ((byte)type << 2)
            | (exists ? EXISTING_FLAG : 0)
            | (isTrumpf ? TRUMPF_FLAG : 0));
    }

    public Card(byte id) => Id = id;

    public byte Id { get; init; }

    public CardType Type => (CardType)((Id & 0x1C) >> 2);
    public CardColor Color => (CardColor)(Id & 0x3);

    // optional meta-data parameters for game logic
    public bool Exists => (Id & EXISTING_FLAG) > 0;
    public bool IsTrumpf => (Id & TRUMPF_FLAG) > 0;

    #region Equality

    // info: EQ_MASK ensures that only the card type and color are used
    //       for comparison; meta-data is ignored to reduce coupling
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is Card c && (c.Id & ORIG_CARD_MASK) == (this.Id & ORIG_CARD_MASK);

    public override int GetHashCode() => Id & ORIG_CARD_MASK;

    public static bool operator ==(Card a, Card b)
        => a.GetHashCode() == b.GetHashCode();
    public static bool operator !=(Card a, Card b)
        => a.GetHashCode() != b.GetHashCode();

    #endregion Equality

    public override string ToString() => $"{Color} {Type}{(IsTrumpf ? " (trumpf)" : "")}";
    // TODO: add an emoji format
}
