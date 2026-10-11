# Team orders and temporary contract intent

The free focus/recall controls remain navigation commands. They do not cast a contract, spend energy, consume the dodge-command opportunity, refresh attack recovery, change lifetime or directly deal damage.

A free explicit team focus now qualifies as a coordinated target for Twin Summon Resonance. The equipment requirement remains; two different companion kinds must actually confirm hits on the same living target within 1.5 seconds, and the existing three-second proc cooldown remains. Repeating a focus order is not a hit.

An ordinary contract defaults to the current explicit team target without clearing the team directive. A captured contract target/ground point temporarily takes priority only for the companion(s) affected by that contract. When its temporary command expires, it resolves the latest team directive: if the team changed from A to C while that companion attacked B, it returns to C. A targeted enemy becoming dead, removed or farther than 14 meters clears that enemy override and immediately restores team selection; a deliberately empty-ground command persists until expiry. The paid contract's existing damage-bonus timer is not refreshed or erased by free orders.

For charged Spirit/Treant contracts, the ordinary/automatic path captures the team focus at charge start. Deliberate desktop Treant confirmation captures the selected point/enemy instead. The temporary confirmation flag is scoped with `try/finally`. Changing the team during charge does not redirect the confirmed spell. A dead charge target clears its enemy reference, while that one-shot spell still uses its fixed ground point; room/owner/epoch changes cancel it. The existing gold charge marker reads the same `TargetPoint` that execution passes through PlayerController and SummonerSpell. Non-contract targeting and original skill costs are unchanged.

## Checks and limits

- `python3 Tests/CompanionIntentProductionTests.py <dotnet>` executes extracted production free-order, CastContract, Command, AcquireTarget, command-expiry and confirmed-hit methods with the real directive/cooperation trackers. Effects, physics and peripheral creation dependencies are narrow shells. Includes repeated free focus/recall orders, A→B override with team changing to C, dead/out-of-range/removed targets, empty-ground commands, equipment/type/target/window/cooldown boundaries and room epoch cleanup.
- Add `--legacy-clear` to that command as an intentional failing negative control. It restores the old `state.Directive.Clear()` in production CastContract and fails the preserved-team assertion.
- `python3 Tests/ContractSnapshotProductionTests.py <dotnet>` executes production Confirm and charge Begin/Cancel/Advance/validation methods against narrow owner/engine shells. Includes team/explicit/empty-ground intent, duplicate confirmation, changed team during charge, dead target, epoch cancellation and exception cleanup.
- Source contracts check timer, free-command and commit-before-clear constraints together with target precedence.

These are production-method and pure-state checks, not Unity physics, renderer, device-input or gameplay observations. Native API, renderer and device checks use the Unity version configured by the project.
