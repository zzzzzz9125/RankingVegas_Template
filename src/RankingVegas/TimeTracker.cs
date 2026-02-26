#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.Timers;
using System.Diagnostics;

namespace RankingVegas
{
    public enum ActivityCategory
    {
        None,
        Markers,
        Track,
        TrackState,
        TrackEvent
    }

    public class TimeTracker
    {
        private readonly Vegas vegas;
        private readonly RankingApiClient apiClient;
        private readonly RankingConfig config;
        
        private Stopwatch totalStopwatch;
        private Stopwatch renderStopwatch;
        private Stopwatch idleStopwatch;
        private Timer idleCheckTimer;
        private Timer reportTimer;
        
        private const int IdleThresholdSeconds = 10;
        private const int MonotonicActivityWindowSeconds = 60;
        
        private long lastReportedMilliseconds = 0;
        private long lastOfflineRecordedMilliseconds = 0;
        private bool isPaused = false;
        private bool isRendering = false;
        private bool isOfflineAccount = false;
        private string currentStatus = Localization.Text("等待同步", "Waiting for sync", "同期待ち");
        private StatusKind currentStatusKind = StatusKind.Active;
        
        // Activity tracking for monotonic operation detection
        private ActivityCategory lastActivityCategory = ActivityCategory.None;
        private DateTime activityWindowStart = DateTime.MinValue;
        private int activityCountInWindow = 0;
        private const int MonotonicActivityThreshold = 5;
        
        public event EventHandler<TimeSpan> TimeUpdated;
        public event EventHandler<string> StatusChanged;
        
        public TimeTracker(Vegas vegas, RankingApiClient apiClient, RankingConfig config)
        {
            this.vegas = vegas;
            this.apiClient = apiClient;
            this.config = config;
            isOfflineAccount = config != null && config.IsOfflineAccount;
            
            totalStopwatch = new Stopwatch();
            renderStopwatch = new Stopwatch();
            idleStopwatch = new Stopwatch();
            
            idleCheckTimer = new Timer(1000);
            idleCheckTimer.Elapsed += IdleCheckTimer_Elapsed;
            
            int reportInterval = GetReportIntervalSeconds();
            reportTimer = new Timer(reportInterval * 1000);
            reportTimer.Elapsed += ReportTimer_Elapsed;
        }
        
        private int GetReportIntervalSeconds()
        {
            if (config == null)
            {
                return isOfflineAccount ? RankingConfig.DefaultOfflineSaveIntervalSeconds : RankingConfig.DefaultOnlineReportIntervalSeconds;
            }
            
            if (isOfflineAccount)
            {
                return Math.Max(config.OfflineSaveIntervalSeconds, RankingConfig.MinOfflineSaveIntervalSeconds);
            }
            else
            {
                return Math.Max(config.OnlineReportIntervalSeconds, RankingConfig.MinOnlineReportIntervalSeconds);
            }
        }
        
        public void UpdateReportInterval()
        {
            int newInterval = GetReportIntervalSeconds();
            reportTimer.Interval = newInterval * 1000;
        }
        
        public void Start()
        {
            RegisterVegasEvents();
            
            totalStopwatch.Start();
            idleStopwatch.Start();
            idleCheckTimer.Start();
            reportTimer.Start();
            
            SetStatus(StatusKind.Active, Localization.Text("计时中", "Timing", "計測中"));
        }
        
        public void Stop()
        {
            UnregisterVegasEvents();
            
            totalStopwatch.Stop();
            renderStopwatch.Stop();
            idleStopwatch.Stop();
            idleCheckTimer.Stop();
            reportTimer.Stop();
            
            ReportAccumulatedTime();
        }
        
        private void RegisterVegasEvents()
        {
            vegas.TrackCountChanged += Vegas_TrackActivity;
            vegas.TrackEventCountChanged += Vegas_TrackEventActivity;
            vegas.TrackEventStateChanged += Vegas_TrackEventActivity;
            vegas.TrackEventTimeChanged += Vegas_TrackEventActivity;
            vegas.MarkersChanged += Vegas_MarkersActivity;
            vegas.TrackStateChanged += Vegas_TrackStateActivity;
            
            vegas.RenderStarted += Vegas_RenderStarted;
            vegas.RenderFinished += Vegas_RenderFinished;
        }
        
        private void UnregisterVegasEvents()
        {
            vegas.TrackCountChanged -= Vegas_TrackActivity;
            vegas.TrackEventCountChanged -= Vegas_TrackEventActivity;
            vegas.TrackEventStateChanged -= Vegas_TrackEventActivity;
            vegas.TrackEventTimeChanged -= Vegas_TrackEventActivity;
            vegas.MarkersChanged -= Vegas_MarkersActivity;
            vegas.TrackStateChanged -= Vegas_TrackStateActivity;
            
            vegas.RenderStarted -= Vegas_RenderStarted;
            vegas.RenderFinished -= Vegas_RenderFinished;
        }
        
