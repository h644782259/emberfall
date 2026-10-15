using System;
using System.Reflection;
using UnityEngine;
namespace Emberfall.Editor
{
    public static class ActorPaletteValidation
    {
        public static void Run()
        {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            var model = new GameObject("Isolated actor palette validation").AddComponent<CombatModel>();
            var create = typeof(CombatModel).GetMethod("Mat", flags);
            var tint = typeof(CombatModel).GetMethod("UpdatePaletteTint", flags);
            var original = new Color(.2f, .3f, .4f, .8f);
            var material = (Material)create.Invoke(model, new object[] { original, VisualSurface.Cloth });
            Action<float,float,bool> apply = (flash,opacity,death) => tint.Invoke(model, new object[] { flash, opacity, death });
            Action<Color,string> check = (expected,message) => {
                if (Vector4.Distance(material.color, expected) > .0001f) throw new Exception(message);
            };
            apply(0, 1, false); check(original, "Idle changed authored tint");
            material.color = Color.magenta;
            apply(0, 1, false); check(Color.magenta, "Unchanged idle state still wrote material");
            apply(.35f, 1, false); check(Color.Lerp(original, new Color(1, .92f, .73f), .35f), "Hit flash missing");
            apply(0, 1, false); check(original, "Flash did not restore original tint");
            apply(0, .4f, true); check(new Color(.2f, .3f, .4f, .4f), "Death opacity missing");
            apply(0, 0, true); check(new Color(.2f, .3f, .4f, 0), "Final fade missing");
            apply(0, 1, false); check(original, "Live state did not restore authored alpha");
            material.color = Color.magenta;
            create.Invoke(model, new object[] { Color.green, VisualSurface.Skin });
            apply(0, 1, false); check(original, "New palette entry did not invalidate cache");
            Debug.Log("PASS: 8 native actor palette checks (idle no-write, hit flash, restoration, death fade, palette growth)");
        }
    }
}
