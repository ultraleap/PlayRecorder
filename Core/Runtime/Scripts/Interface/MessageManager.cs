using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayRecorder.Interface
{

    public class MessageManager : MonoBehaviour
    {
        [SerializeField]
        private GameObject _messagePrefab = null;

        private List<MessageController> _messagePool = new List<MessageController>();

        [SerializeField, Range(10, 200)]
        private int _messagePoolSize = 50;
        private int _messageIndex = 0;

        [SerializeField, Range(0.1f, 5f)]
        private float _messageVisibleTime = 0.5f;

        [SerializeField, Range(0.1f, 5f)]
        private float _messageScale = 1f;

        private Transform _mainCameraTransform;

        private void Start()
        {
            Camera[] cameras = Resources.FindObjectsOfTypeAll<Camera>();
            bool foundMainCamera = false;
            for (int i = 0; i < cameras.Length; i++)
            {
                if(cameras[i].tag == "MainCamera" || cameras[i].name.Contains("Main"))
                {
                    _mainCameraTransform = cameras[i].transform;
                    foundMainCamera = true;
                    break;
                }
            }
            if (!foundMainCamera && cameras.Length > 0)
            {
                _mainCameraTransform = cameras[0].transform;
            }
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
                _messagePool[_messageIndex].CreateMessage(_mainCameraTransform, strings[i], _messageVisibleTime);
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