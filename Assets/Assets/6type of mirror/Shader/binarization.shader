Shader "Soooma/MirrorReflection_Binary_CustomColors"
{
    Properties
    {
        _MainTex("Base (RGB)", 2D) = "white" {}
        [HideInInspector] _ReflectionTex0("", 2D) = "white" {}
        [HideInInspector] _ReflectionTex1("", 2D) = "white" {}
        _Threshold("Threshold", Range(0, 1)) = 0.5
        _Color1("Color for White", Color) = (1,1,1,1) // 二値化の明るい側の色
        _Color2("Color for Black", Color) = (0,0,0,1) // 二値化の暗い側の色
    }
    SubShader
    {
        Tags{ "RenderType" = "Opaque" }
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

            float _Threshold;
            float4 _Color1; // 白の代わりの色
            float4 _Color2; // 黒の代わりの色

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

                half4 tex = tex2D(_MainTex, i.uv);
                half4 refl = unity_StereoEyeIndex == 0 ? tex2Dproj(_ReflectionTex0, UNITY_PROJ_COORD(i.refl)) : tex2Dproj(_ReflectionTex1, UNITY_PROJ_COORD(i.refl));

                // ミラーの反射テクスチャを適用
                half4 result = tex * refl;

                // 輝度を計算 (NTSC系の重み)
                float brightness = dot(result.rgb, float3(0.299, 0.587, 0.114));

                // 二値化処理
                float binary = brightness > _Threshold ? 1.0 : 0.0;

                // 二値化結果に応じて、指定した色を適用
                return lerp(_Color2, _Color1, binary);
            }
            ENDCG
        }
    }
}
