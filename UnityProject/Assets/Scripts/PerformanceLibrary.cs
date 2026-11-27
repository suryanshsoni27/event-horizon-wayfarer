using UnityEngine;

// Imported, licensed performances. Native physics remains the authority for movement.
public sealed class PerformanceLibrary : ScriptableObject {
    public GameObject male, female;
    public GameObject hairMale, hairFemale;
    public AvatarMask upperBody;
    public AnimationClip[] clips;
    public Texture2D skinMale, skinFemale, normalMale, normalFemale, eyes;
    public Texture2D hairColor, hairNormal;
    public AnimationClip Clip(string name) {
        foreach (var c in clips) if (c != null && c.name == name) return c;
        throw new System.InvalidOperationException("Missing authored animation: " + name);
    }
    static PerformanceLibrary current;
    public static PerformanceLibrary Current {
        get {
            if (current == null) current = Resources.Load<PerformanceLibrary>("CharacterPerformances");
            if (current == null) throw new System.InvalidOperationException("Run CharacterPipeline.Prepare in Unity before building.");
            return current;
        }
    }
}
