/*

That shader is usually used to draw light obstacles.
Have main texture, additive color, multiplicative color and optional fast blurring. 
First color is multipicative. It's grabbed from vertex color.
Second color is additive (RGB) and partially multiplicative (A). It's encoded in TEXCOORD1.

*/


Shader "Light2D/Unilt Transparent Dual Color" {
Properties {
	_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	_BlurDistance ("Blur Distance", Float) = 4 // blur distance in pixels
}

SubShader {
	Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
	LOD 100
	
	Cull Off
	ZWrite Off
	Lighting Off
	Blend SrcAlpha OneMinusSrcAlpha 
	
	Pass {  
		CGPROGRAM
			#pragma multi_compile DUMMY BLUR_ON
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord0 : TEXCOORD0;
				float4 color : COLOR0;
				float2 texcoord1 : TEXCOORD1;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
				float4 color0 : COLOR0;
				float4 color1 : COLOR1;
				#ifdef BLUR_ON
				half2 dist : TEXCOORD1;
				#endif
			};

			sampler2D _MainTex;
			#ifdef BLUR_ON
			half _BlurDistance;
			#endif

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.texcoord = v.texcoord0;
				o.color0 = v.color;
				o.color1 = float4(EncodeFloatRGBA(v.texcoord1.x).xyz, EncodeFloatRGBA(v.texcoord1.y).x);

			#ifdef BLUR_ON
				half dist = _BlurDistance*(1.0/_ScreenParams.xy);
				o.dist = half2(dist, dist*0.707);
			#endif

				return o;
			}
			
			fixed4 frag (v2f i) : COLOR
			{
			#ifdef BLUR_ON
				half2 dists[8] = 
				{ 
					half2(i.dist.x, 0),			half2(-i.dist.x, 0),		half2(0, i.dist.x), 
					half2(0, -i.dist.x),		half2(i.dist.y, i.dist.y),	half2(i.dist.y, -i.dist.y),
					half2(-i.dist.y, i.dist.y), half2(-i.dist.y, -i.dist.y)
				}; 

				half4 sum = 0;
				
				sum += tex2D(_MainTex, i.texcoord);
				sum += tex2D(_MainTex, i.texcoord + dists[0]);
				sum += tex2D(_MainTex, i.texcoord + dists[1]);
				sum += tex2D(_MainTex, i.texcoord + dists[2]);
				sum += tex2D(_MainTex, i.texcoord + dists[3]);
				sum += tex2D(_MainTex, i.texcoord + dists[4]);
				sum += tex2D(_MainTex, i.texcoord + dists[5]);
				sum += tex2D(_MainTex, i.texcoord + dists[6]);
				sum += tex2D(_MainTex, i.texcoord + dists[7]);

				half4 col = sum / 9.0;
			#else
				fixed4 col = tex2D(_MainTex, i.texcoord);
			#endif
				return col*i.color0 + fixed4(i.color1.rgb, i.color1.a*i.color1.a*col.a*10);
			}
		ENDCG
	}
}

}
