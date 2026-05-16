@echo off
cd /d "%~dp0"
"%~dp0bin\winws.exe" --wf-tcp=80,443 --dpi-desync=fake,split2 --dpi-desync-autottl=2 --dpi-desync-fooling=md5sig --dpi-desync-split-pos=sniext+4 --dpi-desync-repeats=2 --new --wf-udp=51820 --dpi-desync=fake --dpi-desync-repeats=2 --dpi-desync-any-protocol --dpi-desync-cutoff=d2 --dpi-desync-autottl=2

