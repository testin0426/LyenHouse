Shader "Soooma/MirrorReflection_DistanceBlur"
{
    Properties
    {
        [HideInInspector] _ReflectionTex0("", 2D) = "white" {}
        [HideInInspector] _ReflectionTex1("", 2D) = "white" {}

        _BlurStrength("Blur Strength", Range(0.0, 1.0)) = 0.05
        _MaxBlurDistance("Max Blur Distance", Range(1.0, 20.0)) = 10.0
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

            float _BlurStrength;
            float _MaxBlurDistance;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 worldPos : TEXCOORD1;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 refl : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
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
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // カメラとの距離を計算
                float distanceToCamera = length(_WorldSpaceCameraPos - i.worldPos);

                // ブラーの強さを計算（近いほど 0、遠いほど _BlurStrength）
                float blurFactor = saturate((distanceToCamera - 2.0) / _MaxBlurDistance) * _BlurStrength;

                // ブラー用に周囲のピクセルを取得
                float2 blurOffset = float2(blurFactor, blurFactor);

                half4 center, sample1, sample2, sample3, sample4;

                if (unity_StereoEyeIndex == 0)
                {
                    center  = tex2Dproj(_ReflectionTex0, UNITY_PROJ_COORD(i.refl));
                    sample1 = tex2Dproj(_ReflectionTex0, UNITY_PROJ_COORD(float4(i.refl.xy + blurOffset, i.refl.zw)));
                    sample2 = tex2Dproj(_ReflectionTex0, UNITY_PROJ_COORD(float4(i.refl.xy - blurOffset, i.refl.zw)));
                    sample3 = tex2Dproj(_ReflectionTex0, UNITY_PROJ_COORD(float4(i.refl.xy + float2(blurOffset.x, 0), i.refl.zw)));
                    sample4 = tex2Dproj(_ReflectionTex0, UNITY_PROJ_COORD(float4(i.refl.xy - float2(blurOffset.x, 0), i.refl.zw)));
                }
                else
                {
                    center  = tex2Dproj(_ReflectionTex1, UNITY_PROJ_COORD(i.refl));
                    sample1 = tex2Dproj(_ReflectionTex1, UNITY_PROJ_COORD(float4(i.refl.xy + blurOffset, i.refl.zw)));
                    sample2 = tex2Dproj(_ReflectionTex1, UNITY_PROJ_COORD(float4(i.refl.xy - blurOffset, i.refl.zw)));
                    sample3 = tex2Dproj(_ReflectionTex1, UNITY_PROJ_COORD(float4(i.refl.xy + float2(blurOffset.x, 0), i.refl.zw)));
                    sample4 = tex2Dproj(_ReflectionTex1, UNITY_PROJ_COORD(float4(i.refl.xy - float2(blurOffset.x, 0), i.refl.zw)));
                }

                // ぼかし処理（単純な平均ブラー）
                half4 blurred = (center + sample1 + sample2 + sample3 + sample4) / 5.0;

                return blurred;
            }
            ENDCG
        }
    }
}
