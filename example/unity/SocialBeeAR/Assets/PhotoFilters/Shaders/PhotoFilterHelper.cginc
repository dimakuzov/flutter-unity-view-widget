float3 BrightnessContrast(float3 value, float brightness, float contrast)
{
    return (value - 0.5) * contrast + 0.5 + brightness;
}

float Level1(float value, float inWhite, float inGamma, float inBlack, float outWhite, float outBlack)
{
    return (pow(((value * 255.0) - inBlack) / (inWhite - inBlack), inGamma) * (outWhite - outBlack) + outBlack) / 255.0;
}

float Level3(float3 value, float inWhite, float inGamma, float inBlack, float outWhite, float outBlack)
{
    float3 inBlack3 = float3(inBlack, inBlack, inBlack);
    float3 outBlack3 = float3(outBlack, outBlack, outBlack);
    
    float3 inWhite3 = float3(inWhite, inWhite, inWhite);
    float3 outWhite3 = float3(outWhite, outWhite, outWhite);

    float3 inGamma3 = float3(inGamma, inGamma, inGamma);

    return (pow(((value * 255.0) - inBlack3) / (inWhite3 - inBlack3), inGamma3) * (outWhite3 - outBlack3) + outBlack3) / 255.0;
}

float Vignette(float2 uv, float radius, float softness)
{
    float distFromCenter = distance(uv, float2(0.5, 0.5));
    return smoothstep(radius, radius - softness, distFromCenter);
}

float RadialGradient(float2 uv, float scale)
{
    float distFromCenter = saturate(distance(uv * rcp(scale), float2(0.5, 0.5) * rcp(scale)) * 2);
    return distFromCenter;
}

float SquadVignette(float2 uv, float radius, float softness)
{
    float distFromCenter = min((1 - abs(uv.x - 0.5) * 2), (1 - abs(uv.y - 0.5) * 2));
    return 1 - smoothstep(radius, radius - softness, distFromCenter);
}

float ToGrayscale(float3 value)
{
    return dot(value, float3(0.2126, 0.7152, 0.0722));
}

float4 ScreenBlend(float4 background, float4 foreground)
{
    return 1 - (1 - background) * (1 - foreground);
}

//Target = Background
//Blend  = foreground

float4 OverlayBlend(float4 background, float4 foreground)
{
    return float4(
                    (background.r < 0.5)    ?   (2 * background.r * foreground.r)   :   (1.0 - 2.0 * (1.0 - background.r) * (1.0 - foreground.r)),
                    (background.g < 0.5)    ?   (2 * background.g * foreground.g)   :   (1.0 - 2.0 * (1.0 - background.g) * (1.0 - foreground.g)),
                    (background.b < 0.5)    ?   (2 * background.b * foreground.b)   :   (1.0 - 2.0 * (1.0 - background.b) * (1.0 - foreground.b)),
                     background.a                                                                                                               );
}

float4 FastBlur(float2 uv, sampler2D inputTexture)
{ 
    float2 singleStepOffset = float2(_ScreenParams.z - 1.0, _ScreenParams.w - 1.0);
    
    fixed4 sum = fixed4(0.0, 0.0, 0.0, 0.0);
    sum += tex2D(inputTexture, uv.xy + singleStepOffset * 0.000000) * 0.204164;
    sum += tex2D(inputTexture, uv.xy + singleStepOffset * 1.407333) * 0.304005;
    sum += tex2D(inputTexture, uv.xy - singleStepOffset * 1.407333) * 0.304005;
    sum += tex2D(inputTexture, uv.xy + singleStepOffset * 3.294215) * 0.093913;
    sum += tex2D(inputTexture, uv.xy - singleStepOffset * 3.294215) * 0.093913;
    return sum;
}

float Rand(float n ) { return frac(sin(n) * 43758.5453123); }

float Noise1(float p)
{
    float fl = floor(p);
    float fc = frac(p);
    return lerp(Rand(fl), Rand(fl + 1.0), fc);
}

float Noise2(float2 n) 
{
    const float2 d = float2(0.0, 1.0);
    float2 b = floor(n);
    float2 f = smoothstep(float2(0.0, 0.0), float2(1.0, 1.0), frac(n));
    return lerp(lerp(Rand(b), Rand(b + d.yx), f.x), lerp(Rand(b + d.xy), Rand(b + d.yy), f.x), f.y);
}

float3 Hash33(float3 p3)
{
    p3 = frac(p3 * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yxz + 33.33);
    return frac((p3.xxy + p3.yxx) * p3.zyx);
}

float3 Noise(float2 n)
{
    float3 pos = float3(n, 1);
    return Hash33(pos);        
 }