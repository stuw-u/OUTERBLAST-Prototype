// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Cosmic(Unlit)"
{
	Properties
	{
		_OverallCosmicGlow("Overall Cosmic Glow", Float) = 1
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
	}
	
	SubShader
	{
		
		
		Tags { "RenderType"="Opaque" }
		LOD 100

		CGINCLUDE
		#pragma target 3.0
		ENDCG
		Blend Off
		Cull Back
		ColorMask RGBA
		ZWrite On
		ZTest LEqual
		Offset 0 , 0
		
		
		
		Pass
		{
			Name "Unlit"
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"
			#include "UnityShaderVariables.cginc"


			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				
			};
			
			struct v2f
			{
				float4 vertex : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
				float4 ase_texcoord : TEXCOORD0;
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
			
			v2f vert ( appdata v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				float4 ase_clipPos = UnityObjectToClipPos(v.vertex);
				float4 screenPos = ComputeScreenPos(ase_clipPos);
				o.ase_texcoord = screenPos;
				
				float3 vertexValue =  float3(0,0,0) ;
				#if ASE_ABSOLUTE_VERTEX_POS
				v.vertex.xyz = vertexValue;
				#else
				v.vertex.xyz += vertexValue;
				#endif
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}
			
			fixed4 frag (v2f i ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				fixed4 finalColor;
				float4 screenPos = i.ase_texcoord;
				float4 ase_screenPosNorm = screenPos / screenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float2 temp_output_103_0 = (float2( -0.5,-0.5 ) + ((ase_screenPosNorm).xy - float2( 0,0 )) * (float2( 0.5,0.5 ) - float2( -0.5,-0.5 )) / (float2( 1,1 ) - float2( 0,0 )));
				float2 _TexAnchor = float2(0.5,0.5);
				float2 appendResult20 = (float2(_CosmicFieldXSpeed , _CosmicFieldYSpeed));
				float2 appendResult125 = (float2(_StarMapXSpeed , _StarMapYSpeed));
				
				
				finalColor = ( ( ( tex2D( _CosmicFieldTex, ( ( ( temp_output_103_0 * _CosmicFieldSize ) + _TexAnchor ) + ( appendResult20 * _Time.y ) ) ) * _CosmicFieldGlow ) + ( tex2D( _StarMapTex, ( ( ( temp_output_103_0 * _StarMapSize ) + _TexAnchor ) + ( appendResult125 * _Time.y ) ) ) * _StarMapGlow ) ) * _OverallCosmicGlow );
				return finalColor;
			}
			ENDCG
		}
	}
	CustomEditor "ASEMaterialInspector"
	
	
}
/*ASEBEGIN
Version=16700
7;178;1710;833;3510.48;-247.0985;1.649885;True;False
Node;AmplifyShaderEditor.ScreenPosInputsNode;97;-4075.533,16.94771;Float;False;0;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ComponentMaskNode;98;-3679.313,17.55503;Float;False;True;True;False;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;100;-3246.148,221.5914;Float;False;Property;_CosmicFieldSize;Cosmic Field Size;2;0;Create;True;0;0;False;0;4;4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;103;-3260.045,23.11132;Float;False;5;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT2;1,1;False;3;FLOAT2;-0.5,-0.5;False;4;FLOAT2;0.5,0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;121;-3228.258,1130.604;Float;False;Property;_StarMapSize;Star Map Size;7;0;Create;True;0;0;False;0;6;6;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;119;-3267.373,1626.854;Float;False;Property;_StarMapYSpeed;Star Map (Y) Speed;10;0;Create;True;0;0;False;0;-0.03;-0.03;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;120;-3268.868,1502.883;Float;False;Property;_StarMapXSpeed;Star Map (X) Speed;9;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;22;-3285.653,545.5104;Float;False;Property;_CosmicFieldYSpeed;Cosmic Field (Y) Speed;5;0;Create;True;0;0;False;0;-0.08;-0.08;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;21;-3286.033,414.3578;Float;False;Property;_CosmicFieldXSpeed;Cosmic Field (X) Speed;4;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;20;-2918.424,420.7572;Float;True;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TimeNode;23;-2918.899,732.0723;Float;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;102;-2901.511,227.37;Float;False;Constant;_TexAnchor;Tex Anchor;2;0;Create;True;0;0;False;0;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;99;-2870.563,22.94293;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;125;-2921.008,1508.36;Float;True;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;122;-2873.147,1110.546;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;19;-2528.923,418.3551;Float;True;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;127;-2540.009,1110.552;Float;True;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;126;-2531.507,1505.958;Float;True;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;101;-2537.425,22.94853;Float;True;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;9;-2144.392,21.55327;Float;True;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;128;-2133.761,1112.987;Float;True;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;5;-1655.651,236.605;Float;False;Property;_CosmicFieldGlow;Cosmic Field Glow;3;0;Create;True;0;0;False;0;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;130;-1636.429,1333.166;Float;False;Property;_StarMapGlow;Star Map Glow;8;0;Create;True;0;0;False;0;3;3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;133;-1767.609,-5.751117;Float;True;Property;_CosmicFieldTex;Cosmic Field Tex;1;0;Create;True;0;0;False;0;66246941a3144274fa8176cfca1bfd95;66246941a3144274fa8176cfca1bfd95;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;129;-1741.536,1079.755;Float;True;Property;_StarMapTex;Star Map Tex;6;0;Create;True;0;0;False;0;580e7698afeaf1a4c95ab9543698e5d8;580e7698afeaf1a4c95ab9543698e5d8;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;3;-1264.27,-1.637699;Float;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;131;-1266.853,1085.965;Float;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;132;-816.915,-1.830431;Float;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;269;-836.6257,266.8156;Float;False;Property;_OverallCosmicGlow;Overall Cosmic Glow;0;0;Create;True;0;0;False;0;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;268;-404.1159,-0.6171694;Float;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;75;0,0;Float;False;True;2;Float;ASEMaterialInspector;0;1;Cosmic(Unlit);0770190933193b94aaa3065e307002fa;True;Unlit;0;0;Unlit;2;True;0;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;True;0;False;-1;0;False;-1;True;False;True;0;False;-1;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;RenderType=Opaque=RenderType;True;2;0;False;False;False;False;False;False;False;False;False;True;0;False;0;;0;0;Standard;1;Vertex Position,InvertActionOnDeselection;1;0;1;True;False;2;0;FLOAT4;0,0,0,0;False;1;FLOAT3;0,0,0;False;0
WireConnection;98;0;97;0
WireConnection;103;0;98;0
WireConnection;20;0;21;0
WireConnection;20;1;22;0
WireConnection;99;0;103;0
WireConnection;99;1;100;0
WireConnection;125;0;120;0
WireConnection;125;1;119;0
WireConnection;122;0;103;0
WireConnection;122;1;121;0
WireConnection;19;0;20;0
WireConnection;19;1;23;2
WireConnection;127;0;122;0
WireConnection;127;1;102;0
WireConnection;126;0;125;0
WireConnection;126;1;23;2
WireConnection;101;0;99;0
WireConnection;101;1;102;0
WireConnection;9;0;101;0
WireConnection;9;1;19;0
WireConnection;128;0;127;0
WireConnection;128;1;126;0
WireConnection;133;1;9;0
WireConnection;129;1;128;0
WireConnection;3;0;133;0
WireConnection;3;1;5;0
WireConnection;131;0;129;0
WireConnection;131;1;130;0
WireConnection;132;0;3;0
WireConnection;132;1;131;0
WireConnection;268;0;132;0
WireConnection;268;1;269;0
WireConnection;75;0;268;0
ASEEND*/
//CHKSM=4AC427CE592E21E92AAC86099B59FBB132304C1C