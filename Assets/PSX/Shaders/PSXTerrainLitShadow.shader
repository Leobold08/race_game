Shader "Custom/PSXTerrainLitShadow"
{
    Properties
    {
        [HideInInspector] _Control("Control (RGBA)", 2D) = "red" {}

        [HideInInspector] _Splat0("Layer 0 (R)", 2D) = "white" {}
        [HideInInspector] _Splat1("Layer 1 (G)", 2D) = "white" {}
        [HideInInspector] _Splat2("Layer 2 (B)", 2D) = "white" {}
        [HideInInspector] _Splat3("Layer 3 (A)", 2D) = "white" {}

        _Tile0("Layer 0 Tiling", Float) = 16
        _Tile1("Layer 1 Tiling", Float) = 16
        _Tile2("Layer 2 Tiling", Float) = 16
        _Tile3("Layer 3 Tiling", Float) = 16

        _VertexSnap("Vertex Snap", Float) = 120
        _AmbientStrength("Ambient Strength", Range(0, 1)) = 0.35
        _ShadowStrength("Shadow Strength", Range(0, 1)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Geometry-100"
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "TerrainCompatible" = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_Control);         SAMPLER(sampler_Control);
            TEXTURE2D(_Splat0);          SAMPLER(sampler_Splat0);
            TEXTURE2D(_Splat1);          SAMPLER(sampler_Splat1);
            TEXTURE2D(_Splat2);          SAMPLER(sampler_Splat2);
            TEXTURE2D(_Splat3);          SAMPLER(sampler_Splat3);

            CBUFFER_START(UnityPerMaterial)
                float _Tile0;
                float _Tile1;
                float _Tile2;
                float _Tile3;
                float _VertexSnap;
                float _AmbientStrength;
                float _ShadowStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 controlUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 controlUV : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
                float fogFactor : TEXCOORD4;
                half3 vertexLighting : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                float4 snappedCS = positionInputs.positionCS;
                snappedCS.xy = floor(snappedCS.xy * _VertexSnap) / _VertexSnap;

                output.positionCS = snappedCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = input.uv;
                output.controlUV = input.controlUV;
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);

                // Support URP additional-lights-per-vertex mode.
                output.vertexLighting = VertexLighting(positionInputs.positionWS, normalInputs.normalWS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float4 control = SAMPLE_TEXTURE2D(_Control, sampler_Control, input.controlUV);

                float4 col0 = SAMPLE_TEXTURE2D(_Splat0, sampler_Splat0, input.uv * _Tile0);
                float4 col1 = SAMPLE_TEXTURE2D(_Splat1, sampler_Splat1, input.uv * _Tile1);
                float4 col2 = SAMPLE_TEXTURE2D(_Splat2, sampler_Splat2, input.uv * _Tile2);
                float4 col3 = SAMPLE_TEXTURE2D(_Splat3, sampler_Splat3, input.uv * _Tile3);

                half3 albedo =
                    col0.rgb * control.r +
                    col1.rgb * control.g +
                    col2.rgb * control.b +
                    col3.rgb * control.a;

                half3 normalWS = normalize(input.normalWS);
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);

                AmbientOcclusionFactor aoFactor;
                aoFactor.directAmbientOcclusion = 1.0h;
                aoFactor.indirectAmbientOcclusion = 1.0h;

                half4 shadowMask = CalculateShadowMask(inputData);
                Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);

                half nl = saturate(dot(normalWS, mainLight.direction));
                half mainShadow = lerp(1.0h, mainLight.shadowAttenuation, _ShadowStrength);
                half3 diffuse = albedo * mainLight.color * (nl * mainShadow);

                half3 addDiffuse = 0;
                #if defined(_ADDITIONAL_LIGHTS)
                #if USE_CLUSTER_LIGHT_LOOP
                [loop] for (uint lightIndex = 0u; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                    Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
                    half addNl = saturate(dot(normalWS, light.direction));
                    addDiffuse += albedo * light.color * (addNl * light.distanceAttenuation * light.shadowAttenuation);
                }
                #endif

                uint lightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(lightCount)
                    Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
                    half addNl = saturate(dot(normalWS, light.direction));
                    addDiffuse += albedo * light.color * (addNl * light.distanceAttenuation * light.shadowAttenuation);
                LIGHT_LOOP_END
                #endif

                #if defined(_ADDITIONAL_LIGHTS_VERTEX)
                addDiffuse += albedo * input.vertexLighting;
                #endif

                half3 ambient = albedo * SampleSH(normalWS) * _AmbientStrength;
                half3 lit = diffuse + addDiffuse + ambient;

                lit = MixFog(lit, input.fogFactor);
                return half4(lit, 1.0h);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
        UsePass "Universal Render Pipeline/Lit/Meta"
    }

    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
