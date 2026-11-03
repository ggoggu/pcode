using UnityEngine;
using UnityEngine.UI;

namespace PenguinPinball.UI.Views
{
    /// <summary>
    /// 벤치 슬롯 전용 3D 프리뷰 컴포넌트입니다.
    /// 통합 Penguin3DPreview를 상속받아 벤치 전용 좌표 대역(X: 2500대) 및 인스펙터/테스트 하위 호환성을 유지합니다.
    /// </summary>
    public class BenchSlot3DPreview : Penguin3DPreview
    {
        public void Initialize(int index, RawImage rawImage)
        {
            base.Initialize(index, rawImage, 2500f, "BenchPreviewRig");
        }

        public override void Initialize(int index, RawImage rawImage, float baseX = 2500f, string prefix = "BenchPreviewRig")
        {
            base.Initialize(index, rawImage, baseX, prefix);
        }
    }
}
