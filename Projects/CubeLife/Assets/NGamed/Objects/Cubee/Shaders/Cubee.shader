Shader "CubeLife/Cubee" {
    Properties {
		[Header(Skin)]
		[NoScaleOffset] AlbedoTexture("Albedo", 2D) = "white" {}
		[NoScaleOffset] [Normal] NormalTexture("Normal", 2D) = "bump" {}
		[NoScaleOffset] RoughnessTexture("Roughness", 2D) = "black" {}
		[NoScaleOffset] HeightTexture("Height", 2D) = "black" {}
		[PowerSlider(2.0)] TextureScale("Scale", Range(1.0, 100.0)) = 10.0

		[Header(Base Transition)]
		Base_MinimumAngle("Minimum Angle", Range(1.0, 89.0)) = 10.0
		Base_MaximumAngle("Maximum Angle", Range(1.0, 89.0)) = 60.0
		Base_Depth("Depth", Range(0.01, 1.0)) = 0.2

		[Header(Side Transition)]
		Side_MinimumAngle("Minimum Angle", Range(1.0, 89.0)) = 10.0
		Side_MaximumAngle("Maximum Angle", Range(1.0, 89.0)) = 60.0
		Side_Depth("Depth", Range(0.01, 1.0)) = 0.2

		[Header(Face)]
		[NoScaleOffset] Face_AlbedoTexture("Albedo", 2D) = "black" {}
		[PowerSlider(2.0)] Face_TextureScale("Scale", Range(0.5, 2.0)) = 1.0
		Face_Roughness("Roughness", Range(0.0, 1.0)) = 1.0

		[Header(Debug)]
		[Toggle] Face("Face", Float) = 0.0
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
			#pragma surface surf Standard vertex:vert

			#pragma target 3.0


			struct Input {
				float3 position;
				float3 surfaceNormal;
				float2 coordinates;
			};


			void vert(inout appdata_full data, out Input input) {
				UNITY_INITIALIZE_OUTPUT(Input, input);

				input.position = data.vertex.xyz;
				input.surfaceNormal = data.normal.xyz;
				input.coordinates = data.texcoord.xy;

				// We provide normals in object space.
				data.tangent = float4(1.0, 0.0, 0.0, 1.0);
				data.normal = float4(0.0, 0.0, 1.0, 0.0);
			}


			sampler2D AlbedoTexture;
			sampler2D NormalTexture;
			sampler2D RoughnessTexture;
			sampler2D HeightTexture;
			float TextureScale;

			float Base_MinimumAngle;
			float Base_MaximumAngle;
			float Base_Depth;

			float Side_MinimumAngle;
			float Side_MaximumAngle;
			float Side_Depth;

			sampler2D Face_AlbedoTexture;
			float Face_TextureScale;
			float Face_Roughness;

			float Face;


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


			MaterialSample sampleMaterial(Material material, float2 initialCoordinates, float2 scale, float3 initialTangent, float3 initialBitangent, float3 initialNormal) {
				float2 coordinates = initialCoordinates * scale + 0.5;

				float3 tangent = normalize(cross(initialNormal, initialBitangent));

				// Although the error should never be visible because it would be covered up by another side, the
				// NaNs are propagated through the blend (since 0 * NaN = NaN), still resulting in a visible error.
				if(any(isnan(tangent))) {
					tangent = initialTangent;
				}

				float3 bitangent = cross(tangent, initialNormal);

				float3x3 objectSpaceToTangentSpace = float3x3(tangent, bitangent, initialNormal);
				float3x3 tangentSpaceToObjectSpace = transpose(objectSpaceToTangentSpace);

				float4 albedo = tex2D(material.albedos, coordinates);
				float3 normal = mul(tangentSpaceToObjectSpace, UnpackNormal(tex2D(material.normals, coordinates)));
				float roughness = tex2D(material.roughnesses, coordinates).r;
				float height = tex2D(material.heights, coordinates).r;

				return createSample(albedo, normal, roughness, height);
			}

			MaterialSample sampleLeftMaterial(Material material, float3 position, float3 normal, float scale) {
				return sampleMaterial(material, position.zy, float2(-scale, scale), float3(0.0, 0.0, -1.0), float3(0.0, 1.0, 0.0), normal);
			}

			MaterialSample sampleRightMaterial(Material material, float3 position, float3 normal, float scale) {
				return sampleMaterial(material, position.zy, float2(scale, scale), float3(0.0, 0.0, 1.0), float3(0.0, 1.0, 0.0), normal);
			}

			MaterialSample sampleFrontMaterial(Material material, float3 position, float3 normal, float scale) {
				return sampleMaterial(material, position.xy, float2(scale, scale), float3(1.0, 0.0, 0.0), float3(0.0, 1.0, 0.0), normal);
			}

			MaterialSample sampleBackMaterial(Material material, float3 position, float3 normal, float scale) {
				return sampleMaterial(material, position.xy, float2(-scale, scale), float3(-1.0, 0.0, 0.0), float3(0.0, 1.0, 0.0), normal);
			}

			MaterialSample sampleBottomMaterial(Material material, float3 position, float3 normal, float scale) {
				return sampleMaterial(material, position.xz, float2(scale, -scale), float3(1.0, 0.0, 0.0), float3(0.0, 0.0, -1.0), normal);
			}

			MaterialSample sampleTopMaterial(Material material, float3 position, float3 normal, float scale) {
				return sampleMaterial(material, position.xz, float2(scale, scale), float3(1.0, 0.0, 0.0), float3(0.0, 0.0, 1.0), normal);
			}


			MaterialSample sampleFaceMaterial(sampler2D albedos, float roughness, float3 position, float3 normal, float2 initialCoordinates, float scale) {
				float2 coordinates = 0.5 + (initialCoordinates - 0.5) * scale;

				float4 albedo = tex2D(albedos, coordinates);

				return createSample(albedo, normal, roughness, 0.0);
			}


			float calculateBlend(float opposite, float minimumAngle, float maximumAngle) {
				float angle = degrees(acos(saturate(opposite)));

				return 1.0 - saturate((angle - minimumAngle) / (maximumAngle - minimumAngle));
			}

			float calculateLeftBlend(float3 normal, float minimumAngle, float maximumAngle) {
				return calculateBlend(-normal.x, minimumAngle, maximumAngle);
			}

			float calculateRightBlend(float3 normal, float minimumAngle, float maximumAngle) {
				return calculateBlend(normal.x, minimumAngle, maximumAngle);
			}

			float calculateFrontBlend(float3 normal, float minimumAngle, float maximumAngle) {
				return calculateBlend(-normal.z, minimumAngle, maximumAngle);
			}

			float calculateBackBlend(float3 normal, float minimumAngle, float maximumAngle) {
				return calculateBlend(normal.z, minimumAngle, maximumAngle);
			}

			float calculateBottomBlend(float3 normal, float minimumAngle, float maximumAngle) {
				return calculateBlend(-normal.y, minimumAngle, maximumAngle);
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

			MaterialSample lerpSamples(MaterialSample materialSample1, MaterialSample materialSample2, float blend) {
				float4 albedo = lerp(materialSample1.albedo, materialSample2.albedo, blend);
				float3 normal = normalize(lerp(materialSample1.normal, materialSample2.normal, blend));
				float roughness = lerp(materialSample1.roughness, materialSample2.roughness, blend);
				float height = lerp(materialSample1.height, materialSample2.height, blend);

				return createSample(albedo, normal, roughness, height);
			}
			
			
			void surf(Input input, inout SurfaceOutputStandard output) {
				float3 position = input.position;
				float3 normal = normalize(input.surfaceNormal);
				float2 coordinates = input.coordinates;
				
				Material material = createMaterial(AlbedoTexture, NormalTexture, RoughnessTexture, HeightTexture);
				
				MaterialSample leftSample = sampleLeftMaterial(material, position, normal, TextureScale);
				MaterialSample rightSample = sampleRightMaterial(material, position, normal, TextureScale);
				MaterialSample frontSample = sampleFrontMaterial(material, position, normal, TextureScale);
				MaterialSample backSample = sampleBackMaterial(material, position, normal, TextureScale);
				MaterialSample bottomSample = sampleBottomMaterial(material, position, normal, TextureScale);
				MaterialSample topSample = sampleTopMaterial(material, position, normal, TextureScale);
				MaterialSample faceSample = sampleFaceMaterial(Face_AlbedoTexture, Face_Roughness, position, normal, coordinates, Face_TextureScale);
				
				float leftBlend = calculateLeftBlend(normal, Side_MinimumAngle, Side_MaximumAngle);
				float rightBlend = calculateRightBlend(normal, Side_MinimumAngle, Side_MaximumAngle);
				float frontBlend = calculateFrontBlend(normal, Side_MinimumAngle, Side_MaximumAngle);
				float backBlend = calculateBackBlend(normal, Side_MinimumAngle, Side_MaximumAngle);
				float bottomBlend = calculateBottomBlend(normal, Base_MinimumAngle, Base_MaximumAngle);
				float topBlend = calculateTopBlend(normal, Base_MinimumAngle, Base_MaximumAngle);
				
				MaterialSample materialSample = blendSamples(leftSample, rightSample, rightBlend, Side_Depth);
				materialSample = blendSamples(materialSample, frontSample, frontBlend, Side_Depth);
				materialSample = blendSamples(materialSample, backSample, backBlend, Side_Depth);
				materialSample = blendSamples(materialSample, bottomSample, bottomBlend, Base_Depth);
				materialSample = blendSamples(materialSample, topSample, topBlend, Base_Depth);
				materialSample = lerpSamples(materialSample, faceSample, faceSample.albedo.a * Face);

				output.Albedo = materialSample.albedo.rgb;
				output.Normal = materialSample.normal;
				output.Smoothness = 1.0 - materialSample.roughness;
				output.Metallic = 0.0;
			}
        ENDCG
    }
    FallBack "Diffuse"
}
