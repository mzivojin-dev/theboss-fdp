# Publishing to the Google Play Store

Play Store submission is explicitly out of scope for v1 (see the spec) — the developer doesn't
yet have a Google Play Developer account. This app stops at a signed, installable release build
(see `README.md` for how that's produced and where the keystore lives). This note lists what's
left for whenever that account exists.

## 1. Google Play Developer account

- Register at https://play.google.com/console/signup (one-time $25 USD registration fee).
- Complete identity verification (can take a few days for individual accounts).

## 2. App listing basics

- Create the app in Play Console, set its default language and app/game category.
- Short description, full description, app icon (512x512), feature graphic (1024x500).
- Phone screenshots (minimum 2, JPEG/PNG) — capture from a real run of the signed build.
- Privacy policy URL — required even though this app sends no data anywhere. A one-page
  statement ("all data stays on-device; no analytics; no third-party sharing") hosted anywhere
  public (e.g. a GitHub Pages page) satisfies this.

## 3. Data safety form

Play Console requires a declared data-safety form before publishing. Given this app's design
(spec: "no analytics, no third-party data sharing", everything local):

- Data collected: **none** shared off-device. GPS location, photos, and fill-up data are stored
  only in the app's local SQLite database and local file storage.
- The Google Maps SDK (if a real API key is configured — see `README.md`) does make network
  requests to Google for map tiles/geocoding; disclose this per Google's Maps Platform data-use
  terms if a key is added before publishing.

## 4. Production AAB, not APK

Play Store requires an Android App Bundle (`.aab`), not an APK. The release build process in
`README.md` already produces both — use the `.aab` for the Play Console upload, keep the `.apk`
for direct-install testing/distribution outside the Store.

## 5. Closed testing track first

Before any production release, run at least one closed testing track with a small group of
testers (Play Console requires this for new developer accounts as of their current policy) —
add your own account and/or a few trusted testers by email, upload the `.aab`, and confirm the
app installs and runs correctly from the Play Store test link before promoting to production.

## 6. Target API level / Play policy compliance

Re-check Play Console's current target API level requirement at submission time (Google raises
this roughly annually) — bump `$(SupportedOSPlatformVersion)` / target SDK in
`FoaieDeParcurs.App.csproj` if the installed workload's default has fallen behind by then.

## 7. Signing

Play App Signing is strongly recommended: upload the release keystore's signing key to Google
during the first submission, and Google re-signs the app for distribution with a Google-managed
key — this protects against permanently losing the ability to update the app if the local
keystore is ever lost. See `README.md`'s keystore section for the backup warning regardless.
