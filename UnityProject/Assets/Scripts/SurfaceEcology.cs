using UnityEngine;

// One climate classification is shared by terrain, foliage, encounters and the field guide.
// Species are derived from a destination key, so old checkpoints retain their encounter roles.
public static class SurfaceEcology {
    public enum Biome { Temperate, Arid, Frozen }
    public sealed class Species {
        public readonly Biome biome;
        public readonly int role;
        public readonly string name, tactic;
        public readonly float health, speed, range, windup, cooldown, damage;
        public readonly Color shell, plate, organ;
        public int Id { get { return (int)biome * 3 + role; } }
        public Species(Biome b, int r, string n, string t, float hp, float s, float reach,
                       float tell, float recovery, float hit, Color skin, Color armor, Color glow) {
            biome=b;role=r;name=n;tactic=t;health=hp;speed=s;range=reach;
            windup=tell;cooldown=recovery;damage=hit;shell=skin;plate=armor;organ=glow;
        }
    }
    static Color C(float r,float g,float b,float emission=0) { return new Color(r,g,b,emission); }
    static readonly Species[] species = {
        new Species(Biome.Temperate,0,"MOSS STALKER","Circles its prey. Dodge the mandible lunge.",60,3.2f,2.5f,.7f,1.8f,12,C(.10f,.17f,.105f),C(.26f,.31f,.12f),C(.62f,.75f,.24f,.32f)),
        new Species(Biome.Temperate,1,"SPOREBACK","Retreats at close range. Dodge ballistic spores.",80,2,19,.7f,3.2f,14,C(.20f,.13f,.10f),C(.34f,.26f,.15f),C(.72f,.44f,.16f,.34f)),
        new Species(Biome.Temperate,2,"THORN MATRIARCH","Heavy sweeping bite. Exposed throat is vulnerable.",520,2.5f,4.4f,1,2.4f,24,C(.09f,.14f,.085f),C(.30f,.30f,.14f),C(.78f,.44f,.13f,.4f)),
        new Species(Biome.Arid,0,"DUNE SCORPION","Sprints straight at prey. Sidestep its stinger strike.",72,3.8f,2.8f,.85f,2.25f,16,C(.29f,.17f,.085f),C(.52f,.36f,.18f),C(.65f,.28f,.09f,.22f)),
        new Species(Biome.Arid,1,"GLASSBACK","Long range mineral volleys. Close the distance.",65,2.4f,23,.85f,3,12,C(.25f,.20f,.15f),C(.48f,.42f,.28f),C(.44f,.64f,.59f,.24f)),
        new Species(Biome.Arid,2,"SUNBREAKER","Slow armored siege beast. Watch the broad impact ring.",620,2.2f,4.8f,1.25f,3,30,C(.22f,.13f,.075f),C(.48f,.29f,.12f),C(.9f,.43f,.12f,.35f)),
        new Species(Biome.Frozen,0,"RIME MAW","Tough ambush predator. Bait its slow tusk attack.",82,2.6f,3.1f,.9f,2.1f,18,C(.22f,.28f,.30f),C(.59f,.66f,.63f),C(.22f,.60f,.72f,.25f)),
        new Species(Biome.Frozen,1,"RIMECALLER","Keeps its distance and launches dense ice bolts.",92,1.8f,21,.9f,3.7f,16,C(.20f,.27f,.31f),C(.47f,.61f,.64f),C(.27f,.63f,.81f,.3f)),
        new Species(Biome.Frozen,2,"GLACIER TITAN","Massive tusks and a long recovery. Attack after it swings.",680,1.9f,5.2f,1.3f,3.1f,34,C(.21f,.27f,.28f),C(.63f,.70f,.68f),C(.34f,.70f,.87f,.32f))
    };
    public static Biome Climate(string key) {
        if(!string.IsNullOrEmpty(key)) {
            if(key.EndsWith("P1",System.StringComparison.Ordinal))return Biome.Arid;
            if(key.EndsWith("P2",System.StringComparison.Ordinal))return Biome.Frozen;
        }
        return Biome.Temperate;
    }
    public static Species For(string key,int role) { return species[(int)Climate(key)*3+Mathf.Clamp(role,0,2)]; }
    public static Species For(PlanetExpedition.Planet planet,int role) { return For(planet==null?"HOME":planet.key,role); }
    public static string Habitat(Biome b) { return b==Biome.Arid?"ARID / DUNE FAUNA":b==Biome.Frozen?"FROZEN / CRYOPHILIC FAUNA":"TEMPERATE / CANOPY FAUNA"; }
}
