using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Text.Json;
using GeoJsonWeather.Api;

namespace GeoJsonWeather
{
    public interface IPoolable
    {
        void Reset();
        void Initialize();
    }

    public interface IJsonPoolable : IPoolable
    {
        void Initialize(JsonElement element);
    }

    public class ObjectPool<T> where T : IPoolable, new()
    {
        private readonly ConcurrentBag<T> _pool = [];

        public T GetObject()
        {
            if (!_pool.TryTake(out T o)) return new T();
            o.Reset();
            return o;
        }

        public void ReturnObject(T o)
        {
            _pool.Add(o);
        }
    }

    public class FeatureCollection : IFeatureCollection
    {
        private int _refreshInterval = 1;
        private int _runCount = 0;

        public ObservableCollection<IAlert> Alerts { get; set; } = [];
        public event EventHandler<AlertMessageEventArgs>? AlertMessage;
        public event EventHandler<AlertIssuedEventArgs>? AlertIssued;

        public int RefreshInterval
        {
            get => _refreshInterval;
            set
            {
                if (value < 1) throw new InvalidOperationException("Value must be 1 or greater.");
                _refreshInterval = value;
            }
        }

        public int NewAlertCount { get; set; }

        public async Task FetchData(string url)
        {
            NewAlertCount = 0;
            OnAlertMessage(new AlertMessageEventArgs("Fetching alerts..."));

            // Clear first so UI shows "fresh pull"
            Alerts.Clear();

            ApiFetcher apiFetcher = new ApiFetcherBuilder(url).Build();
            string jsonResponse = await apiFetcher.FetchData();

            using JsonDocument document = JsonDocument.Parse(jsonResponse);
            JsonElement features = document.RootElement.GetProperty("features");

            MapFeatures(features);

            if (NewAlertCount < 1)
                OnAlertMessage(new AlertMessageEventArgs("No new alerts."));

            OnAlertMessage(new AlertMessageEventArgs($"Total alerts: {Alerts.Count}"));

            _runCount++;
        }

        private static readonly Dictionary<string, Func<JsonElement, IAlert>> AlertFactory =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Severe Thunderstorm Warning"] = f => new SevereThunderstormWarning(f),
                ["Tornado Warning"]             = f => new TornadoWarning(f),
                ["Blizzard Warning"]            = f => new BlizzardWarning(f),
                ["Flood Warning"]               = f => new FloodWarning(f),
                ["Flash Flood Warning"]         = f => new FlashFloodWarning(f),
                ["Special Weather Statement"]   = f => new SpecialWeatherStatement(f),
            };

        private static IAlert CreateAlert(JsonElement feature)
        {
            // If anything is missing, just fall back to base Alert.
            try
            {
                var evt = feature.GetProperty("properties").GetProperty("event").GetString();

                if (!string.IsNullOrWhiteSpace(evt) && AlertFactory.TryGetValue(evt, out var ctor))
                    return ctor(feature);

                return new Alert(feature);
            }
            catch
            {
                return new Alert(feature);
            }
        }

        private void MapFeatures(JsonElement features)
        {
            var alertIds = new ConcurrentDictionary<string, bool>();
            DateTime currentTime = DateTime.Now;

            // Collect results thread-safely FIRST, then add to ObservableCollection on this thread.
            var results = new ConcurrentBag<IAlert>();

            var partitioner = Partitioner.Create(features.EnumerateArray());

            Parallel.ForEach(partitioner, feature =>
            {
                try
                {
                    IAlert alert = CreateAlert(feature);

                    // dedupe
                    if (!alertIds.TryAdd(alert.ID, true)) return;

                    // expired
                    if (alert.Expires <= currentTime) return;

                    results.Add(alert);
                }
                catch
                {
                    // ignore malformed alert; keep going
                }
            });

            // Now update ObservableCollection safely on this (caller) thread
            var ordered = results
                .OrderByDescending(a => a.Sent)
                .ToList();

            foreach (IAlert a in ordered)
            {
                Alerts.Add(a);

                // Only fire "issued" after first successful run, like your original behavior
                if (_runCount > 0)
                    OnAlertIssued(new AlertIssuedEventArgs(a));
            }

            NewAlertCount = Alerts.Count;
        }

