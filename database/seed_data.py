#!/usr/bin/env python3
"""
NomadCN 数据库种子脚本
从前端 JS 文件提取城市名，生成城市数据并写入 MySQL

依赖: pip install bcrypt mysql-connector-python pypinyin
用法: python3 seed_data.py
"""
import re, json, random, bcrypt, mysql.connector
from pypinyin import lazy_pinyin

random.seed(42)

# ===== 配置 =====
FRONTEND_DIR = '../nomad-cn'  # 前端代码目录（相对路径）
DB_CONFIG = {
    'host': '127.0.0.1',
    'database': 'nomadcn',
    'user': 'nomadcn',
    'password': 'nomadcn2026',
}

# ===== 1. 从前端文件提取城市名 =====
with open(f'{FRONTEND_DIR}/app.js', 'r') as f:
    js = f.read()
m = re.search(r'const CITY_IMAGES = \{([\s\S]*?)\};', js)
city_names = list(dict.fromkeys(re.findall(r'"([^"]+)":\s*"images/', m.group(1))))
print(f"提取到 {len(city_names)} 个城市名")

# ===== 2. 从 city-coords.js 提取坐标 =====
with open(f'{FRONTEND_DIR}/city-coords.js', 'r') as f:
    coords_js = f.read()
coords = {}
for line in coords_js.split('\n'):
    mm = re.search(r'"([^"]+)":\s*\[([\d.\-]+),\s*([\d.\-]+)\]', line)
    if mm:
        coords[mm.group(1)] = (float(mm.group(2)), float(mm.group(3)))

# ===== 3. 从 housing-prices.js 提取房价 =====
with open(f'{FRONTEND_DIR}/housing-prices.js', 'r') as f:
    hp_js = f.read()
housing_prices = {}
for line in hp_js.split('\n'):
    mm = re.search(r'"([^"]+)":\s*(\d+)', line)
    if mm:
        housing_prices[mm.group(1)] = int(mm.group(2))

# ===== 4. 从 city-deep-data.js 提取深度数据 =====
with open(f'{FRONTEND_DIR}/city-deep-data.js', 'r') as f:
    dd_js = f.read()
deep_data = {}
for m in re.finditer(r'"([^"]+)":\s*\{([^}]*(?:\{[^}]*\}[^}]*)*)\}', dd_js):
    name = m.group(1)
    block = m.group(2)
    data = {}
    for field in ['living', 'community', 'tips', 'bestSeason']:
        fm = re.search(rf'{field}:\s*"((?:[^"\\]|\\.)*)"', block)
        if fm:
            data[field] = fm.group(1).replace('\\n', '\n').replace('\\"', '"')
    deep_data[name] = data

# ===== 5. 城市等级和区域定义 =====
TIER_CITIES = {
    '一线': ['北京', '上海', '广州', '深圳'],
    '新一线': ['成都', '杭州', '重庆', '武汉', '西安', '苏州', '南京', '天津', '长沙', '东莞',
              '宁波', '青岛', '合肥', '佛山', '郑州', '昆明', '沈阳', '哈尔滨', '济南', '无锡',
              '厦门', '福州', '温州', '大连', '长春', '石家庄', '南宁', '常州', '泉州', '南通',
              '嘉兴', '太原', '徐州', '贵阳', '金华', '珠海', '惠州', '绍兴', '台州', '烟台'],
}

