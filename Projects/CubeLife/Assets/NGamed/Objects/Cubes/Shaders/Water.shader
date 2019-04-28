Shader "CubeLife/Water" {
    Properties {
		[Header(Water)]
		[NoScaleOffset] Water_AlbedoTexture("Albedo", 2D) = "white" {}
		[NoScaleOffset] [Normal] Water_NormalTexture("Normal", 2D) = "bump" {}
		[NoScaleOffset] Water_RoughnessTexture("Roughness", 2D) = "black" {}
		[NoScaleOffset] Water_HeightTexture("Height", 2D) = "black" {}
		[PowerSlider(2.0)] Water_TextureScale("Scale", Range(0.125, 8.0)) = 1.0

		[Header(Foam)]
		[NoScaleOffset] Foam_AlbedoTexture("Albedo", 2D) = "white" {}
		[NoScaleOffset] [Normal] Foam_NormalTexture("Normal", 2D) = "bump" {}
		[NoScaleOffset] Foam_RoughnessTexture("Roughness", 2D) = "black" {}
		[NoScaleOffset] Foam_HeightTexture("Height", 2D) = "black" {}
		[PowerSlider(2.0)] Foam_TextureScale("Scale", Range(0.125, 8.0)) = 1.0

		[Header(Depth)]
		[PowerSlider(2.0)] FoamDepth("Foam Depth", Range(0.1, 10.0)) = 1.0
		BlendDepth("Blend Depth", Range(0.01, 1.0)) = 0.2
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Opaque" }
		LOD 200

        CGPROGRAM
			#pragma surface surf Standard vertex:vert

			#pragma target 3.0


			struct Input {
				float2 coordinates;

				// Provided by Unity.
				float4 screenPos;
			};


			void vert(inout appdata_full data, out Input input) {
				UNITY_INITIALIZE_OUTPUT(Input, input);
				
				float4 position = mul(unity_ObjectToWorld, data.vertex);

				input.coordinates = position.xz / position.w;

				// We provide normals in object space.
				data.tangent = float4(1.0, 0.0, 0.0, 1.0);
				data.normal = float4(0.0, 0.0, 1.0, 0.0);
			}


			sampler2D Water_AlbedoTexture;
			sampler2D Water_NormalTexture;
			sampler2D Water_RoughnessTexture;
			sampler2D Water_HeightTexture;
			float Water_TextureScale;

			sampler2D Foam_AlbedoTexture;
			sampler2D Foam_NormalTexture;
			sampler2D Foam_RoughnessTexture;
			sampler2D Foam_HeightTexture;
			float Foam_TextureScale;
		
			float FoamDepth;
			float BlendDepth;


			sampler2D _CameraDepthTexture;


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
				float3 albedo;
				float3 normal;
				float roughness;
				float height;
			};

			MaterialSample createSample(float3 albedo, float3 normal, float roughness, float height) {
				MaterialSample materialSample;

				materialSample.albedo = albedo;
				materialSample.normal = normal;
				materialSample.roughness = roughness;
				materialSample.height = height;

				return materialSample;
			}


			MaterialSample sampleMaterial(Material material, float2 coordinates) {
				float3 albedo = tex2D(material.albedos, coordinates).rgb;
				float3 normal = UnpackNormal(tex2D(material.normals, coordinates));
				float roughness = tex2D(material.roughnesses, coordinates).r;
				float height = tex2D(material.heights, coordinates).r;

				return createSample(albedo, normal, roughness, height);
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

				float3 albedo = (materialSample1.albedo * weights.x + materialSample2.albedo * weights.y) / (weights.x + weights.y);
				float3 normal = normalize(materialSample1.normal * weights.x + materialSample2.normal * weights.y);
				float roughness = (materialSample1.roughness * weights.x + materialSample2.roughness * weights.y) / (weights.x + weights.y);
				float sampleHeight = (materialSample1.height * weights.x + materialSample2.height * weights.y) / (weights.x + weights.y);

				return createSample(albedo, normal, roughness, sampleHeight);
			}


			void surf(Input input, inout SurfaceOutputStandard output) {
				float2 coordinates = input.coordinates;
				float4 screenPosition = UNITY_PROJ_COORD(input.screenPos);

				float2 waterCoordinates = coordinates * Water_TextureScale + 0.5;
				float2 foamCoordinates = coordinates * Foam_TextureScale + 0.5;

				Material waterMaterial = createMaterial(Water_AlbedoTexture, Water_NormalTexture, Water_RoughnessTexture, Water_HeightTexture);
				Material foamMaterial = createMaterial(Foam_AlbedoTexture, Foam_NormalTexture, Foam_RoughnessTexture, Foam_HeightTexture);
				
				MaterialSample waterSample = sampleMaterial(waterMaterial, waterCoordinates);
				MaterialSample foamSample = sampleMaterial(foamMaterial, foamCoordinates);

				float3 tangent = float3(1.0, 0.0, 0.0);
				float3 bitangent = float3(0.0, 0.0, 1.0);
				float3 normal = float3(0.0, 1.0, 0.0);

				float3x3 objectSpaceToTangentSpace = float3x3(tangent, bitangent, normal);
				float3x3 tangentSpaceToObjectSpace = transpose(objectSpaceToTangentSpace);

				waterSample.normal = mul(tangentSpaceToObjectSpace, waterSample.normal);
				foamSample.normal = mul(tangentSpaceToObjectSpace, foamSample.normal);

				// The abs() removes (anti-aliasing?) artifacts.
				float depth = LinearEyeDepth(tex2Dproj(_CameraDepthTexture, screenPosition).r);
				float fragmentDepth = LinearEyeDepth(screenPosition.z / screenPosition.w);
				float waterDepth = abs(depth - fragmentDepth);

				float foam = 1.0 - saturate(waterDepth / FoamDepth);

				MaterialSample materialSample = blendSamples(waterSample, foamSample, foam, BlendDepth);

				output.Albedo = materialSample.albedo;
				output.Normal = materialSample.normal;
				output.Smoothness = 1.0 - materialSample.roughness;
				output.Metallic = 0.0;
			}
        ENDCG
    }
    FallBack "Diffuse"
}
