using System;
using UnityEngine;
using UnityEngine.UI;

namespace GrassyKnight
{
    class StatusBar {
        private const int _FONT_SIZE = 21;
        private const int _MARGIN_TOP = 10;

        private GameObject _canvas;
        private GameObject _textOnCanvas;

        public bool Visible {
            get => _canvas.GetComponent<Canvas>().enabled;
            set => _canvas.GetComponent<Canvas>().enabled = value;
        }

        public StatusBar() {
            _canvas = new GameObject("GrassyKnight StatusBar Canvas",
                                     typeof(Canvas));
            UnityEngine.Object.DontDestroyOnLoad(_canvas);

            Canvas canvasComponent = _canvas.GetComponent<Canvas>();
            canvasComponent.pixelPerfect = true;
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasComponent.enabled = false;

            _textOnCanvas = new GameObject(
                "GrassyKnight StatusBar",
                typeof(Text),
                typeof(CanvasRenderer));
            UnityEngine.Object.DontDestroyOnLoad(_textOnCanvas);
            _textOnCanvas.transform.parent = canvasComponent.transform;
            _textOnCanvas.transform.localPosition =
                new Vector3(
                    0,
                    // Aligns the vertical center to top of screen
                    canvasComponent.pixelRect.height / 2
                        // Adjusts downwards so top of text is now along top
                        // of screen.
                        - _FONT_SIZE / 2
                        // Finally some margin space to taste. uwu.
                        - _MARGIN_TOP,
                    0);

            Text textComponent = _textOnCanvas.GetComponent<Text>();
            textComponent.font = Modding.CanvasUtil.TrajanBold;
            textComponent.text = "Loading GrassyKnight...";
            textComponent.fontSize = _FONT_SIZE;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        private string PrettyStats(GrassStats stats) {
            int struck = stats[GrassState.Cut] + stats[GrassState.ShouldBeCut];
            string result =  $"{struck}/{stats.Total()}";
            if (stats[GrassState.ShouldBeCut] > 0) {
                result += $" ({stats[GrassState.ShouldBeCut]} shameful)";
            }
            return result;
        }

        public void Update(GrassStats scene, GrassStats global) {
            string result = "";

            if (scene == null) {
                result += $"(not in a room) ";
            } else {
                result += $"in room: {PrettyStats(scene)} ";
            }

            result += $"-- globally: {PrettyStats(global)}";
        }
    }
}