REGION_MAP = {
    '北京': '华北', '天津': '华北', '石家庄': '华北', '唐山': '华北', '保定': '华北',
    '沧州': '华北', '廊坊': '华北', '邢台': '华北', '张家口': '华北', '承德': '华北',
    '衡水': '华北', '邯郸': '华北', '太原': '华北', '大同': '华北', '阳泉': '华北',
    '沈阳': '东北', '大连': '东北', '鞍山': '东北', '抚顺': '东北', '本溪': '东北',
    '丹东': '东北', '锦州': '东北', '营口': '东北', '辽阳': '东北', '盘锦': '东北',
    '长春': '东北', '吉林': '东北', '四平': '东北', '通化': '东北', '松原': '东北',
    '哈尔滨': '东北', '齐齐哈尔': '东北', '鸡西': '东北', '鹤岗': '东北', '双鸭山': '东北',
    '大庆': '东北', '佳木斯': '东北', '牡丹江': '东北',
    '上海': '华东', '南京': '华东', '无锡': '华东', '徐州': '华东', '常州': '华东',
    '苏州': '华东', '南通': '华东', '扬州': '华东', '镇江': '华东', '泰州': '华东',
    '杭州': '华东', '宁波': '华东', '温州': '华东', '嘉兴': '华东', '湖州': '华东',
    '绍兴': '华东', '金华': '华东', '衢州': '华东', '舟山': '华东', '台州': '华东',
    '合肥': '华东', '芜湖': '华东', '蚌埠': '华东', '马鞍山': '华东', '安庆': '华东',
    '福州': '华东', '厦门': '华东', '泉州': '华东', '漳州': '华东', '南平': '华东',
    '南昌': '华东', '九江': '华东', '上饶': '华东', '吉安': '华东', '宜春': '华东',
    '济南': '华东', '青岛': '华东', '烟台': '华东', '潍坊': '华东', '济宁': '华东',
    '临沂': '华东', '威海': '华东', '日照': '华东', '德州': '华东', '聊城': '华东',
    '郑州': '华中', '洛阳': '华中', '开封': '华中', '南阳': '华中', '信阳': '华中',
    '商丘': '华中', '新乡': '华中', '许昌': '华中', '平顶山': '华中', '安阳': '华中',
    '焦作': '华中', '周口': '华中', '驻马店': '华中', '漯河': '华中', '濮阳': '华中',
    '武汉': '华中', '宜昌': '华中', '襄阳': '华中', '荆州': '华中', '黄冈': '华中',
    '十堰': '华中', '孝感': '华中', '荆门': '华中', '鄂州': '华中', '黄石': '华中',
    '长沙': '华中', '株洲': '华中', '湘潭': '华中', '衡阳': '华中', '岳阳': '华中',
    '常德': '华中', '郴州': '华中', '邵阳': '华中', '益阳': '华中', '永州': '华中',
    '广州': '华南', '深圳': '华南', '珠海': '华南', '佛山': '华南', '东莞': '华南',
    '中山': '华南', '惠州': '华南', '汕头': '华南', '湛江': '华南', '肇庆': '华南',
    '江门': '华南', '茂名': '华南', '梅州': '华南', '韶关': '华南', '清远': '华南',
    '南宁': '华南', '柳州': '华南', '桂林': '华南', '梧州': '华南', '北海': '华南',
    '海口': '华南', '三亚': '华南', '儋州': '华南',
    '重庆': '西南', '成都': '西南', '绵阳': '西南', '德阳': '西南', '南充': '西南',
    '宜宾': '西南', '泸州': '西南', '乐山': '西南', '自贡': '西南', '内江': '西南',
    '贵阳': '西南', '遵义': '西南', '六盘水': '西南', '安顺': '西南', '毕节': '西南',
    '昆明': '西南', '曲靖': '西南', '玉溪': '西南', '保山': '西南', '昭通': '西南',
    '丽江': '西南', '普洱': '西南', '临沧': '西南', '大理': '西南',
    '拉萨': '西南', '日喀则': '西南', '昌都': '西南', '林芝': '西南',
    '西安': '西北', '宝鸡': '西北', '咸阳': '西北', '渭南': '西北', '延安': '西北',
    '汉中': '西北', '榆林': '西北', '安康': '西北', '商洛': '西北',
    '兰州': '西北', '天水': '西北', '白银': '西北', '庆阳': '西北', '酒泉': '西北',
    '武威': '西北', '张掖': '西北', '平凉': '西北', '定西': '西北',
    '西宁': '西北', '海东': '西北',
    '银川': '西北', '石嘴山': '西北', '吴忠': '西北', '固原': '西北', '中卫': '西北',
    '乌鲁木齐': '西北', '克拉玛依': '西北', '吐鲁番': '西北', '哈密': '西北',
    '呼和浩特': '内蒙', '包头': '内蒙', '赤峰': '内蒙', '通辽': '内蒙',
    '鄂尔多斯': '内蒙', '呼伦贝尔': '内蒙', '巴彦淖尔': '内蒙', '乌兰察布': '内蒙',
}

