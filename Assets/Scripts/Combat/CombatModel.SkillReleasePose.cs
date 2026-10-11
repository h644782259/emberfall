using UnityEngine;
namespace Emberfall
{
    public sealed partial class CombatModel
    {
        // These are skeletal actions. They never select targets, move the actor,
        // spend resources, emit effects or cause damage.
        internal enum ReleaseGesture { None, Sweep, Stamp, StormBlade, Brace, Thrust, Rally, Quake, Execution,
            IcePush, MeteorThrow, StormGuide, ChainPoint, FireStride, ElementWeave, VoidPlant, Echo, Cataclysm,
            BowFan, TrapSet, RainDraw, LeapDraw, RootSet, PierceDraw, VolleyDraw, WindBrace, StarDraw,
            SpiritPush, ThornPlant, WolfCall, TurretCall, SoulPoint, WispCall, GravityPoint, PackBrace, TreantCall }
        internal static ReleaseGesture SkillReleaseGesture(HeroClass hero,int slot)
        {
            if(slot<0||slot>9||slot==3)return ReleaseGesture.None;
            if(hero==HeroClass.Vanguard)
            {
                switch(slot){case 0:return ReleaseGesture.Sweep;case 1:return ReleaseGesture.Stamp;case 2:return ReleaseGesture.StormBlade;
                    case 4:return ReleaseGesture.Brace;case 5:return ReleaseGesture.Thrust;case 6:return ReleaseGesture.Rally;
                    case 7:return ReleaseGesture.Quake;case 8:return ReleaseGesture.Brace;default:return ReleaseGesture.Execution;}
            }
            if(hero==HeroClass.Arcanist)
            {
                switch(slot){case 0:return ReleaseGesture.IcePush;case 1:return ReleaseGesture.MeteorThrow;case 2:return ReleaseGesture.StormGuide;
                    case 4:return ReleaseGesture.ChainPoint;case 5:return ReleaseGesture.FireStride;case 6:return ReleaseGesture.ElementWeave;
                    case 7:return ReleaseGesture.VoidPlant;case 8:return ReleaseGesture.Echo;default:return ReleaseGesture.Cataclysm;}
            }
            if(hero==HeroClass.Ranger)
            {
                switch(slot){case 0:return ReleaseGesture.BowFan;case 1:return ReleaseGesture.TrapSet;case 2:return ReleaseGesture.RainDraw;
                    case 4:return ReleaseGesture.LeapDraw;case 5:return ReleaseGesture.RootSet;case 6:return ReleaseGesture.PierceDraw;
                    case 7:return ReleaseGesture.VolleyDraw;case 8:return ReleaseGesture.WindBrace;default:return ReleaseGesture.StarDraw;}
            }
            switch(slot){case 0:return ReleaseGesture.SpiritPush;case 1:return ReleaseGesture.ThornPlant;case 2:return ReleaseGesture.WolfCall;
                case 4:return ReleaseGesture.TurretCall;case 5:return ReleaseGesture.SoulPoint;case 6:return ReleaseGesture.WispCall;
                case 7:return ReleaseGesture.GravityPoint;case 8:return ReleaseGesture.PackBrace;default:return ReleaseGesture.TreantCall;}
        }
        private Transform releaseBracePlate;
        private void HideSkillPoseProps(){if(releaseBracePlate!=null)releaseBracePlate.gameObject.SetActive(false);}
        private void ShowBracePlate()
        {
            if(leftElbow==null)return;
            if(releaseBracePlate==null)
                releaseBracePlate=Part("Forged defensive forearm plate",PrimitiveType.Cube,new Vector3(-.08f,-.17f,.16f),
                    new Vector3(.58f,.68f,.14f),new Color(.31f,.36f,.42f),leftElbow,VisualSurface.Metal,"Raised breastplate");
            releaseBracePlate.gameObject.SetActive(true);
        }
        private void ApplySkillReleasePose(float t,int slot)
        {
            ReleaseGesture gesture=SkillReleaseGesture(heroClass,slot);if(gesture==ReleaseGesture.None)return;
            t=Mathf.Clamp01(t);
            float load=Mathf.Sin(t*Mathf.PI),drive=Mathf.Sin(Mathf.Clamp01((t-.3f)/.22f)*Mathf.PI*.5f);
            float settle=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.52f)/.48f));drive*=settle;
            if(heroClass==HeroClass.Vanguard)ApplySwordReleasePose(t,slot,gesture,load,drive);
            else if(heroClass==HeroClass.Ranger)ApplyRangerReleasePose(t,slot,gesture,load,drive);
            else ApplyMagicReleasePose(t,slot,gesture,load,drive);
            // Leg loading and the opposing head turn make the action readable at
            // the game camera while leaving accepted movement entirely controller-owned.
            leftKnee.localRotation*=Quaternion.Euler(load*14,0,0);
            rightKnee.localRotation*=Quaternion.Euler(load*11,0,0);
            if(leftFoot!=null)leftFoot.localRotation*=Quaternion.Euler(-load*5,0,0);
            if(rightFoot!=null)rightFoot.localRotation*=Quaternion.Euler(-load*4,0,0);
            cloak.localRotation*=Quaternion.Euler(drive*10,-drive*8,load*4);
        }
        private void ApplySwordReleasePose(float t,int slot,ReleaseGesture gesture,float load,float drive)
        {
            float side=WeaponSwingSide;
            Vector3 readyBody=new Vector3(-11,-side*42,-7),hitBody=new Vector3(16,side*46,10);
            Vector3 readyArm=new Vector3(-105,-side*58,side*70),hitArm=new Vector3(-54,side*66,-side*51);
            Vector3 readySword=new Vector3(-25,0,-19),hitSword=new Vector3(72,0,12);
            Vector3 leftReady=new Vector3(-41,10,-33),leftHit=new Vector3(-57,-15,-42);
            float depth=.07f;
            if(gesture==ReleaseGesture.Stamp)
            {
                readyBody=new Vector3(-8,-15,0);hitBody=new Vector3(25,10,0);depth=.13f;
                readyArm=new Vector3(-126,12,20);hitArm=new Vector3(-31,-10,15);
                readySword=new Vector3(-16,0,-8);hitSword=new Vector3(20,0,-6);
                rightLeg.localRotation*=Pose(Vector3.zero,new Vector3(-48,0,8),new Vector3(12,0,0),t);
                rightKnee.localRotation*=Pose(Vector3.zero,new Vector3(70,0,0),Vector3.zero,t);
                leftReady=new Vector3(-45,0,-38);leftHit=new Vector3(-25,0,-34);
            }
            else if(gesture==ReleaseGesture.StormBlade)
            {readyBody=new Vector3(-8,-side*54,-8);hitBody=new Vector3(12,side*62,8);readyArm=new Vector3(-126,-side*50,side*58);hitArm=new Vector3(-72,side*78,-side*42);}
            else if(gesture==ReleaseGesture.Brace||gesture==ReleaseGesture.Rally)
            {
                depth=gesture==ReleaseGesture.Rally?.06f:.11f;
                readyBody=new Vector3(-9,17,-3);hitBody=new Vector3(13,-9,-6);
                readyArm=new Vector3(-41,-8,34);hitArm=new Vector3(-56,5,27);
                readySword=new Vector3(22,0,-13);hitSword=new Vector3(8,0,-19);
                leftReady=new Vector3(-60,-12,-65);leftHit=new Vector3(-85,-15,-51);
                if(gesture==ReleaseGesture.Rally){leftReady=new Vector3(-122,15,-36);leftHit=new Vector3(-148,10,-21);}
                else
                {
                    if(slot==8){readyBody.y=-12;hitBody.z=-9;hitArm=new Vector3(-72,16,12);leftHit.x=-102;}
                    ShowBracePlate();
                }
            }
            else if(gesture==ReleaseGesture.Thrust)
            {
                depth=.1f;readyBody=new Vector3(-16,-27,-3);hitBody=new Vector3(26,17,0);
                readyArm=new Vector3(-47,-35,25);hitArm=new Vector3(-89,6,2);
                readySword=new Vector3(150,0,0);hitSword=new Vector3(179,0,0);
                leftReady=new Vector3(-25,12,-41);leftHit=new Vector3(-45,-20,-49);
            }
            else if(gesture==ReleaseGesture.Quake||gesture==ReleaseGesture.Execution)
            {
                bool execution=gesture==ReleaseGesture.Execution;
                depth=execution?.19f:.13f;
                readyBody=execution?new Vector3(-31,-18,-4):new Vector3(-23,-12,0);
                hitBody=execution?new Vector3(39,13,2):new Vector3(32,15,0);
                readyArm=execution?new Vector3(-163,13,12):new Vector3(-171,8,19);
                hitArm=execution?new Vector3(-27,-8,-4):new Vector3(-34,-8,-7);
                readySword=execution?new Vector3(-49,0,-7):new Vector3(-38,0,-12);
                hitSword=execution?new Vector3(88,0,2):new Vector3(80,0,6);
                leftReady=new Vector3(-137,-8,-22);leftHit=new Vector3(-56,-14,-16);
                leftElbow.localRotation=Pose(new Vector3(-20,0,0),new Vector3(-68,0,0),new Vector3(-21,0,0),t);
            }
            spine.localRotation*=Pose(Vector3.zero,readyBody,hitBody,t);
            headRig.localRotation*=Pose(Vector3.zero,new Vector3(7,-readyBody.y*.45f,0),new Vector3(-hitBody.x*.3f,-hitBody.y*.5f,0),t);
            rightArm.localRotation=Pose(new Vector3(-18,0,12),readyArm,hitArm,t);
            rightElbow.localRotation=Pose(new Vector3(-16,0,0),new Vector3(-67,0,0),new Vector3(-6,0,0),t);
            swordRig.localRotation=Pose(new Vector3(24,0,-8),readySword,hitSword,t);
            leftArm.localRotation=Pose(new Vector3(-10,0,-8),leftReady,leftHit,t);
            if(gesture!=ReleaseGesture.Quake&&gesture!=ReleaseGesture.Execution)leftElbow.localRotation=Pose(new Vector3(-20,0,0),new Vector3(-74,0,0),new Vector3(-37,0,0),t);
            if(gesture==ReleaseGesture.Execution)
            {
                // The free hand holds the real sword pommel throughout the
                // loaded/downward phase instead of waving beside the weapon.
                float grip=Mathf.SmoothStep(0,1,t/.16f)*(1-Mathf.SmoothStep(0,1,(t-.62f)/.30f));
                Vector3 hand=spine.InverseTransformPoint(swordRig.TransformPoint(WeaponAnchorLocal(WeaponVisualAnchor.SwordPommel)));
                AimArm(leftArm,leftElbow,Vector3.Lerp(new Vector3(-.30f,.05f,.22f),hand,grip),new Vector3(-1,-.25f,-.2f));
            }
            pelvis.localPosition+=new Vector3(-side*load*.035f,-load*depth,drive*.065f);
            spine.localPosition+=new Vector3(0,-load*depth,drive*.09f);
            leftLeg.localRotation*=Quaternion.Euler(load*-8,0,-load*6);
            rightLeg.localRotation*=Quaternion.Euler(load*10,0,load*6);
        }
        private void ApplyMagicReleasePose(float t,int slot,ReleaseGesture gesture,float load,float drive)
        {
            Vector3 readyBody=new Vector3(-12,-20,-3),hitBody=new Vector3(19,24,3);
            Vector3 rightReady=new Vector3(-75,-8,27),rightHit=new Vector3(-91,8,9);
            Vector3 leftReady=new Vector3(-109,25,-44),leftHit=new Vector3(-91,-8,-15);
            Vector3 staffReady=new Vector3(29,0,-15),staffHit=new Vector3(86,0,-6);
            float depth=.055f;
            if(gesture==ReleaseGesture.MeteorThrow||gesture==ReleaseGesture.Cataclysm)
            {
                readyBody=new Vector3(-22,-31,-5);hitBody=new Vector3(26,29,4);
                leftReady=new Vector3(-167,22,-49);leftHit=new Vector3(-72,-13,-24);
                rightReady=new Vector3(-42,-14,20);rightHit=new Vector3(-55,8,17);
                staffReady=new Vector3(10,0,-17);staffHit=new Vector3(24,0,-12);depth=.095f;
                if(gesture==ReleaseGesture.Cataclysm){rightReady=new Vector3(-140,-8,28);rightHit=new Vector3(-75,12,13);staffReady=new Vector3(47,0,-18);staffHit=new Vector3(79,0,-8);}
            }
            else if(gesture==ReleaseGesture.VoidPlant||gesture==ReleaseGesture.ThornPlant)
            {
                readyBody=new Vector3(-13,0,0);hitBody=new Vector3(31,0,0);depth=.14f;
                rightReady=new Vector3(-132,-8,20);rightHit=new Vector3(-19,-8,12);
                staffReady=new Vector3(24,0,-11);staffHit=new Vector3(0,0,-8);
                leftReady=new Vector3(-108,10,-31);leftHit=new Vector3(-33,-6,-37);
                leftKnee.localRotation*=Pose(Vector3.zero,new Vector3(15,0,0),new Vector3(36,0,0),t);
            }
            else if(gesture==ReleaseGesture.WolfCall||gesture==ReleaseGesture.TurretCall||gesture==ReleaseGesture.WispCall||gesture==ReleaseGesture.TreantCall)
            {
                readyBody=new Vector3(-13,-11,0);hitBody=new Vector3(9,17,-3);
                rightReady=new Vector3(-38,-7,22);rightHit=new Vector3(-53,8,20);
                staffReady=new Vector3(10,0,-17);staffHit=new Vector3(15,0,-13);
                leftReady=new Vector3(gesture==ReleaseGesture.WispCall?-153:-127,25,-69);
                leftHit=new Vector3(gesture==ReleaseGesture.TreantCall?-32:-75,gesture==ReleaseGesture.WolfCall?-21:22,-49);
                depth=gesture==ReleaseGesture.TreantCall?.16f:.075f;
                if(gesture==ReleaseGesture.TreantCall){hitBody=new Vector3(29,7,0);rightHit=new Vector3(-20,0,19);staffHit=new Vector3(0,0,-7);}
            }
            else if(gesture==ReleaseGesture.StormGuide||gesture==ReleaseGesture.ElementWeave)
            {
                rightReady=new Vector3(-134,-24,42);rightHit=new Vector3(-108,21,26);
                leftReady=new Vector3(-142,29,-48);leftHit=new Vector3(-105,-15,-27);
                readyBody=new Vector3(-15,-12,0);hitBody=new Vector3(10,16,0);staffReady=new Vector3(51,0,-18);staffHit=new Vector3(63,0,-9);
                if(gesture==ReleaseGesture.ElementWeave){leftReady.z=-72;leftHit.z=-16;rightHit.y=35;}
            }
            else if(gesture==ReleaseGesture.IcePush||gesture==ReleaseGesture.SpiritPush)
            {leftReady=new Vector3(-49,18,-50);leftHit=new Vector3(-101,0,-14);rightReady=new Vector3(-65,-12,29);rightHit=new Vector3(-100,11,7);depth=.085f;}
            else if(gesture==ReleaseGesture.FireStride)
            {readyBody=new Vector3(-10,-26,-6);hitBody=new Vector3(23,18,-4);leftReady=new Vector3(-122,19,-47);leftHit=new Vector3(-67,-18,-35);staffReady=new Vector3(18,0,-18);staffHit=new Vector3(41,0,-11);}
            else if(gesture==ReleaseGesture.ChainPoint||gesture==ReleaseGesture.SoulPoint||gesture==ReleaseGesture.GravityPoint)
            {leftReady=new Vector3(-70,34,-39);leftHit=new Vector3(-105,-6,-9);rightReady=new Vector3(-59,-19,22);rightHit=new Vector3(-91,18,5);staffReady=new Vector3(46,0,-18);staffHit=new Vector3(103,0,-4);if(gesture==ReleaseGesture.GravityPoint){hitBody.x=27;depth=.12f;}}
            else if(gesture==ReleaseGesture.Echo||gesture==ReleaseGesture.PackBrace)
            {readyBody=new Vector3(-6,13,0);hitBody=new Vector3(8,-9,0);leftReady=new Vector3(-29,-24,-38);leftHit=new Vector3(-77,-19,-29);rightReady=new Vector3(-34,-14,28);rightHit=new Vector3(-50,-6,22);staffReady=new Vector3(8,0,-13);staffHit=new Vector3(18,0,-13);}
            spine.localRotation*=Pose(Vector3.zero,readyBody,hitBody,t);
            headRig.localRotation*=Pose(Vector3.zero,new Vector3(8,-readyBody.y*.35f,0),new Vector3(-hitBody.x*.4f,-hitBody.y*.45f,0),t);
            rightArm.localRotation=Pose(new Vector3(-10,0,10),rightReady,rightHit,t);
            rightElbow.localRotation=Pose(new Vector3(-16,0,0),new Vector3(-56,0,0),new Vector3(-9,0,0),t);
            staffRig.localRotation=Pose(new Vector3(12,0,-12),staffReady,staffHit,t);
            leftArm.localRotation=Pose(new Vector3(-12,0,-10),leftReady,leftHit,t);
            leftElbow.localRotation=Pose(new Vector3(-20,0,0),new Vector3(-77,0,0),new Vector3(-8,0,0),t);
            pelvis.localPosition+=new Vector3(-load*.025f,-load*depth,drive*.05f);
            spine.localPosition+=new Vector3(0,-load*depth,drive*.07f);
            leftLeg.localRotation*=Quaternion.Euler(-load*8,0,-load*7);
            rightLeg.localRotation*=Quaternion.Euler(load*10,0,load*7);
            if(castingOrb!=null)castingOrb.localScale=Vector3.one*(.21f+load*.15f);
        }
        private void ApplyRangerReleasePose(float t,int slot,ReleaseGesture gesture,float load,float drive)
        {
            if(gesture==ReleaseGesture.WindBrace)
            {
                spine.localRotation*=Pose(Vector3.zero,new Vector3(-5,-23,-9),new Vector3(12,8,-12),t);
                rightArm.localRotation=Pose(new Vector3(-12,0,8),new Vector3(-56,-15,26),new Vector3(-96,-8,23),t);
                rightElbow.localRotation=Pose(new Vector3(-16,0,0),new Vector3(-87,0,0),new Vector3(-60,0,0),t);
                leftArm.localRotation=Pose(new Vector3(-12,0,-8),new Vector3(-27,12,-31),new Vector3(-49,15,-42),t);
                leftElbow.localRotation=Quaternion.Euler(-38,0,0);
                bowRig.localRotation=Quaternion.Euler(15,12,-32);
                bowRig.localPosition=new Vector3(0,-.23f,.04f)-bowRig.localRotation*WeaponAnchorLocal(WeaponVisualAnchor.BowGrip);
                bowstring.SetPosition(1,new Vector3(0,0,.05f));arrowRig.gameObject.SetActive(false);
                pelvis.localPosition+=new Vector3(-load*.035f,-load*.09f,0);spine.localPosition+=new Vector3(0,-load*.065f,0);return;
            }
            bool ground=gesture==ReleaseGesture.TrapSet||gesture==ReleaseGesture.RootSet;
            if(ground)
            {
                float depth=gesture==ReleaseGesture.RootSet?.22f:.18f;
                spine.localRotation*=Pose(Vector3.zero,new Vector3(10,-20,-6),new Vector3(41,10,0),t);
                headRig.localRotation*=Pose(Vector3.zero,new Vector3(5,10,0),new Vector3(-15,-6,0),t);
                leftArm.localRotation=Pose(new Vector3(-12,0,-8),new Vector3(-32,7,-30),new Vector3(-25,0,-48),t);
                leftElbow.localRotation=Pose(new Vector3(-18,0,0),new Vector3(-59,0,0),new Vector3(-31,0,0),t);
                rightArm.localRotation=Pose(new Vector3(-12,0,8),new Vector3(-68,-18,39),new Vector3(-33,8,12),t);
                rightElbow.localRotation=Pose(new Vector3(-16,0,0),new Vector3(-85,0,0),new Vector3(-7,0,0),t);
                bowRig.localRotation=Pose(new Vector3(0,10,-7),new Vector3(18,7,-32),new Vector3(36,10,-49),t);
                bowRig.localPosition=new Vector3(0,-.23f,.04f)-bowRig.localRotation*WeaponAnchorLocal(WeaponVisualAnchor.BowGrip);
                bowstring.SetPosition(1,new Vector3(0,0,.05f));arrowRig.gameObject.SetActive(false);
                pelvis.localPosition+=new Vector3(-load*.07f,-load*depth,drive*.06f);
                spine.localPosition+=new Vector3(0,-load*depth,drive*.09f);
                leftKnee.localRotation*=Quaternion.Euler(load*45,0,0);
                rightKnee.localRotation*=Quaternion.Euler(load*33,0,0);
                return;
            }
            // Grip stays attached to the left hand; the right hand follows the
            // actual nock during tension and pulls back after string release.
            float ready=Mathf.Min(1,Mathf.Min(t/.12f,(1-t)/.28f));
            float draw=t<.3f?Mathf.SmoothStep(0,1,t/.3f):t<.43f?1:1-Mathf.SmoothStep(0,1,(t-.43f)/.09f);
            float high=gesture==ReleaseGesture.RainDraw?.22f:gesture==ReleaseGesture.StarDraw?.32f:gesture==ReleaseGesture.LeapDraw?.13f:0;
            float yaw=gesture==ReleaseGesture.BowFan?-20:gesture==ReleaseGesture.VolleyDraw?-8:gesture==ReleaseGesture.PierceDraw?-17:-13;
            spine.localRotation*=Pose(Vector3.zero,new Vector3(-high*23,yaw,-7),new Vector3(high*16,-yaw*.45f,4),t);
            headRig.localRotation*=Pose(Vector3.zero,new Vector3(high*8,-yaw*.6f,0),new Vector3(-high*12,0,0),t);
            Vector3 bowHand=Vector3.Lerp(new Vector3(-.32f,.1f,.28f),new Vector3(-.035f,.45f+high,.415f),ready);
            AimArm(leftArm,leftElbow,bowHand,new Vector3(-1,-.5f,0));
            bowRig.rotation=spine.rotation*Quaternion.Euler(-high*80,10,-7);
            bowRig.localPosition=new Vector3(0,-.23f,.04f)-bowRig.localRotation*WeaponAnchorLocal(WeaponVisualAnchor.BowGrip);
            Vector3 nock=new Vector3(0,0,.05f-draw*.30f);bowstring.SetPosition(1,nock);arrowRig.localPosition=nock;
            Vector3 drawn=spine.InverseTransformPoint(bowRig.TransformPoint(nock));
            Vector3 stringHand=Vector3.Lerp(new Vector3(.30f,.04f,.22f),drawn,ready);
            float handRelease=Mathf.SmoothStep(0,1,(t-.43f)/.09f)*(1-Mathf.SmoothStep(0,1,(t-.52f)/.48f));
            stringHand+=new Vector3(handRelease*.11f,handRelease*.04f,-handRelease*.12f);
            AimArm(rightArm,rightElbow,stringHand,new Vector3(1,-.2f,-.2f));
            arrowRig.gameObject.SetActive(t<.52f||t>.86f);
            pelvis.localPosition+=new Vector3(load*.025f,-load*(gesture==ReleaseGesture.LeapDraw?.045f:.075f),drive*.045f);
            spine.localPosition+=new Vector3(0,-load*.05f,drive*.035f);
            leftLeg.localRotation*=Quaternion.Euler(-load*7,0,-load*6);
            rightLeg.localRotation*=Quaternion.Euler(load*9,0,load*6);
        }
    }
}
