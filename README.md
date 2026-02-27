# Thunder

## vMix Location JSON Data Source

Thunder exposes a local location overlay endpoint for vMix table ingestion:

- URL: `http://127.0.0.1:8787/api/v1/vmix/location`
- Format: JSON array with exactly one flattened row object.

Recommended vMix Data Source refresh interval: **3000–5000 ms**.

Config in `ThunderApp/appsettings.json`:

- `Mapbox:AccessToken`
- `Nominatim:UserAgent`
- `Nominatim:BaseUrl`

You can also paste your Mapbox token in-app via **View -> Units and Theme -> Mapbox Token** (saved in unit settings).

If Mapbox is unavailable or token is blank, Thunder falls back to Nominatim.
