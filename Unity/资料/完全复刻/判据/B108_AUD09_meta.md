# B108 · AUD09 `.meta` 导入参数普查（2026-10-31 · 第一百零八会话）

> 只读普查。范围 = `Unity/MyGame/Assets/**/*.meta` 里带 `TextureImporter:` 的文件（**4,621 个**）。
> 目的 = 补 AUD09 §五·2 的欠账：`sRGBTexture` / `alphaIsTransparency` / `textureCompression` / `maxTextureSize` 四类导入参数。
> 扫描脚本（临时、非仓库正式件）：`D:/4/_tmp_view/b108_meta_scan.py` · `b108_tex_cap_scan.py`。

## 一、现读分布（4,621 个贴图 meta）

| 参数 | 分布 |
|---|---|
| `sRGBTexture` | 1 = **4,467** · 0 = **150** · 缺字段 4（DOTween 插件图） |
| `alphaIsTransparency` | 0 = **4,207** · 1 = **414** |
| `textureCompression` | 2 = **3,629** · 1 = **954** · 0 = **34** · 缺字段 4 |
| `maxTextureSize` | 2048 = **4,589** · 4096 = **27** · 8192 = **1** · 其它 4（插件图） |
| `isReadable` | 0 = 4,358 · 1 = **263**（与 `B100_X11` 的卡框判据一致） |
| `enableMipMap` | 1 = 868 · 0 = 3,753 |

按顶层目录：`CardPresentation` 3,676 · `WarpforgeVFX` 623 · `WarpforgeArena1` 301 · `Resources` 14 · 插件/其它 7。

## 二、原判「36 张被 `maxTextureSize` 降采样」—— 🔴 **已推翻（2026-10-31 `B108_R7` 复核）**

**原判不成立**：本报告脚本读的是 `.meta` **顶层旧字段 `maxTextureSize`（恒 2048）**，而 Unity 实际生效的是 **`platformSettings` 块**（arena 的 `Standalone` = **4096**、部分 8192；`ArtBaker.cs:555-558/575` 也实证平台块会盖掉顶层）。按平台块重扫：**arena 282 → 0 降采样 · VFX 623 → 0** ⇒ **真正被降采样的 = 0 张**。
- 原列出的 `battlearena1/Textures/Battle Arena 1 Floor.png`（4096²）：`platformSettings.Standalone.maxTextureSize = 4096` ⇒ **不降采样**。
- 真正的资源侧结论改由 `判据/B108_AUD09_纹理格式.md`（R7 从 `D:/2/unity_run_ref/.../aa/StandaloneWindows64/*.bundle` 84 个原始包实读 Texture2D 头）承担：卡框 SDF 压缩档错（105 张，已派 `B108_A17`）· 战场 114/41/41 记账（战场线暂停）· Vat 12 张 .meta 与驱动要求矛盾但 0 引用。
- ⛔ **以后量导入尺寸必须读 `platformSettings` 的平台块，别读顶层 `maxTextureSize`**（同族坑：`ArtBaker` 自己的注释就记过「改完顶层，导入后仍是 2048」）。

## 三、判据缺口（如实记，⛔ 不拿猜测填空）

1. ✅ **`sRGBTexture` / `textureCompression` 的原版真值已由 `B108_R7` 解出**：`D:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/*.bundle`（84 个原始包，UnityPy 1.24.2）读 `m_TextureFormat` / `m_ColorSpace`；逐类结果 → `判据/B108_AUD09_纹理格式.md`。⚠️ `m_ColorSpace` = 0 Linear / 1 sRGB，**不是** `UnityEngine.ColorSpace`（Gamma=0/Linear=1）。
2. **`alphaIsTransparency`** 不随资产序列化（是导入设置），原版客户端里同样拿不到 ⇒ 只能按用途判（UI/带 alpha 的图 = 1，不透明图 = 0），**不是「原版值」**。
3. 现读分布里 `sRGB=1` 的 4,467 张、`alphaIsTransparency=1` 的 414 张**没有逐张核「该不该」**；本轮只给分布与降采样硬缺口。

## 四、结论与下一步

- ✅ **可结案的部分**：`maxTextureSize` 降采样 = 36 张，全部战场系，已按用户口径**挂到战场线恢复时**。
- ⏳ **仍欠**：`sRGB` / `compression` 的原版真值（需读 bundle Texture2D 头）；`alphaIsTransparency` 的用途级判定。
- 📌 建议下一波（查判据槽位空出时）：在 `D:/2` 资产容器里读一张 Texture2D 的 `m_TextureFormat` / `m_ColorSpace`，先证「这条路读得到」，再决定是否全量。
