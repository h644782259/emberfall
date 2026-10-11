// Explicit test-only disabled feature boundary for EquipmentCompositionProductionTests.
// This preserves production Hero/ApplyEquipment/ApplyFashion/WeaponRig calls verbatim.
// It does not emulate or validate imported assets, animation, or fallback transitions.
using System;
using UnityEngine;
namespace Emberfall
{
    public sealed partial class CombatModel
    {
        // Construction/equipment suite deliberately disables optional pose art.
        // It preserves the real factory call; VanguardActionsProductionTests owns
        // the actual library selection, decode and pose-adapter checks.
        private void ConfigureVanguardArt() {}
        private void ApplyWeaponFashionArt() {}
        private void ApplyWeaponArt(ItemData weapon) {} // WeaponModulesProductionTests owns actual new weapon geometry.
        private bool pilotHasGear, pilotHasFashion, pilotVisible;
        private void InvalidatePilotRendererGroup() {} // No display cache in this disabled-feature boundary.
        private void ReleasePilotRendererGroup() {} // No display cache resources in this boundary.
        private BlenderPilotVisual blenderPilot;
        private static bool PilotStarterCompatible(ItemData item,ItemSlot slot) { return false; }
        private void ConfigureBlenderPilot() { blenderPilot=null;pilotVisible=false; }
        private void SetBlenderPilotVisible(bool visible)
        {
            if(visible)throw new InvalidOperationException("Equipment composition suite must keep optional Blender pilot disabled");
            pilotVisible=false;
        }
    }
    public sealed class BlenderPilotVisual
    {
        public bool Anchor(WeaponVisualAnchor anchor,out Vector3 position)
        {
            throw new InvalidOperationException("Disabled imported visual cannot supply procedural weapon anchors");
        }
    }
}

namespace Emberfall { internal static class ActorSilhouetteF1 { internal static void Apply(UnityEngine.GameObject obj,string name,bool treant) {} } }

// This legacy composition suite covers original fallback geometry. Authored binary
// decoding/selection has a separate production-loader suite and construction export.
namespace Emberfall { internal static class AuthoredActorMeshes { internal static void Apply(UnityEngine.GameObject obj,string name,UnityEngine.PrimitiveType shape) {} } }
