using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayRecorder
{

    public class PlaybackLoop : MonoBehaviour
    {

        [SerializeField]
        private PlaybackManager _manager = null;

        [SerializeField]
        private bool _loopPlayback = true;

        private void OnEnable()
        {
            if(_manager != null)
            {
                _manager.OnTick += OnTick;
            }
        }

        private void OnDisable()
        {
            if (_manager != null)
            {
                _manager.OnTick -= OnTick;
            }
        }

        private void OnTick(int tick)
        {
            if(_loopPlayback && !_manager.changingFiles && tick >= _manager.currentData.frameCount)
            {
                _manager.SetTick(0);
                if(_manager.currentFileIndex == _manager.dataCacheCount - 1)
                {
                    _manager.ChangeCurrentFile(0);
                }
                else
                {
                    _manager.ChangeCurrentFile(_manager.currentFileIndex+1);
                }
            }
        }

        private void OnValidate()
        {
            if(_manager == null)
            {
                _manager = FindObjectOfType<PlaybackManager>();
            }
        }
    }
}