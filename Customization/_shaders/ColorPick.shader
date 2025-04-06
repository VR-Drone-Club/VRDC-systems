 Shader "Custom/ColorPick"
  {
    Properties {
      _MainTex ("Texture", 2D) = "white" {}
      _ColorMask ("Colour Mask", 2D) = "white" {}
      _EmissionMask ("Emission Mask", 2D) = "white" {}
      _NormalMap ("Normal Map", 2D) = "bump" {}
      _Color0 ("Channel 0 Colour", Color) = (1,1,1,1)  
      _Color1 ("Channel 1 Colour", Color) = (1,1,1,1)  
      _Color2 ("Channel 2 Colour", Color) = (1,1,1,1)
      [HDR] _EmissionColor ("Emission Colour", Color) = (1,1,1,1)  
    }
    SubShader {
      Tags { "RenderType" = "Opaque" }
      CGPROGRAM
      #pragma surface surf Lambert
      
      struct Input {
          float2 uv_MainTex;
          float2 uv_NormalMap; 
      };
      
      sampler2D _MainTex;
      sampler2D _ColorMask;
      sampler2D _EmissionMask;
      sampler2D _NormalMap;

      UNITY_INSTANCING_BUFFER_START(Props)
      UNITY_DEFINE_INSTANCED_PROP(float4, _Color0)
      UNITY_DEFINE_INSTANCED_PROP(float4, _Color1)
      UNITY_DEFINE_INSTANCED_PROP(float4, _Color2)
      UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
      UNITY_INSTANCING_BUFFER_END(Props)

      
      void surf (Input IN, inout SurfaceOutput o)
      {
          
          o.Albedo = tex2D(_MainTex, IN.uv_MainTex);
          float4 sampleAmount = tex2D(_ColorMask, IN.uv_MainTex);
          o.Albedo = lerp(o.Albedo, UNITY_ACCESS_INSTANCED_PROP(Props, _Color0) * o.Albedo, sampleAmount.r);
          o.Albedo = lerp(o.Albedo, UNITY_ACCESS_INSTANCED_PROP(Props, _Color1) * o.Albedo, sampleAmount.g);
          o.Albedo = lerp(o.Albedo, UNITY_ACCESS_INSTANCED_PROP(Props, _Color2) * o.Albedo, sampleAmount.b);
          o.Emission = tex2D(_EmissionMask, IN.uv_MainTex) * UNITY_ACCESS_INSTANCED_PROP(Props, _EmissionColor);
          o.Normal = UnpackNormal (tex2D (_NormalMap, IN.uv_NormalMap));
      }
      ENDCG
    }
    Fallback "Diffuse"
  }