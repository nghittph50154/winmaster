# WinMaster v1.0 — Test Plan

## Overview

Manual and automated test coverage for WinMaster v1.0.0.

---

## 1. Startup Tests

| ID | Test | Expected | Status |
|----|------|----------|--------|
| T01 | WinMaster launches | Window appears, Install tab active | ☐ |
| T02 | App cards appear after load | 58 app cards shown in categories | ☐ |
| T03 | Window resize | UI reflows, no overflow | ☐ |
| T04 | Window minimize | Window minimizes to taskbar | ☐ |
| T05 | Window maximize | Window fills screen | ☐ |

---

## 2. Navigation Tests

| ID | Test | Expected | Status |
|----|------|----------|--------|
| T06 | Click INSTALL tab | InstallView shown | ☐ |
| T07 | Click TWEAKS tab | PlaceholderView shown (Tweaks) | ☐ |
| T08 | Click SYSTEM TOOLS tab | PlaceholderView shown (System Tools) | ☐ |
| T09 | Switch back to INSTALL | App cards still visible | ☐ |

---

## 3. Search Tests

| ID | Test | Expected | Status |
|----|------|----------|--------|
| T10 | Type "python" | Python 3, Python 3.12.8 shown | ☐ |
| T11 | Type "git" | Git, GitHub Desktop shown | ☐ |
| T12 | Type "capcut" | CapCut, CapCut 1.5, 7.0, 7.7 shown | ☐ |
| T13 | Clear search | All apps visible | ☐ |
| T14 | Type non-matching text | No apps shown, categories hidden | ☐ |

---

## 4. Selection Tests

| ID | Test | Expected | Status |
|----|------|----------|--------|
| T15 | Click a card | Card selected, count updates | ☐ |
| T16 | Click checkbox | Card selected | ☐ |
| T17 | Click Select All | All visible apps selected | ☐ |
| T18 | Click Clear | All deselected, count = 0 | ☐ |
| T19 | Selected count display | "Selected: N applications" | ☐ |
| T20 | RUN disabled when 0 selected | RUN button grayed | ☐ |
| T21 | RUN enabled when 1+ selected | RUN button active | ☐ |

---

## 5. Installation Tests

| ID | Test | Expected | Status |
|----|------|----------|--------|
| T22 | Run single app (winget) | App installs, log shows progress | ☐ |
| T23 | Sequential install (multiple) | Apps install one by one | ☐ |
| T24 | One app fails, others continue | Failed logged, next app runs | ☐ |
| T25 | Session summary shown | Success/Failed/Skipped counts | ☐ |
| T26 | Card status updates during run | Card shows Installing/Done/Failed | ☐ |

---

## 6. Admin Handling Tests

| ID | Test | Expected | Status |
|----|------|----------|--------|
| T27 | Run without admin | Warning in log for admin apps | ☐ |
| T28 | winget handles UAC internally | Installer prompts UAC itself | ☐ |

---

## 7. Internet Tests

| ID | Test | Expected | Status |
|----|------|----------|--------|
| T29 | Run with internet | Normal install flow | ☐ |
| T30 | Simulate no internet | Log shows "Internet unavailable" | ☐ |
| T31 | Fixed source, no internet | Fixed sources install normally | ☐ |

---

## 8. File Handling Tests

| ID | Test | Expected | Status |
|----|------|----------|--------|
| T32 | EXE fixed source | Installer runs silently | ☐ |
| T33 | MSI fixed source | msiexec runs with /quiet | ☐ |
| T34 | ZIP fixed source | File copied to Downloads/, path shown | ☐ |
| T35 | 7Z fixed source | File copied to Downloads/, path shown | ☐ |
| T36 | RAR fixed source | File copied to Downloads/, path shown | ☐ |
| T37 | Missing fixed source | Clear error: "source file not found" | ☐ |
| T38 | Downloads path uses %USERPROFILE% | No hardcoded username in path | ☐ |

---

## 9. Verification Tests

| ID | Test | Expected | Status |
|----|------|----------|--------|
| T39 | Verify via command (git --version) | "Verified via command" in log | ☐ |
| T40 | Verify via registry | Registry key found = success | ☐ |
| T41 | Verify fails gracefully | "Verification inconclusive" shown | ☐ |
| T42 | Pending app shows clear error | "not yet configured" message | ☐ |

---

## 10. Logging Tests

| ID | Test | Expected | Status |
|----|------|----------|--------|
| T43 | Log shows timestamps | HH:mm:ss prefix on each line | ☐ |
| T44 | Success entries green | ✓ entries shown in green | ☐ |
| T45 | Error entries red | ✗ entries shown in red | ☐ |
| T46 | Log scrolls automatically | Latest entry visible | ☐ |

---

## 11. PowerShell Tests

| ID | Test | Expected | Status |
|----|------|----------|--------|
| T47 | PS script executes | Script output appears in log | ☐ |
| T48 | PS ExecutionPolicy | Bypass applies per-session only | ☐ |

---

## 12. Registry / Config Tests

| ID | Test | Expected | Status |
|----|------|----------|--------|
| T49 | applications.json missing | Clear error at startup | ☐ |
| T50 | applications.json malformed | Startup error, no crash | ☐ |
| T51 | All 58 apps loaded | 58 cards visible | ☐ |

---

## How to Run

```bash
# Build
dotnet build src/WinMaster/WinMaster.csproj

# Run
dotnet run --project src/WinMaster/WinMaster.csproj

# Test each case manually, checking ☐ → ✅
```

---

## Automated Tests (Future)

Unit tests will be added to `tests/WinMaster.Tests/` covering:
- AppRegistryService parsing
- FileSystemHelper path resolution
- InstallResult summary calculations
- Search filtering logic
