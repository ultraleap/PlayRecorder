using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace PlayRecorder.Interface
{

    public class MessageController : MonoBehaviour
    {

        private TextMeshPro _textmesh;

        private float _timeRemaining = 1f;

        private Transform _camera;

        private void Awake()
        {
            _textmesh = GetComponent<TextMeshPro>();
            if (_textmesh == null)
            {
                Debug.LogError("Message prefab does not have a textmesh component.");
            }
        }

        public void CreateMessage(Transform camera, string message, float time)
        {
            CreateMessage(camera, message, time, Color.white);
        }

        public void CreateMessage(Transform camera, string message, float time, Color textColor)
        {
            if(_textmesh == null)
            {
                Debug.LogError("Message cannot be shown as prefab lacks a textmesh component.");
                return;
            }
            _textmesh.color = textColor;
            _textmesh.text = message;
            _timeRemaining = time;
            _camera = FindObjectOfType<Camera>().transform;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            _timeRemaining -= Time.deltaTime;

            transform.LookAt(_camera);
            transform.Rotate(0, 180, 0);

            if(_timeRemaining <= 0)
            {
                gameObject.SetActive(false);
            }
        }
    }

}
