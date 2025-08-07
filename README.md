

# [한국어](https://www.google.com/search?q=%23korean-%EB%B2%84%EC%A0%84)

# NovelpiaDownloader Enhanced Fork

A fork of [CjangCjengh's NovelpiaDownloader](https://github.com/CjangCjengh/NovelpiaDownloader) that enhances the user experience and output quality. This version adds comprehensive metadata (tags, author, synopsis), improves EPUB formatting with HTML tag and newline support, includes file size optimization, and much more\!

-----

## 📚 Table of Contents

  - [✨ Features](https://www.google.com/search?q=%23-features)
  - [🚀 Usage](https://www.google.com/search?q=%23-usage)
  - [💾 Space-Saving Tips](https://www.google.com/search?q=%23-space-saving-tips)
  - [🛠️ Command-Line Arguments](https://www.google.com/search?q=%23%EF%B8%8F-command-line-arguments)
  - [❓ FAQ (Frequently Asked Questions)](https://www.google.com/search?q=%23-faq-frequently-asked-questions)
  - [📜 Legal & Disclaimer](https://www.google.com/search?q=%23-legal--disclaimer)

-----

## ✨ Features

  - **Rich Metadata:** Automatically grabs and embeds tags, author names, and synopses into the EPUB file metadata.
  - **Improved EPUB Formatting:** Supports HTML tags and newlines, preserving the original formatting of the novel.
  - **File Size Optimization:** Includes a WebP image compression feature to significantly reduce the final file size without a noticeable loss in quality.
  - **Command-Line Interface:** Offers a robust command-line interface for automated and scripted downloads.
  - **Bulk Downloads:** Allows you to easily redownload your library to fix formatting, optimize file sizes, and more. (Format is ``outputname, id`` with each novel on a new line.) 
  - **Improved Downloads:** Offers one-click downloads, with novels automatically named and placed in whichever directory you choose. Also includes auto-retries and error detection.

<img width="880" height="698" alt="image" src="https://github.com/user-attachments/assets/81b5a264-cc22-4f82-8a4b-341d342c9fc3" />


## 🚀 Usage

To download paid chapters, you'll need a `LOGINKEY`. You can get it by logging into your Novelpia account in a web browser, opening the developer tools (F12), and navigating to the **Storage** tab. Copy the value of your `LOGINKEY` from there.
*(You must have access to the content that you intend to download on your account.)*

A higher thread count and a lower interval can speed up your downloads, but be aware that this increases the risk of an IP ban.
*(Going above 10 threads can cause the website to rate-limit you, resulting in chapters failing to download and eventually a 24-48 hour IP ban.)*
<img width="450" height="42" alt="image" src="https://github.com/user-attachments/assets/a702e637-1825-4e2c-923c-94def6ef06d0" />

-----

## 💾 Space-Saving Tips

### Image Compression

Built-in image compression can dramatically reduce file size. Use the `-compressimages` and `-jpegquality` arguments to enable this feature.

  - **80% Quality:** Provides large savings with no noticeable quality difference (e.g., 1MB -\> 65KB).
  - **50% Quality:** Offers massive savings with only a small difference in quality (e.g., 1MB -\> 30KB).
  - **10-30% Quality:** For extreme savings, though the quality difference will be noticeable (e.g., 1MB -\> \<10KB).
![Comparison of uncompressed and 10% quality compressed images](https://github.com/user-attachments/assets/09161c74-92d8-4b3e-8e72-8ac574db719d)

### Post-Processing with Calibre

For even greater space savings (10-50%), you can use the Calibre EPUB editor. Converting and saving a new `.epub` file with Calibre optimizes the CSS, HTML, and embedded fonts. This is a manual step, as implementing these optimizations directly is currently outside the scope of this project.

-----

## 🛠️ Command-Line Arguments

The NovelpiaDownloader can be operated directly from the command line, ideal for automated and scripted downloads.

*(Keep the table of arguments and usage examples exactly as you have them, they are very clear and well-formatted.)*

-----

## ❓ FAQ (Frequently Asked Questions)

Here are some solutions to common problems you might encounter.

**Q: I'm getting an error that says I'm not logged in, but I've entered my LOGINKEY.**
A: Ensure your `LOGINKEY` is still valid. Novelpia keys expire after a period of time. Try logging out and back in on the website, then get a new `LOGINKEY` from the storage tab and use that. Make sure you click the "Log In" button in the application.

**Q: The download process seems to be stuck or is extremely slow.**
A: This could be due to a temporary IP ban from Novelpia's servers, which can happen with a high thread count. Try the following:

1.  Reduce your thread count and increase the interval in the settings.
2.  If the problem persists, wait for a few hours and try again, as the IP ban is usually temporary.
3.  Check your network connection.

**Q: The downloaded file is missing chapters or content.**
A: Double-check the `-from` and `-to` arguments to make sure they cover the desired chapter range. Ensure you have a valid `LOGINKEY` for any paid chapters. Lastly, make sure your account has access to the content you are downloading.

**Q: How do I find the `Novel ID`?**
A: The `Novel ID` is the number in the novel's URL. For example, if the URL is `https://novelpia.com/novel/123456`, the `Novel ID` is `123456`.

**Q: My EPUB reader is throwing errors when I try to open the EPUB.**
A: This can be caused by missing chapters (e.g., R19 chapters being skipped due to account permissions). The easiest fix is to open the EPUB in [Calibre](https://calibre-ebook.com/download) (an open-source e-book & EPUB manager) and convert it to a new EPUB file. This can be done in bulk.

-----

## 📜 Legal & Disclaimer

This project is a fork of CjangCjengh's NovelpiaDownloader and is intended for personal use to create backups of content you have legally accessed. I am not affiliated with Novelpia. Please respect their terms of service and copyright laws.

-----


## 🇰🇷 한국어 버전

# NovelpiaDownloader Enhanced Fork

[CjangCjengh's NovelpiaDownloader](https://github.com/CjangCjengh/NovelpiaDownloader)의 포크 버전으로, 사용자 경험과 출력 품질을 향상시켰습니다. 이 버전은 포괄적인 메타데이터(태그, 작가, 시놉시스), HTML 태그 및 줄바꿈을 지원하여 EPUB 포맷팅을 개선하고, 파일 크기 최적화 기능 등을 추가했습니다\!

-----

## 📚 목차

  - [✨ 기능](https://www.google.com/search?q=%23-features)
  - [🚀 사용법](https://www.google.com/search?q=%23-usage)
  - [💾 공간 절약 팁](https://www.google.com/search?q=%23-space-saving-tips)
  - [🛠️ 커맨드라인 인수](https://www.google.com/search?q=%23%EF%B8%8F-command-line-arguments)
  - [❓ FAQ (자주 묻는 질문)](https://www.google.com/search?q=%23-faq-frequently-asked-questions)
  - [📜 법률 및 면책 조항](https://www.google.com/search?q=%23-legal--disclaimer)

-----

## ✨ 기능

  - **풍부한 메타데이터:** 태그, 작가명, 시놉시스를 자동으로 가져와 EPUB 파일 메타데이터에 포함시킵니다.
  - **향상된 EPUB 포맷:** HTML 태그와 줄바꿈을 지원하여 소설의 원본 포맷을 그대로 유지합니다.
  - **파일 크기 최적화:** WebP 이미지 압축 기능이 포함되어 품질 저하 없이 최종 파일 크기를 크게 줄일 수 있습니다.
  - **커맨드라인 인터페이스:** 자동화된 스크립트 다운로드를 위한 강력한 커맨드라인 인터페이스를 제공합니다.
  - **대량 다운로드:** 포맷팅을 수정하거나 파일 크기를 최적화하는 등의 목적으로 라이브러리를 쉽게 다시 다운로드할 수 있습니다.
  - **개선된 다운로드:** 소설 제목이 자동으로 지정되고 원하는 디렉토리에 저장되는 원클릭 다운로드를 제공합니다. 자동 재시도 및 오류 감지 기능도 포함되어 있습니다.
  - 
<img width="886" height="701" alt="image" src="https://github.com/user-attachments/assets/fc8c966b-b6eb-4a86-9b07-119dea42ac47" />


## 🚀 사용법

유료 챕터를 다운로드하려면 `LOGINKEY`가 필요합니다. 웹 브라우저에서 노벨피아 계정에 로그인한 다음, 개발자 도구(F12)를 열고 **Storage** 탭으로 이동하여 `LOGINKEY` 값을 복사할 수 있습니다.
*(다운로드하려는 콘텐츠에 대한 접근 권한이 계정에 있어야 합니다.)*

높은 스레드 수와 낮은 간격은 다운로드 속도를 높일 수 있지만, IP 차단 위험이 증가할 수 있습니다.
*(10개 이상의 스레드를 사용하면 웹사이트에서 속도 제한이 걸려 챕터 다운로드에 실패하고, 결국 24-48시간 동안 IP가 차단될 수 있습니다.)*
<img width="444" height="44" alt="image" src="https://github.com/user-attachments/assets/f9c37fb0-ed3f-4653-b476-ff423b55d099" />

-----

## 💾 공간 절약 팁

### 이미지 압축

내장된 이미지 압축 기능은 파일 크기를 획기적으로 줄일 수 있습니다. 이 기능을 사용하려면 `-compressimages` 및 `-jpegquality` 인수를 사용하세요.

  - **80% 품질:** 눈에 띄는 품질 저하 없이 큰 용량 절약 (예: 1MB -\> 65KB).
  - **50% 품질:** 약간의 품질 차이만으로도 엄청난 용량 절약 (예: 1MB -\> 30KB).
  - **10-30% 품질:** 품질 차이가 눈에 띄지만, 극단적인 용량 절약 가능 (예: 1MB -\> \<10KB).
![Comparison of uncompressed and 10% quality compressed images](https://github.com/user-attachments/assets/09161c74-92d8-4b3e-8e72-8ac574db719d)

### Calibre를 이용한 후처리

더 큰 용량 절약(10-50%)을 위해 Calibre EPUB 편집기를 사용할 수 있습니다. Calibre를 사용하여 새로운 `.epub` 파일로 변환하고 저장하면 CSS, HTML 및 내장된 글꼴이 최적화됩니다. 이러한 최적화 기능을 직접 구현하는 것은 현재 프로젝트 범위 밖에 있으므로 수동으로 진행해야 합니다.

-----

## 🛠️ 커맨드라인 인수

NovelpiaDownloader는 커맨드라인에서 직접 작동할 수 있어, 자동화된 스크립트 다운로드에 이상적입니다.

*(인수와 사용 예시가 담긴 표는 명확하고 잘 정리되어 있으므로 그대로 유지합니다.)*

-----

## ❓ FAQ (자주 묻는 질문)

다음은 흔히 발생할 수 있는 문제에 대한 해결책입니다.

**Q: 로그인했는데도 로그인이 되지 않았다는 오류가 뜹니다.**
A: `LOGINKEY`가 유효한지 확인하세요. 노벨피아 키는 일정 시간이 지나면 만료됩니다. 웹사이트에서 로그아웃했다가 다시 로그인한 후, 스토리지 탭에서 새로운 `LOGINKEY`를 얻어 사용해 보세요. 애플리케이션에서 "Log In" 버튼을 클릭했는지 확인하세요.

**Q: 다운로드 진행이 멈추거나 매우 느립니다.**
A: 이는 노벨피아 서버로부터의 일시적인 IP 차단 때문일 수 있으며, 높은 스레드 수를 사용할 때 발생할 수 있습니다. 다음을 시도해 보세요:

1.  설정에서 스레드 수를 줄이고 간격을 늘리세요.
2.  문제가 지속되면 IP 차단은 보통 일시적이므로 몇 시간 기다렸다가 다시 시도해 보세요.
3.  네트워크 연결 상태를 확인하세요.

**Q: 다운로드된 파일에 챕터나 내용이 누락되었습니다.**
A: `-from` 및 `-to` 인수가 원하는 챕터 범위를 포함하는지 다시 확인하세요. 유료 챕터의 경우 유효한 `LOGINKEY`가 있는지 확인하세요. 마지막으로, 계정이 다운로드하려는 콘텐츠에 대한 접근 권한이 있는지 확인하세요.

**Q: `소설 ID`는 어떻게 찾나요?**
A: `소설 ID`는 소설 URL에 있는 숫자입니다. 예를 들어, URL이 `https://novelpia.com/novel/123456`인 경우 `소설 ID`는 `123456`입니다.

**Q: EPUB 리더에서 파일을 열 때 오류가 발생합니다.**
A: 이는 챕터 누락(예: 계정 권한 부족으로 인해 R19 챕터가 건너뛰어짐)으로 인해 발생할 수 있습니다. 가장 쉬운 해결책은 [Calibre](https://calibre-ebook.com/download)(오픈소스 전자책 및 EPUB 관리자)에서 EPUB 파일을 열고 EPUB 형식으로 변환한 다음, 새로운 EPUB 파일을 다운로드하는 것입니다. 이 작업은 일괄 처리할 수 있습니다.

-----

## 📜 법률 및 면책 조항

이 프로젝트는 CjangCjengh의 NovelpiaDownloader의 포크 버전이며, 사용자가 합법적으로 접근한 콘텐츠를 백업하기 위한 개인적인 용도로 제작되었습니다. 저는 노벨피아와 제휴 관계가 없습니다. 노벨피아의 서비스 약관 및 저작권법을 존중해 주시기 바랍니다.
