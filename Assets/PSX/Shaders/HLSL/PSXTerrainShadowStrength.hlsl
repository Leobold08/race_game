#ifndef PSX_TERRAIN_SHADOW_STRENGTH_INCLUDED
#define PSX_TERRAIN_SHADOW_STRENGTH_INCLUDED

half _ShadowStrength;

Light ApplyPSXShadowStrength(Light light)
{
	half shadowAttenuation = saturate(light.shadowAttenuation);
	half shadowStrength = max(_ShadowStrength, 0.0h);

	if (shadowStrength <= 1.0h)
	{
		light.shadowAttenuation = lerp(1.0h, shadowAttenuation, shadowStrength);
	}
	else
	{
		half darkenExponent = 1.0h + (shadowStrength - 1.0h) * 6.0h;
		light.shadowAttenuation = pow(shadowAttenuation, darkenExponent);
	}

	return light;
}

half4 PSXUniversalFragmentPBR(InputData inputData, SurfaceData surfaceData)
{
	#if defined(_SPECULARHIGHLIGHTS_OFF)
	bool specularHighlightsOff = true;
	#else
	bool specularHighlightsOff = false;
	#endif

	BRDFData brdfData;
	InitializeBRDFData(surfaceData, brdfData);

	#if defined(DEBUG_DISPLAY)
	half4 debugColor;

	if (CanDebugOverrideOutputColor(inputData, surfaceData, brdfData, debugColor))
	{
		return debugColor;
	}
	#endif

	BRDFData brdfDataClearCoat = CreateClearCoatBRDFData(surfaceData, brdfData);
	half4 shadowMask = CalculateShadowMask(inputData);
	AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
	uint meshRenderingLayers = GetMeshRenderingLayer();
	Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
	mainLight = ApplyPSXShadowStrength(mainLight);

	MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

	LightingData lightingData = CreateLightingData(inputData, surfaceData);

	lightingData.giColor = GlobalIllumination(brdfData, brdfDataClearCoat, surfaceData.clearCoatMask,
											  inputData.bakedGI, aoFactor.indirectAmbientOcclusion, inputData.positionWS,
											  inputData.normalWS, inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV);
#ifdef _LIGHT_LAYERS
	if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
#endif
	{
		lightingData.mainLightColor = LightingPhysicallyBased(brdfData, brdfDataClearCoat,
															  mainLight,
															  inputData.normalWS, inputData.viewDirectionWS,
															  surfaceData.clearCoatMask, specularHighlightsOff);
	}

	#if defined(_ADDITIONAL_LIGHTS)
	uint pixelLightCount = GetAdditionalLightsCount();

	#if USE_CLUSTER_LIGHT_LOOP
	[loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
	{
		CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK

		Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
		light = ApplyPSXShadowStrength(light);

#ifdef _LIGHT_LAYERS
		if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
		{
			lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
																		  inputData.normalWS, inputData.viewDirectionWS,
																		  surfaceData.clearCoatMask, specularHighlightsOff);
		}
	}
	#endif

	LIGHT_LOOP_BEGIN(pixelLightCount)
		Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
		light = ApplyPSXShadowStrength(light);

#ifdef _LIGHT_LAYERS
		if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
		{
			lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
																		  inputData.normalWS, inputData.viewDirectionWS,
																		  surfaceData.clearCoatMask, specularHighlightsOff);
		}
	LIGHT_LOOP_END
	#endif

	#if defined(_ADDITIONAL_LIGHTS_VERTEX)
	lightingData.vertexLightingColor += inputData.vertexLighting * brdfData.diffuse;
	#endif

#if REAL_IS_HALF
	return min(CalculateFinalColor(lightingData, surfaceData.alpha), HALF_MAX);
#else
	return CalculateFinalColor(lightingData, surfaceData.alpha);
#endif
}

half4 PSXUniversalFragmentPBR(InputData inputData, half3 albedo, half metallic, half3 specular,
	half smoothness, half occlusion, half3 emission, half alpha)
{
	SurfaceData surfaceData;

	surfaceData.albedo = albedo;
	surfaceData.specular = specular;
	surfaceData.metallic = metallic;
	surfaceData.smoothness = smoothness;
	surfaceData.normalTS = half3(0, 0, 1);
	surfaceData.emission = emission;
	surfaceData.occlusion = occlusion;
	surfaceData.alpha = alpha;
	surfaceData.clearCoatMask = 0;
	surfaceData.clearCoatSmoothness = 1;

	return PSXUniversalFragmentPBR(inputData, surfaceData);
}

#ifdef TERRAIN_GBUFFER
GBufferFragOutput PSXSplatmapFragment(Varyings IN)
#else
void PSXSplatmapFragment(
	Varyings IN
	, out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
	, out uint outRenderingLayers : SV_Target1
#endif
	)
