using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Emberfall.Editor
{
    // Small native integration complement to the managed body-math checks.
    // The caller owns Play Mode and an isolated save. No player startup hook.
    public static class ActorBodyValidation
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly MethodInfo AdvanceJump = typeof(PlayerController).GetMethod("AdvanceJump", Hidden);
        private static readonly MethodInfo TryJump = typeof(PlayerController).GetMethod("TryJump", Hidden);
        private static readonly MethodInfo MovePlayer = typeof(GameSession).GetMethod("MovePlayerWithBodies", Hidden);
        private static readonly MethodInfo ClipAir = typeof(GameSession).GetMethod("ClipPlayerAirMotion", Hidden);
        private static readonly MethodInfo ClearCapsule = typeof(WorldTraversal).GetMethod("IsActorCapsuleClear", BindingFlags.Static | BindingFlags.NonPublic);

        private static bool CapsuleClear(Vector3 at)
        { return (bool)ClearCapsule.Invoke(null, new object[] { at, ActorBodyCollision.PlayerRadius, ActorBodyCollision.PlayerHeight }); }
        private static bool Jump(PlayerController player, Vector3 direction)
        { return (bool)TryJump.Invoke(player, new object[] { direction }); }
        private static Vector3 Air(GameSession game, Vector3 from, Vector3 to)
        { return (Vector3)ClipAir.Invoke(game, new object[] { from, to }); }
        private static Vector3 Move(GameSession game, Vector3 from, Vector3 delta, float dt, ref float fall)
        {
            object[] args = { from, delta, dt, fall, ActorBodyCollision.PlayerRadius };
            Vector3 result = (Vector3)MovePlayer.Invoke(game, args);
            fall = (float)args[3];
            return result;
        }

        private static void RequireIsolation(GameSession game)
        {
            string overridden = SessionState.GetString("Emberfall.ValidationSaveDirectory", "");
            if (!EditorApplication.isPlaying || !Application.isPlaying || game == null || game != GameSession.Instance ||
                game.Progression == null || string.IsNullOrEmpty(overridden))
                throw new InvalidOperationException("Actor-body validation requires an isolated live Play session.");
            string save = Path.GetFullPath(overridden).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string results = Path.GetDirectoryName(save), name = Path.GetFileName(results);
            string expected = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tests", "TestResults"));
            Guid run;
            if (Path.GetFileName(save) != "IsolatedSave" || Path.GetDirectoryName(results) != expected ||
                name.Length < 33 || name[name.Length - 33] != '-' || !Guid.TryParseExact(name.Substring(name.Length - 32), "N", out run) ||
                Path.GetFullPath(game.Progression.SaveDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) != save ||
                Path.GetDirectoryName(Path.GetFullPath(game.Progression.SaveFilePath)) != save)
                throw new InvalidOperationException("Actor-body validation refused a save outside its isolated GUID run.");
            if (game.SceneLoading || !game.HasStarted || game.Player == null || game.IsDead || game.InDungeon || game.InputBlocked)
                throw new InvalidOperationException("Actor-body validation requires the caller's prepared, unblocked camp.");
        }

        private static bool PlayerClear(GameSession game, EnemyController enemy)
        {
            Vector3 at = game.Player.transform.position;
            return CapsuleClear(at) &&
                ActorBodyCollision.IsClear(at, ActorBodyCollision.PlayerRadius, ActorBodyCollision.PlayerHeight,
                    enemy.transform.position, enemy.NavigationRadius, enemy.PhysicalBodyHeight);
        }

        private static IEnumerator FinishJump(PlayerController player)
        {
            // Explicit bounded dt calls advance the actual production jump hook;
            // native transforms / body factories are live, keyboard input is not.
            for (int step = 0; step < 16 && player.IsJumping; step++)
            {
                AdvanceJump.Invoke(player, new object[] { .04f });
                yield return null;
            }
        }

        public static IEnumerator Validate(GameSession game, Action<bool, string> check, Action<string> log)
        {
            if (check == null) throw new ArgumentNullException(nameof(check));
            RequireIsolation(game);
            var player = game.Player;
            var originalScene = player.gameObject.scene;
            var originalPosition = player.transform.position;
            var originalWorld = (GameObject)typeof(GameSession).GetField("world", Hidden).GetValue(game);
            bool gameEnabled = game.enabled, playerEnabled = player.enabled;
            bool worldActive = originalWorld != null && originalWorld.activeSelf;
            float previousTimeScale = Time.timeScale;
            var originals = new List<EnemyController>(game.Enemies);
            var originalEnabled = new List<bool>();
            var ui = game.GetComponent<GameUI>();
            bool uiEnabled = ui != null && ui.enabled;
            Scene scene = default;
            GameObject fixtures = null, enemyObject = null;
            AsyncOperation unload = null;
            log?.Invoke("ACTOR BODY — native scaled WorldBuilder.Pillar, real TryJump/AdvanceJump with live monster, scenery-overlap escape; controlled dt, not keyboard/touch acceptance.");
            try
            {
                game.enabled = false; player.enabled = false;
                if (ui != null) ui.enabled = false;
                foreach (var enemy in originals)
                {
                    originalEnabled.Add(enemy != null && enemy.enabled);
                    if (enemy != null) enemy.enabled = false;
                }
                game.Enemies.Clear();
                if (originalWorld != null) originalWorld.SetActive(false);
                Time.timeScale = 0;
                scene = SceneManager.CreateScene("ActorBody-Isolated-" + Guid.NewGuid().ToString("N"));
                fixtures = new GameObject("Native body fixture world");
                SceneManager.MoveGameObjectToScene(fixtures, scene);
                SceneManager.MoveGameObjectToScene(player.gameObject, scene);
                WorldTraversal.Reset(ZoneKind.Wilderness);
                WorldTerrain.Configure(ZoneKind.Wilderness, 1);
                var resources = fixtures.AddComponent<WorldResources>();
                resources.TerrainFollowing = false;
                var scaled = new GameObject("Native nested pillar scale");
                scaled.transform.SetParent(fixtures.transform, false);
                scaled.transform.localScale = new Vector3(1.5f, 1, 1.5f);
                typeof(WorldBuilder).GetMethod("Pillar", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null,
                    new object[] { scaled.transform, resources, Vector3.zero, 3f, false });
                Physics.SyncTransforms();
                yield return null;

                // 1. Authored geometry registration must use actual native mesh /
                // transform bounds, including the nested cathedral-style scale.
                Bounds bounds = default; bool found = false;
                foreach (var renderer in scaled.GetComponentsInChildren<Renderer>())
                {
                    if (!found) { bounds = renderer.bounds; found = true; }
                    else bounds.Encapsulate(renderer.bounds);
                }
                if (!found) throw new InvalidOperationException("Native WorldBuilder.Pillar created no renderer.");
                check(bounds.size.x > 2 && bounds.max.y > 3, "Native scaled pillar has live rendered dimensions");
                Vector3 before = new Vector3(bounds.min.x - 2, 0, bounds.center.z);
                player.transform.position = before;
                float fall = 0;
                player.transform.position = Move(game, before, Vector3.right * (bounds.size.x + 4), .02f, ref fall);
                check(player.transform.position.x <= bounds.min.x - .43f && player.transform.position.x > bounds.min.x - .7f &&
                    CapsuleClear(player.transform.position),
                    "Native player movement stops at the real scaled pillar face");
                before.y = bounds.max.y + .06f;
                Vector3 over = before + Vector3.right * (bounds.size.x + 4);
                player.transform.position = Air(game, before, over);
                check((player.transform.position - over).sqrMagnitude < .00001f,
                    "Native finite pillar bounds permit genuine vertical clearance above its capital");

                // 2. Exercise the real public jump admission and private update
                // hook against a real initialized creature / its production size.
                enemyObject = new GameObject("Native body contact creature");
                SceneManager.MoveGameObjectToScene(enemyObject, scene);
                enemyObject.transform.position = new Vector3(0, 0, 10);
                var creature = enemyObject.AddComponent<EnemyController>();
                creature.Initialize(game, EnemyKind.Slime, 1); creature.enabled = false;
                game.Enemies.Add(creature);
                Vector3 origin = new Vector3(-3, 0, 6);
                player.Teleport(origin);
                yield return null;
                bool admitted = Jump(player, Vector3.right);
                check(admitted, "Native TryJump admits a prepared grounded player");
                if (!admitted) throw new InvalidOperationException("Native jump fixture was not admitted.");
                Vector3 destination = (Vector3)typeof(PlayerController).GetField("jumpDestination", Hidden).GetValue(player);
                creature.transform.position = Vector3.Lerp(origin, destination, .5f);
                var jump = FinishJump(player); while (jump.MoveNext()) yield return jump.Current;
                check(!player.IsJumping && (player.transform.position - destination).sqrMagnitude < .02f && PlayerClear(game, creature),
                    "Native jump hook crosses the real short creature with height clearance and completes on free ground");

                creature.transform.position = new Vector3(0, 0, 10);
                player.Teleport(origin);
                yield return null;
                admitted = Jump(player, Vector3.right);
                check(admitted, "Native second jump admits before its landing becomes occupied");
                if (!admitted) throw new InvalidOperationException("Native occupied-landing fixture was not admitted.");
                destination = (Vector3)typeof(PlayerController).GetField("jumpDestination", Hidden).GetValue(player);
                creature.transform.position = destination;
                jump = FinishJump(player); while (jump.MoveNext()) yield return jump.Current;
                fall = 0;
                for (int step = 0; step < 50 && !WorldTraversal.CanStand(player.transform.position); step++)
                    player.transform.position = Move(game, player.transform.position, Vector3.back * .08f, .02f, ref fall);
                check(!player.IsJumping && WorldTraversal.CanStand(player.transform.position) && PlayerClear(game, creature),
                    "Native late monster occupancy leaves a legal landing or a body-free escape to ground");
                yield return null;
                check(Jump(player, Vector3.zero), "Native occupied-landing recovery permits a subsequent grounded jump");
                jump = FinishJump(player); while (jump.MoveNext()) yield return jump.Current;

                player.transform.position = creature.transform.position + Vector3.up * creature.PhysicalBodyHeight;
                before = player.transform.position; fall = 4;
                player.transform.position = Move(game, before, Vector3.right * .15f, .02f, ref fall);
                check(player.transform.position.x > before.x + .1f && Mathf.Abs(player.transform.position.y - before.y) < .02f && fall == 0,
                    "Native body-top contact preserves legal horizontal escape and resets blocked falling speed");
                for (int step = 0; step < 50; step++)
                    player.transform.position = Move(game, player.transform.position, Vector3.right * .08f, .02f, ref fall);
                check(WorldTraversal.CanStand(player.transform.position) && PlayerClear(game, creature),
                    "Native player leaves a creature top and falls onto actual ground without sinking through the body");

                // 3. Edge overlap uses the registered native pillar, not an
                // invented stub box. Only outward escape may retain old overlap.
                creature.transform.position = new Vector3(0, 0, 10);
                Vector3 overlap = new Vector3(bounds.min.x - .35f, 1, bounds.center.z);
                player.transform.position = overlap;
                check(!CapsuleClear(overlap), "Native pillar edge fixture begins with a real capsule overlap");
                player.transform.position = Air(game, overlap, overlap + Vector3.left * .08f);
                check(player.transform.position.x < overlap.x - .07f,
                    "Native existing pillar-edge overlap accepts a small outward step before complete separation");
                Vector3 deeper = Air(game, overlap, overlap + Vector3.right * .08f);
                check((deeper - overlap).sqrMagnitude < .00001f,
                    "Native pillar-edge overlap refuses inward movement through the rendered solid");
                before = new Vector3(bounds.min.x - .6f, 1, bounds.center.z);
                Vector3 fresh = Air(game, before, before + Vector3.right * .4f);
                check(CapsuleClear(fresh),
                    "Native overlap escape does not grant a fresh entry into the pillar");
                log?.Invoke("ACTOR BODY native scene=" + scene.name + " pillar=" + bounds + " creatureHeight=" + creature.PhysicalBodyHeight + " project=" + Path.GetFullPath(Path.Combine(Application.dataPath, "..")));
            }
            finally
            {
                if (enemyObject != null) { enemyObject.SetActive(false); UnityEngine.Object.Destroy(enemyObject); }
                game.Enemies.Clear();
                for (int i = 0; i < originals.Count; i++)
                    if (originals[i] != null) { originals[i].enabled = originalEnabled[i]; game.Enemies.Add(originals[i]); }
                if (player != null && originalScene.IsValid())
                {
                    SceneManager.MoveGameObjectToScene(player.gameObject, originalScene);
                    player.transform.position = originalPosition; player.enabled = playerEnabled;
                }
                if (fixtures != null) { fixtures.SetActive(false); UnityEngine.Object.Destroy(fixtures); }
                if (scene.IsValid()) unload = SceneManager.UnloadSceneAsync(scene);
                if (originalWorld != null) originalWorld.SetActive(worldActive);
                if (ui != null) ui.enabled = uiEnabled;
                game.enabled = gameEnabled;
                Time.timeScale = previousTimeScale;
            }
            if (unload != null) yield return unload;
            // Rebuild restores the shared traversal registry / terrain after the
            // isolated additive scene; no fixture mesh or actor enters a save.
            game.StartNew(HeroClass.Vanguard);
            double deadline = EditorApplication.timeSinceStartup + 45;
            while (game.SceneLoading)
            {
                if (!string.IsNullOrEmpty(game.SceneLoadingError)) throw new InvalidOperationException(game.SceneLoadingError);
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Actor-body validation could not restore its fresh isolated camp.");
                yield return null;
            }
            RequireIsolation(game);
            log?.Invoke("ACTOR BODY native integration finished; fresh isolated camp and traversal registry restored.");
        }
    }
}
