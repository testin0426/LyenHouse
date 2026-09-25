Shader "Custom/RainSkybox"
{
    Properties
    {
        [Header(Sky Gradient)]
        _TopColor("Top Color", Color) = (0.00, 0.10, 0.25, 1)
        _BottomColor("Bottom Color", Color) = (0.00, 0.02, 0.08, 1)

        [Header(Rain)]
        _RainColor("Rain Color", Color) = (0.30, 0.60, 1.00, 1)
        _RainIntensity("Rain Intensity", Range(0, 2)) = 1

        [Header(Smoke)]
        _SmokeColor("Smoke Color", Color) = (0.70, 0.75, 0.80, 1)
        _SmokeIntensity("Smoke Intensity", Range(0, 1)) = 0.15
        _SmokeScale("Smoke Scale", Range(0.5, 8)) = 2
        _SmokeSpeed("Smoke Speed", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "RenderPipeline" = "UniversalPipeline" "PreviewType" = "Skybox" }

        Pass
        {
            ZWrite Off
            Cull Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 ray : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _BottomColor;
                half4 _RainColor;
                half _RainIntensity;
                half4 _SmokeColor;
                half _SmokeIntensity;
                half _SmokeScale;
                half _SmokeSpeed;
            CBUFFER_END

            // ---- Simplex noise (Ashima Arts / Ian McEwan, MIT) ----
            #define NOISE_SWIRL_STEPS 2
            #define NOISE_SWIRL_VALUE 1.0
            #define NOISE_SWIRL_STEP_VALUE (NOISE_SWIRL_VALUE / float(NOISE_SWIRL_STEPS))

            float3 _mod289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float4 _mod289(float4 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float4 _permute(float4 x) { return _mod289(((x * 34.0) + 1.0) * x); }
            float4 _taylorInvSqrt(float4 r) { return 1.79284291400159 - 0.85373472095314 * r; }

            float _simplex(float3 v)
            {
                const float2 C = float2(1.0 / 6.0, 1.0 / 3.0);
                const float4 D = float4(0.0, 0.5, 1.0, 2.0);

                float3 i = floor(v + dot(v, C.yyy));
                float3 x0 = v - i + dot(i, C.xxx);

                float3 g = step(x0.yzx, x0.xyz);
                float3 l = 1.0 - g;
                float3 i1 = min(g.xyz, l.zxy);
                float3 i2 = max(g.xyz, l.zxy);

                float3 x1 = x0 - i1 + C.xxx;
                float3 x2 = x0 - i2 + C.yyy;
                float3 x3 = x0 - D.yyy;

                i = _mod289(i);
                float4 p = _permute(_permute(_permute(
                             i.z + float4(0.0, i1.z, i2.z, 1.0))
                           + i.y + float4(0.0, i1.y, i2.y, 1.0))
                           + i.x + float4(0.0, i1.x, i2.x, 1.0));

                float n_ = 0.142857142857; // 1.0 / 7.0
                float3 ns = n_ * D.wyz - D.xzx;

                float4 j = p - 49.0 * floor(p * ns.z * ns.z);

                float4 x_ = floor(j * ns.z);
                float4 y_ = floor(j - 7.0 * x_);

                float4 x = x_ * ns.x + ns.yyyy;
                float4 y = y_ * ns.x + ns.yyyy;
                float4 h = 1.0 - abs(x) - abs(y);

                float4 b0 = float4(x.xy, y.xy);
                float4 b1 = float4(x.zw, y.zw);

                float4 s0 = floor(b0) * 2.0 + 1.0;
                float4 s1 = floor(b1) * 2.0 + 1.0;
                float4 sh = -step(h, float4(0.0, 0.0, 0.0, 0.0));

                float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
                float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;

                float3 p0 = float3(a0.xy, h.x);
                float3 p1 = float3(a0.zw, h.y);
                float3 p2 = float3(a1.xy, h.z);
                float3 p3 = float3(a1.zw, h.w);

                float4 norm = _taylorInvSqrt(float4(dot(p0, p0), dot(p1, p1), dot(p2, p2), dot(p3, p3)));
                p0 *= norm.x;
                p1 *= norm.y;
                p2 *= norm.z;
                p3 *= norm.w;

                float4 m = max(0.6 - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
                m = m * m;
                return 42.0 * dot(m * m, float4(dot(p0, x0), dot(p1, x1), dot(p2, x2), dot(p3, x3)));
            }

            float _fbm3(float3 v)
            {
                float result = _simplex(v);
                result += _simplex(v * 2.0) / 2.0;
                result += _simplex(v * 4.0) / 4.0;
                result /= (1.0 + 1.0 / 2.0 + 1.0 / 4.0);
                return result;
            }

            float _fbm5(float3 v)
            {
                float result = _simplex(v);
                result += _simplex(v * 2.0) / 2.0;
                result += _simplex(v * 4.0) / 4.0;
                result += _simplex(v * 8.0) / 8.0;
                result += _simplex(v * 16.0) / 16.0;
                result /= (1.0 + 1.0 / 2.0 + 1.0 / 4.0 + 1.0 / 8.0 + 1.0 / 16.0);
                return result;
            }

            float _getNoise(float3 v)
            {
                // Make it curl.
                for (int i = 0; i < NOISE_SWIRL_STEPS; i++)
                {
                    v.xy += float2(_fbm3(v), _fbm3(float3(v.xy, v.z + 1000.0))) * NOISE_SWIRL_STEP_VALUE;
                }
                // Normalize.
                return _fbm5(v) / 2.0 + 0.5;
            }

            // Vertical band: 1 where close to a wavy line, fading to 0 with width d.
            float _Band(float2 uv, float d, float o)
            {
                return 1.0 - smoothstep(0.0, d, distance(uv.x, 0.5 + sin(o + uv.y * 3.0) * 0.3));
            }

            // Blue dispersion: three offset bands tinted with blue shades.
            half3 _Streaks(float2 uv, float o)
            {
                float d = 0.05 + abs(sin(o * 0.2)) * 0.25 * distance(uv.y + 0.5, 0.0);

                float r = _Band(uv + float2(d * 0.25, 0.0), d, o);
                float g = _Band(uv - float2(0.015, 0.005), d, o);
                float b = _Band(uv - float2(d * 0.5, 0.015), d, o);

                half3 light = _RainColor.rgb;
                half3 mid   = _RainColor.rgb * 0.5;
                half3 deep  = _RainColor.rgb * 0.2;

                return r * light + g * mid + b * deep;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.ray = IN.positionOS.xyz; // skybox mesh is drawn at the camera, so vertices are view directions
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 dir = normalize(IN.ray);

                // Map the view direction to a full-sky UV (longitude / latitude).
                float2 uv;
                uv.x = atan2(dir.z, dir.x) / TWO_PI + 0.5;
                uv.y = asin(clamp(dir.y, -1.0, 1.0)) / PI + 0.5;

                // Adjustable vertical gradient background (nadir -> zenith).
                half3 background = lerp(_BottomColor.rgb, _TopColor.rgb, saturate(dir.y * 0.5 + 0.5));

                // Blue descending dispersion, layered at different speeds.
                float t = _Time.y;
                half3 rain = (_Streaks(uv, t) +
                              _Streaks(uv, t * 2.0) +
                              _Streaks(uv + float2(0.3, 0.0), t * 3.3)) * 0.5 * _RainIntensity;

                // Light drifting smoke (seamless 3D simplex noise on the view direction).
                float3 p = dir * _SmokeScale;
                p.z += t * _SmokeSpeed;
                float noise = _getNoise(p);
                noise = noise * noise * noise * noise * 2.0; // more contrast
                half3 smoke = noise * _SmokeColor.rgb * _SmokeIntensity;

                return half4(background + rain + smoke, 1.0);
            }
            ENDHLSL
        }
    }
}
