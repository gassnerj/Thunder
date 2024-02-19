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
        private readonly ConcurrentBag<T> _pool;

        public ObjectPool()
        {
            _pool = new ConcurrentBag<T>();
        }

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

        public ObservableCollection<IAlert> Alerts { get; set; } = new();
        public event EventHandler<AlertMessageEventArgs> AlertMessage;
        public event EventHandler<AlertIssuedEventArgs> AlertIssued;

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
            /*NewAlertCount = 0;
            OnAlertMessage(new AlertMessageEventArgs($"Fetching alerts..."));

            ApiFetcher apiFetcher   = new ApiFetcherBuilder(url).Build();
            string     jsonResponse = await apiFetcher.FetchData();

            using JsonDocument document = JsonDocument.Parse(jsonResponse);
            JsonElement        features = document.RootElement.GetProperty("features");
            MapFeatures(features);

            if (NewAlertCount < 1) OnAlertMessage(new AlertMessageEventArgs("No new alerts."));

            //PurgeAlerts();

            OnAlertMessage(new AlertMessageEventArgs($"Total alerts: {Alerts.Count}"));
            lock (Alerts)
            {
                _runCount++;
            }*/
        }


        private void MapFeatures(JsonElement features)
        {
            var      alertIds    = new ConcurrentDictionary<string, bool>();
            DateTime currentTime = DateTime.Now;

            var alertTypes = new HashSet<Type>()
            {
                typeof(SevereThunderstormWarning),
                typeof(TornadoWarning),
                typeof(SpecialWeatherStatement)
            };

            var featurePool = new ObjectPool<Feature>();
            var svrPool     = new ObjectPool<SevereThunderstormWarning>();
            var torPool     = new ObjectPool<TornadoWarning>();
            var spclPool    = new ObjectPool<SpecialWeatherStatement>();
            //var blzPool = new ObjectPool<BlizzardWarning>();
            //var fldPool = new ObjectPool<FloodWarning>();
            //var flshfldPool = new ObjectPool<FlashFloodWarning>();

            var partitioner = Partitioner.Create(features.EnumerateArray());

            Parallel.ForEach(partitioner, feature =>
            {
                Feature f = featurePool.GetObject();

                try
                {
                    f.ID       = feature.GetProperty("id").ToString();
                    f.Type     = feature.GetProperty("type").ToString();
                    f.Geometry = new Geometry(feature);

                    var alertEvent = feature.GetProperty("properties").GetProperty("event").ToString();

                    //switch (alertEvent)
                    //{
                    //    case "Severe Thunderstorm Warning":
                    //        SevereThunderstormWarning severeThunderstormWarning = svrPool.GetObject();
                    //        severeThunderstormWarning?.Initialize(feature);
                    //        f.Alert = severeThunderstormWarning;
                    //        break;
                    //    case "Tornado Warning":
                    //        TornadoWarning tornadoWarning = torPool.GetObject();
                    //        tornadoWarning?.Initialize(feature);
                    //        f.Alert = tornadoWarning;
                    //        break;
                    //    case "Special Weather Statement":
                    //        SpecialWeatherStatement specialWeatherStatement = spclPool.GetObject();
                    //        specialWeatherStatement?.Initialize(feature);
                    //        f.Alert = specialWeatherStatement;
                    //        break;
                    //    default:
                    //        f.Alert = new Alert(feature);
                    //        break;
                    //}


                    f.Alert = alertEvent switch
                    {
                        "Severe Thunderstorm Warning" => new SevereThunderstormWarning(feature),
                        "Tornado Warning"             => new TornadoWarning(feature),
                        "Blizzard Warning"            => new BlizzardWarning(feature),
                        "Flood Warning"               => new FloodWarning(feature),
                        "Flash Flood Warning"         => new FlashFloodWarning(feature),
                        "Special Weather Statement"   => new SpecialWeatherStatement(feature),
                        _                             => new Alert(feature)
                    };

                    if (f.Alert != null && !alertIds.TryAdd(f.Alert.ID, true)) return;
                    if (f.Alert != null && f.Alert.Expires <= currentTime) return;

                    // temporarily adding a condition so that only certain alerts are added.
                    if (f.Alert != null && !alertTypes.Contains(f.Alert.GetType())) return;
                    lock (Alerts)
                    {
                        Alerts.Add(f.Alert);
                        if (_runCount > 0)
                            OnAlertIssued(new AlertIssuedEventArgs(f.Alert));
                    }
                }
                finally
                {
                    featurePool.ReturnObject(f);
                }
            });
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
        public string ID { get; set; }
        public string Type { get; set; }
        public Geometry Geometry { get; set; }
        public Alert Alert { get; set; }

        public Feature()
        {
        }

        public Feature(JsonElement feature)
        {
            ID   = feature.GetProperty("id").ToString();
            Type = feature.GetProperty("type").ToString();
        }

        public void Reset()
        {
            ID       = null;
            Type     = null;
            Geometry = null;
            Alert    = null;
        }

        public virtual void Initialize()
        {
        }

        public virtual void Initialize(JsonElement element)
        {
            ID   = element.GetProperty("id").ToString();
            Type = element.GetProperty("type").ToString();
        }
    }

    public class Geometry
    {
        public string Type { get; set; }
        public Polygon Polygon { get; set; }

        public Geometry()
        {
        }

        public Geometry(JsonElement feature)
        {
            try
            {
                Type = feature.GetProperty("geometry").GetProperty("type").GetString();

                JsonElement.ArrayEnumerator coordinatesArray = feature.GetProperty("geometry").GetProperty("coordinates").EnumerateArray();
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
            return (from coordinate in coordinatesArray
                from c in coordinate.EnumerateArray()
                let latitude = c[0].GetDouble()
                let longitude = c[1].GetDouble()
                select new Coordinate() { Latitude = latitude, Longitude = longitude }).ToList();
        }
    }

    public class Polygon
    {
        public List<Coordinate> Coordinates { get; set; }
    }

    public class Coordinate
    {
        public Coordinate()
        {
            
        }
        public Coordinate(string longitude, string latitude)
        {
            Latitude  = Convert.ToDouble(latitude);
            Longitude = Convert.ToDouble(longitude);
        }

        public Coordinate(double longitude, double latitude)
        {
            Latitude  = latitude;
            Longitude = longitude;
        }
        public double Latitude { get; init; }
        public double Longitude { get; init; }
    }

    public class Alert : Feature, IAlert
    {
        private Color _color;
        private string _description;
        public string URL { get; set; }
        public new string Type { get; set; }
        public new string ID { get; set; }
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

        public Alert()
        {
        }

        public Alert(JsonElement feature)
        {
            URL               = feature.GetProperty("properties").GetProperty("@id").GetString();
            Type              = feature.GetProperty("properties").GetProperty("@type").GetString();
            ID                = feature.GetProperty("properties").GetProperty("id").GetString();
            AreaDescription   = feature.GetProperty("properties").GetProperty("areaDesc").GetString();
            GeoCode           = new GeoCode();
            AffectedZonesUrls = null;
            References        = null;
            Sent              = ISO8601Parse(feature.GetProperty("properties").GetProperty("sent").GetString());
            Effective         = ISO8601Parse(feature.GetProperty("properties").GetProperty("effective").GetString());
            Onset             = ISO8601Parse(feature.GetProperty("properties").GetProperty("onset").GetString());
            Expires           = ISO8601Parse(feature.GetProperty("properties").GetProperty("expires").GetString());
            Ends              = ISO8601Parse(feature.GetProperty("properties").GetProperty("ends").GetString());
            Status            = feature.GetProperty("properties").GetProperty("status").GetString();
            MessageType       = feature.GetProperty("properties").GetProperty("messageType").GetString();
            Severity          = feature.GetProperty("properties").GetProperty("severity").GetString();
            Certainty         = feature.GetProperty("properties").GetProperty("certainty").GetString();
            Urgency           = feature.GetProperty("properties").GetProperty("urgency").GetString();
            Event             = feature.GetProperty("properties").GetProperty("event").GetString();
            Sender            = feature.GetProperty("properties").GetProperty("sender").GetString();
            SenderName        = feature.GetProperty("properties").GetProperty("senderName").GetString();
            Headline          = feature.GetProperty("properties").GetProperty("headline").GetString();
            Description       = feature.GetProperty("properties").GetProperty("description").GetString();
            Instruction       = feature.GetProperty("properties").GetProperty("instruction").GetString();
            Response          = feature.GetProperty("properties").GetProperty("response").GetString();
            Category          = feature.GetProperty("properties").GetProperty("category").GetString();
            Parameters        = new Dictionary<string, string>();

            foreach (JsonProperty param in feature.GetProperty("properties").GetProperty("parameters").EnumerateObject())
            {
                Parameters.Add(param.Name, param.Value[0].ToString());
            }
            foreach (JsonElement geoCode in feature.GetProperty("properties").GetProperty("geocode").GetProperty("UGC").EnumerateArray())
            {
                GeoCode.UGCCodes.Add(geoCode.GetString());
            }
            foreach (JsonElement sameCode in feature.GetProperty("properties").GetProperty("geocode").GetProperty("SAME").EnumerateArray())
            {
                GeoCode.SAMECodes.Add(sameCode.GetString());
            }
        }
    }

    public class SevereThunderstormWarning : Alert
    {
        public string HailSize { get; set; }
        public string WindGust { get; set; }
        public string TornadoDetection { get; set; }

        public override Color AlertColor => Color.Yellow;

        public SevereThunderstormWarning()
        {
        }

        public SevereThunderstormWarning(JsonElement feature) : base(feature)
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

    public class TornadoWarning : SevereThunderstormWarning
    {
        public override Color AlertColor => Color.Red;
        public override Color SecondaryColor => Color.White;

        public TornadoWarning()
        {
        }

        public TornadoWarning(JsonElement feature) : base(feature)
        {
        }
    }

    public class FlashFloodWarning : Alert
    {
        public override Color AlertColor => Color.LightGreen;
        public override Color SecondaryColor => Color.Black;

        public FlashFloodWarning()
        {
        }

        public FlashFloodWarning(JsonElement feature) : base(feature)
        {
        }
    }

    public class FloodWarning : Alert
    {
        public override Color AlertColor => Color.DarkGreen;
        public override Color SecondaryColor => Color.White;

        public FloodWarning()
        {
        }

        public FloodWarning(JsonElement feature) : base(feature)
        {
        }
    }

    public class BlizzardWarning : Alert
    {
        public override Color AlertColor => Color.RoyalBlue;
        public override Color SecondaryColor => Color.White;

        public BlizzardWarning()
        {
        }

        public BlizzardWarning(JsonElement feature) : base(feature)
        {
        }
    }

    public class SpecialWeatherStatement : Alert
    {
        public override Color AlertColor => Color.SandyBrown;
        public override Color SecondaryColor => Color.Black;

        public SpecialWeatherStatement()
        {
        }

        public SpecialWeatherStatement(JsonElement feature) : base(feature)
        {
        }

        public override void Initialize(JsonElement element)
        {
            base.Initialize(element);
        }
    }

    public class GeoCode
    {
        public List<string> UGCCodes { get; set; }
        public List<string> SAMECodes { get; set; }

        public GeoCode()
        {
            UGCCodes  = new List<string>();
            SAMECodes = new List<string>();
        }
    }

    public class State
    {
        public string Name { get; set; }
        public string Abbreviation { get; set; }
        public string FIPSCode { get; set; }
    }

    public class County : State
    {
        public new string Name { get; set; }
        public string UGCCode { get; set; }
    }

    public class AlertMessageEventArgs : EventArgs
    {
        public string Message { get; set; }

        public AlertMessageEventArgs(string message)
        {
            Message = message;
        }
    }

    public class AlertIssuedEventArgs : EventArgs
    {
        public IAlert Alert { get; set; }

        public AlertIssuedEventArgs(Alert alert)
        {
            Alert = alert;
        }
    }

    public static class StringExtension
    {
        public static string ConvertLineBreaksToHtml(this string str)
        {
            return str != null ? str.Replace("\n", "<br/>") : string.Empty;
        }
    }
}