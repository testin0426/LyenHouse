struct Attributes
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float fogFactor : TEXCOORD0;
};

float GetCameraFOV()
{
    float t = unity_CameraProjection._m11;
    float Rad2Deg = 180 / 3.1415;
    float fov = atan(1.0f / t) * 2.0 * Rad2Deg;
    return fov;
}

float ApplyOutlineDistanceFadeOut(float inputMulFix)
{
    return saturate(inputMulFix);
}

float GetOutlineCameraFovAndDistanceFixMultiplier(float positionVS_Z)
{
    float cameraMulFix;
    if (unity_OrthoParams.w == 0)
    {
        // Perspective camera: keep the outline roughly the same width on screen
        // across camera distance and FOV.
        cameraMulFix = abs(positionVS_Z);
        cameraMulFix = ApplyOutlineDistanceFadeOut(cameraMulFix);
        cameraMulFix *= GetCameraFOV();
    }
    else
    {
        // Orthographic camera.
        float orthoSize = abs(unity_OrthoParams.y);
        orthoSize = ApplyOutlineDistanceFadeOut(orthoSize);
        cameraMulFix = orthoSize * 50;
    }

    return cameraMulFix * 0.00005;
}

Varyings vert(Attributes input)
{
    Varyings output = (Varyings)0;

    VertexPositionInputs vertexPositionInput = GetVertexPositionInputs(input.positionOS);
    VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    float width = _OutlineWidth;
    width *= GetOutlineCameraFovAndDistanceFixMultiplier(vertexPositionInput.positionVS.z);

    float3 positionWS = vertexPositionInput.positionWS;
    positionWS += vertexNormalInput.normalWS * width;

    output.positionCS = TransformWorldToHClip(positionWS);
    output.fogFactor = ComputeFogFactor(vertexPositionInput.positionCS.z);
    return output;
}

half4 frag(Varyings input) : SV_TARGET
{
    half4 color = half4(_OutlineColor.rgb, 1);
    color.rgb = pow(saturate(color.rgb), _OutlineGamma);
    color.rgb = MixFog(color.rgb, input.fogFactor);
    return color;
}
