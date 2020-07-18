/*-------------------------------------------------------------------------------------------
 * Copyright (c) Fuyuno Mikazuki / Natsuneko. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 *------------------------------------------------------------------------------------------*/

Shader "Mochizuki/FukidashiSystem/Shader"
{
    Properties
    {
        [NoScaleOffset]
        _Texture             ("Texture"                       , 2D) = "white" {}
        [NoScaleOffset]
        _TextureNo           ("Texture No",           Range(0, 16)) = 0
        [HideInInspector]
        _BackgroundColor     ("Background Color",            Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #pragma vertex   vs
            #pragma fragment fs

            #include "UnityCG.cginc"
            #include "includes/core.cginc"

            ENDCG
        }
    }
}
