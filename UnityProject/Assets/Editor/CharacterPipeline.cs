using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Reproducible import: retarget authored performances through Unity Humanoid avatars.
public static class CharacterPipeline {
    const string Source = "Assets/Art/Quaternius/";
    public static void Prepare() {
        foreach (string path in Directory.GetFiles(Source, "*.fbx")) {
            var m = (ModelImporter)AssetImporter.GetAtPath(path);
            bool hair = path.Contains("Hair_");
            m.animationType = hair ? ModelImporterAnimationType.None : ModelImporterAnimationType.Human;
            m.avatarSetup = hair ? ModelImporterAvatarSetup.NoAvatar : ModelImporterAvatarSetup.CreateFromThisModel;
            m.importAnimation = path.Contains("UAL");
            m.isReadable = true;
            m.optimizeGameObjects = false;
            m.materialImportMode = ModelImporterMaterialImportMode.None;
            m.animationCompression = ModelImporterAnimationCompression.Optimal;
            m.animationRotationError = .3f;
            m.animationPositionError = .2f;
            m.SaveAndReimport();
            var clips = m.defaultClipAnimations;
            foreach (var c in clips) {
                c.name = c.name.Replace("Armature|", "");
                c.loopTime = c.name.Contains("Loop"); c.loopPose = c.loopTime;
                c.lockRootRotation = true; c.lockRootHeightY = true; c.lockRootPositionXZ = true;
                c.keepOriginalOrientation = true; c.keepOriginalPositionXZ = true;
                c.keepOriginalPositionY = true; c.heightFromFeet = true;
                c.events = Array.Empty<AnimationEvent>();
            }
            if (clips.Length > 0) { m.clipAnimations = clips; m.SaveAndReimport(); }
        }
        foreach (string p in Directory.GetFiles(Source + "Textures", "*.png")) {
            var m = (TextureImporter)AssetImporter.GetAtPath(p);
            m.textureType = p.Contains("Normal") ? TextureImporterType.NormalMap : TextureImporterType.Default;
            m.sRGBTexture = !p.Contains("Normal") && !p.Contains("Roughness");
            m.maxTextureSize = 2048; m.mipmapEnabled = true; m.anisoLevel = 4;
            m.textureCompression = TextureImporterCompression.CompressedHQ;
            m.SaveAndReimport();
        }
        const string libraryPath = "Assets/Resources/CharacterPerformances.asset";
        var library = AssetDatabase.LoadAssetAtPath<PerformanceLibrary>(libraryPath);
        if (library == null) { library = ScriptableObject.CreateInstance<PerformanceLibrary>(); AssetDatabase.CreateAsset(library, libraryPath); }
        library.male = AssetDatabase.LoadAssetAtPath<GameObject>(Source + "Superhero_Male_FullBody.fbx");
        library.female = AssetDatabase.LoadAssetAtPath<GameObject>(Source + "Superhero_Female_FullBody.fbx");
        library.hairMale = AssetDatabase.LoadAssetAtPath<GameObject>(Source + "Hair_Buzzed.fbx");
        library.hairFemale = AssetDatabase.LoadAssetAtPath<GameObject>(Source + "Hair_SimpleParted.fbx");
        library.clips = new[]{"UAL1_Standard.fbx", "UAL2_Standard.fbx"}.SelectMany(n => AssetDatabase.LoadAllAssetsAtPath(Source + n).OfType<AnimationClip>()).Where(c => !c.name.StartsWith("__preview__")).ToArray();
        const string maskPath = "Assets/Resources/CharacterUpperBody.mask";
        var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
        if (mask == null) { mask = new AvatarMask(); AssetDatabase.CreateAsset(mask, maskPath); }
        for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++) mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
        foreach (var part in new[]{AvatarMaskBodyPart.Body, AvatarMaskBodyPart.Head, AvatarMaskBodyPart.LeftArm, AvatarMaskBodyPart.RightArm, AvatarMaskBodyPart.LeftFingers, AvatarMaskBodyPart.RightFingers}) mask.SetHumanoidBodyPartActive(part, true);
        library.upperBody = mask;
        library.skinMale = AssetDatabase.LoadAssetAtPath<Texture2D>(Source + "Textures/T_Superhero_Male_Ligh.png");
        library.skinFemale = AssetDatabase.LoadAssetAtPath<Texture2D>(Source + "Textures/T_Superhero_Female_Light_BaseColor.png");
        library.normalMale = AssetDatabase.LoadAssetAtPath<Texture2D>(Source + "Textures/T_Superhero_Male_Normal.png");
        library.normalFemale = AssetDatabase.LoadAssetAtPath<Texture2D>(Source + "Textures/T_Superhero_Female_Normal.png");
        library.eyes = AssetDatabase.LoadAssetAtPath<Texture2D>(Source + "Textures/T_Eye_Brown.png");
        library.hairColor = AssetDatabase.LoadAssetAtPath<Texture2D>(Source + "Textures/T_Hair_1_BaseColor.png");
        library.hairNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(Source + "Textures/T_Hair_1_Normal.png");
        EditorUtility.SetDirty(library); EditorUtility.SetDirty(mask); AssetDatabase.SaveAssets();
        var report = new List<string>();
        foreach (var model in new[]{library.male, library.female}) {
            var a = model.GetComponent<Animator>();
            report.Add(model.name + " avatar=" + (a != null && a.avatar != null && a.avatar.isValid && a.avatar.isHuman));
            foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>()) report.Add(r.name+" vertices="+r.sharedMesh.vertexCount+" bounds="+r.bounds+" bones="+r.bones.Length+" submeshes="+r.sharedMesh.subMeshCount);
            foreach (var t in model.GetComponentsInChildren<Transform>()) if (new[]{"Head", "pelvis", "foot_l", "hand_r", "spine_03"}.Contains(t.name)) report.Add(t.name+" position="+t.position+" rotation="+t.rotation.eulerAngles);
            if (a == null || a.avatar == null || !a.avatar.isValid || !a.avatar.isHuman) throw new Exception("Invalid Humanoid avatar: " + model.name);
        }
        foreach (var c in library.clips) report.Add(c.name + " duration=" + c.length + " humanoid=" + c.isHumanMotion + " loop=" + c.isLooping);
        Directory.CreateDirectory("../evidence"); File.WriteAllLines("../evidence/character-import.txt", report);
        Debug.Log("CHARACTER_IMPORT_SUCCESS clips=" + library.clips.Length);
    }
}
