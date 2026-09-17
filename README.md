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

# tray app

artik `ZapretTray.exe` var. saat yaninda ufak bi z simgesi olarak duruyor. cift tiklayip aciyorsun, uac soracak, normal, winws.exe zaten yonetici istiyor.

sol tik panel aciyor, sag tik menu cikariyor. panelde kocaman bi start/stop dugmesi var. simge beyazsa acik, soluk griyse kapali, bakinca anliyorsun.

ne yapiyor:
- ac kapa. ayarlari `zapret_gorev.cmd` dosyasindan okuyor yani config hala orada duruyor. orayi degistirirsen uygulama da ona gore calisir, iki yerde ayni seyi tutmuyorsun.
- windows acilisinda calis anahtari. `hizmet_kur.bat` ile ayni isi yapiyor, gorev olusturuyor.
- tepside otomatik ac anahtari. bunu acarsan her acilista uac sormadan geliyor.
- winws.exe cokerse kendi geri baslatiyor. 2 dakikada 3 defa denedi hala olmuyorsa birakiyor ve haber veriyor, sonsuz donguye girmiyor.
- dns bekcisi. public dns var mi, dns sifreli mi diye bakiyor. sadece ayara bakmiyor, gercekten doh calisiyor mu diye sorgu atiyor. cunku doh engellenirse windows sessizce duz metne dusuyor ve kayit defterinde hicbir sey degismiyor. o duruma dusersen uyariyor. zapret'in orada bi faydasi olmuyor zaten, ip'yi zaten yanlis aliyorsun.
- onar dugmesi. firewall kurali, defender istisnasi, dns cache, dosya engeli. hepsini arka arkaya yapiyor.
- test dugmesi. roblox, proton, discord ve kontrol icin cloudflare'a baglanip tls el sikismasini olcuyor. baglanti resetleniyorsa BLOCKED, 3 saniyeden uzun suruyorsa THROTTLED diyor. cloudflare da patlarsa sorun sansur degil internetin diye soyluyor. tahmin etmek yerine bakmis oluyorsun.

cikinca zapret de duruyor, tekrar acinca geri geliyor. yani simge tam anlamiyla ac kapa dugmesi.

log da panelin icinde gorunuyor, notepad acmaya gerek yok.

derlemek istersen `tray/build.cmd`. windows'un kendi icindeki csc.exe ile derleniyor, sdk falan kurmana gerek yok. tek dosya cikiyor.

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
