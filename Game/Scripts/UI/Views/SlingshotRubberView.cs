using UnityEngine;

namespace PenguinPinball.Core
{
    [RequireComponent(typeof(LineRenderer))]
    public class SlingshotRubberView : MonoBehaviour
    {
        private LineRenderer lineRenderer;
        private SlingshotViewModel viewModel;

        private void Awake()
        {
            EnsureLineRenderer();
        }

        private void EnsureLineRenderer()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
                if (lineRenderer != null)
                {
                    lineRenderer.positionCount = 3;
                    lineRenderer.useWorldSpace = false; // 로컬 좌표계 사용
                }
            }
        }

        public void BindViewModel(SlingshotViewModel vm)
        {
            if (viewModel != null)
            {
                viewModel.OnLineUpdated -= RenderLine;
            }

            viewModel = vm;

            if (viewModel != null)
            {
                viewModel.OnLineUpdated += RenderLine;
            }
        }

        private void OnDestroy()
        {
            if (viewModel != null)
            {
                viewModel.OnLineUpdated -= RenderLine;
            }
        }

        private void RenderLine(Vector3 p0, Vector3 p1, Vector3 p2)
        {
            EnsureLineRenderer();

            if (lineRenderer == null) return;

            lineRenderer.SetPosition(0, p0);
            lineRenderer.SetPosition(1, p1);
            lineRenderer.SetPosition(2, p2);
        }
    }
}