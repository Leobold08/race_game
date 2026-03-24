Shader "Custom/Skybox/6 Sided Free Rotation"
{
    Properties
    {
        _Tint("Tint Color", Color) = (0.5, 0.5, 0.5, 0.5)
        [Gamma] _Exposure("Exposure", Range(0, 8)) = 1

        _Yaw("Yaw (Y)", Range(-180, 180)) = 0
        _Pitch("Pitch (X)", Range(-90, 90)) = 0
        _Roll("Roll (Z)", Range(-180, 180)) = 0

        [NoScaleOffset] _FrontTex("Front [+Z] (HDR)", 2D) = "grey" {}
        [NoScaleOffset] _BackTex("Back [-Z] (HDR)", 2D) = "grey" {}
        [NoScaleOffset] _LeftTex("Left [+X] (HDR)", 2D) = "grey" {}
        [NoScaleOffset] _RightTex("Right [-X] (HDR)", 2D) = "grey" {}
        [NoScaleOffset] _UpTex("Up [+Y] (HDR)", 2D) = "grey" {}
        [NoScaleOffset] _DownTex("Down [-Y] (HDR)", 2D) = "grey" {}
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        CGINCLUDE
        #include "UnityCG.cginc"

        fixed4 _Tint;
        half _Exposure;
        float _Yaw;
        float _Pitch;
        float _Roll;

        sampler2D _FrontTex;
        sampler2D _BackTex;
        sampler2D _LeftTex;
        sampler2D _RightTex;
        sampler2D _UpTex;
        sampler2D _DownTex;

        half4 _FrontTex_HDR;
        half4 _BackTex_HDR;
        half4 _LeftTex_HDR;
        half4 _RightTex_HDR;
        half4 _UpTex_HDR;
        half4 _DownTex_HDR;

        struct appdata
        {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f
        {
            float4 vertex : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        float3x3 RotationYawPitchRoll(float yawDeg, float pitchDeg, float rollDeg)
        {
            float yaw = radians(yawDeg);
            float pitch = radians(pitchDeg);
            float roll = radians(rollDeg);

            float sy, cy;
            float sx, cx;
            float sz, cz;
            sincos(yaw, sy, cy);
            sincos(pitch, sx, cx);
            sincos(roll, sz, cz);

            float3x3 rotY = float3x3(
                cy, 0, sy,
                0, 1, 0,
                -sy, 0, cy
            );

            float3x3 rotX = float3x3(
                1, 0, 0,
                0, cx, -sx,
                0, sx, cx
            );

            float3x3 rotZ = float3x3(
                cz, -sz, 0,
                sz, cz, 0,
                0, 0, 1
            );

            return mul(rotY, mul(rotX, rotZ));
        }

        v2f vert(appdata v)
        {
            v2f o;
            float3 rotated = mul(RotationYawPitchRoll(_Yaw, _Pitch, _Roll), v.vertex.xyz);
            o.vertex = UnityObjectToClipPos(float4(rotated, 1.0));
            o.uv = v.uv;
            return o;
        }

        half4 OutputSkyColor(half4 encodedColor, half4 decodeData)
        {
            half3 sky = DecodeHDR(encodedColor, decodeData);
            sky *= _Tint.rgb * _Exposure;
            return half4(sky, 1.0h);
        }

        half4 fragFront(v2f i) : SV_Target { return OutputSkyColor(tex2D(_FrontTex, i.uv), _FrontTex_HDR); }
        half4 fragBack(v2f i) : SV_Target { return OutputSkyColor(tex2D(_BackTex, i.uv), _BackTex_HDR); }
        half4 fragLeft(v2f i) : SV_Target { return OutputSkyColor(tex2D(_LeftTex, i.uv), _LeftTex_HDR); }
        half4 fragRight(v2f i) : SV_Target { return OutputSkyColor(tex2D(_RightTex, i.uv), _RightTex_HDR); }
        half4 fragUp(v2f i) : SV_Target { return OutputSkyColor(tex2D(_UpTex, i.uv), _UpTex_HDR); }
        half4 fragDown(v2f i) : SV_Target { return OutputSkyColor(tex2D(_DownTex, i.uv), _DownTex_HDR); }
        ENDCG

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragFront
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragBack
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragLeft
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragRight
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragUp
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragDown
            ENDCG
        }
    }
}
