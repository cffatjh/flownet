# JurisFlow Proje Özellikleri ve Detaylı Dokümantasyon

Bu belge, JurisFlow hukuk bürosu yönetim sisteminin (Legal Practice Management Software) tüm modüllerini, teknik özelliklerini ve kullanıcı yeteneklerini detaylı bir şekilde açıklamaktadır.

---

## 1. Dashboard (Genel Bakış)
Kullanıcının sisteme giriş yaptığında karşılaştığı ana ekrandır. Büronun genel durumunu özetler.

*   **Günlük Özet:**
    *   Günün randevuları ve yaklaşan etkinlikler.
    *   Öncelikli "Yapılacaklar" (To-Do) listesi.
    *   Kritik bildirimler (vadesi gelen faturalar, onay bekleyen belgeler).
*   **Finansal Hızlı Bakış:**
    *   O ayki faturalandırılabilir saatler.
    *   Toplam tahsilat durumu.
*   **Performans Grafikleri:**
    *   Avukat bazlı performans grafikleri (Admin yetkisiyle).
    *   Dava yoğunluk haritası.

## 2. Matters (Dava ve Dosya Yönetimi)
Hukuk bürosunun temel iş birimidir. Her bir dava veya hukuki danışmanlık işi bir "Matter" olarak takip edilir.

*   **Dava Listesi & Filtreleme:**
    *   **Görünüm:** Tüm davaların listesi (Durum, Sorumlu Avukat, Açılış Tarihi).
    *   **Filtreler:** Aktif, Kapanmış, Beklemede, Arşivlenmiş davalar.
*   **Matter Detay Görünümü:**
    *   **Genel Bilgiler:** Dava adı, müvekkil, karşı taraf, mahkeme bilgileri, dava numarası.
    *   **Durum Takibi:** `Open` (Açık), `Pending` (Beklemede), `Closed` (Kapalı), `Archived` (Arşivlenmiş).
    *   **Finansal Yapı:**
        *   **Ücret Modelleri:** Saatlik (Hourly), Sabit Ücret (Flat Fee), Başarı Primi (Contingency), Karma (Hybrid).
        *   **Emanet (Trust) Bakiyesi:** Müvekkilin hesabındaki mevcut bakiye ve minimum eşik uyarıları.
    *   **İlişkili Veriler:**
        *   **Zaman Çizelgesi (Timeline):** Davadaki tüm aktivitelerin kronolojik akışı.
        *   **Görevler:** Bu davaya atanmış spesifik görevler.
        *   **Belgeler:** Davaya özgü dosya ve evraklar.
        *   **Faturalar:** Kesilmiş ve ödenmemiş faturalar.
*   **Özellikler:**
    *   **Hızlı Ekleme:** Yeni dava oluşturma sihirbazı (Müvekkil seçimi, Pratik alanı - Aile, Ceza, Ticaret vb.).
    *   **Çatışma Kontrolü (Conflict Check):** Yeni dava açılırken karşı taraf ve ilgili kişilerin sistemde taranarak menfaat çatışmasının önlenmesi.

## 3. CRM (Müvekkil İlişkileri Yönetimi)
Mevcut müvekkillerin ve potansiyel müşteri adaylarının (Leads) yönetildiği modüldür.

*   **Pipeline (Satış Hunisi) Görünümü:**
    *   Potansiyel işlerin aşamaları: `New Inquiry` (Yeni Talep) -> `Contacted` (İletişime Geçildi) -> `Scheduled` (Randevu Ayarlandı) -> `No Show` (Gelmendi) -> `Retained` (Anlaşıldı/Müvekkil Oldu).
    *   Her aşamadaki tahmini gelir (Estimated Value) takibi.
    *   Sürükle-bırak ile aşama değiştirme (Kanban benzeri yapı).
*   **Müvekkil Yönetimi:**
    *   **Müvekkil Kartı:** Kimlik bilgileri, iletişim kanalları, fatura tercihleri.
    *   **Portal Erişimi:** Müvekkile özel "Client Portal" erişimi verme/kaldırma.
*   **Lead (Aday) Yönetimi:**
    *   **Kaynak Takibi:** Referans, Web Sitesi, Reklam vb. kaynak analizi.
    *   **Dönüşüm:** Tek tıkla "Lead"i "Client"a ve ilgili "Matter"a dönüştürme.

## 4. Tasks (Görev Yönetimi)
Büro içi iş akışının ve görev dağılımının yapıldığı yerdir.

*   **Görünüm Modları:**
    *   **Kanban Board:** `To Do`, `In Progress`, `Review`, `Done` sütunları ile görsel iş akışı.
    *   **Liste Görünümü:** Detaylı, sıralanabilir liste.
*   **Görev Özellikleri:**
    *   **Detaylar:** Başlık, Açıklama, Öncelik (High, Medium, Low).
    *   **Atama:** Sorumlu avukat veya personel atama (Initials ile gösterim).
    *   **Tarihler:** Başlangıç Tarihi ve Son Teslim Tarihi (Due Date).
    *   **İlişkilendirme:** Görevi belirli bir Dava (`Matter`) ile ilişkilendirme.
