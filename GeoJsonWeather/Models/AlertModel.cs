using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json;

namespace GeoJsonWeather.Models;

public class AlertModel
{
    private Color _color;

    public string Url { get; set; }
    public string Type { get; set; }
    public string Id { get; set; }
    public string AreaDescription { get; set; }
    public GeoCode GeoCode { get; set; }
    public List<string> AffectedZonesUrls { get; set; }
    public List<string> References { get; set; }
    public DateTime Sent { get; set; }
    public DateTime Effective { get; set; }
    public DateTime Onset { get; set; }
    public DateTime Expires { get; set; }
    public DateTime? Ends { get; set; }
    public string Status { get; set; }
    public string MessageType { get; set; }
    public string Category { get; set; }
    public string Severity { get; set; }
    public string Certainty { get; set; }
    public string Urgency { get; set; }
    public string Event { get; set; }
    public string Sender { get; set; }
    public string SenderName { get; set; }
    public string Headline { get; set; }
    public string Description { get; set; }
    public string Instruction { get; set; }
    public string Response { get; set; }
    public Dictionary<string, string> Parameters { get; set; }
    public List<County> Counties { get; set; }
    public List<State> States { get; set; }
    public List<string> ZipCodes { get; set; }

    public virtual Color AlertColor
    {
        get => Color.LightGray;
        set => _color = value;
    }

    public virtual Color SecondaryColor
    {
        get => Color.Black;
        set => _color = value;
    }
}

    public class SevereThunderstormWarningModel : AlertModel
    {
        public string HailSize { get; set; }
        public string WindGust { get; set; }
        public string TornadoDetection { get; set; }

        public override Color AlertColor => Color.Yellow;

        public SevereThunderstormWarningModel()
        {
        }

        public SevereThunderstormWarningModel(JsonElement feature) : base(feature)
        {
            HailSize         = Parameters.ContainsKey($"hailSize") ? Parameters["hailSize"] : null;
            TornadoDetection = Parameters.ContainsKey($"tornadoDetection") ? Parameters["tornadoDetection"] : null;
            WindGust         = Parameters.ContainsKey($"windGust") ? WindGust = Parameters["windGust"] : null;
        }

        public override void Initialize(JsonElement element)
        {
            base.Initialize(element);

            HailSize         = Parameters != null && Parameters.TryGetValue("hailSize", out string parameter) ? parameter : null;
            TornadoDetection = Parameters != null && Parameters.TryGetValue("tornadoDetection", out string parameter1) ? parameter1 : null;
            WindGust         = Parameters != null && Parameters.TryGetValue("windGust", out string parameter2) ? parameter2 : null;
        }
    }

    public class TornadoWarningModel : SevereThunderstormWarningModel
    {
        public override Color AlertColor => Color.Red;
        public override Color SecondaryColor => Color.White;
    }

    public class FlashFloodWarningModel : AlertModel
    {
        public override Color AlertColor => Color.LightGreen;
        public override Color SecondaryColor => Color.Black;
    }

    public class FloodWarningModel : AlertModel
    {
        public override Color AlertColor => Color.DarkGreen;
        public override Color SecondaryColor => Color.White;
    }

    public class BlizzardWarningModel : AlertModel
    {
        public override Color AlertColor => Color.RoyalBlue;
        public override Color SecondaryColor => Color.White;
    }

    public class SpecialWeatherStatementModel : AlertModel
    {
        public override Color AlertColor => Color.SandyBrown;
        public override Color SecondaryColor => Color.Black;
    }