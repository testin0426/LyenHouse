Shader "Rasta/RenderTextureSyncer/Encoder"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
        _Weight ("Weight (HSVV)", Vector) = (0.5, 0.1, 1, 0)
        _WeightCurve ("Weight Curve (_SV_)", Vector) = (0, 3, 0.2, 0)
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
            Vector _Weight;
            Vector _WeightCurve;
            Vector _Precision;

            float3 rgb2hsv (float3 rgb)
            {
                float3 hsv = 0;

                float rgbMax = (rgb.r >= rgb.g && rgb.r >= rgb.b) ? rgb.r : ((rgb.g >= rgb.r && rgb.g >= rgb.b) ? rgb.g : rgb.b);
                float rgbMin = (rgb.r <= rgb.g && rgb.r <= rgb.b) ? rgb.r : ((rgb.g <= rgb.r && rgb.g <= rgb.b) ? rgb.g : rgb.b);
                
                if (rgb.r == rgb.g && rgb.r == rgb.b)
                {
                    hsv.x = 0;
                }
                else if (rgb.r >= rgb.g && rgb.r >= rgb.b)
                {
                    hsv.x = 60 * (rgb.g - rgb.b)/(rgbMax - rgbMin);
                }
                else if (rgb.g >= rgb.r && rgb.g >= rgb.b)
                {
                    hsv.x = 60 * (rgb.b - rgb.r)/(rgbMax - rgbMin) + 120;
                }
                else
                {
                    hsv.x = 60 * (rgb.r - rgb.g)/(rgbMax - rgbMin) + 240;
                }
                hsv.x = hsv.x < 0 ? 360+hsv.x : hsv.x;
                
                hsv.y = rgbMax != 0 ? (rgbMax - rgbMin)/rgbMax : 0;

                hsv.z = rgbMax;
                
                hsv.x = hsv.x / 360.0;
                
                return hsv;
            }

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

                return rgb / 255.0;
            }

            float4 frag (v2f_customrendertexture IN) : COLOR
            {
                float4 col = 1;

                float2 uv = IN.localTexcoord.xy;
                uint2 uvi = uv * float2(width, height);
                uint2 uvi_output = uv * float2(width / 2, height / 4);
                uint2 block = uint2(uint(uvi.x / blockWidth), uint(uvi.y / blockHeight));
                uint2 blockuv = uint2(uvi.x % blockWidth, uvi.y % blockHeight);

                block = uint2(uvi_output.x / 2, uvi_output.y);
                
                float3 colHSVMax = 0;
                float3 colHSVMin = 0;
                float colHMax180 = 0;
                float colHMin180 = 0;
                for (int _xy = 0; _xy < blockWidth*blockHeight; _xy++){
                    float4 _col4 = tex2D(_MainTex, uint2(block.x * blockWidth + (_xy % blockWidth), block.y * blockHeight + (_xy / blockWidth)) / float2(width - 1, height - 1));
                    float3 _col = rgb2hsv(_col4.rgb);
                    
                    colHSVMax = _xy == 0 ? _col : colHSVMax;
                    colHSVMin = _xy == 0 ? _col : colHSVMin;
                    
                    colHSVMax.x = _col.x > colHSVMax.x ? _col.x : colHSVMax.x;
                    colHSVMax.y = _col.y > colHSVMax.y ? _col.y : colHSVMax.y;
                    colHSVMax.z = _col.z > colHSVMax.z ? _col.z : colHSVMax.z;
                    
                    colHSVMin.x = _col.x < colHSVMin.x ? _col.x : colHSVMin.x;
                    colHSVMin.y = _col.y < colHSVMin.y ? _col.y : colHSVMin.y;
                    colHSVMin.z = _col.z < colHSVMin.z ? _col.z : colHSVMin.z;

                    float _colH180 = _col.x > 0.5 ? 1 - _col.x: _col.x;

                    colHMax180 = _xy == 0 ? _colH180 : colHMax180;
                    colHMin180 = _xy == 0 ? _colH180 : colHMin180;

                    colHMax180 = _colH180 > colHMax180 ? _colH180 : colHMax180;
                    colHMin180 = _colH180 < colHMin180 ? _colH180 : colHMin180;
                }
                float3 colHSVDiff = colHSVMax - colHSVMin;
                colHSVDiff.x = (colHMax180 - colHMin180) < colHSVDiff.x ? colHMax180 - colHMin180 : colHSVDiff.x;
                

                float3 colA = tex2D(_MainTex, uint2(block.x * blockWidth, block.y * blockHeight) / float2(width - 1, height - 1)).rgb;
                float3 colB = tex2D(_MainTex, uint2(block.x * blockWidth, block.y * blockHeight) / float2(width - 1, height - 1)).rgb;
                float dist = 0;
                for (int _xyA = 0; _xyA < blockWidth*blockHeight; _xyA++){
                    float4 _col4A = tex2D(_MainTex, uint2(block.x * blockWidth + (_xyA % blockWidth), block.y * blockHeight + (_xyA / blockWidth)) / float2(width - 1, height - 1));
                    float3 _colA = rgb2hsv(_col4A.rgb);
                    float _colAH180 = _colA.x > 0.5 ? 1 - _colA.x: _colA.x;

                    for (int _xyB = 0; _xyB < blockWidth*blockHeight; _xyB++){
                        float4 _col4B = tex2D(_MainTex, uint2(block.x * blockWidth + (_xyB % blockWidth), block.y * blockHeight + (_xyB / blockWidth)) / float2(width - 1, height - 1));
                        float3 _colB = rgb2hsv(_col4B.rgb);
                        float _colBH180 = _colB.x > 0.5 ? 1 - _colB.x: _colB.x;
                        
                        float _colAS = 1 - pow(1 - _colA.y, _WeightCurve.y);
                        float _colBS = 1 - pow(1 - _colB.y, _WeightCurve.y);
                        float _colAV = pow(_colA.z, _WeightCurve.z);
                        float _colBV = pow(_colB.z, _WeightCurve.z);

                        float _dist     = pow(_colA.x - _colB.x, 2)*_Weight.x         + pow(_colAS - _colBS, 2)*_Weight.y + pow(_colAV - _colBV, 2)*_Weight.z;
                        float _dist180  = pow(_colAH180.x - _colBH180.x, 2)*_Weight.x + pow(_colAS - _colBS, 2)*_Weight.y + pow(_colAV - _colBV, 2)*_Weight.z;
                        _dist = _dist180 < _dist ? _dist180 : _dist;

                        _dist = colHSVMax.z < _Weight.w ? pow(_colAV - _colBV, 2) : _dist;

                        dist = (_xyA == 0 && _xyB == 0) ? _dist : dist;
                        
                        colA = _dist >= dist ? _colA : colA;
                        colB = _dist >= dist ? _colB : colB;
                        dist = _dist >= dist ? _dist : dist;
                    }
                }
                
                float4 k = float4(0.0, 1.0/3.0, 2.0/3.0, 1.0);

                if (uvi_output.x % 2 == 0){
                    colA.y = pow(colA.y, _Precision.y);
                    colB.y = pow(colB.y, _Precision.y);
                    colA.z = pow(colA.z, _Precision.z);
                    colB.z = pow(colB.z, _Precision.z);

                    colA = float3(colA.x, colA.z, colA.y);
                    colB = float3(colB.x, colB.z, colB.y);

                    col = float4(
                    (((0xf9 & (int(round(colA.r * 31))) << 3)) | ((0x07 & (int(round(colA.g * 63))) >> 3))) / 255.0,
                    (((0xe0 & (int(round(colA.g * 63))) << 5)) | ((0x1f & (int(round(colA.b * 31))) >> 0))) / 255.0,
                    (((0xf9 & (int(round(colB.r * 31))) << 3)) | ((0x07 & (int(round(colB.g * 63))) >> 3))) / 255.0,
                    (((0xe0 & (int(round(colB.g * 63))) << 5)) | ((0x1f & (int(round(colB.b * 31))) >> 0))) / 255.0
                    );
                }else{
                    int4 col_output = 0;
                    int col_num = 0;
                    float3 col_comp = 0;
                    float3 col_temp = 0;
                    float4 k = float4(0, 1/3.0, 2/3.0, 1);
                    for (int y = 0; y < blockHeight; y++){
                        for (int x = 0; x < blockWidth; x++){
                            float dist = 0;
                            col_temp = tex2D(_MainTex, int2(block.x * blockWidth + x, block.y * blockHeight + y) / float2(width - 1, height - 1));
                            col_num = 0;
                            for (int i = 0; i < 4; i++){
                                col_comp = hsv2rgb(colA) * (1 - (k[i])) + hsv2rgb(colB) * k[i];

                                float _dist = sqrt(pow(col_temp.r - col_comp.r, 2) + pow(col_temp.g - col_comp.g, 2) + pow(col_temp.b - col_comp.b, 2));

                                dist = i == 0 ? _dist : dist;

                                if (_dist < dist){
                                    col_num = i;
                                    dist = _dist;
                                }
                            }
                            col_output[y] = col_output[y] | (col_num << (x * 2));
                        }
                    }
                    col = col_output / 255.0;
                }

                return col;
            }
            ENDCG
        }
    }
}
