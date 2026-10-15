using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Emberfall.Editor
{
    [InitializeOnLoad]
    public static class MobileFollowValidation
    {
        private const string Pending = "Emberfall.MobileFollowValidation.Pending";
        private static IEnumerator checks;
        private static double deadline;
        static MobileFollowValidation() { EditorApplication.update += Tick; }
        public static void Run()
        {
            string path=Path.GetFullPath("Tests/TestResults/MobileFollow-"+Guid.NewGuid().ToString("N")+"/IsolatedSave");
            Directory.CreateDirectory(path);
            SessionState.SetString("Emberfall.ValidationSaveDirectory",path);
            SessionState.SetBool(Pending,true);
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
            EditorApplication.EnterPlaymode();
        }
        private static void Tick()
        {
            if(!SessionState.GetBool(Pending,false)||!EditorApplication.isPlaying||!Application.isPlaying)return;
            try
            {
                if(checks==null){checks=Validate();deadline=EditorApplication.timeSinceStartup+120;}
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Mobile follow validation timed out");
                if(checks.MoveNext())return;
                Debug.Log("PASS: native mobile follow integration (stable walking camera, screen-relative movement, independent look)");
                Finish(0);
            }
            catch(Exception error){Debug.LogException(error);Finish(1);}
        }
        private static void Finish(int code)
        {
            SessionState.SetBool(Pending,false);
            SessionState.EraseString("Emberfall.ValidationSaveDirectory");
            EditorApplication.Exit(code);
        }
        private static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
        private static IEnumerator Validate()
        {
            while(GameSession.Instance==null)yield return null;
            var game=GameSession.Instance;
            MobileControls.SimulationEnabled=true;
            game.StartNew(HeroClass.Arcanist);
            while(!game.HasStarted||game.SceneLoading||game.InputBlocked)yield return null;
            foreach(var enemy in game.Enemies)if(enemy!=null)enemy.gameObject.SetActive(false);
            var view=Camera.main.GetComponent<AdventureCamera>();
            view.Snap();
            var controls=game.GetComponent<MobileControls>();
            Vector2 origin=controls.ControlScreenPoint("move");
            Vector3 start=game.Player.transform.position;
            Check(controls.ProcessPointer(500,TouchPhase.Began,origin),"Joystick start rejected");
            controls.ProcessPointer(500,TouchPhase.Moved,origin+Vector2.down*200);
            // Walking must never orbit, even after the old automatic-follow delay.
            double until=Time.realtimeSinceStartupAsDouble+1.5;
            while(Time.realtimeSinceStartupAsDouble<until)yield return null;
            Check(game.Player.transform.position.z<start.z-.1f,"Down input did not move toward screen bottom");
            Check(Vector3.Dot(game.Player.transform.forward,Vector3.back)>.9f,"Character stayed locked forward");
            Check(Mathf.Abs(Mathf.DeltaAngle(0,view.Yaw))<.01f,"Walking rotated the camera");
            foreach(var direction in new[]{Vector2.left,Vector2.right,Vector2.up,Vector2.down})
            {
                controls.ProcessPointer(500,TouchPhase.Moved,origin+direction*150);
                until=Time.realtimeSinceStartupAsDouble+.6;
                while(Time.realtimeSinceStartupAsDouble<until)yield return null;
                Check(Mathf.Abs(Mathf.DeltaAngle(0,view.Yaw))<.01f,"Directional movement rotated camera");
            }
            view.ApplyMobileOrbit(35,0);
            until=Time.realtimeSinceStartupAsDouble+.2;
            while(Time.realtimeSinceStartupAsDouble<until)yield return null;
            Check(Vector3.Dot(AdventureCamera.MobileMovement(Vector2.down),Quaternion.Euler(0,35,0)*Vector3.back)>.99f,"Held input did not use current screen");
            controls.ProcessPointer(500,TouchPhase.Ended,origin);
            yield return null;
            float facing=game.Player.transform.eulerAngles.y;
            view.ApplyMobileOrbit(25,0);
            Check(Mathf.Abs(Mathf.DeltaAngle(facing,game.Player.transform.eulerAngles.y))<.01f,"Free look turned stationary character");
            until=Time.realtimeSinceStartupAsDouble+1.5;
            while(Time.realtimeSinceStartupAsDouble<until)yield return null;
            Check(Mathf.Abs(Mathf.DeltaAngle(60,view.Yaw))<.01f,"Released view drifted");
            for(int angle=0;angle<360;angle+=45)
            {
                view.ApplyMobileOrbit(Mathf.DeltaAngle(view.Yaw,angle),0);
                until=Time.realtimeSinceStartupAsDouble+.2;
            while(Time.realtimeSinceStartupAsDouble<until)yield return null;
                foreach(var direction in new[]{Vector2.left,Vector2.right,Vector2.up,Vector2.down})
                {
                    Vector3 expected=Quaternion.Euler(0,angle,0)*new Vector3(direction.x,0,direction.y);
                    Check(Vector3.Dot(AdventureCamera.MobileMovement(direction),expected)>.999f,"Screen direction mapping failed");
                }
            }
            MobileControls.ResetInput();
        }
    }
}
