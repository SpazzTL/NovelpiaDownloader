// MainWin.Helpers.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using SkiaSharp;

namespace NovelpiaDownloader
{
    public partial class MainWin : Form
    {
        private void DownloadChapter(Action<string> log, string chapterId, string chapterName, string jsonPath)
        {
            for (int i = 1; i <= MAX_DOWNLOAD_RETRIES; i++)
            {
                try
                {
                    string resp = PostRequest(log, $"https://novelpia.com/proc/viewer_data/{chapterId}", novelpia.loginkey);
                    if (string.IsNullOrEmpty(resp) || resp.Contains("본인인증"))
                        throw new Exception("Authentication failed or content not available.");

                    using (var file = new StreamWriter(jsonPath, false))
                        file.Write(resp);

                    log(chapterName + "\r\n");
                    return;
                }
                catch (Exception ex)
                {
                    log($"CHAPTER FAILED! ({chapterName}) (Attempt {i}/{MAX_DOWNLOAD_RETRIES}): {ex.Message}\r\n");
                    if (i < MAX_DOWNLOAD_RETRIES)
                    {
                        log("RETRYING!\r\n");
                        Thread.Sleep(RETRY_DELAY_MS);
                    }
                    else
                    {
                        log($"All retries failed for chapter: {chapterName}. It will be missing from the output.\r\n");
                    }
                }
            }
        }

        private void DownloadImage(Action<string> log, string url, string path, string type, bool enableCompression, int jpegQuality, SKEncodedImageFormat format)
        {
            if (!url.StartsWith("http")) url = "https:" + url;

            for (int i = 1; i <= MAX_DOWNLOAD_RETRIES; i++)
            {
                try
                {
                    log($"{type} 다운로드 시작\r\n{url}\r\n");
                    string directory = Path.GetDirectoryName(path);
                    if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                    using (var downloader = new WebClient())
                    using (var imageStream = new MemoryStream(downloader.DownloadData(url)))
                    {
                        if (enableCompression && jpegQuality >= 0 && jpegQuality <= 100)
                        {
                            using (var originalBitmap = SKBitmap.Decode(imageStream))
                            {
                                if (originalBitmap == null) throw new Exception("SkiaSharp could not decode the image data.");
                                using (var originalImage = SKImage.FromBitmap(originalBitmap))
                                using (var encodedData = originalImage.Encode(format, jpegQuality))
                                {
                                    if (encodedData == null) throw new Exception($"SkiaSharp could not encode the image to {format}.");
                                    File.WriteAllBytes(path, encodedData.ToArray());
                                    log($"{type} (압축됨, 품질: {jpegQuality}%, 형식: {format}) 다운로드 완료!\r\n");
                                }
                            }
                        }
                        else
                        {
                            File.WriteAllBytes(path, imageStream.ToArray());
                            log($"{type} 다운로드 완료!\r\n");
                        }
                    }
                    return;
                }
                catch (Exception ex)
                {
                    log($"IMAGE FAILED! ({type}) (Attempt {i}/{MAX_DOWNLOAD_RETRIES}): {ex.Message}\r\n{url}\r\n");
                    if (i < MAX_DOWNLOAD_RETRIES)
                    {
                        log("RETRYING!\r\n");
                        Thread.Sleep(RETRY_DELAY_MS);
                    }
                    else
                    {
                        log($"All retries failed for image: {url}. It will be missing from the output.\r\n");
                    }
                }
            }
        }

        private void ExecuteThreads(List<Thread> threads, int batch_size, float interval)
        {
            for (int i = 0; i < threads.Count; i += batch_size)
            {
                int remain = threads.Count - i;
                int limit = batch_size < remain ? batch_size : remain;
                for (int j = 0; j < limit; j++) threads[i + j].Start();
                for (int j = 0; j < limit; j++) threads[i + j].Join();
                Thread.Sleep((int)(interval * 1000));
            }
        }

        private static string PostRequest(Action<string> log, string url, string loginkey, string data = null)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.UserAgent = "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build=MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Mobile Safari/537.36";
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                request.Headers.Add("cookie", $"LOGINKEY={loginkey};");
                if (!string.IsNullOrEmpty(data))
                {
                    using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                    {
                        streamWriter.Write(data);
                    }
                }
                var response = (HttpWebResponse)request.GetResponse();
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    return streamReader.ReadToEnd();
                }
            }
            catch (WebException ex)
            {
                log($"Web request error for {url}: {ex.Message}\r\n");
                if (ex.Response != null)
                {
                    using (var errorStream = ex.Response.GetResponseStream())
                    using (var reader = new StreamReader(errorStream))
                    {
                        log($"Response: {reader.ReadToEnd()}\r\n");
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                log($"An unexpected error occurred in PostRequest for {url}: {ex.Message}\r\n");
                return null;
            }
        }

        private string SanitizeFilename(string filename)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"[{0}]", invalidChars);
            return Regex.Replace(filename, invalidRegStr, "_");
        }

        private string MinifyCss(string css)
        {
            css = Regex.Replace(css, @"/\*[\s\S]*?\*/", string.Empty);
            css = Regex.Replace(css, @"\s+", " ");
            css = Regex.Replace(css, @"\s*([{}:;,])\s*", "$1");
            return css.Trim();
        }
    }
}