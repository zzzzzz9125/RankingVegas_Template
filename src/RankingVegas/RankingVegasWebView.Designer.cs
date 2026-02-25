namespace RankingVegas
{
    sealed partial class RankingVegasWebView
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            
            if (disposing && delayedInitTimer != null)
            {
                delayedInitTimer.Stop();
                delayedInitTimer.Dispose();
            }
            
            if (disposing && webView != null)
            {
                webView.Dispose();
            }
            
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.MinimumSize = new System.Drawing.Size(400, 500);
            this.DisplayName = Localization.Text("排行", "Ranking", "ランキング");
            this.Font = new System.Drawing.Font("Microsoft Yahei UI", 9);

            this.ResumeLayout(false);
        }
    }
}
