// Raindrop Effect on glass - URP version.
// Samples _CameraOpaqueTexture (the scene rendered behind transparent objects) instead of a
// GrabPass, then refracts/reflects it. The drop math is unchanged from the original Shadertoy
// conversion.

Shader "LyenHouse/Rain"
{
    Properties
    {
        [Header(Rain)]
        _RainAmount ("Rain Amount", Range(0.0, 1.0)) = 0.7
        _RainSpeed  ("Rain Speed",  Range(0.0, 2.0)) = 0.2
        _RainScale  ("Rain Scale",  Range(0.1, 3.0)) = 1.0

        [Header(Lighting)]
        _RefractStrength  ("Refraction Strength", Range(0.0, 0.1))   = 0.02
        _BlurStrength     ("Background Blur",     Range(0.0, 8.0))   = 2.0
        _FresnelColor     ("Fresnel Tint",        Color)             = (0.12, 0.14, 0.18, 1.0)
        _SpecularPower    ("Specular Power",      Range(1.0, 256.0)) = 64.0
        _SpecularStrength ("Specular Strength",   Range(0.0, 2.0))   = 0.6
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "Rain"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            half   _RainAmount;
            half   _RainSpeed;
            half   _RainScale;
            half   _RefractStrength;
            half   _BlurStrength;
            half4  _FresnelColor;
            half   _SpecularPower;
            half   _SpecularStrength;

            #define S(a, b, t) smoothstep(a, b, t)

            float3 N13(float p)
            {
                float3 p3 = frac(p * float3(0.1031, 0.11369, 0.13787));
                p3 += dot(p3, p3.yzx + 19.19);
                return frac(float3((p3.x + p3.y) * p3.z, (p3.x + p3.z) * p3.y, (p3.y + p3.z) * p3.x));
            }

            float N(float t)
            {
                return frac(sin(t * 12345.564) * 7658.76);
            }

            float Saw(float b, float t)
            {
                return S(0.0, b, t) * S(1.0, b, t);
            }

            float2 DropLayer2(float2 uv, float t)
            {
                float2 UV = uv;

                uv.y += t * 0.75;
                float2 a = float2(6.0, 1.0);
                float2 grid = a * 2.0;
                float2 id = floor(uv * grid);

                float colShift = N(id.x);
                uv.y += colShift;

                id = floor(uv * grid);
                float3 n = N13(id.x * 35.2 + id.y * 2376.1);
                float2 st = frac(uv * grid) - float2(0.5, 0.0);

                float x = n.x - 0.5;

                float y = UV.y * 20.0;
                float wiggle = sin(y + sin(y));
                x += wiggle * (0.5 - abs(x)) * (n.z - 0.5);
                x *= 0.7;
                float ti = frac(t + n.z);
                y = (Saw(0.85, ti) - 0.5) * 0.9 + 0.5;
                float2 p = float2(x, y);

                float d = length((st - p) * a.yx);

                float mainDrop = S(0.4, 0.0, d);

                float r = sqrt(S(1.0, y, st.y));
                float cd = abs(st.x - x);
                float trail = S(0.23 * r, 0.15 * r * r, cd);
                float trailFront = S(-0.02, 0.02, st.y - y);
                trail *= trailFront * r * r;

                y = UV.y;
                float trail2 = S(0.2 * r, 0.0, cd);
                float droplets = max(0.0, (sin(y * (1.0 - y) * 120.0) - st.y)) * trail2 * trailFront * n.z;
                y = frac(y * 10.0) + (st.y - 0.5);
                float dd = length(st - float2(x, y));
                droplets = S(0.3, 0.0, dd);
                float m = mainDrop + droplets * r * trailFront;

                return float2(m, trail);
            }

            float StaticDrops(float2 uv, float t)
            {
                uv *= 40.0;

                float2 id = floor(uv);
                uv = frac(uv) - 0.5;
                float3 n = N13(id.x * 107.45 + id.y * 3543.654);
                float2 p = (n.xy - 0.5) * 0.7;
                float d = length(uv - p);

                float fade = Saw(0.025, frac(t + n.z));
                float c = S(0.3, 0.0, d) * frac(n.z * 10.0) * fade;
                return c;
            }

            // Returns float4: xy = analytic normal, z = drop mask, w = trail
            float4 Drops(float2 uv, float t, float l0, float l1, float l2)
            {
                float s = StaticDrops(uv, t) * l0;
                float2 m1 = DropLayer2(uv, t) * l1;
                float2 m2 = DropLayer2(uv * 1.85, t) * l2;

                float c = s + m1.x + m2.x;
                c = S(0.3, 1.0, c);

                // Analytic normal from drop shape via screen-space derivatives.
                float2 norm = float2(ddx(c), ddy(c));

                return float4(norm, c, max(m1.y * l0, m2.y * l1));
            }

            // Cheap blur around the sample point in the opaque scene texture.
            float3 SampleBackground(float2 uv, float blur)
            {
                float2 texel = 1.0 / _ScreenParams.xy;
                float2 off = texel * blur;

                float3 c = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv).rgb * 4.0;
                c += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + float2( off.x, 0.0)).rgb;
                c += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + float2(-off.x, 0.0)).rgb;
                c += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + float2(0.0,  off.y)).rgb;
                c += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + float2(0.0, -off.y)).rgb;
                return c * 0.125;
            }

            struct Attributes
            {
                float4 vertex : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 grabUV : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.vertex.xyz);
                o.grabUV = ComputeScreenPos(o.positionCS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // Screen UV in the opaque texture (0..1).
                float2 UV = i.grabUV.xy / i.grabUV.w;

                // Aspect-corrected, centered UV for the drop field (matches Shadertoy `uv`).
                float2 aspect = _ScreenParams.xy / _ScreenParams.y;
                float2 uv = (UV - 0.5) * aspect;
                uv *= _RainScale;
                uv *= 0.7; // original zoom

                float T = _Time.y;
                float t = T * _RainSpeed;

                float rainAmount = _RainAmount;

                float maxBlur = lerp(0.0, 1.0, rainAmount);
                float minBlur = _BlurStrength;

                float staticDrops = S(-0.5, 1.0, rainAmount) * 2.0;
                float layer1 = S(0.25, 0.75, rainAmount);
                float layer2 = S(0.0, 0.5, rainAmount);

                float4 d = Drops(uv, t, staticDrops, layer1, layer2);
                float2 dropNorm = d.xy * 40.0; // amplify normal
                float drop = d.z;
                float trail = d.w;

                // Refraction: offset the background sample by the analytic normal.
                float2 refractOffset = dropNorm * _RefractStrength;

                // Surface normal for lighting.
                float3 surfN = normalize(float3(dropNorm, 1.0));
                float3 viewDir = float3(0.0, 0.0, 1.0);

                // Fresnel (water IOR ~1.33, F0 = 0.02).
                float cosTheta = max(dot(surfN, viewDir), 0.0);
                float fresnel = 0.02 + 0.98 * pow(1.0 - cosTheta, 5.0);

                // Specular highlight.
                float3 lightDir = normalize(float3(0.5, 0.8, 0.6));
                float3 halfDir = normalize(lightDir + viewDir);
                float spec = pow(max(dot(surfN, halfDir), 0.0), _SpecularPower);

                // Outside drop: light blur. Inside drop: strong blur + refraction.
                float focus = lerp(maxBlur, minBlur, S(0.05, 0.15, drop));
                float2 sampleUV = UV + refractOffset;
                float3 col = SampleBackground(sampleUV, focus);

                // Compose.
                col += fresnel * drop * _FresnelColor.rgb;
                col += spec * _SpecularStrength * drop;
                float rim = S(0.15, 0.5, drop) * S(0.95, 0.5, drop);
                col *= 1.0 - rim * 0.1;
                col += trail * 0.03;

                // Post processing.
                col *= lerp(0.8, 1.0, S(0.0, 0.5, UV.y));
                col *= 1.0 - 0.3 * pow(length(UV - 0.5), 2.0);

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
