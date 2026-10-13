Shader "Prototype/Soft Day Sky"
{
    Properties
    {
        _Zenith ("Upper sky", Color) = (.16,.43,.78,1)
        _Horizon ("Horizon haze", Color) = (.68,.84,.92,1)
        _CloudColor ("Clouds", Color) = (.99,.98,.94,1)
        _SunDirection ("Sun direction", Vector) = (.4,.65,.35,0)
        _CloudCoverage ("Cloud coverage", Range(.3,.75)) = .53
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 position : SV_POSITION; float3 direction : TEXCOORD0; };
            float4 _Zenith, _Horizon, _CloudColor, _SunDirection;
            float _CloudCoverage;
            v2f vert(appdata input)
            {
                v2f output;
                output.position=UnityObjectToClipPos(input.vertex);
                output.direction=input.vertex.xyz;
                return output;
            }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float noise(float2 p)
            {
                float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
            }
            float4 frag(v2f input) : SV_Target
            {
                float3 direction=normalize(input.direction);
                float elevation=saturate(direction.y);
                float3 sky=lerp(_Horizon.rgb,_Zenith.rgb,pow(elevation,.6));
                float2 uv=direction.xz/(max(direction.y,.05)+.3)*2.3+float2(_Time.y*.003,0);
                float cloudNoise=noise(uv)*.57+noise(uv*2.03+11.7)*.28+noise(uv*4.1+23.3)*.15;
                float clouds=smoothstep(_CloudCoverage,_CloudCoverage+.18,cloudNoise)*smoothstep(.03,.22,elevation);
                float3 cloudColor=_CloudColor.rgb*lerp(.88,1.03,cloudNoise);
                sky=lerp(sky,cloudColor,clouds);
                float sun=saturate(dot(direction,normalize(_SunDirection.xyz)));
                sky+=float3(1,.85,.57)*(pow(sun,600)*.8+pow(sun,18)*.06)*(1-clouds*.8);
                return float4(sky,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
