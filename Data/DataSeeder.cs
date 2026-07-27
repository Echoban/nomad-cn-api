using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NomadCN.Api.Models;

namespace NomadCN.Api.Data;

/// <summary>
/// 数据播种器 - 在数据库为空时自动填充城市数据和测试用户
/// 从 wwwroot 前端文件中提取城市名、坐标、房价、深度描述等数据
/// </summary>
public static class DataSeeder
{
    private static readonly string WwwrootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    // ===== 城市等级定义 =====
    private static readonly Dictionary<string, string[]> TierCities = new()
    {
        ["一线"] = new[] { "北京", "上海", "广州", "深圳" },
        ["新一线"] = new[]
        {
            "成都", "杭州", "重庆", "武汉", "西安", "苏州", "南京", "天津", "长沙", "东莞",
            "宁波", "青岛", "合肥", "佛山", "郑州", "昆明", "沈阳", "哈尔滨", "济南", "无锡",
            "厦门", "福州", "温州", "大连", "长春", "石家庄", "南宁", "常州", "泉州", "南通",
            "嘉兴", "太原", "徐州", "贵阳", "金华", "珠海", "惠州", "绍兴", "台州", "烟台"
        },
    };

    // ===== 区域映射（参考 seed_data.py 中的 REGION_MAP） =====
    private static readonly Dictionary<string, string> RegionMap = new()
    {
        // 华北
        ["北京"] = "华北", ["天津"] = "华北", ["石家庄"] = "华北", ["唐山"] = "华北", ["保定"] = "华北",
        ["沧州"] = "华北", ["廊坊"] = "华北", ["邢台"] = "华北", ["张家口"] = "华北", ["承德"] = "华北",
        ["衡水"] = "华北", ["邯郸"] = "华北", ["太原"] = "华北", ["大同"] = "华北", ["阳泉"] = "华北",
        // 东北
        ["沈阳"] = "东北", ["大连"] = "东北", ["鞍山"] = "东北", ["抚顺"] = "东北", ["本溪"] = "东北",
        ["丹东"] = "东北", ["锦州"] = "东北", ["营口"] = "东北", ["辽阳"] = "东北", ["盘锦"] = "东北",
        ["长春"] = "东北", ["吉林"] = "东北", ["四平"] = "东北", ["通化"] = "东北", ["松原"] = "东北",
        ["哈尔滨"] = "东北", ["齐齐哈尔"] = "东北", ["鸡西"] = "东北", ["鹤岗"] = "东北", ["双鸭山"] = "东北",
        ["大庆"] = "东北", ["佳木斯"] = "东北", ["牡丹江"] = "东北",
        // 华东
        ["上海"] = "华东", ["南京"] = "华东", ["无锡"] = "华东", ["徐州"] = "华东", ["常州"] = "华东",
        ["苏州"] = "华东", ["南通"] = "华东", ["扬州"] = "华东", ["镇江"] = "华东", ["泰州"] = "华东",
        ["杭州"] = "华东", ["宁波"] = "华东", ["温州"] = "华东", ["嘉兴"] = "华东", ["湖州"] = "华东",
        ["绍兴"] = "华东", ["金华"] = "华东", ["衢州"] = "华东", ["舟山"] = "华东", ["台州"] = "华东",
        ["合肥"] = "华东", ["芜湖"] = "华东", ["蚌埠"] = "华东", ["马鞍山"] = "华东", ["安庆"] = "华东",
        ["福州"] = "华东", ["厦门"] = "华东", ["泉州"] = "华东", ["漳州"] = "华东", ["南平"] = "华东",
        ["南昌"] = "华东", ["九江"] = "华东", ["上饶"] = "华东", ["吉安"] = "华东", ["宜春"] = "华东",
        ["济南"] = "华东", ["青岛"] = "华东", ["烟台"] = "华东", ["潍坊"] = "华东", ["济宁"] = "华东",
        ["临沂"] = "华东", ["威海"] = "华东", ["日照"] = "华东", ["德州"] = "华东", ["聊城"] = "华东",
        // 华中
        ["郑州"] = "华中", ["洛阳"] = "华中", ["开封"] = "华中", ["南阳"] = "华中", ["信阳"] = "华中",
        ["商丘"] = "华中", ["新乡"] = "华中", ["许昌"] = "华中", ["平顶山"] = "华中", ["安阳"] = "华中",
        ["焦作"] = "华中", ["周口"] = "华中", ["驻马店"] = "华中", ["漯河"] = "华中", ["濮阳"] = "华中",
        ["武汉"] = "华中", ["宜昌"] = "华中", ["襄阳"] = "华中", ["荆州"] = "华中", ["黄冈"] = "华中",
        ["十堰"] = "华中", ["孝感"] = "华中", ["荆门"] = "华中", ["鄂州"] = "华中", ["黄石"] = "华中",
        ["长沙"] = "华中", ["株洲"] = "华中", ["湘潭"] = "华中", ["衡阳"] = "华中", ["岳阳"] = "华中",
        ["常德"] = "华中", ["郴州"] = "华中", ["邵阳"] = "华中", ["益阳"] = "华中", ["永州"] = "华中",
        // 华南
        ["广州"] = "华南", ["深圳"] = "华南", ["珠海"] = "华南", ["佛山"] = "华南", ["东莞"] = "华南",
        ["中山"] = "华南", ["惠州"] = "华南", ["汕头"] = "华南", ["湛江"] = "华南", ["肇庆"] = "华南",
        ["江门"] = "华南", ["茂名"] = "华南", ["梅州"] = "华南", ["韶关"] = "华南", ["清远"] = "华南",
        ["南宁"] = "华南", ["柳州"] = "华南", ["桂林"] = "华南", ["梧州"] = "华南", ["北海"] = "华南",
        ["海口"] = "华南", ["三亚"] = "华南", ["儋州"] = "华南",
        // 西南
        ["重庆"] = "西南", ["成都"] = "西南", ["绵阳"] = "西南", ["德阳"] = "西南", ["南充"] = "西南",
        ["宜宾"] = "西南", ["泸州"] = "西南", ["乐山"] = "西南", ["自贡"] = "西南", ["内江"] = "西南",
        ["贵阳"] = "西南", ["遵义"] = "西南", ["六盘水"] = "西南", ["安顺"] = "西南", ["毕节"] = "西南",
        ["昆明"] = "西南", ["曲靖"] = "西南", ["玉溪"] = "西南", ["保山"] = "西南", ["昭通"] = "西南",
        ["丽江"] = "西南", ["普洱"] = "西南", ["临沧"] = "西南", ["大理"] = "西南",
        ["拉萨"] = "西南", ["日喀则"] = "西南", ["昌都"] = "西南", ["林芝"] = "西南",
        // 西北
        ["西安"] = "西北", ["宝鸡"] = "西北", ["咸阳"] = "西北", ["渭南"] = "西北", ["延安"] = "西北",
        ["汉中"] = "西北", ["榆林"] = "西北", ["安康"] = "西北", ["商洛"] = "西北",
        ["兰州"] = "西北", ["天水"] = "西北", ["白银"] = "西北", ["庆阳"] = "西北", ["酒泉"] = "西北",
        ["武威"] = "西北", ["张掖"] = "西北", ["平凉"] = "西北", ["定西"] = "西北",
        ["西宁"] = "西北", ["海东"] = "西北",
        ["银川"] = "西北", ["石嘴山"] = "西北", ["吴忠"] = "西北", ["固原"] = "西北", ["中卫"] = "西北",
        ["乌鲁木齐"] = "西北", ["克拉玛依"] = "西北", ["吐鲁番"] = "西北", ["哈密"] = "西北",
        // 内蒙
        ["呼和浩特"] = "内蒙", ["包头"] = "内蒙", ["赤峰"] = "内蒙", ["通辽"] = "内蒙",
        ["鄂尔多斯"] = "内蒙", ["呼伦贝尔"] = "内蒙", ["巴彦淖尔"] = "内蒙", ["乌兰察布"] = "内蒙",
    };