*   **Deadline (Son Tarih) Yönetimi:**
    *   **Akıllı Uyarılar:** Son tarihe 24 saat kala "Acil" (Amber), süresi geçince "Gecikmiş" (Kırmızı) uyarıları.
    *   **Geri Sayım:** Görev kartlarında görsel uyarı ikonları.
*   **Şablonlar (Templates):**
    *   Sık tekrarlanan işler için (örn. "Yeni Dava Açılış İşlemleri") görev şablonları oluşturma.
    *   Tek seferde şablondan çoklu görev üretme.
*   **Sonuçlandırma:**
    *   Görevi tamamlarken sonuç seçimi: `Success`, `Failed`, `Cancelled`.

## 5. Communications (İletişim Merkezi)
Büro içi ve dışı iletişimin merkezileştirildiği alandır.

*   **Entegrasyonlar:**
    *   **E-posta:** Outlook/Gmail entegrasyonu (Planlanan).
    *   **SMS:** Twilio entegrasyonu ile müvekkillere SMS gönderme/alma.
*   **Dahili Mesajlaşma:**
    *   Personel arası güvenli mesajlaşma.
    *   Dava veya görev bağlamında not bırakma.
*   **Secure Messages (Client Portal):**
    *   Müvekkillerle portal üzerinden şifreli, güvenli yazışma.

## 6. Video Calls (Görüntülü Görüşme)
Uzaktan görüşmeler için entegre çözüm.

*   **Özellikler:**
    *   Sistem üzerinden doğrudan video konferans başlatma.
    *   Görüşme linkini otomatik olarak e-posta/SMS ile müvekkile iletme.
    *   Görüşme süresini otomatik olarak "Time Tracker"a (Zaman Takibi) aktarma ve faturalandırma.

## 7. Documents (Belge Yönetimi)
Tüm hukuki belgelerin saklandığı, düzenlendiği ve versiyonlandığı modüldür.

*   **Dosya Yapısı:**
    *   **Konumlar:** "Dosyalarım" (Kişisel), "Dava Dosyaları" (Matter bazlı klasörleme).
    *   **Kategoriler:** Sözleşme, Dilekçe, Delil, Fatura, Yazışma vb. etiketleme.
    *   **Durumlar:** `Draft` (Taslak), `Final`, `Filed` (Sunuldu), `On Legal Hold` (Yasal Saklama).
*   **Özellikler:**
    *   **Yükleme:** Sürükle-bırak veya dosya seçici.
    *   **Önizleme:** PDF, Word (.docx), Metin (.txt) ve Resim dosyalarını tarayıcı içinde görüntüleme.
    *   **Google Docs Entegrasyonu:** Google Drive'daki belgeleri senkronize etme ve bağlama.
    *   **Arama:**
        *   **Metadata:** Dosya adı, etiket veya kategoriye göre.
        *   **İçerik Arama (OCR/Text):** Belge *içeriğinde* geçen metne göre arama yapabilme.
    *   **Toplu İşlemler:** Birden fazla belgeyi seçip tek seferde bir davaya atama veya silme.

## 8. Calendar (Takvim)
Duruşma, randevu ve son tarihlerin takip edildiği gelişmiş takvim.

*   **Görünüm:** Aylık, Haftalık, Günlük görünümler.
*   **Etkinlik Tipleri:**
    *   `Court` (Duruşma - Kırmızı renk kodu).
    *   `Meeting` (Toplantı).
    *   `Deadline` (Yasal Süre Sonu).
    *   `Deposition` (İfade Alma).
*   **Özellikler:**
    *   **Sürükle-Bırak:** Etkinlik günlerini kolayca değiştirme.
    *   **Tekrarlayan Etkinlikler:** Günlük, Haftalık, Aylık tekrar seçenekleri.
    *   **Hatırlatıcılar:** E-posta veya sistem içi bildirim (15 dk, 1 saat, 1 gün önce vb.).
    *   **Senkronizasyon:** Dava görevlerindeki "Due Date"ler otomatik olarak takvime düşer.

## 9. Billing (Faturalandırma ve Muhasebe)
Büronun gelir takibi ve faturalama işlemlerinin merkezi.

*   **Dashboard:**
    *   Toplam alacak, vadesi geçenler, ödenenler ve faturalandırılmamış iş (WIP) özeti.
*   **Fatura Oluşturma:**
    *   **Otomatik Derleme:** Seçilen davaya ait girilmiş zaman kayıtlarını (Time Entries) ve masrafları (Expenses) otomatik çeker.
    *   **Özelleştirme:** Vergi oranları, indirimler, notlar ekleme.
    *   **Formatlar:** Standart PDF fatura veya **LEDES 1998B** formatında e-fatura ihracı.
