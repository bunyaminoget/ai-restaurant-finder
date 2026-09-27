import { useState } from "react";
import { NearbyList } from "./components/NearbyList";
import { RestaurantDetailPage } from "./components/RestaurantDetailPage";
import "./App.css";

const DEFAULT_LAT = 41.0082; // İstanbul
const DEFAULT_LNG = 28.9784;

export function App() {
  const [latitudeInput, setLatitudeInput] = useState(String(DEFAULT_LAT));
  const [longitudeInput, setLongitudeInput] = useState(String(DEFAULT_LNG));
  const [formError, setFormError] = useState("");
  const [applied, setApplied] = useState({ latitude: DEFAULT_LAT, longitude: DEFAULT_LNG });
  const [selectedId, setSelectedId] = useState<string>("");
  const [manualId, setManualId] = useState("");

  return (
    <main className="layout">
      <h1>Nearby Eats</h1>

      <section aria-label="Konum seçimi" className="card">
        <form
          onSubmit={(event) => {
            event.preventDefault();
            const latitude = Number(latitudeInput.trim());
            const longitude = Number(longitudeInput.trim());
            if (latitudeInput.trim().length === 0 || Number.isNaN(latitude)) {
              setFormError("Geçerli bir enlem girin.");
              return;
            }
            if (longitudeInput.trim().length === 0 || Number.isNaN(longitude)) {
              setFormError("Geçerli bir boylam girin.");
              return;
            }
            if (latitude < -90 || latitude > 90) {
              setFormError("Latitude must be between -90 and 90.");
              return;
            }
            if (longitude < -180 || longitude > 180) {
              setFormError("Longitude must be between -180 and 180.");
              return;
            }
            setFormError("");
            setApplied({ latitude, longitude });
          }}
        >
          <label>
            Enlem
            <input
              type="number"
              step="any"
              value={latitudeInput}
              onChange={(event) => setLatitudeInput(event.target.value)}
            />
          </label>
          <label>
            Boylam
            <input
              type="number"
              step="any"
              value={longitudeInput}
              onChange={(event) => setLongitudeInput(event.target.value)}
            />
          </label>
          <button type="submit">Yakınlardakileri ara</button>
          {formError && <p role="alert">{formError}</p>}
        </form>

        <form
          aria-label="ID ile detay aç"
          onSubmit={(event) => {
            event.preventDefault();
            setSelectedId(manualId.trim());
          }}
        >
          <label>
            Restoran ID (GUID)
            <input
              type="text"
              value={manualId}
              placeholder="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
              onChange={(event) => setManualId(event.target.value)}
            />
          </label>
          <button type="submit" disabled={manualId.trim().length === 0}>
            Detayı aç
          </button>
        </form>
      </section>

      <div className="columns">
        <section aria-label="Yakındaki restoranlar" className="card">
          <h2>Yakındakiler</h2>
          <NearbyList
            latitude={applied.latitude}
            longitude={applied.longitude}
            selectedId={selectedId}
            onSelect={(id) => {
              setSelectedId(id);
              setManualId(id);
            }}
          />
        </section>

        {selectedId ? (
          <RestaurantDetailPage restaurantId={selectedId} onBack={() => setSelectedId("")} />
        ) : (
          <section aria-label="Restoran detayı" className="card">
            <p role="status">Detayı görmek için listeden bir restoran seçin veya bir ID girin.</p>
          </section>
        )}
      </div>
    </main>
  );
}
