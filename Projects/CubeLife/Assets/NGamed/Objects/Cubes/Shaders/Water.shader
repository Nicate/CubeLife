Shader "CubeLife/Water" {
    Properties {
		[Header(Top)]
		[NoScaleOffset] Top_AlbedoTexture("Albedo", 2D) = "white" {}
		[NoScaleOffset] [Normal] Top_NormalTexture("Normal", 2D) = "bump" {}
		[NoScaleOffset] Top_RoughnessTexture("Roughness", 2D) = "black" {}
		[NoScaleOffset] Top_HeightTexture("Height", 2D) = "black" {}
		[PowerSlider(2.0)] Top_TextureScale("Scale", Range(1.0, 100.0)) = 10.0

		[Header(Side)]
		[NoScaleOffset] Side_AlbedoTexture("Albedo", 2D) = "white" {}
		[NoScaleOffset] [Normal] Side_NormalTexture("Normal", 2D) = "bump" {}
		[NoScaleOffset] Side_RoughnessTexture("Roughness", 2D) = "black" {}
		[NoScaleOffset] Side_HeightTexture("Height", 2D) = "black" {}
		[PowerSlider(2.0)] Side_TextureScale("Scale", Range(1.0, 100.0)) = 10.0

		[Header(Top Transition)]
		Top_MinimumAngle("Minimum Angle", Range(1.0, 89.0)) = 10.0
		Top_MaximumAngle("Maximum Angle", Range(1.0, 89.0)) = 60.0
		Top_Depth("Depth", Range(0.01, 1.0)) = 0.2

		[Header(Side Transition)]
		Side_MinimumAngle("Minimum Angle", Range(1.0, 89.0)) = 10.0
		Side_MaximumAngle("Maximum Angle", Range(1.0, 89.0)) = 60.0
		Side_Depth("Depth", Range(0.01, 1.0)) = 0.2
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
			#pragma surface surf Standard vertex:vert

			#pragma target 3.0


			struct Input {
				float3 worldPosition;
				float3 surfaceNormal;
			};


			void vert(inout appdata_full data, out Input input) {
				UNITY_INITIALIZE_OUTPUT(Input, input);
				
				float4 position = mul(unity_ObjectToWorld, data.vertex);

				input.worldPosition = position.xyz / position.w;
				input.surfaceNormal = data.normal.xyz;

				// We provide normals in object space.
				data.tangent = float4(1.0, 0.0, 0.0, 1.0);
				data.normal = float4(0.0, 0.0, 1.0, 0.0);
			}


			sampler2D Top_AlbedoTexture;
			sampler2D Top_NormalTexture;
			sampler2D Top_RoughnessTexture;
			sampler2D Top_HeightTexture;
			float Top_TextureScale;

			sampler2D Side_AlbedoTexture;
			sampler2D Side_NormalTexture;
			sampler2D Side_RoughnessTexture;
			sampler2D Side_HeightTexture;
			float Side_TextureScale;

			float Top_MinimumAngle;
			float Top_MaximumAngle;
			float Top_Depth;

			float Side_MinimumAngle;
			float Side_MaximumAngle;
			float Side_Depth;


			struct Material {
				sampler2D albedos;
				sampler2D normals;
				sampler2D roughnesses;
				sampler2D heights;
			};

			Material createMaterial(sampler2D albedos, sampler2D normals, sampler2D roughnesses, sampler2D heights) {
				Material material;

				material.albedos = albedos;
				material.normals = normals;
				material.roughnesses = roughnesses;
				material.heights = heights;

				return material;
			}


			struct MaterialSample {
				float4 albedo;
				float3 normal;
				float roughness;
				float height;
			};

			MaterialSample createSample(float4 albedo, float3 normal, float roughness, float height) {
				MaterialSample materialSample;

				materialSample.albedo = albedo;
				materialSample.normal = normal;
				materialSample.roughness = roughness;
				materialSample.height = height;

				return materialSample;
			}


			MaterialSample sampleMaterial(Material material, float2 coordinates) {
				float4 albedo = tex2D(material.albedos, coordinates);
				float3 normal = UnpackNormal(tex2D(material.normals, coordinates));
				float roughness = tex2D(material.roughnesses, coordinates).r;
				float height = tex2D(material.heights, coordinates).r;

				return createSample(albedo, normal, roughness, height);
			}

			MaterialSample sampleFrontMaterial(Material material, float3 position, float3 normal, float scale) {
				float2 coordinates = position.xy * scale + 0.5;

				MaterialSample materialSample = sampleMaterial(material, coordinates);

				float3 tangent = normalize(cross(normal, float3(0.0, 1.0, 0.0)));

				// The error would never be visible because the top would cover it up but the NaNs are propagated through the blend (since 0 * NaN = NaN).
				if(any(isnan(tangent))) {
					tangent = float3(1.0, 0.0, 0.0);
				}

				float3 bitangent = cross(tangent, normal);

				float3x3 objectSpaceToTangentSpace = float3x3(tangent, bitangent, normal);
				float3x3 tangentSpaceToObjectSpace = transpose(objectSpaceToTangentSpace);

				materialSample.normal = mul(tangentSpaceToObjectSpace, materialSample.normal);

				return materialSample;
			}

			MaterialSample sampleRightMaterial(Material material, float3 position, float3 normal, float scale) {
				float2 coordinates = position.zy * scale + 0.5;

				MaterialSample materialSample = sampleMaterial(material, coordinates);

				float3 tangent = normalize(cross(normal, float3(0.0, 1.0, 0.0)));

				// The error would never be visible because the top would cover it up but the NaNs are propagated through the blend (since 0 * NaN = NaN).
				if(any(isnan(tangent))) {
					tangent = float3(0.0, 0.0, 1.0);
				}

				float3 bitangent = cross(tangent, normal);

				float3x3 objectSpaceToTangentSpace = float3x3(tangent, bitangent, normal);
				float3x3 tangentSpaceToObjectSpace = transpose(objectSpaceToTangentSpace);

				materialSample.normal = mul(tangentSpaceToObjectSpace, materialSample.normal);

				return materialSample;
			}

			MaterialSample sampleBackMaterial(Material material, float3 position, float3 normal, float scale) {
				float2 coordinates = position.xy * float2(-scale, scale) + 0.5;

				MaterialSample materialSample = sampleMaterial(material, coordinates);

				float3 tangent = normalize(cross(normal, float3(0.0, 1.0, 0.0)));

				// The error would never be visible because the top would cover it up but the NaNs are propagated through the blend (since 0 * NaN = NaN).
				if(any(isnan(tangent))) {
					tangent = float3(-1.0, 0.0, 0.0);
				}

				float3 bitangent = cross(tangent, normal);

				float3x3 objectSpaceToTangentSpace = float3x3(tangent, bitangent, normal);
				float3x3 tangentSpaceToObjectSpace = transpose(objectSpaceToTangentSpace);

				materialSample.normal = mul(tangentSpaceToObjectSpace, materialSample.normal);

				return materialSample;
			}

			MaterialSample sampleLeftMaterial(Material material, float3 position, float3 normal, float scale) {
				float2 coordinates = position.zy * float2(-scale, scale) + 0.5;

				MaterialSample materialSample = sampleMaterial(material, coordinates);

				float3 tangent = normalize(cross(normal, float3(0.0, 1.0, 0.0)));

				// The error would never be visible because the top would cover it up but the NaNs are propagated through the blend (since 0 * NaN = NaN).
				if(any(isnan(tangent))) {
					tangent = float3(0.0, 0.0, -1.0);
				}

				float3 bitangent = cross(tangent, normal);

				float3x3 objectSpaceToTangentSpace = float3x3(tangent, bitangent, normal);
				float3x3 tangentSpaceToObjectSpace = transpose(objectSpaceToTangentSpace);

				materialSample.normal = mul(tangentSpaceToObjectSpace, materialSample.normal);

				return materialSample;
			}

			MaterialSample sampleTopMaterial(Material material, float3 position, float3 normal, float scale) {
				float2 coordinates = position.xz * scale + 0.5;

				MaterialSample materialSample = sampleMaterial(material, coordinates);

				float3 tangent = normalize(cross(normal, float3(0.0, 0.0, 1.0)));

				// The error would never be visible because the side would cover it up but the NaNs are propagated through the blend (since 0 * NaN = NaN).
				if(any(isnan(tangent))) {
					tangent = float3(1.0, 0.0, 0.0);
				}

				float3 bitangent = cross(tangent, normal);

				float3x3 objectSpaceToTangentSpace = float3x3(tangent, bitangent, normal);
				float3x3 tangentSpaceToObjectSpace = transpose(objectSpaceToTangentSpace);

				materialSample.normal = mul(tangentSpaceToObjectSpace, materialSample.normal);

				return materialSample;
			}



			float calculateBlend(float opposite, float minimumAngle, float maximumAngle) {
				float angle = degrees(acos(saturate(opposite)));

				return 1.0 - saturate((angle - minimumAngle) / (maximumAngle - minimumAngle));
			}

			float calculateRightBlend(float3 normal, float minimumAngle, float maximumAngle) {
				return calculateBlend(normal.x, minimumAngle, maximumAngle);
			}

			float calculateBackBlend(float3 normal, float minimumAngle, float maximumAngle) {
				return calculateBlend(normal.z, minimumAngle, maximumAngle);
			}

			float calculateLeftBlend(float3 normal, float minimumAngle, float maximumAngle) {
				return calculateBlend(-normal.x, minimumAngle, maximumAngle);
			}

			float calculateTopBlend(float3 normal, float minimumAngle, float maximumAngle) {
				return calculateBlend(normal.y, minimumAngle, maximumAngle);
			}


			/**
			 * Based on http://www.gamasutra.com/blogs/AndreyMishkinis/20130716/196339/Advanced_Terrain_Texture_Splatting.php,
			 * but truncates the depth to the blend value so height blending does not occur outside of the interpolation interval.
			 */
			MaterialSample blendSamples(MaterialSample materialSample1, MaterialSample materialSample2, float blend, float depth) {
				float2 heights = float2(materialSample1.height, materialSample2.height);
				float2 blends = float2(1.0 - blend, blend);

				heights = blends + heights;

				float height = max(heights.x, heights.y);
				float2 depths = min(depth, blends);
				float2 weights = saturate(heights - (height - depths));

				float4 albedo = (materialSample1.albedo * weights.x + materialSample2.albedo * weights.y) / (weights.x + weights.y);
				float3 normal = normalize(materialSample1.normal * weights.x + materialSample2.normal * weights.y);
				float roughness = (materialSample1.roughness * weights.x + materialSample2.roughness * weights.y) / (weights.x + weights.y);
				float sampleHeight = (materialSample1.height * weights.x + materialSample2.height * weights.y) / (weights.x + weights.y);

				return createSample(albedo, normal, roughness, sampleHeight);
			}
			
			
			void surf(Input input, inout SurfaceOutputStandard output) {
				float3 position = input.worldPosition;
				float3 normal = normalize(input.surfaceNormal);
				
				Material sideMaterial = createMaterial(Side_AlbedoTexture, Side_NormalTexture, Side_RoughnessTexture, Side_HeightTexture);
				Material topMaterial = createMaterial(Top_AlbedoTexture, Top_NormalTexture, Top_RoughnessTexture, Top_HeightTexture);
				
				MaterialSample frontSample = sampleFrontMaterial(sideMaterial, position, normal, Side_TextureScale);
				MaterialSample rightSample = sampleRightMaterial(sideMaterial, position, normal, Side_TextureScale);
				MaterialSample backSample = sampleBackMaterial(sideMaterial, position, normal, Side_TextureScale);
				MaterialSample leftSample = sampleLeftMaterial(sideMaterial, position, normal, Side_TextureScale);
				MaterialSample topSample = sampleTopMaterial(topMaterial, position, normal, Top_TextureScale);

				float rightBlend = calculateRightBlend(normal, Side_MinimumAngle, Side_MaximumAngle);
				float backBlend = calculateBackBlend(normal, Side_MinimumAngle, Side_MaximumAngle);
				float leftBlend = calculateLeftBlend(normal, Side_MinimumAngle, Side_MaximumAngle);
				float topBlend = calculateTopBlend(normal, Top_MinimumAngle, Top_MaximumAngle);

				MaterialSample frontRightSample = blendSamples(frontSample, rightSample, rightBlend, Side_Depth);
				MaterialSample frontRightBackSample = blendSamples(frontRightSample, backSample, backBlend, Side_Depth);
				MaterialSample frontRightBackLeftSample = blendSamples(frontRightBackSample, leftSample, leftBlend, Side_Depth);
				MaterialSample materialSample = blendSamples(frontRightBackLeftSample, topSample, topBlend, Top_Depth);
				
				output.Albedo = materialSample.albedo.rgb;
				output.Normal = materialSample.normal;
				output.Smoothness = 1.0 - materialSample.roughness;
				output.Metallic = 0.0;
			}
        ENDCG
    }
    FallBack "Diffuse"
}
