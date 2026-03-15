# Zapret: SuperOnline DPI Bypass (Hybrid Mode)

SuperOnline ISP'nin uyguladığı agresif DPI (Derin Paket İnceleme) sansürünü aşmak için geliştirilmiş, oyun sunucularını (Roblox) ve katı güncelleyicileri (Discord) bozmadan çalışan özel **Zapret** yapılandırması.

---

## Türkçe

###  Neden Bu Proje? (Sorun ve Çözüm)

Standart DPI aşma araçları (örn. GoodbyeDPI) veya temel Zapret yapılandırmaları, paketlerin içine "sahte (fake)" veri basarak veya paket sırasını bozarak çalışır. Bu durum standart web sitelerinde işe yarasa da, katı TLS/SSL kurallarına sahip sunucularda (Roblox WinINet altyapısı veya Discord Rust güncelleyicisi) Man-in-the-Middle (MitM) saldırısı olarak algılanır ve **`SslConnectFail`** veya **`10054 Connection Reset`** hatalarına yol açar.

Bu proje **Hibrit (Çoklu Profil)** bir mimari kullanır:
1. **Narin Profil:** Sadece belirlenen oyun/uygulama sunucularına saf TCP parçalaması (`split2`) uygular. Paket sırası bozulmaz, sahte veri eklenmez. Oyunlar sıfır hatayla açılır.
2. **Genel Trafik:** Geri kalan tüm ağ trafiğine sahte paket ve `autottl` enjekte ederek SuperOnline DPI cihazını kör eder. 

###  Gereksinimler

- Windows 10 veya Windows 11 (64-bit)
- Administrator (Yönetici) yetkisi
- WinDivert sürücüsü (Script tarafından otomatik yönetilir)

###  Kurulum ve Kullanım

#### Seçenek 1: Arka Plan Servisi Olarak Kurma (Tavsiye Edilen)
Bu yöntem ile CMD ekranı görmezsiniz, sistem her açıldığında hayalet modda (sıfır pencere) otomatik çalışır.

1. Dosyaları indirin ve klasöre çıkartın.
2. `hizmet_kur.bat` dosyasına sağ tıklayıp **"Yönetici olarak çalıştır"** deyin.
3. Başarı mesajını gördükten sonra işlem tamamdır. (Kaldırmak için `hizmet_kaldir.bat` kullanabilirsiniz).

#### Seçenek 2: Manuel Kullanım
Sadece ihtiyacınız olduğunda açıp kapatmak isterseniz:
1. `zapret_bypass.bat` dosyasına sağ tıklayıp **"Yönetici olarak çalıştır"** deyin.
2. CMD penceresini açık tuttuğunuz sürece bypass aktif olacaktır.

### � Sorun Giderme (Troubleshooting)

**Kurulum başarısız mı oldu? Hata alınıyor mu?**

Log dosyasını kontrol edin: **`kurulum.log`**

Bu dosya Windows klasöründe Zapret dosyalarıyla aynı konumda bulunur. İçinde:
- ✅ Başarılı adımlar
- ❌ Hata mesajları ve hata kodları  
- 🕐 Zaman damgaları (ne zaman oldu)

**Log dosyası nasıl açılır?**
1. Windows Dosya Gezgini'nde Zapret klasörüne gidin
2. `kurulum.log` dosyasına sağ tıklayın → "Şununla aç" → "Not Defteri" seçin
3. Son satırlara bakıp ne yanlış gittiğini görün

**Yaygın Sorunlar:**
| Sorun | Çözüm |
|-------|-------|
| "Yönetici olarak çalıştır" hatası | `.bat` dosyasına sağ tıklayıp "Yönetici olarak çalıştır" seçin |
| "winws.exe bulunamadi" | Tüm dosyaların aynı klasörde olduğunu kontrol edin |
| "domain_listesi.txt bulunamadi" | İsmi yanlış yazıldı mı kontrol edin |
| SuperOnline hala bloke ediyor | `domain_listesi.txt` dosyasını kontrol edin - doğru domain listesi var mı? |
| PC yeniden başladıktan sonra çalışmıyor | `hizmet_kur.bat` başarıyla çalıştı mı kontrol edin (`kurulum.log` içinde check) |

### 📁 Dosya Yapısı ve İstisna Listesi

| Dosya | Açıklama |
|-------|----------|
| `zapret_bypass.bat` | Manuel çalıştırma için ana bypass scripti. |
| `hizmet_kur.bat` | Scripti Windows Görev Zamanlayıcıya görünmez servis olarak ekler. |
| `hizmet_kaldir.bat` | Kurulan arka plan servisini sistemden temizler. |
| `zapret_sessiz.vbs` | CMD penceresini gizleyen köprü VBScript dosyası. |
| `domain_listesi.txt` | **ÖNEMLİ:** Sahte paket (fake) gönderilmeyecek, narin davranılacak domainlerin listesi |
| `kurulum.log` | Tüm kurulum ve servisi çalıştırma olaylarının kaydı (Sorun giderme için önemli!) |

---

## English

###  Why This Project? (The Problem & Solution)

Standard DPI bypass tools (like GoodbyeDPI) or basic Zapret configurations work by injecting "fake" data into packets or scrambling packet order (Out-of-Order). While this works for standard web browsing, strict servers with rigid TLS/SSL handshakes (like Roblox's WinINet infrastructure or Discord's updater) detect this as packet corruption or a Man-in-the-Middle (MitM) attack, resulting in **`SslConnectFail`** or **`10054 Connection Reset`** errors.

This project utilizes a **Hybrid (Multi-Profile) Architecture**:
1. **Exception List (Delicate Profile):** Applies only pure TCP segmentation (`split2`) to specified game/app servers. No fake data is injected, and packet sequence is preserved. Games launch flawlessly.
2. **General Traffic (Heavy Profile):** Injects fake packets with dynamically calculated `autottl` to blind the SuperOnline DPI infrastructure for all other web traffic.

###  Requirements

- Windows 10 or Windows 11 (64-bit)
- Administrator privileges
- WinDivert driver (Managed automatically by the scripts)

###  Installation & Usage

#### Option 1: Install as Background Service (Recommended)
This method runs the bypass silently in the background (zero window) every time your PC boots.

1. Download and extract the repository.
2. Right-click `hizmet_kur.bat` (install_service) and select **"Run as administrator"**.
3. Once you see the success message, you are good to go. (Use `hizmet_kaldir.bat` to uninstall).

#### Option 2: Manual Execution
If you prefer to run it only when needed:
1. Right-click `zapret_bypass.bat` and select **"Run as administrator"**.
2. The bypass will remain active as long as the CMD window is open.

### 📁 File Structure & Exception List

| File | Description |
|------|-------------|
| `zapret_bypass.bat` | The main script for manual execution. |
| `hizmet_kur.bat` | Registers the bypass as a hidden scheduled Windows task. |
| `hizmet_kaldir.bat` | Removes the background service from the system. |
| `zapret_sessiz.vbs` | A VBS bridge script used to completely hide the CMD window. |
| `domain_listesi.txt` | **CRITICAL:** The list of domains that require the delicate bypass |

###  License & Disclaimer

- The custom scripts in this repository are licensed under the **MIT License**.
- The original Zapret software is distributed under its own license.
- **Disclaimer:** This software is provided for educational and diagnostic purposes only. The developer assumes no liability for any misuse or actions taken by the user.