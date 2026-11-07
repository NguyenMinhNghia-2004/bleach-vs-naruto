"""Add Vietnamese ("vi") names and win quotes to every pack meta.json (fighters, assists, maps).
Quotes are matched by their Chinese source text, so variants sharing a quote share the translation.

Usage: python vi_packs.py
"""
import glob
import json
import os

PACKS = r'D:\Na vs Ble\BVN-Unity\Assets\BVNData\Resources\bvn\raw\packs'

NAMES = {
    # Bleach
    'aizen': 'Aizen Sōsuke', 'byakuya': 'Kuchiki Byakuya', 'byakuya_bankai': 'Kuchiki Byakuya (Bankai)',
    'chad': 'Sado Yasutora (Chad)', 'gin': 'Ichimaru Gin', 'grimmjow': 'Grimmjow Jaegerjaquez',
    'ichigo': 'Kurosaki Ichigo', 'ichigo_bankai': 'Kurosaki Ichigo (Bankai)', 'ichigo_vizored_half': 'Kurosaki Ichigo (Nửa Hollow)',
    'ichigo_vizored': 'Kurosaki Ichigo (Hollow hoá)', 'ichigo_zero': 'Kurosaki Ichigo (Zero)', 'ichigo_dangai': 'Kurosaki Ichigo (Tu luyện)',
    'ogihci': 'Hollow Ichigo', 'ogihci_bankai': 'Hollow Ichigo (Bankai)', 'ogihci_vasteLord': 'Ichigo (Vasto Lorde)',
    'ikkaku': 'Madarame Ikkaku', 'kenpachi': 'Zaraki Kenpachi', 'mayuri': 'Kurotsuchi Mayuri', 'nemu': 'Kurotsuchi Nemu',
    'orihime': 'Inoue Orihime', 'renji': 'Abarai Renji', 'rukia': 'Kuchiki Rukia', 'rukia_shikai': 'Kuchiki Rukia (Shikai)',
    'soifon': 'Soi Fon', 'starrk': 'Coyote Starrk', 'toushirou': 'Hitsugaya Tōshirō', 'toushirou_bankai': 'Hitsugaya Tōshirō (Bankai)',
    'ulquiorra': 'Ulquiorra Cifer', 'ulquiorra_resurreccion': 'Ulquiorra Cifer (Resurrección)', 'uryuu': 'Ishida Uryū',
    'halibel': 'Tier Harribel', 'kon': 'Kon', 'rangiku': 'Matsumoto Rangiku', 'yachiru': 'Kusajishi Yachiru',
    'yamamoto': 'Yamamoto Genryūsai', 'yoruichi': 'Shihōin Yoruichi',
    # Naruto
    'deidara': 'Deidara', 'gaara': 'Gaara', 'itachi': 'Uchiha Itachi', 'jiraiya': 'Jiraiya', 'juggo': 'Jūgo',
    'kakashi': 'Hatake Kakashi', 'kakashi_kamui': 'Hatake Kakashi (Kamui)', 'karin': 'Uzumaki Karin', 'killerbee': 'Killer Bee',
    'kimimaro': 'Kimimaro', 'konan': 'Konan', 'lee': 'Rock Lee', 'lee_tomon': 'Rock Lee (Đỗ Môn)', 'madara': 'Uchiha Madara',
    'naruto': 'Uzumaki Naruto', 'naruto_oneTail': 'Uzumaki Naruto (Một đuôi)', 'naruto_fourTail': 'Uzumaki Naruto (Bốn đuôi)',
    'naruto_sennin': 'Uzumaki Naruto (Tiên nhân)', 'naruto_kyubi': 'Uzumaki Naruto (Cửu Vĩ)', 'neji': 'Hyūga Neji',
    'obito': 'Uchiha Obito', 'obito_majime': 'Uchiha Obito (Bộ mặt thật)', 'orochimaru': 'Orochimaru', 'pain': 'Pain',
    'sakura': 'Haruno Sakura', 'sasuke': 'Uchiha Sasuke', 'sasuke_juin': 'Uchiha Sasuke (Ấn nguyền)',
    'sasuke_susanoo': 'Uchiha Sasuke (Susanoo)', 'temari': 'Temari', 'hinata': 'Hyūga Hinata', 'hiruko': 'Sasori (Hiruko)',
    'ino': 'Yamanaka Ino', 'minato': 'Namikaze Minato', 'sai': 'Sai', 'shikamaru': 'Nara Shikamaru', 'suigetsu': 'Hōzuki Suigetsu',
    'tenten': 'Tenten', 'tsuchikage': 'Ōnoki (Tsuchikage)', 'tsunate': 'Tsunade',
    # Rurouni Kenshin
    'kenshin': 'Himura Kenshin', 'kenshin_battousai': 'Himura Kenshin (Battōsai)', 'shishio': 'Shishio Makoto',
    # random slots
    'random': 'Ngẫu nhiên', 'random_bleach': 'Ngẫu nhiên (Bleach)', 'random_naruto': 'Ngẫu nhiên (Naruto)',
}

