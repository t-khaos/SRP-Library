using UnityEngine;

public class CullingInfo
{
      public ComputeBuffer CullingResult;
      public Frustum CullingFrustum;

      public CullingInfo(ComputeShader cs, int instanceCount)
      {
            CullingResult = new ComputeBuffer(instanceCount, sizeof(float) * 16, ComputeBufferType.Append);
            CullingFrustum = new Frustum();
      }
}