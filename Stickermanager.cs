using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Producktivity
{
    public class StickerManager
    {
        public static readonly string StickerDir = "stickers";
        private static readonly string MetaPath  = Path.Combine(StickerDir, "meta.json");

        private static readonly string[] SupportedExt =
            { ".png", ".jpg", ".jpeg", ".gif" };

        private List<StickerEntry> _entries = new();

        public IReadOnlyList<StickerEntry> Entries => _entries;

        public void Load()
        {
            if (!Directory.Exists(StickerDir))
                Directory.CreateDirectory(StickerDir);

            var meta = LoadMeta();

            var files = Directory.GetFiles(StickerDir)
                .Where(f => SupportedExt.Contains(
                    Path.GetExtension(f).ToLowerInvariant()))
                .OrderBy(f => f)
                .ToList();

            _entries.Clear();
            foreach (var file in files)
            {
                string key = Path.GetFileName(file);
                meta.TryGetValue(key, out var saved);
                _entries.Add(new StickerEntry
                {
                    FilePath = file,
                    FileName = key,
                    Enabled  = saved?.Enabled ?? true,
                    Stars    = saved?.Stars   ?? 0
                });
            }
        }

        public void Save()
        {
            var meta = _entries.ToDictionary(
                e => e.FileName,
                e => new StickerMeta { Enabled = e.Enabled, Stars = e.Stars });

            string json = JsonSerializer.Serialize(meta,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(MetaPath, json);
        }

        public StickerEntry Import(string sourcePath)
        {
            string ext  = Path.GetExtension(sourcePath).ToLowerInvariant();
            if (!SupportedExt.Contains(ext)) return null;

            string dest = Path.Combine(StickerDir, Path.GetFileName(sourcePath));
            int idx = 1;
            while (File.Exists(dest))
            {
                string name = Path.GetFileNameWithoutExtension(sourcePath);
                dest = Path.Combine(StickerDir, $"{name}_{idx++}{ext}");
            }
            File.Copy(sourcePath, dest);

            var entry = new StickerEntry
            {
                FilePath = dest,
                FileName = Path.GetFileName(dest),
                Enabled  = true,
                Stars    = 0
            };
            _entries.Add(entry);
            Save();
            return entry;
        }

        public void Delete(StickerEntry entry)
        {
            _entries.Remove(entry);
            entry.DisposeImage();
            try { File.Delete(entry.FilePath); } catch { }
            Save();
        }

        public StickerEntry PickRandom(Random rnd)
        {
            var pool = _entries.Where(e => e.Enabled).ToList();
            if (pool.Count == 0) return null;

            double total = pool.Sum(e => StarWeight(e.Stars));
            double roll  = rnd.NextDouble() * total;
            double acc   = 0;
            foreach (var e in pool)
            {
                acc += StarWeight(e.Stars);
                if (roll < acc) return e;
            }
            return pool[pool.Count - 1];
        }

        public static double StarWeight(int stars) => stars switch
        {
            1 => 1.5,
            2 => 3.0,
            3 => 5.0,
            _ => 1.0
        };

        public bool HasEnabled => _entries.Any(e => e.Enabled);

        private Dictionary<string, StickerMeta> LoadMeta()
        {
            if (!File.Exists(MetaPath)) return new();
            try
            {
                string json = File.ReadAllText(MetaPath);
                return JsonSerializer.Deserialize<Dictionary<string, StickerMeta>>(json)
                       ?? new();
            }
            catch { return new(); }
        }

        private class StickerMeta
        {
            public bool Enabled { get; set; }
            public int  Stars   { get; set; }
        }
    }

    public class StickerEntry
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public bool   Enabled  { get; set; }
        public int    Stars    { get; set; }

        private Image _cachedImage;

        public Image GetImage()
        {
            if (_cachedImage != null) return _cachedImage;
            try { _cachedImage = Image.FromFile(FilePath); }
            catch { _cachedImage = null; }
            return _cachedImage;
        }

        public void DisposeImage()
        {
            _cachedImage?.Dispose();
            _cachedImage = null;
        }

        public Size GetDisplaySize(int minPx = 80, int maxPx = 200)
        {
            var img = GetImage();
            if (img == null) return new Size(maxPx, maxPx);

            float w = img.Width, h = img.Height;
            float scale = 1f;
            float longer = Math.Max(w, h);
            if (longer > maxPx) scale = maxPx / longer;
            float shorter = Math.Min(w, h) * scale;
            if (shorter < minPx && longer > 0)
                scale = Math.Max(scale, minPx / Math.Min(w, h));
            scale = Math.Min(scale, maxPx / longer);

            return new Size(
                Math.Max(1, (int)(w * scale)),
                Math.Max(1, (int)(h * scale)));
        }
    }
}