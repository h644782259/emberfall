#!/usr/bin/env python3
"""Targeted lifecycle/allocation wiring checks; these do not execute Unity."""
from pathlib import Path

root = Path(__file__).resolve().parent.parent
pet = (root / "Assets/Scripts/Combat/SummonedCompanion.cs").read_text()
player = (root / "Assets/Scripts/Combat/PlayerController.cs").read_text()
fixture = (root / "Assets/Editor/SummonerValidation.cs").read_text()

def body(source, signature):
    start = source.index("{", source.index(signature))
    depth = 1
    for end in range(start + 1, len(source)):
        depth += (source[end] == "{") - (source[end] == "}")
        if depth == 0:
            return source[start + 1:end]
    raise AssertionError("Unclosed method: " + signature)

checks = []
def check(condition, message):
    if not condition:
        raise AssertionError(message)
    checks.append(message)

retire = body(pet, "internal static void RetireOwner")
check("object.ReferenceEquals(owner, null)" in retire and "owner == null" not in retire,
      "Destroyed Unity owners must still reach explicit dictionary removal")
check("bonds.Remove(owner)" in retire and "object.ReferenceEquals(pet.Owner, owner)" in retire,
      "Retirement removes the exact owner's state and pets")
check("owner.transform" not in retire and "owner.CombatEpoch" not in retire,
      "Retirement never dereferences the destroyed owner's native object")
check("for (int i = active.Count - 1; i >= 0; i--)" in retire and "pet.Dismiss()" in retire,
      "Retirement permits synchronous removal of the current pet")
check("SummonedCompanion.RetireOwner(this)" in body(player, "private void OnDestroy()"),
      "Real player teardown retires static owner state")
transfer = body(pet, "public static void TransferPermanentPartners")
check(transfer.index("owner.HeroClass != HeroClass.Summoner") < transfer.index("bonds[owner]"),
      "Non-summoner transitions cannot create a bond entry")
reset = body(pet, "private static void ResetRegistry()")
check(all(name + ".Clear()" in reset for name in ("active", "bonds", "staleOwners")),
      "A new runtime session clears all companion registries")
check("RuntimeInitializeLoadType.SubsystemRegistration" in pet,
      "Registry reset also supports disabled domain reload")
state = body(pet, "private static BondState State")
check("new List<" not in state and state.count("staleOwners.Clear()") == 2,
      "Pruning reuses storage and does not retain removed owner keys in scratch space")
capacity = body(pet, "public static void EnforceCapacity")
check(not any(token in capacity for token in ("Snapshot(", ".ToArray(", ".FindAll(", "new List<", "=>")),
      "Each capacity check avoids the previous temporary companion collection and predicate")
check("for (int i = 0; i < active.Count;)" in capacity and "pet.Owner != owner" in capacity,
      "Capacity walks owner-filtered entries in oldest-first order")
check(capacity.count("pet.Dismiss(CompanionRetirementReason.Replaced); continue;") == 2,
      "Both route-removal branches recheck the shifted current slot")
starter = body(pet, "public static bool HasStarter")
check("foreach (SummonedCompanion pet in active)" in starter and "=>" not in starter and ".Exists(" not in starter,
      "Per-frame starter presence checks do not create a capturing predicate")
check("public static SummonedCompanion[] Snapshot" in pet,
      "Infrequent callers retain the existing detached snapshot API")
check("RequireIsolatedRuntime" in fixture and "IEnumerator lifecycle = ValidateOwnerLifecycle" in fixture,
      "Future engine assertions remain behind the isolated fixture guard and are actually dispatched")
check("retired == null && !ReferenceEquals(retired, null)" in fixture and "first.enabled = second.enabled = false" in fixture,
      "Prepared teardown check covers destroyed-object equality without pet Update doing the cleanup first")
check("oldestSpirit.IsAlive && oldestSpirit.IsPermanent" in fixture and "!extraWolf.IsAlive && !newerSpirit.IsAlive" in fixture,
      "Prepared route fixture checks oldest retention and adjacent removals")
print("PASS:", len(checks), "companion lifetime/allocation source contracts")
