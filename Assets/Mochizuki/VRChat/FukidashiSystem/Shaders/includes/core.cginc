/*-------------------------------------------------------------------------------------------
 * Copyright (c) Fuyuno Mikazuki / Natsuneko. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 *------------------------------------------------------------------------------------------*/

sampler2D _Texture;
float4 _Texture_ST;
sampler2D _CameraMemoryTexture;
float4 _CameraMemoryTexture_ST;
float _TextureNo;
float4 _BackgroundColor;

#ifdef S_RENDERMODE_CONTROLLER
#define  RENDERMODE_CONTROLLER
#endif

#ifdef S_RENDERMODE_VIEWER
#define  RENDERMODE_VIEWER
#endif

#define TEXTURE_PARTS_COUNT 16.0
#define TEXTURE_PARTS_HEIGHT_DIVIDE 8.0
#define TEXTURE_PARTS_WIDTH_DIVIDE 2.0

struct v2f
{
    float4 position : POSITION;
    float2 uv       : TEXCOORD;
};

#include "vertex.cginc"
#include "fragment.cginc"
