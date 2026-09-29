#!/bin/bash
# 秒级 C# 类型检查（改完 .cs 先跑它、再跑 Unity 批处理）—— 2026-09-29 从临时目录搬进仓库
#
# 为什么快：Unity 一次批处理 1–9 分钟，**大半是编译**；这里直接用 Roslyn `csc`
# 拿 Unity 生成的 `.csproj` 当「源清单 + 引用清单」编两遍（运行时程序集 / 编辑器程序集），**几秒**。
# ⚠️ 它**只管编不编得过、不跑任何断言**（断言仍然走 `工具/_run_8_checks.sh` 那 11 条）。
# ⚠️ **批处理跑着的时候别改 `.cs`**（见 CLAUDE.md 铁律 12 / 已知的坑）。
#
# 用法：bash d:/4/Unity/工具/typecheck.sh
# 输出：两行「xxx错误数: N」，**0 = 过**；非 0 时上面会列错误行。
#
# 原理与三个前提（缺一个会报一堆假的 `CS0006 找不到元数据文件`）：
#  ① cwd 必须是 `Unity/MyGame`（csproj 的 HintPath 是相对路径）；
#  ② `dotnet msbuild` 走不通（DOTween 是针对 .NET 4.7.2 编的，本机只有 4.7.1 的引用程序集 ⇒ MSB3274），
#     而 `csc` 不做框架版本检查 ⇒ 直接 `dotnet <sdk>/Roslyn/bincore/csc.dll @rsp`；
#  ③ **新建的 `.cs` 不在 csproj 里**（那是上次 Unity 生成的）⇒ `gen_csc_rsp.py` 会**自动扫** `Assets/`
#     补进去（2026-09-29 改的：原来是一串手写的「新增文件」名单，每加一个文件就得改一次，
#     漏了就报 `CS0246 找不到类型` —— **看着像代码错，其实是源清单没跟上**）。
set -u
cd /d/4/Unity/MyGame || exit 1
TMP="${TMPDIR:-/tmp}"
PY="${PY:-D:/2/Warpforge_tools/py312/python.exe}"
CSC="/c/Program Files/dotnet/sdk/8.0.425/Roslyn/bincore/csc.dll"
"$PY" d:/4/Unity/工具/gen_csc_rsp.py >/dev/null || exit 1
R1="$(cygpath -w "$TMP/wf_csc.rsp")"
R2="$(cygpath -w "$TMP/wf_csc_editor.rsp")"
echo "--- 运行时程序集 ---"
dotnet "$CSC" "@$R1" 2>&1 | grep -E " error " | head -20
echo "运行时错误数: $(dotnet "$CSC" "@$R1" 2>&1 | grep -c ' error ')"
echo "--- 编辑器程序集 ---"
dotnet "$CSC" "@$R2" 2>&1 | grep -E " error " | head -20
echo "编辑器错误数: $(dotnet "$CSC" "@$R2" 2>&1 | grep -c ' error ')"
