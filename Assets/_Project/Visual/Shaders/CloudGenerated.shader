Shader "CloudGenerated"
{
    Properties
    {
        Color_2B4C9150("ColorA", Color) = (1, 1, 1, 0)
        Color_FB67AEF0("ColorB", Color) = (0, 0, 0, 0)
        Vector1_6D1798BF("NoiseScale", Float) = 0.1
        Vector1_5999976("NoiseScale2", Float) = 0.05
        Vector1_679704B0("NoiseAmplitude", Float) = 0.5
        Vector1_A6F58D30("NoiseAmplitude2", Float) = 0.5
        Vector2_9325A8D3("WindVector", Vector) = (0.1, 0.1, 0, 0)
        Vector2_485E428B("WindVector2", Vector) = (-0.07, -0.02, 0, 0)
        Vector1_2A1815D5("NoiseMasterAmplitude", Float) = 2
        Vector1_27C3FAE("NoiseMasterScale", Float) = 1
        Vector1_8ECA0007("BowlRadius", Float) = 1
        Vector1_B301EA24("BowlAmplitude", Float) = 1
        Vector1_47D2C48B("FadeDepth", Float) = 1
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent+0"
        }
        
        Pass
        {
            Name "Pass"
            Tags 
            { 
                // LightMode: <None>
            }
           
            // Render State
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            Cull Off
            ZTest LEqual
            //ZWrite Off
            ZWrite On
            // ColorMask: <None>
            
        
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
        
            // Debug
            // <None>
        
            // --------------------------------------------------
            // Pass
        
            // Pragmas
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
        
            // Keywords
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma shader_feature _ _SAMPLE_GI
            // GraphKeywords: <None>
            
            // Defines
            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define VARYINGS_NEED_POSITION_WS 
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_VIEWDIRECTION_WS
            #define FEATURES_GRAPH_VERTEX
            #pragma multi_compile_instancing
            #define SHADERPASS_UNLIT
            #define REQUIRE_DEPTH_TEXTURE
            
        
            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl"
        
            // --------------------------------------------------
            // Graph
        
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
            float4 Color_2B4C9150;
            float4 Color_FB67AEF0;
            float Vector1_6D1798BF;
            float Vector1_5999976;
            float Vector1_679704B0;
            float Vector1_A6F58D30;
            float2 Vector2_9325A8D3;
            float2 Vector2_485E428B;
            float Vector1_2A1815D5;
            float Vector1_27C3FAE;
            float Vector1_8ECA0007;
            float Vector1_B301EA24;
            float Vector1_47D2C48B;
            CBUFFER_END
        
            // Graph Functions
            
            void Unity_Distance_float3(float3 A, float3 B, out float Out)
            {
                Out = distance(A, B);
            }
            
            void Unity_Divide_float(float A, float B, out float Out)
            {
                Out = A / B;
            }
            
            void Unity_Power_float(float A, float B, out float Out)
            {
                Out = pow(A, B);
            }
            
            void Unity_Multiply_float(float A, float B, out float Out)
            {
                Out = A * B;
            }
            
            void Unity_Multiply_float(float3 A, float3 B, out float3 Out)
            {
                Out = A * B;
            }
            
            void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
            {
                RGBA = float4(R, G, B, A);
                RGB = float3(R, G, B);
                RG = float2(R, G);
            }
            
            void Unity_Multiply_float(float2 A, float2 B, out float2 Out)
            {
                Out = A * B;
            }
            
            void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
            {
                Out = UV * Tiling + Offset;
            }
            
            
            float2 Unity_GradientNoise_Dir_float(float2 p)
            {
                // Permutation and hashing used in webgl-nosie goo.gl/pX7HtC
                p = p % 289;
                float x = (34 * p.x + 1) * p.x % 289 + p.y;
                x = (34 * x + 1) * x % 289;
                x = frac(x / 41) * 2 - 1;
                return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
            }
            
            void Unity_GradientNoise_float(float2 UV, float Scale, out float Out)
            { 
                float2 p = UV * Scale;
                float2 ip = floor(p);
                float2 fp = frac(p);
                float d00 = dot(Unity_GradientNoise_Dir_float(ip), fp);
                float d01 = dot(Unity_GradientNoise_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
                float d10 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
                float d11 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
                fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
                Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
            }
            
            void Unity_Add_float(float A, float B, out float Out)
            {
                Out = A + B;
            }
            
            void Unity_Add_float3(float3 A, float3 B, out float3 Out)
            {
                Out = A + B;
            }
            
            void Unity_FresnelEffect_float(float3 Normal, float3 ViewDir, float Power, out float Out)
            {
                Out = pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power);
            }
            
            void Unity_Maximum_float(float A, float B, out float Out)
            {
                Out = max(A, B);
            }
            
            void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
            {
                Out = lerp(A, B, T);
            }
            
            void Unity_SceneDepth_Eye_float(float4 UV, out float Out)
            {
                Out = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
            }
            
            void Unity_Subtract_float(float A, float B, out float Out)
            {
                Out = A - B;
            }
            
            void Unity_Saturate_float(float In, out float Out)
            {
                Out = saturate(In);
            }
        
            // Graph Vertex
            struct VertexDescriptionInputs
            {
                float3 ObjectSpaceNormal;
                float3 ObjectSpaceTangent;
                float3 ObjectSpacePosition;
                float3 WorldSpacePosition;
                float3 TimeParameters;
            };
            
            struct VertexDescription
            {
                float3 VertexPosition;
                float3 VertexNormal;
                float3 VertexTangent;
            };
            
            VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
            {
                VertexDescription description = (VertexDescription)0;
                float3 _Vector3_42BF4410_Out_0 = float3(0, 1, 0);
                float _Property_3EBAA763_Out_0 = Vector1_B301EA24;
                float _Distance_3E7DB305_Out_2;
                Unity_Distance_float3(SHADERGRAPH_OBJECT_POSITION, IN.WorldSpacePosition, _Distance_3E7DB305_Out_2);
                float _Property_E59E1886_Out_0 = Vector1_8ECA0007;
                float _Divide_54445D56_Out_2;
                Unity_Divide_float(_Distance_3E7DB305_Out_2, _Property_E59E1886_Out_0, _Divide_54445D56_Out_2);
                float _Power_F3202449_Out_2;
                Unity_Power_float(_Divide_54445D56_Out_2, 2, _Power_F3202449_Out_2);
                float _Multiply_FA325571_Out_2;
                Unity_Multiply_float(_Property_3EBAA763_Out_0, _Power_F3202449_Out_2, _Multiply_FA325571_Out_2);
                float3 _Multiply_977AF5EB_Out_2;
                Unity_Multiply_float(_Vector3_42BF4410_Out_0, (_Multiply_FA325571_Out_2.xxx), _Multiply_977AF5EB_Out_2);
                float _Property_BFC7C144_Out_0 = Vector1_2A1815D5;
                float3 _Vector3_A0858659_Out_0 = float3(0, 1, 0);
                float _Property_8E30EB24_Out_0 = Vector1_679704B0;
                float _Split_FB7D0148_R_1 = IN.WorldSpacePosition[0];
                float _Split_FB7D0148_G_2 = IN.WorldSpacePosition[1];
                float _Split_FB7D0148_B_3 = IN.WorldSpacePosition[2];
                float _Split_FB7D0148_A_4 = 0;
                float4 _Combine_97479C20_RGBA_4;
                float3 _Combine_97479C20_RGB_5;
                float2 _Combine_97479C20_RG_6;
                Unity_Combine_float(_Split_FB7D0148_R_1, _Split_FB7D0148_B_3, 0, 0, _Combine_97479C20_RGBA_4, _Combine_97479C20_RGB_5, _Combine_97479C20_RG_6);
                float _Property_462E892A_Out_0 = Vector1_6D1798BF;
                float2 _Property_D80901BF_Out_0 = Vector2_9325A8D3;
                float2 _Multiply_2B3864E9_Out_2;
                Unity_Multiply_float(_Property_D80901BF_Out_0, (IN.TimeParameters.x.xx), _Multiply_2B3864E9_Out_2);
                float2 _TilingAndOffset_370EEC4A_Out_3;
                Unity_TilingAndOffset_float(_Combine_97479C20_RG_6, (_Property_462E892A_Out_0.xx), _Multiply_2B3864E9_Out_2, _TilingAndOffset_370EEC4A_Out_3);
                float _Property_2BB73975_Out_0 = Vector1_27C3FAE;
                float _GradientNoise_15DAB3EE_Out_2;
                Unity_GradientNoise_float(_TilingAndOffset_370EEC4A_Out_3, _Property_2BB73975_Out_0, _GradientNoise_15DAB3EE_Out_2);
                float _Multiply_E74EB9CD_Out_2;
                Unity_Multiply_float(_Property_8E30EB24_Out_0, _GradientNoise_15DAB3EE_Out_2, _Multiply_E74EB9CD_Out_2);
                float _Property_A153E5A_Out_0 = Vector1_A6F58D30;
                float _Property_6ABA27ED_Out_0 = Vector1_5999976;
                float2 _Property_A4096496_Out_0 = Vector2_485E428B;
                float2 _Multiply_D1680C1_Out_2;
                Unity_Multiply_float(_Property_A4096496_Out_0, (IN.TimeParameters.x.xx), _Multiply_D1680C1_Out_2);
                float2 _TilingAndOffset_9F90AEDD_Out_3;
                Unity_TilingAndOffset_float(_Combine_97479C20_RG_6, (_Property_6ABA27ED_Out_0.xx), _Multiply_D1680C1_Out_2, _TilingAndOffset_9F90AEDD_Out_3);
                float _GradientNoise_E6A640A9_Out_2;
                Unity_GradientNoise_float(_TilingAndOffset_9F90AEDD_Out_3, _Property_2BB73975_Out_0, _GradientNoise_E6A640A9_Out_2);
                float _Multiply_3D05AC76_Out_2;
                Unity_Multiply_float(_Property_A153E5A_Out_0, _GradientNoise_E6A640A9_Out_2, _Multiply_3D05AC76_Out_2);
                float _Add_CD31FB90_Out_2;
                Unity_Add_float(_Multiply_E74EB9CD_Out_2, _Multiply_3D05AC76_Out_2, _Add_CD31FB90_Out_2);
                float _Power_1C5A9453_Out_2;
                Unity_Power_float(_Add_CD31FB90_Out_2, 2, _Power_1C5A9453_Out_2);
                float _Property_6CDD6A64_Out_0 = Vector1_2A1815D5;
                float _Multiply_FDB61DF3_Out_2;
                Unity_Multiply_float(_Power_1C5A9453_Out_2, _Property_6CDD6A64_Out_0, _Multiply_FDB61DF3_Out_2);
                float3 _Multiply_C1173F9D_Out_2;
                Unity_Multiply_float(_Vector3_A0858659_Out_0, (_Multiply_FDB61DF3_Out_2.xxx), _Multiply_C1173F9D_Out_2);
                float3 _Multiply_A8380507_Out_2;
                Unity_Multiply_float((_Property_BFC7C144_Out_0.xxx), _Multiply_C1173F9D_Out_2, _Multiply_A8380507_Out_2);
                float3 _Add_DA7D6057_Out_2;
                Unity_Add_float3(IN.ObjectSpacePosition, _Multiply_A8380507_Out_2, _Add_DA7D6057_Out_2);
                float3 _Add_249D45D3_Out_2;
                Unity_Add_float3(_Multiply_977AF5EB_Out_2, _Add_DA7D6057_Out_2, _Add_249D45D3_Out_2);
                description.VertexPosition = _Add_249D45D3_Out_2;
                description.VertexNormal = IN.ObjectSpaceNormal;
                description.VertexTangent = IN.ObjectSpaceTangent;
                return description;
            }
            
            // Graph Pixel
            struct SurfaceDescriptionInputs
            {
                float3 WorldSpaceNormal;
                float3 WorldSpaceViewDirection;
                float3 WorldSpacePosition;
                float4 ScreenPosition;
                float3 TimeParameters;
            };
            
            struct SurfaceDescription
            {
                float3 Color;
                float Alpha;
                float AlphaClipThreshold;
            };
            
            SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
            {
                SurfaceDescription surface = (SurfaceDescription)0;
                float4 _Property_E0E191F0_Out_0 = Color_2B4C9150;
                float4 _Property_3E4DBEB3_Out_0 = Color_FB67AEF0;
                float _Property_8E30EB24_Out_0 = Vector1_679704B0;
                float _Split_FB7D0148_R_1 = IN.WorldSpacePosition[0];
                float _Split_FB7D0148_G_2 = IN.WorldSpacePosition[1];
                float _Split_FB7D0148_B_3 = IN.WorldSpacePosition[2];
                float _Split_FB7D0148_A_4 = 0;
                float4 _Combine_97479C20_RGBA_4;
                float3 _Combine_97479C20_RGB_5;
                float2 _Combine_97479C20_RG_6;
                Unity_Combine_float(_Split_FB7D0148_R_1, _Split_FB7D0148_B_3, 0, 0, _Combine_97479C20_RGBA_4, _Combine_97479C20_RGB_5, _Combine_97479C20_RG_6);
                float _Property_462E892A_Out_0 = Vector1_6D1798BF;
                float2 _Property_D80901BF_Out_0 = Vector2_9325A8D3;
                float2 _Multiply_2B3864E9_Out_2;
                Unity_Multiply_float(_Property_D80901BF_Out_0, (IN.TimeParameters.x.xx), _Multiply_2B3864E9_Out_2);
                float2 _TilingAndOffset_370EEC4A_Out_3;
                Unity_TilingAndOffset_float(_Combine_97479C20_RG_6, (_Property_462E892A_Out_0.xx), _Multiply_2B3864E9_Out_2, _TilingAndOffset_370EEC4A_Out_3);
                float _Property_2BB73975_Out_0 = Vector1_27C3FAE;
                float _GradientNoise_15DAB3EE_Out_2;
                Unity_GradientNoise_float(_TilingAndOffset_370EEC4A_Out_3, _Property_2BB73975_Out_0, _GradientNoise_15DAB3EE_Out_2);
                float _Multiply_E74EB9CD_Out_2;
                Unity_Multiply_float(_Property_8E30EB24_Out_0, _GradientNoise_15DAB3EE_Out_2, _Multiply_E74EB9CD_Out_2);
                float _Property_A153E5A_Out_0 = Vector1_A6F58D30;
                float _Property_6ABA27ED_Out_0 = Vector1_5999976;
                float2 _Property_A4096496_Out_0 = Vector2_485E428B;
                float2 _Multiply_D1680C1_Out_2;
                Unity_Multiply_float(_Property_A4096496_Out_0, (IN.TimeParameters.x.xx), _Multiply_D1680C1_Out_2);
                float2 _TilingAndOffset_9F90AEDD_Out_3;
                Unity_TilingAndOffset_float(_Combine_97479C20_RG_6, (_Property_6ABA27ED_Out_0.xx), _Multiply_D1680C1_Out_2, _TilingAndOffset_9F90AEDD_Out_3);
                float _GradientNoise_E6A640A9_Out_2;
                Unity_GradientNoise_float(_TilingAndOffset_9F90AEDD_Out_3, _Property_2BB73975_Out_0, _GradientNoise_E6A640A9_Out_2);
                float _Multiply_3D05AC76_Out_2;
                Unity_Multiply_float(_Property_A153E5A_Out_0, _GradientNoise_E6A640A9_Out_2, _Multiply_3D05AC76_Out_2);
                float _Add_CD31FB90_Out_2;
                Unity_Add_float(_Multiply_E74EB9CD_Out_2, _Multiply_3D05AC76_Out_2, _Add_CD31FB90_Out_2);
                float _Power_1C5A9453_Out_2;
                Unity_Power_float(_Add_CD31FB90_Out_2, 2, _Power_1C5A9453_Out_2);
                float _Add_87AECF85_Out_2;
                Unity_Add_float(_Power_1C5A9453_Out_2, 0.1, _Add_87AECF85_Out_2);
                float _FresnelEffect_F990A67E_Out_3;
                Unity_FresnelEffect_float(IN.WorldSpaceNormal, IN.WorldSpaceViewDirection, 4, _FresnelEffect_F990A67E_Out_3);
                float _Multiply_4FB2FF09_Out_2;
                Unity_Multiply_float(_Power_1C5A9453_Out_2, _FresnelEffect_F990A67E_Out_3, _Multiply_4FB2FF09_Out_2);
                float _Maximum_50D93AD6_Out_2;
                Unity_Maximum_float(_Add_87AECF85_Out_2, _Multiply_4FB2FF09_Out_2, _Maximum_50D93AD6_Out_2);
                float4 _Lerp_614BE378_Out_3;
                Unity_Lerp_float4(_Property_E0E191F0_Out_0, _Property_3E4DBEB3_Out_0, (_Maximum_50D93AD6_Out_2.xxxx), _Lerp_614BE378_Out_3);
                float _SceneDepth_401D6B58_Out_1;
                Unity_SceneDepth_Eye_float(float4(IN.ScreenPosition.xy / IN.ScreenPosition.w, 0, 0), _SceneDepth_401D6B58_Out_1);
                float4 _ScreenPosition_586ABBBF_Out_0 = IN.ScreenPosition;
                float _Split_2386007B_R_1 = _ScreenPosition_586ABBBF_Out_0[0];
                float _Split_2386007B_G_2 = _ScreenPosition_586ABBBF_Out_0[1];
                float _Split_2386007B_B_3 = _ScreenPosition_586ABBBF_Out_0[2];
                float _Split_2386007B_A_4 = _ScreenPosition_586ABBBF_Out_0[3];
                float _Subtract_83D9749_Out_2;
                Unity_Subtract_float(_Split_2386007B_A_4, 1, _Subtract_83D9749_Out_2);
                float _Subtract_CA57DA50_Out_2;
                Unity_Subtract_float(_SceneDepth_401D6B58_Out_1, _Subtract_83D9749_Out_2, _Subtract_CA57DA50_Out_2);
                float _Property_9B243036_Out_0 = Vector1_47D2C48B;
                float _Divide_62069B5D_Out_2;
                Unity_Divide_float(_Subtract_CA57DA50_Out_2, _Property_9B243036_Out_0, _Divide_62069B5D_Out_2);
                float _Saturate_DE3C70E0_Out_1;
                Unity_Saturate_float(_Divide_62069B5D_Out_2, _Saturate_DE3C70E0_Out_1);
                surface.Color = (_Lerp_614BE378_Out_3.xyz);
                surface.Alpha = _Saturate_DE3C70E0_Out_1;
                surface.AlphaClipThreshold = 0;
                return surface;
            }
        
            // --------------------------------------------------
            // Structs and Packing
        
            // Generated Type: Attributes
            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID : INSTANCEID_SEMANTIC;
                #endif
            };
        
            // Generated Type: Varyings
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS;
                float3 normalWS;
                float3 viewDirectionWS;
                #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID : CUSTOM_INSTANCE_ID;
                #endif
                #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                #endif
                #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                #endif
            };
            
            // Generated Type: PackedVaryings
            struct PackedVaryings
            {
                float4 positionCS : SV_POSITION;
                #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID : CUSTOM_INSTANCE_ID;
                #endif
                float3 interp00 : TEXCOORD0;
                float3 interp01 : TEXCOORD1;
                float3 interp02 : TEXCOORD2;
                #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                #endif
                #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                #endif
            };
            
            // Packed Type: Varyings
            PackedVaryings PackVaryings(Varyings input)
            {
                PackedVaryings output = (PackedVaryings)0;
                output.positionCS = input.positionCS;
                output.interp00.xyz = input.positionWS;
                output.interp01.xyz = input.normalWS;
                output.interp02.xyz = input.viewDirectionWS;
                #if UNITY_ANY_INSTANCING_ENABLED
                output.instanceID = input.instanceID;
                #endif
                #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                #endif
                #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                output.cullFace = input.cullFace;
                #endif
                return output;
            }
            
            // Unpacked Type: Varyings
            Varyings UnpackVaryings(PackedVaryings input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = input.positionCS;
                output.positionWS = input.interp00.xyz;
                output.normalWS = input.interp01.xyz;
                output.viewDirectionWS = input.interp02.xyz;
                #if UNITY_ANY_INSTANCING_ENABLED
                output.instanceID = input.instanceID;
                #endif
                #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                #endif
                #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                output.cullFace = input.cullFace;
                #endif
                return output;
            }
        
            // --------------------------------------------------
            // Build Graph Inputs
        
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
            {
                VertexDescriptionInputs output;
                ZERO_INITIALIZE(VertexDescriptionInputs, output);
            
                output.ObjectSpaceNormal =           input.normalOS;
                output.ObjectSpaceTangent =          input.tangentOS;
                output.ObjectSpacePosition =         input.positionOS;
                output.WorldSpacePosition =          TransformObjectToWorld(input.positionOS);
                output.TimeParameters =              _TimeParameters.xyz;
            
                return output;
            }
            
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
            {
                SurfaceDescriptionInputs output;
                ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
            
            	// must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
            	float3 unnormalizedNormalWS = input.normalWS;
                const float renormFactor = 1.0 / length(unnormalizedNormalWS);
            
            
                output.WorldSpaceNormal =            renormFactor*input.normalWS.xyz;		// we want a unit length Normal Vector node in shader graph
            
            
                output.WorldSpaceViewDirection =     input.viewDirectionWS; //TODO: by default normalized in HD, but not in universal
                output.WorldSpacePosition =          input.positionWS;
                output.ScreenPosition =              ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);
                output.TimeParameters =              _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
            #else
            #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
            #endif
            #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
            
                return output;
            }
            
        
            // --------------------------------------------------
            // Main
        
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UnlitPass.hlsl"
        
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags 
            { 
                "LightMode" = "ShadowCaster"
            }
           
            // Render State
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            Cull Off
            ZTest LEqual
            ZWrite On
            // ColorMask: <None>
            
        
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
        
            // Debug
            // <None>
        
            // --------------------------------------------------
            // Pass
        
            // Pragmas
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma multi_compile_instancing
        
            // Keywords
            #pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            // GraphKeywords: <None>
            
            // Defines
            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define VARYINGS_NEED_POSITION_WS 
            #define FEATURES_GRAPH_VERTEX
            #pragma multi_compile_instancing
            #define SHADERPASS_SHADOWCASTER
            #define REQUIRE_DEPTH_TEXTURE
            
        
            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl"
        
            // --------------------------------------------------
            // Graph
        
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
            float4 Color_2B4C9150;
            float4 Color_FB67AEF0;
            float Vector1_6D1798BF;
            float Vector1_5999976;
            float Vector1_679704B0;
            float Vector1_A6F58D30;
            float2 Vector2_9325A8D3;
            float2 Vector2_485E428B;
            float Vector1_2A1815D5;
            float Vector1_27C3FAE;
            float Vector1_8ECA0007;
            float Vector1_B301EA24;
            float Vector1_47D2C48B;
            CBUFFER_END
        
            // Graph Functions
            
            void Unity_Distance_float3(float3 A, float3 B, out float Out)
            {
                Out = distance(A, B);
            }
            
            void Unity_Divide_float(float A, float B, out float Out)
            {
                Out = A / B;
            }
            
            void Unity_Power_float(float A, float B, out float Out)
            {
                Out = pow(A, B);
            }
            
            void Unity_Multiply_float(float A, float B, out float Out)
            {
                Out = A * B;
            }
            
            void Unity_Multiply_float(float3 A, float3 B, out float3 Out)
            {
                Out = A * B;
            }
            
            void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
            {
                RGBA = float4(R, G, B, A);
                RGB = float3(R, G, B);
                RG = float2(R, G);
            }
            
            void Unity_Multiply_float(float2 A, float2 B, out float2 Out)
            {
                Out = A * B;
            }
            
            void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
            {
                Out = UV * Tiling + Offset;
            }
            
            
            float2 Unity_GradientNoise_Dir_float(float2 p)
            {
                // Permutation and hashing used in webgl-nosie goo.gl/pX7HtC
                p = p % 289;
                float x = (34 * p.x + 1) * p.x % 289 + p.y;
                x = (34 * x + 1) * x % 289;
                x = frac(x / 41) * 2 - 1;
                return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
            }
            
            void Unity_GradientNoise_float(float2 UV, float Scale, out float Out)
            { 
                float2 p = UV * Scale;
                float2 ip = floor(p);
                float2 fp = frac(p);
                float d00 = dot(Unity_GradientNoise_Dir_float(ip), fp);
                float d01 = dot(Unity_GradientNoise_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
                float d10 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
                float d11 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
                fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
                Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
            }
            
            void Unity_Add_float(float A, float B, out float Out)
            {
                Out = A + B;
            }
            
            void Unity_Add_float3(float3 A, float3 B, out float3 Out)
            {
                Out = A + B;
            }
            
            void Unity_SceneDepth_Eye_float(float4 UV, out float Out)
            {
                Out = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
            }
            
            void Unity_Subtract_float(float A, float B, out float Out)
            {
                Out = A - B;
            }
            
            void Unity_Saturate_float(float In, out float Out)
            {
                Out = saturate(In);
            }
        
            // Graph Vertex
            struct VertexDescriptionInputs
            {
                float3 ObjectSpaceNormal;
                float3 ObjectSpaceTangent;
                float3 ObjectSpacePosition;
                float3 WorldSpacePosition;
                float3 TimeParameters;
            };
            
            struct VertexDescription
            {
                float3 VertexPosition;
                float3 VertexNormal;
                float3 VertexTangent;
            };
            
            VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
            {
                VertexDescription description = (VertexDescription)0;
                float3 _Vector3_42BF4410_Out_0 = float3(0, 1, 0);
                float _Property_3EBAA763_Out_0 = Vector1_B301EA24;
                float _Distance_3E7DB305_Out_2;
                Unity_Distance_float3(SHADERGRAPH_OBJECT_POSITION, IN.WorldSpacePosition, _Distance_3E7DB305_Out_2);
                float _Property_E59E1886_Out_0 = Vector1_8ECA0007;
                float _Divide_54445D56_Out_2;
                Unity_Divide_float(_Distance_3E7DB305_Out_2, _Property_E59E1886_Out_0, _Divide_54445D56_Out_2);
                float _Power_F3202449_Out_2;
                Unity_Power_float(_Divide_54445D56_Out_2, 2, _Power_F3202449_Out_2);
                float _Multiply_FA325571_Out_2;
                Unity_Multiply_float(_Property_3EBAA763_Out_0, _Power_F3202449_Out_2, _Multiply_FA325571_Out_2);
                float3 _Multiply_977AF5EB_Out_2;
                Unity_Multiply_float(_Vector3_42BF4410_Out_0, (_Multiply_FA325571_Out_2.xxx), _Multiply_977AF5EB_Out_2);
                float _Property_BFC7C144_Out_0 = Vector1_2A1815D5;
                float3 _Vector3_A0858659_Out_0 = float3(0, 1, 0);
                float _Property_8E30EB24_Out_0 = Vector1_679704B0;
                float _Split_FB7D0148_R_1 = IN.WorldSpacePosition[0];
                float _Split_FB7D0148_G_2 = IN.WorldSpacePosition[1];
                float _Split_FB7D0148_B_3 = IN.WorldSpacePosition[2];
                float _Split_FB7D0148_A_4 = 0;
                float4 _Combine_97479C20_RGBA_4;
                float3 _Combine_97479C20_RGB_5;
                float2 _Combine_97479C20_RG_6;
                Unity_Combine_float(_Split_FB7D0148_R_1, _Split_FB7D0148_B_3, 0, 0, _Combine_97479C20_RGBA_4, _Combine_97479C20_RGB_5, _Combine_97479C20_RG_6);
                float _Property_462E892A_Out_0 = Vector1_6D1798BF;
                float2 _Property_D80901BF_Out_0 = Vector2_9325A8D3;
                float2 _Multiply_2B3864E9_Out_2;
                Unity_Multiply_float(_Property_D80901BF_Out_0, (IN.TimeParameters.x.xx), _Multiply_2B3864E9_Out_2);
                float2 _TilingAndOffset_370EEC4A_Out_3;
                Unity_TilingAndOffset_float(_Combine_97479C20_RG_6, (_Property_462E892A_Out_0.xx), _Multiply_2B3864E9_Out_2, _TilingAndOffset_370EEC4A_Out_3);
                float _Property_2BB73975_Out_0 = Vector1_27C3FAE;
                float _GradientNoise_15DAB3EE_Out_2;
                Unity_GradientNoise_float(_TilingAndOffset_370EEC4A_Out_3, _Property_2BB73975_Out_0, _GradientNoise_15DAB3EE_Out_2);
                float _Multiply_E74EB9CD_Out_2;
                Unity_Multiply_float(_Property_8E30EB24_Out_0, _GradientNoise_15DAB3EE_Out_2, _Multiply_E74EB9CD_Out_2);
                float _Property_A153E5A_Out_0 = Vector1_A6F58D30;
                float _Property_6ABA27ED_Out_0 = Vector1_5999976;
                float2 _Property_A4096496_Out_0 = Vector2_485E428B;
                float2 _Multiply_D1680C1_Out_2;
                Unity_Multiply_float(_Property_A4096496_Out_0, (IN.TimeParameters.x.xx), _Multiply_D1680C1_Out_2);
                float2 _TilingAndOffset_9F90AEDD_Out_3;
                Unity_TilingAndOffset_float(_Combine_97479C20_RG_6, (_Property_6ABA27ED_Out_0.xx), _Multiply_D1680C1_Out_2, _TilingAndOffset_9F90AEDD_Out_3);
                float _GradientNoise_E6A640A9_Out_2;
                Unity_GradientNoise_float(_TilingAndOffset_9F90AEDD_Out_3, _Property_2BB73975_Out_0, _GradientNoise_E6A640A9_Out_2);
                float _Multiply_3D05AC76_Out_2;
                Unity_Multiply_float(_Property_A153E5A_Out_0, _GradientNoise_E6A640A9_Out_2, _Multiply_3D05AC76_Out_2);
                float _Add_CD31FB90_Out_2;
                Unity_Add_float(_Multiply_E74EB9CD_Out_2, _Multiply_3D05AC76_Out_2, _Add_CD31FB90_Out_2);
                float _Power_1C5A9453_Out_2;
                Unity_Power_float(_Add_CD31FB90_Out_2, 2, _Power_1C5A9453_Out_2);
                float _Property_6CDD6A64_Out_0 = Vector1_2A1815D5;
                float _Multiply_FDB61DF3_Out_2;
                Unity_Multiply_float(_Power_1C5A9453_Out_2, _Property_6CDD6A64_Out_0, _Multiply_FDB61DF3_Out_2);
                float3 _Multiply_C1173F9D_Out_2;
                Unity_Multiply_float(_Vector3_A0858659_Out_0, (_Multiply_FDB61DF3_Out_2.xxx), _Multiply_C1173F9D_Out_2);
                float3 _Multiply_A8380507_Out_2;
                Unity_Multiply_float((_Property_BFC7C144_Out_0.xxx), _Multiply_C1173F9D_Out_2, _Multiply_A8380507_Out_2);
                float3 _Add_DA7D6057_Out_2;
                Unity_Add_float3(IN.ObjectSpacePosition, _Multiply_A8380507_Out_2, _Add_DA7D6057_Out_2);
                float3 _Add_249D45D3_Out_2;
                Unity_Add_float3(_Multiply_977AF5EB_Out_2, _Add_DA7D6057_Out_2, _Add_249D45D3_Out_2);
                description.VertexPosition = _Add_249D45D3_Out_2;
                description.VertexNormal = IN.ObjectSpaceNormal;
                description.VertexTangent = IN.ObjectSpaceTangent;
                return description;
            }
            
            // Graph Pixel
            struct SurfaceDescriptionInputs
            {
                float3 WorldSpacePosition;
                float4 ScreenPosition;
            };
            
            struct SurfaceDescription
            {
                float Alpha;
                float AlphaClipThreshold;
            };
            
            SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
            {
                SurfaceDescription surface = (SurfaceDescription)0;
                float _SceneDepth_401D6B58_Out_1;
                Unity_SceneDepth_Eye_float(float4(IN.ScreenPosition.xy / IN.ScreenPosition.w, 0, 0), _SceneDepth_401D6B58_Out_1);
                float4 _ScreenPosition_586ABBBF_Out_0 = IN.ScreenPosition;
                float _Split_2386007B_R_1 = _ScreenPosition_586ABBBF_Out_0[0];
                float _Split_2386007B_G_2 = _ScreenPosition_586ABBBF_Out_0[1];
                float _Split_2386007B_B_3 = _ScreenPosition_586ABBBF_Out_0[2];
                float _Split_2386007B_A_4 = _ScreenPosition_586ABBBF_Out_0[3];
                float _Subtract_83D9749_Out_2;
                Unity_Subtract_float(_Split_2386007B_A_4, 1, _Subtract_83D9749_Out_2);
                float _Subtract_CA57DA50_Out_2;
                Unity_Subtract_float(_SceneDepth_401D6B58_Out_1, _Subtract_83D9749_Out_2, _Subtract_CA57DA50_Out_2);
                float _Property_9B243036_Out_0 = Vector1_47D2C48B;
                float _Divide_62069B5D_Out_2;
                Unity_Divide_float(_Subtract_CA57DA50_Out_2, _Property_9B243036_Out_0, _Divide_62069B5D_Out_2);
                float _Saturate_DE3C70E0_Out_1;
                Unity_Saturate_float(_Divide_62069B5D_Out_2, _Saturate_DE3C70E0_Out_1);
                surface.Alpha = _Saturate_DE3C70E0_Out_1;
                surface.AlphaClipThreshold = 0;
                return surface;
            }
        
            // --------------------------------------------------
            // Structs and Packing
        
            // Generated Type: Attributes
            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID : INSTANCEID_SEMANTIC;
                #endif
            };
        
            // Generated Type: Varyings
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS;
                #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID : CUSTOM_INSTANCE_ID;
                #endif
                #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                #endif
                #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                #endif
            };
            
            // Generated Type: PackedVaryings
            struct PackedVaryings
            {
                float4 positionCS : SV_POSITION;
                #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID : CUSTOM_INSTANCE_ID;
                #endif
                float3 interp00 : TEXCOORD0;
                #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                #endif
                #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                #endif
            };
            
            // Packed Type: Varyings
            PackedVaryings PackVaryings(Varyings input)
            {
                PackedVaryings output = (PackedVaryings)0;
                output.positionCS = input.positionCS;
                output.interp00.xyz = input.positionWS;
                #if UNITY_ANY_INSTANCING_ENABLED
                output.instanceID = input.instanceID;
                #endif
                #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                #endif
                #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                output.cullFace = input.cullFace;
                #endif
                return output;
            }
            
            // Unpacked Type: Varyings
            Varyings UnpackVaryings(PackedVaryings input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = input.positionCS;
                output.positionWS = input.interp00.xyz;
                #if UNITY_ANY_INSTANCING_ENABLED
                output.instanceID = input.instanceID;
                #endif
                #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                #endif
                #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                output.cullFace = input.cullFace;
                #endif
                return output;
            }
        
            // --------------------------------------------------
            // Build Graph Inputs
        
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
            {
                VertexDescriptionInputs output;
                ZERO_INITIALIZE(VertexDescriptionInputs, output);
            
                output.ObjectSpaceNormal =           input.normalOS;
                output.ObjectSpaceTangent =          input.tangentOS;
                output.ObjectSpacePosition =         input.positionOS;
                output.WorldSpacePosition =          TransformObjectToWorld(input.positionOS);
                output.TimeParameters =              _TimeParameters.xyz;
            
                return output;
            }
            
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
            {
                SurfaceDescriptionInputs output;
                ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
            
            
            
            
            
                output.WorldSpacePosition =          input.positionWS;
                output.ScreenPosition =              ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
            #else
            #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
            #endif
            #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
            
                return output;
            }
            
        
            // --------------------------------------------------
            // Main
        
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl"
        
            ENDHLSL
        }
        
        Pass
        {
            Name "DepthOnly"
            Tags 
            { 
                "LightMode" = "DepthOnly"
            }
           
            // Render State
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            Cull Off
            ZTest LEqual
            ZWrite Off
            ColorMask 0
            
        
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
        
            // Debug
            // <None>
        
            // --------------------------------------------------
            // Pass
        
            // Pragmas
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma multi_compile_instancing
        
            // Keywords
            // PassKeywords: <None>
            // GraphKeywords: <None>
            
            // Defines
            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define VARYINGS_NEED_POSITION_WS 
            #define FEATURES_GRAPH_VERTEX
            #pragma multi_compile_instancing
            #define SHADERPASS_DEPTHONLY
            #define REQUIRE_DEPTH_TEXTURE
            
        
            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl"
        
            // --------------------------------------------------
            // Graph
        
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
            float4 Color_2B4C9150;
            float4 Color_FB67AEF0;
            float Vector1_6D1798BF;
            float Vector1_5999976;
            float Vector1_679704B0;
            float Vector1_A6F58D30;
            float2 Vector2_9325A8D3;
            float2 Vector2_485E428B;
            float Vector1_2A1815D5;
            float Vector1_27C3FAE;
            float Vector1_8ECA0007;
            float Vector1_B301EA24;
            float Vector1_47D2C48B;
            CBUFFER_END
        
            // Graph Functions
            
            void Unity_Distance_float3(float3 A, float3 B, out float Out)
            {
                Out = distance(A, B);
            }
            
            void Unity_Divide_float(float A, float B, out float Out)
            {
                Out = A / B;
            }
            
            void Unity_Power_float(float A, float B, out float Out)
            {
                Out = pow(A, B);
            }
            
            void Unity_Multiply_float(float A, float B, out float Out)
            {
                Out = A * B;
            }
            
            void Unity_Multiply_float(float3 A, float3 B, out float3 Out)
            {
                Out = A * B;
            }
            
            void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
            {
                RGBA = float4(R, G, B, A);
                RGB = float3(R, G, B);
                RG = float2(R, G);
            }
            
            void Unity_Multiply_float(float2 A, float2 B, out float2 Out)
            {
                Out = A * B;
            }
            
            void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
            {
                Out = UV * Tiling + Offset;
            }
            
            
            float2 Unity_GradientNoise_Dir_float(float2 p)
            {
                // Permutation and hashing used in webgl-nosie goo.gl/pX7HtC
                p = p % 289;
                float x = (34 * p.x + 1) * p.x % 289 + p.y;
                x = (34 * x + 1) * x % 289;
                x = frac(x / 41) * 2 - 1;
                return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
            }
            
            void Unity_GradientNoise_float(float2 UV, float Scale, out float Out)
            { 
                float2 p = UV * Scale;
                float2 ip = floor(p);
                float2 fp = frac(p);
                float d00 = dot(Unity_GradientNoise_Dir_float(ip), fp);
                float d01 = dot(Unity_GradientNoise_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
                float d10 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
                float d11 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
                fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
                Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
            }
            
            void Unity_Add_float(float A, float B, out float Out)
            {
                Out = A + B;
            }
            
            void Unity_Add_float3(float3 A, float3 B, out float3 Out)
            {
                Out = A + B;
            }
            
            void Unity_SceneDepth_Eye_float(float4 UV, out float Out)
            {
                Out = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
            }
            
            void Unity_Subtract_float(float A, float B, out float Out)
            {
                Out = A - B;
            }
            
            void Unity_Saturate_float(float In, out float Out)
            {
                Out = saturate(In);
            }
        
            // Graph Vertex
            struct VertexDescriptionInputs
            {
                float3 ObjectSpaceNormal;
                float3 ObjectSpaceTangent;
                float3 ObjectSpacePosition;
                float3 WorldSpacePosition;
                float3 TimeParameters;
            };
            
            struct VertexDescription
            {
                float3 VertexPosition;
                float3 VertexNormal;
                float3 VertexTangent;
            };
            
            VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
            {
                VertexDescription description = (VertexDescription)0;
                float3 _Vector3_42BF4410_Out_0 = float3(0, 1, 0);
                float _Property_3EBAA763_Out_0 = Vector1_B301EA24;
                float _Distance_3E7DB305_Out_2;
                Unity_Distance_float3(SHADERGRAPH_OBJECT_POSITION, IN.WorldSpacePosition, _Distance_3E7DB305_Out_2);
                float _Property_E59E1886_Out_0 = Vector1_8ECA0007;
                float _Divide_54445D56_Out_2;
                Unity_Divide_float(_Distance_3E7DB305_Out_2, _Property_E59E1886_Out_0, _Divide_54445D56_Out_2);
                float _Power_F3202449_Out_2;
                Unity_Power_float(_Divide_54445D56_Out_2, 2, _Power_F3202449_Out_2);
                float _Multiply_FA325571_Out_2;
                Unity_Multiply_float(_Property_3EBAA763_Out_0, _Power_F3202449_Out_2, _Multiply_FA325571_Out_2);
                float3 _Multiply_977AF5EB_Out_2;
                Unity_Multiply_float(_Vector3_42BF4410_Out_0, (_Multiply_FA325571_Out_2.xxx), _Multiply_977AF5EB_Out_2);
                float _Property_BFC7C144_Out_0 = Vector1_2A1815D5;
                float3 _Vector3_A0858659_Out_0 = float3(0, 1, 0);
                float _Property_8E30EB24_Out_0 = Vector1_679704B0;
                float _Split_FB7D0148_R_1 = IN.WorldSpacePosition[0];
                float _Split_FB7D0148_G_2 = IN.WorldSpacePosition[1];
                float _Split_FB7D0148_B_3 = IN.WorldSpacePosition[2];
                float _Split_FB7D0148_A_4 = 0;
                float4 _Combine_97479C20_RGBA_4;
                float3 _Combine_97479C20_RGB_5;
                float2 _Combine_97479C20_RG_6;
                Unity_Combine_float(_Split_FB7D0148_R_1, _Split_FB7D0148_B_3, 0, 0, _Combine_97479C20_RGBA_4, _Combine_97479C20_RGB_5, _Combine_97479C20_RG_6);
                float _Property_462E892A_Out_0 = Vector1_6D1798BF;
                float2 _Property_D80901BF_Out_0 = Vector2_9325A8D3;
                float2 _Multiply_2B3864E9_Out_2;
                Unity_Multiply_float(_Property_D80901BF_Out_0, (IN.TimeParameters.x.xx), _Multiply_2B3864E9_Out_2);
                float2 _TilingAndOffset_370EEC4A_Out_3;
                Unity_TilingAndOffset_float(_Combine_97479C20_RG_6, (_Property_462E892A_Out_0.xx), _Multiply_2B3864E9_Out_2, _TilingAndOffset_370EEC4A_Out_3);
                float _Property_2BB73975_Out_0 = Vector1_27C3FAE;
                float _GradientNoise_15DAB3EE_Out_2;
                Unity_GradientNoise_float(_TilingAndOffset_370EEC4A_Out_3, _Property_2BB73975_Out_0, _GradientNoise_15DAB3EE_Out_2);
                float _Multiply_E74EB9CD_Out_2;
                Unity_Multiply_float(_Property_8E30EB24_Out_0, _GradientNoise_15DAB3EE_Out_2, _Multiply_E74EB9CD_Out_2);
                float _Property_A153E5A_Out_0 = Vector1_A6F58D30;
                float _Property_6ABA27ED_Out_0 = Vector1_5999976;
                float2 _Property_A4096496_Out_0 = Vector2_485E428B;
                float2 _Multiply_D1680C1_Out_2;
                Unity_Multiply_float(_Property_A4096496_Out_0, (IN.TimeParameters.x.xx), _Multiply_D1680C1_Out_2);
                float2 _TilingAndOffset_9F90AEDD_Out_3;
                Unity_TilingAndOffset_float(_Combine_97479C20_RG_6, (_Property_6ABA27ED_Out_0.xx), _Multiply_D1680C1_Out_2, _TilingAndOffset_9F90AEDD_Out_3);
                float _GradientNoise_E6A640A9_Out_2;
                Unity_GradientNoise_float(_TilingAndOffset_9F90AEDD_Out_3, _Property_2BB73975_Out_0, _GradientNoise_E6A640A9_Out_2);
                float _Multiply_3D05AC76_Out_2;
                Unity_Multiply_float(_Property_A153E5A_Out_0, _GradientNoise_E6A640A9_Out_2, _Multiply_3D05AC76_Out_2);
                float _Add_CD31FB90_Out_2;
                Unity_Add_float(_Multiply_E74EB9CD_Out_2, _Multiply_3D05AC76_Out_2, _Add_CD31FB90_Out_2);
                float _Power_1C5A9453_Out_2;
                Unity_Power_float(_Add_CD31FB90_Out_2, 2, _Power_1C5A9453_Out_2);
                float _Property_6CDD6A64_Out_0 = Vector1_2A1815D5;
                float _Multiply_FDB61DF3_Out_2;
                Unity_Multiply_float(_Power_1C5A9453_Out_2, _Property_6CDD6A64_Out_0, _Multiply_FDB61DF3_Out_2);
                float3 _Multiply_C1173F9D_Out_2;
                Unity_Multiply_float(_Vector3_A0858659_Out_0, (_Multiply_FDB61DF3_Out_2.xxx), _Multiply_C1173F9D_Out_2);
                float3 _Multiply_A8380507_Out_2;
                Unity_Multiply_float((_Property_BFC7C144_Out_0.xxx), _Multiply_C1173F9D_Out_2, _Multiply_A8380507_Out_2);
                float3 _Add_DA7D6057_Out_2;
                Unity_Add_float3(IN.ObjectSpacePosition, _Multiply_A8380507_Out_2, _Add_DA7D6057_Out_2);
                float3 _Add_249D45D3_Out_2;
                Unity_Add_float3(_Multiply_977AF5EB_Out_2, _Add_DA7D6057_Out_2, _Add_249D45D3_Out_2);
                description.VertexPosition = _Add_249D45D3_Out_2;
                description.VertexNormal = IN.ObjectSpaceNormal;
                description.VertexTangent = IN.ObjectSpaceTangent;
                return description;
            }
            
            // Graph Pixel
            struct SurfaceDescriptionInputs
            {
                float3 WorldSpacePosition;
                float4 ScreenPosition;
            };
            
            struct SurfaceDescription
            {
                float Alpha;
                float AlphaClipThreshold;
            };
            
            SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
            {
                SurfaceDescription surface = (SurfaceDescription)0;
                float _SceneDepth_401D6B58_Out_1;
                Unity_SceneDepth_Eye_float(float4(IN.ScreenPosition.xy / IN.ScreenPosition.w, 0, 0), _SceneDepth_401D6B58_Out_1);
                float4 _ScreenPosition_586ABBBF_Out_0 = IN.ScreenPosition;
                float _Split_2386007B_R_1 = _ScreenPosition_586ABBBF_Out_0[0];
                float _Split_2386007B_G_2 = _ScreenPosition_586ABBBF_Out_0[1];
                float _Split_2386007B_B_3 = _ScreenPosition_586ABBBF_Out_0[2];
                float _Split_2386007B_A_4 = _ScreenPosition_586ABBBF_Out_0[3];
                float _Subtract_83D9749_Out_2;
                Unity_Subtract_float(_Split_2386007B_A_4, 1, _Subtract_83D9749_Out_2);
                float _Subtract_CA57DA50_Out_2;
                Unity_Subtract_float(_SceneDepth_401D6B58_Out_1, _Subtract_83D9749_Out_2, _Subtract_CA57DA50_Out_2);
                float _Property_9B243036_Out_0 = Vector1_47D2C48B;
                float _Divide_62069B5D_Out_2;
                Unity_Divide_float(_Subtract_CA57DA50_Out_2, _Property_9B243036_Out_0, _Divide_62069B5D_Out_2);
                float _Saturate_DE3C70E0_Out_1;
                Unity_Saturate_float(_Divide_62069B5D_Out_2, _Saturate_DE3C70E0_Out_1);
                surface.Alpha = _Saturate_DE3C70E0_Out_1;
                surface.AlphaClipThreshold = 0;
                return surface;
            }
        
            // --------------------------------------------------
            // Structs and Packing
        
            // Generated Type: Attributes
            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID : INSTANCEID_SEMANTIC;
                #endif
            };
        
            // Generated Type: Varyings
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS;
                #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID : CUSTOM_INSTANCE_ID;
                #endif
                #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                #endif
                #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                #endif
            };
            
            // Generated Type: PackedVaryings
            struct PackedVaryings
            {
                float4 positionCS : SV_POSITION;
                #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID : CUSTOM_INSTANCE_ID;
                #endif
                float3 interp00 : TEXCOORD0;
                #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                #endif
                #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                #endif
            };
            
            // Packed Type: Varyings
            PackedVaryings PackVaryings(Varyings input)
            {
                PackedVaryings output = (PackedVaryings)0;
                output.positionCS = input.positionCS;
                output.interp00.xyz = input.positionWS;
                #if UNITY_ANY_INSTANCING_ENABLED
                output.instanceID = input.instanceID;
                #endif
                #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                #endif
                #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                output.cullFace = input.cullFace;
                #endif
                return output;
            }
            
            // Unpacked Type: Varyings
            Varyings UnpackVaryings(PackedVaryings input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = input.positionCS;
                output.positionWS = input.interp00.xyz;
                #if UNITY_ANY_INSTANCING_ENABLED
                output.instanceID = input.instanceID;
                #endif
                #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                #endif
                #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                output.cullFace = input.cullFace;
                #endif
                return output;
            }
        
            // --------------------------------------------------
            // Build Graph Inputs
        
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
            {
                VertexDescriptionInputs output;
                ZERO_INITIALIZE(VertexDescriptionInputs, output);
            
                output.ObjectSpaceNormal =           input.normalOS;
                output.ObjectSpaceTangent =          input.tangentOS;
                output.ObjectSpacePosition =         input.positionOS;
                output.WorldSpacePosition =          TransformObjectToWorld(input.positionOS);
                output.TimeParameters =              _TimeParameters.xyz;
            
                return output;
            }
            
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
            {
                SurfaceDescriptionInputs output;
                ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
            
            
            
            
            
                output.WorldSpacePosition =          input.positionWS;
                output.ScreenPosition =              ComputeScreenPos(TransformWorldToHClip(input.positionWS), _ProjectionParams.x);
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
            #else
            #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
            #endif
            #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
            
                return output;
            }
            
        
            // --------------------------------------------------
            // Main
        
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"
        
            ENDHLSL
        }
        
    }
    FallBack "Hidden/Shader Graph/FallbackError"
}