TAGS_POOL = ['自然风光', '美食之都', '咖啡文化', '历史古都', '海滨城市', '科技之城',
             '避暑胜地', '丝路风情', '民族风情', '登山', '茶文化', '冰雪', '温泉',
             '草原', '湿地', '侨乡', '边关', '运河', '红色旅游', '水果之乡']

CLIMATE_TYPES = ['亚热带季风', '温带季风', '热带季风', '高原气候', '温带大陆性', '地中海']

TIER_DATA = {
    '一线':   {'cost': (6000,9000), 'rent': (3500,6000), 'food': (1500,2500), 'transport': (400,800),
              'internet': (85,98), 'safety': (70,85), 'healthcare': (80,95), 'walkability': (75,90),
              'nightlife': (80,95), 'coffee': (80,95), 'coworking': (80,95), 'air': (40,80), 'temp': (12,22)},
    '新一线': {'cost': (4000,6000), 'rent': (2000,4000), 'food': (1000,1800), 'transport': (250,500),
              'internet': (75,92), 'safety': (75,90), 'healthcare': (70,88), 'walkability': (65,85),
              'nightlife': (65,85), 'coffee': (65,85), 'coworking': (70,88), 'air': (35,75), 'temp': (14,24)},
    '二线':   {'cost': (3000,5000), 'rent': (1500,3000), 'food': (800,1500), 'transport': (200,400),
              'internet': (65,85), 'safety': (75,92), 'healthcare': (60,80), 'walkability': (55,78),
              'nightlife': (50,75), 'coffee': (50,75), 'coworking': (55,75), 'air': (30,70), 'temp': (13,25)},
    '三线':   {'cost': (2500,4000), 'rent': (1000,2500), 'food': (600,1200), 'transport': (150,300),
              'internet': (55,78), 'safety': (78,95), 'healthcare': (50,72), 'walkability': (45,70),
              'nightlife': (35,60), 'coffee': (35,60), 'coworking': (40,65), 'air': (25,65), 'temp': (12,26)},
    '四线':   {'cost': (2000,3500), 'rent': (800,1800), 'food': (500,1000), 'transport': (120,250),
              'internet': (45,70), 'safety': (80,96), 'healthcare': (40,65), 'walkability': (40,65),
              'nightlife': (25,50), 'coffee': (25,50), 'coworking': (30,55), 'air': (20,60), 'temp': (11,27)},
    '五线':   {'cost': (1500,3000), 'rent': (600,1500), 'food': (400,800), 'transport': (100,200),
              'internet': (35,62), 'safety': (82,97), 'healthcare': (35,58), 'walkability': (35,60),
              'nightlife': (20,45), 'coffee': (20,45), 'coworking': (25,50), 'air': (15,55), 'temp': (10,28)},
}

def get_tier(name):
    for tier, cities in TIER_CITIES.items():
        if name in cities: return tier
    return random.choice(['二线', '三线', '四线', '五线'])

def get_region(name):
    return REGION_MAP.get(name, '华东')

def gen_tags(name, tier):
    count = random.randint(2, 5)
    return json.dumps(random.sample(TAGS_POOL, min(count, len(TAGS_POOL))), ensure_ascii=False)

def gen_desc(name, tier, region):
    return f'{name}是{region}地区的一座{tier}城市，拥有独特的自然风光和人文历史。这里生活节奏舒适，适合数字游民远程办公。'

# ===== 6. 生成城市数据并插入 =====
conn = mysql.connector.connect(**DB_CONFIG)
cur = conn.cursor()

