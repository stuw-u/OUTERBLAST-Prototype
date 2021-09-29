// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Cosmic(Surface,Mask)"
{
	Properties
	{
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_OverallCosmicGlow("Overall Cosmic Glow", Float) = 1
		_MaskTex("Mask Tex", 2D) = "white" {}
		_CosmicFieldTex("Cosmic Field Tex", 2D) = "white" {}
		_CosmicFieldSize("Cosmic Field Size", Float) = 4
		_CosmicFieldGlow("Cosmic Field Glow", Float) = 1
		_CosmicFieldXSpeed("Cosmic Field (X) Speed", Float) = 0
		_CosmicFieldYSpeed("Cosmic Field (Y) Speed", Float) = -0.08
		_StarMapTex("Star Map Tex", 2D) = "white" {}
		_StarMapSize("Star Map Size", Float) = 6
		_StarMapGlow("Star Map Glow", Float) = 3
		_StarMapXSpeed("Star Map (X) Speed", Float) = 0
		_StarMapYSpeed("Star Map (Y) Speed", Float) = -0.03
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf Unlit keepalpha addshadow fullforwardshadows 
		struct Input
		{
			float4 screenPos;
			float2 uv_texcoord;
		};

		uniform sampler2D _CosmicFieldTex;
		uniform float _CosmicFieldSize;
		uniform float _CosmicFieldXSpeed;
		uniform float _CosmicFieldYSpeed;
		uniform float _CosmicFieldGlow;
		uniform sampler2D _StarMapTex;
		uniform float _StarMapSize;
		uniform float _StarMapXSpeed;
		uniform float _StarMapYSpeed;
		uniform float _StarMapGlow;
		uniform float _OverallCosmicGlow;
		uniform sampler2D _MaskTex;
		uniform float4 _MaskTex_ST;
		uniform float _Cutoff = 0.5;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float2 temp_output_103_0 = (float2( -0.5,-0.5 ) + ((ase_screenPosNorm).xy - float2( 0,0 )) * (float2( 0.5,0.5 ) - float2( -0.5,-0.5 )) / (float2( 1,1 ) - float2( 0,0 )));
			float2 _TexAnchor = float2(0.5,0.5);
			float2 appendResult20 = (float2(_CosmicFieldXSpeed , _CosmicFieldYSpeed));
			float2 appendResult125 = (float2(_StarMapXSpeed , _StarMapYSpeed));
			o.Emission = ( ( ( tex2D( _CosmicFieldTex, ( ( ( temp_output_103_0 * _CosmicFieldSize ) + _TexAnchor ) + ( appendResult20 * _Time.y ) ) ) * _CosmicFieldGlow ) + ( tex2D( _StarMapTex, ( ( ( temp_output_103_0 * _StarMapSize ) + _TexAnchor ) + ( appendResult125 * _Time.y ) ) ) * _StarMapGlow ) ) * _OverallCosmicGlow ).rgb;
			o.Alpha = 1;
			float2 uv_MaskTex = i.uv_texcoord * _MaskTex_ST.xy + _MaskTex_ST.zw;
			clip( tex2D( _MaskTex, uv_MaskTex ).r - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=16700
289;428;1154;815;1367.259;97.44279;1.720417;True;False
Node;AmplifyShaderEditor.ScreenPosInputsNode;97;-4094.533,64.94771;Float;False;0;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ComponentMaskNode;98;-3698.313,65.55503;Float;False;True;True;False;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TFHCRemapNode;103;-3279.045,71.11132;Float;False;5;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT2;1,1;False;3;FLOAT2;-0.5,-0.5;False;4;FLOAT2;0.5,0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;121;-3247.258,1178.604;Float;False;Property;_StarMapSize;Star Map Size;9;0;Create;True;0;0;False;0;6;6;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;21;-3305.033,462.3578;Float;False;Property;_CosmicFieldXSpeed;Cosmic Field (X) Speed;6;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;120;-3287.868,1550.883;Float;False;Property;_StarMapXSpeed;Star Map (X) Speed;11;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;119;-3286.373,1674.854;Float;False;Property;_StarMapYSpeed;Star Map (Y) Speed;12;0;Create;True;0;0;False;0;-0.03;-0.03;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;22;-3304.653,593.5104;Float;False;Property;_CosmicFieldYSpeed;Cosmic Field (Y) Speed;7;0;Create;True;0;0;False;0;-0.08;-0.08;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;100;-3265.148,269.5914;Float;False;Property;_CosmicFieldSize;Cosmic Field Size;4;0;Create;True;0;0;False;0;4;4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;125;-2940.008,1556.36;Float;True;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;20;-2937.424,468.7572;Float;True;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;99;-2889.563,70.94293;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;102;-2920.511,275.37;Float;False;Constant;_TexAnchor;Tex Anchor;2;0;Create;True;0;0;False;0;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;122;-2892.147,1158.546;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TimeNode;23;-2937.899,780.0723;Float;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;101;-2556.425,70.94853;Float;True;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;19;-2547.923,466.3551;Float;True;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;126;-2550.507,1553.958;Float;True;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;127;-2559.009,1158.552;Float;True;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;9;-2163.392,69.55327;Float;True;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;128;-2156.594,1157.156;Float;True;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;5;-1674.651,284.605;Float;False;Property;_CosmicFieldGlow;Cosmic Field Glow;5;0;Create;True;0;0;False;0;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;129;-1760.536,1127.755;Float;True;Property;_StarMapTex;Star Map Tex;8;0;Create;True;0;0;False;0;580e7698afeaf1a4c95ab9543698e5d8;580e7698afeaf1a4c95ab9543698e5d8;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;133;-1786.609,42.24888;Float;True;Property;_CosmicFieldTex;Cosmic Field Tex;3;0;Create;True;0;0;False;0;66246941a3144274fa8176cfca1bfd95;66246941a3144274fa8176cfca1bfd95;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;130;-1655.429,1381.166;Float;False;Property;_StarMapGlow;Star Map Glow;10;0;Create;True;0;0;False;0;3;3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;131;-1285.853,1133.965;Float;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;3;-1283.27,46.3623;Float;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;269;-855.6257,314.8156;Float;False;Property;_OverallCosmicGlow;Overall Cosmic Glow;1;0;Create;True;0;0;False;0;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;132;-835.915,46.16957;Float;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;268;-423.1159,47.38283;Float;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;271;-524.9131,390.2204;Float;True;Property;_MaskTex;Mask Tex;2;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;270;0,0;Float;False;True;2;Float;ASEMaterialInspector;0;0;Unlit;Cosmic(Surface,Mask);False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Custom;0.5;True;True;0;True;Transparent;;Geometry;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;0;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;98;0;97;0
WireConnection;103;0;98;0
WireConnection;125;0;120;0
WireConnection;125;1;119;0
WireConnection;20;0;21;0
WireConnection;20;1;22;0
WireConnection;99;0;103;0
WireConnection;99;1;100;0
WireConnection;122;0;103;0
WireConnection;122;1;121;0
WireConnection;101;0;99;0
WireConnection;101;1;102;0
WireConnection;19;0;20;0
WireConnection;19;1;23;2
WireConnection;126;0;125;0
WireConnection;126;1;23;2
WireConnection;127;0;122;0
WireConnection;127;1;102;0
WireConnection;9;0;101;0
WireConnection;9;1;19;0
WireConnection;128;0;127;0
WireConnection;128;1;126;0
WireConnection;129;1;128;0
WireConnection;133;1;9;0
WireConnection;131;0;129;0
WireConnection;131;1;130;0
WireConnection;3;0;133;0
WireConnection;3;1;5;0
WireConnection;132;0;3;0
WireConnection;132;1;131;0
WireConnection;268;0;132;0
WireConnection;268;1;269;0
WireConnection;270;2;268;0
WireConnection;270;10;271;1
ASEEND*/
//CHKSM=8333BB75ED97544C81DCCDAE543231E1AF4108AD