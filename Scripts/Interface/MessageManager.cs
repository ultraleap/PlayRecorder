using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayRecorder.Interface
{

    public class MessageManager : MonoBehaviour
    {

        List<MessageCache> _awaitingMessages = new List<MessageCache>();

        [SerializeField]
        GameObject _messagePrefab = null;

        List<MessageController> _messagePool = new List<MessageController>();

        [SerializeField, Range(10, 200)]
        int _messagePoolSize = 50;
        int _messageIndex = 0;

        [SerializeField, Range(0.1f, 5f)]
        float _messageVisibleTime = 0.5f;

        [SerializeField, Range(0.1f, 5f)]
        float _messageScale = 1f;

        private void Start()
        {
            for (int i = 0; i < _messagePoolSize; i++)
            {
                GameObject go = Instantiate(_messagePrefab, transform);
                MessageController mc = go.GetComponent<MessageController>();
                _messagePool.Add(mc);
                go.SetActive(false);
            }
        }

        private void OnEnable()
        {
            PlaybackManager playManager = FindObjectOfType<PlaybackManager>();
            if(playManager != null)
            {
                playManager.OnPlayMessages += OnPlayMessages;
            }
        }

        private void OnDisable()
        {
            PlaybackManager playManager = FindObjectOfType<PlaybackManager>();
            if (playManager != null)
            {
                playManager.OnPlayMessages -= OnPlayMessages;
            }
        }

        void OnPlayMessages(RecordComponent component, List<string> strings)
        {
            for (int i = 0; i < strings.Count; i++)
            {
                _messagePool[_messageIndex].CreateMessage(strings[i], _messageVisibleTime);
                _messagePool[_messageIndex].transform.position = component.transform.position;
                _messagePool[_messageIndex].transform.localScale = _messagePrefab.transform.localScale * _messageScale;
                _messageIndex++;
                if (_messageIndex >= _messagePool.Count)
                {
                    _messageIndex = 0;
                }
            }
        }

    }

}