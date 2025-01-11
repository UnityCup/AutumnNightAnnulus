settext("ある夜、\n散歩がしたくなって外に出た");
settext("秋の夜風は心地よくて\n僕は目を細めた");
settext("やあ、今日はいい天気だね");
settext("僕が驚いて空を見上げると\n山の向こうにAnnulusが浮かんでいた");

starttimer();
if (!choice("「こんばんは」", "無視する")){
    annulus("聞こえてる？");
    if (!choice("「こんばんは」", "無視する")){
        gameover();
    }
}

const time = endtimer();
annulus("こんばんは");

if (time > 30){
    annulus("返答に" + time + "秒もかかっていたね\nそんなに返事に困ることだったかな？");
}

settext("僕らはしばらく無言で歩いた");
settext("いつもだったら怖いはずの夜道が\nAnnulusの光に照らされてぼんやり輝き\n不思議と不安はなかった")

settext("「ふぁ」\nあくびが一つ浮かんだ");
settext("気付くとAnnulusは消えていて、\n空には月が輝いていた");
gameover();
