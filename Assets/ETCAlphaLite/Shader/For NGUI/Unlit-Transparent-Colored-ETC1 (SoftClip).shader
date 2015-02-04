Shader "Unlit/Transparent Colored ETC1 (SoftClip)" 
{
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_AlphaTex ("Alpha (A)", 2D) = "white" {}
	}
	
	SubShader
	{
		LOD 100

		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}
		
		Pass
		{
			Cull Off
			Lighting Off
			ZWrite Off
			Fog { Mode Off }
			Offset -1, -1
			ColorMask RGB
			AlphaTest Greater .01
			Blend SrcAlpha OneMinusSrcAlpha
			
			CGPROGRAM
            #pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			
			sampler2D _MainTex;
			sampler2D _AlphaTex;
			float4 _MainTex_ST;
			float2 _ClipSharpness = float2(20.0, 20.0);
			
			struct appdata {
	        float4 vertex : POSITION;
	        float2 texcoord : TEXCOORD0;
	        half4 color : COLOR;
			};
			
			struct v2f
			{
			float4 pos : SV_POSITION;
	        float2 uv : TEXCOORD0;
	        half4 color : COLOR;
			float2 worldPos : TEXCOORD1;
	        };
	        
	        v2f vert(appdata v)
	        {
	        v2f result;
	        
	        result.pos = mul(UNITY_MATRIX_MVP, v.vertex);
	        result.uv = v.texcoord;
	        result.color = v.color;
	        result.worldPos = TRANSFORM_TEX(v.vertex.xy, _MainTex);
	        return result;
	        }
	        
	        half4 frag(v2f i) : COLOR
	        {
			// Softness factor
			float2 factor = (float2(1.0, 1.0) - abs(i.worldPos)) * _ClipSharpness;
			
			half4 col = tex2D(_MainTex, i.uv) * i.color;
			col.a = tex2D(_AlphaTex, i.uv).r * i.color.r;
			col.a *= clamp( min(factor.x, factor.y), 0.0, 1.0);
			return col;
	        }
			ENDCG
		}
	}	
	
	SubShader
	{
		LOD 100

		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}
		
		Pass
		{
			Cull Off
			Lighting Off
			ZWrite Off
			Fog { Mode Off }
			ColorMask RGB
			AlphaTest Greater .01
			Blend SrcAlpha OneMinusSrcAlpha
			ColorMaterial AmbientAndDiffuse
			
			SetTexture [_MainTex]
			{
				Combine Texture * Primary
			}
		}
	}
}
