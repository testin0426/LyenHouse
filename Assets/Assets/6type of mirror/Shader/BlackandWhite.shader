// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Soooma/MirrorReflection_Grayscale"
{
    Properties
    {
        _MainTex("Base (RGB)", 2D) = "white" {}
        [HideInInspector] _ReflectionTex0("", 2D) = "white" {}
        [HideInInspector] _ReflectionTex1("", 2D) = "white" {}
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

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _ReflectionTex0;
            sampler2D _ReflectionTex1;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
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
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.refl = ComputeNonStereoScreenPos(o.pos);

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // メインテクスチャの取得
                half4 tex = tex2D(_MainTex, i.uv);

                // 反射テクスチャの取得（左右の目で異なるテクスチャ）
                half4 refl = unity_StereoEyeIndex == 0 
                    ? tex2Dproj(_ReflectionTex0, UNITY_PROJ_COORD(i.refl)) 
                    : tex2Dproj(_ReflectionTex1, UNITY_PROJ_COORD(i.refl));

                // 合成
                half4 color = tex * refl;

                // グレースケール化 (輝度計算: 0.299R + 0.587G + 0.114B)
                half luminance = dot(color.rgb, half3(0.299, 0.587, 0.114));

                // 白黒で出力
                return half4(luminance, luminance, luminance, color.a);
            }
            ENDCG
        }
    }
}
