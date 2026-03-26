using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Types;

namespace Yggdrassil.Renderer.Buffers
{
    public class PerFrameConstantsBuffer
    {
        public Matrix4x4 WorldViewProjection = Matrix4x4.Identity;
        public Vector3 LightDirection;
        public float Padding1;

        public Vector3 CameraPosition;
        public float Padding2;

        public Vector3 CameraFacing;
        public float Padding3;
    }
}