    // ===== 标签池 =====
    private static readonly string[] TagsPool =
    {
        "自然风光", "美食之都", "咖啡文化", "历史古都", "海滨城市", "科技之城",
        "避暑胜地", "丝路风情", "民族风情", "登山", "茶文化", "冰雪", "温泉",
        "草原", "湿地", "侨乡", "边关", "运河", "红色旅游", "水果之乡"
    };

    // ===== 气候类型池 =====
    private static readonly string[] ClimateTypes =
    {
        "亚热带季风", "温带季风", "热带季风", "高原气候", "温带大陆性", "地中海"
    };

    // ===== 每个等级的数据范围 =====
    private static readonly Dictionary<string, Dictionary<string, (int Min, int Max)>> TierData = new()
    {
        ["一线"] = new()
        {
            ["cost"] = (6000, 9000), ["rent"] = (3500, 6000), ["food"] = (1500, 2500),
            ["transport"] = (400, 800), ["internet"] = (85, 98), ["safety"] = (70, 85),
            ["healthcare"] = (80, 95), ["walkability"] = (75, 90), ["nightlife"] = (80, 95),
            ["coffee"] = (80, 95), ["coworking"] = (80, 95), ["air"] = (40, 80), ["temp"] = (12, 22),
        },
        ["新一线"] = new()
        {
            ["cost"] = (4000, 6000), ["rent"] = (2000, 4000), ["food"] = (1000, 1800),
            ["transport"] = (250, 500), ["internet"] = (75, 92), ["safety"] = (75, 90),
            ["healthcare"] = (70, 88), ["walkability"] = (65, 85), ["nightlife"] = (65, 85),
            ["coffee"] = (65, 85), ["coworking"] = (70, 88), ["air"] = (35, 75), ["temp"] = (14, 24),
        },
        ["二线"] = new()
        {
            ["cost"] = (3000, 5000), ["rent"] = (1500, 3000), ["food"] = (800, 1500),
            ["transport"] = (200, 400), ["internet"] = (65, 85), ["safety"] = (75, 92),
            ["healthcare"] = (60, 80), ["walkability"] = (55, 78), ["nightlife"] = (50, 75),
            ["coffee"] = (50, 75), ["coworking"] = (55, 75), ["air"] = (30, 70), ["temp"] = (13, 25),
        },
        ["三线"] = new()
        {
            ["cost"] = (2500, 4000), ["rent"] = (1000, 2500), ["food"] = (600, 1200),
            ["transport"] = (150, 300), ["internet"] = (55, 78), ["safety"] = (78, 95),
            ["healthcare"] = (50, 72), ["walkability"] = (45, 70), ["nightlife"] = (35, 60),
            ["coffee"] = (35, 60), ["coworking"] = (40, 65), ["air"] = (25, 65), ["temp"] = (12, 26),
        },
        ["四线"] = new()
        {
            ["cost"] = (2000, 3500), ["rent"] = (800, 1800), ["food"] = (500, 1000),
            ["transport"] = (120, 250), ["internet"] = (45, 70), ["safety"] = (80, 96),
            ["healthcare"] = (40, 65), ["walkability"] = (40, 65), ["nightlife"] = (25, 50),
            ["coffee"] = (25, 50), ["coworking"] = (30, 55), ["air"] = (20, 60), ["temp"] = (11, 27),
        },
        ["五线"] = new()
        {
            ["cost"] = (1500, 3000), ["rent"] = (600, 1500), ["food"] = (400, 800),
            ["transport"] = (100, 200), ["internet"] = (35, 62), ["safety"] = (82, 97),
            ["healthcare"] = (35, 58), ["walkability"] = (35, 60), ["nightlife"] = (20, 45),
            ["coffee"] = (20, 45), ["coworking"] = (25, 50), ["air"] = (15, 55), ["temp"] = (10, 28),
        },
    };

