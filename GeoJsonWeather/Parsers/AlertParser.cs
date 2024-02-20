using System.Collections.Generic;
using System.Drawing;
using System.Text.Json;
using GeoJsonWeather.Models;

namespace GeoJsonWeather.Parsers;

public class AlertParser : ParserBase, IJsonParser<AlertModel>
{
    public AlertModel GetItem(JsonElement feature)
    {
        return new AlertModel()
        {
            Url               = feature.GetProperty("properties").GetProperty("@id").GetString(),
            Type              = feature.GetProperty("properties").GetProperty("@type").GetString(),
            Id                = feature.GetProperty("properties").GetProperty("id").GetString(),
            AreaDescription   = feature.GetProperty("properties").GetProperty("areaDesc").GetString(),
            GeoCode           = new GeoCode(),
            AffectedZonesUrls = null,
            References        = null,
            Sent              = ISO8601Parse(feature.GetProperty("properties").GetProperty("sent").GetString()),
            Effective         = ISO8601Parse(feature.GetProperty("properties").GetProperty("effective").GetString()),
            Onset             = ISO8601Parse(feature.GetProperty("properties").GetProperty("onset").GetString()),
            Expires           = ISO8601Parse(feature.GetProperty("properties").GetProperty("expires").GetString()),
            Ends              = ISO8601Parse(feature.GetProperty("properties").GetProperty("ends").GetString()),
            Status            = feature.GetProperty("properties").GetProperty("status").GetString(),
            MessageType       = feature.GetProperty("properties").GetProperty("messageType").GetString(),
            Severity          = feature.GetProperty("properties").GetProperty("severity").GetString(),
            Certainty         = feature.GetProperty("properties").GetProperty("certainty").GetString(),
            Urgency           = feature.GetProperty("properties").GetProperty("urgency").GetString(),
            Event             = feature.GetProperty("properties").GetProperty("event").GetString(),
            Sender            = feature.GetProperty("properties").GetProperty("sender").GetString(),
            SenderName        = feature.GetProperty("properties").GetProperty("senderName").GetString(),
            Headline          = feature.GetProperty("properties").GetProperty("headline").GetString(),
            Description       = feature.GetProperty("properties").GetProperty("description").GetString(),
            Instruction       = feature.GetProperty("properties").GetProperty("instruction").GetString(),
            Response          = feature.GetProperty("properties").GetProperty("response").GetString(),
            Category          = feature.GetProperty("properties").GetProperty("category").GetString(),
            Parameters        = AddParameters(feature)
        };
    }

    private static Dictionary<string, string> AddParameters(JsonElement feature)
    {
        var parameters = new Dictionary<string, string>();

        foreach (JsonProperty param in feature.GetProperty("properties").GetProperty("parameters").EnumerateObject())
        {
            parameters.Add(param.Name, param.Value[0].ToString());
        }
        foreach (JsonElement geoCode in feature.GetProperty("properties").GetProperty("geocode").GetProperty("UGC").EnumerateArray())
        {
            GeoCode.UGCCodes.Add(geoCode.GetString());
        }
        foreach (JsonElement sameCode in feature.GetProperty("properties").GetProperty("geocode").GetProperty("SAME").EnumerateArray())
        {
            GeoCode.SAMECodes.Add(sameCode.GetString());
        }
        return parameters;
    }
}

