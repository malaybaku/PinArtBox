
## PinArtBox

Application example to show the web camera image as if it is PinArt on Looking Glass

ウェブカメラの映像をLooking Glass上でピンアートボックスのように表示するアプリケーションです。

https://twitter.com/baku_dreameater/status/1094948481641283586

https://twitter.com/baku_dreameater/status/1094953725406240768


## How to Specify the Web Camera / ウェブカメラの設定方法

In the `WebCamPinArt` script parameters in `PinArtController` object, set the `device name` to the camera name you use.

`PinArtController`の`WebCamPinArt`のなかで、`device name`に使用したいカメラの名前を指定します。


## About Performance / パフォーマンスについて

The number of the pin and shadow on/off strongly affect to the performance. 

If FPS goes down, please reduce the number of pin `Horizontal Division` and `Vertical Disivion`. In that case, keep the ratio of the tow parameters to be about 3:2.

ピンの個数とシャドウの設定がパフォーマンスに大きく影響します。

もしFPSが下がってしまう場合、ピンの配置数を`Horizontal Division`と`Vertical Division`の設定で小さくします。このとき、二つのパラメータの非がほぼ3:2になるように変更してください。

## Contact

* [Twitter](https://twitter.com/baku_dreameater)
* [Blog](https://www.baku-dreameater.net/)

