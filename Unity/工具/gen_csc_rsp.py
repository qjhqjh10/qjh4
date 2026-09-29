import io, re, os
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
with io.open(out, 'w', encoding='utf-8') as f:
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
            rel = full[len(MG + '/Assets/'):]
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
with io.open(TMP + '/wf_csc_editor.rsp', 'w', encoding='utf-8') as f:
    f.write('-target:library' + chr(10) + '-nostdlib+' + chr(10) + '-noconfig' + chr(10))
    f.write('-langversion:9.0' + chr(10) + '-nowarn:0169,0649,0414,0219,0067' + chr(10))
    f.write('-define:' + defines2 + chr(10))
    f.write('-out:"' + TMP + '/wfcheck/WFCheckEditor.dll"' + chr(10))
    for r in sorted(refs2):
        f.write('-r:"' + r + '"' + chr(10))
    for c in compiles2:
        f.write('"' + MG + '/' + c.replace(BS, '/') + '"' + chr(10))
print('editor sources=', len(compiles2), 'refs=', len(refs2))
