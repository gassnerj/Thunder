using System;
using System.Collections.Generic;

namespace GeoJsonWeather
{
    public interface IAlert
    {
        List<string> AffectedZonesUrls { get; set; }
        string AreaDescription { get; set; }
        string Category { get; set; }
        string Certainty { get; set; }
        List<County> Counties { get; set; }
        string Description { get; set; }
        DateTime Effective { get; set; }
        DateTime? Ends { get; set; }
        string Event { get; set; }
        DateTime Expires { get; set; }
        GeoCode GeoCode { get; set; }
        string Headline { get; set; }
        string ID { get; set; }
        string Instruction { get; set; }
        string MessageType { get; set; }
        DateTime Onset { get; set; }
        Dictionary<string, string> Parameters { get; set; }
        List<string> References { get; set; }
        string Response { get; set; }
        string Sender { get; set; }
        string SenderName { get; set; }
        DateTime Sent { get; set; }
        string Severity { get; set; }
        List<State> States { get; set; }
        string Status { get; set; }
        string Type { get; set; }
        string Urgency { get; set; }
        string URL { get; set; }
        List<string> ZipCodes { get; set; }
    }
}