MAPS = {
    'DanGai': 'Đoạn Giới (Dangai)', 'Forest': 'Khu rừng', 'GenSe': 'Thế giới thực', 'GoteiJusantai': 'Gotei 13',
    'HotSpring': 'Suối nước nóng', 'KirinoMura': 'Làng Sương Mù', 'KonohaMura': 'Làng Lá', 'Roof': 'Sân thượng',
    'Rukongai': 'Rukongai', 'Seireitei': 'Seireitei', 'ShumatsuNoTani': 'Thung lũng Tận Cùng', 'Sokyoku': 'Đồi Sōkyoku',
    'UchihaMura': 'Khu tộc Uchiha', 'UchinaruSekai': 'Thế giới nội tâm', 'ZangetsuHaka': 'Mộ Zanpakutō', 'random': 'Ngẫu nhiên',
}

QUOTES = {
    '这个世界在最初并没有真实,也没有谎言,只有俨然存在的事实,|可是,存在于这个世界的所有事物,|只会将对自己有利的事实误认为真实而活。':
        'Thuở ban đầu, thế giới này không có thật cũng chẳng có giả, chỉ có những sự thật hiển nhiên tồn tại.|Thế nhưng mọi vật trên đời|đều sống bằng cách nhận lầm sự thật có lợi cho mình là chân lý.',
    '与实力并不相称的生命力也是一种浪费啊！': 'Một sức sống không xứng với thực lực cũng chỉ là lãng phí mà thôi!',
    '不能批判死者……': 'Không thể phán xét người đã chết...',
    '像你这样的水准,我不准你再次对我刀刃相向！': 'Với trình độ như ngươi, ta không cho phép ngươi chĩa kiếm vào ta thêm lần nào nữa!',
    '我们之所以觉得岩壁上的花很美，|那是因为我们就站在岩壁上的缘故，|不必害怕，因为我们像花那样，脚步并没有向外踩出去。':
        'Ta thấy bông hoa trên vách đá thật đẹp|là vì chính ta đang đứng trên vách đá ấy.|Đừng sợ, bởi giống như bông hoa, ta chưa từng bước ra khỏi mép vực.',
    '真是迟钝啊。连倒下去都是这样。': 'Thật chậm chạp. Ngay cả lúc ngã xuống cũng vậy.',
    '喔……真没想到一介平民也能体会贵族的感受啊。': 'Ồ... không ngờ một kẻ thường dân cũng hiểu được cảm giác của quý tộc.',
    '像你这种人，死在我的卍解之下，还早了一千年。': 'Hạng như ngươi mà được chết dưới Bankai của ta thì còn sớm một nghìn năm.',
    '如果手上没有剑，我就不能保护你。': 'Nếu không có kiếm trong tay, tôi không thể bảo vệ cậu.',
    '如果我一直握着剑，我就无法抱紧你。': 'Nếu cứ mãi cầm kiếm, tôi chẳng thể ôm chặt lấy cậu.',
    '……': '...',
    '艺术就是爆炸！嗯。': 'Nghệ thuật là bùng nổ! Ừm.',
    '我的忍术全都是艺术！嗯。': 'Mọi nhẫn thuật của ta đều là nghệ thuật! Ừm.',
    '艺术是美丽而短暂的，消失那一瞬间 才称为美丽，嗯。': 'Nghệ thuật đẹp và ngắn ngủi, khoảnh khắc biến mất mới gọi là đẹp, ừm.',
    '就只有这样而已吗?': 'Chỉ có vậy thôi sao?',
    '我无话可说！': 'Ta chẳng còn gì để nói!',
    '对不起，蓝染队长。': 'Xin lỗi, đội trưởng Aizen.',
    '真没办法……': 'Đành chịu thôi...',
    '不了解爱，也不了解光……只有破坏才是存在的证明！': 'Không hiểu tình yêu, cũng chẳng hiểu ánh sáng... chỉ có huỷ diệt mới là bằng chứng của sự tồn tại!',
    '啊哈哈哈哈哈……这下正好，死神，终于有杀你的价值了。': 'Ahahahaha... thế mới phải chứ, Tử thần, cuối cùng ngươi cũng đáng để ta giết.',
    '我不明白，我怎么会输给你这种货色！': 'Ta không hiểu nổi, sao ta lại thua một kẻ như ngươi!',
    '光是战斗根本没有意思 只是活下去也没有意义。': 'Chỉ chiến đấu thôi thì chẳng có ý nghĩa gì, chỉ sống sót thôi cũng vô nghĩa.',
    '我也不知道为什么，总之很爽就是了！': 'Tôi cũng không biết vì sao, nhưng mà sướng lắm!',
    '正因为我们看不见，那才可怕。': 'Chính vì ta không nhìn thấy nên mới đáng sợ.',
    '一护，王与坐骑的区别是什么？|不要告诉我是两只脚和四只脚的区别！': 'Ichigo, vua và chiến mã khác nhau ở đâu?|Đừng bảo ta là khác ở hai chân với bốn chân đấy!',
    '答案只有一个，那就是本能！|被刻印在事物最原始的根本上，透彻无比的杀戮本能！': 'Câu trả lời chỉ có một, đó là bản năng!|Bản năng giết chóc trần trụi, khắc sâu vào tận cội nguồn của vạn vật!',
    '你缺少的就是这个，这种露骨的本能！|所以你才比我弱啊，一护！': 'Thứ ngươi thiếu chính là cái bản năng trần trụi đó!|Vì thế ngươi mới yếu hơn ta, Ichigo!',
    '我们每个人，打从出生的那一刻起就要迈向死亡。': 'Mỗi người chúng ta, ngay từ lúc chào đời đã bước dần về cái chết.',
    '无法超越死亡的人，那就必须去理解某些事。': 'Kẻ không thể vượt qua cái chết thì phải học cách thấu hiểu một vài điều.',
    '结束总是在开始之前就已经存在了。': 'Kết thúc luôn tồn tại từ trước cả khi bắt đầu.',
    '优秀也是有烦恼的……': 'Giỏi giang cũng có nỗi khổ riêng...',
    '人类就是这样，自以为自己知道了什么，就否定了自己不知道的东西，只会局限在自己的知识范围内自以为是……':
        'Con người là vậy, tưởng mình biết điều gì đó rồi phủ nhận những gì mình không biết, cứ tự cho là đúng trong giới hạn hiểu biết của bản thân...',
    '只要是人，都是依靠自己的知识与认知并且也被之束缚生活着的，|那就叫做现实。|但是，知识与认知是暧昧不清的东西，现实也许只是镜花水月。':
        'Đã là người thì đều sống dựa vào tri thức và nhận thức của mình, và cũng bị chúng trói buộc.|Đó gọi là hiện thực.|Nhưng tri thức và nhận thức vốn mơ hồ, hiện thực có lẽ chỉ là hoa trong gương, trăng dưới nước.',
    '不分东南西北，整个天下都是无人能敌的三忍白发童子蛤蟆使者。': 'Khắp đông tây nam bắc, cả thiên hạ này không ai địch nổi Tam Nhẫn tóc trắng, sứ giả loài cóc!',
    '对于忍者而言怎样活着无所谓，怎样死去才是最重要的。': 'Với ninja, sống thế nào không quan trọng, chết thế nào mới là điều quan trọng nhất.',
    '忍者指的是能忍耐的人': 'Ninja là người biết nhẫn nại.',
    '哈哈哈哈哈哈': 'Hahahahaha!',
    '我不会再失控了。': 'Tôi sẽ không mất kiểm soát nữa.',
    '忍者是不能按常理来思考的！': 'Ninja không được suy nghĩ theo lẽ thường!',
    '放心吧，就算死我也会保护你们的，我绝对不让我的同伴被杀。': 'Yên tâm đi, dù có chết ta cũng sẽ bảo vệ các em, ta tuyệt đối không để đồng đội bị giết.',
    '以现在的你是赢不了我的，你根本不明白什么是真正的强大！': 'Với ngươi bây giờ thì không thắng nổi ta đâu, ngươi chẳng hiểu sức mạnh thật sự là gì!',
    '佐助，人家想和佐助两个人……': 'Sasuke, em muốn được ở riêng với Sasuke...',
    '真想再看一次他那时的面庞': 'Thật muốn được nhìn lại gương mặt anh ấy lúc đó.',
    '佐助，你在哪里？等等我……': 'Sasuke, anh ở đâu? Đợi em với...',
    '来吧、厮杀吧！': 'Tới đây, chém giết nào!',
    '不畏惧死，为的是玩乐死亡。': 'Không sợ chết là để được đùa giỡn với cái chết.',
    '每舍弃自尊一次，我们就越像是野兽。|每扼杀一颗心，我们就远离野兽一步。': 'Mỗi lần vứt bỏ lòng tự trọng, ta lại giống dã thú hơn.|Mỗi lần bóp nghẹt một trái tim, ta lại cách xa dã thú thêm một bước.',
    '剑是凶器，剑技是杀人的伎俩，|无论用多么美丽的语言去掩饰，那始终是事实': 'Kiếm là hung khí, kiếm thuật là kỹ năng giết người.|Dù che đậy bằng lời lẽ đẹp đẽ đến đâu, đó vẫn là sự thật.',
    '你这笨蛋，你这混蛋！': 'Đồ ngốc nhà ngươi, đồ khốn nhà ngươi!',
    '哟！本大爷是最强的！哟！': 'Yo! Bổn đại gia là mạnh nhất! Yo!',
    '哟！你这笨蛋，你这混蛋！哟！……': 'Yo! Đồ ngốc nhà ngươi, đồ khốn nhà ngươi! Yo!...',
    '好吧，想怎么个死法': 'Được thôi, muốn chết kiểu nào đây?',
    '人是为了某种目的而降生的，我坚信，活着便有重要的使命。': 'Con người sinh ra vì một mục đích nào đó. Ta tin chắc rằng sống là để làm tròn một sứ mệnh quan trọng.',
    '也许的确是这样…我的身体病魔缠身，不能坚持很久…但是，我不会灭亡…而且我不是一个人…作为担当起大蛇丸大人一部分野心的人…我将在大蛇丸大人的心中永存…':
        'Có lẽ đúng vậy... thân ta bệnh tật, chẳng trụ được bao lâu... Nhưng ta sẽ không diệt vong... ta không đơn độc... Là người gánh một phần tham vọng của ngài Orochimaru... ta sẽ sống mãi trong lòng ngài.',
    '我就是木叶美丽的苍蓝之兽——李洛克': 'Ta chính là con mãnh thú xanh tươi đẹp đẽ của làng Lá — Rock Lee!',
    '现在就是我贯彻我的忍道的时候了！': 'Giờ chính là lúc ta thực hiện nhẫn đạo của mình!',
    '我要证明就算不会用体术或幻术也能成为一个了不起的忍者！': 'Ta sẽ chứng minh dù không dùng được nhẫn thuật hay ảo thuật, ta vẫn có thể thành một ninja xuất sắc!',
    '现在的我没有力量，换句话说，现在的我只是一具躯壳罢了。': 'Ta lúc này không có sức mạnh, nói cách khác, ta chỉ là một cái xác rỗng mà thôi.',
    '世间的一切与我融为一体，我将成为一统所有的完全体。': 'Vạn vật trên đời hoà làm một với ta, ta sẽ trở thành thể hoàn chỉnh thống nhất tất cả.',
    '神话是仿效事实被传承的。': 'Thần thoại được truyền lại bằng cách mô phỏng sự thật.',
    '一切攻击对我宇智波斑是没用的。': 'Mọi đòn tấn công đều vô dụng với Uchiha Madara ta.',
    '好吧，第四次忍界大战，我在此向你们宣战！': 'Được thôi, ta tuyên chiến với các ngươi — Đại chiến Ninja lần thứ tư!',
    '要控制别人，就要学会利用其内心黑暗的技巧。': 'Muốn khống chế người khác, phải biết lợi dụng bóng tối trong lòng họ.',
    '我已经对你没兴趣了，闪一边去。': 'Ta hết hứng thú với ngươi rồi, tránh sang một bên.',
    '哎呀哎呀，浪费了我这么多时间。': 'Ôi chao ôi chao, phí của ta bao nhiêu thời gian.',
    '药物每天8次，机器实验就每天5次吧，|而改造我也会尽量避免把你搞死。': 'Thuốc thì ngày 8 lần, thí nghiệm máy móc ngày 5 lần nhé,|còn cải tạo thì ta sẽ cố không làm ngươi chết.',
    '只要你变成全村人都认可的最棒忍者，|你就可以把火影这个名字抢过来！': 'Chỉ cần trở thành ninja giỏi nhất được cả làng công nhận,|cậu có thể giành lấy danh hiệu Hokage!',
    '我是绝对不会放弃的，因为这就是我的忍道！': 'Tôi tuyệt đối không bỏ cuộc, vì đó chính là nhẫn đạo của tôi!',
    '好！我决定了,我要走一条属于我自己的忍道,|朝着绝对不让自己后悔的道路一直大步的走下去！': 'Được! Tôi quyết rồi, tôi sẽ đi trên nhẫn đạo của riêng mình,|sải bước mãi trên con đường mà tôi không bao giờ phải hối hận!',
    '我从来都不认为——上忍就是终点！': 'Tôi chưa bao giờ nghĩ Thượng nhẫn là điểm cuối!',
    '人的命运，是从一出生就被注定了。': 'Số phận con người đã được định sẵn từ lúc chào đời.',
    '命运，从对手是我的那一刻开始便被决定了。': 'Số phận đã được định đoạt ngay từ khoảnh khắc đối thủ của ngươi là ta.',
    '不重视同伴的人，连废物都不如！': 'Kẻ không coi trọng đồng đội còn không bằng đồ bỏ đi!',
    '现实越残酷，内心的空洞就越大。': 'Hiện thực càng tàn khốc, lỗ hổng trong lòng càng lớn.',
    '这个世界已无所谓的希望，希望和放弃没有任何的区别，|只是一种敷衍的托词罢了。': 'Thế giới này chẳng còn hi vọng gì, hi vọng hay buông xuôi cũng chẳng khác nhau,|chỉ là một cái cớ cho qua mà thôi.',
    '我谁也不是，也不想变成谁。。。。': 'Ta chẳng là ai cả, cũng chẳng muốn trở thành ai...',
    '如果我是那雨滴的话……|那么，我能够像把不曾交会的天空与大地连接起来那样……|把某人的心串联起来吗？': 'Nếu tôi là giọt mưa kia...|liệu tôi có thể, như nối liền bầu trời và mặt đất chưa từng gặp nhau...|kết nối trái tim của một ai đó không?',
    '要是人生有五次就好了。|这样的话我五次都要住在不同的城镇，|五次都要做不同的工作……然后五次都要……喜欢上同一个人。': 'Giá mà được sống năm lần.|Năm lần tôi sẽ sống ở năm thị trấn khác nhau,|làm năm công việc khác nhau... và cả năm lần... đều yêu cùng một người.',
    '条约只是让对手麻痹的伪装，从现在开始历史将被改写': 'Hiệp ước chỉ là lớp nguỵ trang để đối thủ lơ là, từ giờ lịch sử sẽ được viết lại.',
    '有形的东西迟早会毁灭，无形的东西才会永恒！': 'Thứ hữu hình sớm muộn sẽ huỷ diệt, chỉ thứ vô hình mới trường tồn!',
    '肉体的生命太短暂,太无常': 'Sinh mệnh thể xác thật quá ngắn ngủi, quá vô thường.',
    '我曾经发誓，我说过我一定要救她……|并没有向谁，仅仅是向我的——灵魂而发誓！': 'Ta đã thề, ta đã nói nhất định sẽ cứu cô ấy...|Không thề với ai cả, chỉ thề với — linh hồn của chính ta!',
    '在触不到的燎牙上点火，就象不必仰望星星就能解决，|就象不必喊破喉咙就可以一样。': 'Châm lửa lên chiếc nanh không thể chạm tới,|như chẳng cần ngước nhìn sao trời, chẳng cần gào rách cổ họng.',
    '命运是轮回的吗?还是伸向前方的? | 假如,是轮回的话,那就应该把那轮回之环击碎吗?  | 还是应该逆来顺受呢?': 'Số phận là vòng luân hồi hay là con đường trải dài phía trước?|Nếu là luân hồi, ta có nên đập vỡ vòng luân hồi ấy?|Hay nên cam chịu mà thuận theo?',
    '人们之所以能怀抱希望，是因为他们看不见死亡。': 'Con người còn ôm ấp hi vọng là vì họ không nhìn thấy cái chết.',
    '我每次都受到其他人的保护，好不甘心哦……既然如此，这次……必须有我来保护我所珍惜的人们了……': 'Lần nào em cũng được người khác bảo vệ, thật không cam lòng... Vậy thì lần này... em phải tự mình bảo vệ những người em trân quý...',
    '我每次……都希望自己能够成为一个厉害的忍者……': 'Lúc nào em... cũng mong mình trở thành một ninja thật giỏi...',
    '羁绊太多，只会迷失自我。': 'Quá nhiều ràng buộc chỉ khiến ta đánh mất chính mình.',
    '孤独，不是被父母责备后难过的那种程度比得上的。': 'Cô độc không phải thứ có thể so với nỗi buồn khi bị cha mẹ trách mắng.',
    '我是个复仇者，我是为复仇而生的，为了复仇我可以放弃一切。': 'Ta là kẻ báo thù, ta sinh ra để báo thù, vì báo thù ta có thể từ bỏ tất cả.',
    '你的影子就像是……毫无目的的毒针一样……将我的去路给缝死。': 'Cái bóng của người như... những mũi kim độc vô định... khâu chặt con đường của tôi.',
    '你的光芒就像是……轻柔地打在水塔的雷一般……断绝了我的生命之源。': 'Ánh sáng của người như... tia sét nhẹ nhàng đánh xuống tháp nước... cắt đứt nguồn sống của tôi.',
    '难以忍受的孤独蚀骨吸髓，无法摆脱，无论怎样，只要不再一个人就好，甚至是将自己一分为二。': 'Nỗi cô độc khôn cùng gặm nhấm tận xương tuỷ, không sao thoát được. Thế nào cũng được, miễn là không còn một mình, kể cả phải xẻ đôi bản thân.',
    '我厌恶孤独，但是它却如影随形。': 'Ta ghét cô độc, nhưng nó cứ bám theo như hình với bóng.',
    '笨蛋！': 'Đồ ngốc!',
    '哼！不堪一击！': 'Hừ! Chẳng chịu nổi một đòn!',
    '在强烈的阳光下，印在薄冰之上的足迹消失无踪。 | 不必害怕被欺骗，这世界原本就建立在假象之上。': 'Dưới nắng gắt, dấu chân in trên lớp băng mỏng tan biến không dấu vết.|Đừng sợ bị lừa dối, thế giới này vốn được dựng trên những ảo ảnh.',
    '不要叫我冬狮郎，是日番谷队长！': 'Đừng gọi ta là Tōshirō, là đội trưởng Hitsugaya!',
    '还差得远呢！': 'Còn kém xa lắm!',
    '太阳已经被我们击落了。': 'Mặt trời đã bị chúng ta bắn hạ rồi.',
    '没有什么是我看不到的。': 'Không có gì mà ta không nhìn thấy.',
    '人类，真是到最后，也出人意料的生物。': 'Con người, đến tận cuối cùng vẫn là sinh vật khó đoán.',
    '我赌上灭却师的骄傲，一定要赢你！': 'Ta đặt cược niềm kiêu hãnh của Quincy, nhất định phải thắng ngươi!',
    '不堪一击……': 'Chẳng chịu nổi một đòn...',
}


