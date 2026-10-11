using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace Emberfall.Editor
{
    public static class CameraValidation
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        // Run in a cleared, live desktop fixture. No OS input is injected.
        public static IEnumerator Validate(GameSession game, Action<bool, string> check, Action<string> log)
        {
            ValidateGestures(check);
            Camera view = Camera.main;
            AdventureCamera orbit = view.GetComponent<AdventureCamera>();
            float savedYaw = Read<float>(orbit, "yaw");
            float savedPitch = Read<float>(orbit, "pitch");
            float savedDistance = Read<float>(orbit, "distance");
            bool savedSimulation = MobileControls.SimulationEnabled;
            bool orbitEnabled = orbit.enabled;
            bool playerEnabled = game.Player.enabled;
            SkillTargetingController targeting = game.Player.GetComponent<SkillTargetingController>();
            SkillChargeController charge = game.Player.GetComponent<SkillChargeController>();
            SkillRuntime runtime = Read<SkillRuntime>(game.Player, "skillRuntime");
            int savedRank = game.Progression.Profile.skillRanks[9];
            try
            {
                MobileControls.SimulationEnabled = false;
                orbit.enabled = false;
                // ProcessOrbitInput still uses the real component and shared click
                // contract, but the test owns these samples instead of system input.
                Process(orbit, Vector2.zero, true, true, false);
                Process(orbit, new Vector2(1800, 5000), false, true, false);
                check(Mathf.Abs(orbit.Pitch - AdventureCamera.MaximumPitch) < .001f && orbit.Yaw >= 0 && orbit.Yaw < 360,
                    "Camera drag clamps steep pitch and wraps multiple horizontal turns");
                float rotatedYaw = orbit.Yaw;
                orbit.Snap();
                Process(orbit, Vector2.zero, true, true, false);
                Process(orbit, new Vector2(-3600, -5000), false, true, false);
                check(Mathf.Abs(orbit.Pitch - AdventureCamera.MinimumPitch) < .001f && Mathf.Abs(Mathf.DeltaAngle(orbit.Yaw, Mathf.Repeat(rotatedYaw - 792f, 360f))) < .01f,
                    "Camera drag clamps low pitch and preserves direction across negative yaw wrap");
                Process(orbit, new Vector2(-3600, -5000), false, false, true);
                check(!AdventureCamera.CancelSkillRequested, "Completing an orbit drag does not request skill cancellation");

                Process(orbit, Vector2.zero, false, false, false, true, true, 100);
                check(Mathf.Abs(Read<float>(orbit, "distance") - 13f) < .001f, "Scroll zoom respects the nearest distance limit");
                orbit.Snap();
                check(view.transform.position.y >= .45f && view.transform.forward.y > 0f,
                    "Lowest pitch remains above the ground and looks upward past the horizon");
                Process(orbit, Vector2.zero, false, false, false, true, true, -100);
                check(Mathf.Abs(Read<float>(orbit, "distance") - 25f) < .001f, "Scroll zoom respects the farthest distance limit");
                float heldPitch = orbit.Pitch, heldYaw = orbit.Yaw;
                Process(orbit, Vector2.zero, true, true, false, false, false, 100);
                Process(orbit, new Vector2(900, 900), false, true, false, false, false, 100);
                check(orbit.Pitch == heldPitch && orbit.Yaw == heldYaw && Read<float>(orbit, "distance") == 25f,
                    "Paused or otherwise blocked input cannot rotate or zoom the camera");

                view.transform.rotation = Quaternion.Euler(55, 90, 0);
                Vector3 forward = AdventureCamera.CameraRelativeMovement(Vector2.up, view.transform);
                Vector3 right = AdventureCamera.CameraRelativeMovement(Vector2.right, view.transform);
                Vector3 diagonal = AdventureCamera.CameraRelativeMovement(Vector2.one, view.transform);
                check(Vector3.Distance(forward, Vector3.right) < .001f && Vector3.Distance(right, Vector3.back) < .001f,
                    "After a quarter-turn, W and D follow the camera's horizontal forward/right directions");
                check(Mathf.Abs(diagonal.y) < .001f && Mathf.Abs(diagonal.magnitude - 1) < .001f,
                    "Camera-relative diagonal movement remains on the ground and cannot move faster");
                view.transform.rotation = Quaternion.Euler(75, 270, 0);
                check(Vector3.Distance(AdventureCamera.CameraRelativeMovement(Vector2.up * .5f, view.transform), Vector3.left * .5f) < .001f,
                    "Camera pitch does not reduce movement speed and fractional input retains its magnitude");

                Set(orbit, "yaw", 135f); Set(orbit, "pitch", 32f); Set(orbit, "distance", 18f); orbit.Snap();
                Vector3 ground = game.Player.transform.position + new Vector3(2, 0, 1);
                Vector3 screen = view.WorldToScreenPoint(ground);
                float enter;
                Ray ray = view.ScreenPointToRay(screen);
                check(screen.z > 0 && new Plane(Vector3.up, Vector3.zero).Raycast(ray, out enter) && Vector3.Distance(ray.GetPoint(enter), ground) < .01f,
                    "Orbit camera still maps mouse screen positions to the intended ground-ray aim point");

                charge.Cancel(); targeting.Cancel(); runtime.Advance(200); runtime.FillEnergy();
                game.Progression.Profile.skillRanks[9] = Math.Max(1, savedRank);
                game.Player.enabled = false;
                int frame = Time.frameCount;
                while (Time.frameCount == frame) yield return null;
                check(targeting.Begin(9), "Camera cancellation fixture enters a real skill preview");
                Process(orbit, Vector2.zero, true, true, false);
                targeting.TickInput();
                check(targeting.IsTargeting && !AdventureCamera.CancelSkillRequested, "Right-button press alone does not cancel a pending skill preview");
                Process(orbit, new Vector2(24, 14), false, true, false);
                targeting.TickInput();
                Process(orbit, new Vector2(24, 14), false, false, true);
                targeting.TickInput();
                check(targeting.IsTargeting && !AdventureCamera.CancelSkillRequested, "Preview survives camera dragging and the drag release");
                Process(orbit, Vector2.zero, true, true, false);
                Process(orbit, new Vector2(2, 1), false, false, true);
                targeting.TickInput();
                check(!targeting.IsTargeting && AdventureCamera.CancelSkillRequested && targeting.CancelledThisFrame,
                    "A short release click cancels the real preview through the shared camera gesture contract");

                frame = Time.frameCount;
                while (Time.frameCount == frame) yield return null;
                check(!AdventureCamera.CancelSkillRequested, "A release click cannot leak into the next input frame");
                check(targeting.Begin(9) && targeting.Confirm() && charge.IsCharging, "Camera cancellation fixture starts a real charged ultimate");
                Process(orbit, Vector2.zero, true, true, false);
                Process(orbit, new Vector2(25, 15), false, true, false);
                Invoke(game.Player, "Update");
                check(charge.IsCharging, "Right-button dragging cannot cancel the real player's pending charge");
                Process(orbit, new Vector2(25, 15), false, false, true);
                Invoke(game.Player, "Update");
                check(charge.IsCharging, "Releasing a drag cannot cancel the real player's pending charge");
                Process(orbit, Vector2.zero, true, true, false);
                Process(orbit, new Vector2(1, 1), false, false, true);
                Invoke(game.Player, "Update");
                check(!charge.IsCharging && game.Player.Energy == 100 && game.Player.SkillCooldownRemaining(9) == 0,
                    "Right-click release cancels the real charge without spending energy or cooldown");
                log("CAMERA passed gesture ownership/release, drag threshold, UI/pause rejection, pitch/yaw/zoom limits, ground clearance, camera-relative movement, ground ray aiming and real preview/charge cancellation.");
            }
            finally
            {
                charge.Cancel(); targeting.Cancel();
                game.Progression.Profile.skillRanks[9] = savedRank;
                Set(orbit, "yaw", savedYaw); Set(orbit, "pitch", savedPitch); Set(orbit, "distance", savedDistance);
                orbit.Snap();
                orbit.enabled = orbitEnabled;
                game.Player.enabled = playerEnabled;
                MobileControls.SimulationEnabled = savedSimulation;
            }
        }

        private static void ValidateGestures(Action<bool, string> check)
        {
            var gesture = new CameraOrbitInput();
            gesture.Advance(Vector2.zero, true, true, false, true, true);
            gesture.Advance(new Vector2(6, 0), false, true, false, true, true);
            check(gesture.IsHeld && !gesture.IsDragging && !gesture.Clicked && gesture.DragDelta == Vector2.zero,
                "Right-button jitter at the six-pixel threshold does not move the camera or cancel a skill");
            gesture.Advance(new Vector2(6, 0), false, false, true, true, true);
            check(gesture.Clicked && !gesture.IsHeld, "Only release commits a below-threshold right click");
            gesture.Advance(Vector2.zero, false, false, false, true, true);
            check(!gesture.Clicked, "A click is delivered for exactly one input sample");
            gesture.Advance(Vector2.zero, true, true, false, true, true);
            gesture.Advance(new Vector2(7, 0), false, true, false, true, true);
            check(gesture.IsDragging && gesture.DragDelta == new Vector2(7, 0), "Crossing the shared threshold starts an orbit with the accumulated motion");
            gesture.Advance(Vector2.zero, false, false, true, true, true);
            check(!gesture.Clicked && !gesture.IsHeld, "Dragging back to the press point is still a drag, never a cancel click");
            gesture.Advance(Vector2.zero, true, true, false, false, true);
            gesture.Advance(new Vector2(100, 100), false, true, false, true, true);
            check(!gesture.IsHeld && gesture.DragDelta == Vector2.zero, "A press that began over UI cannot become a world drag by leaving the UI");
            gesture.Advance(Vector2.zero, true, true, false, true, true);
            gesture.Advance(new Vector2(20, 0), false, true, false, false, true);
            check(gesture.IsDragging, "An owned world drag remains stable when its pointer crosses UI");
            gesture.Advance(new Vector2(20, 0), false, true, false, false, false);
            gesture.Advance(new Vector2(20, 0), false, false, true, true, true);
            check(!gesture.IsHeld && !gesture.Clicked, "Pause or focus loss releases gesture ownership without a phantom click");
            gesture.Advance(Vector2.zero, true, true, false, true, true);
            gesture.Advance(Vector2.zero, false, false, false, true, true);
            check(!gesture.IsHeld && !gesture.Clicked, "A lost mouse-up event cannot leave orbit dragging stuck");
        }

        private static void Process(AdventureCamera camera, Vector2 point, bool down, bool held, bool up, bool start = true, bool keep = true, float scroll = 0)
        { Invoke(camera, "ProcessOrbitInput", point, down, held, up, start, keep, scroll); }
        private static T Read<T>(object target, string name) { return (T)target.GetType().GetField(name, Private).GetValue(target); }
        private static void Set(object target, string name, object value) { target.GetType().GetField(name, Private).SetValue(target, value); }
        private static object Invoke(object target, string name, params object[] values) { return target.GetType().GetMethod(name, Private).Invoke(target, values); }
    }
}
