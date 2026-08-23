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
  FoaieDeParcurs.Pdf     - PdfSharp rendering of the Foaie de Parcurs document
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

No Google Maps SDK or API key is required — address lookup uses Android's own system Geocoder,
with a bundled offline Romanian city list as the fallback when there's no network.

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
3. For a signed release build, see [Release signing](#release-signing) below.

## Release signing

Release builds (APK and AAB) must be signed with a release keystore. **Never commit the keystore
or its passwords** — `/keystore/` is gitignored in its entirety for exactly this reason.

### 1. Generate the keystore (one-time)

```
keytool -genkeypair -v -storetype PKCS12 \
  -keystore keystore/foaiedeparcurs-release.keystore \
  -alias foaiedeparcurs \
  -keyalg RSA -keysize 2048 -validity 10000
```

`keytool` defaults to a PKCS12 keystore, and PKCS12 requires the store password and the key
password to be identical — it silently ignores a separately-specified `-keypass` and reuses the
store password for both, so there is really only one secret to keep track of. Write the password
down somewhere safe (a password manager, not another repo) immediately after generating it —
`keystore/RELEASE_CREDENTIALS_DO_NOT_COMMIT.txt` is a reasonable local scratch copy, but it is
gitignored and **not a backup**.

**Losing this keystore (and its password) means losing the ability to ever update the app under
the same signing identity again** — you would have to publish it as a brand-new app. Back it up
somewhere durable and private.

### 2. Build the signed APK / AAB

```
dotnet build src/FoaieDeParcurs.App/FoaieDeParcurs.App.csproj \
  -f net10.0-android -c Release \
  -t:SignAndroidPackage \
  -p:AndroidPackageFormat=apk \
  -p:AndroidSigningKeyStore=<path-to-keystore> \
  -p:AndroidSigningKeyAlias=foaiedeparcurs \
  -p:AndroidSigningStorePass=<password> \
  -p:AndroidSigningKeyPass=<password>
```

Swap `-p:AndroidPackageFormat=aab` to produce an Android App Bundle instead (required for Play
Store uploads — see [`PLAY_STORE.md`](PLAY_STORE.md)). The signed output lands under
`src/FoaieDeParcurs.App/bin/Release/net10.0-android/` as
`com.mzivojin.foaiedeparcurs-Signed.apk` / `.aab`.

### 3. Install and verify

```
adb install -r src/FoaieDeParcurs.App/bin/Release/net10.0-android/com.mzivojin.foaiedeparcurs-Signed.apk
```

## Configuration

On first launch, open **Settings** and fill in:

- Company name, CUI, driver name, vehicle plate/make/model, declared fuel consumption norm
- Default email recipient and subject/body template
- Reporting cadence (per-fill-up or monthly)

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
