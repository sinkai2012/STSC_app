using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace STSC_app
{
    public static class UpdateChecker
    {
        public static readonly string CurrentVersion = $"v{App.AppVer}";
        private const string RepoOwner = "sinkai2012";
        private const string RepoName = "STSC_app";

        /// <summary>
        /// ★ 1. 自動チェック：GitHubの最新リリースを優先して判定
        /// </summary>
        public static async Task CheckUpdateAsync(bool isManualCheck = false)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "STSC_app");

                    string apiUrl = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases";
                    string jsonString = await client.GetStringAsync(apiUrl);

                    using (JsonDocument doc = JsonDocument.Parse(jsonString))
                    {
                        var releases = doc.RootElement.EnumerateArray().ToList();
                        if (releases.Count == 0) return;

                        // 最新のリリース（配列の先頭）を取得
                        var latestRelease = releases[0];
                        string latestTag = latestRelease.GetProperty("tag_name").GetString() ?? "";

                        // バージョン比較
                        if (IsUpdateRequired(CurrentVersion, latestTag))
                        {
                            if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
                            {
                                string message = $"新しいバージョン ({latestTag}) が利用可能です！\n\n今すぐアップデートして再起動しますか？";
                                bool isYes = await mainWindow.ShowCustomDialogAsync("アップデートのお知らせ", message, "今すぐ更新", "あとで");

                                if (isYes)
                                {
                                    string? downloadUrl = GetZipUrlFromRelease(latestRelease);
                                    if (!string.IsNullOrEmpty(downloadUrl))
                                    {
                                        await DownloadAndStartUpdaterAsync(client, downloadUrl);
                                    }
                                }
                            }
                        }
                        else if (isManualCheck)
                        {
                            if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
                            {
                                await mainWindow.ShowCustomDialogAsync("バージョン確認", "お使いのアプリは最新バージョンです。", "OK", "");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (isManualCheck && System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
                {
                    await mainWindow.ShowCustomDialogAsync("エラー", $"アップデート確認に失敗しました:\n{ex.Message}", "OK", "");
                }
            }
        }

        /// <summary>
        /// ★ 2. settings.xaml 用：GitHubから公開中の全バージョン一覧を取得
        /// </summary>
        public static async Task<List<string>> GetVersionListAsync()
        {
            var versionList = new List<string>();
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "STSC_app");
                    string apiUrl = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases";
                    string jsonString = await client.GetStringAsync(apiUrl);

                    using (JsonDocument doc = JsonDocument.Parse(jsonString))
                    {
                        foreach (var release in doc.RootElement.EnumerateArray())
                        {
                            string tagName = release.GetProperty("tag_name").GetString() ?? "";
                            if (!string.IsNullOrEmpty(tagName))
                            {
                                versionList.Add(tagName);
                            }
                        }
                    }
                }
            }
            catch { }
            return versionList;
        }

        /// <summary>
        /// ★ 3. settings.xaml 用：選択した特定のバージョンをダウンロードしてインストール
        /// </summary>
        public static async Task InstallSpecificVersionAsync(string targetVersion)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "STSC_app");
                    string apiUrl = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/tags/{targetVersion}";
                    string jsonString = await client.GetStringAsync(apiUrl);

                    using (JsonDocument doc = JsonDocument.Parse(jsonString))
                    {
                        string? downloadUrl = GetZipUrlFromRelease(doc.RootElement);

                        if (string.IsNullOrEmpty(downloadUrl))
                        {
                            if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
                            {
                                await mainWindow.ShowCustomDialogAsync("エラー", $"バージョン '{targetVersion}' 内にダウンロード用 ZIP が見つかりませんでした。", "OK", "");
                            }
                            return;
                        }

                        await DownloadAndStartUpdaterAsync(client, downloadUrl);
                    }
                }
            }
            catch (Exception ex)
            {
                if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
                {
                    await mainWindow.ShowCustomDialogAsync("エラー", $"インストールの実行に失敗しました:\n{ex.Message}", "OK", "");
                }
            }
        }

        // リリース内の ZIP ファイル URL を取得するヘルパー
        private static string? GetZipUrlFromRelease(JsonElement releaseElement)
        {
            if (releaseElement.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    string name = asset.GetProperty("name").GetString() ?? "";
                    if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        return asset.GetProperty("browser_download_url").GetString();
                    }
                }
            }
            return null;
        }

        // バージョン判定
        private static bool IsUpdateRequired(string currentVerStr, string latestVerStr)
        {
            Version currentVer = ParseVersionNumber(currentVerStr);
            Version latestVer = ParseVersionNumber(latestVerStr);
            return latestVer > currentVer;
        }

        private static Version ParseVersionNumber(string ver)
        {
            if (string.IsNullOrEmpty(ver)) return new Version(0, 0, 0);
            string clean = ver.TrimStart('v', 'V').Split('-')[0];
            return Version.TryParse(clean, out Version? parsed) ? parsed : new Version(0, 0, 0);
        }

        // ダウンロード & アップデータ起動
        private static async Task DownloadAndStartUpdaterAsync(HttpClient client, string downloadUrl)
        {
            string tempZipPath = Path.Combine(Path.GetTempPath(), "STSC_app_update.zip");

            using (HttpResponseMessage response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                using (Stream streamToReadFrom = await response.Content.ReadAsStreamAsync())
                using (Stream streamToWriteTo = File.Open(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await streamToReadFrom.CopyToAsync(streamToWriteTo);
                }
            }

            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string updaterPath = Path.Combine(appDir, "STSC_Update.exe");

            if (File.Exists(updaterPath))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = updaterPath,
                    Arguments = $"\"{tempZipPath}\"",
                    UseShellExecute = true
                };
                Process.Start(startInfo);
                System.Windows.Application.Current.Shutdown();
            }
            else
            {
                if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
                {
                    await mainWindow.ShowCustomDialogAsync("エラー", $"更新用プログラム (STSC_Update.exe) が見つかりませんでした。", "OK", "");
                }
            }
        }
    }
}