        public void PurgeAlerts()
        {
            foreach (IAlert alert in from alert in Alerts.ToList()
                     where alert.Expires <= DateTime.Now
                     select alert)
            {
                Alerts.Remove(alert);
            }
        }

        internal protected static DateTime ISO8601Parse(string dateTime)
        {
            bool success = DateTime.TryParse(dateTime, out DateTime date);
            return success ? date : DateTime.Now;
        }

        protected virtual void OnAlertIssued(AlertIssuedEventArgs e)
        {
            AlertIssued?.Invoke(this, e);
        }

        protected virtual void OnAlertMessage(AlertMessageEventArgs e)
        {
            AlertMessage?.Invoke(this, e);
        }
    }

    public class Feature : FeatureCollection, IJsonPoolable
    {
        public string? ID { get; set; }
        public string? Type { get; set; }
        public Geometry? Geometry { get; set; }
        public Alert? Alert { get; set; }

        public Feature() { }

        public Feature(JsonElement feature)
        {
            ID = feature.GetProperty("id").ToString();
            Type = feature.GetProperty("type").ToString();
        }

        public void Reset()
        {
            ID = null;
            Type = null;
            Geometry = null;
            Alert = null;
        }

        public virtual void Initialize() { }

        public virtual void Initialize(JsonElement element)
        {
            ID = element.GetProperty("id").ToString();
            Type = element.GetProperty("type").ToString();
        }
    }

    public class Geometry
    {
        public string? Type { get; set; }
        public Polygon? Polygon { get; set; }

        public Geometry() { }

        public Geometry(JsonElement feature)
        {
            try
            {
                Type = feature.GetProperty("geometry").GetProperty("type").GetString();

                JsonElement.ArrayEnumerator coordinatesArray =
                    feature.GetProperty("geometry").GetProperty("coordinates").EnumerateArray();

                Polygon = new Polygon()
                {
                    Coordinates = GetCoordinates(coordinatesArray)
                };
            }
            catch
            {
                // ignored
            }
        }

        private static List<Coordinate> GetCoordinates(JsonElement.ArrayEnumerator coordinatesArray)
        {
            // GeoJSON coordinates are [lon, lat]
            return (from coordinate in coordinatesArray
                    from c in coordinate.EnumerateArray()
                    let lon = c[0].GetDouble()
                    let lat = c[1].GetDouble()
                    select new Coordinate(lon, lat)).ToList();
        }
    }

    public class Polygon
    {
        public List<Coordinate>? Coordinates { get; set; }
    }

    public class Coordinate
    {
        public Coordinate() { }

        public Coordinate(string longitude, string latitude)
        {
            Latitude = Convert.ToDouble(latitude);
            Longitude = Convert.ToDouble(longitude);
        }

        public Coordinate(double longitude, double latitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public double Latitude { get; init; }
        public double Longitude { get; init; }
    }

    public class Alert : Feature, IAlert
    {
        private Color _color;

        public string? URL { get; set; }
        public new string? Type { get; set; }
        public new string? ID { get; set; }
        public string? AreaDescription { get; set; }
        public GeoCode GeoCode { get; set; } = new();
        public List<string>? AffectedZonesUrls { get; set; }   // <-- NOW POPULATED
        public List<string>? References { get; set; }
        public DateTime Sent { get; set; }
        public DateTime Effective { get; set; }
        public DateTime Onset { get; set; }
        public DateTime Expires { get; set; }
        public DateTime? Ends { get; set; }
        public string? Status { get; set; }
        public string? MessageType { get; set; }
        public string? Category { get; set; }
        public string? Severity { get; set; }
        public string? Certainty { get; set; }
        public string? Urgency { get; set; }
        public string? Event { get; set; }
        public string? Sender { get; set; }
        public string? SenderName { get; set; }
        public string? Headline { get; set; }

        public string? Description { get; set; }
        public string? Instruction { get; set; }
        public string? Response { get; set; }

        public Dictionary<string, string> Parameters { get; set; } = new();
        public List<County>? Counties { get; set; }
        public List<State>? States { get; set; }
        public List<string>? ZipCodes { get; set; }
        public string? GeometryJson { get; set; }

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

        public Alert() { }

        public Alert(JsonElement feature)
        {
            // Raw GeoJSON geometry (works for Polygon + MultiPolygon)
            if (feature.TryGetProperty("geometry", out JsonElement geomEl) &&
                geomEl.ValueKind != JsonValueKind.Null &&
                geomEl.ValueKind != JsonValueKind.Undefined)
            {
                GeometryJson = geomEl.GetRawText();
            }
            else
            {
                GeometryJson = null;
            }

            var props = feature.GetProperty("properties");

            URL = props.GetProperty("@id").GetString();
            Type = props.GetProperty("@type").GetString();
            ID = props.GetProperty("id").GetString();
            AreaDescription = props.GetProperty("areaDesc").GetString();

            Sent = ISO8601Parse(props.GetProperty("sent").GetString() ?? "");
            Effective = ISO8601Parse(props.GetProperty("effective").GetString() ?? "");
            Onset = ISO8601Parse(props.GetProperty("onset").GetString() ?? "");
            Expires = ISO8601Parse(props.GetProperty("expires").GetString() ?? "");

            // Some alerts can have ends = null; your original code would parse null -> now
            string? endsProp = props.TryGetProperty("ends", out JsonElement endsEl) ? endsEl.GetString() : null;
            Ends = string.IsNullOrWhiteSpace(endsProp) ? null : ISO8601Parse(endsProp);

            Status = props.GetProperty("status").GetString();
            MessageType = props.GetProperty("messageType").GetString();
            Severity = props.GetProperty("severity").GetString();
            Certainty = props.GetProperty("certainty").GetString();
            Urgency = props.GetProperty("urgency").GetString();
            Event = props.GetProperty("event").GetString();
            Sender = props.GetProperty("sender").GetString();
            SenderName = props.GetProperty("senderName").GetString();
            Headline = props.GetProperty("headline").GetString();
            Description = props.GetProperty("description").GetString();
            Instruction = props.GetProperty("instruction").GetString();
            Response = props.GetProperty("response").GetString();
            Category = props.GetProperty("category").GetString();

            // ---------------- affectedZones (IMPLEMENTED) ----------------
            // Watches/advisories often have geometry = null. The alert tells you which
            // forecast zones are affected via properties.affectedZones, which you can
            // fetch to obtain zone polygons.
            if (props.TryGetProperty("affectedZones", out JsonElement zonesEl) &&
                zonesEl.ValueKind == JsonValueKind.Array)
            {
                var list = new List<string>();
                foreach (var z in zonesEl.EnumerateArray())
                {
                    var url = z.GetString();
                    if (!string.IsNullOrWhiteSpace(url))
                        list.Add(url);
                }

                AffectedZonesUrls = list.Count > 0 ? list : null;
            }
            else
            {
                AffectedZonesUrls = null;
            }

            // geometry (legacy object model; keep if you still use it elsewhere)
            try
            {
                Geometry = new Geometry(feature);
            }
            catch
            {
                Geometry = null;
            }

            // parameters
            if (props.TryGetProperty("parameters", out JsonElement paramsEl))
            {
                foreach (JsonProperty param in paramsEl.EnumerateObject())
                {
                    if (param.Value.ValueKind == JsonValueKind.Array && param.Value.GetArrayLength() > 0)
                        Parameters[param.Name] = param.Value[0].ToString();
                }
            }

            // geocode
            if (!props.TryGetProperty("geocode", out JsonElement geoEl)) return;

            if (geoEl.TryGetProperty("UGC", out JsonElement ugcEl))
            {
                foreach (JsonElement geoCode in ugcEl.EnumerateArray())
                    GeoCode.UGCCodes.Add(geoCode.GetString() ?? "");
            }

            if (!geoEl.TryGetProperty("SAME", out JsonElement sameEl)) return;
            foreach (JsonElement sameCode in sameEl.EnumerateArray())
                GeoCode.SAMECodes.Add(sameCode.GetString() ?? "");
        }
    }

    public class SevereThunderstormWarning : Alert
    {
        public string? HailSize { get; set; }
        public string? WindGust { get; set; }
        public string? TornadoDetection { get; set; }

        public override Color AlertColor => Color.Yellow;

        public SevereThunderstormWarning() { }

        public SevereThunderstormWarning(JsonElement feature) : base(feature)
        {
            HailSize = Parameters.GetValueOrDefault("hailSize");
            TornadoDetection = Parameters.GetValueOrDefault("tornadoDetection");
            WindGust = Parameters.GetValueOrDefault("windGust");
        }

        public override void Initialize(JsonElement element)
        {
            base.Initialize(element);

            HailSize = Parameters.GetValueOrDefault("hailSize");
            TornadoDetection = Parameters.GetValueOrDefault("tornadoDetection");
            WindGust = Parameters.GetValueOrDefault("windGust");
        }
    }

    public class TornadoWarning : SevereThunderstormWarning
    {
        public override Color AlertColor => Color.Red;
        public override Color SecondaryColor => Color.White;

        public TornadoWarning() { }

        public TornadoWarning(JsonElement feature) : base(feature) { }
    }

    public class FlashFloodWarning : Alert
    {
        public override Color AlertColor => Color.LightGreen;
        public override Color SecondaryColor => Color.Black;

        public FlashFloodWarning() { }
        public FlashFloodWarning(JsonElement feature) : base(feature) { }
    }

    public class FloodWarning : Alert
    {
        public override Color AlertColor => Color.DarkGreen;
        public override Color SecondaryColor => Color.White;

        public FloodWarning() { }
        public FloodWarning(JsonElement feature) : base(feature) { }
    }

    public class BlizzardWarning : Alert
    {
        public override Color AlertColor => Color.RoyalBlue;
        public override Color SecondaryColor => Color.White;

        public BlizzardWarning() { }
        public BlizzardWarning(JsonElement feature) : base(feature) { }
    }

    public class SpecialWeatherStatement : Alert
    {
        public override Color AlertColor => Color.SandyBrown;
        public override Color SecondaryColor => Color.Black;

        public SpecialWeatherStatement() { }
        public SpecialWeatherStatement(JsonElement feature) : base(feature) { }

        public override void Initialize(JsonElement element)
        {
            base.Initialize(element);
        }
    }

    public class GeoCode
    {
        public List<string> UGCCodes { get; set; } = [];
        public List<string> SAMECodes { get; set; } = [];
    }

    public class State
    {
        public string? Name { get; set; }
        public string? Abbreviation { get; set; }
        public string? FIPSCode { get; set; }
    }

    public class County : State
    {
        public new string? Name { get; set; }
        public string? UGCCode { get; set; }
    }

    public class AlertMessageEventArgs(string message) : EventArgs
    {
        public string Message { get; set; } = message;
    }

    public class AlertIssuedEventArgs(IAlert alert) : EventArgs
    {
        public IAlert Alert { get; set; } = alert;
    }

    public static class StringExtension
    {
        public static string ConvertLineBreaksToHtml(this string str)
        {
            return str.Replace("\n", "<br/>");
        }
    }
}