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
            {"ClearAndResetButton", "Clear & Reset"},

            // New Localization Strings for Logs & Dialogs
            {"LoginSuccessful", "Login successful!"},
            {"LoginFailedOnInit", "Login failed! (This is normal on init)"},
            {"PresetDirectoryError", "Error: Preset directory does not exist: {0}"},
            {"PresetDirectoryErrorMessageBox", "The preset save directory does not exist. Please select a valid directory."},
            {"ErrorTitle", "Error"},
            {"QuickDownloadInitiated", "Quick Download initiated. Output path: {0}"},
            {"DownloadInitiated", "Download initiated. Output path: {0}"},
            {"OutputFileNotFoundWarning", "Warning: Output file not found after download. Waiting 3 seconds..."},
            {"CorruptOutputRetry", "CORRUPT OUTPUT, RETRYING! (Attempt {0}/{1})"},
            {"FatalOutputError", "FATAL: Output file not created after all retries."},
            {"QuickSettingsCleared", "Quick Download settings have been cleared."},
            {"LoginFailed", "Login failed!"},
            {"FilterEPUB", "EPUB Files|*.epub"},
            {"FilterHTML", "HTML Files|*.html"},
            {"FilterTXT", "Text Files|*.txt"},
            {"BatchFilter", "Text files|*.txt"},
            {"BatchFileTitle", "Select the Novel List File"},
            {"BatchFolderDescription", "Select the output directory for downloaded novels"},
            {"ProgressChapters", "({0}/{1} chapters)"},
            {"ProgressNovels", "({0}/{1} novels)"},
            {"StatusIdle", "Idle"},
            {"FontFilter", "|*.json"},
            {"BrowseFolderDescription", "Select a folder to save downloads to"},

            // Download.cs Logs
            {"DownloadStarted", "Download started: Novel ID {0}"},
            {"AnalyzingNovel", "Analyzing novel to get chapter list..."},
            {"ScanningNotices", "Scanning for author notices..."},
            {"FoundNotices", "Found {0} author notice(s)."},
            {"FoundZeroNotices", "Found 0 author notices."},
            {"ScanningChapters", "Scanning for regular chapters..."},
            {"NoMorePages", "Reached end of chapter list. Found {0} chapters in total."},
            {"DownloadingChapters", "Downloading chapters..."},
            {"ProcessingChapter", "Processing chapter {0} of {1}: {2}"},
            {"FailedDownload", "ERROR: Failed to download chapter {0} ({1}). Retrying... (Attempt {2}/{3})"},
            {"FailedAfterRetries", "ERROR: Failed to download chapter {0} ({1}) after {2} retries."},
            {"HtmlProcessing", "Converting to HTML..."},
            {"EpubProcessing", "Converting to EPUB..."},
            {"EpubCreationFailure", "ERROR: Failed to create EPUB file."},
            {"DownloadComplete", "Download complete!"},
            {"SavingFile", "Saving file: {0}"},
            {"UnexpectedError", "An unexpected error occurred during download: {0}"},
            {"ChapterContentProcessing", "Processing chapter content {0} of {1}"},
            {"ImageProcessing", "Processing image: {0} to {1}"},
            {"ImageCompressionError", "Error compressing image: {0}"},
            {"ImageConversionError", "Error converting image to JPG: {0}"},
            {"NovelDataSaveError", "Error saving novel data: {0}"},
            {"ChapterProcessingError", "Error processing chapter: {0}"},
            {"ImageDownloadError", "Failed to download image from {0}"},
            {"FileExistsSkipping", "File already exists, skipping: {0}"},
            {"DownloadingImage", "Downloading image {0} of {1}: {2}"},
            {"ImageSavedTo", "Image saved to: {0}"},

            // Batch.cs Logs
            {"BatchStart", "Starting batch download from: {0}"},
            {"ListFileNotFound", "Error: List file not found at {0}"},
            {"NovelDownloadSkipped", "Skipping novel '{0}' as ID is empty or invalid."},
            {"BatchNovelProgress", "Downloading novel {0} of {1}: {2}"},
            {"BatchComplete", "Batch download complete!"},
            {"BatchFatalError", "FATAL ERROR in batch download: {0}"},

            // Helpers.cs Logs
            {"FetchingMetadata", "Fetching metadata for Novel ID: {0}..."},
            {"MetadataFetchComplete", "Metadata fetch complete."},
            {"MetadataFetchError", "Error fetching novel metadata: {0}"},
            {"ChapterDownloadStart", "Downloading chapter {0} ({1})..."},
            {"ChapterDownloadSuccess", "Chapter {0} ({1}) downloaded successfully."},
            {"DownloadingImageFrom", "Downloading image from: {0}"},
            {"SavingImage", "Saving image to: {0}"},
            {"DownloadAttempt", "Attempt {0} to download chapter {1} of {2}..."},
            {"DownloadSuccess", "Download successful!"},
            {"ChapterDownloadFailed", "Chapter download failed after {0} attempts."},
            {"FetchingSession", "Fetching session and cookie..."},
            {"SessionFetchFailed", "Failed to fetch session and cookie. Status code: {0}"},
            {"PostRequestFailed", "Post request failed. URL: {0}, Error: {1}"},
            {"PostRequestFailedNoDetail", "Post request failed with no details."}
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
            {"ClearAndResetButton", "초기화"},

            // New Localization Strings for Logs & Dialogs
            {"LoginSuccessful", "로그인 성공!"},
            {"LoginFailedOnInit", "로그인 실패! (초기화 시 정상)"},
            {"PresetDirectoryError", "오류: 사전 설정된 디렉토리가 존재하지 않습니다: {0}"},
            {"PresetDirectoryErrorMessageBox", "사전 설정된 저장 디렉토리가 존재하지 않습니다. 유효한 디렉토리를 선택해주세요."},
            {"ErrorTitle", "오류"},
            {"QuickDownloadInitiated", "빠른 다운로드가 시작되었습니다. 출력 경로: {0}"},
            {"DownloadInitiated", "다운로드가 시작되었습니다. 출력 경로: {0}"},
            {"OutputFileNotFoundWarning", "경고: 다운로드 후 출력 파일을 찾을 수 없습니다. 3초간 대기 중..."},
            {"CorruptOutputRetry", "손상된 출력, 재시도! (시도 {0}/{1})"},
            {"FatalOutputError", "치명적 오류: 모든 재시도 후에도 출력 파일이 생성되지 않았습니다."},
            {"QuickSettingsCleared", "빠른 다운로드 설정이 초기화되었습니다."},
            {"LoginFailed", "로그인 실패!"},
            {"FilterEPUB", "EPUB 파일|*.epub"},
            {"FilterHTML", "HTML 파일|*.html"},
            {"FilterTXT", "텍스트 파일|*.txt"},
            {"BatchFilter", "텍스트 파일|*.txt"},
            {"BatchFileTitle", "소설 목록 파일 선택"},
            {"BatchFolderDescription", "다운로드된 소설의 출력 디렉토리를 선택하세요"},
            {"ProgressChapters", "({0}/{1} 챕터)"},
            {"ProgressNovels", "({0}/{1} 소설)"},
            {"StatusIdle", "대기 중"},
            {"FontFilter", "|*.json"},
            {"BrowseFolderDescription", "다운로드를 저장할 폴더를 선택하세요"},
            
            // Download.cs Logs
            {"DownloadStarted", "다운로드가 시작되었습니다: 소설 ID {0}"},
            {"AnalyzingNovel", "소설을 분석하여 챕터 목록을 가져오는 중..."},
            {"ScanningNotices", "작가의 공지를 스캔하는 중..."},
            {"FoundNotices", "{0}개의 작가 공지를 찾았습니다."},
            {"FoundZeroNotices", "0개의 작가 공지를 찾았습니다."},
            {"ScanningChapters", "일반 챕터를 스캔하는 중..."},
            {"NoMorePages", "챕터 목록의 끝에 도달했습니다. 총 {0}개의 챕터를 찾았습니다."},
            {"DownloadingChapters", "챕터를 다운로드하는 중..."},
            {"ProcessingChapter", "{1}개 중 {0}번째 챕터 처리 중: {2}"},
            {"FailedDownload", "오류: 챕터 {0} ({1}) 다운로드 실패. 재시도 중... (시도 {2}/{3})"},
            {"FailedAfterRetries", "오류: 챕터 {0} ({1})가 {2}번의 재시도 후에도 다운로드되지 않았습니다."},
            {"HtmlProcessing", "HTML로 변환 중..."},
            {"EpubProcessing", "EPUB로 변환 중..."},
            {"EpubCreationFailure", "오류: EPUB 파일 생성 실패."},
            {"DownloadComplete", "다운로드 완료!"},
            {"SavingFile", "파일 저장 중: {0}"},
            {"UnexpectedError", "다운로드 중 예상치 못한 오류가 발생했습니다: {0}"},
            {"ChapterContentProcessing", "{1}개 중 {0}번째 챕터 내용 처리 중"},
            {"ImageProcessing", "이미지 처리 중: {0} to {1}"},
            {"ImageCompressionError", "이미지 압축 오류: {0}"},
            {"ImageConversionError", "이미지를 JPG로 변환하는 중 오류가 발생했습니다: {0}"},
            {"NovelDataSaveError", "소설 데이터 저장 오류: {0}"},
            {"ChapterProcessingError", "챕터 처리 오류: {0}"},
            {"ImageDownloadError", "{0}에서 이미지 다운로드 실패"},
            {"FileExistsSkipping", "파일이 이미 존재하여 건너뜁니다: {0}"},
            {"DownloadingImage", "{1}개 중 {0}번째 이미지 다운로드 중: {2}"},
            {"ImageSavedTo", "이미지 저장 위치: {0}"},

            // Batch.cs Logs
            {"BatchStart", "{0}에서 일괄 다운로드 시작"},
            {"ListFileNotFound", "오류: {0}에서 목록 파일을 찾을 수 없습니다"},
            {"NovelDownloadSkipped", "ID가 비어 있거나 유효하지 않아 소설 '{0}'를 건너뜁니다."},
            {"BatchNovelProgress", "{1}개 중 {0}번째 소설 다운로드 중: {2}"},
            {"BatchComplete", "일괄 다운로드 완료!"},
            {"BatchFatalError", "일괄 다운로드 중 치명적 오류: {0}"},

            // Helpers.cs Logs
            {"FetchingMetadata", "소설 ID {0}의 메타데이터를 가져오는 중..."},
            {"MetadataFetchComplete", "메타데이터 가져오기 완료."},
            {"MetadataFetchError", "소설 메타데이터를 가져오는 중 오류가 발생했습니다: {0}"},
            {"ChapterDownloadStart", "챕터 {0} ({1}) 다운로드 중..."},
            {"ChapterDownloadSuccess", "챕터 {0} ({1}) 다운로드 성공."},
            {"DownloadingImageFrom", "이미지 다운로드 위치: {0}"},
            {"SavingImage", "이미지 저장 위치: {0}"},
            {"DownloadAttempt", "{2}개 중 {1}번째 챕터 다운로드 시도 {0}회..."},
            {"DownloadSuccess", "다운로드 성공!"},
            {"ChapterDownloadFailed", "{0}번의 시도 후 챕터 다운로드에 실패했습니다."},
            {"FetchingSession", "세션 및 쿠키를 가져오는 중..."},
            {"SessionFetchFailed", "세션 및 쿠키를 가져오는 데 실패했습니다. 상태 코드: {0}"},
            {"PostRequestFailed", "POST 요청 실패. URL: {0}, 오류: {1}"},
            {"PostRequestFailedNoDetail", "상세 정보 없이 POST 요청 실패."}
        };

        public static string GetString(string key)
        {
            var dictionary = CurrentLanguage == Language.Korean ? KoreanStrings : EnglishStrings;
            return dictionary.ContainsKey(key) ? dictionary[key] : $"[{key}]";
        }
    }
}