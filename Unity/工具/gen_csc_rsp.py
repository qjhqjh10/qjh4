# 生成 `csc` 用的两份响应文件（运行时 / 编辑器程序集）。本脚本**只写临时文件**、不产出数据；
# 加 `--dry` 仍会跑完所有扫描与读数，但**一个字节都不写**（把写口换成空接收器）。
import io, re, os, sys

# 🔴 `--dry` = 只跑计算、打印读数、**零写盘**。默认（不带它）行为与以前完全一样。
DRY = '--dry' in sys.argv[1:]


class _DrySink(object):
    """`--dry` 时的假文件：吃掉写入、不建文件。"""
    def __enter__(self):
        return self

    def __exit__(self, *a):
        return False

    def write(self, s):
        return len(s)


def _out(path):
    return _DrySink() if DRY else io.open(path, 'w', encoding='utf-8')

BS = chr(92)
MG = 'D:/4/Unity/MyGame'
src = io.open(MG + '/Assembly-CSharp.csproj', encoding='utf-8-sig').read()
compiles = re.findall(r'<Compile Include="([^"]+)" />', src)
refs = set(re.findall(r'<HintPath>([^<]+)</HintPath>', src))
refs = {r for r in refs if r.lower().endswith('.dll')}
refdir = 'C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7.1'
for f in ('mscorlib.dll', 'System.dll', 'System.Core.dll', 'System.Xml.dll',
          'System.Runtime.Serialization.dll', 'System.Data.dll', 'System.Numerics.dll'):
    p = refdir + '/' + f
    if os.path.exists(p):
        refs.add(p)
refs.add(MG + '/Assets/Plugins/Demigiant/DOTween/DOTween.dll')
refs.add(MG + '/Library/ScriptAssemblies/Assembly-CSharp-firstpass.dll')
defines = re.search(r'<DefineConstants>([^<]+)</DefineConstants>', src).group(1)
TMP = os.environ.get('TMPDIR', 'C:/Users/qjh36/AppData/Local/Temp')
out = TMP + '/wf_csc.rsp'
with _out(out) as f:
    f.write('-target:library' + chr(10) + '-nostdlib+' + chr(10) + '-noconfig' + chr(10))
    f.write('-langversion:9.0' + chr(10) + '-nowarn:0169,0649,0414,0219,0067' + chr(10))
    f.write('-define:' + defines + chr(10))
    f.write('-out:"' + TMP + '/wfcheck/WFCheck.dll"' + chr(10))
    for r in sorted(refs):
        f.write('-r:"' + r + '"' + chr(10))
    for c in compiles:
        f.write('"' + MG + '/' + c.replace(BS, '/') + '"' + chr(10))
    # ---- 新增文件：csproj 是**上次 Unity 生成**的，之后新建的 .cs 不在里面 ----
    #  2026-09-29 改成**自动扫**（原来是一串手写名单，每加一个文件就得改这里，
    #  漏了会报 CS0246「找不到类型」—— 看着像代码错，其实是源清单没跟上）。
    #  规则：`Assets/` 下所有 .cs，排除 `Editor/`（那是另一个程序集）与 `Plugins/`。
    have = {c.replace(BS, '/') for c in compiles}
    extra = []
    for root, dirs, files in os.walk(MG + '/Assets'):
        r = root.replace(BS, '/')
        if '/Editor' in r or '/Plugins' in r or '/Editor/' in r + '/':
            continue
        for fn in files:
            if not fn.endswith('.cs'):
                continue
            full = r + '/' + fn
            rel = 'Assets/' + full[len(MG + '/Assets/'):]
            if rel not in have:
                extra.append(full)
    for c in sorted(extra):
        f.write('"' + c + '"' + chr(10))
print('sources=', len(compiles) + len(extra), 'refs=', len(refs), 'extra=', len(extra))

# ---- 第 2 份：Editor 程序集（拿刚编出来的 WFCheck.dll 当 Assembly-CSharp 用）----
src2 = io.open(MG + '/Assembly-CSharp-Editor.csproj', encoding='utf-8-sig').read()
compiles2 = re.findall(r'<Compile Include="([^"]+)" />', src2)
refs2 = set(re.findall(r'<HintPath>([^<]+)</HintPath>', src2))
refs2 = {r for r in refs2 if r.lower().endswith('.dll')}
refs2 = {r for r in refs2 if 'Assembly-CSharp.dll' not in r}
refs2.add(TMP + '/wfcheck/WFCheck.dll')
refs2.add(MG + '/Assets/Plugins/Demigiant/DOTween/DOTween.dll')
defines2 = re.search(r'<DefineConstants>([^<]+)</DefineConstants>', src2).group(1)
with _out(TMP + '/wf_csc_editor.rsp') as f:
    f.write('-target:library' + chr(10) + '-nostdlib+' + chr(10) + '-noconfig' + chr(10))
    f.write('-langversion:9.0' + chr(10) + '-nowarn:0169,0649,0414,0219,0067' + chr(10))
    f.write('-define:' + defines2 + chr(10))
    f.write('-out:"' + TMP + '/wfcheck/WFCheckEditor.dll"' + chr(10))
    for r in sorted(refs2):
        f.write('-r:"' + r + '"' + chr(10))
    for c in compiles2:
        f.write('"' + MG + '/' + c.replace(BS, '/') + '"' + chr(10))
    # ---- 🔴 2026-09-30 补：**编辑器程序集也要自动扫新文件** ----
    #   原来只有第 1 份（运行时）会扫（见上面的说明），第 2 份**只认 csproj** ——
    #   而 csproj 是**上次 Unity 生成**的 ⇒ **新加的 `Editor/*.cs` 编不到**
    #   ⇒ 报 `CS0246 找不到类型 MeshVertexColors`，看着像代码错、其实是源清单没跟上
    #   （踩的实例：`MeshVertexColors.cs` 加了之后类型检查红、Unity 里却是好的）。
    #   规则：路径里含 `/Editor/` 的 `.cs`（编辑器程序集的惯例）且不在 csproj 里 ⇒ 补上。
    have2 = {c.replace(BS, '/') for c in compiles2}
    extra2 = []
    for root, dirs, files in os.walk(MG + '/Assets'):
        r = root.replace(BS, '/')
        if '/Editor/' not in r + '/':
            continue
        for fn in files:
            if not fn.endswith('.cs'):
                continue
            full = r + '/' + fn
            rel = 'Assets/' + full[len(MG + '/Assets/'):]
            if rel not in have2:
                extra2.append(full)
    for c in sorted(extra2):
        f.write('"' + c + '"' + chr(10))
print('editor sources=', len(compiles2), 'refs=', len(refs2), 'extra=', len(extra2))
if DRY:
    print('干跑（未写盘）：%s · %s；要落盘请去掉 --dry'
          % (out, TMP + '/wf_csc_editor.rsp'))