    // 非一线/新一线城市的随机等级池
    private static readonly string[] OtherTiers = { "二线", "三线", "四线", "五线" };

    // 测试用户头像颜色池
    private static readonly string[] UserAvatarColors = { "#00d9a3", "#ff6b6b", "#4ecdc4", "#ffe66d", "#a8e6cf" };

    /// <summary>
    /// 执行数据播种：在数据库为空时自动填充城市数据和测试用户
    /// </summary>
    public static async Task SeedAsync(AppDbContext db)
    {
        // 如果已有城市数据则跳过
        if (await db.Cities.AnyAsync())
        {
            return;
        }

        // 检查 wwwroot 目录是否存在
        if (!Directory.Exists(WwwrootPath))
        {
            Console.WriteLine("[DataSeeder] wwwroot 目录不存在，跳过数据播种: " + WwwrootPath);
            return;
        }

        // 使用固定种子保证数据一致性
        var rng = new Random(42);

        // ===== 解析前端文件 =====
        var cityNames = ParseCityNames();
        var coords = ParseCoords();
        var housingPrices = ParseHousingPrices();
        var deepData = ParseDeepData();

        if (cityNames.Count == 0)
        {
            Console.WriteLine("[DataSeeder] 未提取到城市名，跳过数据播种");
            return;
        }

        Console.WriteLine($"[DataSeeder] 提取到 {cityNames.Count} 个城市名，开始生成数据...");

        // ===== 生成城市数据 =====
        foreach (var name in cityNames)
        {
            var tier = GetTier(name, rng);
            var region = GetRegion(name);
            var td = TierData[tier];

            // 坐标
            coords.TryGetValue(name, out var coord);
            var lat = coord.Lat;
            var lng = coord.Lng;

            // 房价（如果不在文件中则随机生成）
            int hp;
            if (!housingPrices.TryGetValue(name, out hp))
            {
                hp = rng.Next(5000, 30001);
            }

            // 深度数据
            deepData.TryGetValue(name, out var dd);
            dd ??= new Dictionary<string, string>();

            // 生成各项指标
            var air = rng.Next(td["air"].Min, td["air"].Max + 1);
            var aqi = air < 35 ? "优" : air < 75 ? "良" : air < 115 ? "轻度" : "中度";

            var cost = rng.Next(td["cost"].Min, td["cost"].Max + 1);
            var rent = rng.Next(td["rent"].Min, td["rent"].Max + 1);
            var food = rng.Next(td["food"].Min, td["food"].Max + 1);
            var transport = rng.Next(td["transport"].Min, td["transport"].Max + 1);
            var internet = rng.Next(td["internet"].Min, td["internet"].Max + 1);
            var safety = rng.Next(td["safety"].Min, td["safety"].Max + 1);
            var healthcare = rng.Next(td["healthcare"].Min, td["healthcare"].Max + 1);
            var walkability = rng.Next(td["walkability"].Min, td["walkability"].Max + 1);
            var nightlife = rng.Next(td["nightlife"].Min, td["nightlife"].Max + 1);
            var coffee = rng.Next(td["coffee"].Min, td["coffee"].Max + 1);
            var coworking = rng.Next(td["coworking"].Min, td["coworking"].Max + 1);
            var temp = rng.Next(td["temp"].Min, td["temp"].Max + 1);

            // 评分计算
            var costScore = Math.Max(0, Math.Min(100, (8000 - cost) / 50.0));
            var score = (int)(costScore * 0.2 + internet * 0.2 + (100 - air) * 0.15 +
                              safety * 0.15 +
                              (healthcare + walkability + nightlife + coffee + coworking) / 5.0 * 0.3);

            // 网络速度
            var broadband = $"{rng.Next(300, 1001)}M";
            var mobile = $"5G/{rng.Next(100, 501)}M";

            // 气候
            var climate = ClimateTypes[rng.Next(ClimateTypes.Length)];

            // 标签
            var tags = GenTags(rng);

            // 气候类型
            var climateType = ClimateTypes[rng.Next(ClimateTypes.Length)];

            // 描述
            var description = $"{name}是{region}地区的一座{tier}城市，拥有独特的自然风光和人文历史。这里生活节奏舒适，适合数字游民远程办公。";

            // 喜欢数
            var likes = rng.Next(1, 501);

            var city = new City
            {
                Name = name,
                Region = region,
                Tier = tier,
                Flag = "🇨🇳",
                Cost = cost,
                Rent = rent,
                Food = food,
                Transport = transport,
                Internet = internet,
                Broadband = broadband,
                Mobile = mobile,
                Climate = climate,
                TempAvg = temp,
                AirQuality = air,
                AqiLevel = aqi,
                Safety = safety,
                Healthcare = healthcare,
                Walkability = walkability,
                Nightlife = nightlife,
                Coffee = coffee,
                Coworking = coworking,
                Tags = tags,
                ClimateType = climateType,
                Description = description,
                Latitude = (decimal)lat,
                Longitude = (decimal)lng,
                HousingPrice = hp,
                DeepLiving = dd.GetValueOrDefault("living", ""),
                DeepCommunity = dd.GetValueOrDefault("community", ""),
                DeepTips = dd.GetValueOrDefault("tips", ""),
                DeepBestSeason = dd.GetValueOrDefault("bestSeason", ""),
                Score = score,
                Likes = likes,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
            };

            db.Cities.Add(city);
        }

        // ===== 创建测试用户 =====
        var testUsers = new[]
        {
            new { Username = "webuser", Email = "web@test.com", Password = "web123456" },
            new { Username = "apiuser", Email = "api@test.com", Password = "api123456" },
        };

        foreach (var u in testUsers)
        {
            var user = new User
            {
                Username = u.Username,
                Email = u.Email.ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(u.Password, workFactor: 11),
                AvatarLetter = u.Username[..1].ToUpper(),
                AvatarColor = UserAvatarColors[rng.Next(UserAvatarColors.Length)],
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
            };
            db.Users.Add(user);
            Console.WriteLine($"[DataSeeder] 创建测试用户: {u.Username} / {u.Password}");
        }

        await db.SaveChangesAsync();
        Console.WriteLine($"[DataSeeder] 数据播种完成: {cityNames.Count} 个城市 + {testUsers.Length} 个测试用户");
    }

