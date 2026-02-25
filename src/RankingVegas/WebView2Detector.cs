#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace RankingVegas
{
    public static class WebView2Detector
    {
        private static bool? isAvailable;
        private static bool suppressPrompt;
        
        public static bool IsWebView2Available()
        {
            if (isAvailable.HasValue)
            {
                return isAvailable.Value;
            }
            
            try
            {
                string version = Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString();
                isAvailable = !string.IsNullOrEmpty(version);
                return isAvailable.Value;
            }
            catch
            {
                isAvailable = false;
                return false;
            }
        }
        
        public static DockableControl CreateDockView()
        {
            if (IsWebView2Available())
            {
                return new RankingVegasWebView();
            }

            PromptInstallWebView2();
            return new RankingVegasWinForms();
        }

        private static void PromptInstallWebView2()
        {
            if (suppressPrompt)
            {
                return;
            }

            string message = Localization.Text(
                "检测到未安装 WebView2 运行时。\n\n是否现在安装？选择\"否\"将继续使用传统界面且不再提示。",
                "WebView2 runtime was not found.\n\nInstall it now? Choose No to continue with the classic interface and stop showing this prompt.",
                "WebView2 ランタイムが見つかりません。\n\n今すぐインストールしますか？「いいえ」を選択するとクラシックインターフェースを使用し、このプロンプトは表示されなくなります。");
            string caption = Localization.Text("WebView2 运行时", "WebView2 Runtime", "WebView2 ランタイム");

            DialogResult result = MessageBox.Show(message, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (result == DialogResult.Yes)
            {
                try
                {
                    Process.Start("https://go.microsoft.com/fwlink/p/?LinkId=2124703");
                }
                catch
                {
                }
            }
            else
            {
                suppressPrompt = true;
            }
        }
    }
}
