Shader "Mask/MaskedForARPlane"
{
	Properties
	{
		[HideInInspector] __dirty( "", Int ) = 1
		_Color("_Color", Color) = (1,1,1,1)
		_MainTex("Albedo", 2D) = "white" {}
//		_Normal("Normal", 2D) = "bump" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+1" }
		Cull Back
		Stencil
		{
			Ref 1
			Comp Equal
			Pass Keep
			Fail Keep
		}
		CGPROGRAM
		#pragma target 3.0
		// #pragma surface surf Standard keepalpha addshadow fullforwardshadows 
		#pragma surface surf StandardCustomLighting keepalpha addshadow fullforwardshadows 
		struct Input
		{
			// float2 uv_texcoord;
			float2 uv_texcoord;
		};

		struct SurfaceOutputCustomLightingCustom
		{
			half3 Albedo;
			half3 Normal;
			half3 Emission;
			half Metallic;
			half Smoothness;
			half Occlusion;
			half Alpha;
			Input SurfInput;
			UnityGIInput GIData;
		};

		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform float4 _Color;

		inline half4 LightingStandardCustomLighting( inout SurfaceOutputCustomLightingCustom s, half3 viewDir, UnityGI gi )
		{
			UnityGIInput data = s.GIData;
			Input i = s.SurfInput;
			half4 c = 0;
			c.rgb = 0;
			c.a = 1;
			return c;
		}

		inline void LightingStandardCustomLighting_GI( inout SurfaceOutputCustomLightingCustom s, UnityGIInput data, inout UnityGI gi )
		{
			s.GIData = data;
		}

		void surf( Input i , inout SurfaceOutputCustomLightingCustom o )
		{
			o.SurfInput = i;
			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			float4 temp_output_3_0 = ( tex2D( _MainTex, uv_MainTex ) * _Color );
			o.Albedo = temp_output_3_0.rgb;
			o.Emission = temp_output_3_0.rgb;
		}
		
		// uniform sampler2D _Normal;
		// uniform float4 _Normal_ST;
		// uniform sampler2D _Albedo;
		// uniform float4 _Albedo_ST;
		// uniform float4 _Color;

		// void surf( Input i , inout SurfaceOutputStandard o )
		// {
		// 	float2 uv_Normal = i.uv_texcoord * _Normal_ST.xy + _Normal_ST.zw;
		// 	o.Normal = UnpackNormal( tex2D( _Normal, uv_Normal ) );
		// 	float2 uv_Albedo = i.uv_texcoord * _Albedo_ST.xy + _Albedo_ST.zw;
		// 	o.Albedo = ( tex2D( _Albedo, uv_Albedo ) * _Color ).xyz;
		// 	o.Alpha = 1;
		// }

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
