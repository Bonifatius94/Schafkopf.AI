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
    public Card(CardType type, CardColor color)
    {
        Id = (byte)(((byte)type << 2) | (byte)color);
    }

    public Card(byte id)
    {
        Id = id;
    }

    public byte Id { get; init; }

    public CardType Type => (CardType)(Id >> 2);
    public CardColor Color => (CardColor)(Id & 3);

    #region Equality

    public override bool Equals([NotNullWhen(true)] object obj)
        => obj != null && obj is Card c && c.Id == this.Id;

    public override int GetHashCode() => Id;

    #endregion Equality

    public override string ToString() => $"{Color} {Type}";
    // TODO: add an emoji format
}