        private void Vegas_MarkersActivity(object sender, EventArgs e)
        {
            HandleActivity(ActivityCategory.Markers);
        }
        
        private void Vegas_TrackActivity(object sender, EventArgs e)
        {
            HandleActivity(ActivityCategory.Track);
        }

        private void Vegas_TrackStateActivity(object sender, EventArgs e)
        {
            HandleActivity(ActivityCategory.TrackState);
        }

        private void Vegas_TrackEventActivity(object sender, EventArgs e)
        {
            HandleActivity(ActivityCategory.TrackEvent);
        }
        
        private void HandleActivity(ActivityCategory category)
        {
            DateTime now = DateTime.Now;
            
            if ((now - activityWindowStart).TotalSeconds > MonotonicActivityWindowSeconds)
            {
                activityWindowStart = now;
                lastActivityCategory = category;
                activityCountInWindow = 1;
            }
            else
            {
                if (category == lastActivityCategory)
                {
                    activityCountInWindow++;
                    
                    if (activityCountInWindow >= MonotonicActivityThreshold)
                    {
                        return;
                    }
                }
                else
                {
                    lastActivityCategory = category;
                    activityCountInWindow = 1;
                }
            }
            
            idleStopwatch.Restart();
            
            if (isPaused)
            {
                isPaused = false;
                totalStopwatch.Start();
                SetStatus(StatusKind.Active, Localization.Text("计时中", "Timing", "計測中"));
            }
        }
        
        private void Vegas_RenderStarted(object sender, EventArgs e)
        {
            isRendering = true;
            
            totalStopwatch.Stop();
            renderStopwatch.Restart();
            
            SetStatus(StatusKind.Rendering, Localization.Text("渲染中", "Rendering", "レンダリング中"));
        }
        
        private void Vegas_RenderFinished(object sender, RenderStatusEventArgs e)
        {
            isRendering = false;
            renderStopwatch.Stop();
            idleStopwatch.Restart();
            
            activityWindowStart = DateTime.MinValue;
            lastActivityCategory = ActivityCategory.None;
            activityCountInWindow = 0;

            if (!isPaused)
            {
                totalStopwatch.Start();
                SetStatus(StatusKind.Active, Localization.Text("计时中", "Timing", "計測中"));
            }
            else
            {
                SetStatus(StatusKind.Idle, Localization.Text("挂机暂停中", "Idle paused", "アイドル一時停止中"));
            }
        }
        
        private void IdleCheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Script detection not working
            /*
            if (!isRendering)
            {
                try
                {
                    if (Vegas.COM != null)
                    {
                        bool isInScript = Vegas.COM.IsInScript() == 0;
                        if (isInScript)
                        {
                            if (totalStopwatch.IsRunning)
                            {
                                totalStopwatch.Stop();
                            }
                            OnTimeUpdated(GetTotalTime());
                            return;
                        }
                    }
                }
                catch { }
            }
            */

            if (!isPaused && !isRendering && !totalStopwatch.IsRunning)
            {
                totalStopwatch.Start();
            }

            if (!isPaused && !isRendering && idleStopwatch.Elapsed.TotalSeconds >= IdleThresholdSeconds)
            {
                isPaused = true;
                totalStopwatch.Stop();
                SetStatus(StatusKind.Idle, Localization.Text("挂机暂停中", "Idle paused", "アイドル一時停止中"));
            }
            
            OnTimeUpdated(GetTotalTime());
        }
        
