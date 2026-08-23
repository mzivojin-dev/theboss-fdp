# Foaie de Parcurs — Fill-Up Tracker

An Android app (.NET MAUI) for a single company driver in Romania. It tracks GPS trails while
driving between fuel fill-ups, auto-builds the route log from that trail, and generates the
legally required "Foaie de Parcurs" as a PDF, handed off to your email app to send.

Everything is local-first: no backend, no cloud sync, no accounts. See the spec at
[docs/agents](docs/agents) and the tracked work at
[GitHub Issues](https://github.com/mzivojin-dev/theboss-fdp/issues) for the full design.

## Solution layout

```
FoaieDeParcurs.sln
src/
  FoaieDeParcurs.App     - the MAUI Android app (UI, platform services, DI wiring)
  FoaieDeParcurs.Core    - pure domain logic: entities, TripLedger (route derivation,
                            verification, document assembly). No SQLite/Android/PDF dependency.
  FoaieDeParcurs.Data    - EF Core (Sqlite) persistence for the Core entities
  FoaieDeParcurs.Pdf     - QuestPDF rendering of the Foaie de Parcurs document
tests/
  FoaieDeParcurs.Tests   - xUnit tests for Core, Data, and Pdf
```

`FoaieDeParcurs.App` currently targets `net10.0-android` only. iOS can be added later by
appending `net10.0-ios` to its `<TargetFrameworks>` and adding a `Platforms/iOS` folder — the
rest of the SingleProject MAUI structure is already platform-agnostic.

## Prerequisites

- [.NET SDK 10.0](https://dotnet.microsoft.com/download) or later, with the `maui-android` workload:
  ```
  dotnet workload install maui-android
  ```
- Android SDK (platform 36, build-tools 36.0.0, an emulator system image) — installed automatically
  alongside the MAUI workload, or via Android Studio's SDK Manager.
- A Google Maps SDK API key for the map screens (see [Configuration](#configuration) below) — the
  app remains usable without one, falling back to an offline city lookup.

## Running on the emulator

1. Restore local tools once: `dotnet tool restore`
2. Build and run:
   ```
   dotnet build src/FoaieDeParcurs.App/FoaieDeParcurs.App.csproj -f net10.0-android -t:Run
   ```
   or open `FoaieDeParcurs.sln` in Visual Studio / Rider and run the `FoaieDeParcurs.App`
   project with an Android emulator selected as the target device.
3. To simulate driving, use the emulator's **Extended Controls → Location** panel to play back a
   route — the app's foreground-service tracker picks up mock locations the same way it would
   real GPS.

## Deploying to a physical device

1. Enable Developer Options and USB debugging on the phone, connect via USB, and confirm it's
   visible: `adb devices`
2. Build and install a Debug APK:
   ```
   dotnet build src/FoaieDeParcurs.App/FoaieDeParcurs.App.csproj -f net10.0-android -t:Run -p:AndroidDeviceType=device
   ```
   or `adb install` a built APK directly from `src/FoaieDeParcurs.App/bin/Debug/net10.0-android/`.
3. For a signed release build, see [`PLAY_STORE.md`](PLAY_STORE.md) for the keystore/signing
   process — a release keystore is required and is **never** committed to this repo.

## Configuration

On first launch, open **Settings** and fill in:

- Company name, CUI, driver name, vehicle plate/make/model, declared fuel consumption norm
- Default email recipient and subject/body template
- Reporting cadence (per-fill-up or monthly)
- Your own Google Maps SDK API key (optional — leave blank to use the offline city-lookup fallback)

## Running the tests

```
dotnet test tests/FoaieDeParcurs.Tests/FoaieDeParcurs.Tests.csproj
```

The test project targets plain `net10.0` (not Android), so it runs on any machine without an
emulator or device attached — this is where the `TripLedger` domain engine, repository
round-trips, and PDF rendering are covered.

## Database migrations

The schema is managed with EF Core migrations, applied automatically on app startup. To add a
new migration after changing an entity in `FoaieDeParcurs.Core`:

```
dotnet ef migrations add <Name> --project src/FoaieDeParcurs.Data --startup-project src/FoaieDeParcurs.Data
```
