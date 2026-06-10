# OBD-II primer

A short domain reference so contributors (and agents) don't have to relearn the
protocol from scratch. This is a practical summary, not the full standard.

## What is OBD-II?

**On-Board Diagnostics II** is a standardized system (mandated in the US for
cars since 1996, EU "EOBD" from ~2001) that exposes engine and emissions data
through a 16-pin **DLC** connector, usually under the dashboard.

Underneath OBD-II runs one of several signaling protocols. Modern cars (2008+ in
the US) use **CAN bus (ISO 15765-4)**. Older cars may use ISO 9141-2, KWP2000
(ISO 14230), or J1850 PWM/VPWM. An ELM327 adapter hides these differences and
auto-detects the protocol for us.

## ELM327 adapters

The de-facto standard interface chip. We talk to it as a serial device (USB,
Bluetooth RFCOMM, or TCP for Wi-Fi clones) using ASCII commands terminated by
carriage return (`\r`). It echoes a prompt `>` when ready for the next command.

Two command categories:

- **AT commands** — configure the adapter itself.
- **OBD commands** — hex digits forwarded to the vehicle.

Common init sequence:

| Command | Meaning |
|---------|---------|
| `ATZ`   | Reset the adapter |
| `ATE0`  | Echo off (don't repeat our command back) |
| `ATL0`  | Linefeeds off |
| `ATS0`  | Spaces off (compact responses) |
| `ATH1`  | Headers on (needed for multi-ECU / ISO-TP work) |
| `ATSP0` | Set protocol = auto-detect |

## OBD-II requests: modes and PIDs

A request is a **mode** (a.k.a. service) byte plus, for most modes, a **PID**.

| Mode | Purpose |
|------|---------|
| `01` | Show current live data |
| `02` | Show freeze-frame data (snapshot when a DTC was set) |
| `03` | Show stored Diagnostic Trouble Codes (DTCs) |
| `04` | Clear DTCs and stored values (**write — opt-in only**) |
| `06` | On-board monitoring test results |
| `07` | Show pending DTCs |
| `09` | Vehicle information (VIN, calibration IDs) |
| `0A` | Permanent DTCs |

**PIDs** (Parameter IDs) select what to read within a mode. Example request for
live engine RPM: mode `01`, PID `0C` → send `010C`.

### Example: engine RPM (PID 010C)

- Request: `010C`
- Response (headers off, spaces off): `410C0CF8`
  - `41` = `0x40 + mode 01` (positive response)
  - `0C` = echoed PID
  - `0C F8` = data bytes A, B
- Formula: `((A * 256) + B) / 4` = `((12 * 256) + 248) / 4` = **825 rpm**

### A few common Mode 01 PIDs

| PID | Name | Bytes | Formula | Unit |
|-----|------|-------|---------|------|
| `04` | Engine load | 1 | `A * 100 / 255` | % |
| `05` | Coolant temperature | 1 | `A - 40` | °C |
| `0C` | Engine RPM | 2 | `((A*256)+B)/4` | rpm |
| `0D` | Vehicle speed | 1 | `A` | km/h |
| `0F` | Intake air temp | 1 | `A - 40` | °C |
| `10` | MAF air flow | 2 | `((A*256)+B)/100` | g/s |
| `11` | Throttle position | 1 | `A * 100 / 255` | % |
| `2F` | Fuel level | 1 | `A * 100 / 255` | % |

### Supported-PID discovery

PIDs `00`, `20`, `40`, … return a bitmask of which PIDs in the next range the
vehicle supports. We query these first to know what to offer the user.

## Manufacturer notes: Toyota / Lexus

Toyota and Lexus share a diagnostic platform. Most models from roughly 2008
onward use **ISO 15765-4 CAN (11-bit, 500 kbaud)** — ELM327 protocol `6`
(`ATSP6`). Selecting it explicitly (via `--make toyota`/`--make lexus`) avoids
the adapter's slower auto-detection. Older Toyota/Lexus may use ISO 9141-2
(`ATSP3`) or KWP2000; fall back with `--protocol auto` or `--protocol iso9141`.

Generic OBD-II modes (01/03/04/07/09) used by this tool are standardized across
makes, so reading status, reading DTCs, and clearing DTCs work regardless of
manufacturer.

### SRS / airbag via UDS

The airbag system (**SRS**) is *not* covered by generic OBD-II — it lives on its
own ECU and is accessed with **UDS (ISO 14229) over CAN (ISO 15765)**. The tool
addresses the SRS module by CAN headers and uses:

- **Read codes:** ReadDTCInformation, service `0x19`, subfunction `0x02`
  (reportDTCByStatusMask, mask `0xFF`). Positive response `0x59`. Each DTC is
  **3 bytes** (so `B0100` + a failure-type suffix, e.g. `B0100-13`) plus a
  status byte.
- **Count:** subfunction `0x01` (reportNumberOfDTCByStatusMask).
- **Clear codes:** ClearDiagnosticInformation, service `0x14`, group `0xFFFFFF`.
  Positive response `0x54`.

**Toyota/Lexus caveat:** the SRS CAN request/response headers vary by model and
year and aren't publicly standardized. Defaults are `7B0`/`7B8`; override with
`--srs-tx` / `--srs-rx`. Validate on the real vehicle. And per the safety notes
below, never clear SRS codes before the fault is physically repaired.

## US emissions inspections (I/M readiness)

OBD-II exists in the USA mainly for emissions enforcement, so the inspection-
relevant data is standardized:

- **Mode 01 PID 01 — Monitor status since DTCs cleared.** 4 bytes (A,B,C,D):
  - A: bit 7 = MIL ("check engine") commanded on; bits 6-0 = number of confirmed DTCs.
  - B: continuous monitors — bit 3 selects spark (0) vs compression/diesel (1);
    bits 0-2 = misfire / fuel-system / comprehensive-components *supported*;
    bits 4-6 = the same monitors *incomplete* (1 = not ready).
  - C/D: non-continuous monitors — C = supported bitmask, D = incomplete bitmask
    (catalyst, heated catalyst, EVAP, secondary air, O2 sensor, O2 heater, EGR,
    …; the names differ for diesels).
  A vehicle generally passes a US OBD inspection when the **MIL is off** and at
  most **one** monitor is "not ready" (two for pre-2000 vehicles) — but exact
  rules vary by state.
- **Mode 0A — Permanent DTCs.** Cannot be cleared by a scan tool or battery
  disconnect; they clear only after the vehicle's own monitors confirm the repair.
  This blocks "clear codes then drive to the test" cheating, so inspections read them.
- **Mode 09 PID 02 — VIN.** Used for registration, recall lookups, and emissions
  records. The response is multi-frame ASCII after a `49 02` marker.

## Diagnostic Trouble Codes (DTCs)

Mode `03` returns stored codes as 2-byte values decoded into the familiar
`P0420` form:

- First two bits → letter: `P` powertrain, `C` chassis, `B` body, `U` network.
- Remaining bits → 4 hex/decimal digits.

Example: bytes `01 33` → `P0133`.

## ISO-TP (multi-frame) responses

Responses longer than one CAN frame (8 bytes) — e.g. VIN in mode `09` — are
split using **ISO 15765-2 (ISO-TP)**: a first frame announces total length, then
consecutive frames carry the rest. The protocol layer reassembles these.

## Safety notes

- **Reading** (modes 01/02/03/07/09/0A) is non-intrusive.
- **Clearing** (mode 04) erases DTCs *and* freeze-frame/readiness data — only on
  explicit user request, never by default.
- Never send commands while the vehicle is in motion.

## References

- ELM327 datasheet (Elm Electronics) — AT command set.
- ISO 15765-4 (CAN), ISO 15765-2 (ISO-TP), ISO 14230 (KWP2000), ISO 9141-2.
- SAE J1979 — OBD-II modes & standard PIDs.
- SAE J2012 — DTC definitions.