    // ===== 从 app.js 提取城市名 =====
    private static List<string> ParseCityNames()
    {
        var names = new List<string>();
        var seen = new HashSet<string>();
        var path = Path.Combine(WwwrootPath, "app.js");
        if (!File.Exists(path)) return names;

        var js = File.ReadAllText(path);
        var blockMatch = Regex.Match(js, @"const CITY_IMAGES = \{([\s\S]*?)\};");
        if (!blockMatch.Success) return names;

        foreach (Match m in Regex.Matches(blockMatch.Groups[1].Value, @"""([^""]+)"":\s*""images/"))
        {
            if (seen.Add(m.Groups[1].Value))
            {
                names.Add(m.Groups[1].Value);
            }
        }
        return names;
    }

    // ===== 从 city-coords.js 提取坐标 =====
    private static Dictionary<string, (double Lat, double Lng)> ParseCoords()
    {
        var coords = new Dictionary<string, (double Lat, double Lng)>();
        var path = Path.Combine(WwwrootPath, "city-coords.js");
        if (!File.Exists(path)) return coords;

        var js = File.ReadAllText(path);
        foreach (Match m in Regex.Matches(js, @"""([^""]+)"":\s*\[([\d.\-]+),\s*([\d.\-]+)\]"))
        {
            var name = m.Groups[1].Value;
            var lat = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
            var lng = double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
            coords[name] = (lat, lng);
        }
        return coords;
    }

    // ===== 从 housing-prices.js 提取房价 =====
    private static Dictionary<string, int> ParseHousingPrices()
    {
        var prices = new Dictionary<string, int>();
        var path = Path.Combine(WwwrootPath, "housing-prices.js");
        if (!File.Exists(path)) return prices;

        var js = File.ReadAllText(path);
        foreach (Match m in Regex.Matches(js, @"""([^""]+)"":\s*(\d+)"))
        {
            prices[m.Groups[1].Value] = int.Parse(m.Groups[2].Value);
        }
        return prices;
    }

    // ===== 从 city-deep-data.js 提取深度数据 =====
    private static Dictionary<string, Dictionary<string, string>> ParseDeepData()
    {
        var deepData = new Dictionary<string, Dictionary<string, string>>();
        var path = Path.Combine(WwwrootPath, "city-deep-data.js");
        if (!File.Exists(path)) return deepData;

        var js = File.ReadAllText(path);
        // 匹配 "城市名": { ... } 块（支持嵌套大括号）
        foreach (Match m in Regex.Matches(js, @"""([^""]+)"":\s*\{([^}]*(?:\{[^}]*\}[^}]*)*)\}"))
        {
            var name = m.Groups[1].Value;
            var block = m.Groups[2].Value;
            var data = new Dictionary<string, string>();

            foreach (var field in new[] { "living", "community", "tips", "bestSeason" })
            {
                var fm = Regex.Match(block, field + @":\s*""((?:[^""\\]|\\.)*)""");
                if (fm.Success)
                {
                    data[field] = fm.Groups[1].Value
                        .Replace("\\n", "\n")
                        .Replace("\\\"", "\"");
                }
            }
            deepData[name] = data;
        }
        return deepData;
    }

    // ===== 获取城市等级 =====
    private static string GetTier(string name, Random rng)
    {
        foreach (var (tier, cities) in TierCities)
        {
            if (Array.IndexOf(cities, name) >= 0)
            {
                return tier;
            }
        }
        return OtherTiers[rng.Next(OtherTiers.Length)];
    }

    // ===== 获取城市区域 =====
    private static string GetRegion(string name)
    {
        return RegionMap.TryGetValue(name, out var region) ? region : "华东";
    }

    // ===== 生成标签（JSON 数组） =====
    private static string GenTags(Random rng)
    {
        var count = rng.Next(2, 6); // 2-5
        count = Math.Min(count, TagsPool.Length);

        // Fisher-Yates 洗牌选取 count 个标签
        var pool = (string[])TagsPool.Clone();
        for (int i = pool.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        var selected = pool.Take(count).ToArray();
        return JsonSerializer.Serialize(selected, JsonOpts);
    }
}
