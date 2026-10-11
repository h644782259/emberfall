using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Emberfall.Editor
{
    // Prepared engine checks. Run only inside the isolated runtime suite after
    // existing combat text has expired; this is not a user-save entry point.
    public static class FeedbackValidation
    {
        public static IEnumerator Validate(GameSession game, Action<bool, string> check, Action<string> log)
        {
            typeof(InventoryUIValidation).GetMethod("RequireIsolatedRuntime", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { game });
            if (FloatingNumber.ActiveCount != 0) throw new InvalidOperationException("Feedback fixture requires a quiet scene without existing combat text.");
            Camera camera = Camera.main;
            if (camera == null) throw new InvalidOperationException("Feedback fixture requires the gameplay camera.");
            bool simulation = MobileControls.SimulationEnabled;
            RenderTexture previousTarget=camera.targetTexture;
            // Dimension-only camera surface; never Create/Render this large texture.
            var layoutSurface=new RenderTexture(8192,8192,0);
            camera.targetTexture=layoutSurface;
            var stored = (List<GameSession.SystemMessage>)typeof(GameSession).GetField("systemMessages", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
            GameSession.SystemMessage[] previousMessages = stored.ToArray();
            try
            {
                IList<GameSession.SystemMessage> view = game.SystemMessages;
                check(ReferenceEquals(view, game.SystemMessages), "System log reuses one read-only wrapper");
                for (int i = 0; i < 40; i++) game.LogSystem("Feedback fixture " + i);
                check(ReferenceEquals(view, game.SystemMessages) && view.Count == 32 &&
                    view[0].Text == "Feedback fixture 8" && view[31].Text == "Feedback fixture 39",
                    "Cached system log view reflects append and bounded eviction immediately");
                bool refused = false;
                try { view.Add(new GameSession.SystemMessage()); } catch (NotSupportedException) { refused = true; }
                check(refused && view.Count == 32, "System log readers cannot mutate the collection");

                foreach (bool touch in new[] { false, true })
                {
                    MobileControls.SimulationEnabled = touch;
                    int cap = MobileControls.Active ? 20 : 36;
                    FloatingNumber oldest = null;
                    for (int i = 0; i < cap; i++)
                    {
                        FloatingNumber number = FloatingNumber.Spawn(At(camera, i), "1", Color.white);
                        check(number != null, "Separated text fills available global slot " + i);
                        if (oldest == null) oldest = number;
                    }
                    int objects = Objects().Length;
                    Vector3 free = At(camera, cap + 1);
                    check(FloatingNumber.ActiveCount == cap && !FloatingNumber.CanSpawn(free), "Ordinary text respects actual display cap " + cap);
                    game.SpawnFloatingText(free, "blocked", Color.white);
                    game.SpawnCombatDamage(free, "blocked", false);
                    check(Objects().Length == objects, "Both ordinary session callers reject before creating a text component");
                    check(FloatingNumber.CanSpawn(free, true) && oldest.gameObject.activeSelf && FloatingNumber.ActiveCount == cap,
                        "Critical precheck preserves its prospective oldest ordinary replacement");
                    game.SpawnCombatDamage(free, "99", true);
                    check(!oldest.gameObject.activeSelf && FloatingNumber.ActiveCount == cap,
                        "Accepted critical replaces exactly the oldest ordinary text without raising count");
                    for (int i = 1; i < cap; i++)
                        check(FloatingNumber.Spawn(At(camera, cap + i + 1), "99", Color.yellow, true) != null,
                            "Remaining ordinary text can be replaced by critical text");
                    objects = Objects().Length;
                    game.SpawnCombatDamage(At(camera, cap * 3), "blocked", true);
                    check(!FloatingNumber.CanSpawn(At(camera, cap * 3), true) && Objects().Length == objects,
                        "A full critical-only display rejects additional critical text before allocation");
                    RetireAll();
                    for(int i=0;i<cap;i++)check(FloatingNumber.Spawn(At(camera,i),"1",Color.white)!=null,"Refill damage budget");
                    for(int i=0;i<CombatTextLayout.MechanismLimit;i++)check(FloatingNumber.Spawn(At(camera,cap+i),"打断",Color.cyan,isMechanism:true)!=null,"Mechanism reserve survives full damage budget");
                    objects=Objects().Length;
                    check(FloatingNumber.Spawn(At(camera,cap+5),"满额",Color.cyan,isMechanism:true)==null&&Objects().Length==objects,"Full mechanism pool rejects before allocation");
                    check(FloatingNumber.MechanismCount==4&&FloatingNumber.ActiveCount==cap+4,"Separate total bound");
                    RetireAll();
                    check(FloatingNumber.MechanismCount==0&&FloatingNumber.ActiveCount==0,"Both pools return immediately on retirement");

                    Vector3 cluster = At(camera, -20);
                    int laneMask = 0;
                    for (int i = 0; i < 6; i++)
                    {
                        FloatingNumber number = FloatingNumber.Spawn(cluster, "1", Color.white, i >= 4);
                        check(number != null, "Local ordinary/critical limit admits lane " + i);
                        laneMask |= 1 << (int)typeof(FloatingNumber).GetField("lane", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(number);
                        if (i == 3) check(!FloatingNumber.CanSpawn(cluster), "Four nearby ordinary numbers block a fifth ordinary number");
                    }
                    check(CountBits(laneMask) == 6 && !FloatingNumber.CanSpawn(cluster, true), "Six nearby numbers occupy distinct measured slots and block a seventh critical");
                    // Reject replacement only when the crowded cluster has no
                    // ordinary entry of its own that could free a local lane.
                    RetireAll();
                    for (int i = 0; i < 6; i++)
                        check(FloatingNumber.Spawn(cluster, "99", Color.yellow, true) != null,
                            "Rejection fixture fills local lane with critical text " + i);
                    FloatingNumber distantOrdinary = null;
                    for (int i = 0; i < cap - 6; i++)
                    {
                        FloatingNumber number = FloatingNumber.Spawn(At(camera, i), "1", Color.white);
                        if (distantOrdinary == null) distantOrdinary = number;
                    }
                    objects = Objects().Length;
                    game.SpawnCombatDamage(cluster, "blocked", true);
                    check(distantOrdinary.gameObject.activeSelf && FloatingNumber.ActiveCount == cap && Objects().Length == objects,
                        "Dense rejected critical preserves an unrelated ordinary number at the global cap");
                    RetireAll();
                }
            }
            finally
            {
                RetireAll(); MobileControls.SimulationEnabled = simulation;camera.targetTexture=previousTarget;UnityEngine.Object.Destroy(layoutSurface);
                stored.Clear(); stored.AddRange(previousMessages);
            }
            yield return null;
            log("FEEDBACK passed real-cap admission, critical replacement and rejection, six local lanes, and cached live read-only system log checks.");
        }

        private static Vector3 At(Camera camera, int index)
        { int cell=index<0?120:index;return camera.ScreenToWorldPoint(new Vector3(320+(cell%12)*640,120+(cell/12)*640,12)); }
        private static int CountBits(int bits){int result=0;while(bits!=0){result+=bits&1;bits>>=1;}return result;}
        private static FloatingNumber[] Objects()
        { return UnityEngine.Object.FindObjectsByType<FloatingNumber>(FindObjectsInactive.Include); }
        private static void RetireAll()
        {
            MethodInfo retire = typeof(FloatingNumber).GetMethod("Retire", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (FloatingNumber number in Objects()) retire.Invoke(number, null);
        }
    }
}
