import json
r = json.load(open('/tmp/rel29.json'))
for a in r.get('assets', []):
    n = a['name']
    if 'runtime' in n.lower() and ('linux' in n.lower() or 'riscv' in n.lower()):
        print(a['browser_download_url'])
    elif 'kpu' in n and 'linux' in n.lower():
        print(a['browser_download_url'])
