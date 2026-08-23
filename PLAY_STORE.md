# Publishing to the Google Play Store

Play Store submission was out of scope for v1 — the app stops at a signed, installable release
build (see `README.md` for how that's produced and where the keystore lives). This is what's
actually left, in the order it matters.

Requirements below were verified against Google's documentation on 2026-08-23. Play policies
change often — re-check the linked pages at submission time.

---

## 0. The thing that will cost you the most time: account type

This decides whether you can ship in days or weeks, so decide it first.

**Personal account** — cheap and quick to open, but any personal account created after
2023-11-13 must **run a closed test with at least 12 testers, opted in continuously for at least
14 days**, before you can even apply for production access. Twelve real Google accounts, actually
opted in, for two solid weeks. You then apply for production access and answer questions about
your testing.
([source](https://support.google.com/googleplay/android-developer/answer/14151465))

**Organization account** — Google's 12-tester rule is written as applying to *personal* accounts,
so an organization account appears to avoid it. The trade-off is that an organization account
requires a **D-U-N-S number** for business verification, which is free from Dun & Bradstreet but
can take days to weeks to be issued.
([source](https://support.google.com/googleplay/android-developer/answer/13628312))

Given this app is built for a company that already has a CUI, the organization route is likely
both more appropriate and faster overall — but **confirm the testing requirement for org accounts
with Google before paying**, because Google's help pages don't state the exemption explicitly and
getting this wrong costs weeks either way.

Either way: one-time $25 USD registration fee, plus identity verification.

---

## 1. Fix these before you submit

These are real blockers in the current build, not paperwork:

- **No physical-device testing has ever been done.** Every release so far was verified only on an
  emulator. Real GPS behaves differently — drift, tunnels, signal loss, battery drain, and
  Android's aggressive background-process killing on some OEM builds (Xiaomi, Samsung, Huawei are
  notorious) directly affect this app's core tracking loop. Do a real drive test before exposing
  this to anyone.

- **A privacy policy must be publicly hosted at a URL.** Required even though the app sends
  nothing anywhere. GitHub Pages is fine. Content is simple and honest for this app: all data
  (GPS, photos, fill-ups) stays in local storage on the device; no analytics; no third-party
  sharing; the only outbound traffic is Android's system geocoder when you search an address.

---

## 2. Store listing assets

- App icon **512×512** PNG, feature graphic **1024×500**.
- At least 2 phone screenshots (JPEG/PNG) — take them from a real run with a filled-in Vehicle
  Profile, not the empty-state screens.
- Short description, full description, app category, default language (Romanian makes sense given
  the UI).

---

## 3. Required declarations in Play Console

**Data safety form.** Declare what leaves the device. For this app as built: nothing is collected
or shared off-device — GPS points, receipt photos, and fill-up records live only in the local
SQLite database and app-local file storage. The app bundles no Maps SDK; the only network call
is to Android's own geocoder when searching an address.

**Foreground service declaration — this app definitely needs one.** The app targets API 36 and
declares `FOREGROUND_SERVICE_LOCATION` for the trip tracker, so Play requires a declaration in
**App content** covering: what the foreground service does, what breaks for the user if the system
defers it, the use case, and **a link to a video demonstrating the feature** — showing the exact
steps a user takes to trigger it. Budget time to record that video (screen recording of toggling
tracking in Settings and driving a route is enough).
([source](https://support.google.com/googleplay/android-developer/answer/13392821))

**Location permission declaration.** Expect to justify `ACCESS_FINE_LOCATION`. The app
deliberately does *not* request `ACCESS_BACKGROUND_LOCATION` (it uses a foreground service with a
persistent notification instead), which avoids the much stricter background-location review — keep
it that way.

---

## 4. Target API level

Play requires **API 36 (Android 16) or higher** for new apps and updates as of 2026-08-31.
**This app already targets API 36**, so it's compliant — verify it's still current at submission
time, since Google raises this roughly annually.
([source](https://support.google.com/googleplay/android-developer/answer/11926878))

---

## 5. Upload an AAB, not an APK

Play requires an Android App Bundle. The release process in `README.md` produces both — upload
the `.aab`, keep the `.apk` for direct-install distribution outside the Store.

Bump `ApplicationVersion` (Android `versionCode`) in `FoaieDeParcurs.App.csproj` for **every**
upload — Play rejects a re-used version code outright.

---

## 6. Signing

Enroll in **Play App Signing**. Google holds the app signing key and re-signs your uploads,
which means losing the local keystore stops being catastrophic — you can request an upload-key
reset instead of permanently losing the ability to update the app. Back up
`keystore/foaiedeparcurs-release.keystore` and its password regardless (see `README.md`).
