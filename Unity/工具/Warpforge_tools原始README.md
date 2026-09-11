# Warpforge 解包工具集

Warpforge（战锤40K）Unity 游戏资源解包/解码工具。配套的游戏解包成果在 `D:\2\Warpforge_assets_full`（资源）和 `D:\2\Warpforge_code`（反编译代码）。

## 目录结构

```
Warpforge_tools/
├── py312/       # Python 3.12 嵌入式环境（含 UnityPy、texture2ddecoder、av、Pillow、fsb5）
├── scripts/     # 全部提取/解码/分析脚本
└── data/        # 枚举数据（可由脚本重新生成）
```

## 环境

用自带的 Python（无需安装任何东西）：

```bash
D:\2\Warpforge_tools\py312\python.exe <script.py>
```

注意：**不要用系统自带的 Python 3.14 跑这些脚本**（其 UnityPy 缺少纹理转换模块，导出会失败）。

## 脚本说明

### 提取（核心）
| 脚本 | 作用 |
|---|---|
| `extract_full.py` | 全量提取：所有资源包的贴图/音频/网格/字体/视频 + 全部对象 JSON 转储（按包分目录） |
| `fix_exports.py` | 补充导出：纹理→PNG、网格→OBJ、字体→TTF、视频→MP4、vorbis→OGG（用 `obj.read().image` 等 API，需 py312） |
| `fsb_audio.py` | **FSB5 音频解码器**（手写 IMA-ADPCM 解码 + FSB5 头解析 + vorbis 包提取） |
| `ogg_build.py` | 纯 Python OGG 构建器（页 CRC/分段/时长，从游戏自带 ogg 收割 vorbis setup 头） |
| `rebuild_vorbis.py` | 把 FSB5 里的 vorbis 包重建为完整 OGG |

### 分析（历史检查工具）
| 脚本 | 作用 |
|---|---|
| `check_extract.py` | 枚举全部 93 个资源文件的对象类型计数 → `data/enum_result.json` |
| `enum_names.py` | 枚举各包对象名称（SO 名、贴图/音频/网格名）→ `data/enum_names.json` |
| `analyze_enum.py` | 按包汇总关键内容（已被 `verify_extract.py` 取代） |
| `compare_names.py` | 名称级完整度对比（源对象名 vs 提取文件） |
| `classify.py` | 用 monoscripts 包把提取的 JSON 分类为 Sprite/Shader/MonoBehaviour 等 |
| `match_origin.py` | 追溯提取文件来自哪个包 |
| `sample_sos.py` | 查看各包 ScriptableObject 名称样例 |
| `validate_ima.py` | IMA 解码验证（波形统计） |
| `ab_test_ima.py` | 解码参数 A/B 测试 |
| `verify_extract.py` | **当前完整的提取完整度验证**（源计数 vs 提取计数，推荐用这个） |
| `classify_move.py` | **按内容分类整理**：把 `Warpforge_assets_full` 按用途分类移动到 `D:\解包整理`（卡牌/装饰品/UI/音频/场景/模型/游戏数据等 12 大类），移动清单写入 `解包整理\manifest.json` 可逆 |

## 使用流程（重新提取时）

```bash
# 1. 全量提取（JSON 转储 + 音频初步处理）
D:\2\Warpforge_tools\py312\python.exe extract_full.py

# 2. 补充导出（纹理 PNG / 网格 OBJ / 字体 / 视频 / vorbis→OGG）
D:\2\Warpforge_tools\py312\python.exe fix_exports.py

# 3. 验证完整度
D:\2\Warpforge_tools\py312\python.exe verify_extract.py
```

## 已知限制

- `OvertimeStart`（48kHz 立体声音乐）无匹配 setup 头，保留原始 vorbis 包
- 2 张 `Font Texture` 特殊格式贴图无法解码
- 3 个空网格（CandleFlame）仅有 JSON 数据转储
- ~0.4% 对象 typetree 解析失败（主要在主数据文件的 UI 组件）
