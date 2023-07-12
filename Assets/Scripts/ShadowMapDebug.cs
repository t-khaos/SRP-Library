using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowMapDebug : MonoBehaviour
{
    private ShadowMapping _shadowMapping;
    // Start is called before the first frame update
    void Start()
    {
        _shadowMapping = new ShadowMapping();
    }

    // Update is called once per frame
    void Update()
    {
        Light light = RenderSettings.sun;
        Vector3 lightDir = light.transform.forward;
        _shadowMapping.UpdateFrustem(Camera.main, lightDir);
        _shadowMapping.DebugFrustem();
    }
}
