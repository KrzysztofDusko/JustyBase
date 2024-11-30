using System;

namespace JustyBase.Editor;

public readonly struct BraceMatchingResult : IEquatable<BraceMatchingResult>
{
    public int LeftPosition { get; }
    public int RightPosition { get; }

    public BraceMatchingResult(int leftPosition, int rightPosition) :this()
    {
        LeftPosition = leftPosition;
        RightPosition = rightPosition;
    }


    public bool Equals(BraceMatchingResult other)
    {
        return LeftPosition == other.LeftPosition && RightPosition == other.RightPosition;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        return obj is BraceMatchingResult result && Equals(result);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (LeftPosition.GetHashCode() * 397) ^ RightPosition.GetHashCode();
        }
    }

    public static bool operator ==(BraceMatchingResult left, BraceMatchingResult right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(BraceMatchingResult left, BraceMatchingResult right)
    {
        return !left.Equals(right);
    }
}