#endif
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
#ifdef _ALPHATEST_ON
	ClipHoles(IN.uvMainAndLM.xy);
#endif

	half3 normalTS = half3(0.0h, 0.0h, 1.0h);
#ifdef TERRAIN_SPLAT_BASEPASS
	half3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvMainAndLM.xy).rgb;
	half smoothness = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uvMainAndLM.xy).a;
	half metallic = SAMPLE_TEXTURE2D(_MetallicTex, sampler_MetallicTex, IN.uvMainAndLM.xy).r;
	half alpha = 1;
	half occlusion = 1;
#else
	half4 hasMask = half4(_LayerHasMask0, _LayerHasMask1, _LayerHasMask2, _LayerHasMask3);
	half4 masks[4];
	ComputeMasks(masks, hasMask, IN);

	float2 splatUV = (IN.uvMainAndLM.xy * (_Control_TexelSize.zw - 1.0f) + 0.5f) * _Control_TexelSize.xy;
	half4 splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, splatUV);

	half alpha = dot(splatControl, 1.0h);
#ifdef _TERRAIN_BLEND_HEIGHT
	if (_NumLayersCount <= 4)
		HeightBasedSplatModify(splatControl, masks);
#endif

	half weight;
	half4 mixedDiffuse;
	half4 defaultSmoothness;
	SplatmapMix(IN.uvMainAndLM, IN.uvSplat01, IN.uvSplat23, splatControl, weight, mixedDiffuse, defaultSmoothness, normalTS);
	half3 albedo = mixedDiffuse.rgb;

	half4 defaultMetallic = half4(_Metallic0, _Metallic1, _Metallic2, _Metallic3);
	half4 defaultOcclusion = half4(_MaskMapRemapScale0.g, _MaskMapRemapScale1.g, _MaskMapRemapScale2.g, _MaskMapRemapScale3.g) +
							half4(_MaskMapRemapOffset0.g, _MaskMapRemapOffset1.g, _MaskMapRemapOffset2.g, _MaskMapRemapOffset3.g);

	half4 maskSmoothness = half4(masks[0].a, masks[1].a, masks[2].a, masks[3].a);
	defaultSmoothness = lerp(defaultSmoothness, maskSmoothness, hasMask);
	half smoothness = dot(splatControl, defaultSmoothness);

	half4 maskMetallic = half4(masks[0].r, masks[1].r, masks[2].r, masks[3].r);
	defaultMetallic = lerp(defaultMetallic, maskMetallic, hasMask);
	half metallic = dot(splatControl, defaultMetallic);

	half4 maskOcclusion = half4(masks[0].g, masks[1].g, masks[2].g, masks[3].g);
	defaultOcclusion = lerp(defaultOcclusion, maskOcclusion, hasMask);
	half occlusion = dot(splatControl, defaultOcclusion);
#endif

	InputData inputData;
	InitializeInputData(IN, normalTS, inputData);
	SetupTerrainDebugTextureData(inputData, IN.uvMainAndLM.xy);

#if defined(_DBUFFER)
	half3 specular = half3(0.0h, 0.0h, 0.0h);
	ApplyDecal(IN.clipPos,
		albedo,
		specular,
		inputData.normalWS,
		metallic,
		occlusion,
		smoothness);
#endif

	InitializeBakedGIData(IN, inputData);

#ifdef TERRAIN_GBUFFER
	BRDFData brdfData;
	InitializeBRDFData(albedo, metallic, half3(0.0h, 0.0h, 0.0h), smoothness, alpha, brdfData);

	half4 color;
	Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, inputData.shadowMask);
	mainLight = ApplyPSXShadowStrength(mainLight);
	MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, inputData.shadowMask);
	color.rgb = GlobalIllumination(brdfData, (BRDFData)0, 0, inputData.bakedGI, occlusion, inputData.positionWS,
								   inputData.normalWS, inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV);
	color.a = alpha;
	SplatmapFinalColor(color, inputData.fogCoord);

	brdfData.albedo.rgb *= alpha;
	brdfData.diffuse.rgb *= alpha;
	brdfData.specular.rgb *= alpha;
	brdfData.reflectivity *= alpha;
	inputData.normalWS = inputData.normalWS * alpha;
	smoothness *= alpha;

	return PackGBuffersBRDFData(brdfData, inputData, smoothness, color.rgb, occlusion);
#else
	half4 color = PSXUniversalFragmentPBR(inputData, albedo, metallic, half3(0.0h, 0.0h, 0.0h), smoothness, occlusion, half3(0, 0, 0), alpha);

	SplatmapFinalColor(color, inputData.fogCoord);

	outColor = half4(color.rgb, 1.0h);

#ifdef _WRITE_RENDERING_LAYERS
	outRenderingLayers = EncodeMeshRenderingLayer();
#endif
#endif
}

#endif
