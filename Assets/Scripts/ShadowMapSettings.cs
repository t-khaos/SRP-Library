using UnityEditor.Experimental.GraphView;using UnityEngine;


[System.Serializable]
public class ShadowMapSettings
{
    [Min(0)] public float MaxDistance = 100f;
    public enum TextureSize
    {
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096,
    }
    
    public struct Directional
    {
        public TextureSize AtlasSize;
    }

    public Directional directional = new Directional
    {
        AtlasSize = TextureSize._1024
    };
    
    
}

