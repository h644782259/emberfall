using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace Emberfall
{
    /// <summary>Photographs the same finished gear meshes/materials that CombatModel wears.</summary>
    public static class ProductionGearIconArt
    {
        private const int IconLayer = 31;
        private const int RasterSize = 192;
        private const int CacheLimit = 192;
        private sealed class Request
        {
            internal HeroClass Hero;
            internal ItemData Item, BaseWeapon;
            internal FashionData Fashion;
            internal string Key;
        }
        private sealed class Entry
        {
            internal Texture2D Texture;
            internal LinkedListNode<string> Node;
        }
        private static readonly Dictionary<string, Entry> cache = new Dictionary<string, Entry>();
        private static readonly LinkedList<string> recency = new LinkedList<string>();
        // A held retired wrapper retains a small recipe snapshot for recovery.
        // Unheld wrappers and their metadata can be collected; there is no strong
        // registry retaining every photograph ever requested during the session.
        private static ConditionalWeakTable<Texture2D, Request> sources = new ConditionalWeakTable<Texture2D, Request>();
        private static readonly HashSet<string> reported = new HashSet<string>();
        private static readonly Dictionary<string, float> retryAfter = new Dictionary<string, float>();
        private static bool rendering;
        public static int CachedCount { get { return cache.Count; } }
        public static string LastError { get; private set; }

        public static Texture2D Equipment(ItemData item, HeroClass hero)
        {
            if (item == null) return null;
            string key = "gear/" + (int)hero + "/" + EquipmentKey(item);
            Texture2D cached;
            if (TryCached(key, out cached)) return cached;
            var copy = Copy(item);
            return Resolve(new Request { Hero = hero, Item = copy, Key = key });
        }
        public static Texture2D Equipment(ItemSlot slot, int level, Rarity rarity, HeroClass hero, int upgrade = 0)
        {
            return Equipment(new ItemData { slot = slot, level = level, rarity = rarity, upgradeLevel = upgrade, name = "Icon recipe" }, hero);
        }
        public static Texture2D Fashion(FashionData fashion, HeroClass hero, ItemData baseWeapon = null)
        {
            if (fashion == null) return null;
            var baseItem = fashion.slot == FashionSlot.Weapon ? baseWeapon : null;
            string key = "fashion/" + (int)hero + "/" + (int)fashion.slot + "/" + (int)fashion.rarity + "/" +
                (int)fashion.AppearanceRarity + "/" + fashion.upgradeRank + "/" + EquipmentKey(baseItem);
            Texture2D cached;
            if (TryCached(key, out cached)) return cached;
            var copy = new FashionData { id = "Icon fashion", name = fashion.name, slot = fashion.slot, rarity = fashion.rarity,
                appearanceTier = fashion.appearanceTier, upgradeRank = fashion.upgradeRank };
            return Resolve(new Request { Hero = hero, Fashion = copy, BaseWeapon = Copy(baseItem), Key = key });
        }
        public static Texture2D Fashion(FashionSlot slot, int appearanceTier, HeroClass hero)
        {
            // Collected fashions preserve an independent appearance tier at legendary quality.
            return Fashion(new FashionData { slot = slot, rarity = Rarity.Legendary, appearanceTier = Mathf.Clamp(appearanceTier, 0, 3) }, hero);
        }
        public static bool IsGenerated(Texture2D source)
        {
            Request request;
            return !ReferenceEquals(source, null) && sources.TryGetValue(source, out request);
        }
        public static Texture2D Refresh(Texture2D source)
        {
            Request request;
            return !ReferenceEquals(source, null) && sources.TryGetValue(source, out request) ? Resolve(request) : source;
        }
        private static ItemData Copy(ItemData item)
        {
            return item == null ? null : new ItemData { id = "Icon equipment", name = item.name, slot = item.slot, level = item.level,
                rarity = item.rarity, upgradeLevel = item.upgradeLevel, mechanic = item.mechanic, mechanicVariantUnlocked = item.mechanicVariantUnlocked };
        }
        private static string EquipmentKey(ItemData item)
        {
            if (item == null) return "base";
            // The starter sword alone has imported modules; the exact starter predicate
            // also observes name and mechanic. Keep that branch in the visual identity.
            string starterName = item.slot == ItemSlot.Weapon ? "初行长剑" : item.slot == ItemSlot.Armor ? "初行战衣" : "初行护符";
            bool starter = item.name == starterName && item.level == 1 && item.rarity == Rarity.Common && item.upgradeLevel == 0 &&
                item.mechanic == EquipmentMechanic.None && !item.mechanicVariantUnlocked;
            var look = new EquipmentAppearance(item);
            return (int)item.slot + "/" + look.Tier + "/" + look.LevelBand + "/" + look.RarityRank + "/" + look.UpgradeRank + "/" + (starter ? "starter" : "drop");
        }
        private static bool TryCached(string key, out Texture2D texture)
        {
            Entry entry;
            if (cache.TryGetValue(key, out entry))
            {
                if (entry.Texture != null) { recency.Remove(entry.Node); recency.AddLast(entry.Node); texture = entry.Texture; return true; }
                recency.Remove(entry.Node); cache.Remove(key);
            }
            texture = null; return false;
        }
        private static Texture2D Resolve(Request request)
        {
            Texture2D cached;
            if (TryCached(request.Key, out cached)) return cached;
            if (!Application.isPlaying || rendering || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return null;
            if ((int)request.Hero < 0 || (int)request.Hero > 3) return null;
            float retry;
            if (retryAfter.TryGetValue(request.Key, out retry) && Time.unscaledTime < retry) return null;
            rendering = true;
            try
            {
                var texture = Render(request, null);
                while (cache.Count >= CacheLimit)
                {
                    string retired = recency.First.Value; recency.RemoveFirst();
                    var old = cache[retired]; cache.Remove(retired); Destroy(old.Texture);
                }
                var node = recency.AddLast(request.Key);
                cache[request.Key] = new Entry { Texture = texture, Node = node };
                sources.Add(texture, request); retryAfter.Remove(request.Key); LastError = null; return texture;
            }
            catch (Exception error)
            {
                LastError = error.Message;
                if (retryAfter.Count >= CacheLimit) retryAfter.Clear();
                retryAfter[request.Key] = Time.unscaledTime + 2f;
                if (reported.Add(request.Key)) Debug.LogWarning("Production gear icon " + request.Key + ": " + error.Message);
                return null;
            }
            finally { rendering = false; }
        }

        // The native review tool records actual selected renderers alongside its images.
        public static Texture2D ReviewEquipment(ItemData item, HeroClass hero, IList<string> geometry)
        { return Render(new Request { Hero = hero, Item = Copy(item), Key = "review gear" }, geometry, true); }
        public static Texture2D ReviewFashion(FashionData fashion, HeroClass hero, ItemData baseWeapon, IList<string> geometry)
        { return Render(new Request { Hero = hero, Fashion = fashion, BaseWeapon = Copy(baseWeapon), Key = "review fashion" }, geometry, true); }

        private static Texture2D Render(Request request, IList<string> geometry, bool readable = false)
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Production gear photographs require Play Mode.");
            GameObject stage = null; RenderTexture target = null; Camera lens = null; Texture2D result = null;
            var previousActive = RenderTexture.active;
            var random = UnityEngine.Random.state;
            var ambientMode = RenderSettings.ambientMode; var ambient = RenderSettings.ambientLight; bool fog = RenderSettings.fog;
            Light[] lights = null; int[] masks = null;
            try
            {
                stage = new GameObject("Production gear photograph") { hideFlags = HideFlags.HideAndDontSave };
                // A short-lived presentation stage never survives to the player loop.
                stage.transform.position = new Vector3(0, -5000, 0);
                var model = CombatModel.Hero(stage.transform, request.Hero);
                if (request.Item != null)
                    model.ApplyEquipment(request.Item.slot == ItemSlot.Weapon ? request.Item : null,
                        request.Item.slot == ItemSlot.Armor ? request.Item : null, request.Item.slot == ItemSlot.Relic ? request.Item : null);
                else
                {
                    model.ApplyEquipment(request.BaseWeapon, null, null);
                    model.ApplyFashion(request.Fashion.slot == FashionSlot.Wings ? request.Fashion : null,
                        request.Fashion.slot == FashionSlot.Weapon ? request.Fashion : null);
                }
                // Factory construction pose keeps gear at the real shoulder/head sockets.
                // A weapon is posed vertically for inspection, retaining every local
                // child mesh/scale/socket from the production weapon recipe.
                bool weapon = request.Item != null ? request.Item.slot == ItemSlot.Weapon : request.Fashion.slot == FashionSlot.Weapon;
                if (weapon)
                    foreach (var joint in model.GetComponentsInChildren<Transform>(true))
                        if (joint.name == "Sword Wrist" || joint.name == "Staff Wrist" || joint.name == "Bow")
                            joint.rotation = Quaternion.Euler(0, request.Hero == HeroClass.Ranger ? 90 : -12, 0);
                foreach (var joint in stage.GetComponentsInChildren<Transform>(true)) joint.gameObject.layer = IconLayer;
                foreach (var collider in stage.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                foreach (var behaviour in stage.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
                var visible = new List<Renderer>();
                foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    bool selected = renderer.enabled && renderer.gameObject.activeInHierarchy && Selected(renderer, model.transform, request);
                    if (renderer is TrailRenderer || renderer is ParticleSystemRenderer) selected = false;
                    renderer.enabled = selected;
                    if (!selected) continue;
                    renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                    visible.Add(renderer);
                    if (geometry != null)
                    {
                        var filter = renderer.GetComponent<MeshFilter>();
                        var mesh = filter != null ? filter.sharedMesh : renderer is SkinnedMeshRenderer skin ? skin.sharedMesh : null;
                        geometry.Add(renderer.name + " | " + (mesh != null ? mesh.name + " | " + mesh.vertexCount + " vertices" : renderer.GetType().Name));
                    }
                }
                if (visible.Count == 0) throw new InvalidOperationException("The production gear recipe selected no visible renderers.");
                var cameraObject = new GameObject("Gear photograph camera"); cameraObject.transform.SetParent(stage.transform, false);
                lens = cameraObject.AddComponent<Camera>(); lens.enabled = false; lens.orthographic = true;
                lens.useOcclusionCulling = false; lens.clearFlags = CameraClearFlags.SolidColor;
                lens.backgroundColor = Color.clear; lens.cullingMask = 1 << IconLayer;
                lens.nearClipPlane = .01f; lens.farClipPlane = 20; lens.allowHDR = false; lens.allowMSAA = false;
                bool back = request.Fashion != null && request.Fashion.slot == FashionSlot.Wings;
                var bounds = visible[0].bounds;
                foreach (var renderer in visible) bounds.Encapsulate(renderer.bounds);
                lens.transform.position = bounds.center + (back ? new Vector3(.12f, .28f, -8) : new Vector3(.6f, .3f, 8));
                lens.transform.LookAt(bounds.center);
                float halfWidth = 0, halfHeight = 0;
                foreach (var renderer in visible)
                {
                    Bounds box = renderer.bounds;
                    for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 point = lens.transform.InverseTransformPoint(box.center + Vector3.Scale(box.extents, new Vector3(x, y, z)));
                        halfWidth = Mathf.Max(halfWidth, Mathf.Abs(point.x)); halfHeight = Mathf.Max(halfHeight, Mathf.Abs(point.y));
                    }
                }
                lens.aspect = 1; lens.orthographicSize = Mathf.Max(.08f, Mathf.Max(halfWidth, halfHeight) * 1.13f);
                target = new RenderTexture(RasterSize, RasterSize, 16, RenderTextureFormat.ARGB32) { name = "Production gear photograph target", hideFlags = HideFlags.HideAndDontSave };
                target.Create(); lens.targetTexture = target;
                if (!target.IsCreated()) throw new InvalidOperationException("Gear icon render target allocation failed.");
#if UNITY_2023_1_OR_NEWER
                lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
#else
                lights = UnityEngine.Object.FindObjectsOfType<Light>();
#endif
                masks = new int[lights.Length];
                for (int i = 0; i < lights.Length; i++) { masks[i] = lights[i].cullingMask; lights[i].cullingMask &= ~(1 << IconLayer); }
                AddLight(stage.transform, new Vector3(30, back ? 25 : 155, 0), 1.15f, new Color(1, .91f, .79f));
                AddLight(stage.transform, new Vector3(15, back ? 150 : 30, 0), .6f, new Color(.64f, .78f, 1));
                RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.34f, .38f, .44f); RenderSettings.fog = false;
                lens.Render(); RenderTexture.active = target;
                result = new Texture2D(RasterSize, RasterSize, TextureFormat.RGBA32, false) { name = "Production " + request.Key,
                    hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                result.ReadPixels(new Rect(0, 0, RasterSize, RasterSize), 0, 0, false);
                // Export probes need pixels; live GUI photographs release the CPU copy.
                result.Apply(false, !readable);
                return result;
            }
            catch { if (result != null) Destroy(result); throw; }
            finally
            {
                UnityEngine.Random.state = random;
                RenderTexture.active = previousActive;
                RenderSettings.ambientMode = ambientMode; RenderSettings.ambientLight = ambient; RenderSettings.fog = fog;
                if (lights != null && masks != null)
                    for (int i = 0; i < lights.Length; i++) if (lights[i] != null) lights[i].cullingMask = masks[i];
                if (lens != null) lens.targetTexture = null;
                if (target != null) { target.Release(); Destroy(target); }
                if (stage != null) { stage.SetActive(false); Destroy(stage); }
            }
        }
        private static bool Selected(Renderer renderer, Transform model, Request request)
        {
            // Clothing roots include the real detached shoulder and contract head pieces.
            // No skin, mannequin, effects, nocked arrows or unrelated equipment enter art.
            for (Transform joint = renderer.transform; joint != null && joint != model; joint = joint.parent)
            {
                string name = joint.name;
                if (name == "Arrow Nock") return false;
                if (request.Item != null)
                {
                    if (request.Item.slot == ItemSlot.Weapon && (name == "Equipped Weapon" || name == "Drawn Bowstring")) return true;
                    if (request.Item.slot == ItemSlot.Armor && (name == "Equipped Armor" || name == "Equipped class costume" || name == "Equipped Shoulder" ||
                        name == "Equipped class shoulder" || name == "Equipped contract crown forks")) return true;
                    if (request.Item.slot == ItemSlot.Relic && name == "Equipped Relic") return true;
                }
                else
                {
                    if (request.Fashion.slot == FashionSlot.Wings && name == "Fashion Wings") return true;
                    if (request.Fashion.slot == FashionSlot.Weapon && (name == "Sword Wrist" || name == "Staff Wrist" || name == "Bow")) return true;
                }
            }
            return false;
        }
        private static void AddLight(Transform stage, Vector3 angles, float intensity, Color color)
        {
            var lamp = new GameObject("Gear photograph light"); lamp.transform.SetParent(stage, false); lamp.transform.localRotation = Quaternion.Euler(angles);
            var light = lamp.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = intensity; light.color = color;
            light.cullingMask = 1 << IconLayer; light.shadows = LightShadows.None;
        }
        private static void Destroy(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear()
        {
            foreach (var entry in cache.Values) Destroy(entry.Texture);
            cache.Clear(); recency.Clear(); sources = new ConditionalWeakTable<Texture2D, Request>(); reported.Clear(); retryAfter.Clear(); LastError = null; rendering = false;
        }
    }
}
