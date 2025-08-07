// Localization.cs
using System.Collections.Generic;

namespace NovelpiaDownloader
{
    public enum Language
    {
        English,
        Korean
    }

    public static class Localization
    {
        public static Language CurrentLanguage { get; set; } = Language.English;

        private static readonly Dictionary<string, string> EnglishStrings = new Dictionary<string, string>
        {
            // Main Window
            {"FormTitle", "Novelpia Downloader V5.0"},
            {"ToggleLanguage", "언어 전환 (Toggle Lang)"},

            // Login Group
            {"LoginGroup", "Login"},
            {"EmailLabel", "Email"},
            {"PasswordLabel", "Password"},
            {"LoginKeyLabel", "LOGINKEY"},
            {"LoginButton", "Login"},
            {"LoginWithKeyButton", "Login"},

            // Settings Group
            {"SettingsGroup", "Settings"},
            {"FontMappingLabel", "Font Mapping"},
            {"OpenButton", "Open..."},
            {"ThreadsLabel", "Threads"},
            {"IntervalLabel", "Interval"},
            {"SecondsUnit", "sec"},

            // Download Group
            {"DownloadGroup", "Download"},
            {"NovelIdLabel", "Novel ID"},
            {"DownloadRangeLabel", "Download Range"},
            {"FromLabel", "From "},
            {"ToLabel", "To "},
            {"FormatLabel", "Format"},
            {"OptionsLabel", "Options"},
            {"CompressImagesCheck", "Compress Images"},
            {"ImageQualityLabel", "Quality"},
            {"IncludeHtmlCheck", "Save as HTML (instead of TXT)"},
            {"DownloadNoticesCheck", "Download Author Notices"},
            {"RetryChaptersCheck", "Retry Chapters"},
            {"DownloadButton", "Download"},
            {"BatchDownloadButton", "Batch Download from List..."},
            {"QuickDownloadOptionsButton", "Quick Download Options"},

            // Quick Download Panel
            {"EnableQuickDownloadCheck", "Enable Quick Download (No Save Prompt)"},
            {"SaveToLabel", "Save To:"},
            {"BrowseButton", "Browse..."},
            {"FileNamingGroup", "File Naming"},
            {"SaveAsTitleRadio", "Save as Title"},
            {"SaveAsIdRadio", "Save as ID"},
            {"AppendChapterRangeCheck", "Append chapter range to title for ongoing novels"},
            {"ClearAndResetButton", "Clear & Reset"}
        };

        private static readonly Dictionary<string, string> KoreanStrings = new Dictionary<string, string>
        {
            // Main Window
            {"FormTitle", "노벨피아 다운로더 V5.0"},
            {"ToggleLanguage", "ENGLISH"},

            // Login Group
            {"LoginGroup", "로그인"},
            {"EmailLabel", "이메일"},
            {"PasswordLabel", "비밀번호"},
            {"LoginKeyLabel", "로그인 키"},
            {"LoginButton", "로그인"},
            {"LoginWithKeyButton", "로그인"},

            // Settings Group
            {"SettingsGroup", "설정"},
            {"FontMappingLabel", "폰트 매핑"},
            {"OpenButton", "열기..."},
            {"ThreadsLabel", "스레드"},
            {"IntervalLabel", "간격"},
            {"SecondsUnit", "초"},

            // Download Group
            {"DownloadGroup", "다운로드"},
            {"NovelIdLabel", "소설 번호"},
            {"DownloadRangeLabel", "다운로드 범위"},
            {"FromLabel", "부터"},
            {"ToLabel", "까지"},
            {"FormatLabel", "포맷"},
            {"OptionsLabel", "옵션"},
            {"CompressImagesCheck", "이미지 압축"},
            {"ImageQualityLabel", "품질"},
            {"IncludeHtmlCheck", "HTML로 저장 (TXT 대신)"},
            {"DownloadNoticesCheck", "작가의 공지 다운로드"},
            {"RetryChaptersCheck", "실패 시 챕터 재시도"},
            {"DownloadButton", "다운로드"},
            {"BatchDownloadButton", "목록에서 다운로드..."},
            {"QuickDownloadOptionsButton", "빠른 다운로드 옵션"},

            // Quick Download Panel
            {"EnableQuickDownloadCheck", "빠른 다운로드 활성화 (저장 대화상자 없음)"},
            {"SaveToLabel", "저장 위치:"},
            {"BrowseButton", "찾아보기..."},
            {"FileNamingGroup", "파일 이름 형식"},
            {"SaveAsTitleRadio", "제목으로 저장"},
            {"SaveAsIdRadio", "ID로 저장"},
            {"AppendChapterRangeCheck", "연재중인 소설 제목에 챕터 범위 추가"},
            {"ClearAndResetButton", "초기화"}
        };

        public static string GetString(string key)
        {
            var dictionary = CurrentLanguage == Language.Korean ? KoreanStrings : EnglishStrings;
            return dictionary.ContainsKey(key) ? dictionary[key] : $"[{key}]";
        }
    }
}