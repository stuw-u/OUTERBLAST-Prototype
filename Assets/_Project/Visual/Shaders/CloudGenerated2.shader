Shader "CloudsGenerated2"
{
    Properties
    {
        Color_2B4C9150("ColorA", Color) = (1, 1, 1, 0)
        Color_FB67AEF0("ColorB", Color) = (0, 0, 0, 0)
        Vector4_FCCF4198("NoiseScales", Vector) = (0, 0, 0, 0)
        Vector4_C772A297("NoiseAmplitudes", Vector) = (0, 0, 0, 0)
        Vector2_9325A8D3("WindVector0", Vector) = (0.1, 0.1, 0, 0)
        Vector2_485E428B("WindVector1", Vector) = (-0.07, -0.02, 0, 0)
        Vector2_5F5A2E76("WindVector2", Vector) = (-0.07, -0.02, 0, 0)
        Vector2_D2BA5EAC("WindVector3", Vector) = (-0.07, -0.02, 0, 0)
        Vector1_2A1815D5("DisplacementAmplitude", Float) = 2
        Vector3_93AC9E4B("DisplacementUpVector", Vector) = (0, 1, 0, 0)
        Vector1_8ECA0007("HorizonRadius", Float) = 1
        Vector1_B301EA24("HorizonAmplitude", Float) = 1
        Vector3_6FCFF75D("HorizonUpVector", Vector) = (0, 1, 0, 0)
        Vector1_47D2C48B("FadeDepth", Float) = 1
        Vector1_FE7208CA("Cutout", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent+0"
        }

        Pass {
           
            // Render State
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            Cull Back
            ZTest LEqual
            ZWrite On
            
        
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
            float4 Vector4_FCCF4198;
            float4 Vector4_C772A297;
            float2 Vector2_9325A8D3;
            float2 Vector2_485E428B;
            float2 Vector2_5F5A2E76;
            float2 Vector2_D2BA5EAC;
            float Vector1_2A1815D5;
            float3 Vector3_93AC9E4B;
            float Vector1_8ECA0007;
            float Vector1_B301EA24;
            float3 Vector3_6FCFF75D;
            float Vector1_47D2C48B;
            float Vector1_FE7208CA;
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
            
            void Unity_Multiply_float(float A, float B, out float Out)
            {
                Out = A * B;
            }
            
            void Unity_Multiply_float(float3 A, float3 B, out float3 Out)
            {
                Out = A * B;
            }
            
            void Unity_InverseLerp_float(float A, float B, float T, out float Out)
            {
                Out = (T - A)/(B - A);
            }
            
            void Unity_Saturate_float(float In, out float Out)
            {
                Out = saturate(In);
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
            
            struct Bindings_GradientParameters_1f9f22adc598b5142ad39d53668f52a3
            {
                float3 TimeParameters;
            };
            
            void SG_GradientParameters_1f9f22adc598b5142ad39d53668f52a3(float2 Vector2_2A50B438, float Vector1_8F829853, float Vector1_2F1A21C7, float2 Vector2_9AF1FA29, Bindings_GradientParameters_1f9f22adc598b5142ad39d53668f52a3 IN, out float NoiseValue_1)
            {
                float _Property_641A2330_Out_0 = Vector1_8F829853;
                float2 _Property_FD6F22A8_Out_0 = Vector2_2A50B438;
                float _Property_B0EC5D2E_Out_0 = Vector1_2F1A21C7;
                float2 _Property_27ED58B5_Out_0 = Vector2_9AF1FA29;
                float2 _Multiply_C4AD8D2D_Out_2;
                Unity_Multiply_float(_Property_27ED58B5_Out_0, (IN.TimeParameters.x.xx), _Multiply_C4AD8D2D_Out_2);
                float2 _TilingAndOffset_601F0AA2_Out_3;
                Unity_TilingAndOffset_float(_Property_FD6F22A8_Out_0, (_Property_B0EC5D2E_Out_0.xx), _Multiply_C4AD8D2D_Out_2, _TilingAndOffset_601F0AA2_Out_3);
                float _GradientNoise_BD305F07_Out_2;
                Unity_GradientNoise_float(_TilingAndOffset_601F0AA2_Out_3, 1, _GradientNoise_BD305F07_Out_2);
                float _Multiply_4A171C35_Out_2;
                Unity_Multiply_float(_Property_641A2330_Out_0, _GradientNoise_BD305F07_Out_2, _Multiply_4A171C35_Out_2);
                float _Saturate_CE31FA12_Out_1;
                Unity_Saturate_float(_Multiply_4A171C35_Out_2, _Saturate_CE31FA12_Out_1);
                NoiseValue_1 = _Saturate_CE31FA12_Out_1;
            }
            
            void Unity_Add_float(float A, float B, out float Out)
            {
                Out = A + B;
            }
            
            void Unity_Subtract_float(float A, float B, out float Out)
            {
                Out = A - B;
            }
            
            void Unity_Maximum_float(float A, float B, out float Out)
            {
                Out = max(A, B);
            }
            
            void Unity_Add_float3(float3 A, float3 B, out float3 Out)
            {
                Out = A + B;
            }
            
            void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
            {
                Out = lerp(A, B, T);
            }
            
            void Unity_SceneDepth_Eye_float(float4 UV, out float Out)
            {
                Out = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
            }
            
            struct Bindings_DepthFade_727c67c9227948540bf7815fcf1af1ff
            {
                float4 ScreenPosition;
            };
            
            void SG_DepthFade_727c67c9227948540bf7815fcf1af1ff(float Vector1_D7826E53, Bindings_DepthFade_727c67c9227948540bf7815fcf1af1ff IN, out float FadeValue_1)
            {
                float _SceneDepth_4DE7DF52_Out_1;
                Unity_SceneDepth_Eye_float(float4(IN.ScreenPosition.xy / IN.ScreenPosition.w, 0, 0), _SceneDepth_4DE7DF52_Out_1);
                float4 _ScreenPosition_43B7084E_Out_0 = IN.ScreenPosition;
                float _Split_CA9BB7F_R_1 = _ScreenPosition_43B7084E_Out_0[0];
                float _Split_CA9BB7F_G_2 = _ScreenPosition_43B7084E_Out_0[1];
                float _Split_CA9BB7F_B_3 = _ScreenPosition_43B7084E_Out_0[2];
                float _Split_CA9BB7F_A_4 = _ScreenPosition_43B7084E_Out_0[3];
                float _Subtract_D4904387_Out_2;
                Unity_Subtract_float(_Split_CA9BB7F_A_4, 1, _Subtract_D4904387_Out_2);
                float _Subtract_28F1C88D_Out_2;
                Unity_Subtract_float(_SceneDepth_4DE7DF52_Out_1, _Subtract_D4904387_Out_2, _Subtract_28F1C88D_Out_2);
                float _Property_72302219_Out_0 = Vector1_D7826E53;
                float _Divide_C6E6C90_Out_2;
                Unity_Divide_float(_Subtract_28F1C88D_Out_2, _Property_72302219_Out_0, _Divide_C6E6C90_Out_2);
                float _Saturate_8822D02C_Out_1;
                Unity_Saturate_float(_Divide_C6E6C90_Out_2, _Saturate_8822D02C_Out_1);
                FadeValue_1 = _Saturate_8822D02C_Out_1;
            }
            
            void Unity_Smoothstep_float(float Edge1, float Edge2, float In, out float Out)
            {
                Out = smoothstep(Edge1, Edge2, In);
            }
            
            void Unity_Minimum_float(float A, float B, out float Out)
            {
                Out = min(A, B);
            };
        
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
                float _Distance_3E7DB305_Out_2;
                Unity_Distance_float3(SHADERGRAPH_OBJECT_POSITION, IN.WorldSpacePosition, _Distance_3E7DB305_Out_2);
                float _Property_E59E1886_Out_0 = Vector1_8ECA0007;
                float _Divide_54445D56_Out_2;
                Unity_Divide_float(_Distance_3E7DB305_Out_2, _Property_E59E1886_Out_0, _Divide_54445D56_Out_2);
                float _Vector1_1018C17_Out_0 = _Divide_54445D56_Out_2;
                float _Multiply_877BF7FE_Out_2;
                Unity_Multiply_float(_Vector1_1018C17_Out_0, 2, _Multiply_877BF7FE_Out_2);
                float _Property_3EBAA763_Out_0 = Vector1_B301EA24;
                float _Multiply_FA325571_Out_2;
                Unity_Multiply_float(_Multiply_877BF7FE_Out_2, _Property_3EBAA763_Out_0, _Multiply_FA325571_Out_2);
                float3 _Property_234B992E_Out_0 = Vector3_6FCFF75D;
                float3 _Multiply_977AF5EB_Out_2;
                Unity_Multiply_float((_Multiply_FA325571_Out_2.xxx), _Property_234B992E_Out_0, _Multiply_977AF5EB_Out_2);
                float3 _Property_45B34F9E_Out_0 = Vector3_93AC9E4B;
                float _Property_F54DAF60_Out_0 = Vector1_2A1815D5;
                float _InverseLerp_96403A7_Out_3;
                Unity_InverseLerp_float(1, 0.5, _Divide_54445D56_Out_2, _InverseLerp_96403A7_Out_3);
                float _Saturate_1AB66C17_Out_1;
                Unity_Saturate_float(_InverseLerp_96403A7_Out_3, _Saturate_1AB66C17_Out_1);
                float _Split_FB7D0148_R_1 = IN.WorldSpacePosition[0];
                float _Split_FB7D0148_G_2 = IN.WorldSpacePosition[1];
                float _Split_FB7D0148_B_3 = IN.WorldSpacePosition[2];
                float _Split_FB7D0148_A_4 = 0;
                float4 _Combine_97479C20_RGBA_4;
                float3 _Combine_97479C20_RGB_5;
                float2 _Combine_97479C20_RG_6;
                Unity_Combine_float(_Split_FB7D0148_R_1, _Split_FB7D0148_B_3, 0, 0, _Combine_97479C20_RGBA_4, _Combine_97479C20_RGB_5, _Combine_97479C20_RG_6);
                float4 _Property_A8ED2E63_Out_0 = Vector4_C772A297;
                float _Split_350843_R_1 = _Property_A8ED2E63_Out_0[0];
                float _Split_350843_G_2 = _Property_A8ED2E63_Out_0[1];
                float _Split_350843_B_3 = _Property_A8ED2E63_Out_0[2];
                float _Split_350843_A_4 = _Property_A8ED2E63_Out_0[3];
                float4 _Property_803B615C_Out_0 = Vector4_FCCF4198;
                float _Split_C3100D97_R_1 = _Property_803B615C_Out_0[0];
                float _Split_C3100D97_G_2 = _Property_803B615C_Out_0[1];
                float _Split_C3100D97_B_3 = _Property_803B615C_Out_0[2];
                float _Split_C3100D97_A_4 = _Property_803B615C_Out_0[3];
                float2 _Property_2C416846_Out_0 = Vector2_9325A8D3;
                Bindings_GradientParameters_1f9f22adc598b5142ad39d53668f52a3 _GradientParameters_9252C9F8;
                _GradientParameters_9252C9F8.TimeParameters = IN.TimeParameters;
                float _GradientParameters_9252C9F8_NoiseValue_1;
                SG_GradientParameters_1f9f22adc598b5142ad39d53668f52a3(_Combine_97479C20_RG_6, _Split_350843_R_1, _Split_C3100D97_R_1, _Property_2C416846_Out_0, _GradientParameters_9252C9F8, _GradientParameters_9252C9F8_NoiseValue_1);
                float2 _Property_C9F8CE45_Out_0 = Vector2_485E428B;
                Bindings_GradientParameters_1f9f22adc598b5142ad39d53668f52a3 _GradientParameters_8FCF5626;
                _GradientParameters_8FCF5626.TimeParameters = IN.TimeParameters;
                float _GradientParameters_8FCF5626_NoiseValue_1;
                SG_GradientParameters_1f9f22adc598b5142ad39d53668f52a3(_Combine_97479C20_RG_6, _Split_350843_G_2, _Split_C3100D97_G_2, _Property_C9F8CE45_Out_0, _GradientParameters_8FCF5626, _GradientParameters_8FCF5626_NoiseValue_1);
                float _Add_AB622F23_Out_2;
                Unity_Add_float(_GradientParameters_9252C9F8_NoiseValue_1, _GradientParameters_8FCF5626_NoiseValue_1, _Add_AB622F23_Out_2);
                float2 _Property_61F7C203_Out_0 = Vector2_5F5A2E76;
                Bindings_GradientParameters_1f9f22adc598b5142ad39d53668f52a3 _GradientParameters_BF716A70;
                _GradientParameters_BF716A70.TimeParameters = IN.TimeParameters;
                float _GradientParameters_BF716A70_NoiseValue_1;
                SG_GradientParameters_1f9f22adc598b5142ad39d53668f52a3(_Combine_97479C20_RG_6, _Split_350843_B_3, _Split_C3100D97_B_3, _Property_61F7C203_Out_0, _GradientParameters_BF716A70, _GradientParameters_BF716A70_NoiseValue_1);
                float2 _Property_F330087E_Out_0 = Vector2_D2BA5EAC;
                Bindings_GradientParameters_1f9f22adc598b5142ad39d53668f52a3 _GradientParameters_658E7EF0;
                _GradientParameters_658E7EF0.TimeParameters = IN.TimeParameters;
                float _GradientParameters_658E7EF0_NoiseValue_1;
                SG_GradientParameters_1f9f22adc598b5142ad39d53668f52a3(_Combine_97479C20_RG_6, _Split_350843_A_4, _Split_C3100D97_A_4, _Property_F330087E_Out_0, _GradientParameters_658E7EF0, _GradientParameters_658E7EF0_NoiseValue_1);
                float _Add_690E9AA6_Out_2;
                Unity_Add_float(_GradientParameters_BF716A70_NoiseValue_1, _GradientParameters_658E7EF0_NoiseValue_1, _Add_690E9AA6_Out_2);
                float _Add_F985F698_Out_2;
                Unity_Add_float(_Add_AB622F23_Out_2, _Add_690E9AA6_Out_2, _Add_F985F698_Out_2);
                float _Multiply_64591833_Out_2;
                Unity_Multiply_float(_Saturate_1AB66C17_Out_1, _Add_F985F698_Out_2, _Multiply_64591833_Out_2);
                float _Property_4313AE26_Out_0 = Vector1_FE7208CA;
                float _Add_2E302B9_Out_2;
                Unity_Add_float(_Property_4313AE26_Out_0, 1, _Add_2E302B9_Out_2);
                float _Multiply_85F30B64_Out_2;
                Unity_Multiply_float(_Multiply_64591833_Out_2, _Add_2E302B9_Out_2, _Multiply_85F30B64_Out_2);
                float _Subtract_F7CDD53F_Out_2;
                Unity_Subtract_float(_Multiply_85F30B64_Out_2, _Property_4313AE26_Out_0, _Subtract_F7CDD53F_Out_2);
                float _Maximum_EA7D7D7A_Out_2;
                Unity_Maximum_float(0, _Subtract_F7CDD53F_Out_2, _Maximum_EA7D7D7A_Out_2);
                float _Vector1_10CA5459_Out_0 = _Maximum_EA7D7D7A_Out_2;
                float _Multiply_7F6A14A6_Out_2;
                Unity_Multiply_float(_Property_F54DAF60_Out_0, _Vector1_10CA5459_Out_0, _Multiply_7F6A14A6_Out_2);
                float3 _Multiply_C1173F9D_Out_2;
                Unity_Multiply_float(_Property_45B34F9E_Out_0, (_Multiply_7F6A14A6_Out_2.xxx), _Multiply_C1173F9D_Out_2);
                float3 _Add_DA7D6057_Out_2;
                Unity_Add_float3(IN.ObjectSpacePosition, _Multiply_C1173F9D_Out_2, _Add_DA7D6057_Out_2);
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
                float _Distance_3E7DB305_Out_2;
                Unity_Distance_float3(SHADERGRAPH_OBJECT_POSITION, IN.WorldSpacePosition, _Distance_3E7DB305_Out_2);
                float _Property_E59E1886_Out_0 = Vector1_8ECA0007;
                float _Divide_54445D56_Out_2;
                Unity_Divide_float(_Distance_3E7DB305_Out_2, _Property_E59E1886_Out_0, _Divide_54445D56_Out_2);
                float _InverseLerp_96403A7_Out_3;
                Unity_InverseLerp_float(1, 0.5, _Divide_54445D56_Out_2, _InverseLerp_96403A7_Out_3);
                float _Saturate_1AB66C17_Out_1;
                Unity_Saturate_float(_InverseLerp_96403A7_Out_3, _Saturate_1AB66C17_Out_1);
                float _Split_FB7D0148_R_1 = IN.WorldSpacePosition[0];
                float _Split_FB7D0148_G_2 = IN.WorldSpacePosition[1];
                float _Split_FB7D0148_B_3 = IN.WorldSpacePosition[2];
                float _Split_FB7D0148_A_4 = 0;
                float4 _Combine_97479C20_RGBA_4;
                float3 _Combine_97479C20_RGB_5;
                float2 _Combine_97479C20_RG_6;
                Unity_Combine_float(_Split_FB7D0148_R_1, _Split_FB7D0148_B_3, 0, 0, _Combine_97479C20_RGBA_4, _Combine_97479C20_RGB_5, _Combine_97479C20_RG_6);
                float4 _Property_A8ED2E63_Out_0 = Vector4_C772A297;
                float _Split_350843_R_1 = _Property_A8ED2E63_Out_0[0];
                float _Split_350843_G_2 = _Property_A8ED2E63_Out_0[1];
                float _Split_350843_B_3 = _Property_A8ED2E63_Out_0[2];
                float _Split_350843_A_4 = _Property_A8ED2E63_Out_0[3];
                float4 _Property_803B615C_Out_0 = Vector4_FCCF4198;
                float _Split_C3100D97_R_1 = _Property_803B615C_Out_0[0];
                float _Split_C3100D97_G_2 = _Property_803B615C_Out_0[1];
                float _Split_C3100D97_B_3 = _Property_803B615C_Out_0[2];
                float _Split_C3100D97_A_4 = _Property_803B615C_Out_0[3];
                float2 _Property_2C416846_Out_0 = Vector2_9325A8D3;
                Bindings_GradientParameters_1f9f22adc598b5142ad39d53668f52a3 _GradientParameters_9252C9F8;
                _GradientParameters_9252C9F8.TimeParameters = IN.TimeParameters;
                float _GradientParameters_9252C9F8_NoiseValue_1;
                SG_GradientParameters_1f9f22adc598b5142ad39d53668f52a3(_Combine_97479C20_RG_6, _Split_350843_R_1, _Split_C3100D97_R_1, _Property_2C416846_Out_0, _GradientParameters_9252C9F8, _GradientParameters_9252C9F8_NoiseValue_1);
                float2 _Property_C9F8CE45_Out_0 = Vector2_485E428B;
                Bindings_GradientParameters_1f9f22adc598b5142ad39d53668f52a3 _GradientParameters_8FCF5626;
                _GradientParameters_8FCF5626.TimeParameters = IN.TimeParameters;
                float _GradientParameters_8FCF5626_NoiseValue_1;
                SG_GradientParameters_1f9f22adc598b5142ad39d53668f52a3(_Combine_97479C20_RG_6, _Split_350843_G_2, _Split_C3100D97_G_2, _Property_C9F8CE45_Out_0, _GradientParameters_8FCF5626, _GradientParameters_8FCF5626_NoiseValue_1);
                float _Add_AB622F23_Out_2;
                Unity_Add_float(_GradientParameters_9252C9F8_NoiseValue_1, _GradientParameters_8FCF5626_NoiseValue_1, _Add_AB622F23_Out_2);
                float2 _Property_61F7C203_Out_0 = Vector2_5F5A2E76;
                Bindings_GradientParameters_1f9f22adc598b5142ad39d53668f52a3 _GradientParameters_BF716A70;
                _GradientParameters_BF716A70.TimeParameters = IN.TimeParameters;
                float _GradientParameters_BF716A70_NoiseValue_1;
                SG_GradientParameters_1f9f22adc598b5142ad39d53668f52a3(_Combine_97479C20_RG_6, _Split_350843_B_3, _Split_C3100D97_B_3, _Property_61F7C203_Out_0, _GradientParameters_BF716A70, _GradientParameters_BF716A70_NoiseValue_1);
                float2 _Property_F330087E_Out_0 = Vector2_D2BA5EAC;
                Bindings_GradientParameters_1f9f22adc598b5142ad39d53668f52a3 _GradientParameters_658E7EF0;
                _GradientParameters_658E7EF0.TimeParameters = IN.TimeParameters;
                float _GradientParameters_658E7EF0_NoiseValue_1;
                SG_GradientParameters_1f9f22adc598b5142ad39d53668f52a3(_Combine_97479C20_RG_6, _Split_350843_A_4, _Split_C3100D97_A_4, _Property_F330087E_Out_0, _GradientParameters_658E7EF0, _GradientParameters_658E7EF0_NoiseValue_1);
                float _Add_690E9AA6_Out_2;
                Unity_Add_float(_GradientParameters_BF716A70_NoiseValue_1, _GradientParameters_658E7EF0_NoiseValue_1, _Add_690E9AA6_Out_2);
                float _Add_F985F698_Out_2;
                Unity_Add_float(_Add_AB622F23_Out_2, _Add_690E9AA6_Out_2, _Add_F985F698_Out_2);
                float _Multiply_64591833_Out_2;
                Unity_Multiply_float(_Saturate_1AB66C17_Out_1, _Add_F985F698_Out_2, _Multiply_64591833_Out_2);
                float _Property_4313AE26_Out_0 = Vector1_FE7208CA;
                float _Add_2E302B9_Out_2;
                Unity_Add_float(_Property_4313AE26_Out_0, 1, _Add_2E302B9_Out_2);
                float _Multiply_85F30B64_Out_2;
                Unity_Multiply_float(_Multiply_64591833_Out_2, _Add_2E302B9_Out_2, _Multiply_85F30B64_Out_2);
                float _Subtract_F7CDD53F_Out_2;
                Unity_Subtract_float(_Multiply_85F30B64_Out_2, _Property_4313AE26_Out_0, _Subtract_F7CDD53F_Out_2);
                float _Maximum_EA7D7D7A_Out_2;
                Unity_Maximum_float(0, _Subtract_F7CDD53F_Out_2, _Maximum_EA7D7D7A_Out_2);
                float _Vector1_294BF220_Out_0 = _Maximum_EA7D7D7A_Out_2;
                float4 _Lerp_614BE378_Out_3;
                Unity_Lerp_float4(_Property_E0E191F0_Out_0, _Property_3E4DBEB3_Out_0, (_Vector1_294BF220_Out_0.xxxx), _Lerp_614BE378_Out_3);
                float _Split_C8824328_R_1 = _Lerp_614BE378_Out_3[0];
                float _Split_C8824328_G_2 = _Lerp_614BE378_Out_3[1];
                float _Split_C8824328_B_3 = _Lerp_614BE378_Out_3[2];
                float _Split_C8824328_A_4 = _Lerp_614BE378_Out_3[3];
                float _Property_9B243036_Out_0 = Vector1_47D2C48B;
                Bindings_DepthFade_727c67c9227948540bf7815fcf1af1ff _DepthFade_8253B7B1;
                _DepthFade_8253B7B1.ScreenPosition = IN.ScreenPosition;
                float _DepthFade_8253B7B1_FadeValue_1;
                SG_DepthFade_727c67c9227948540bf7815fcf1af1ff(_Property_9B243036_Out_0, _DepthFade_8253B7B1, _DepthFade_8253B7B1_FadeValue_1);
                float _Smoothstep_F4387AD1_Out_3;
                Unity_Smoothstep_float(0, 1, _DepthFade_8253B7B1_FadeValue_1, _Smoothstep_F4387AD1_Out_3);
                float _Minimum_B54D6040_Out_2;
                Unity_Minimum_float(_Split_C8824328_A_4, _Smoothstep_F4387AD1_Out_3, _Minimum_B54D6040_Out_2);
                surface.Color = (_Lerp_614BE378_Out_3.xyz);
                surface.Alpha = _Minimum_B54D6040_Out_2;
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
        
    }
    FallBack "Hidden/Shader Graph/FallbackError"
}
