# Android lifecycle input and audio

This shared runtime change keeps existing combat and save rules. It does not change Android PlayerSettings or native activity configuration.

- Background UI is disabled. Focus loss/suspension clears IMGUI control ownership, scrolling, skill drags, movement and orbit input. A release latch requires held pointers to lift before a new surface can receive them.
- Screen dimensions, safe-area coordinates, layout scale, raw DPI and orientation identify the touch coordinate system. A changed value cancels old pointer ownership, including a 180-degree rotation with unchanged dimensions.
- UI Back records its consumed frame. `GameUI.GameplayBackAllowed` checks the current modal state and this frame marker, so gameplay consumers must check this property even after a panel closes. Active gameplay cancels charge/targeting first; modal UI owns Back while blocked. Title Back requests the existing exit confirmation only on Android. Confirmed Android exit uses `Application.Quit`; iOS keeps its existing return-to-title behavior. Player/targeting consumer integration is supplied in the companion controller change.
- Audio observes the combined `!Focused || Suspended` state. Only transitions stop old effects and pause/unpause the ambient loop. Repeated callbacks cannot replay it; foreground alone cannot override suspension. Audio configuration rebuilds preserve a sample cursor where supplied by Unity. Ambient still ignores the menu listener pause, so manual menu pause retains its prior behavior.
- The existing save lifecycle gate remains responsible for one successful save per background episode and retries after save failure. Manual pause state is not changed by resume.

## Reproducible checks

`AndroidLifecycleTests` depends on `TouchViewportState`, `TouchReleaseLatch`, `AudioLifecycleGate`, `ApplicationPauseState`, and `SaveLifecycleGate`. It checks all sequences of eight focus/suspension callbacks, viewport changes, held-pointer release, manual pause preservation and save retry. Companion tests cover pause state, touch release and safe exit. `AndroidLifecycleSourceTests.py` checks production wiring.

These are pure-state, source-contract and reference-API checks, not engine/device tests. Actual Android system/predictive Back delivery into `KeyCode.Escape`, background audio, interruption by phone calls, orientation/safe-area rendering, resume touch ownership and quit behavior require Unity/adb/device validation. There is no claim that these behaviors were observed on a device. Existing iOS platform/font settings are unchanged.

## Pause subpage Back regression

On mobile, open Pause → More settings (page 1) → Touch layout (page 2). Previously Back toggled the session pause off from either subpage, while the visible return action led to the main pause page. Back now returns to page 0 and retains pause; a subsequent Back uses the existing main pause behavior. Dedicated panels (Controls, SaveLocation, etc.) and exit confirmations retain their own navigation and frame consumption.

`MobilePauseNavigationTests` compiles the production `GameUI.PauseNavigation.cs` partial with a minimal session/UI shell, checking pause navigation combinations and repeated Back behavior. It must run separately from the full Unity runtime because the shell intentionally supplies the partial's dependencies. `MobilePauseNavigationSourceTests.py` checks integration priority. These checks do not demonstrate OS Back delivery or touch rendering.

## Foreground room-transition multi-touch regression

`SuspendInputs` also runs during room entry and settlement. It now uses foreground UI cleanup while focused and unsuspended; only actual background state adds the lifecycle all-pointer release latch. Viewport invalidation still explicitly adds that latch. The interaction's `finally` continues to set the ordinary UI gate to the initiating finger. Existing room-entry ownership reset still stops the old movement gesture; this does not introduce automatic cross-room movement.

Run `python3 Tests/ProductionTouchLifecycleTests.py /path/to/dotnet`. This extracts and executes the actual UI transition/lifecycle getters, cleanup methods, mobile interaction entry, session input suspension, and mobile input reset with the real `TouchReleaseLatch` and `TouchViewportState`. Only engine values and surrounding game dependencies are stubbed. The replay uses joystick finger 11, interaction finger 12 and replacement control 13 at 1 and 110 seconds: foreground entry permits 13 after 12 releases, while background/viewport invalidation continues to reject 13 until all pointers lift. A negative control restores the old foreground-to-background dispatch and must fail the foreground replacement-control assertion. This is production-method replay, not Unity event delivery or device testing.

## Chest-owned pause overlay

The mobile chest page's Menu action pauses without discarding `Panel.Chests`. Pause settings pages must therefore accept either `None` or `Chests` as their background when returning to the main pause page. Previously Back bypassed that step and returned directly to the chest, leaving the settings-page index stale. The fix does not acknowledge or reset a reward, close the chest panel, or alter its receipt. SaveSelection/manual-save confirmation and other dedicated panels remain excluded.

`MobilePauseNavigationTests` checks combinations including Chests, Controls, SaveSelection, SaveLocation and Bindings. Run `python3 Tests/ChestPauseBackProductionTests.py /path/to/dotnet` to execute the complete production `GameUI.Update` and production pause helper against narrow engine/session shells: both settings pages, pending choice/reveal, chest details, exit cancellation, repeated Back and reopening the menu. Receipt identity and pending flags are checked for non-mutation in this dispatch path; this is not a persistence-I/O, reward-grant, Unity-rendering or device-input test.
