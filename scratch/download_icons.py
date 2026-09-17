import os
import json
import urllib.parse
import requests

# Direct clean icon URLs using Google Favicon API (high-res 128px) or direct Wikimedia CDN
APP_DOMAINS = {
    "microsoft-edge": "https://www.google.com/s2/favicons?domain=microsoft.com&sz=128",
    "google-chrome": "https://www.google.com/s2/favicons?domain=google.com/chrome&sz=128",
    "brave": "https://www.google.com/s2/favicons?domain=brave.com&sz=128",
    "tor-browser": "https://www.google.com/s2/favicons?domain=torproject.org&sz=128",
    "discord": "https://www.google.com/s2/favicons?domain=discord.com&sz=128",
    "zoom": "https://www.google.com/s2/favicons?domain=zoom.us&sz=128",
    "telegram": "https://www.google.com/s2/favicons?domain=telegram.org&sz=128",
    "zalo": "https://www.google.com/s2/favicons?domain=zalo.me&sz=128",
    "revoltg": "https://www.google.com/s2/favicons?domain=revolt.chat&sz=128",
    "chatgpt-desktop": "https://www.google.com/s2/favicons?domain=openai.com&sz=128",
    "codex": "https://www.google.com/s2/favicons?domain=openai.com&sz=128",
    "cursor": "https://www.google.com/s2/favicons?domain=cursor.com&sz=128",
    "git": "https://www.google.com/s2/favicons?domain=git-scm.com&sz=128",
    "github-desktop": "https://www.google.com/s2/favicons?domain=github.com&sz=128",
    "nodejs": "https://www.google.com/s2/favicons?domain=nodejs.org&sz=128",
    "python3": "https://www.google.com/s2/favicons?domain=python.org&sz=128",
    "python-3128": "https://www.google.com/s2/favicons?domain=python.org&sz=128",
    "vscode": "https://www.google.com/s2/favicons?domain=code.visualstudio.com&sz=128",
    "antigravity": "https://www.google.com/s2/favicons?domain=google.com&sz=128",
    "notepadplusplus": "https://www.google.com/s2/favicons?domain=notepad-plus-plus.org&sz=128",
    "office365": "https://www.google.com/s2/favicons?domain=office.com&sz=128",
    "foxit-reader": "https://www.google.com/s2/favicons?domain=foxit.com&sz=128",
    "goodnotes": "https://www.google.com/s2/favicons?domain=goodnotes.com&sz=128",
    "steam": "https://www.google.com/s2/favicons?domain=steampowered.com&sz=128",
    "genshin-impact": "https://www.google.com/s2/favicons?domain=hoyoverse.com&sz=128",
    "roblox": "https://www.google.com/s2/favicons?domain=roblox.com&sz=128",
    "xmcl": "https://www.google.com/s2/favicons?domain=xmcl.app&sz=128",
    "geforce-now": "https://www.google.com/s2/favicons?domain=nvidia.com&sz=128",
    "riot-client": "https://www.google.com/s2/favicons?domain=riotgames.com&sz=128",
    "autoruns": "https://www.google.com/s2/favicons?domain=sysinternals.com&sz=128",
    "7zip": "https://www.google.com/s2/favicons?domain=7-zip.org&sz=128",
    "anydesk": "https://www.google.com/s2/favicons?domain=anydesk.com&sz=128",
    "cloudflare-warp": "https://www.google.com/s2/favicons?domain=1.1.1.1&sz=128",
    "idm": "https://www.google.com/s2/favicons?domain=internetdownloadmanager.com&sz=128",
    "rufus": "https://www.google.com/s2/favicons?domain=rufus.ie&sz=128",
    "winrar": "https://www.google.com/s2/favicons?domain=win-rar.com&sz=128",
    "tailscale": "https://www.google.com/s2/favicons?domain=tailscale.com&sz=128",
    "xdm": "https://www.google.com/s2/favicons?domain=github.com&sz=128",
    "bcuninstaller": "https://www.google.com/s2/favicons?domain=bcuninstaller.com&sz=128",
    "memreduct": "https://www.google.com/s2/favicons?domain=henrypp.org&sz=128",
    "windirstat": "https://www.google.com/s2/favicons?domain=windirstat.net&sz=128",
    "windhawk": "https://www.google.com/s2/favicons?domain=windhawk.net&sz=128",
    "nilesoft-shell": "https://www.google.com/s2/favicons?domain=nilesoft.org&sz=128",
    "fxsound": "https://www.google.com/s2/favicons?domain=fxsound.com&sz=128",
    "ventoy": "https://www.google.com/s2/favicons?domain=ventoy.net&sz=128",
    "deskin": "https://www.google.com/s2/favicons?domain=deskin.io&sz=128",
    "ultraviewer": "https://www.google.com/s2/favicons?domain=ultraviewer.net&sz=128",
    "evkey": "https://www.google.com/s2/favicons?domain=evkeyvn.com&sz=128",
    "minitool-partition": "https://www.google.com/s2/favicons?domain=partitionwizard.com&sz=128",
    "obs-studio": "https://www.google.com/s2/favicons?domain=obsproject.com&sz=128",
    "vmware": "https://www.google.com/s2/favicons?domain=vmware.com&sz=128",
    "xp-pen": "https://www.google.com/s2/favicons?domain=xp-pen.com&sz=128",
    "capcut": "https://www.google.com/s2/favicons?domain=capcut.com&sz=128",
    "capcut-1.5": "https://www.google.com/s2/favicons?domain=capcut.com&sz=128",
    "capcut-7.0": "https://www.google.com/s2/favicons?domain=capcut.com&sz=128",
    "capcut-7.7": "https://www.google.com/s2/favicons?domain=capcut.com&sz=128",
    "yindiao-g17-drive": "https://www.google.com/s2/favicons?domain=yindiao.com&sz=128",
    "inphic-drive": "https://www.google.com/s2/favicons?domain=inphic.cn&sz=128",
    "aula-f87-drive": "https://www.google.com/s2/favicons?domain=aulacn.com&sz=128"
}

def download_all():
    output_dir = os.path.join(os.getcwd(), "src", "WinMaster", "Assets", "Icons")
    os.makedirs(output_dir, exist_ok=True)

    headers = {
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
    }

    print(f"Downloading 59 app icons via Google Favicon API...")
    success = 0

    for app_id, url in APP_DOMAINS.items():
        filepath = os.path.join(output_dir, f"{app_id}.png")
        try:
            resp = requests.get(url, headers=headers, timeout=10)
            if resp.status_code == 200 and len(resp.content) > 100:
                with open(filepath, "wb") as f:
                    f.write(resp.content)
                success += 1
                print(f"[OK] {app_id}.png ({len(resp.content)} bytes)")
            else:
                print(f"[FAIL] {app_id}")
        except Exception as e:
            print(f"[ERR] {app_id}: {e}")

    print(f"\nCompleted: {success}/{len(APP_DOMAINS)} icons downloaded.")

if __name__ == "__main__":
    download_all()
