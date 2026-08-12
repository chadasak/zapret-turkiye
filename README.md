# Zapret Turkiye Surumu

Bu paket, Zapret'in TR icin ayarlanmis hali.
Roblox ve Discord genelinde test edildi, ISP'lerin getirdigi Proton VPN engelini de udp wireguard seviyesinde asiyor. simdilik wireguard'in engelini asamiyorum.

Not: Dns ayari eksikse Turkiye'de calismaz. Bu proje DNS farki olmadan hedef sunuculara yonelik cozumleri dogru calistiramaz. Bu yuzden aktif baglantiniza en azindan bir public DNS ekleyin. Benim tavsiyem 1.1.1.1 (yedek 1.0.0.1).

Kisa ozet
- Wi-Fi / ethernet ayarlarindan DNS'i ekleyin: `1.1.1.1` (veya yedek `1.0.0.1`)
- Yonetici olarak ac.
- `hizmet_kur.bat` ile arkada sabitle, ya da `zapret_bypass.bat` ile manuel calistir.
- Sorun cikarsa `kurulum.log` dosyasina bak.

Sadece Tcp trafigini degil, udp trafigine de etki ederek proton vpn kullanmaniza izin verir. Proton vpn simdilik sadece udp wireguard kisminda calisiyor.


# Kurulum
- hizmeti kurmadan once dns ayarlamayi unutmayin; DNS eklenmezse Turkiye'de calismayabilir. Onerilen deger: `1.1.1.1`.
- DNS eklemek icin ornek komut: `netsh interface ipv4 set dns name="Wi-Fi" static 1.1.1.1 primary`
- dns over https ayari extra guvenlik sagliyabilir.
- tek seferlik kullanimlar icin `zapret_bypass.bat` dosyasini yonetici olarak acmaniz yeterli.
- arkaplanda ve pcyi her actiginizda calismasini istiyorsaniz `hizmet_kur.bat` dosyasini yonetici olarak acin. Sizin yerinize firewall ve defender ayarlarini yapip windows hizmeti olarak arkaplanda calistiracak.
- hizmeti kaldirmak icin `hizmet_kaldir.bat` dosyasini yonetici olarak acin. hem windows hizmetlerini kaldiracak hem de firewall ve defender ayarlarini eski haline cevirecektir.
- kurulum esasinda herhangi bir problemde log dosyasini kontrol etmeyi unutmayin

# Update
Zapret update aldiginda tek yapacaginiz `bin` klasorundekileri degistirmek olacak. Sonrasinda tekrardan `hizmet_kur.bat` dosyasini yonetici olarak calistirin. Zaten bu dosya zapret servisini kaldiriyor, dns cache temizliyor ve sifirdan tekrardan bi servis olusturuyor. Tekrardan hizmet kaldir -> hizmet kur yapmaniza gerek yok.
not: zapret artik sadece bugfix guncellemeleri alacak.

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
```
