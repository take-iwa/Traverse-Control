using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bridge_full
{
    class TraverseController
    {
        public TraverseController()
        {
        }

        // 制御
        public bool TraverseControl(/*オサをどうしたいか*/)
        {
            return true;
        }

        // オサ位置　設定値取得
        private long GetReedPositionValue()
        {
            return 0;
        }

        // オサ位置　設定値変更
        private bool SetReedPositionValue()
        {
            return true;
        }

        // オサ幅　設定値取得
        private long GetReedWidthValue()
        {
            return 0;
        }

        // オサ幅　設定値変更 広げる
        private bool SetReedWidthValueWiden()
        {
            return true;
        }

        // オサ幅　設定値変更 狭める
        private bool SetReedWidthValueNarrow()
        {
            return true;
        }

        // 警報を鳴らす
        private bool SoundAlarm()
        {
            return true;
        }

        // 速度 取得
        private long GetSpeedValue()
        {
            return 0;
        }

        // 張力 取得
        private long GetTensionValue()
        {
            return 0;
        }

        // トラバース位置
        private long GetTraversePositionValue()
        {
            return 0;
        }

        // トラバース速度
        private long GetTraverseSpeedValue()
        {
            return 0;
        }

        // オサ移動幅
        private long GetReedMovementAreaValue()
        {
            return 0;
        }

        // ピッチ幅
        private long GetPitchWidthValue()
        {
            return 0;
        }
    }
}
