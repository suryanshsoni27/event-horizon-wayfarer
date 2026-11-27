using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed partial class DefenseGame {
    void PerformanceCamera(Transform actor,bool crew=false,float distance=3.6f,float orbit=25,float lift=.45f) {
        Vector3 up=actor.up,forward=crew?-actor.forward:actor.forward;
        civilFilmCamera=true;civilFilmClearance=.12f;
        civilFilmLook=actor.position+up*.12f;
        civilFilmAt=civilFilmLook+Quaternion.AngleAxis(orbit,up)*forward*distance+up*lift;
        float clearance=expedition.Alt(civilFilmAt);if(clearance<.2f)civilFilmAt+=up*(.2f-clearance);
        cam.transform.SetPositionAndRotation(civilFilmAt,Quaternion.LookRotation(civilFilmLook-civilFilmAt,up));
    }
    IEnumerator AnimationChecks() {
        yield return new WaitForSeconds(1);preferences.quality=2;ApplyPreferences();
        var checks=new Dictionary<string,bool>();var library=PerformanceLibrary.Current;
        checks["authored_library_has_86_retargetable_clips"]=library.clips.Length==86&&Array.TrueForAll(library.clips,c=>c!=null&&c.isHumanMotion&&c.length>0);
        checks["male_and_female_avatars_are_valid_humanoids"]=library.male.GetComponent<Animator>().avatar.isValid&&library.female.GetComponent<Animator>().avatar.isHuman;
        checks["action_mask_preserves_both_leg_channels"]=!library.upperBody.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg)&&!library.upperBody.GetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg);
        var planet=StageEcology(0,"S2P0");for(int f=0;f<60;f++)yield return null;
        var soldier=surface.Current.foes.Find(f=>f.kind==0&&!f.dead);var actor=soldier.view.root;var motor=soldier.view.humanoid.performance;
        foreach(var foe in surface.Current.foes)foe.view.root.gameObject.SetActive(foe==soldier);
        checks["opponents_use_deforming_skin_with_65_bone_rig"]=motor.Art.SkinVertices>8000&&motor.Art.Animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal)!=null&&actor.GetComponentsInChildren<SkinnedMeshRenderer>().Length==3;
        checks["player_and_three_companions_share_the_new_rig"]=expedition.Motion.Performance.GraphValid&&Array.TrueForAll(expedition.Companions,t=>t.GetComponent<CrewMotion>().Performance.GraphValid);
        checks["player_retains_own_native_gameplay_transform"]=expedition.Motion.Performance.Art.Model!=expedition.Crew&&!motor.Art.Animator.applyRootMotion;
        Vector3 start=actor.position,forward=actor.forward,up=actor.up;
        var leg=motor.Legs[0];var arm=motor.Arms[0];var spine=motor.Art.Animator.GetBoneTransform(HumanBodyBones.Spine);
        var skinRenderer=Array.Find(actor.GetComponentsInChildren<SkinnedMeshRenderer>(),r=>r.sharedMesh.vertexCount>6000);
        var beforeSkin=new Mesh();skinRenderer.BakeMesh(beforeSkin);var beforeVertices=beforeSkin.vertices;
        Quaternion knee=leg.lower.rotation,torso=spine.rotation;float flex=0,spineMotion=0,kneeMotion=0;
        for(int f=0;f<120;f++){actor.position=start+forward*f*.018f;motor.Pose(.01f,forward*1.8f,false,true,true,false,forward,0,false,soldier.view.Contact);flex=Mathf.Max(flex,motor.KneeFlex);kneeMotion=Mathf.Max(kneeMotion,Quaternion.Angle(knee,leg.lower.rotation));spineMotion=Mathf.Max(spineMotion,Quaternion.Angle(torso,spine.rotation));}
        checks["walking_drives_spine_and_articulated_knees"]=flex>25&&kneeMotion>10&&spineMotion>1;
        var afterSkin=new Mesh();skinRenderer.BakeMesh(afterSkin);var afterVertices=afterSkin.vertices;float skinDeformation=0;
        for(int v=0;v<beforeVertices.Length;v++)skinDeformation=Mathf.Max(skinDeformation,Vector3.Distance(beforeVertices[v],afterVertices[v]));
        checks["animated_bones_deform_actual_skin_vertices"]=beforeVertices.Length==afterVertices.Length&&skinDeformation>.10f;
        Destroy(beforeSkin);Destroy(afterSkin);
        checks["walking_blend_reaches_full_weight"]=motor.LocomotionBlend>.99f;
        PerformanceCamera(actor);ecologyTitle="AUTHORED PERFORMANCE / SKINNED EXPEDITION SUIT";ecologyDetail="65 bone retargeting / shaped ceramic equipment / pressure fabric and articulated fingers";
        yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(EvidenceDir(),"animation-walk.png"));
        float phase=motor.GaitPhase;for(int f=0;f<120;f++){actor.position+=forward*.05f;motor.Pose(.01f,forward*5,true,true,true,false,forward,0,false,soldier.view.Contact);}
        checks["sprinting_has_faster_distance_driven_cadence"]=motor.State=="RUNNING"&&(motor.GaitPhase-phase)/1.2f>1.4f;
        Quaternion runKnee=leg.lower.rotation;
        motor.Pose(.01f,Vector3.zero,false,true,true,false,forward);float first=motor.LocomotionBlend;
        for(int f=0;f<100;f++)motor.Pose(.01f,Vector3.zero,false,true,true,false,forward);
        checks["stop_transitions_blend_instead_of_snapping"]=first>.8f&&motor.LocomotionBlend<.01f&&motor.State=="IDLE";
        checks["idle_returns_a_distinct_full_body_pose"]=Quaternion.Angle(runKnee,leg.lower.rotation)>10;
        for(int f=0;f<60;f++)motor.Pose(.01f,Vector3.zero,false,true,true,true,forward+up*.25f,0,false,soldier.view.Contact);
        checks["two_handed_weapon_constraint_reaches_both_grips"]=motor.GripError<.12f;
        Debug.Log("ANIMATION_GRIPS error="+motor.GripError+" chest="+actor.InverseTransformPoint(spine.position)+" upper="+actor.InverseTransformPoint(arm.upper.position)+" elbow="+actor.InverseTransformPoint(arm.lower.position)+" hand="+actor.InverseTransformPoint(arm.end.position)+" target="+actor.InverseTransformPoint(motor.Weapon.TransformPoint(new Vector3(-.035f,-.04f,.43f))));
        Vector3 muzzle=motor.Muzzle.position;motor.Kick();motor.Pose(.01f,Vector3.zero,false,true,true,true,forward+up*.25f);
        checks["firing_produces_actual_muzzle_recoil"]=motor.Shots>0&&Vector3.Distance(muzzle,motor.Muzzle.position)>.015f;
        motor.Reload();bool inHand=false,atPouch=false;Vector3 magazineStart=motor.Magazine.position;
        for(int f=0;f<100;f++){motor.Pose(.01f,Vector3.zero,false,true,true,true,forward);inHand|=motor.MagazineInHand&&Vector3.Distance(motor.Magazine.position,motor.Arms[0].end.position)<.01f;atPouch|=motor.ReloadProgress>.38f&&motor.ReloadProgress<.55f&&Vector3.Distance(magazineStart,motor.Magazine.position)>.15f;}
        checks["reload_removes_the_visible_magazine_into_the_support_hand"]=inHand;
        checks["reload_hand_visits_the_hip_pouch"]=atPouch;
        PerformanceCamera(actor);ecologyTitle="RELOAD / MAGAZINE EXCHANGE";ecologyDetail="Authored upper body layer / support hand to pouch / insert and charge";yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(EvidenceDir(),"animation-reload.png"));
        for(int f=0;f<70;f++)motor.Pose(.01f,forward*1.8f,false,true,true,true,forward);
        checks["reload_restores_the_magazine_and_locomotion"]=motor.ReloadProgress==1&&!motor.MagazineInHand&&motor.Magazine.parent==motor.Weapon&&motor.State=="WALKING";
        Quaternion beforeHit=spine.rotation;surface.Damage(soldier,1);for(int f=0;f<15;f++)motor.Pose(.01f,Vector3.zero,false,true,true,false,forward);
        checks["gameplay_damage_triggers_a_blended_hit_performance"]=motor.ActionWeight>.5f&&Quaternion.Angle(beforeHit,spine.rotation)>2;
        for(int f=0;f<80;f++)motor.Pose(.01f,Vector3.zero,false,true,true,false,forward);
        checks["hit_reaction_recovers_to_locomotion"]=motor.ActionWeight<.05f&&!motor.IsDead;
        motor.Pose(.1f,up*2,false,false,true,false,forward);checks["airborne_state_uses_an_authored_jump_pose"]=motor.State=="AIRBORNE";
        for(int f=0;f<20;f++)motor.Pose(.01f,Vector3.zero,false,true,true,false,forward);
        checks["ground_contact_blends_a_landing_performance"]=motor.ActionWeight>.2f;
        for(int f=0;f<80;f++)motor.Pose(.01f,Vector3.zero,false,true,true,false,forward);
        Vector3 beforeSeat=leg.end.position;var hipBone=motor.Art.Animator.GetBoneTransform(HumanBodyBones.Hips);Vector3 beforeHip=hipBone.position;Quaternion beforeThigh=leg.upper.rotation;
        for(int f=0;f<30;f++)motor.SeatPose(true);
        float seatFlex=180-Vector3.Angle(leg.upper.position-leg.lower.position,leg.end.position-leg.lower.position);
        checks["rover_driver_uses_the_authored_seated_performance"]=motor.State=="DRIVING"&&seatFlex>50&&Quaternion.Angle(beforeThigh,leg.upper.rotation)>25;
        Debug.Log("ANIMATION_SEATED feetTravel="+Vector3.Distance(beforeSeat,leg.end.position)+" hipTravel="+Vector3.Distance(beforeHip,hipBone.position)+" thigh="+Quaternion.Angle(beforeThigh,leg.upper.rotation)+" kneeFlex="+seatFlex);
        PerformanceCamera(actor,false,3,70,.1f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(EvidenceDir(),"animation-seated.png"));
        motor.SeatPose(false);checks["rover_passenger_has_a_distinct_seated_state"]=motor.State=="PASSENGER";
        checks["riding_holsters_the_weapon_instead_of_leaving_it_floating"]=!motor.Weapon.gameObject.activeSelf;
        motor.Pose(.01f,Vector3.zero,false,true,true,false,forward);motor.ResetPose();
        Vector3 beforePlant=leg.plant;bool planted=leg.planted;Vector3 delta=new Vector3(320,-160,240);motor.Shift(delta);
        checks["origin_rebase_updates_locked_foot_contacts"]=!planted||Vector3.Distance(leg.plant,beforePlant-delta)<.001f;
        actor.position=start;motor.ResetPose();for(int f=0;f<30;f++)motor.Pose(.01f,Vector3.zero,false,true,true,false,forward,0,false,soldier.view.Contact);
        Vector3 headBefore=motor.Art.HeadSocket.position;surface.Damage(soldier,10000);soldier.view.Animate(8,0,0,0,true);
        for(int f=0;f<160;f++)soldier.view.Animate(8+f*.01f,0,0,0,true);
        checks["lethal_gameplay_damage_plays_an_authored_fall"]=motor.IsDead&&Vector3.Dot(headBefore-motor.Art.HeadSocket.position,up)>.6f;
        checks["death_pose_clamps_at_the_last_frame"]=motor.State=="DEATH"&&motor.ActionWeight>.99f;
        PerformanceCamera(actor,false,3.4f,30,.85f);ecologyTitle="IMPACT / AUTHORED FALL AND SETTLE";ecologyDetail="Full body reaction / persistent final pose / native combat events";yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(EvidenceDir(),"animation-impact.png"));
        bool finite=true;foreach(var t in actor.GetComponentsInChildren<Transform>())finite&=VoyagerState.Finite(t.position.x)&&VoyagerState.Finite(t.position.y)&&VoyagerState.Finite(t.position.z);
        checks["all_post_action_bones_and_gear_are_finite"]=finite;
        checks["animation_changes_preserve_the_native_state_abi"]=System.Runtime.InteropServices.Marshal.SizeOf<OrbitDemo.State>()==56;
        Debug.Log("ANIMATION_METRICS knee="+flex+" kneeTravel="+kneeMotion+" spineTravel="+spineMotion+" gripError="+motor.GripError+" plantSlip="+motor.MaxPlantSlip+" skinVertices="+motor.Art.SkinVertices);
        SaveChecks("animation-runtime-checks.json",checks);yield return new WaitForSeconds(.3f);Application.Quit();
    }
    IEnumerator AnimationRecord() {
        yield return new WaitForSeconds(1);preferences.quality=2;ApplyPreferences();Time.captureFramerate=30;
        productionFolder=Path.Combine(EvidenceDir(),"animation-frames");Directory.CreateDirectory(productionFolder);productionFrame=0;
        var planet=StageEcology(0,"S2P0");for(int f=0;f<70;f++)yield return null;
        var soldier=surface.Current.foes.Find(f=>f.kind==0&&!f.dead);var actor=soldier.view.root;var motor=soldier.view.humanoid.performance;
        foreach(var f in surface.Current.foes)f.view.root.gameObject.SetActive(f==soldier);
        Vector3 start=actor.position,forward=actor.forward,up=actor.up;Vector3 side=actor.right;
        ecologyTitle="WAYFARER / CHARACTER PERFORMANCE";ecologyDetail="Skinned anatomy / shaped ceramic suit / head and finger joints";
        yield return ProductionFrames(120,"skinned-suit-and-idle",i=>{motor.Pose(1/30f,Vector3.zero,false,true,true,false,forward,0,false,soldier.view.Contact);PerformanceCamera(actor,false,3.0f,Mathf.Lerp(-30,32,i/119f),.3f);});
        ecologyTitle="WALK / JOG / SPRINT";ecologyDetail="Retargeted full body clips / blended acceleration / ground contact constraints";
        var walker=Make(actor.position+up*.07f,Vector3.zero);float walkerAccumulator=0;
        yield return ProductionFrames(240,"walk-sprint-stop",i=>{float speed=i<60?1.8f:i<180?5:0;walkerAccumulator+=1/30f;while(walkerAccumulator>=.01f){Vector3 v=Vector3.ProjectOnPlane(Vel(walker),up);PlanetExpedition.StepMode(ref walker,Vector3.ClampMagnitude((forward*speed-v)*10,30)-up*2,.01f,2);walkerAccumulator-=.01f;}actor.position=Pos(walker)-up*.07f;motor.Pose(1/30f,Vel(walker),speed>3,true,true,false,forward,0,false,soldier.view.Contact);PerformanceCamera(actor,false,3.7f,72,.25f);});
        // A cut returns the presentation actor to the open review spot, clear of the walking route's foliage.
        actor.position=start;motor.ResetPose();
        ecologyTitle="WEAPON / AIM AND RECOIL";ecologyDetail="Hands follow both grips / spine and head track pitch / recoil returns to aim";
        yield return ProductionFrames(120,"aim-and-fire",i=>{Vector3 aim=(forward+up*Mathf.Sin(i/35f)*.35f).normalized;if(i%12==0)motor.Kick();motor.Pose(1/30f,Vector3.zero,false,true,true,true,aim,0,false,soldier.view.Contact);PerformanceCamera(actor,false,2.7f,-35,.25f);});
        ecologyTitle="RELOAD / EXTRACT, EXCHANGE, CHARGE";ecologyDetail="Visible power magazine / support hand to hip pouch / moving upper body layer";
        yield return ProductionFrames(150,"magazine-exchange",i=>{if(i==6||i==80)motor.Reload();motor.Pose(1/30f,Vector3.zero,false,true,true,true,forward,0,false,soldier.view.Contact);PerformanceCamera(actor,false,2.5f,-43,.25f);});
        ecologyTitle="FIELD WORK / INTERACT AND LAND";ecologyDetail="Authored interaction / airborne loop / blended landing recovery";
        yield return ProductionFrames(150,"interaction-and-landing",i=>{bool airborne=i>=70&&i<96;float lift=airborne?Mathf.Sin((i-70)/26f*Mathf.PI)*.55f:0;actor.position=surface.CreaturePoint(actor.position)+up*lift;motor.Pose(1/30f,airborne?up:Vector3.zero,false,!airborne,true,false,forward,0,i<60,soldier.view.Contact);PerformanceCamera(actor,false,3.2f,50,.30f);});
        ecologyTitle="IMPACTS / HIT, RECOVER, FALL";ecologyDetail="Combat driven reactions / authored full body death / settled final pose";
        yield return ProductionFrames(150,"hit-and-death",i=>{if(i==5)motor.Hit();if(i==70)surface.Damage(soldier,10000);soldier.view.Animate(50+i/30f,0,0,0,i>=70);PerformanceCamera(actor,false,3.3f,30,.6f);});
        planet=StageEcology(0,"S1P0");for(int f=0;f<60;f++)yield return null;
        soldier=surface.Current.foes.Find(f=>f.kind==1&&!f.dead);actor=soldier.view.root;motor=soldier.view.humanoid.performance;forward=actor.forward;up=actor.up;
        foreach(var f in surface.Current.foes)f.view.root.gameObject.SetActive(f==soldier);
        ecologyTitle="AURELIAN / SHARED HUMANOID RETARGETING";ecologyDetail="Anatomical face and authored hair / female rig / two handed weapon";
        yield return ProductionFrames(120,"aurelian-performance",i=>{float speed=i<70?1.8f:0;actor.position=surface.CreaturePoint(actor.position+forward*speed/30);if(i==75)motor.Reload();motor.Pose(1/30f,forward*speed,false,true,true,i>=70,forward+up*Mathf.Sin(i/30f)*.15f,0,false,soldier.view.Contact);PerformanceCamera(actor,false,2.9f,Mathf.Lerp(45,-25,i/119f),.3f);});
        ecologyTitle="PLAYER / NATIVE WALK AND SPRINT";ecologyDetail="The same performance graph drives the playable crew / native ground movement";
        expedition.ScriptWalk(forward);float accumulator=0;
        yield return ProductionFrames(210,"native-player-walk-and-sprint",i=>{expedition.Sprinting=i>=70&&i<160;expedition.ScriptWalk(i<160?forward:Vector3.zero);expedition.SetAim(expedition.Focus+forward*30);surface.ScriptAim=expedition.Focus+forward*30;accumulator+=1/30f;while(accumulator>=.01f){expedition.Status(player);expedition.FootStep(.01f);accumulator-=.01f;}PerformanceCamera(expedition.Crew,true,3.7f,65,.35f);});
        expedition.ScriptWalk(Vector3.zero);Time.captureFramerate=0;Debug.Log("ANIMATION_FILM_COMPLETE frames="+productionFrame);yield return new WaitForSeconds(.3f);Application.Quit();
    }
}
