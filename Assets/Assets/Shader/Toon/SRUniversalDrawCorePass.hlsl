struct Attributes
{
    float3 positionOS : POSITION;
    half3 normalOS : NORMAL;
    half4 tangentOS : TANGENT;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float2 uv : TEXCOORD0;
    float4 positionWSAndFogFactor : TEXCOORD1; // xyz: positionWS, w: vertex fog factor
    float3 normalWS : TEXCOORD2;
    float3 viewDirectionWS : TEXCOORD3;
    float3 SH : TEXCOORD4;
    float4 positionCS : SV_POSITION;
};

float3 desaturation(float3 color)
{
    float3 grayXfer = float3(0.3, 0.59, 0.11);
    float grayf = dot(color, grayXfer);
    return float3(grayf, grayf, grayf);
}

Varyings vert(Attributes input)
{
    Varyings output = (Varyings)0;

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS);
    VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    output.positionWSAndFogFactor = float4(vertexInput.positionWS, ComputeFogFactor(vertexInput.positionCS.z));
    output.normalWS = vertexNormalInput.normalWS;
    output.viewDirectionWS = unity_OrthoParams.w == 0 ? GetCameraPositionWS() - vertexInput.positionWS : GetWorldToViewMatrix()[2].xyz;
    output.SH = SampleSH(lerp(vertexNormalInput.normalWS, float3(0, 0, 0), _IndirectLightFlattenNormal));
    output.positionCS = vertexInput.positionCS;

    return output;
}

float4 frag(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_TARGET
{
    float3 positionWS = input.positionWSAndFogFactor.xyz;

    float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
    Light mainLight = GetMainLight(shadowCoord);

    float3 lightDirectionWS = normalize(mainLight.direction);
    float3 normalWS = normalize(input.normalWS);
    float3 viewDirectionWS = normalize(input.viewDirectionWS);

    float3 baseColor = tex2D(_BaseMap, input.uv).rgb * _BaseColor.rgb;
    baseColor *= lerp(_BackFaceTintColor, _FrontFaceTintColor, isFrontFace);

    // Indirect light (spherical harmonics, as in the original SRToon).
    float3 indirectLightColor = input.SH.rgb * _IndirectLightUsage;
    indirectLightColor *= lerp(1, baseColor, _IndirectLightMixBaseColor);

    // Toon ramp from the main light.
    float3 mainLightColor = lerp(desaturation(mainLight.color), mainLight.color, _MainLightColorUsage);
    float NoL = dot(normalWS, lightDirectionWS);
    float remappedNoL = NoL * 0.5 + 0.5;
    float mainLightShadow = smoothstep(_ShadowThresholdCenter - _ShadowThresholdSoftness, _ShadowThresholdCenter + _ShadowThresholdSoftness, remappedNoL);

    float rampUVx = mainLightShadow * (1 - _ShadowRampOffset) + _ShadowRampOffset;
    float2 rampUV = float2(rampUVx, 0.5);
    float isDay = lightDirectionWS.y * 0.5 + 0.5;
    float3 rampColor = lerp(tex2D(_CoolRamp, rampUV).rgb, tex2D(_WarmRamp, rampUV).rgb, isDay);
    mainLightColor *= baseColor * rampColor;

    // Specular (Blinn-Phong).
    float3 specularColor = 0;
    {
        float3 halfVectorWS = normalize(viewDirectionWS + lightDirectionWS);
        float NoH = dot(normalWS, halfVectorWS);
        float blinnPhong = pow(saturate(NoH), _SpecularExpon);
        specularColor = blinnPhong * _SpecularStrength * mainLight.color.rgb * _SpecularBrightness;
    }

    // Rim light (depth-offset based).
    float linearEyeDepth = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
    float3 normalVS = mul((float3x3)UNITY_MATRIX_V, normalWS);
    float2 uvOffset = float2(sign(normalVS.x), 0) * _RimLightWidth / (1 + linearEyeDepth) / 100;
    int2 loadTexPos = input.positionCS.xy + uvOffset * _ScaledScreenParams.xy;
    loadTexPos = min(max(loadTexPos, 0), _ScaledScreenParams.xy - 1);
    float offsetSceneDepth = LoadSceneDepth(loadTexPos);
    float offsetLinearEyeDepth = LinearEyeDepth(offsetSceneDepth, _ZBufferParams);
    float rimLight = saturate(offsetLinearEyeDepth - (linearEyeDepth + _RimLightThreshold)) / _RimLightFadeout;

    float3 rimLightColor = rimLight * mainLight.color.rgb;
    rimLightColor *= _RimLightTintColor;
    rimLightColor *= _RimLightBrightness;

    // Emission.
    float3 emissionColor = 0;
    #if _EMISSION_ON
    {
        emissionColor = tex2D(_EmissionMap, input.uv).rgb;
        emissionColor *= _EmissionTintColor;
        emissionColor *= _EmissionIntensity;
    }
    #endif

    // Compose.
    float3 albedo = 0;
    albedo += indirectLightColor;
    albedo += mainLightColor;
    albedo += specularColor;
    albedo += rimLightColor * lerp(1, albedo, _RimLightMixAlbedo);
    albedo += emissionColor;

    float alpha = _Alpha;
    float4 color = float4(albedo, alpha);
    color.rgb = MixFog(color.rgb, input.positionWSAndFogFactor.w);
    return color;
}
