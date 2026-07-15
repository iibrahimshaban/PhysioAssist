# PhysioAssist — InitialReport Module (Backend Final)

## 1. طريقة النسخ
انسخي فولدر `PhysioAssist.Api` ده فوق فولدر مشروعك الحقيقي — كل مسار مطابق تمامًا
لمكانه (يعني `Modules/InitialReportModule/...` هيحل محل اللي عندك، و`Shared/...`
هيدمج/يستبدل الملفات المذكورة بس، من غير ما يمسح باقي حاجتك).

**ملفات جديدة بالكامل:**
- `Shared/Interfaces/IQRService.cs`
- `Shared/QRService/QRService.cs`
- `Shared/QRService/QrSettings.cs`
- `Shared/Interfaces/IPdfService.cs`
- `Shared/PdfService/PdfService.cs`
- `Shared/Dtos/Pdf/TreatmentPlanPdfRequest.cs`
- `Shared/Interfaces/INotificationService.cs`
- `Shared/NotificationService/NotificationService.cs`
- `Shared/Interfaces/IInitialReportQueryService.cs`
- كل ملفات `Modules/InitialReportModule/*`

**ملفات لازم تستبدليها بالكامل (موجودة عندك، اتعدلت):**
- `Shared/Helpers/EmailBodyBuilder.cs` → أضفنا method جديدة `ReportReady(...)`
- `Shared/Interfaces/IMediaStorageService.cs` → أضفنا method جديدة `UploadRawFileAsync(...)`
- `DependancyInjection.cs` (الرئيسي) → أضفنا تسجيل الخدمات الجديدة

---

## 2. NuGet Packages المطلوبة
```bash
dotnet add package QuestPDF
dotnet add package QRCoder
```

---

## 3. appsettings.json — ضيفي القسم ده
```json
"QrSettings": {
  "SecretKey": "حطي هنا مفتاح سري طويل وعشوائي (32+ حرف)",
  "DefaultExpiryDays": 365
}
```

---

## 4. ⚠️ خطوة لازم تكمليها بنفسك (الوحيدة المتبقية)

### `CloudinaryService.cs`
لازم تضيفي فيها implementation للـ method الجديدة `UploadRawFileAsync` (المستخدمة
لرفع الـ PDF بتاع خطة العلاج، لأن `UploadImageAsync` بتدعم صور بس).

الشكل العام المتوقع (بناءً على استخدام عادي لـ CloudinaryDotNet SDK):
```csharp
public async Task<string> UploadRawFileAsync(Stream fileStream, string folder, string publicId, string fileExtension)
{
    var uploadParams = new RawUploadParams
    {
        File = new FileDescription($"{publicId}.{fileExtension}", fileStream),
        Folder = folder,
        PublicId = publicId
    };

    var result = await _cloudinary.UploadAsync(uploadParams);
    return result.SecureUrl.ToString();
}
```
> ⚠️ السطر `_cloudinary` ده افتراض إن عندك field اسمه كده في الكلاس — لو الاسم مختلف
> (أو الـ constructor بتاعك شكله مختلف)، ابعتيلي محتوى `CloudinaryService.cs`
> الحالي وأظبطها بالظبط.

---

## 5. افتراضات لازم تتأكدي منها

| الافتراض | فين اتستخدم | لو غلط |
|---|---|---|
| `DoctorId == ApplicationUser.Id` | `InitialReportController.Create`, `InitialReportService.SubmitAsync` | ابعتي شكل العلاقة الحقيقية بين `Doctor` و`ApplicationUser` |
| Patient lookup مباشر عبر `ApplicationDbContext` | `InitialReportService.SubmitAsync` | لو فيه `IPatientQueryService` جاهزة فعلاً، استبدلي الاستعلام المباشر بيها |
| مرفقات صور بس (`image/*`) | `UploadAttachmentAsync` | لو محتاجة PDF كمرفق كمان، قوليلي نوسّع الـ validation |

---

## 6. الحاجات المؤجلة (لسه محفوظة، مش في السكوب ده)
- ❌ WhatsApp notification (Email بس دلوقتي)
- ❌ AI Autocomplete على نص التقرير (محتاجة `GroqRefinementClient.cs` / `GeminiTranscriptionClient.cs` لبناءها بنفس الأسلوب)

---

## 7. الـ Endpoints النهائية

راجعي ملف `InitialReport-API-Reference.md` (اتبعت في رسالة سابقة) — فيه كل
الـ endpoints مع أمثلة Request/Response جاهزة للاستخدام مباشرة في Angular.
