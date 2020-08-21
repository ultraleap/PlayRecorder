using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace PlayRecorder.Interface
{

    public class MessageController : MonoBehaviour
    {

        TextMeshPro _textmesh;

        float _timeRemaining = 1f;

        Transform _camera;

        private void Awake()
        {
            _textmesh = GetComponent<TextMeshPro>();
            if (_textmesh == null)
            {
                Debug.LogError("Message prefab does not have a textmesh component.");
            }
        }

        public void CreateMessage(string message, float time)
        {
            CreateMessage(message, time, Color.white);
        }

        public void CreateMessage(string message, float time, Color textColor)
        {
            if(_textmesh == null)
            {
                Debug.LogError("Message cannot be shown as prefab lacks a textmesh component.");
                return;
            }
            _textmesh.color = textColor;
            _timeRemaining = time;
            _camera = Camera.main.transform;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            _timeRemaining -= Time.deltaTime;

            transform.LookAt(_camera);

            if(_timeRemaining <= 0)
            {
                gameObject.SetActive(false);
            }
        }
    }

}
