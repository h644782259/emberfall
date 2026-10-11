using UnityEngine;
namespace Emberfall
{
    public static partial class WorldBuilder
    {
        private static void ApplyEnvironmentLighting(bool dungeon,int hub,Light key,Light fill)
        {
            var profile=new EnvironmentLightProfile(dungeon,hub);
            key.intensity=profile.KeyIntensity;fill.intensity=profile.FillIntensity;
            key.transform.localRotation=Quaternion.Euler(profile.KeyElevation,-35,0);
            if(!dungeon)
            {
                // Warm quarry work faces, cool observatory domes and neutral caravan skin tones.
                key.color=profile.Hub==1?new Color(1,.80f,.61f):profile.Hub==2?new Color(.78f,.88f,1):new Color(1,.88f,.72f);
                fill.color=profile.Hub==2?new Color(.67f,.56f,.46f):new Color(.44f,.65f,.77f);
                RenderSettings.ambientSkyColor=new Color(.52f,.62f,.72f)*profile.AmbientScale;
                RenderSettings.ambientEquatorColor=new Color(.30f,.39f,.38f)*profile.AmbientScale;
                RenderSettings.ambientGroundColor=new Color(.19f,.23f,.20f)*profile.AmbientScale;
                RenderSettings.fogColor=profile.Hub==1?new Color(.19f,.15f,.13f):profile.Hub==2?new Color(.10f,.14f,.22f):new Color(.1f,.18f,.24f);
            }
            RenderSettings.fogDensity=profile.FogDensity;
        }
        private static void BuildHubLightPools(Transform parent,int hub)
        {
            // Fixed two unshadowed local accents, not one realtime light per building.
            for(int i=0;i<EnvironmentLightProfile.TownAccentLights;i++)
            {
                Vector3 at=GameSession.HubNpcPosition(i)+new Vector3(0,2.5f,1);
                PointLight(parent,at,i==1?new Color(1,.65f,.33f):hub==2?new Color(.60f,.78f,1):new Color(1,.85f,.60f),i==1?1.1f:.8f,EnvironmentLightProfile.AccentRange);
            }
        }
        private static void BuildPortalFocus(Transform parent,WorldResources r,Vector3 center)
        {
            // Flush, opaque travel inlay; no combat warning ring and no traversal changes.
            Material inset=r.Material(new Color(.30f,.55f,.49f),false,VisualSurface.Metal);
            for(int side=-1;side<=1;side+=2)
                for(int step=0;step<3;step++)
                    Primitive(parent,"Gate approach inset",PrimitiveType.Cube,center+new Vector3(side*(.85f-step*.10f),.05f,-1.35f+step*.32f),new Vector3(.24f,.009f,.27f),inset);
        }
    }
}
