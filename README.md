# NovelpiaDownloader Enhanced Fork

A fork of [CjangCjengh's NovelpiaDownloader](https://github.com/CjangCjengh/NovelpiaDownloader) that enhances the user experience and output quality. This version adds comprehensive metadata (tags, author, synopsis), improves epub formatting with HTML tag and newline support, and includes features to optimize file size.

---

## 📚 Table of Contents

- [✨ Features](#-features)
- [🚀 Usage](#-usage)
- [💾 Space Saving Tips](#-space-saving-tips)
- [🛠️ Command-Line Arguments](#️-command-line-arguments)
- [❓ FAQ (Frequently Asked Questions)](#-faq-frequently-asked-questions)
- [📜 Legal & Disclaimer](#-legal--disclaimer)

---

## ✨ Features

- **Rich Metadata:** Automatically grabs and embeds tags, author names, and synopses into the epub file metadata.
- **Improved EPUB Formatting:** Supports HTML tags and newlines, preserving the original formatting of the novel.
- **File Size Optimization:** Includes an webp image compression feature to significantly reduce the final file size without a noticeable loss in quality.
- **Command-Line Interface:** Offers a robust command-line interface for automated and scripted downloads.
- **Bulk Downloads:** Allows you to easily redownload your library to fix formatting, optimize file sizes, etc.

## 🚀 Usage

To download paid chapters, you'll need a `LOGINKEY`. You can get it by logging into your Novelpia account in a web browser, opening the developer tools (F12), and navigating to the `Storage` tab. Copy the value of your `LOGINKEY` from there.
(YOU MUST HAVE ACCESS TO THE CONTENT THAT YOU INTEND TO DOWNLOAD ON YOUR ACCOUNT.)

A higher thread count and lower interval can speed up your downloads, but be aware that this increases the risk of an IP ban.


---

## 💾 Space Saving Tips

### Image Compression

Our built-in image compression can dramatically reduce file size. Use the `-compressimages` and `-jpegquality` arguments to enable this feature.

- **80% Quality:** Provides large savings with no noticeable quality difference (e.g., 1MB -> 65KB).
- **50% Quality:** Offers massive savings with only a small difference in quality (e.g., 1MB -> 30KB).
- **10-30% Quality:** For extreme savings, though the quality difference will be noticeable (e.g., 1MB -> <10KB).

![Comparison of uncompressed and 10% quality compressed images](https://github.com/user-attachments/assets/09161c74-92d8-4b3e-8e72-8ac574db719d)

### Post-Processing with Calibre

For even greater space savings (10-50%), you can use the Calibre epub editor. Converting and saving a new `.epub` file with Calibre optimizes the CSS, HTML, and embedded fonts. This is a manual step, as implementing these optimizations directly is currently outside the scope of this project.

---

## 🛠️ Command-Line Arguments

The NovelpiaDownloader can be operated directly from the command line, ideal for automated and scripted downloads.

*(Keep the table of arguments and usage examples exactly as you have them, they are very clear and well-formatted.)*

---

## ❓ FAQ (Frequently Asked Questions)

Here are some solutions to common problems you might encounter.

**Q: I'm getting an error that says I'm not logged in, but I've entered my LOGINKEY.**
A: Ensure your `LOGINKEY` is still valid. Novelpia keys expire after a period of time. Try logging out and back in on the website, then get a new `LOGINKEY` from the storage tab and use that. MAKE SURE YOU CLICK THE LOG IN BUTTON in application.

**Q: The download process seems to be stuck or is extremely slow.**
A: This could be due to a temporary IP ban from Novelpia's servers, which can happen with a high thread count. Try the following:
1. Reduce your thread count and increase the interval in the settings.
2. If the problem persists, wait for a few hours and try again, as the IP ban is usually temporary.
3. Check your network connection.

**Q: The downloaded file is missing chapters or content.**
A: Double-check the `-from` and `-to` arguments to make sure they cover the desired chapter range. Ensure you have a valid `LOGINKEY` for any paid chapters. Lastly make sure your accont has access to the content you are donwloading.

**Q: How do I find the `Novel ID`?**
A: The `Novel ID` is the number in the novel's URL. For example, if the URL is `https://novelpia.com/novel/123456`, the `Novel ID` is `123456`.

**Q: My epub reader is throwing errors when I try to open the epub.**
A: This can be caused by missing chapters(r19 chapters being skipped due to account permissions, etc). The easiest fix is to open the epub in [Calibre](https://calibre-ebook.com/download) (A open-source e-book & epub manager) and convert to epub, then download the new epub file. This can be done in bulk. 
---

## 📜 Legal & Disclaimer

This project is a fork of CjangCjengh's NovelpiaDownloader and is intended for personal use to create backups of content you have legally accessed. I am not affiliated with Novelpia. Please respect their terms of service and copyright laws.
