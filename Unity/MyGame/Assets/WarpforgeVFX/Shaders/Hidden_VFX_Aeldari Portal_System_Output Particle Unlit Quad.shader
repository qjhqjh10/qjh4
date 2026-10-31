// Hidden_VFX_Aeldari Portal_System_Output Particle Unlit Quad.shader
// 名称 = `Hidden/VFX/Aeldari Portal/System/Output Particle Unlit Quad`（原版 VFX Graph 输出 shader 的复刻件 · Aeldari Portal · System）
//
// 唯一来源（第一权威）= Ruri 真反编译体（SPIR-V → HLSL 交叉编译产物）：
//   `D:/2/tools/ruri_out_1020_allshaders/ExportedProject/Assets/1e59d1729d67847d186ed2dfccb91988_1.shader`
//   原生 shader 本体另在随包 bundle 里（`StreamingAssets/WarpforgeVFX/wf_arena_shaders.bundle` /
//   `wf_shaders_extra.bundle`，按名可取）；但 Unity 拒绝把 bundle 里的 Shader 序列化成工程资产
//   （项目已知坑），而 `.mat` 的 `m_Shader` 只认工程资产 ⇒ 按 R4-04 的「手写 6 个 shader」路线，
//   把原版反编译体**原样搬进工程**。
//
// 只做了**可移植性归一**（逐条都可逆、无行为差异）：
//   ① 去掉 `#pragma use_dxc` —— 不是 Unity 认的 pragma（Unity 行的是 `never_use_dxc`），让 Unity 自选编译器；
//   ② `register(tN, space0)` → `register(tN)` —— register space 是 DXC/SPIR-V 写法，FXC 不认（槽位不变）；
//   ③ `precise float` → `float` —— 指令级精度提示，不改变数值结果；
//   ④ `float(uint64_t(x))` → `float(x)` —— 同一个 uint→float 转换，去掉 64 位依赖；
//   ⑤ `SPIRV_Cross_BaseVertex/BaseInstance` → 0 —— VFX 走 DrawProcedural，基础顶点/实例恒为 0。
//   **未改**：Shader 名 · SubShader Tags · Pass 名/Tags · Blend/ZWrite/Cull/ZClip · 顶点/片元算式。
//
// 🔑 **`.meta` 的 guid 刻意沿用【原版 shader 的 guid】**：`c5062bee1d8d1c041a065a71297da43f`
//   —— 6 个 `.mat` 的 `m_Shader` 里本来就写着这个 guid（原版遗留、一直悬空），
//   把工程 shader 的 guid 设成同一个值 ⇒ **`.mat` 一个字节都不用改**，引用自动接上
//   （该 guid 在工程 `Assets/**` + `Packages/**` 的 `.meta` 里现读 0 命中，无冲突）。
//
// 🔴 本文件**不是**「自己设计」：语义完全来自原版反编译体。
// ⚠️ 本批不跑 Unity ⇒ **未经 Unity 编译验证**；真机/批处理验收见 `资料/完全复刻/报告/B107_R4.md`。
Shader "Hidden/VFX/Aeldari Portal/System/Output Particle Unlit Quad" {
    SubShader {
        Tags {
            "IGNOREPROJECTOR"="true"
            "QUEUE"="Transparent+0"
            "RenderType"="Transparent"
        }
        Pass {
            Name "UniversalForwardOnly"
            Blend SrcAlpha One, SrcAlpha One
            ZClip On
            ZWrite Off
            Cull Off
            Tags {
                "LIGHTMODE"="UniversalForwardOnly"
            }
            HLSLPROGRAM
            #pragma target 5.0
            #include "HLSLSupport.cginc"
            #pragma vertex main
            #pragma fragment main

            // Stage: Vertex
            #ifdef SHADER_STAGE_VERTEX
            // Stage: Vertex, Blob: 1, ParamBlob: 0, Language: hlsl
            // Generated names (not authored, no CPU-side property matches them):  <Buffer>_Unmapped = byte range no symbol describes;
            ByteAddressBuffer graphValuesBuffer : register(t0);
            ByteAddressBuffer attributeBuffer : register(t1);
            cbuffer type_Globals : register(b0)
            {
                float4 _Globals_Unmapped[66] : packoffset(c0);
                column_major float4x4 unity_MatrixV : packoffset(c66);
                float4 _Globals_Unmapped_at_1120[8] : packoffset(c70);
                column_major float4x4 unity_MatrixVP : packoffset(c78);
            };

            cbuffer type_UnityPerDraw : register(b1)
            {
                column_major float4x4 unity_ObjectToWorld : packoffset(c0);
                column_major float4x4 unity_WorldToObject : packoffset(c4);
            };

            cbuffer type_PerInstance : register(b2)
            {
                float _InstanceIndex : packoffset(c0);
                float _InstanceActiveIndex : packoffset(c0.y);
            };

            Texture2D<float4> bakedTexture : register(t2);
            SamplerState samplerbakedTexture : register(s0);

            static float4 gl_Position;
            static int gl_VertexIndex;
            static int gl_InstanceIndex;
            static int gl_BaseVertexARB;
            static int gl_BaseInstanceARB;
                        static float2 TEXCOORD;
            static float4 COLOR;
            static float3 TEXCOORD_6;
            static uint2 INDEX;

            struct SPIRV_Cross_Input
            {
                uint gl_VertexIndex : SV_VertexID;
                uint gl_InstanceIndex : SV_InstanceID;
            };

            struct SPIRV_Cross_Output
            {
                float2 TEXCOORD : TEXCOORD4;
                float4 COLOR : TEXCOORD8;
                float3 TEXCOORD_6 : TEXCOORD12;
                uint2 INDEX : TEXCOORD16;
                float4 gl_Position : SV_Position;
            };

            uint spvBitfieldInsert(uint Base, uint Insert, uint Offset, uint Count)
            {
                uint Mask = Count == 32 ? 0xffffffff : (((1u << Count) - 1) << (Offset & 31));
                return (Base & ~Mask) | ((Insert << Offset) & Mask);
            }

            uint2 spvBitfieldInsert(uint2 Base, uint2 Insert, uint Offset, uint Count)
            {
                uint Mask = Count == 32 ? 0xffffffff : (((1u << Count) - 1) << (Offset & 31));
                return (Base & ~Mask) | ((Insert << Offset) & Mask);
            }

            uint3 spvBitfieldInsert(uint3 Base, uint3 Insert, uint Offset, uint Count)
            {
                uint Mask = Count == 32 ? 0xffffffff : (((1u << Count) - 1) << (Offset & 31));
                return (Base & ~Mask) | ((Insert << Offset) & Mask);
            }

            uint4 spvBitfieldInsert(uint4 Base, uint4 Insert, uint Offset, uint Count)
            {
                uint Mask = Count == 32 ? 0xffffffff : (((1u << Count) - 1) << (Offset & 31));
                return (Base & ~Mask) | ((Insert << Offset) & Mask);
            }

            uint ByteAddressMask(uint index, uint stride)
            {
                return index & (4294967295u / stride);
            }

            float _60(float3 _58, float3 _59)
            {
                float _64 = _58.x * _59.x;
                return mad(_58.z, _59.z, mad(_58.y, _59.y, _64));
            }

            float _74(uint _73)
            {
                return float(_73);
            }

            void vert_main()
            {
                uint _82 = uint(gl_VertexIndex) - uint(gl_BaseVertexARB);
                uint _91 = (_82 >> 2u) + ((uint(gl_InstanceIndex) - uint(gl_BaseInstanceARB)) << 11u);
                uint _97 = asuint(_InstanceActiveIndex);
                bool _116 = _91 >= graphValuesBuffer.Load<uint>(ByteAddressMask((_97 << 6u) >> 2u, 4u) * 4 + 0);
                if (_116)
                {
                    gl_Position.x = asfloat(0xffffffffu /* nan */);
                    gl_Position.y = 0.0f;
                    gl_Position.z = 0.0f;
                    gl_Position.w = 0.0f;
                    COLOR.x = 0.0f;
                    COLOR.y = 0.0f;
                    COLOR.z = 0.0f;
                    COLOR.w = 0.0f;
                    TEXCOORD_6.x = 0.0f;
                    TEXCOORD_6.y = 0.0f;
                    TEXCOORD_6.z = 0.0f;
                    TEXCOORD.x = 0.0f;
                    TEXCOORD.y = 0.0f;
                    INDEX.x = 0u;
                    INDEX.y = 0u;
                    return;
                }
                uint _142 = asuint(_InstanceIndex) * 13312u;
                uint _145 = ((_91 << 2u) + _142) << 2u;
                bool _153 = attributeBuffer.Load<uint>(ByteAddressMask((_145 + 4108u) >> 2u, 4u) * 4 + 0) == 0u;
                if (_153)
                {
                    gl_Position.x = asfloat(0xffffffffu /* nan */);
                    gl_Position.y = 0.0f;
                    gl_Position.z = 0.0f;
                    gl_Position.w = 0.0f;
                    COLOR.x = 0.0f;
                    COLOR.y = 0.0f;
                    COLOR.z = 0.0f;
                    COLOR.w = 0.0f;
                    TEXCOORD_6.x = 0.0f;
                    TEXCOORD_6.y = 0.0f;
                    TEXCOORD_6.z = 0.0f;
                    TEXCOORD.x = 0.0f;
                    TEXCOORD.y = 0.0f;
                    INDEX.x = 0u;
                    INDEX.y = 0u;
                    return;
                }
                if (_153)
                {
                    gl_Position.x = _116 ? asfloat(0xffffffffu /* nan */) : 0.0f;
                    gl_Position.y = 0.0f;
                    gl_Position.z = 0.0f;
                    gl_Position.w = 0.0f;
                    COLOR.x = 0.0f;
                    COLOR.y = 0.0f;
                    COLOR.z = 0.0f;
                    COLOR.w = 0.0f;
                    TEXCOORD_6.x = 0.0f;
                    TEXCOORD_6.y = 0.0f;
                    TEXCOORD_6.z = 0.0f;
                    TEXCOORD.x = 0.0f;
                    TEXCOORD.y = 0.0f;
                    INDEX.x = 0u;
                    INDEX.y = 0u;
                    return;
                }
                uint _189 = ByteAddressMask(spvBitfieldInsert(48u, _97, 6u, 26u) >> 2u, 4u);
                uint2 _195 = uint2(graphValuesBuffer.Load<uint>(_189 * 4 + 0), graphValuesBuffer.Load<uint>((_189 + 1u) * 4 + 0));
                uint _200 = (_91 + _142) << 2u;
                uint _212 = ByteAddressMask((_145 + 4096u) >> 2u, 4u);
                uint3 _222 = uint3(attributeBuffer.Load<uint>(_212 * 4 + 0), attributeBuffer.Load<uint>((_212 + 1u) * 4 + 0), attributeBuffer.Load<uint>((_212 + 2u) * 4 + 0));
                uint _229 = ByteAddressMask((_145 + 36864u) >> 2u, 4u);
                uint4 _242 = uint4(attributeBuffer.Load<uint>(_229 * 4 + 0), attributeBuffer.Load<uint>((_229 + 1u) * 4 + 0), attributeBuffer.Load<uint>((_229 + 2u) * 4 + 0), attributeBuffer.Load<uint>((_229 + 3u) * 4 + 0));
                uint _246 = _242.w;
                float _263 = (unity_MatrixV[1u].x);
                float _264 = (unity_MatrixV[1u].y);
                float _265 = (unity_MatrixV[1u].z);
                float _278 = (unity_MatrixV[0u].x);
                float _279 = (unity_MatrixV[0u].y);
                float _280 = (unity_MatrixV[0u].z);
                float _293 = (unity_MatrixV[2u].x);
                float _294 = (unity_MatrixV[2u].y);
                float _295 = (unity_MatrixV[2u].z);
                float _298 = mad(_293, unity_ObjectToWorld[0u].z, mad(_278, unity_ObjectToWorld[0u].x, _263 * unity_ObjectToWorld[0u].y));
                float _299 = mad(_294, unity_ObjectToWorld[0u].z, mad(_279, unity_ObjectToWorld[0u].x, _264 * unity_ObjectToWorld[0u].y));
                float _314 = mad(_293, unity_ObjectToWorld[1u].z, mad(_278, unity_ObjectToWorld[1u].x, _263 * unity_ObjectToWorld[1u].y));
                float _315 = mad(_294, unity_ObjectToWorld[1u].z, mad(_279, unity_ObjectToWorld[1u].x, _264 * unity_ObjectToWorld[1u].y));
                float _332 = mad(_293, unity_ObjectToWorld[2u].z, mad(_278, unity_ObjectToWorld[2u].x, _263 * unity_ObjectToWorld[2u].y));
                float _334 = mad(_294, unity_ObjectToWorld[2u].z, mad(_279, unity_ObjectToWorld[2u].x, _264 * unity_ObjectToWorld[2u].y));
                float3 _335 = float3(_298, _314, _332);
                float _337 = rsqrt(_60(_335, _335));
                float3 _341 = float3(_299, _315, _334);
                float _343 = rsqrt(_60(_341, _341));
                float _347 = -mad(_295, unity_ObjectToWorld[0u].z, mad(_280, unity_ObjectToWorld[0u].x, _265 * unity_ObjectToWorld[0u].y));
                float _348 = -mad(_295, unity_ObjectToWorld[1u].z, mad(_280, unity_ObjectToWorld[1u].x, _265 * unity_ObjectToWorld[1u].y));
                float _349 = -mad(_295, unity_ObjectToWorld[2u].z, mad(_280, unity_ObjectToWorld[2u].x, _265 * unity_ObjectToWorld[2u].y));
                float3 _350 = float3(_347, _348, _349);
                float _352 = rsqrt(_60(_350, _350));
                float _361 = mad(clamp(asfloat(attributeBuffer.Load<uint>(ByteAddressMask((_200 + 20480u) >> 2u, 4u) * 4 + 0)) / asfloat(attributeBuffer.Load<uint>(ByteAddressMask(_200 >> 2u, 4u) * 4 + 0)), 0.0f, 1.0f), 0.9921875f, 0.00390625f);
                float _390 = _74(_82 & 1u);
                float _391 = _74(_82 & 2u);
                float _396 = asfloat(_246);
                float3 _405 = float3(_390 - 0.5f, mad(_391, 0.5f, -0.5f), 1.0f);
                float _406 = _60(float3(_396 * (_337 * _298), _396 * (_343 * _299), asfloat(_222.x)), _405);
                float _409 = _60(float3(_396 * (_337 * _314), _396 * (_343 * _315), asfloat(_222.y)), _405);
                float _412 = _60(float3(_396 * (_337 * _332), _396 * (_343 * _334), asfloat(_222.z)), _405);
                float _442 = mad(unity_ObjectToWorld[2u].x, _412, mad(unity_ObjectToWorld[0u].x, _406, _409 * unity_ObjectToWorld[1u].x)) + unity_ObjectToWorld[3u].x;
                float _443 = mad(unity_ObjectToWorld[2u].y, _412, mad(unity_ObjectToWorld[0u].y, _406, _409 * unity_ObjectToWorld[1u].y)) + unity_ObjectToWorld[3u].y;
                float _444 = mad(unity_ObjectToWorld[2u].z, _412, mad(unity_ObjectToWorld[0u].z, _406, _409 * unity_ObjectToWorld[1u].z)) + unity_ObjectToWorld[3u].z;
                gl_Position.x = mad((unity_MatrixVP[2u].x), _444, mad((unity_MatrixVP[0u].x), _442, _443 * (unity_MatrixVP[1u].x))) + (unity_MatrixVP[3u].x);
                gl_Position.y = mad((unity_MatrixVP[2u].y), _444, mad((unity_MatrixVP[0u].y), _442, _443 * (unity_MatrixVP[1u].y))) + (unity_MatrixVP[3u].y);
                gl_Position.z = mad((unity_MatrixVP[2u].z), _444, mad((unity_MatrixVP[0u].z), _442, _443 * (unity_MatrixVP[1u].z))) + (unity_MatrixVP[3u].z);
                gl_Position.w = mad((unity_MatrixVP[2u].w), _444, mad((unity_MatrixVP[0u].w), _442, _443 * (unity_MatrixVP[1u].w))) + (unity_MatrixVP[3u].w);
                float _513 = asfloat(_246);
                float _514 = 1.0f / _513;
                float3 _521 = float3(-(_514 * (_352 * _347)), -(_514 * (_352 * _348)), -(_514 * (_352 * _349)));
                float _528 = _60(_521, float3(unity_WorldToObject[0u].x, unity_WorldToObject[0u].y, unity_WorldToObject[0u].z));
                float _535 = _60(_521, float3(unity_WorldToObject[1u].x, unity_WorldToObject[1u].y, unity_WorldToObject[1u].z));
                float _542 = _60(_521, float3(unity_WorldToObject[2u].x, unity_WorldToObject[2u].y, unity_WorldToObject[2u].z));
                float3 _543 = float3(_528, _535, _542);
                float _545 = rsqrt(_60(_543, _543));
                float _552 = ((_513 * (_513 * _513)) < 0.0f) ? (-1.0f) : 1.0f;
                TEXCOORD_6.x = (_545 * _528) * _552;
                TEXCOORD_6.y = (_545 * _535) * _552;
                TEXCOORD_6.z = (_545 * _542) * _552;
                COLOR.x = asfloat(_242.x);
                COLOR.y = asfloat(_242.y);
                COLOR.z = asfloat(_242.z);
                COLOR.w = bakedTexture.SampleLevel(samplerbakedTexture, float2((asfloat(_195.y) > 0.5f) ? (mad(-frac(_361 * 128.0f), 0.0078125f, _361) + 0.00390625f) : _361, asfloat(_195.x)), 0.0f).w;
                TEXCOORD.x = _390;
                TEXCOORD.y = _391 * 0.5f;
                INDEX.x = 0u;
                INDEX.y = _97;
            }

            SPIRV_Cross_Output main(SPIRV_Cross_Input stage_input)
            {
                gl_VertexIndex = int(stage_input.gl_VertexIndex);
                gl_InstanceIndex = int(stage_input.gl_InstanceIndex);
                gl_BaseVertexARB = 0;
                gl_BaseInstanceARB = 0;
                vert_main();
                SPIRV_Cross_Output stage_output;
                stage_output.gl_Position = gl_Position;
                stage_output.TEXCOORD = TEXCOORD;
                stage_output.COLOR = COLOR;
                stage_output.TEXCOORD_6 = TEXCOORD_6;
                stage_output.INDEX = INDEX;
                return stage_output;
            }

            #endif

            // Stage: Fragment
            #ifdef SHADER_STAGE_FRAGMENT
            // Stage: Fragment, Blob: 3, ParamBlob: 2, Language: hlsl
            // Generated names (not authored, no CPU-side property matches them):  <Buffer>_Unmapped = byte range no symbol describes;
            cbuffer type_Globals : register(b0)
            {
                float4 _Globals_Unmapped[4u] : packoffset(c0);
                float2 _GlobalMipBias : packoffset(c4);
            };

            Texture2D<float4> mainTexture : register(t0);
            SamplerState samplermainTexture : register(s0);

            static float2 TEXCOORD;
            static float4 COLOR;
            static float4 SV_Target;

            struct SPIRV_Cross_Input
            {
                float2 TEXCOORD : TEXCOORD4;
                nointerpolation float4 COLOR : TEXCOORD8;
            };

            struct SPIRV_Cross_Output
            {
                float4 SV_Target : SV_Target0;
            };

            void frag_main()
            {
                float4 _44 = mainTexture.SampleBias(samplermainTexture, float2(TEXCOORD.x, TEXCOORD.y), asfloat(asuint(_GlobalMipBias.x)));
                SV_Target.w = clamp(_44.w * COLOR.w, 0.0f, 1.0f);
                SV_Target.x = _44.x * COLOR.x;
                SV_Target.y = _44.y * COLOR.y;
                SV_Target.z = _44.z * COLOR.z;
            }

            SPIRV_Cross_Output main(SPIRV_Cross_Input stage_input)
            {
                TEXCOORD = stage_input.TEXCOORD;
                COLOR = stage_input.COLOR;
                frag_main();
                SPIRV_Cross_Output stage_output;
                stage_output.SV_Target = SV_Target;
                return stage_output;
            }

            #endif

            ENDHLSL
        }
        Pass {
            Blend SrcAlpha One, SrcAlpha One
            ZClip On
            ZWrite Off
            Cull Off
            Tags {
                "LIGHTMODE"="Universal2D"
            }
            HLSLPROGRAM
            #pragma target 5.0
            #include "HLSLSupport.cginc"
            #pragma vertex main
            #pragma fragment main

            // Stage: Vertex
            #ifdef SHADER_STAGE_VERTEX
            // Stage: Vertex, Blob: 1, ParamBlob: 0, Language: hlsl
            // Generated names (not authored, no CPU-side property matches them):  <Buffer>_Unmapped = byte range no symbol describes;
            ByteAddressBuffer graphValuesBuffer : register(t0);
            ByteAddressBuffer attributeBuffer : register(t1);
            cbuffer type_Globals : register(b0)
            {
                float4 _Globals_Unmapped[66] : packoffset(c0);
                column_major float4x4 unity_MatrixV : packoffset(c66);
                float4 _Globals_Unmapped_at_1120[8] : packoffset(c70);
                column_major float4x4 unity_MatrixVP : packoffset(c78);
            };

            cbuffer type_UnityPerDraw : register(b1)
            {
                column_major float4x4 unity_ObjectToWorld : packoffset(c0);
                column_major float4x4 unity_WorldToObject : packoffset(c4);
            };

            cbuffer type_PerInstance : register(b2)
            {
                float _InstanceIndex : packoffset(c0);
                float _InstanceActiveIndex : packoffset(c0.y);
            };

            Texture2D<float4> bakedTexture : register(t2);
            SamplerState samplerbakedTexture : register(s0);

            static float4 gl_Position;
            static int gl_VertexIndex;
            static int gl_InstanceIndex;
            static int gl_BaseVertexARB;
            static int gl_BaseInstanceARB;
                        static float2 TEXCOORD;
            static float4 COLOR;
            static float3 TEXCOORD_6;
            static uint2 INDEX;

            struct SPIRV_Cross_Input
            {
                uint gl_VertexIndex : SV_VertexID;
                uint gl_InstanceIndex : SV_InstanceID;
            };

            struct SPIRV_Cross_Output
            {
                float2 TEXCOORD : TEXCOORD4;
                float4 COLOR : TEXCOORD8;
                float3 TEXCOORD_6 : TEXCOORD12;
                uint2 INDEX : TEXCOORD16;
                float4 gl_Position : SV_Position;
            };

            uint spvBitfieldInsert(uint Base, uint Insert, uint Offset, uint Count)
            {
                uint Mask = Count == 32 ? 0xffffffff : (((1u << Count) - 1) << (Offset & 31));
                return (Base & ~Mask) | ((Insert << Offset) & Mask);
            }

            uint2 spvBitfieldInsert(uint2 Base, uint2 Insert, uint Offset, uint Count)
            {
                uint Mask = Count == 32 ? 0xffffffff : (((1u << Count) - 1) << (Offset & 31));
                return (Base & ~Mask) | ((Insert << Offset) & Mask);
            }

            uint3 spvBitfieldInsert(uint3 Base, uint3 Insert, uint Offset, uint Count)
            {
                uint Mask = Count == 32 ? 0xffffffff : (((1u << Count) - 1) << (Offset & 31));
                return (Base & ~Mask) | ((Insert << Offset) & Mask);
            }

            uint4 spvBitfieldInsert(uint4 Base, uint4 Insert, uint Offset, uint Count)
            {
                uint Mask = Count == 32 ? 0xffffffff : (((1u << Count) - 1) << (Offset & 31));
                return (Base & ~Mask) | ((Insert << Offset) & Mask);
            }

            uint ByteAddressMask(uint index, uint stride)
            {
                return index & (4294967295u / stride);
            }

            float _60(float3 _58, float3 _59)
            {
                float _64 = _58.x * _59.x;
                return mad(_58.z, _59.z, mad(_58.y, _59.y, _64));
            }

            float _74(uint _73)
            {
                return float(_73);
            }

            void vert_main()
            {
                uint _82 = uint(gl_VertexIndex) - uint(gl_BaseVertexARB);
                uint _91 = (_82 >> 2u) + ((uint(gl_InstanceIndex) - uint(gl_BaseInstanceARB)) << 11u);
                uint _97 = asuint(_InstanceActiveIndex);
                bool _116 = _91 >= graphValuesBuffer.Load<uint>(ByteAddressMask((_97 << 6u) >> 2u, 4u) * 4 + 0);
                if (_116)
                {
                    gl_Position.x = asfloat(0xffffffffu /* nan */);
                    gl_Position.y = 0.0f;
                    gl_Position.z = 0.0f;
                    gl_Position.w = 0.0f;
                    COLOR.x = 0.0f;
                    COLOR.y = 0.0f;
                    COLOR.z = 0.0f;
                    COLOR.w = 0.0f;
                    TEXCOORD_6.x = 0.0f;
                    TEXCOORD_6.y = 0.0f;
                    TEXCOORD_6.z = 0.0f;
                    TEXCOORD.x = 0.0f;
                    TEXCOORD.y = 0.0f;
                    INDEX.x = 0u;
                    INDEX.y = 0u;
                    return;
                }
                uint _142 = asuint(_InstanceIndex) * 13312u;
                uint _145 = ((_91 << 2u) + _142) << 2u;
                bool _153 = attributeBuffer.Load<uint>(ByteAddressMask((_145 + 4108u) >> 2u, 4u) * 4 + 0) == 0u;
                if (_153)
                {
                    gl_Position.x = asfloat(0xffffffffu /* nan */);
                    gl_Position.y = 0.0f;
                    gl_Position.z = 0.0f;
                    gl_Position.w = 0.0f;
                    COLOR.x = 0.0f;
                    COLOR.y = 0.0f;
                    COLOR.z = 0.0f;
                    COLOR.w = 0.0f;
                    TEXCOORD_6.x = 0.0f;
                    TEXCOORD_6.y = 0.0f;
                    TEXCOORD_6.z = 0.0f;
                    TEXCOORD.x = 0.0f;
                    TEXCOORD.y = 0.0f;
                    INDEX.x = 0u;
                    INDEX.y = 0u;
                    return;
                }
                if (_153)
                {
                    gl_Position.x = _116 ? asfloat(0xffffffffu /* nan */) : 0.0f;
                    gl_Position.y = 0.0f;
                    gl_Position.z = 0.0f;
                    gl_Position.w = 0.0f;
                    COLOR.x = 0.0f;
                    COLOR.y = 0.0f;
                    COLOR.z = 0.0f;
                    COLOR.w = 0.0f;
                    TEXCOORD_6.x = 0.0f;
                    TEXCOORD_6.y = 0.0f;
                    TEXCOORD_6.z = 0.0f;
                    TEXCOORD.x = 0.0f;
                    TEXCOORD.y = 0.0f;
                    INDEX.x = 0u;
                    INDEX.y = 0u;
                    return;
                }
                uint _189 = ByteAddressMask(spvBitfieldInsert(48u, _97, 6u, 26u) >> 2u, 4u);
                uint2 _195 = uint2(graphValuesBuffer.Load<uint>(_189 * 4 + 0), graphValuesBuffer.Load<uint>((_189 + 1u) * 4 + 0));
                uint _200 = (_91 + _142) << 2u;
                uint _212 = ByteAddressMask((_145 + 4096u) >> 2u, 4u);
                uint3 _222 = uint3(attributeBuffer.Load<uint>(_212 * 4 + 0), attributeBuffer.Load<uint>((_212 + 1u) * 4 + 0), attributeBuffer.Load<uint>((_212 + 2u) * 4 + 0));
                uint _229 = ByteAddressMask((_145 + 36864u) >> 2u, 4u);
                uint4 _242 = uint4(attributeBuffer.Load<uint>(_229 * 4 + 0), attributeBuffer.Load<uint>((_229 + 1u) * 4 + 0), attributeBuffer.Load<uint>((_229 + 2u) * 4 + 0), attributeBuffer.Load<uint>((_229 + 3u) * 4 + 0));
                uint _246 = _242.w;
                float _263 = (unity_MatrixV[1u].x);
                float _264 = (unity_MatrixV[1u].y);
                float _265 = (unity_MatrixV[1u].z);
                float _278 = (unity_MatrixV[0u].x);
                float _279 = (unity_MatrixV[0u].y);
                float _280 = (unity_MatrixV[0u].z);
                float _293 = (unity_MatrixV[2u].x);
                float _294 = (unity_MatrixV[2u].y);
                float _295 = (unity_MatrixV[2u].z);
                float _298 = mad(_293, unity_ObjectToWorld[0u].z, mad(_278, unity_ObjectToWorld[0u].x, _263 * unity_ObjectToWorld[0u].y));
                float _299 = mad(_294, unity_ObjectToWorld[0u].z, mad(_279, unity_ObjectToWorld[0u].x, _264 * unity_ObjectToWorld[0u].y));
                float _314 = mad(_293, unity_ObjectToWorld[1u].z, mad(_278, unity_ObjectToWorld[1u].x, _263 * unity_ObjectToWorld[1u].y));
                float _315 = mad(_294, unity_ObjectToWorld[1u].z, mad(_279, unity_ObjectToWorld[1u].x, _264 * unity_ObjectToWorld[1u].y));
                float _332 = mad(_293, unity_ObjectToWorld[2u].z, mad(_278, unity_ObjectToWorld[2u].x, _263 * unity_ObjectToWorld[2u].y));
                float _334 = mad(_294, unity_ObjectToWorld[2u].z, mad(_279, unity_ObjectToWorld[2u].x, _264 * unity_ObjectToWorld[2u].y));
                float3 _335 = float3(_298, _314, _332);
                float _337 = rsqrt(_60(_335, _335));
                float3 _341 = float3(_299, _315, _334);
                float _343 = rsqrt(_60(_341, _341));
                float _347 = -mad(_295, unity_ObjectToWorld[0u].z, mad(_280, unity_ObjectToWorld[0u].x, _265 * unity_ObjectToWorld[0u].y));
                float _348 = -mad(_295, unity_ObjectToWorld[1u].z, mad(_280, unity_ObjectToWorld[1u].x, _265 * unity_ObjectToWorld[1u].y));
                float _349 = -mad(_295, unity_ObjectToWorld[2u].z, mad(_280, unity_ObjectToWorld[2u].x, _265 * unity_ObjectToWorld[2u].y));
                float3 _350 = float3(_347, _348, _349);
                float _352 = rsqrt(_60(_350, _350));
                float _361 = mad(clamp(asfloat(attributeBuffer.Load<uint>(ByteAddressMask((_200 + 20480u) >> 2u, 4u) * 4 + 0)) / asfloat(attributeBuffer.Load<uint>(ByteAddressMask(_200 >> 2u, 4u) * 4 + 0)), 0.0f, 1.0f), 0.9921875f, 0.00390625f);
                float _390 = _74(_82 & 1u);
                float _391 = _74(_82 & 2u);
                float _396 = asfloat(_246);
                float3 _405 = float3(_390 - 0.5f, mad(_391, 0.5f, -0.5f), 1.0f);
                float _406 = _60(float3(_396 * (_337 * _298), _396 * (_343 * _299), asfloat(_222.x)), _405);
                float _409 = _60(float3(_396 * (_337 * _314), _396 * (_343 * _315), asfloat(_222.y)), _405);
                float _412 = _60(float3(_396 * (_337 * _332), _396 * (_343 * _334), asfloat(_222.z)), _405);
                float _442 = mad(unity_ObjectToWorld[2u].x, _412, mad(unity_ObjectToWorld[0u].x, _406, _409 * unity_ObjectToWorld[1u].x)) + unity_ObjectToWorld[3u].x;
                float _443 = mad(unity_ObjectToWorld[2u].y, _412, mad(unity_ObjectToWorld[0u].y, _406, _409 * unity_ObjectToWorld[1u].y)) + unity_ObjectToWorld[3u].y;
                float _444 = mad(unity_ObjectToWorld[2u].z, _412, mad(unity_ObjectToWorld[0u].z, _406, _409 * unity_ObjectToWorld[1u].z)) + unity_ObjectToWorld[3u].z;
                gl_Position.x = mad((unity_MatrixVP[2u].x), _444, mad((unity_MatrixVP[0u].x), _442, _443 * (unity_MatrixVP[1u].x))) + (unity_MatrixVP[3u].x);
                gl_Position.y = mad((unity_MatrixVP[2u].y), _444, mad((unity_MatrixVP[0u].y), _442, _443 * (unity_MatrixVP[1u].y))) + (unity_MatrixVP[3u].y);
                gl_Position.z = mad((unity_MatrixVP[2u].z), _444, mad((unity_MatrixVP[0u].z), _442, _443 * (unity_MatrixVP[1u].z))) + (unity_MatrixVP[3u].z);
                gl_Position.w = mad((unity_MatrixVP[2u].w), _444, mad((unity_MatrixVP[0u].w), _442, _443 * (unity_MatrixVP[1u].w))) + (unity_MatrixVP[3u].w);
                float _513 = asfloat(_246);
                float _514 = 1.0f / _513;
                float3 _521 = float3(-(_514 * (_352 * _347)), -(_514 * (_352 * _348)), -(_514 * (_352 * _349)));
                float _528 = _60(_521, float3(unity_WorldToObject[0u].x, unity_WorldToObject[0u].y, unity_WorldToObject[0u].z));
                float _535 = _60(_521, float3(unity_WorldToObject[1u].x, unity_WorldToObject[1u].y, unity_WorldToObject[1u].z));
                float _542 = _60(_521, float3(unity_WorldToObject[2u].x, unity_WorldToObject[2u].y, unity_WorldToObject[2u].z));
                float3 _543 = float3(_528, _535, _542);
                float _545 = rsqrt(_60(_543, _543));
                float _552 = ((_513 * (_513 * _513)) < 0.0f) ? (-1.0f) : 1.0f;
                TEXCOORD_6.x = (_545 * _528) * _552;
                TEXCOORD_6.y = (_545 * _535) * _552;
                TEXCOORD_6.z = (_545 * _542) * _552;
                COLOR.x = asfloat(_242.x);
                COLOR.y = asfloat(_242.y);
                COLOR.z = asfloat(_242.z);
                COLOR.w = bakedTexture.SampleLevel(samplerbakedTexture, float2((asfloat(_195.y) > 0.5f) ? (mad(-frac(_361 * 128.0f), 0.0078125f, _361) + 0.00390625f) : _361, asfloat(_195.x)), 0.0f).w;
                TEXCOORD.x = _390;
                TEXCOORD.y = _391 * 0.5f;
                INDEX.x = 0u;
                INDEX.y = _97;
            }

            SPIRV_Cross_Output main(SPIRV_Cross_Input stage_input)
            {
                gl_VertexIndex = int(stage_input.gl_VertexIndex);
                gl_InstanceIndex = int(stage_input.gl_InstanceIndex);
                gl_BaseVertexARB = 0;
                gl_BaseInstanceARB = 0;
                vert_main();
                SPIRV_Cross_Output stage_output;
                stage_output.gl_Position = gl_Position;
                stage_output.TEXCOORD = TEXCOORD;
                stage_output.COLOR = COLOR;
                stage_output.TEXCOORD_6 = TEXCOORD_6;
                stage_output.INDEX = INDEX;
                return stage_output;
            }

            #endif

            // Stage: Fragment
            #ifdef SHADER_STAGE_FRAGMENT
            // Stage: Fragment, Blob: 3, ParamBlob: 2, Language: hlsl
            // Generated names (not authored, no CPU-side property matches them):  <Buffer>_Unmapped = byte range no symbol describes;
            cbuffer type_Globals : register(b0)
            {
                float4 _Globals_Unmapped[4u] : packoffset(c0);
                float2 _GlobalMipBias : packoffset(c4);
            };

            Texture2D<float4> mainTexture : register(t0);
            SamplerState samplermainTexture : register(s0);

            static float2 TEXCOORD;
            static float4 COLOR;
            static float4 SV_Target;

            struct SPIRV_Cross_Input
            {
                float2 TEXCOORD : TEXCOORD4;
                nointerpolation float4 COLOR : TEXCOORD8;
            };

            struct SPIRV_Cross_Output
            {
                float4 SV_Target : SV_Target0;
            };

            void frag_main()
            {
                float4 _44 = mainTexture.SampleBias(samplermainTexture, float2(TEXCOORD.x, TEXCOORD.y), asfloat(asuint(_GlobalMipBias.x)));
                SV_Target.w = clamp(_44.w * COLOR.w, 0.0f, 1.0f);
                SV_Target.x = _44.x * COLOR.x;
                SV_Target.y = _44.y * COLOR.y;
                SV_Target.z = _44.z * COLOR.z;
            }

            SPIRV_Cross_Output main(SPIRV_Cross_Input stage_input)
            {
                TEXCOORD = stage_input.TEXCOORD;
                COLOR = stage_input.COLOR;
                frag_main();
                SPIRV_Cross_Output stage_output;
                stage_output.SV_Target = SV_Target;
                return stage_output;
            }

            #endif

            ENDHLSL
        }
    }
}
