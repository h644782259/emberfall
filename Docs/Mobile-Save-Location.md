# Mobile save-location utility

`DrawMobileSaveLocation()` uses the shared mobile panel geometry. At 568 × 320 touch units, the measured body has 188 units of scroll height, with fixed 48-unit Copy/Back actions. Desktop touch simulation adds a fixed Open Directory action; iOS/Android do not offer a desktop file-URL action.

The full directory and active character filename appear in 14-unit text. `MobileSavePathText.Wrap` uses the actual font-width callback and preserves Unicode text elements while adding display line breaks. The cached presentation updates when the directory, active filename, width or touch scale changes. Copy always uses the original directory string, with no display line breaks or truncation.

The view does not save, move or delete character data. Failed clipboard/directory requests stay visible at the top of measured content. Back and close use the existing pause-origin lifecycle, including when a blessing is pending. The directory action reports that it requested the system to open the location, not that an external file manager successfully appeared.

Migration help distinguishes copying a path from exporting files. It preserves character `.json` files, `.bak` backups, recoverable `.tmp` files and `.delete-pending` markers so an unfinished deletion cannot be bypassed by transferring an old backup alone. A detached deleted slot is identified as unavailable. Resizing rewraps text without discarding an action error.

`MobileSaveLocationTests` covers long unbroken paths, Windows separators/spaces, Chinese paths, surrogate pairs, combining marks, deterministic wrapping, and compact phone/tablet action geometry. `MobileSaveLocationSourceTests.py` checks original-path copying, platform branches, scroll placement, failure visibility and absence of save mutations. These tests do not exercise the operating-system clipboard or Unity GUI rendering.
