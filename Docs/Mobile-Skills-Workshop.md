# Mobile skills and camp workshop

The touch views use safe-area touch units rather than shrinking the desktop modals. `DrawMobileSkills()` and `DrawMobileCampWorkshop()` are separate partial methods; desktop routing and shared progression services remain outside these files.

## Layout and interaction

- At 568 × 320 touch units, the skill list and detail each have a 188-unit-high viewport. The camp has a 136-unit-high viewport below its four fixed tabs. Header, close control and footer stay outside scrolling content.
- Skill selection rows are at least 48 units high and grow with measured text. Camp actions and footer actions are 48 units high; camp tabs are 44 units high. Paragraph text uses 14–16 units and the shared font's `CalcHeight`, so long descriptions add content height rather than clipping into fixed desktop rectangles.
- All ten skill identities are in the scrollable list. The second pane shows actual prerequisites, costs, cooldowns, charge information, all three rank descriptions and corrected field budgets. Passive skills explain their automatic behavior. There are no mobile hotbar pages or assignment controls.
- Both panes and all camp tabs use `BeginTouchScroll`/`EndTouchScroll`, retaining the shared two-axis drag cancellation. Tab/skill changes reset capture. Actions use the existing transition latch to prevent an in-flight touch from acting again.

## Preserved services

The skill footer calls `LearnSkill` and uses `SkillLockReason`; learnable red dots use `ProgressionAttention`. The camp retains specialization (including balanced), summoner routes, four mastery directions, the initial/enhanced 10/20-point core states, skill-rank refund, mastery/core reset, first-clear selection, mechanism exchange, equipped-item reforge, elemental variants and epic-to-legendary ascension. Item actions display and pass the item's stable ID.

The loot tab retains both pending and recovery mailboxes, capacity checks, explicit claims, pickup autosell switches and protected bulk sale. Combined claims stop at a failed pending claim instead of making another call that could erase its error. A successful first mailbox claim remains durable if the second fails; this UI does not promise an atomic transaction across both mailboxes. Tutorial rows reflect the real four progress bits and provide direct skill/inventory navigation.

Every result checks both the service return value and `LastError`. An error stays in measured content and resets its scroll position to the top; the shared notification also reports it. No UI method writes profile fields, recreates items or copies progression formulas.

## Validation scope

`MobileSkillsWorkshopLayoutTests` checks the production safe-area/panel geometry for compact landscape phones, high-density phones and iPads, including the exact 568 × 320 body-height boundary. `MobileSkillsWorkshopSourceTests.py` checks measured wrapping, all ten skill identities, real progression dispatch, stable IDs, failed-claim short-circuiting, fixed actions and shared scroll capture. Exact Unity API compilation checks the actual partial methods.

These checks do not execute Unity's IMGUI event loop or certify glyph rendering. A later player test should swipe each pane, end outside it, change tabs, background/resume, and confirm that scrolling never learns, sells or claims an item. It should also inspect the longest Chinese rank/core/mechanism descriptions at 568 × 320 and on iPad.
