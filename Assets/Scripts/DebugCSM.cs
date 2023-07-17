using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class debug : MonoBehaviour
{
    // Start is called before the first frame update
    private ShadowMapping shadowMapping = new ShadowMapping();
    private CSM csm = new CSM();

    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        var camera = Camera.main;
        //初始化相机视锥体
        shadowMapping.frustum.InitByCamera(camera, camera.nearClipPlane, 100);
        //初始化层级视锥体
        for (var i = 0; i < 4; i++)
        {
            var dir = shadowMapping.frustum.farCorners[i] - shadowMapping.frustum.nearCorners[i];

            csm.subShadowMapping[0].frustum.nearCorners[i] = shadowMapping.frustum.nearCorners[i];
            csm.subShadowMapping[0].frustum.farCorners[i] = csm.subShadowMapping[0].frustum.nearCorners[i] + dir * csm.splits[0];

            csm.subShadowMapping[1].frustum.nearCorners[i] = csm.subShadowMapping[0].frustum.farCorners[i];
            csm.subShadowMapping[1].frustum.farCorners[i] = csm.subShadowMapping[1].frustum.nearCorners[i] + dir * csm.splits[1];

            csm.subShadowMapping[2].frustum.nearCorners[i] = csm.subShadowMapping[1].frustum.farCorners[i];
            csm.subShadowMapping[2].frustum.farCorners[i] = csm.subShadowMapping[2].frustum.nearCorners[i] + dir * csm.splits[2];

            csm.subShadowMapping[3].frustum.nearCorners[i] = csm.subShadowMapping[2].frustum.farCorners[i];
            csm.subShadowMapping[3].frustum.farCorners[i] = csm.subShadowMapping[3].frustum.nearCorners[i] + dir * csm.splits[3];
        }

        var lightDir = RenderSettings.sun.transform.forward;
        for (int i = 0; i < 4; i++)
        {
            DebugExtension.DrawFrustum(csm.subShadowMapping[i].frustum, Color.red);
            DebugExtension.DrawFrustumOrientBoundingBox(csm.subShadowMapping[i].frustum, lightDir, Color.blue);
        }
    }
}