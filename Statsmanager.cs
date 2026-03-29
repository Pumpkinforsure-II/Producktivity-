using System;
using System.IO;
using System.Text.Json;

namespace Producktivity
{
    public class StatsData
    {
        public int    TotalTextMsgs    { get; set; }
        public int    TotalStickerMsgs { get; set; }
        public int    TotalBlocks      { get; set; }
        public double TotalRuntimeSecs { get; set; }
    }

    public class StatsManager
    {
        private static readonly string SavePath = "stats.json";

        private StatsData _data = new();
        private DateTime  _sessionStart;

        // ── 公开属性 ──────────────────────────────────────────
        public int TotalTextMsgs    => _data.TotalTextMsgs;
        public int TotalStickerMsgs => _data.TotalStickerMsgs;
        public int TotalBlocks      => _data.TotalBlocks;

        // ── 加载 / 保存 ─────────────────────────────────────
        public void Load()
        {
            _sessionStart = DateTime.Now;
            if (!File.Exists(SavePath)) return;
            try
            {
                string json = File.ReadAllText(SavePath);
                _data = JsonSerializer.Deserialize<StatsData>(json) ?? new();
            }
            catch { _data = new StatsData(); }
        }

        public void Save()
        {
            _data.TotalRuntimeSecs += (DateTime.Now - _sessionStart).TotalSeconds;
            _sessionStart = DateTime.Now;
            try
            {
                File.WriteAllText(SavePath,
                    JsonSerializer.Serialize(_data,
                        new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        // ── 计数 ─────────────────────────────────────────────
        public void AddTextMsg()    => _data.TotalTextMsgs++;
        public void AddStickerMsg() => _data.TotalStickerMsgs++;
        public void AddBlock()      => _data.TotalBlocks++;

        // ── 格式化运行时长 ───────────────────────────────────
        public string GetFormattedTotalRuntime()
        {
            double secs = _data.TotalRuntimeSecs
                        + (DateTime.Now - _sessionStart).TotalSeconds;
            var ts = TimeSpan.FromSeconds(secs);

            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}小时 {ts.Minutes}分钟 {ts.Seconds}秒";
            if (ts.TotalMinutes >= 1)
                return $"{(int)ts.TotalMinutes}分钟 {ts.Seconds}秒";
            return $"{(int)ts.TotalSeconds}秒";
        }
    }
}