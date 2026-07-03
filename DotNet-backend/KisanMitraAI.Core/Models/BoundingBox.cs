namespace KisanMitraAI.Core.Models;

/// <summary>
/// Value object representing a bounding box for defect location
/// </summary>
public record BoundingBox
{
    public float Left { get; init; }
    public float Top { get; init; }
    public float Width { get; init; }
    public float Height { get; init; }

    public BoundingBox(float left, float top, float width, float height)
    {
        if (left < 0 || left > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(left), 
                "Left must be between 0 and 1 (normalized coordinates)");
        }

        if (top < 0 || top > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(top), 
                "Top must be between 0 and 1 (normalized coordinates)");
        }

        if (width <= 0 || width > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(width), 
                "Width must be between 0 and 1 (normalized coordinates)");
        }

        if (height <= 0 || height > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(height), 
                "Height must be between 0 and 1 (normalized coordinates)");
        }

        Left = left;
        Top = top;
        Width = width;
        Height = height;
    }
}
