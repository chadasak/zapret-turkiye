# Zapret Turkiye Surumu

Bu paket, Zapret'in TR icin ayarlanmis hali.
Roblox ve Discord genelinde test edildi, gayet isi goruyor.

Kisa ozet:
- Yonetici olarak ac.
- `hizmet_kur.bat` ile arkada sabitle, ya da `zapret_bypass.bat` ile manuel calistir.
- Sorun cikarsa `kurulum.log` dosyasina bak.

windows disinda kullanacaklar icin zapret config'i:
  --wf-tcp=80,443 ^
  --dpi-desync=fake,split2 ^
  --dpi-desync-autottl=2 ^
  --dpi-desync-fooling=md5sig ^
  --dpi-desync-split-pos=sniext+4 ^
  --dpi-desync-repeats=2 ^
  --new ^
  --wf-udp=51820 ^
  --dpi-desync=fake ^
  --dpi-desync-repeats=2 ^
  --dpi-desync-any-protocol ^
  --dpi-desync-cutoff=d2 ^
  --dpi-desync-autottl=2
