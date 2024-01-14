Shader "Unlit/BezierParticleShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _DELTA_OFFSET("DELTA_OFFSET", Float) = 0
        _USE_BEZIER("USE_BEZIER", Float) = 2
        _USE_WIGGLE("USE_WIGGLE", Float) = 2
        _FOLLOW_PATH("FOLLOW_PATH", Float) = 0
        _FOLLOW_SIZE("FOLLOW_SIZE", Float) = 0
        _USE_ROTATE("USE_ROTATE", Float) = 0
        _USE_FADE("USE_FADE", Float) = 0
        _USE_BLINK("USE_BLINK", Float) = 0
        _USE_GRAVITY("USE_GRAVITY", Float) = 0
        _USE_SCALE("USE_SCALE", Float) = 0
        _USE_SPRITE("USE_SPRITE", Float) = 0
        _MULTI_IMGS("MULTI_IMGS", Float) = 0
        _LOOP_COUNT("LOOP_COUNT", Float) = 0

        _StartMin("Start Min", Vector) = (0,0,0,0)
        _StartMax("Start Max", Vector) = (0,0,0,0)

        _SpeedSize("Speed / Size", Vector) = (0,0,0,0)

        _AlphaLifeCurveImgNum("Alpha / Life Curve / Img Num", Vector) = (0,0,0,0)

        _EndMin("End Min", Vector) = (0,0,0,0)
        _EndMax("End Max", Vector) = (0,0,0,0)

        _CtrMin("Ctrl Min", Vector) = (0,0,0,0)
        _CtrMax("Ctrl Max", Vector) = (0,0,0,0)

        _FreqMin("Freq Min", Vector) = (0,0,0,0)
        _FreqMax("Freq Max", Vector) = (0,0,0,0)

        _AmpMin("Amp Min", Vector) = (0,0,0,0)
        _AmpMax("Amp Max", Vector) = (0,0,0,0)

        _DelayMin("Delay Min", Vector) = (0,0,0,0)
        _DelayMax("Delay Max", Vector) = (0,0,0,0)

        _RotMin("Rot Min", Vector) = (0,0,0,0)
        _RotMax("Rot Max", Vector) = (0,0,0,0)

        _GravityMin("Gravity Min", Vector) = (0,0,0,0)
        _GravityMax("Gravity Max", Vector) = (0,0,0,0)

        _Blink("Blink", Vector) = (0,0,0,0)

        _ScaleRotRND("Scale Rot RND", Vector) = (0,0,0,0)

        _PosFractStartTimeSeedTailLen("Pos Fract / Start Time / Seed / TailLen ", Vector) = (0,0,0,0)

        _Global_Opacity("Global Opacity", Float) = 1
    }
        SubShader
        {
            Tags { "Queue" = "Transparent"  "IgnoreProjection" = "True" "RenderType" = "Transparent" }
            LOD 100

            Pass
            {
                Tags{"LightMode" = "ForwardBase"}

                ZWrite Off
                Blend SrcAlpha OneMinusSrcAlpha
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile_instancing

                #include "UnityCG.cginc"
                #include "UnityInstancing.cginc"

                #define PI 3.1415926535
                #define TWO_PI 6.283185307

                uniform float _DELTA_OFFSET;
                uniform float _USE_BEZIER;
                uniform float _USE_WIGGLE;
                uniform float _FOLLOW_PATH;
                uniform float _FOLLOW_SIZE;
                uniform float _USE_ROTATE;
                uniform float _USE_FADE;
                uniform float _USE_BLINK;
                uniform float _USE_GRAVITY;
                uniform float _USE_SCALE;
                uniform float _USE_SPRITE;
                uniform float _MULTI_IMGS;
                uniform float _LOOP_COUNT;

                uniform float4 _StartMin;
                uniform float4 _StartMax;

                uniform float4 _SpeedSize;

                uniform float4 _AlphaLifeCurveImgNum;

                uniform float3 _EndMin;
                uniform float3 _EndMax;

                uniform float3 _CtrMin;
                uniform float3 _CtrMax;

                uniform float3 _FreqMin;
                uniform float3 _FreqMax;

                uniform float3 _AmpMin;
                uniform float3 _AmpMax;

                uniform float3 _DelayMin;
                uniform float3 _DelayMax;

                uniform float3 _RotMin;
                uniform float3 _RotMax;

                uniform float3 _GravityMin;
                uniform float3 _GravityMax;

                uniform float3 _Blink;

                uniform float4 _ScaleRotRND;

                uniform float4 _PosFractStartTimeSeedTailLen;

                uniform float _Global_Opacity;

                //uniform float posFract;
                //uniform float startTime;
                /*uniform float3 startMin;
                uniform float3 startMax;

                uniform float3 endMin;
                uniform float3 endMax;

                uniform float2 speed;
                uniform float2 size;
                uniform float2 alpha;

                uniform float lifeCurve;
                //uniform float time;
                uniform float imgNum;
                uniform float seed;*/

                //uniform float2 scale;

                float hash11(float val) {
                    return frac(sin((val + _PosFractStartTimeSeedTailLen.z) * 12345.67) * 753.5453123);
                }

                float2 hash12(float val) {
                    float3 rnd = frac((val + _PosFractStartTimeSeedTailLen.z) * float3(.1031, .1030, .0973));
                    rnd += dot(rnd, rnd.yzx + 33.33);
                    return frac((rnd.xx + rnd.yz) * rnd.zy);
                }

                float3 hash13(float val) {
                    float3 rnd = frac((val + _PosFractStartTimeSeedTailLen.z) * float3(5.3983, 5.4427, 6.9371));
                    rnd += dot(rnd.zxy, rnd.xyz + float3(21.5351, 14.3137, 15.3219));
                    return frac(float3(rnd.x * rnd.y * 95.4337, rnd.y * rnd.z * 97.597, rnd.z * rnd.x * 93.8365));
                }

                //uniform float3 ctrMin;
                //uniform float3 ctrMax;

                float3 calcBezier(float3 a, float3 b, float3 c, float t) {
                    float s = 1. - t;
                    float3 q = s * s * a + 2. * s * t * b + t * t * c;
                    return q;
                }

                //uniform float3 freqMin;
                //uniform float3 freqMax;
                //uniform float3 ampMin;
                //uniform float3 ampMax;
                //uniform float3 delayMin;
                //uniform float3 delayMax;

                float3 calcWiggle(float3 freq, float3 amp, float3 delay, float timeValue) {
                    float3 value = float3(0,0,0);
                    float3 nowAmp = float3(1,1,1);
                    float baseFreq = 1.0 / (2.0 - 1.0 / pow(2.0, float(_USE_WIGGLE)));
                    float nowFreq = baseFreq;
                    float3 ampCount = float3(0,0,0);
                    for (int i = 0; i < _USE_WIGGLE; i++) {
                        value += nowAmp * sin(timeValue * nowFreq * TWO_PI / freq + delay);
                        nowFreq = baseFreq * pow(2.0, float(i + 1));
                        ampCount += nowAmp;
                        nowAmp *= .5;
                    }
                    return value / ampCount * amp;
                }

                //uniform float3 rotMin;
                //uniform float3 rotMax;
                float3 rotSpeed;
                float3 calcRotate(float3 shape, float3 speed, float timeValue) {
                    float3 r = speed * timeValue;
                    float a = sin(r.x); float b = cos(r.x);
                    float c = sin(r.y); float d = cos(r.y);
                    float e = sin(r.z); float f = cos(r.z);
                    float ac = a * c;
                    float bc = b * c;
                    half3x3 rota = half3x3(d * f, d * e, -c, ac * f - b * e, ac * e + b * f, a * d, bc * f + a * e, bc * e - a * f, b * d);
                    return mul(rota ,shape);
                }


                half3x3 lookAtNormal(float3 origin, float3 target) {
                    float3 ww = normalize(target - origin);
                    float3 rr = float3(0., sign(ww.x), 0.0);
                    float3 uu = normalize(cross(ww, rr));
                    float3 vv = normalize(cross(uu, ww));
                    return -half3x3(ww, vv, uu);
                }

                // 定义拖尾长度
                //uniform float tailLen;
                // 定义一直看向摄像头的模式
                float3 lookAtLine(float3 top, float3 bottom, float3 shape, float2 uv) {
                    float3 A = _WorldSpaceCameraPos - bottom;
                    float3 B = top - bottom;
                    float3 C = normalize(cross(A, B));
                    float3 start = bottom + normalize(B) * shape.y;
                    float3 newPosition = lerp(top, bottom, uv.y);
                    newPosition += shape.x * C;
                    return newPosition;
                }

                //uniform float3 blink;

                float calcBlink(float x, float  y, float z, float w) {
                    return smoothstep(x - z, x + z, w) * smoothstep(y + z, y - z, w);
                }

                // 定义重力

                //uniform float3 gravityMin;
                //uniform float3 gravityMax;
                float3 gravity;

                // 定义形状渐变

                //uniform float2 scale;


                float2 vUv;
                float vOpacity;

                float3 startPnt;

                float3 controlPnt;

                float3 endPnt;


                float3 wiggleFreq;
                float3 wiggleAmp;
                float3 wiggleDelay;


                float newSize;
                float snowOpacity;

                void initValue(float cycleCount,uint id) {
                    // 普通模式
                    startPnt = lerp(_StartMin, _StartMax, hash13(id + cycleCount));
                    endPnt = lerp(_EndMin, _EndMax, hash13(id + cycleCount + 1.));

                    if(_DELTA_OFFSET == 1)
                        endPnt += startPnt;

                    if(_USE_BEZIER == 1)
                        controlPnt = lerp(_CtrMin, _CtrMax, hash13(id + cycleCount + 2.));

                    if(_DELTA_OFFSET == 1)
                        controlPnt += startPnt;

                    if (_USE_WIGGLE > 0) {
                        wiggleFreq = lerp(_FreqMin, _FreqMax, hash13(id + cycleCount + 3.));
                        wiggleAmp = lerp(_AmpMin, _AmpMax, hash13(id + cycleCount + 4.));
                        wiggleDelay = lerp(_DelayMin, _DelayMax, hash13(id + cycleCount + 5.));
                    }

                    if(_USE_ROTATE != 0)
                        rotSpeed = lerp(_RotMin, _RotMax, hash13(id + cycleCount + 6.));

                    if(_USE_GRAVITY == 1)
                        gravity = lerp(_GravityMin, _GravityMax, hash13(id + cycleCount + 7.));

                    snowOpacity = lerp(_AlphaLifeCurveImgNum.x, _AlphaLifeCurveImgNum.y, hash11(id + cycleCount + 8.));
                }



                float3 calcOffsetByTime(float timeValue, float delta) {
                    // remap time with curve < 1 为ease-out > 1为ease-in
                    float life = pow(frac(timeValue) + delta, _AlphaLifeCurveImgNum.z);

                    float3 offset = float3(0, 0, 0);

                    if(_USE_BEZIER == 0)
                        offset = startPnt;
                    else if(_USE_BEZIER == 1)
                        offset = calcBezier(startPnt, controlPnt, endPnt, life);
                    else
                        offset = lerp(startPnt, endPnt, life);

                    if(_USE_GRAVITY == 1)
                        offset = lerp(offset, endPnt + gravity, life);

                    if(_USE_WIGGLE > 0)
                        offset += calcWiggle(wiggleFreq, wiggleAmp, wiggleDelay, timeValue + delta);

                    return offset;
                }

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float2 opacity : TEXCOORD1;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert(appdata v)
            {
                v2f o;
                //o.vertex = UnityObjectToClipPos(v.vertex);
                //o.uv = TRANSFORM_TEX(v.uv, _MainTex);

#ifdef INSTANCING_ON
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                float sizeRnd = hash11(v.instanceID + 6.);
                newSize = lerp(_SpeedSize.z, _SpeedSize.w, sizeRnd);

                float newSpeed = 0.0;
                
                if (_FOLLOW_SIZE == 1)
                    newSpeed = lerp(_SpeedSize.x, _SpeedSize.y, sizeRnd);
                else
                    newSpeed = lerp(_SpeedSize.x, _SpeedSize.y, hash11(v.instanceID + 7.));

                float cycleCount = 0;

                float life = 0;
                float wholeLife = 0;
                if (_LOOP_COUNT > 0) {
                    wholeLife = ((_Time.y - _PosFractStartTimeSeedTailLen.y) * newSpeed + _PosFractStartTimeSeedTailLen.x) - 1.;
                    life = wholeLife;
                    // life += (1.-posFract)*(1.-newSpeed/speed.y);//增加补偿慢速粒子初始life值, 防止慢速粒子过晚出现.
                    if (life >= 0. && life<float(_LOOP_COUNT)) {
                        life = frac(life);
                    }
                    else {
                        life = clamp(life, 0., 1.);
                    }
                    cycleCount = floor(wholeLife);
                }
                else{
                    wholeLife = (_Time.y * newSpeed + _PosFractStartTimeSeedTailLen.x);
                    life = frac(wholeLife);

                    if (_LOOP_COUNT == -1)
                        cycleCount = 0.;
                    else
                        cycleCount = floor(wholeLife);
                }

                initValue(cycleCount,v.instanceID);

                if (_MULTI_IMGS == 1) {
                    float imgRnd = hash11(v.instanceID + cycleCount + 9.);
                    vUv = float2((v.uv.x + floor(imgRnd * _AlphaLifeCurveImgNum.w)) / _AlphaLifeCurveImgNum.w, v.uv.y);
                }
                else {
                    vUv = v.uv;
                }

                vOpacity = snowOpacity;

                if(_USE_FADE > 0)
                    vOpacity *= smoothstep(0., .2, sin(life * PI));

                if (_USE_BLINK > 0) {
                    float2 blinkRnd = hash12(v.instanceID + cycleCount + 10.);
                    vOpacity *= lerp(_Blink.x, _Blink.y, calcBlink(.1, .5, .2, sin(_Time.y * _Blink.z * (.5 + .5 * blinkRnd.x) + blinkRnd.y * TWO_PI) * .5 + .5));
                }

                float3 scalePos = v.vertex.xyz * newSize;

                if(_USE_SCALE == 1)
                    scalePos *= lerp(_ScaleRotRND.x, _ScaleRotRND.y, life);

                if(_USE_BLINK == 2)
                    scalePos *= vOpacity;

                float3 offset = calcOffsetByTime(wholeLife, 0.);
                // 旋转跟随路径
                if (_FOLLOW_PATH == 1) {
                    float3 offsetNext = calcOffsetByTime(wholeLife, .01);
                    offset += mul(lookAtNormal(offsetNext, offset),scalePos);
                    o.vertex = UnityObjectToClipPos(float4(offset, 1.0));
                }
                // 旋转跟随路径 并始终朝向摄像机 适合平面做方向线条
                else if (_FOLLOW_PATH == 2) {
                    float3 offsetNext = calcOffsetByTime(wholeLife, .01 * _PosFractStartTimeSeedTailLen.w);
                    offset = lookAtLine(offsetNext, offset, scalePos,v.uv);
                    o.vertex = UnityObjectToClipPos(float4(offset, 1.0));
                }
                // 不跟随路径 可以支持自转
                else {
                    if (_USE_ROTATE == 1) {
                        scalePos = calcRotate(scalePos, rotSpeed, wholeLife);
                    }
                    else if (_USE_ROTATE == 2) {
                        scalePos = calcRotate(scalePos, rotSpeed, 1.);
                    }

                    if (_USE_SPRITE == 1) {
                        float4 mvPosition = mul(UNITY_MATRIX_MV,float4(offset, 1.0));
                        mvPosition.xyz += scalePos;
                        o.vertex = mul(UNITY_MATRIX_MV,mvPosition);
                    }
                    else {
                        offset += scalePos;
                        //o.vertex = UnityObjectToClipPos(float4(scalePos,1.0));
                        o.vertex = UnityObjectToClipPos(float4(offset, 1.0));
                    }
                }
                o.uv = TRANSFORM_TEX(vUv, _MainTex);
                o.opacity = float2(vOpacity, 0.0);
#else
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.opacity = float2(1.0, 1.0);
#endif
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);

                col.a *= i.opacity * _Global_Opacity;
                return col;
            }
            ENDCG
        }
    }
}
