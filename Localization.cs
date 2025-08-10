// Localization.cs
using System.Collections.Generic;

namespace NovelpiaDownloaderEnhanced
{
    public enum Language
    {
        English,
        Korean
    }
    // Handles localization for the application.
    public static class Localization
    {
        public static Language CurrentLanguage { get; set; } = Language.English;

        private static readonly Dictionary<string, string> EnglishStrings = new Dictionary<string, string>
        {
            // Main Window
            {"FormTitle", "Novelpia Downloader"},
            {"LanguageButton", "한국어"},
            {"LoginSuccess", "Login Success!"},
            {"LoginFailed", "Login Failed!"},
            {"LoginAttempted", "Login Attempted!"},
            {"DownloadOptions", "Download Options"},
            {"Hide", "Hide"},
            {"Show", "Show"},
            {"EmailLabel", "Email:"},
            {"PasswordLabel", "Password:"},
            {"LoginKeyLabel", "Login Key:"},
            {"Login", "Login"},
            {"DownloadInitiated", "Download Initiated for Novel ID : {0} | {1}"},
            {"FatalOutputError", "!!!Fatal Error Saving Epub!!!" },
            {"FilterEPUB", "EPUB files (*.epub)|*.epub|All files (*.*)|*.*"},
            {"FilterTXT", "Text files (*.txt)|*.txt|All files (*.*)|*.*"},

        };

        private static readonly Dictionary<string, string> KoreanStrings = new Dictionary<string, string>
        {
            // Main Window
            {"FormTitle", "노벨피아 다운로더"},
            {"LanguageButton", "ENGLISH"},
            {"LoginSuccess", "로그인 성공!"},
            {"LoginFailed", "로그인 실패!"},
            {"LoginAttempted", "로그인 시도!"},
            {"DownloadOptions", "다운로드 옵션"},
            {"Hide", "Hide"},
            {"Show", "Show"},
            {"EmailLabel", "이메일:"},
            {"PasswordLabel", "비밀번호:"},
            {"LoginKeyLabel", "로그인 키:"},
            {"Login", "로그인"},
            {"DownloadInitiated", "다운로드 시작되었습니다"},
            {"FatalOutputError", "!!!EPUB 저장 중 치명적인 오류!!!" },
            {"FilterEPUB", "EPUB 파일 (*.epub)|*.epub|모든 파일 (*.*)|*.*"},
            {"FilterTXT", "텍스트 파일 (*.txt)|*.txt|모든 파일 (*.*)|*.*"}
        };

        public static string GetString(string key)
        {
            var dictionary = CurrentLanguage == Language.Korean ? KoreanStrings : EnglishStrings;
            return dictionary.ContainsKey(key) ? dictionary[key] : $"[{key}]";
        }
    }
}