#if EMBERFALL_VISUAL_VALIDATION
using System.Collections;
using System.Reflection;
using UnityEngine;
namespace Emberfall
{
    public sealed partial class VisualValidationPlayer
    {
        private IEnumerator VerifyImpactControls()
        {
            result.captureScope="Isolated live player: repeated frost launch, gravity descent, meteor, knockback, pressure and expiry; enemy AI enabled.";
            yield return SetResolution(1920,1017);
            SetField("selectedClass",HeroClass.Arcanist);Invoke("StartSelectedHero");yield return WaitForSceneReady();
            yield return new WaitForSecondsRealtime(2);
            var hero=session.Player;
            var go=new GameObject("Impact validation target");
            Vector3 spot=WorldTraversal.NearestWalkable(hero.transform.position+Vector3.left*2.3f,.65f);
            go.transform.position=spot;
            var enemy=go.AddComponent<EnemyController>();enemy.Initialize(session,EnemyKind.Guardian,1);session.Enemies.Add(enemy);
            typeof(EnemyController).GetProperty("Health").SetValue(enemy,50000f);
            typeof(EnemyController).GetProperty("MaxHealth").SetValue(enemy,50000f);
            typeof(PlayerController).GetMethod("CastSkill",PrivateInstance).Invoke(hero,new object[]{0});
            float until=Time.time+4.4f,previous=0,peak=0;int launches=0;bool captured=false;
            while(Time.time<until)
            {
                var s=enemy.StatusEffects;float velocity=s.AirborneVelocity;
                if(velocity>0&&previous<=0)launches++;
                peak=Mathf.Max(peak,s.AirborneHeight);previous=velocity;
                if(!captured&&s.AirborneHeight>1.7f){captured=true;
                    var armor=enemy.GetComponentInChildren<GuardArmorVisual>();
                    Check(armor!=null&&armor.transform.parent==enemy.GetComponentInChildren<CombatModel>().transform,"Guardian armor follows the animated model root");
                    Check(armor.transform.position.y-enemy.transform.position.y>1.5f,"Airborne armor rises with guardian, not left on ground");
                    yield return Capture("frost-airborne",0);}
                yield return null;
            }
            Check(launches>=3,"Real frost field relaunches on descending contact at least three times; observed="+launches);
            Check(peak>2&&peak<2.3f,"Frost apex follows bounded gravitational trajectory");
            yield return new WaitForSecondsRealtime(2);
            Check(!enemy.StatusEffects.IsAirborne,"After field expiry the target lands");
            go.transform.position=spot;
            CombatArea.Spawn(hero,session,spot,3,1f,.7f,.7f,0,1,new Color(1,.5f,.2f),fallingMeteor:true,statusSkill:1,visual:SkillVisualRecipe.Fire);
            yield return new WaitForSecondsRealtime(1.15f);
            Check(enemy.StatusEffects.AirborneHeight>1,"Real falling meteor launches the target");
            yield return Capture("meteor-vertical-launch",0);
            yield return new WaitForSecondsRealtime(2);
            go.transform.position=spot;
            enemy.TakeDamage(1,Vector3.left,1.5f,1.2f);
            yield return new WaitForSecondsRealtime(.4f);
            Check(Vector3.Dot(go.transform.position-spot,Vector3.left)>.35f,"Damage impact actually moves the enemy outward");
            yield return Capture("knockback-separation",0);
            yield return new WaitForSecondsRealtime(2);
            go.transform.position=spot;
            typeof(EnemyController).GetField("knockVelocity",PrivateInstance).SetValue(enemy,Vector3.zero);
            SummonerSpell.Cast(hero,session,7,1,spot,1);
            yield return new WaitForSecondsRealtime(.5f);
            Check(enemy.StatusEffects.KnockedDown&&enemy.IsStunned,"Pressure applies authoritative grounded control");
            Vector3 held=go.transform.position;
            yield return Capture("pressure-knockdown",0);
            yield return new WaitForSecondsRealtime(1);
            Check((go.transform.position-held).sqrMagnitude<.0025f,"Suppressed enemy cannot navigate during the field");
            yield return new WaitForSecondsRealtime(4);
            Check(!enemy.StatusEffects.KnockedDown&&!enemy.StatusEffects.IsAirborne,"Pressure releases after its producer expires");
            go.transform.position=spot;
            SkillPerformanceVfx.Sustain(hero,spot,ElementalistFieldTuning.GravityRadius,2,SkillPerformanceBody.Gravity);
            yield return Capture("black-hole-transparency",.3f);
            Check(!session.InputBlocked&&enemy.enabled,"Validation ran with active enemy simulation");
            session.Enemies.Remove(enemy);Destroy(go);
        }
    }
}
#endif
