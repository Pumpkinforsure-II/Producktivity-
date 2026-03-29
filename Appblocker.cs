using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace Producktivity
{
    public class AppBlocker
    {
        // ── 拦截列表 ──────────────────────────────────────────
        public static List<string> BlockedApps { get; private set; } = new();
        private static readonly string ConfigPath = "blocked_apps.json";

        // ── 拦截次数（由 StatsManager 持久化） ────────────────
        private StatsManager _stats;

        // ── 计时器 ────────────────────────────────────────────
        private System.Windows.Forms.Timer _timer;
        private Form1 _duck;
        private readonly Random _rnd = new();

        // ── 34 条劝阻消息 ────────────────────────────────────
        private static readonly string[] DissuadeTexts =
        {
            // 搞怪风
            "嘎！检测到你想偷偷玩 {0}，已经帮你关掉了！专心工作吧～",
            "嘎嘎！{0} 被我拦截了！你可是要做大事的人！",
            "别玩 {0} 了！先把正事做完再说！",
            "检测到 {0} 启动，已自动关闭。你可以的，再坚持一下！",
            "嘎～{0} 已被阻止。摸鱼的时间用来学习不好吗？",
            "{0}？不行不行！乖乖干活！我相信你！",
            "嘎！抓到你了！想偷偷打开 {0}？已经被我光速关掉啦～",
            "嘎嘎！{0} 休想得逞！快去干活，待会再玩！",
            "喂喂喂！{0} 被我拦下啦！你的小聪明可逃不过我的眼睛～",
            "哈哈！{0} 想偷跑？没门！先搞定正事再说！",
            "嘎！{0} 已经被我封印了！专心模式启动！",
            "别以为我没看见！{0} 被我抓住啦，快回到工作中去！",
            "嘎嘎嘎！{0} 想偷偷冒头？不行不行，先忙完再说！",
            "嘿！{0} 被我按住了！你可比它厉害多了，加油！",
            "嘎！想玩 {0}？先过我这一关！——好啦，快去工作！",
            "抓到啦！{0} 已经被我关掉咯，今天的你可是要干大事的人！",

            // 正式风
            "检测到 {0} 启动，已自动关闭。请保持专注。",
            "{0} 已被拦截。当前不宜分心，继续当前任务。",
            "系统已阻止 {0}。请确认当前优先级后再进行操作。",
            "{0} 不可用。建议先完成手头工作。",
            "已拦截 {0}。专注是效率的前提。",
            "{0} 已被屏蔽。请继续保持当前工作状态。",
            "检测到干扰项 {0}，已自动处理。请继续。",
            "{0} 无法启动。建议集中精力完成当前任务。",
            "系统已阻止 {0}。如有需要，请在休息时间再试。",
            "{0} 已被拦截。请保持专注，持续推进。",

            // 温柔风
            "乖乖～{0} 被我轻轻关掉啦，我们先把重要的事情做完好不好？",
            "嘘～{0} 暂时不能玩哦，先把手头的事完成，我会一直陪着你的。",
            "我帮你把 {0} 挡住啦，不是不让你玩，是相信你可以先做完正事～",
            "辛苦啦，但 {0} 现在还不能打开哦，再坚持一下，我陪着你。",
            "别急哦，{0} 我先帮你保管一下，你专心做完这件事，我会很为你骄傲的。",
            "知道你想放松，但 {0} 现在不合适哦，我们一起把眼前的事情做好吧。",
            "我帮你挡掉了 {0}，不是限制你，是想让你不被干扰地完成重要的事～",
            "乖～{0} 先放一边，我相信你很快就能搞定手上的任务，然后好好休息。",
        };

        // ── 配置加载 / 保存 ──────────────────────────────────
        public static void LoadConfig()
        {
            if (!File.Exists(ConfigPath))
            {
                BlockedApps = new List<string> { "steam" };
                Save();
                return;
            }
            try
            {
                string json = File.ReadAllText(ConfigPath);
                BlockedApps = JsonSerializer.Deserialize<List<string>>(json) ?? new();
            }
            catch { BlockedApps = new List<string> { "steam" }; }
        }

        public static void Save()
        {
            try
            {
                File.WriteAllText(ConfigPath,
                    JsonSerializer.Serialize(BlockedApps,
                        new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        // ── 启动 / 停止 ─────────────────────────────────────
        public void Start(Form1 duck)
        {
            _duck  = duck;
            _timer = new System.Windows.Forms.Timer { Interval = 3000 };
            _timer.Tick += OnTick;
            _timer.Start();
        }

        public void SetStatsManager(StatsManager stats) => _stats = stats;

        public void Stop() => _timer?.Stop();

        // ── 扫描 Tick ────────────────────────────────────────
        private void OnTick(object sender, EventArgs e)
        {
            if (BlockedApps.Count == 0) return;

            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    string name = proc.ProcessName.ToLowerInvariant();
                    if (BlockedApps.Any(b => name.Contains(b)))
                    {
                        proc.Kill();
                        string msg = string.Format(
                            DissuadeTexts[_rnd.Next(DissuadeTexts.Length)],
                            proc.ProcessName);
                        _duck?.SayToChat(msg);

                        _stats?.AddBlock();
                    }
                }
                catch { }
            }
        }
    }
}