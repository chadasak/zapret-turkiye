# Zapret Turkiye Surumu

Bu paket, Zapret'in TR icin ayarlanmis hali.
Roblox ve Discord genelinde test edildi, ISP'lerin getirdigi Proton VPN engelini asiyor.

Kisa ozet
- Yonetici olarak ac.
- `hizmet_kur.bat` ile arkada sabitle, ya da `zapret_bypass.bat` ile manuel calistir.
- Sorun cikarsa `kurulum.log` dosyasina bak.

Sadece Tcp trafigini degil, udp trafigine de etki ederek proton vpn kullanmaniza izin verir. Proton vpn simdilik sadece udp wireguard kisminda calisiyor.

# Kurulum
- tek seferlik kullanimlar icin `zapret_bypass.bat` dosyasini yonetici olarak acmaniz yeterli.
- arkaplanda ve pcyi her actiginizda calismasini istiyorsaniz `hizmet_kur.bat` dosyasini yonetici olarak acin. Sizin yerinize firewall ve defender ayarlarini yapip windows hizmeti olarak arkaplanda calistiracak.
- hizmeti kaldirmak icin `hizmet_kaldir.bat` dosyasini yonetici olarak acin. hem windows hizmetlerini kaldiracak hem de firewall ve defender ayarlarini eski haline cevirecektir.
- kurulum esasinda herhangi bir problemde log dosyasini kontrol etmeyi unutmayin

# Update
Zapret update aldiginda tek yapacaginiz `bin` klasorundekileri degistirmek olacak. Sonrasinda tekrardan `hizmet_kur.bat` dosyasini yonetici olarak calistirin. Zaten bu dosya zapret servisini kaldiriyor, dns cache temizliyor ve sifirdan tekrardan bi servis olusturuyor. Tekrardan hizmet kaldir -> hizmet kur yapmaniza gerek yok.

# VPN'ler
Su an kullandigim ISP proton vpn'i handshake sirasinda engelliyor. Bu zapret config'indeki udp kisimlari bu engeli ve sansuru asmak icin kullaniliyor. Proton VPN'in windows client'inde stealth mode bile calismazken bu zapret config'i ile `UDP WireGuard` ayari calismaya basladi.

# windows disinda kullanacaklar icin zapret config'i
```bash
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
