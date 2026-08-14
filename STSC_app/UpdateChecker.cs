using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace STSC_app
{
    // Gist JSON の受け取り用クラス
    public class UpdateInfo
    {
        public string? ver { get; set; }
        public string? date { get; set; }
        public string? url { get; set; }
    }

    public static class UpdateChecker
    {
        // ★ 現在のアプリバージョン（更新時はここを書き換えます）
        public const string CurrentVersion = "1.0.0";

        // ★ あなたの Gist の Raw URL
        private const string JsonUrl = "https://gist.githubusercontent.com/sinkai2012/dc49cbaf8eead285228ed389df4e5275/raw/information.json";

        public static async Task CheckUpdateAsync(bool isManualCheck = false)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "STSC_app");

                    // キャッシュ防止のためにタイムスタンプを付与
                    string requestUrl = $"{JsonUrl}?t={DateTime.Now.Ticks}";
                    string jsonString = await client.GetStringAsync(requestUrl);

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var updateList = JsonSerializer.Deserialize<List<UpdateInfo>>(jsonString, options);
                    var latest = updateList?.FirstOrDefault();

                    if (latest != null && !string.IsNullOrEmpty(latest.ver))
                    {
                        Version currentVer = new Version(CurrentVersion);
                        Version latestVer = new Version(latest.ver);

                        // Gist 側のバージョンが新しい場合
                        if (latestVer > currentVer)
                        {
                            string message = $"新しいバージョン ({latest.ver}) が利用可能です！\n" +
                                             $"公開日: {latest.date}\n\n" +
                                             $"今すぐアップデートして再起動しますか？";

                            var result = System.Windows.MessageBox.Show(
                                message,
                                "アップデートのお知らせ",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Information
                            );

                            if (result == MessageBoxResult.Yes && !string.IsNullOrWhiteSpace(latest.url))
                            {
                                // Zipをダウンロードして更新ソフトを起動
                                await DownloadAndStartUpdaterAsync(latest.url);
                            }
                        }
                        else if (isManualCheck)
                        {
                            System.Windows.MessageBox.Show(
                                "お使いのアプリは最新バージョンです。",
                                "バージョン確認",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (isManualCheck)
                {
                    System.Windows.MessageBox.Show($"アップデート情報の取得に失敗しました:\n{ex.Message}");
                }
            }
        }

        // Zip ファイルをダウンロードして STSC_Update.exe に引き渡す処理
        private static async Task DownloadAndStartUpdaterAsync(string downloadUrl)
        {
            try
            {
                // Windows の一時フォルダに保存するファイルパスを設定
                string tempZipPath = Path.Combine(Path.GetTempPath(), "STSC_app_update.zip");

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "STSC_app");

                    // Zip をダウンロード
                    byte[] fileBytes = await client.GetByteArrayAsync(downloadUrl);
                    await File.WriteAllBytesAsync(tempZipPath, fileBytes);
                }

                // アプリの実行場所を取得
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string updaterPath = Path.Combine(appDir, "STSC_Update.exe");

                if (File.Exists(updaterPath))
                {
                    // STSC_Update.exe に Zip のパスと解凍先のフォルダを渡して起動
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = updaterPath,
                        Arguments = $"\"{tempZipPath}\" \"{appDir}\"",
                        UseShellExecute = true
                    };

                    Process.Start(startInfo);

                    // ★ メインアプリを閉じてファイルを解放する
                    System.Windows.Application.Current.Shutdown();
                }
                else
                {
                    System.Windows.MessageBox.Show($"更新用プログラム (STSC_Update.exe) が見つかりませんでした。\n配置場所: {updaterPath}");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"更新データのダウンロードに失敗しました:\n{ex.Message}");
            }
        }
    }
}