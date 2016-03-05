Shader "Voxe/TexturedCube"
{
	Properties
	{
		_MainTex ("Base Texture", 2D) = "white" {}
		_BumpMap ("Normal Texture", 2D) = "bump" {}
		_SunAmount ("Sun factor", Range(0,1)) = 1
	}
	
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert addshadow
		
		struct Input
		{
			float2 uv_MainTex: TEXCOORD0;
			float2 uv_BumpMap: TEXCOORD1;
			float4 color: COLOR;
		};

		uniform sampler2D _MainTex;
		uniform sampler2D _BumpMap;
		uniform float _SunAmount;
		
		void surf(Input IN, inout SurfaceOutput o)
		{			
			float3 light = IN.color.rgb;
			float sun = IN.color.a * _SunAmount * 2;
			
			float3 ambient = UNITY_LIGHTMODEL_AMBIENT * sun;			
			ambient = max(ambient, 0.002);
			ambient = max(ambient, light);

			o.Emission = o.Albedo * ambient;
			o.Normal = UnpackNormal (tex2D (_BumpMap, IN.uv_BumpMap));
			o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb;
		}
		
		ENDCG
	}
	
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 80
		
		Pass
		{
			Lighting Off
			
			Material
			{
				Diffuse (1,1,1,1)
				Ambient (1,1,1,1)
			}
			
			BindChannels
			{
				Bind "Vertex", vertex
				Bind "texcoord", texcoord
				Bind "Color", color
			}
			
			SetTexture [_MainTex]
			{
				Combine texture * primary
			} 
		}
	}

	FallBack "Unlit/Texture"
}