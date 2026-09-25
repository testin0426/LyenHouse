Shader "Rasta/RenderTextureSyncer/Decoder"
{
    Properties
    {
        _MainTex("InputTex", 2D) = "black" {}
        _Precision ("Presision Adjustment (_SV_)", Vector) = (0, 1.5, 0.75, 0)
     }

     SubShader
     {
        Lighting Off
        Blend One Zero

        Pass
        {
            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0
            
            #define width 256.0
            #define height 256.0
            
            #define blockWidth 4.0
            #define blockHeight 4.0

            sampler2D _MainTex;
            Vector _Precision;
            
            
            float3 hsv2rgb (float3 hsv)
            {
                hsv.x = hsv.x * 360;
                hsv.yz = hsv.yz * 255;

                float3 rgb = 0;
                float hsvMax = hsv.z;
                float hsvMin = hsvMax - (hsv.y/255 * hsvMax);

                if (hsv.x <= 60){
                    rgb = float3(hsvMax, (hsv.x / 60) * (hsvMax - hsvMin) + hsvMin, hsvMin);
                }else if (hsv.x <= 120){
                    rgb = float3(((120 - hsv.x) / 60) * (hsvMax - hsvMin) + hsvMin, hsvMax, hsvMin);
                }else if (hsv.x <= 180){
                    rgb = float3(hsvMin, hsvMax, ((hsv.x - 120) / 60) * (hsvMax - hsvMin) + hsvMin);
                }else if (hsv.x <= 240){
                    rgb = float3(hsvMin, ((240 - hsv.x) / 60) * (hsvMax - hsvMin) + hsvMin, hsvMax);
                }else if (hsv.x <= 300){
                    rgb = float3(((hsv.x - 240) / 60) * (hsvMax - hsvMin) + hsvMin, hsvMin, hsvMax);
                }else if (hsv.x <= 360){
                    rgb = float3(hsvMax, hsvMin, ((360 - hsv.x) / 60) * (hsvMax - hsvMin) + hsvMin);
                }

                return rgb / 255;
            }

            float4 frag(v2f_customrendertexture IN) : COLOR
            {
                float4 col = 0;

                float2 uv = IN.localTexcoord.xy;
                int2 uvi = uv * float2(width, height);
                int2 uvi_input = uv * float2(width / 2, height / 4);
                int2 block = int2(int(uvi.x / blockWidth), int(uvi.y / blockHeight));
                int2 blockuv = int2(uvi.x % blockWidth, uvi.y % blockHeight);
                block = int2(uvi_input.x / 2.0, uvi_input.y);
                
                float4 colAB = tex2D(_MainTex, int2(block.x * 2, block.y) / float2((width - 1) / 2, (height - 1) / 4));
                float4 col_nums = tex2D(_MainTex, int2(block.x * 2 + 1, block.y) / float2((width - 1) / 2, (height - 1) / 4));

                float3 colA = float3(
                    (0x1f & ((int(colAB[0] * 255)) >> 3)) / 31.0,
                    ((0x38 & (int(colAB[0] * 255) << 3)) | (0x07 & (int(colAB[1] * 255) >> 5))) / 63.0,
                    (0x1f & ((int(colAB[1] * 255)) >> 0)) / 31.0
                    );
                float3 colB = float3(
                    (0x1f & ((int(colAB[2] * 255)) >> 3)) / 31.0,
                    ((0x38 & (int(colAB[2] * 255) << 3)) | (0x07 & (int(colAB[3] * 255) >> 5))) / 63.0,
                    (0x1f & ((int(colAB[3] * 255)) >> 0)) / 31.0
                    );

                float4 k = float4(0, 1/3.0, 2/3.0, 1);

                int col_num = 0x03 & (int(col_nums[blockuv.y] * 255.0) >> (blockuv.x * 2));
                
                colA = float3(colA.x, colA.z, colA.y);
                colB = float3(colB.x, colB.z, colB.y);
                colA.y = pow(colA.y, 1 / _Precision.y);
                colB.y = pow(colB.y, 1 / _Precision.y);
                colA.z = pow(colA.z, 1 / _Precision.z);
                colB.z = pow(colB.z, 1 / _Precision.z);

                colA = hsv2rgb(colA);
                colB = hsv2rgb(colB);

                col.rgb = colA * (1 - (k[col_num])) + colB * k[col_num];

                return col;
            }
            ENDCG
        }
    }
}