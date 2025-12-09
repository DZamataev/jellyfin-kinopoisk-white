import re

def proceed(src):
    parts = re.split(r'((?:19|20)\d{2})', src)

    drop = len(parts)
    year = None
    for last in parts[::-1]:
        drop -= 1
        if re.search(r'^\d{4}$', last):
            year = last
            break
    result = " ".join(parts[:drop])

    if result == "":
        result = re.sub(r'\.\w+$', '', src)

    result = re.sub(r'[^\w]+|[_\s]', ' ', result)

    print()
    print(src)
    print("parts:", parts)

    print("year:", year)
    print("drop:", drop)

    print("result:", result)

proceed("Blade.Runner.2077.2018.1080p.BrRip.x264.YIFY.mp4")
proceed("Kill.Bill.Vol.2.2004.1080p.BrRIp.x264.YIFY.mp4")
proceed("Guns.Akimbo.mp4")
proceed("Сторож_2019.mkv")
proceed("Бумер Фильм второй_745.avi")
