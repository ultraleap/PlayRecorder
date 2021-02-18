using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PlayRecorder.Interface
{
    public class PlayPauseButton : MonoBehaviour
    {
        private PlaybackManager _manager;

        private Button _button;
        private TMP_Text _text;

        private bool _playing = false;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _text = GetComponentInChildren<TMP_Text>();
        }

        private void OnEnable()
        {
            _manager = FindObjectOfType<PlaybackManager>();
            if(_manager != null)
            {
                _button.onClick.AddListener(() => {
                    _playing = _manager.TogglePlaying();
                    UpdatePlaying();
                });
            }
        }

        private void UpdatePlaying()
        {
            _text.text = _playing ? "Play" : "Pause";
        }
    }
}