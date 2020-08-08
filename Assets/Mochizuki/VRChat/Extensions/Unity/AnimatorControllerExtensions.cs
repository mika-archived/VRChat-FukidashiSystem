/*-------------------------------------------------------------------------------------------
 * Copyright (c) Fuyuno Mikazuki / Natsuneko. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 *------------------------------------------------------------------------------------------*/

#if UNITY_EDITOR

using System.Linq;

using UnityEditor.Animations;

namespace Mochizuki.VRChat.Extensions.Unity
{
    public static class AnimatorControllerExtensions
    {
        public static AnimatorControllerLayer GetLayer(this AnimatorController obj, string name)
        {
            return obj.layers.FirstOrDefault(w => w.name == name);
        }

        public static bool HasLayer(this AnimatorController obj, string name)
        {
            return obj.layers.Any(w => w.name == name);
        }

        public static bool HasParameter(this AnimatorController obj, string name)
        {
            return obj.parameters.Any(w => w.name == name);
        }
    }
}

#endif