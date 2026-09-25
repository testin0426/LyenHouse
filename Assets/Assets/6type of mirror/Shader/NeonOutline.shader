Shader "Soooma/MirrorReflection_NeonEdgeOnly"
{
    Properties
    {
        [HideInInspector] _ReflectionTex0("", 2D) = "white" {}
        [HideInInspector] _ReflectionTex1("", 2D) = "white" {}

        _EdgeColor("Edge Color", Color) = (0, 1, 1, 1) // シアン
        _BackgroundColor("Background Color", Color) = (0, 0, 0, 1) // 黒
        _EdgeThreshold("Edge Threshold", Range(0.1, 5.0)) = 1.0
        _GlowIntensity("Glow Intensity", Range(0.0, 5.0)) = 2.0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityInstancing.cginc"

            sampler2D _ReflectionTex0;
            sampler2D _ReflectionTex1;

            float4 _EdgeColor;
            float4 _BackgroundColor;
            float _EdgeThreshold;
            float _GlowIntensity;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 refl : TEXCOORD1;
                float4 pos : SV_POSITION;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.refl = ComputeNonStereoScreenPos(o.pos);
                return o;
            }

            // Sobelフィルタでエッジを検出
            float DetectEdge(sampler2D tex, float4 screenPos)
            {
                float2 uv = screenPos.xy / screenPos.w;

                float2 texSize = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y);

                // 周囲のピクセルを取得
                float3 TL = tex2D(tex, uv + float2(-texSize.x, texSize.y)).rgb; // 左上
                float3 T  = tex2D(tex, uv + float2(0, texSize.y)).rgb;           // 上
                float3 TR = tex2D(tex, uv + float2(texSize.x, texSize.y)).rgb;  // 右上
                float3 L  = tex2D(tex, uv + float2(-texSize.x, 0)).rgb;         // 左
                float3 R  = tex2D(tex, uv + float2(texSize.x, 0)).rgb;          // 右
                float3 BL = tex2D(tex, uv + float2(-texSize.x, -texSize.y)).rgb;// 左下
                float3 B  = tex2D(tex, uv + float2(0, -texSize.y)).rgb;         // 下
                float3 BR = tex2D(tex, uv + float2(texSize.x, -texSize.y)).rgb; // 右下

                // Sobel演算（水平・垂直方向の差分を計算）
                float3 Gx = -TL - 2.0 * L - BL + TR + 2.0 * R + BR;
                float3 Gy = -TL - 2.0 * T - TR + BL + 2.0 * B + BR;

                // エッジの強さを計算
                return length(Gx + Gy);
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // 反射テクスチャを取得（左右の目で異なるテクスチャ）
                float edge;
                if (unity_StereoEyeIndex == 0)
                    edge = DetectEdge(_ReflectionTex0, i.refl);
                else
                    edge = DetectEdge(_ReflectionTex1, i.refl);

                // 閾値でエッジを強調
                edge = smoothstep(_EdgeThreshold * 0.5, _EdgeThreshold, edge);

                // ネオンエッジと背景色をブレンド
                half4 outline = _EdgeColor * edge * _GlowIntensity;
                half4 finalColor = lerp(_BackgroundColor, outline, edge);

                return finalColor;
            }
            ENDCG
        }
    }
}
