#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;

namespace RankingVegas
{
    internal class RankingVegasCommand
    {
        public const string CommandName = "RankingVegas";
        private Vegas myVegas;
        private readonly CustomCommand RankingVegasCmd = new CustomCommand(CommandCategory.View, CommandName);
        private TimeTracker globalTimeTracker;
        private RankingConfig globalConfig;
        private RankingApiClient globalApiClient;
        
        private static RankingVegasCommand instance;
        private DockableControl currentDockView;
        
        public static RankingVegasCommand Instance
        {
            get { return instance; }
        }
        
        public TimeTracker GlobalTimeTracker
        {
            get { return globalTimeTracker; }
        }
        
        public RankingConfig GlobalConfig
        {
            get { return globalConfig; }
        }
        
        public RankingApiClient GlobalApiClient
        {
            get { return globalApiClient; }
        }

#if !Sony
        public static Color UIBackColor = ScriptPortal.MediaSoftware.Skins.Skins.Colors.ButtonFace;
        public static Color UIForeColor = ScriptPortal.MediaSoftware.Skins.Skins.Colors.ButtonText;
#else
        public static Color UIBackColor = Sony.MediaSoftware.Skins.Skins.Colors.ButtonFace;
        public static Color UIForeColor = Sony.MediaSoftware.Skins.Skins.Colors.ButtonText;
#endif

        public static string VegasVersion { get; private set; }
        public static string VegasIconBase64 { get; private set; }

        internal void RankingVegasInit(Vegas Vegas, ref List<CustomCommand> CustomCommands)
        {
            instance = this;
            myVegas = Vegas;

            try
            {
                string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), CommandName);
                string fileName = "toolbar_icon.png";
                string iconPath = Path.Combine(folderPath, fileName);

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                if (!File.Exists(iconPath))
                {
                    EmbeddedResourceHelper.SaveEmbeddedResource(fileName, folderPath);
                }

                RankingVegasCmd.IconFile = iconPath;
            }

            catch { }

            RankingVegasCmd.DisplayName = CommandName;
            RankingVegasCmd.Invoked += RankingVegas_Invoked;
            RankingVegasCmd.MenuPopup += RankingVegasCommand_MenuPopup;
            CustomCommands.Add(RankingVegasCmd);
            myVegas.AppInitialized += Vegas_AppInitialized;
        }
        
        private void Vegas_AppInitialized(object sender, EventArgs e)
        {
            try
            {
                // Capture Vegas version and icon
                try
                {
                    VegasVersion = myVegas.Version;
                }
                catch
                {
                    VegasVersion = "";
                }

                try
                {
                    string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                    using (Icon appIcon = Icon.ExtractAssociatedIcon(exePath))
                    {
                        if (appIcon != null)
                        {
                            using (Bitmap bmp = appIcon.ToBitmap())
                            using (MemoryStream ms = new MemoryStream())
                            {
                                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                VegasIconBase64 = Convert.ToBase64String(ms.ToArray());
                            }
                        }
                    }
                }
                catch
                {
                    VegasIconBase64 = "";
                }

                if (globalConfig == null)
                {
                    globalConfig = RankingConfig.Load();
                }
                bool isConfigured = globalConfig.IsConfigured();

                if (!isConfigured)
                {
                    globalConfig.IsOfflineAccount = true;
                    globalConfig.Save();
                }

                if (isConfigured && string.IsNullOrEmpty(globalConfig.SessionCode))
                {
                    globalConfig.SessionCode = RankingConfig.GenerateSessionCode();
                    globalConfig.Save();
                }

                globalApiClient = isConfigured ? new RankingApiClient(globalConfig.AppId, globalConfig.AppSecret) : null;
                globalTimeTracker = new TimeTracker(myVegas, globalApiClient, globalConfig);
                globalTimeTracker.Start();
                
                if (currentDockView != null)
                {
                    if (currentDockView is RankingVegasWebView webViewDock)
                    {
                        webViewDock.RefreshFromGlobalState(globalTimeTracker, globalConfig, globalApiClient);
                    }
                    else if (currentDockView is RankingVegasWinForms winFormsDock)
                    {
                        winFormsDock.RefreshFromGlobalState(globalTimeTracker, globalConfig, globalApiClient);
                    }
                }
            }
            catch
            {
            }
        }

        private void RankingVegasCommand_MenuPopup(object sender, EventArgs e)
        {
            ((CustomCommand)sender).Checked = myVegas.FindDockView(CommandName);
        }

        private void RankingVegas_Invoked(object sender, EventArgs e)
        {
            if (!myVegas.ActivateDockView(CommandName))
            {
                DockableControl dockView;
                
                if (globalConfig == null)
                {
                    globalConfig = RankingConfig.Load();
                }

                if (globalConfig.UIStyle == UIStyleOption.WinForms)
                {
                    dockView = new RankingVegasWinForms();
                }
                else
                {
                    dockView = WebView2Detector.CreateDockView();
                    if (dockView is RankingVegasWinForms && globalConfig != null)
                    {
                        globalConfig.UIStyle = UIStyleOption.WinForms;
                        globalConfig.Save();
                    }
                }
                
                dockView.AutoLoadCommand = RankingVegasCmd;

                if (dockView is RankingVegasWebView webViewDock)
                {
                    webViewDock.MyVegas = myVegas;
                    webViewDock.GlobalTimeTracker = globalTimeTracker;
                    webViewDock.GlobalConfig = globalConfig;
                    webViewDock.GlobalApiClient = globalApiClient;
                }
                else if (dockView is RankingVegasWinForms winFormsDock)
                {
                    winFormsDock.MyVegas = myVegas;
                    winFormsDock.GlobalTimeTracker = globalTimeTracker;
                    winFormsDock.GlobalConfig = globalConfig;
                    winFormsDock.GlobalApiClient = globalApiClient;
                }

                currentDockView = dockView;
                myVegas.LoadDockView(dockView);
            }
        }
        
        internal void RegisterDockView(DockableControl dockView)
        {
            currentDockView = dockView;
            
            if (globalTimeTracker != null && globalConfig != null && globalApiClient != null)
            {
                if (dockView is RankingVegasWebView webViewDock)
                {
                    webViewDock.RefreshFromGlobalState(globalTimeTracker, globalConfig, globalApiClient);
                }
                else if (dockView is RankingVegasWinForms winFormsDock)
                {
                    winFormsDock.RefreshFromGlobalState(globalTimeTracker, globalConfig, globalApiClient);
                }
            }
        }
        
        internal void UnregisterDockView(DockableControl dockView)
        {
            if (currentDockView == dockView)
            {
                currentDockView = null;
            }
        }
        
        internal void SwitchUIStyle(UIStyleOption newStyle)
        {
            if (globalConfig == null)
                return;
            
            UIStyleOption oldStyle = globalConfig.UIStyle;
            if (oldStyle == newStyle)
                return;
            
            globalConfig.UIStyle = newStyle;
            globalConfig.Save();
            
            // Close current dock view
            if (currentDockView != null)
            {
                try
                {
                    currentDockView.Close();
                }
                catch
                {
                }
                currentDockView = null;
            }

            // Open the new style dock view
            myVegas.InvokeCommand(RankingVegasCmd);
            if (myVegas.FindDockView(CommandName, out IDockView dockView) && dockView is DockableControl dock)
            {
                dock.AutoLoadCommand = RankingVegasCmd;
            }
        }
    }
}