﻿using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLib.Minecraft.GameResource
{
    public class AssetList : IReadOnlyDictionary<string, AssetIndex>
    {
        public AssetList(Model model)
        {
            if (model is null)
                throw new ArgumentNullException(nameof(model));

            _items = new();
            _items = model.objects.ToDictionary(item => item.Key, item => new AssetIndex(item.Value));
        }

        private readonly Dictionary<string, AssetIndex> _items;

        public IEnumerable<string> Keys => _items.Keys;

        public IEnumerable<AssetIndex> Values => _items.Values;

        public int Count => _items.Count;

        public AssetIndex this[string key] => _items[key];
        
        public static async Task<AssetList> DownloadAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException($"“{nameof(url)}”不能为 null 或空。", nameof(url));

            byte[] bytes = await DownloadUtil.DownloadBytesAsync(url);
            string text = Encoding.UTF8.GetString(bytes);
            var model = JsonConvert.DeserializeObject<AssetList.Model>(text) ?? throw new FormatException();
            return new(model);
        }

        public bool ContainsKey(string key)
        {
            return _items.ContainsKey(key);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out AssetIndex value)
        {
            return _items.TryGetValue(key, out value);
        }

        public IEnumerator<KeyValuePair<string, AssetIndex>> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_items).GetEnumerator();
        }

        public class Model
        {
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
            public Dictionary<string, AssetIndex.Model> objects { get; set; }
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        }
    }
}