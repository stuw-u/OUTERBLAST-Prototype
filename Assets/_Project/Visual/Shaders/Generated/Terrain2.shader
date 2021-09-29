Shader "Terrain2"
{
	Properties 
	{
		TopColorA("Top Color A", Color) = (0.3250979, 0.8301887, 0.3679773, 0)
		TopColorB("Top Color B", Color) = (0.3064468, 0.764151, 0.5423112, 0)
		TopPatternScale("Top Pattern Scale", Float) = 1
		SideColorTop("Side Color Top", Color) = (0.8018868, 0.6532211, 0.5409641, 0)
		SideColorBottom("Side Color Bottom", Color) = (0.6320754, 0.3469485, 0.3250364, 0)
		SideColorHeightRange("Side Color Height Range", Vector) = (32, -32, 0, 0)
		BurntColor("Burnt Color", Color) = (0.09141865, 0, 0.1415094, 0.8431373)
		TriplanarColorBlend("Color Triplanar Blend", Float) = 8
		TriplanarNormalBlend("Color Normal Blend", Float) = 8
		EmitValue("Emit", Float) = 0.16
		TopNormalScale("Top Normal Scale", Float) = 0.2
		SideNormalScale("Side Normal Scale", Float) = 0.2
		TopNormalMap("Top Normal Map", 2D) = "bump" {}
		TopNormalAmplitude("Top Normal Amplitude", Float) = 1.0
		SideNormalMap("Side Normal Map", 2D) = "bump" {}
		SideOcclusionMap("Side Occlusion Map", 2D) = "white" {}
		SideNormalAmplitude("Side Normal Amplitude", Float) = 1.0
		SideOcclusionAmplitude("Side Occlusion Amplitude", Float) = 1.0
	}

	SubShader 
	{
		Tags 
		{
			"RenderPipeline" = "UniversalPipeline"
			"RenderType" = "Opaque"
			"Queue" = "Geometry+0"
		}

		// Surface
		Pass 
		{
			Name "Universal Forward"
			Tags { "LightMode" = "UniversalForward" }

			// Render State
			Blend Off
			Cull Back
			ZTest LEqual
			ZWrite On

			HLSLPROGRAM

			// Pragmas, Includes and Attributes / Varyings
			#if 1 
			#pragma exclude_renderers gles gles3 glcore
			#pragma target 4.5

			// Material Keywords
			#pragma shader_feature_local _NORMALMAP
			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
			#pragma shader_feature_local_fragment _EMISSION
			//#pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
			//#pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#pragma shader_feature_local_fragment _OCCLUSIONMAP
			//#pragma shader_feature_local _PARALLAXMAP
			//#pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
			//#pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
			//#pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
			//#pragma shader_feature_local_fragment _SPECULAR_SETUP
			#pragma shader_feature_local _RECEIVE_SHADOWS_OFF

			// Universal Pipeline keywords
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile_fragment _ _SHADOWS_SOFT
			#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
			#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
			#pragma multi_compile _ SHADOWS_SHADOWMASK

			// Unity defined keywords
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile_fog

			// GPU Instancing
			#pragma multi_compile_instancing

			// Includes
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
			//#include "TerrainUtils.hlsl"
			//#include "Assets/_Project/Visual/Shaders/Generated/TerrainUtils.hlsl"
			#include "Assets/_Project/Visual/Shaders/Generated/TerrainLighting.hlsl"

			#pragma vertex vert
			#pragma fragment frag

			// Shader Properties
			CBUFFER_START(UnityPerMaterial)
			float4 TopColorA;
			float4 TopColorB;
			float TopPatternScale;
			float4 SideColorTop;
			float4 SideColorBottom;
			float2 SideColorHeightRange;
			float4 BurntColor;
			float TriplanarColorBlend;
			float TriplanarNormalBlend;
			float EmitValue;
			float TopNormalScale;
			float SideNormalScale;
			float TopNormalAmplitude;
			float SideNormalAmplitude;
			float SideOcclusionAmplitude;
			CBUFFER_END
			TEXTURE2D(TopNormalMap);
			SAMPLER(sampler_TopNormalMap);
			TEXTURE2D(SideNormalMap);
			SAMPLER(sampler_SideNormalMap);
			TEXTURE2D(SideOcclusionMap);
			SAMPLER(sampler_SideOcclusionMap);

			struct Attributes {
				float4 positionOS   : POSITION;
				float3 normalOS     : NORMAL;
				float4 tangentOS    : TANGENT;
				float4 uv           : TEXCOORD0;
				float2 lightmapUV   : TEXCOORD1;
			};

			struct Varyings {
				float4 positionCS               : SV_POSITION;
				float4 topPatternColor          : COLOR;
				float2 cutAndBurntValue			: TEXCOORD0;
				DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);
				// Note this macro is using TEXCOORD1
				#ifdef REQUIRES_WORLD_SPACE_POS_INTERPOLATOR
				float3 positionWS               : TEXCOORD2;
				#endif
				float3 normalWS                 : TEXCOORD3;
				#ifdef _NORMALMAP
				float4 tangentWS                : TEXCOORD4;
				#endif
				float3 viewDirWS                : TEXCOORD5;
				half4 fogFactorAndVertexLight   : TEXCOORD6;
				// x: fogFactor, yzw: vertex light
				#ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
				float4 shadowCoord              : TEXCOORD7;
				#endif
			};
			#endif

			// Init Input
			#if 1
			InputData InitializeInputData(Varyings IN, float3 normalWS) {
				InputData inputData = (InputData)0;

				#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
				inputData.positionWS = IN.positionWS;
				#endif

				half3 viewDirWS = SafeNormalize(IN.viewDirWS);
				inputData.normalWS = normalWS;
				/*#ifdef _NORMALMAP
				float sgn = IN.tangentWS.w; // should be either +1 or -1
				float3 bitangent = sgn * cross(IN.normalWS.xyz, IN.tangentWS.xyz);
				inputData.normalWS = TransformTangentToWorld(normalTS, half3x3(IN.tangentWS.xyz, bitangent.xyz, IN.normalWS.xyz));
				#else
				inputData.normalWS = IN.normalWS;
				#endif*/

				inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
				inputData.viewDirectionWS = viewDirWS;

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				inputData.shadowCoord = IN.shadowCoord;
				#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
				inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
				#else
				inputData.shadowCoord = float4(0, 0, 0, 0);
				#endif

				inputData.fogCoord = IN.fogFactorAndVertexLight.x;
				inputData.vertexLighting = IN.fogFactorAndVertexLight.yzw;
				inputData.bakedGI = SAMPLE_GI(IN.lightmapUV, IN.vertexSH, inputData.normalWS);
				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
				return inputData;
			}
			#endif

			// Surface Data
			#if 1
			SurfaceData InitializeSurfaceData(Varyings IN) {
				SurfaceData surfaceData = (SurfaceData)0;
				// Note, we can just use SurfaceData surfaceData; here and not set it.
				// However we then need to ensure all values in the struct are set before returning.
				// By casting 0 to SurfaceData, we automatically set all the contents to 0.




				float3 positionWS = IN.positionWS;
				half3 viewDirectionWS = SafeNormalize(GetCameraPositionWS() - positionWS);
				float3 normalWS = normalize(IN.normalWS);																								// World Normal
				float noiseValue = noise(positionWS * 1.5) * 0.3 + noise(positionWS * 3)*0.15 + noise(positionWS * 9)*0.05;								// General Usage Noise Value
				float4 topColor = IN.topPatternColor;																									// Top Color
				float4 sideColor = lerp(SideColorTop, SideColorBottom, saturate(unlerp(SideColorHeightRange.x, SideColorHeightRange.y, positionWS.y)));	// Side Color
				float cutValue = 1.0 - step(IN.cutAndBurntValue.g, 0.5 + noiseValue * 0.2);																// Cut Value
				float burntValue = lerp(0.0, BurntColor.a, IN.cutAndBurntValue.r);																		// Burnt Value
				float smoothCutValue = 1.0 - saturate(IN.cutAndBurntValue.g + noiseValue * 0.2);														// Smooth Cut Value
				float4 trueTopColor = lerp(topColor, sideColor, cutValue);


				// Normals
				#if 1
				// Triplanar normals
				float blendNormal = TriplanarNormalBlend;
				float3 TriplanarNormal_UVTOP = positionWS * TopNormalScale;
				float3 TriplanarNormal_UV = positionWS * SideNormalScale;

				float4 powBlendOcclusion = SafePositivePow_float(float4(normalWS.x, max(0.0, -normalWS.y), max(0.0, normalWS.y), normalWS.z), min(blendNormal, floor(log2(Min_float()) / log2(1 / sqrt(3)))));
				float4 triplanarBlendOcclusion = powBlendOcclusion / dot(powBlendOcclusion, float4(1, 1, 1, 1));

				float4 TriplanarNormal_Blend = SafePositivePow_float(float4(normalWS.x, max(0.0, -normalWS.y), max(0.0, normalWS.y), normalWS.z), min(blendNormal, floor(log2(Min_float()) / log2(1 / sqrt(3)))));
				TriplanarNormal_Blend /= (TriplanarNormal_Blend.x + TriplanarNormal_Blend.y + TriplanarNormal_Blend.z + TriplanarNormal_Blend.w).xxxx;

				float3 TriplanarNormal_X = UnpackNormal(SAMPLE_TEXTURE2D(SideNormalMap, sampler_SideNormalMap, TriplanarNormal_UV.zy)) * float4(SideNormalAmplitude, SideNormalAmplitude, 1.0, 1.0);
				float3 TriplanarNormal_YBottom = UnpackNormal(SAMPLE_TEXTURE2D(SideNormalMap, sampler_SideNormalMap, TriplanarNormal_UV.xz)) * float4(SideNormalAmplitude, SideNormalAmplitude, 1.0, 1.0);
				float3 TriplanarNormal_YTop = UnpackNormal(SAMPLE_TEXTURE2D(TopNormalMap, sampler_TopNormalMap, TriplanarNormal_UVTOP.xz)) * float4(TopNormalAmplitude, TopNormalAmplitude, 1.0, 1.0);
				float3 TriplanarNormal_Z = UnpackNormal(SAMPLE_TEXTURE2D(SideNormalMap, sampler_SideNormalMap, TriplanarNormal_UV.xy)) * float4(SideNormalAmplitude, SideNormalAmplitude, 1.0, 1.0);

				float TriplanarOcclusion_X = 1.0 - SAMPLE_TEXTURE2D(SideOcclusionMap, sampler_SideOcclusionMap, TriplanarNormal_UV.zy).r;
				float TriplanarOcclusion_YBottom = 1.0 - SAMPLE_TEXTURE2D(SideOcclusionMap, sampler_SideOcclusionMap, TriplanarNormal_UV.xz).r;
				float TriplanarOcclusion_Z = 1.0 - SAMPLE_TEXTURE2D(SideOcclusionMap, sampler_SideOcclusionMap, TriplanarNormal_UV.xy).r;

				TriplanarNormal_X = float3(TriplanarNormal_X.xy + normalWS.zy, abs(TriplanarNormal_X.z) * normalWS.x);
				TriplanarNormal_YBottom = float3(TriplanarNormal_YBottom.xy + normalWS.xz, abs(TriplanarNormal_YBottom.z) * normalWS.y);
				TriplanarNormal_YTop = float3(TriplanarNormal_YTop.xy + normalWS.xz, abs(TriplanarNormal_YTop.z) * normalWS.y);
				TriplanarNormal_Z = float3(TriplanarNormal_Z.xy + normalWS.xy, abs(TriplanarNormal_Z.z) * normalWS.z);
				normalWS = float4(normalize(
					TriplanarNormal_X.zyx * TriplanarNormal_Blend.x +
					TriplanarNormal_YBottom.xzy * TriplanarNormal_Blend.y +
					lerp(TriplanarNormal_YTop.xzy * TriplanarNormal_Blend.z, TriplanarNormal_YBottom.xzy * TriplanarNormal_Blend.z, cutValue) +
					TriplanarNormal_Z.xyz * TriplanarNormal_Blend.w), 1);
				float occlusion = (triplanarBlendOcclusion.x * TriplanarOcclusion_X + triplanarBlendOcclusion.y * TriplanarOcclusion_YBottom + triplanarBlendOcclusion.z * 0.0 + triplanarBlendOcclusion.w * TriplanarOcclusion_Z) * SideOcclusionAmplitude;
				#endif

				

				
				// Colors
				#if 1

				// Triplanar Blend Values
				float4 powBlend = pow(float4(abs(normalWS.x), max(0.0, normalWS.y), max(0.0, -normalWS.y), abs(normalWS.z)), TriplanarColorBlend);
				powBlend.y = 1.0 - saturate(step(powBlend.y, noiseValue));
				float4 triplanarBlend = powBlend / dot(powBlend, float4(1, 1, 1, 1));

				// Base Terrain Color
				float3 mulR = triplanarBlend.r * sideColor.rgb;
				float3 mulG = triplanarBlend.g * trueTopColor.rgb;
				float3 mulB = triplanarBlend.b * sideColor.rgb;
				float3 mulA = triplanarBlend.a * sideColor.rgb;
				float3 baseTerrainColor = mulR + mulG + mulB + mulA;
				float3 trueTerrainColor = lerp(baseTerrainColor, BurntColor.rgb, burntValue); // Burnt Value

				// Emission
				float fresnelValue = fresnelEffect(normalWS, viewDirectionWS, 1.0);
				float3 baseEmitColor = lerp(baseTerrainColor * EmitValue.xxx, BurntColor.rgb, burntValue);
				float3 trueEmitColor = baseEmitColor * fresnelValue.xxx;
				#endif


				// Not supporting the metallic/specular map or occlusion map
				// for an example of that see : /Shaders/LitInput.hlsl
				surfaceData.albedo = trueTerrainColor * max(saturate(1.0 - occlusion), 0.2);
				surfaceData.alpha = 1.0;
				surfaceData.smoothness = 0.0;
				surfaceData.normalTS = normalWS; //NOTE: surfaceTS in this case is in WS!
				surfaceData.emission = trueEmitColor;
				surfaceData.occlusion = occlusion;
				return surfaceData;
			}
			#endif

			// Verts
			#if 1
			Varyings vert(Attributes IN) {
				Varyings OUT;


				// Vertex Position
				VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
				OUT.positionCS = positionInputs.positionCS;
				#ifdef REQUIRES_WORLD_SPACE_POS_INTERPOLATOR
				OUT.positionWS = positionInputs.positionWS;
				#endif


				// UVs & Vertex Colour
				OUT.cutAndBurntValue = IN.uv.xy;
				OUT.topPatternColor.xyz = sampleGroundColor(OUT.positionWS.xz * TopPatternScale.xx, TopColorA.rgb, TopColorB.rgb);


				// View Direction
				OUT.viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);


				// Normals & Tangents
				VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
				OUT.normalWS = normalInputs.normalWS;
				#ifdef _NORMALMAP
				real sign = IN.tangentOS.w * GetOddNegativeScale();
				OUT.tangentWS = half4(normalInputs.tangentWS.xyz, sign);
				#endif


				// Vertex Lighting & Fog
				half3 vertexLight = VertexLighting(positionInputs.positionWS, normalInputs.normalWS);
				half fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
				OUT.fogFactorAndVertexLight = half4(fogFactor, vertexLight);


				// Baked Lighting & SH (used for Ambient if there is no baked)
				OUTPUT_LIGHTMAP_UV(IN.lightmapUV, unity_LightmapST, OUT.lightmapUV);
				OUTPUT_SH(OUT.normalWS.xyz, OUT.vertexSH);


				// Shadow Coord
				#ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
				OUT.shadowCoord = GetShadowCoord(positionInputs);
				#endif
				return OUT;
			}
			#endif

			// Frag
			#if 1
			half4 frag(Varyings IN) : SV_Target{
				SurfaceData surfaceData = InitializeSurfaceData(IN);
				InputData inputData = InitializeInputData(IN, surfaceData.normalTS); //NOTE: surfaceTS in this case is in WS!

				half4 color = Modif_UniversalFragmentPBR(inputData, surfaceData);
				color.rgb = MixFog(color.rgb, IN.fogFactorAndVertexLight.x);
				return color;
			}
			#endif

			ENDHLSL
		}

		// ShadowCaster
		Pass
		{
			Name "ShadowCaster"
			Tags{"LightMode" = "ShadowCaster"}

			ZWrite On
			ZTest LEqual
			ColorMask 0
			Cull[_Cull]

			HLSLPROGRAM
			#pragma exclude_renderers gles gles3 glcore
			#pragma target 4.5

			// -------------------------------------
			// Material Keywords
			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
			#pragma multi_compile _ DOTS_INSTANCING_ON

			#pragma vertex ShadowPassVertex
			#pragma fragment ShadowPassFragment

			#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
			ENDHLSL
		}

		// DepthOnly
		Pass
		{
			Name "DepthOnly"
			Tags{"LightMode" = "DepthOnly"}

			ZWrite On
			ColorMask 0
			Cull[_Cull]

			HLSLPROGRAM
			#pragma exclude_renderers gles gles3 glcore
			#pragma target 4.5

			#pragma vertex DepthOnlyVertex
			#pragma fragment DepthOnlyFragment

			// -------------------------------------
			// Material Keywords
			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
			#pragma multi_compile _ DOTS_INSTANCING_ON

			#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
			ENDHLSL
		}

		// DepthNormals
		Pass
		{
			Name "DepthNormals"
			Tags{"LightMode" = "DepthNormals"}

			ZWrite On
			Cull Back

			HLSLPROGRAM
			#pragma exclude_renderers gles gles3 glcore
			#pragma target 4.5

			#pragma vertex DepthNormalsVertex
			#pragma fragment DepthNormalsFragment

			// -------------------------------------
			// Material Keywords
			#pragma shader_feature_local _NORMALMAP
			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
			#pragma multi_compile _ DOTS_INSTANCING_ON

			#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
			ENDHLSL
		}
	}
}