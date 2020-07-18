/*-------------------------------------------------------------------------------------------
 * Copyright (c) Fuyuno Mikazuki / Natsuneko. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 *------------------------------------------------------------------------------------------*/

// #include "core.cginc"

float2 getTextureUVFromNo(float2 uv)
{
    const uint no = (uint) clamp(floor(_TextureNo), 0, 15);
    const float u = uv.x / TEXTURE_PARTS_WIDTH_DIVIDE;
    const float v = uv.y / TEXTURE_PARTS_HEIGHT_DIVIDE;
    const float offsetU = lerp(0.0, 0.5, (uint) fmod(no, TEXTURE_PARTS_WIDTH_DIVIDE));
    const float offsetV = 0.875 - (0.125 * floor(no / TEXTURE_PARTS_WIDTH_DIVIDE));

    return float2(offsetU + u, offsetV + v);
}

float4 fs(v2f i) : SV_TARGET
{
    float4 color;

    if (floor(_TextureNo) == 16) {
        color = float4(1.0, 1.0, 1.0, 1.0);
    } else {
        const float2 uv = getTextureUVFromNo(i.uv);
        color = tex2D(_Texture, uv) * _BackgroundColor;
    }

    return color;
}