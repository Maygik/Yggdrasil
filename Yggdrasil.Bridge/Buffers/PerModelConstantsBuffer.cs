using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrasil.Types;

namespace Yggdrasil.Renderer.Buffers
{
    public class PerModelConstantsBuffer
    {
        Matrix4x4 worldMatrix = Matrix4x4.Identity;
    }
}