cur.execute("SET FOREIGN_KEY_CHECKS=0")
cur.execute("TRUNCATE TABLE city_likes")
cur.execute("TRUNCATE TABLE cities")
cur.execute("TRUNCATE TABLE users")
cur.execute("SET FOREIGN_KEY_CHECKS=1")

sql = """INSERT INTO cities (Name, Region, Tier, Flag, Cost, Rent, Food, Transport, Internet, Broadband, Mobile,
    Climate, TempAvg, AirQuality, AqiLevel, Safety, Healthcare, Walkability, Nightlife, Coffee, Coworking,
    Tags, ClimateType, Description, Latitude, Longitude, HousingPrice, DeepLiving, DeepCommunity, DeepTips,
    DeepBestSeason, Score, Likes, CreatedAt, UpdatedAt)
    VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,NOW(),NOW())"""

count = 0
for name in city_names:
    tier = get_tier(name)
    region = get_region(name)
    td = TIER_DATA[tier]
    lat, lng = coords.get(name, (0, 0))
    hp = housing_prices.get(name, random.randint(5000, 30000))
    dd = deep_data.get(name, {})
    air = random.randint(td['air'][0], td['air'][1])
    aqi = '优' if air < 35 else ('良' if air < 75 else ('轻度' if air < 115 else '中度'))

    cost = random.randint(td['cost'][0], td['cost'][1])
    rent = random.randint(td['rent'][0], td['rent'][1])
    food = random.randint(td['food'][0], td['food'][1])
    transport = random.randint(td['transport'][0], td['transport'][1])
    internet = random.randint(td['internet'][0], td['internet'][1])
    safety = random.randint(td['safety'][0], td['safety'][1])
    healthcare = random.randint(td['healthcare'][0], td['healthcare'][1])
    walkability = random.randint(td['walkability'][0], td['walkability'][1])
    nightlife = random.randint(td['nightlife'][0], td['nightlife'][1])
    coffee = random.randint(td['coffee'][0], td['coffee'][1])
    coworking = random.randint(td['coworking'][0], td['coworking'][1])
    temp = random.randint(td['temp'][0], td['temp'][1])

    cost_score = max(0, min(100, (8000 - cost) / 50))
    score = int(cost_score * 0.2 + internet * 0.2 + (100 - air) * 0.15 + safety * 0.15 +
                (healthcare + walkability + nightlife + coffee + coworking) / 5 * 0.3)

    cur.execute(sql, (
        name, region, tier, '🇨🇳', cost, rent, food, transport, internet,
        f'{random.randint(300, 1000)}M', f'5G/{random.randint(100, 500)}M',
        random.choice(CLIMATE_TYPES), temp, air, aqi, safety, healthcare, walkability, nightlife, coffee, coworking,
        gen_tags(name, tier), random.choice(CLIMATE_TYPES), gen_desc(name, tier, region),
        lat, lng, hp,
        dd.get('living', ''), dd.get('community', ''), dd.get('tips', ''), dd.get('bestSeason', ''),
        score, random.randint(1, 500)
    ))
    count += 1

conn.commit()
print(f"✓ 插入 {count} 个城市")

# ===== 7. 创建测试用户 =====
users = [
    ('webuser', 'web@test.com', 'web123456'),
    ('apiuser', 'api@test.com', 'api123456'),
]
for username, email, password in users:
    pw_hash = bcrypt.hashpw(password.encode('utf-8'), bcrypt.gensalt(11)).decode('utf-8')
    letter = username[0].upper()
    color = random.choice(['#00d9a3', '#ff6b6b', '#4ecdc4', '#ffe66d', '#a8e6cf'])
    cur.execute("INSERT INTO users (Username, Email, PasswordHash, AvatarLetter, AvatarColor, CreatedAt, UpdatedAt) VALUES (%s,%s,%s,%s,%s,NOW(),NOW())",
                (username, email, pw_hash, letter, color))
    print(f"✓ 创建用户: {username} / {password}")

conn.commit()
cur.close()
conn.close()
print("\n✅ 数据库种子完成!")
