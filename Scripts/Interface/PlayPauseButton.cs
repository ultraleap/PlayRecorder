using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PlayRecorder.Interface
{

    public class PlayPauseButton : MonoBehaviour
    {
        PlaybackManager _manager;

        Button _button;
        TMP_Text _text;

        bool _playing = false;

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

        void UpdatePlaying()
        {
            _text.text = _playing ? "Play" : "Pause";
        }


    }

}