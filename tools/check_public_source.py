#!/usr/bin/env python3
"""Check tracked public files; this is a release boundary check, not a license audit."""
from pathlib import Path
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
EXACT = {
    '.gitignore', 'LICENSE', 'README.md', 'README.en.md', 'THIRD_PARTY.md',
    'CONTRIBUTING.md', '.github/workflows/public-source.yml',
    'Packages/manifest.json', 'Packages/packages-lock.json',
    'ProjectSettings/ProjectVersion.txt', 'ProjectSettings/TagManager.asset',
    'ProjectSettings/DynamicsManager.asset', 'ProjectSettings/TimeManager.asset',
    'Assets/ZombieSurvival/Settings/SurvivalConfig.asset',
    *{'Assets/ZombieSurvival/Resources/DeadDistrict/' + n + '.asset'
      for n in ('CommercialStreet', 'QuarantineCamp', 'FreightDepot')},
}
SOURCE_DIRS = ('Assets/ZombieSurvival/Runtime/', 'Assets/ZombieSurvival/Editor/',
               'Assets/ZombieSurvival/Plugins/iOS/')
SENSITIVE = re.compile(
    r'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----|'
    r'gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{30,}|'
    r'AKIA[A-Z0-9]{16}|' + '/Users' + r'/[^/\s]+/|' + '/var' + '/folders/|'
    r'(?i:password|api[_-]?key|access[_-]?token)\s*[:=]\s*["\'][^"\'\n]{12,}["\']'
)

def allowed(name):
    if name.endswith('.meta'):
        target = ROOT / name[:-5]
        return target.exists() and (target.is_dir() or allowed(name[:-5]))
    return (name in EXACT
            or (name.startswith(SOURCE_DIRS) and Path(name).suffix in ('.cs', '.mm'))
            or (name.startswith('docs/') and Path(name).suffix in ('.md', '.txt'))
            or (name.startswith('docs/media/') and Path(name).suffix in ('.jpg', '.png'))
            or (name.startswith('tools/') and Path(name).suffix == '.py'))

result = subprocess.run(['git', 'ls-files', '-z'], cwd=ROOT, check=True, capture_output=True)
names = [n for n in result.stdout.decode().split('\0') if n]
issues = []
for name in names:
    f = ROOT / name
    if not allowed(name):
        issues.append(f'Not in public allowlist: {name}')
        continue
    if f.is_symlink():
        issues.append(f'Symlink not allowed: {name}')
        continue
    if f.stat().st_size > 8 * 1024 * 1024:
        issues.append(f'Oversized repository file: {name}')
    if name.startswith('docs/media/'):
        continue
    try:
        text = f.read_text()
    except UnicodeDecodeError:
        issues.append(f'Unexpected binary: {name}')
        continue
    if SENSITIVE.search(text):
        issues.append(f'Potential sensitive data: {name} (value suppressed)')
    if name.endswith(('.md', '.txt')):
        for link in re.findall(r'\]\(([^\s)]+)(?:\s+[^)]*)?\)', text):
            if ':' in link or link.startswith('#'):
                continue
            if not (f.parent / link.split('#')[0]).exists():
                issues.append(f'Broken local link in {name}: {link}')
if not names:
    issues.append('No tracked files; stage the reviewed files before checking.')
for issue in issues:
    print(issue)
print(f'{len(names)} tracked files checked; {len(issues)} issue(s).')
sys.exit(bool(issues))
