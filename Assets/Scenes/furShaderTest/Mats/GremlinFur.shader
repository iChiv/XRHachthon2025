Shader "XPD/URP/Gremlin Fur"
{
    Properties
    {
        [Space(20)] 
        [Header(Gremlin Fur Alpha)]
		[Space(10)] 
        [NoScaleOffset]_MainTex("MainTex", 2D) = "white" {}
        [Space(20)] 
        [Header(Gremlin Color)]
		[Space(10)] 
        _Color("Color", Color) = (0.1527234,0.6445589,0.8301887)
        _GlowStrength("Glow Strength", Range( 0 , 1)) = 0

		[Space(10)] 
        _SpecularStrength("Specular Strength", Range(0 , 1)) = 1
        [Space(20)] 
        [Header(Alpha Test Cutoff)]
		[Space(10)] 
        _Cutoff("Cutoff", Range(0 , 1)) = 1
        [Space(20)] 
        [Header(Fur Facing Controls)]
		[Space(10)] 
        _Strength("Facing Lerp Strength", Range( 0 , 1)) = 0
        _Scale("Scale", Range( 0 , 10)) = 1
        _Inflate("Inflate", Range( -1 , 1)) = 0
        [Header(Noise Offset. Vary per Gremlin)]
		[Space(10)] 
        _NoiseAmount("Noise Amount", Range(0, 5)) = 0
        _NoiseSpeed("Noise Speed", Range(0, 5)) = 0
        _NoiseOffset("Noise Offset", Range(0, 5000)) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType"="Transparent" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #pragma target 3.0
        ENDHLSL

        Pass
        {
            Name "Thresholded ZWrite pass"
            Tags { "LightMode"="UniversalForward" }

            Blend Off
            AlphaToMask Off
            Cull Back
            ColorMask RGBA
            ZWrite On
            ZTest LEqual
            Offset 1, 0

            Stencil // gremlin shadow
            {
                Ref 2
                ReadMask 2 // Don't overwrite other stencil bits
                Comp always
                Pass replace
            }


            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "SNoise.hlsl"

            struct meshData
            {
                float4 vertex : POSITION;
                float4 vertexColor : COLOR;
                half2 texCoord0 : TEXCOORD0;
                half2 texCoord1 : TEXCOORD1;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID 
            };

            struct interpolators
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 vertexNormal : TEXCOORD1;
                half2 texCoord0 : TEXCOORD2;
                half3 worldViewDirection : TEXCOORD4;
                half4 vertexColor : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                uniform float _Scale;
                uniform float _Inflate;
                uniform float _Strength;
                uniform half _CenterDarkDistance;
                uniform half4 _Color;
                uniform half _SpecularStrength;
                uniform sampler2D _MainTex;
                uniform half _Cutoff;
                uniform half _NoiseAmount;
                uniform half _NoiseSpeed;
                uniform half _NoiseOffset;
                uniform half _GlowStrength;
            CBUFFER_END

            //CBUFFER_START(UnityPerDraw)
	           // float4x4 unity_ObjectToWorld;
	           // float4x4 unity_WorldToObject;
	           // float4 unity_LODFade;
	           // real4 unity_WorldTransformParams;
            //CBUFFER_END

            interpolators vert(meshData v)
            {
                interpolators o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.texCoord0 = v.texCoord0.xy;
					                  

                float3 worldNormal = TransformObjectToWorldNormal(v.normal);
                o.vertexNormal = worldNormal;
                float3 worldPos = TransformObjectToWorld(v.vertex.xyz);
                o.worldPos = worldPos;

                // animated noise for waving the cards back and forth
                float distanceFromOrigin = length(half3(0, 0, 0) - worldPos);
                half animatedNoiseMask = saturate(v.vertexColor.r + (1 - _Color.a)); // _Color alpha of 0 hides the eyes, removes wiggle masking
                float animatedNoise = snoise(float2(((_Time.y * _NoiseSpeed) + _NoiseOffset - (animatedNoiseMask * 0.2)) + (distanceFromOrigin * 5), 1.0));
                animatedNoise *= _NoiseAmount;
                animatedNoise *= animatedNoiseMask; //mask wiggles by vert  ex color around eyes


                float cos1 = cos(animatedNoise);
                float sin1 = sin(animatedNoise);

                // wiggling the UVs with the noise
                float2 animatingUVs = mul(v.texCoord1.xy - float2(0.0, 0.5), float2x2(cos1, -sin1, sin1, cos1)) + float2(0.0, 0.5);
                half2 offsetUVs = (animatingUVs * 2) - 1;

                // camera facing
                float summedWorldPos = (worldPos.x + worldPos.y + worldPos.z);
                half cosWorldPos = cos(summedWorldPos);
                half sinWorldPos = sin(summedWorldPos);

                float2 rotator = mul(offsetUVs, float2x2(cosWorldPos, -sinWorldPos, sinWorldPos, cosWorldPos));
                float3 rotatorAppend = float3(rotator, 0.0);
                float3 cameraFacing = normalize(mul(float4(mul(float4(rotatorAppend, 0.0), GetWorldToViewMatrix()).xyz, 0.0), GetObjectToWorldMatrix()).xyz);
                float3 lerpToCamera = lerp(float3(0, 0, 0), ((cameraFacing * _Scale) + (_Inflate * v.normal)), _Strength);
                float3 lerpVert = v.vertex.xyz + lerpToCamera;
                o.vertex = TransformObjectToHClip(lerpVert);
                o.worldViewDirection = GetWorldSpaceNormalizeViewDir(worldPos);

                half colorPulseControl = frac( (_Time.y * 1.75) + (v.vertex.z * .25));
                colorPulseControl = abs( colorPulseControl - 0.5) * 5;
                colorPulseControl = saturate (colorPulseControl);
                o.vertexColor = saturate(_Color + _Color + 0.2 ) *  saturate(colorPulseControl *  (v.vertexColor.r * 1.5 ));
                o.vertexColor *= _GlowStrength;
                return o;
            }

            half3 fastPow(half3 a, half b) {
                return a / ((1.0 - b) * a + b);
            }

            half3 halfLambert(half3 lightColor, half3 lightDir, half3 normal)
            {
                half NdotL = dot(normal, lightDir) * 0.5 + 0.5;
                return lightColor * NdotL;
            }

            half3 backlight(half3 lightColor, half3 lightDir, half3 viewDir)
            {
                half NdotL = dot(-1 * viewDir, lightDir) * 0.5 + 0.5;
                return lightColor * NdotL;
            }


            half4 frag(interpolators i) : SV_Target
            {

                UNITY_SETUP_INSTANCE_ID(i);
                half4 finalColor;
                float3 WorldPosition = i.worldPos;
                half3 worldNormal = normalize(i.vertexNormal.xyz);

                // diffuse
                float3 lightPos = _MainLightPosition.xyz;
                half3 lighting = halfLambert(_MainLightColor.rgb * unity_LightData.z, lightPos, worldNormal);

                /* // no point in paying for these extra lighting calcs when we're just using a single directional light

                uint lightsCount = GetAdditionalLightsCount();
                for (unsigned int j = 0; j < lightsCount; j++)
                {
                    Light light = GetAdditionalLight(j, i.worldPos);
                    lighting += halfLambert(light.color * (light.distanceAttenuation * light.shadowAttenuation), light.direction, worldNormal);
                }
                */


                // backlighting
                half3 backLighting = backlight(_MainLightColor.rgb * unity_LightData.z, lightPos, i.worldViewDirection);
                backLighting = fastPow(backLighting, 4);
                half3 fresnel = 1 - saturate(dot(i.worldViewDirection, worldNormal));
                fresnel = fastPow(fresnel, 4);
                backLighting *= fresnel;

                // specular
                half3 specular = _SpecularStrength * _MainLightColor.rgb  * lighting *  fastPow( max( 0.0, dot( reflect( -lightPos, worldNormal ),  i.worldViewDirection)), 3);

                half4 baseTexture = (tex2D(_MainTex, i.texCoord0));

                finalColor.rgb = (_Color.rgb + i.vertexColor.rgb) * (lighting + backLighting);

                finalColor.rgb += specular + (i.vertexColor.rgb * 0.5);
                finalColor.a = baseTexture.a;

                clip(finalColor.a - _Cutoff);
                finalColor.a = 1;

                return finalColor;
            }

            ENDHLSL
        }

    }
}
