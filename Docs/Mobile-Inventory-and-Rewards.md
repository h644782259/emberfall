# Mobile inventory and reward panels

This change replaces the scaled desktop inventory, fashion collection and chest screens when mobile controls are active. Desktop routing remains the existing implementation.

## Layout and interaction

- All panel positions use the shared safe-area `MobilePanelLayout` in logical touch units. Tabs are 44 units high; primary, sale, lock, purchase and chest actions are 48 units high. Content uses measured, wrapping 14–18-unit text and 22-unit score numbers.
- Below 800 logical units, inventory opens a full-width list, then a full-width selected-item detail. Back, equip and slot enhancement remain in a fixed footer. At wider tablet sizes, list and detail use independent scroll columns.
- Bag filters and sorting remain available; equipped slots, supplies and fashion collection have direct tabs. Dragging uses the existing `BeginTouchScroll` suppression, and transitions use `BlockUITransition` so the releasing finger cannot operate the next screen.
- Body content scrolls independently of the header, tabs and footer. Scores show current equipment, resulting candidate score, signed net change, and actual attack/defense/health changes. Empty slots use zero. Item titles keep rarity color, level locks stay legible, and mechanism/active variant descriptions remain visible alongside score comparisons.
- The mobile list keeps a bounded preview and measured-row cache. Profile replacement, equipment IDs, slot ranks, level, inventory count, filter/sort, width and touch scale invalidate the relevant cached data. Existing service changes or new save profiles are reflected without cloning every preview on each repaint. Rows outside the scroll viewport skip their IMGUI draw work.

## Service and save behavior

Inventory uses the existing stable-ID `Equip`, `Upgrade`, `SetItemLocked`, `SellInventoryItem` and `BuyPotion` paths. It does not recalculate costs or modify profile fields. Equip eligibility, currently equipped/locked sale protection, slot enhancement caps, affordability and save failures are preserved. Slot reinforcement is automatic on a swap and uses the candidate's actual basis. Equipment, supplies and fashion actions copy their service result into measured status at the top of the corresponding scroll body. Failed actions cancel the active drag and reset that body to the top; successful actions preserve its scroll position. Status is cleared when the active hero/slot changes, and sale keeps the original single notification path.

Fashion uses `EquippedFashion`, `StrongestFashion`, `EquipFashion`, `UnequipFashion` and `ChooseLegendaryFashion`. Collection strength is shown independently of worn appearance. Legendary exchange is available only in camp, with the actual `FashionChoiceCost`, and only for a missing legendary slot.

Chest choices share the desktop reveal fields, cached chest textures, animation duration, reduced-effects behavior, rarity sounds and `FinishChestReveal`. Every unopened card has the same appearance and action geometry. The single-choice consequence is visible before opening. Rules occupy a separate scrollable view, with probabilities counted from the deterministic production rarity table and gold bounds derived from the saved pending tier.

The only grant call occurs after an explicit choice. Failed saves keep the unopened choice retryable and display the error. Successful opening consumes the saved offer before animation; repeat input cannot call it again. Skipping changes the animation clock only. Acknowledgement uses the existing service and remains retryable on save failure. Saved receipt IDs restore the display without rolling another reward. The chest header always provides Menu, including when opening or acknowledgement cannot be saved. Menu pauses without consuming an offer, discarding a receipt or rerolling. Opening errors appear before the choices; acknowledgement failures appear before the result and reset that scroll to the top. Unopened offers and unacknowledged receipts remain saved until the existing service lifecycle completes.

## Integration

Route mobile inventory, fashion and chest panels to `DrawMobileInventory()`, `DrawMobileFashion()` and `DrawMobileChests()`. These methods are private members of the existing `GameUI` partial class. They use the shared `GameUI.MobilePanels` helpers. `MobileCollectionLayout` supplies the production comparison/card geometry used by the focused pure tests.

## Validation and limits

Prepared checks include compact 568×320 logical landscape, multiple phone densities and iPad sizes; fixed-action containment and non-overlap; readable score-column width; equal chest card dimensions; and real service/receipt/source wiring. The pure layout tests and source contracts can run without Unity. Runtime source is also compiled against the installed Unity 6000.6.3f1 assemblies.

No Unity Editor, PlayMode, IMGUI render, shader, touch-device performance or platform build was executed in this environment. Actual text rasterization, scrolling feel and reveal appearance still need engine/device verification. No user save was opened or modified by this work.