        private void ReportTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ReportAccumulatedTime();
        }
        
        private void ReportAccumulatedTime()
        {
            long currentMilliseconds = totalStopwatch.ElapsedMilliseconds;

            if (isOfflineAccount || apiClient == null || config == null || string.IsNullOrEmpty(config.SessionCode))
            {
                SaveOfflineAccumulatedTime(currentMilliseconds);
                return;
            }

            long deltaMilliseconds = currentMilliseconds - lastReportedMilliseconds;
            int deltaSeconds = (int)(deltaMilliseconds / 1000);
            
            if (deltaSeconds >= 60 && deltaSeconds <= 3600)
            {
                try
                {
                    var response = apiClient.ReportDuration(config.SessionCode, deltaSeconds);
                    
                    if (response.Success)
                    {
                        lastReportedMilliseconds = currentMilliseconds;
                        SetStatus(StatusKind.Active, Localization.Format("上报成功: {0} 秒", "Report sent: {0} s", "報告送信: {0} 秒", deltaSeconds));
                        
                        System.Threading.Tasks.Task.Delay(2000).ContinueWith(_ =>
                        {
                            if (isPaused)
                            {
                                SetStatus(StatusKind.Idle, Localization.Text("挂机暂停中", "Idle paused", "アイドル一時停止中"));
                            }
                            else if (isRendering)
                            {
                                SetStatus(StatusKind.Rendering, Localization.Text("渲染中", "Rendering", "レンダリング中"));
                            }
                            else
                            {
                                SetStatus(StatusKind.Active, Localization.Text("计时中", "Timing", "計測中"));
                            }
                        });
                    }
                    else
                    {
                        SetStatus(StatusKind.Error, Localization.Format("上报失败: {0}", "Report failed: {0}", "報告失敗: {0}", response.Message));
                    }
                }
                catch (Exception ex)
                {
                    SetStatus(StatusKind.Error, Localization.Format("上报异常: {0}", "Report error: {0}", "報告エラー: {0}", ex.Message));
                }
            }
        }
        
        public TimeSpan GetTotalTime()
        {
            return totalStopwatch.Elapsed;
        }
        
        public TimeSpan GetRenderTime()
        {
            return renderStopwatch.Elapsed;
        }
        
        public bool IsRendering()
        {
            return isRendering;
        }
        
        public string GetCurrentStatus()
        {
            return currentStatus;
        }

        public StatusKind GetCurrentStatusKind()
        {
            return currentStatusKind;
        }

        public void RefreshStatus()
        {
            if (isRendering)
            {
                SetStatus(StatusKind.Rendering, Localization.Text("渲染中", "Rendering", "レンダリング中"));
            }
            else if (isPaused)
            {
                SetStatus(StatusKind.Idle, Localization.Text("挂机暂停中", "Idle paused", "アイドル一時停止中"));
            }
            else
            {
                SetStatus(StatusKind.Active, Localization.Text("计时中", "Timing", "計測中"));
            }
        }
        
        protected virtual void OnTimeUpdated(TimeSpan time)
        {
            TimeUpdated?.Invoke(this, time);
        }
        
        protected virtual void OnStatusChanged(string status)
        {
            currentStatus = status;
            StatusChanged?.Invoke(this, status);
        }
        
        private void SetStatus(StatusKind kind, string status)
        {
            currentStatusKind = kind;
            OnStatusChanged(status);
        }
        
        public void Dispose()
        {
            Stop();
            
            if (idleCheckTimer != null)
            {
                idleCheckTimer.Dispose();
                idleCheckTimer = null;
            }
            
            if (reportTimer != null)
            {
                reportTimer.Dispose();
                reportTimer = null;
            }
        }
        
        public void SetOfflineMode(bool isOffline)
        {
            if (isOfflineAccount == isOffline)
            {
                return;
            }

            long currentMilliseconds = totalStopwatch.ElapsedMilliseconds;

            if (!isOffline)
            {
                SaveOfflineAccumulatedTime(currentMilliseconds);
            }

            isOfflineAccount = isOffline;

            if (config != null)
            {
                config.IsOfflineAccount = isOffline;
                config.Save();
            }

            if (isOffline)
            {
                lastOfflineRecordedMilliseconds = lastReportedMilliseconds > 0 ? lastReportedMilliseconds : currentMilliseconds;
            }
            else
            {
                lastReportedMilliseconds = currentMilliseconds;
            }
            
            UpdateReportInterval();
        }
        
        public int GetOfflineTotalDurationSeconds()
        {
            if (config == null)
            {
                return 0;
            }

            if (!isOfflineAccount)
            {
                return config.OfflineTotalSeconds;
            }

            long currentMilliseconds = totalStopwatch.ElapsedMilliseconds;
            int deltaSeconds = (int)((currentMilliseconds - lastOfflineRecordedMilliseconds) / 1000);
            if (deltaSeconds < 0)
            {
                deltaSeconds = 0;
            }

            return config.OfflineTotalSeconds + deltaSeconds;
        }

        private void SaveOfflineAccumulatedTime(long currentMilliseconds)
        {
            if (config == null)
            {
                return;
            }

            long deltaMilliseconds = currentMilliseconds - lastOfflineRecordedMilliseconds;
            int deltaSeconds = (int)(deltaMilliseconds / 1000);

            if (deltaSeconds <= 0)
            {
                return;
            }

            config.OfflineTotalSeconds += deltaSeconds;
            lastOfflineRecordedMilliseconds = currentMilliseconds;
            config.Save();
        }
    }
}
