Shader "Unlit/SRUniversal"
{
    Properties
    {
        [Header(Base Color)]
        [NoScaleOffset] _BaseMap("Base map (Default white)", 2D) = "white" {}
        _BaseColor("Base color tint (Default white)", Color) = (1,1,1)
        _FrontFaceTintColor("Front face tint color (Default white)", Color) = (1,1,1)
        _BackFaceTintColor("Back face tint color (Default white)", Color) = (1,1,1)
        _Alpha("Alpha (Default 1)", Range(0, 1)) = 1
        _AlphaClip("Alpha clip threshold (Default 0)", Range(0, 1)) = 0

        [Header(Ramp Map)]
        [NoScaleOffset] _CoolRamp("Cool ramp (Default white)", 2D) = "white" {}
        [NoScaleOffset] _WarmRamp("Warm ramp (Default white)", 2D) = "white" {}

        [Header(Indirect Lighting)]
        _IndirectLightFlattenNormal("Indirect light flatten normal (Default 0)", Range(0, 1)) = 0
        _IndirectLightUsage("Indirect light usage (Default 0.5)", Range(0, 1)) = 0.5
        _IndirectLightMixBaseColor("Indirect light mix base color (Default 1)", Range(0, 1)) = 1

        [Header(Main Lighting)]
        _MainLightColorUsage("Main light color usage (Default 1)", Range(0, 1)) = 1
        _ShadowThresholdCenter("Shadow threshold center (Default 0)", Range(-1, 1)) = 0
        _ShadowThresholdSoftness("Shadow threshold softness (Default 0.1)", Range(0, 1)) = 0.1
        _ShadowRampOffset("Shadow ramp offset (Default 0.75)", Range(0, 1)) = 0.75

        [Header(Specular)]
        _SpecularExpon("Specular exponent (Default 50)", Range(1, 128)) = 50
        _SpecularStrength("Specular strength (Default 0.04)", Range(0, 1)) = 0.04
        _SpecularBrightness("Specular brightness (Default 1)", Range(0, 10)) = 1

        [Header(Rim Lighting)]
        _RimLightWidth("Rim light width (Default 1)", Range(0, 10)) = 1
        _RimLightThreshold("Rim light threshold (Default 0.05)", Range(-1, 1)) = 0.05
        _RimLightFadeout("Rim light fadeout (Default 1)", Range(0.01, 1)) = 1
        [HDR] _RimLightTintColor("Rim light tint color (Default white)", Color) = (1,1,1)
        _RimLightBrightness("Rim light brightness (Default 1)", Range(0, 10)) = 1
        _RimLightMixAlbedo("Rim light mix albedo (Default 0.9)", Range(0, 1)) = 0.9

        [Header(Emission)]
        [Toggle(_EMISSION_ON)] _UseEmission("Use emission (Default Off)", Float) = 0
        [NoScaleOffset] _EmissionMap("Emission map (Default white)", 2D) = "white" {}
        [HDR] _EmissionTintColor("Emission tint color (Default white)", Color) = (1,1,1)
        _EmissionIntensity("Emission intensity (Default 1)", Range(0, 100)) = 1

        [Header(Outline)]
        [Toggle(_OUTLINE_ON)] _UseOutline("Use outline (Default Off)", Float) = 0
        _OutlineWidth("Outline width (Default 1)", Range(0, 10)) = 1
        _OutlineColor("Outline color (Default black)", Color) = (0,0,0)
        _OutlineGamma("Outline gamma (Default 16)", Range(1, 255)) = 16

        [Header(Surface Options)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull (Default back)", Float) = 2
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlendMode("Src blend mode (Default One)", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlendMode("Dst blend mode (Default Zero)", Float) = 0
        [Enum(UnityEngine.Rendering.BlendOp)] _BlendOp("Blend operation (Default Add)", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite("ZWrite (Default On)", Float) = 1
        _StencilRef("Stencil reference (Default 0)", Range(0,255)) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp("Stencil comparison (Default disabled)", Int) = 0
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPassOp("Stencil pass operation (Default keep)", Int) = 0
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilFailOp("Stencil fail operation (Default keep)", Int) = 0
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilZFailOp("Stencil Z fail operation (Default keep)", Int) = 0
    }

    SubShader
    {
        LOD 100

        HLSLINCLUDE
        #pragma multi_compile_local _ _OUTLINE_ON
        #pragma shader_feature_local _EMISSION_ON
        ENDHLSL

        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite [_ZWrite]
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite [_ZWrite]
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags{"LightMode" = "DepthNormals"}

            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DrawCore"
            Tags
            {
                "LightMode" = "UniversalForward"
                "RenderType" = "Opaque"
            }

            Cull [_Cull]
            Stencil
            {
                Ref [_StencilRef]
                Comp [_StencilComp]
                Pass [_StencilPassOp]
                Fail [_StencilFailOp]
                ZFail [_StencilZFailOp]
            }

            Blend [_SrcBlendMode] [_DstBlendMode]
            BlendOp [_BlendOp]
            ZWrite [_ZWrite]

            HLSLPROGRAM
            #pragma multi_compile _ MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ SHADOWS_SOFT
            #pragma multi_compile _ FOG

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fog

            #include "SRUniversalInput.hlsl"
            #include "SRUniversalDrawCorePass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DrawOutline"
            Tags
            {
                "RenderPipeline" = "UniversalPipeline"
                "RenderType" = "Opaque"
                "LightMode" = "UniversalForwardOnly"
            }
            Cull Front
            ZWrite [_ZWrite]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #if _OUTLINE_ON
            #include "SRUniversalInput.hlsl"
            #include "SRUniversalDrawOutlinePass.hlsl"
            #else
            struct Attributes {};
            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };
            Varyings vert(Attributes input)
            {
                return (Varyings)0;
            }
            float4 frag(Varyings input) : SV_TARGET
            {
                return 0;
            }
            #endif

            ENDHLSL
        }
    }
}