def main():
    missing_names, missing_quotes = set(), set()
    for kind in ('fighters', 'assists'):
        for f in sorted(glob.glob(os.path.join(PACKS, kind, '*', 'meta.json'))):
            with open(f, encoding='utf-8') as fh:
                meta = json.load(fh)
            zh = meta['language']['zh-CN']
            vi = {'name': {}, 'says': {}}
            for vid, zname in zh.get('name', {}).items():
                if vid in NAMES:
                    vi['name'][vid] = NAMES[vid]
                else:
                    missing_names.add(vid)
                    vi['name'][vid] = zname
            for vid, says in zh.get('says', {}).items():
                out = []
                for s in says:
                    if s in QUOTES:
                        out.append(QUOTES[s])
                    else:
                        missing_quotes.add(s)
                        out.append(s)
                vi['says'][vid] = out
            meta['language']['vi'] = vi
            with open(f, 'w', encoding='utf-8') as fh:
                json.dump(meta, fh, ensure_ascii=False, indent=1)
    for f in sorted(glob.glob(os.path.join(PACKS, 'maps', '*', 'meta.json'))):
        with open(f, encoding='utf-8') as fh:
            meta = json.load(fh)
        mid = meta['id']
        if mid not in MAPS:
            missing_names.add('map:' + mid)
        meta['language']['vi'] = {'name': MAPS.get(mid, meta['language']['zh-CN']['name'])}
        with open(f, 'w', encoding='utf-8') as fh:
            json.dump(meta, fh, ensure_ascii=False, indent=1)
    print('missing names:', sorted(missing_names))
    print('missing quotes:', len(missing_quotes), list(missing_quotes)[:5])


if __name__ == '__main__':
    main()
