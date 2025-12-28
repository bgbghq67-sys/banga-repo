using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Banga_Photobooth
{
    public class TemplateMetadata
    {
        [JsonPropertyName("templateName")]
        public string TemplateName { get; set; } = string.Empty;

        [JsonPropertyName("resolution")]
        public Resolution Resolution { get; set; } = new();

        [JsonPropertyName("dpi")]
        public int Dpi { get; set; }

        [JsonPropertyName("orientation")]
        public string Orientation { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("photoSlots")]
        public List<PhotoSlot> PhotoSlots { get; set; } = new();

        [JsonPropertyName("qrSlot")]
        public Slot? QrSlot { get; set; }

        [JsonPropertyName("logoSlot")]
        public Slot? LogoSlot { get; set; }

        [JsonPropertyName("templateBoundary")]
        public TemplateBoundary? TemplateBoundary { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }

    public class TemplateBoundary
    {
        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("cornerRadius")]
        public int CornerRadius { get; set; }
    }

    public class Resolution
    {
        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }
    }

    public class PhotoSlot
    {
        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }
    }

    public class Slot
    {
        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }
    }
}

