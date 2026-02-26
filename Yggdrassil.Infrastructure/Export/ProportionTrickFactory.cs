using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Domain.Project;
using Yggdrassil.Domain.Scene;

namespace Yggdrassil.Infrastructure.Export
{
    public static class ProportionTrickFactory
    {
        // Builds the reference animations for proportion trick
        public static void BuildAnimations(Project project, out SceneModel reference_male, out SceneModel reference_female, out SceneModel proportions)
        {
            // Import the base proportions model, which is a T-pose model with the correct proportions

            // Adjust the proportions bone positions to match the project's rig mapping
            // If the proportions model has a bone that is missing in the rig mapping, move that proportions bone to its child bone's position,
            // and rotate it to match the direction of the child bone in the rig mapping, effectively collapsing that bone
            // This allows 2 spine setups to work, as well as missing clavicles, etc. The proportions model is just a guide for the bone positions and rotations, so it doesn't need to have the same bone hierarchy as the project rig mapping
            // To find the correct position:
            // For each bone, rotate so that the child bone's position is in the correct direction,
            // then scale the position to match the distance to the child bone in the project rig mapping
            // We MUST NOT simply copy the bone positions and rotations, as we need the local axis to be preserved for proportion trick to work

            // Then copy the rotations of the proportions bones to the reference bones
            // Finally, return a SceneModel for each of the three animations: reference_male, reference_female, proportions
            // This is everything needed for proportion trick
            // proportions also works as a ragdoll pose for the model






            throw new NotImplementedException();
        }
    }
}
