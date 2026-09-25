#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

CBUFFER_START(UnityPerMaterial)
    sampler2D _BaseMap;
    float4 _BaseMap_ST;
    float4 _BaseColor;

    float3 _FrontFaceTintColor;
    float3 _BackFaceTintColor;
    float _Alpha;
    float _AlphaClip;

    sampler2D _CoolRamp;
    sampler2D _WarmRamp;

    float _IndirectLightFlattenNormal;
    float _IndirectLightUsage;
    float _IndirectLightMixBaseColor;

    float _MainLightColorUsage;
    float _ShadowThresholdCenter;
    float _ShadowThresholdSoftness;
    float _ShadowRampOffset;

    float _SpecularExpon;
    float _SpecularStrength;
    float _SpecularBrightness;

    float _RimLightWidth;
    float _RimLightThreshold;
    float _RimLightFadeout;
    float3 _RimLightTintColor;
    float _RimLightBrightness;
    float _RimLightMixAlbedo;

#ifdef _EMISSION_ON
    sampler2D _EmissionMap;
    float3 _EmissionTintColor;
    float _EmissionIntensity;
#endif

#ifdef _OUTLINE_ON
    float _OutlineWidth;
    float3 _OutlineColor;
    float _OutlineGamma;
#endif
CBUFFER_END
