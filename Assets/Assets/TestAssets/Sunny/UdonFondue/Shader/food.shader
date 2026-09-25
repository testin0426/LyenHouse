// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "sunny/food"
{
	Properties
	{
		_MainTexture("MainTexture", 2D) = "white" {}
		_GlossTexture("GlossTexture", 2D) = "white" {}
		_NormalTexture("NormalTexture", 2D) = "bump" {}
		_normal("normal", Range( 0 , 1)) = 0
		_smooth("smooth", Range( 0 , 1)) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform sampler2D _NormalTexture;
		uniform float4 _NormalTexture_ST;
		uniform float _normal;
		uniform sampler2D _MainTexture;
		uniform float4 _MainTexture_ST;
		uniform sampler2D _GlossTexture;
		uniform float4 _GlossTexture_ST;
		uniform float _smooth;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_NormalTexture = i.uv_texcoord * _NormalTexture_ST.xy + _NormalTexture_ST.zw;
			float2 temp_output_2_0_g3 = uv_NormalTexture;
			float2 break6_g3 = temp_output_2_0_g3;
			float temp_output_25_0_g3 = ( pow( 0.0 , 3.0 ) * 0.1 );
			float2 appendResult8_g3 = (float2(( break6_g3.x + temp_output_25_0_g3 ) , break6_g3.y));
			float4 tex2DNode14_g3 = tex2D( _NormalTexture, temp_output_2_0_g3 );
			float temp_output_4_0_g3 = _normal;
			float3 appendResult13_g3 = (float3(1.0 , 0.0 , ( ( tex2D( _NormalTexture, appendResult8_g3 ).g - tex2DNode14_g3.g ) * temp_output_4_0_g3 )));
			float2 appendResult9_g3 = (float2(break6_g3.x , ( break6_g3.y + temp_output_25_0_g3 )));
			float3 appendResult16_g3 = (float3(0.0 , 1.0 , ( ( tex2D( _NormalTexture, appendResult9_g3 ).g - tex2DNode14_g3.g ) * temp_output_4_0_g3 )));
			float3 normalizeResult22_g3 = normalize( cross( appendResult13_g3 , appendResult16_g3 ) );
			o.Normal = normalizeResult22_g3;
			float2 uv_MainTexture = i.uv_texcoord * _MainTexture_ST.xy + _MainTexture_ST.zw;
			o.Albedo = tex2D( _MainTexture, uv_MainTexture ).rgb;
			float2 uv_GlossTexture = i.uv_texcoord * _GlossTexture_ST.xy + _GlossTexture_ST.zw;
			o.Smoothness = ( tex2D( _GlossTexture, uv_GlossTexture ).r * _smooth );
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18935
-1936;85;1920;929;97.13451;476.9625;1;True;True
Node;AmplifyShaderEditor.SamplerNode;43;528.8837,122.2976;Inherit;True;Property;_GlossTexture;GlossTexture;3;0;Create;True;0;0;0;False;0;False;-1;None;372491522b3d05a448815f12ff5c92e4;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;45;543.8655,324.0375;Inherit;False;Property;_smooth;smooth;6;0;Create;True;0;0;0;False;0;False;0;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;40;395.584,-216.6024;Inherit;True;Property;_NormalTexture;NormalTexture;4;0;Create;True;0;0;0;False;0;False;None;ab5b15b2f58212f44a3bcf39179092ec;True;bump;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.RangedFloatNode;41;405.8838,-18.70242;Inherit;False;Property;_normal;normal;5;0;Create;True;0;0;0;False;0;False;0;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;2;452.4631,-444.3513;Inherit;True;Property;_MainTexture;MainTexture;0;0;Create;True;0;0;0;False;0;False;-1;None;cc87cac2f91000949902b20d58471972;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;46;914.8655,150.0375;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;38;810.0624,-113.9198;Inherit;False;NormalCreate;1;;3;e12f7ae19d416b942820e3932b56220f;0;4;1;SAMPLER2D;;False;2;FLOAT2;0,0;False;3;FLOAT;0;False;4;FLOAT;2;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;31;1187.515,-105.9785;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;sunny/food;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;18;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;46;0;43;1
WireConnection;46;1;45;0
WireConnection;38;1;40;0
WireConnection;38;4;41;0
WireConnection;31;0;2;0
WireConnection;31;1;38;0
WireConnection;31;4;46;0
ASEEND*/
//CHKSM=0D146F444ED93760D15880EBBDC05E652ECADAD5