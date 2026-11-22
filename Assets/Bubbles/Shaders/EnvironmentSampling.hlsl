// 共通の投影処理により、写真背景とシャボン玉の屈折を一致させる。
TEXTURE2D(_HDRI); SAMPLER(sampler_HDRI);
float3 EnvironmentRay(float2 uv)
{
    float aspect=_ScreenParams.x/max(_ScreenParams.y,1);
    return normalize(float3((uv*2-1)*float2(aspect,1)*_ViewTanHalfFov,1));
}
float3 EnvironmentColour(float3 direction)
{
    direction=normalize(direction);
    float2 uv=float2(atan2(direction.x,direction.z)*.159154943+.5+_EnvironmentYaw,asin(clamp(direction.y,-1,1))*.318309886+.5);
    float3 radiance=max(SAMPLE_TEXTURE2D_LOD(_HDRI,sampler_HDRI,uv,0).rgb,0)*exp2(_Exposure);
    return radiance/(1+radiance);
}