*   **Zaman ve Masraf Girişi:**
    *   **Süre Ölçer (Timer):** İşlem yaparken süreyi başlat/durdur.
    *   **Gider Kodları:** UTBMS (Uniform Task-Based Management System) kodları ile uyumlu masraf girişi (örn. E101 - Copying).
*   **Ödeme Takibi:**
    *   Fatura bazlı tahsilat kaydı.
    *   Parçalı ödeme desteği.
    *   Fatura durumları: `Draft`, `Sent`, `Paid`, `Overdue`, `Bad Debt`.

## 10. Trust / IOLTA (Emanet Hesap Yönetimi)
Müvekkil emanet paralarının (Retainer) yasalara uygun yönetimi.

*   **İşlemler:**
    *   **Deposit (Yatırma):** Müvekkilden alınan avans/masraf karşılığı.
    *   **Withdrawal (Çekme):** Masraflar için harcama.
    *   **Transfer:** Fatura ödemesi için emanetten işletme hesabına virman.
*   **Kurallar:**
    *   Emanet bakiyesinin negatife düşmesini engelleyen sistem kontrolleri.
    *   Her işlem için zorunlu açıklama ve tarih kaydı.

## 11. Employees (Personel Yönetimi)
Büro çalışanlarının ve yetkilerinin yönetimi.

*   **Roller:**
    *   `Partner`, `Associate`, `Of Counsel` (Avukatlar).
    *   `Paralegal`, `Legal Secretary`, `Legal Assistant` (Destek Personeli).
    *   `Office Manager`, `Accountant`, `Receptionist` (İdari).
*   **Personel Kartı:**
    *   **Kimlik:** Fotoğraf, İsim, İletişim, Acil Durum Kişisi.
    *   **Finansal:** Saatlik ücret (Hourly Rate) ve Maaş bilgisi.
    *   **Baro Bilgileri:** Avukatlar için Baro Numarası, Kayıtlı Olduğu Eyalet, Kabul Tarihi ve Lisans Durumu (`Active`, `Suspended` vb.).
*   **Yönetim:**
    *   Şifre sıfırlama.
    *   Yetki seviyesi belirleme.
    *   Aktif/İzinli/İşten Ayrılmış durum takibi.

## 12. Reports (Raporlama)
Büro performansını analiz eden detaylı raporlar.

*   **Performans Raporları:**
    *   **Attorney Performance:** Avukat bazında faturalandırılan saat ve gelir.
    *   **Realization Rate:** Çalışılan saatin ne kadarının faturaya, ne kadarının tahsilata dönüştüğü.
*   **Finansal Raporlar:**
    *   **A/R Aging:** Yaşlandırma raporu (Gecikmiş alacakların analizi: 30-60-90+ gün).
    *   **Client Profitability:** Hangi müvekkilin en çok kazandırdığı analizi.
*   **Dava Raporları:**
    *   Pratik alanına göre dava dağılımı.
    *   Dava kapanma süreleri.
*   **İhracat:** Raporları CSV veya PDF olarak dışa aktarma.

## 13. AI Legal Associate ("Juris")
Sisteme entegre Generatif AI asistanı. Hukuki süreçleri hızlandırır.

*   **Yetenekleri:**
    *   **Belge Hazırlama (Drafting):** Şablonlara dayalı hukuki dilekçe ve sözleşme taslağı oluşturma.
    *   **Özetleme (Summarization):** Yüklü belgeleri veya dava dosyalarını okuyup özet çıkarma.
    *   **Araştırma (Research):** İnternet erişimi ile güncel içtihat ve mevzuat taraması yapma.
    *   **Dava Analizi:** Davanın güçlü ve zayıf yönlerini analiz etme, emsal kararlara göre sonuç tahmini (Prediction).
    *   **Görev Önerisi:** Davanın türüne göre yapılması gereken görevleri otomatik listeleme.
*   **Context (Bağlam):** Kenar çubuğundan seçilen belgeleri AI'ya bağlam olarak vererek o belgeler üzerinden soru sorma imkanı.

## 14. Settings (Ayarlar)
Sistem yapılandırması.

*   **Kullanıcı Ayarları:**
    *   Profil düzenleme, Biyografi.
    *   Dil (İngilizce/Türkçe) ve Para Birimi (USD/EUR/TRY/GBP) seçimi.
    *   Tema (Açık/Koyu/Sistem).
*   **Güvenlik:**
    *   Şifre değiştirme.
    *   **2FA (İki Faktörlü Doğrulama):** Aç/Kapa.
    *   Oturum zaman aşımı süresi ayarlama.
    *   Audit Log (Denetim İzleri) takibi (Kim ne zaman ne yaptı).
*   **Firma Ayarları (Admin):**
    *   Firma adı, iletişim bilgileri, Vergi No.
*   **Fatura Ayarları (Admin):**
    *   Varsayılan saatlik ücretler (Partner, Associate vb. için ayrı ayrı).
    *   Faturaleme artış dilimi (6dk, 15dk vb.).
    *   Yuvarlama kuralları.
    *   LEDES ve UTBMS kod zorunluluğu ayarları.
