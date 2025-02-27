using System;
using Shooter.Scripts.Shared;
using UnityEngine;

namespace Shooter.Scripts.Client
{
    public class SniperTrailEffect : MonoBehaviour
    {
        [SerializeField] private LineRenderer _trailRenderer;
        [SerializeField] private AudioSource  _source;
        [SerializeField] private AudioClip[]  _shootClips;

        private const    float                     MaxAliveTime = 0.5f;
        private          float                     _aliveTimer;
        private          Action<SniperTrailEffect> _onDeathCallback;
        private readonly Vector3[]                 _positions = new Vector3[2];

        public void Init(Action<SniperTrailEffect> onDeathCallback)
        {
            _onDeathCallback = onDeathCallback;
            gameObject.SetActive(false);
        }

        public void Spawn(Vector2 from, Vector2 to)
        {
            _aliveTimer = MaxAliveTime;
            _source.transform.position = from;
            _trailRenderer.transform.position = from;
            _positions[0] = from;
            _positions[1] = to;
            _trailRenderer.SetPositions(_positions);
            gameObject.SetActive(true);

            _source.PlayOneShot(_shootClips.GetRandomElement());
        }

        private void Update()
        {
            _aliveTimer -= Time.deltaTime;
            if (_aliveTimer <= 0f)
            {
                _onDeathCallback(this);
                gameObject.SetActive(false);
                return;
            }

            float t1 = _aliveTimer / (MaxAliveTime);
            float t2 = _aliveTimer / (MaxAliveTime * 2f);
            
            Color startColor = new Color(1f, 0.93f, 0.78f);
            Color endColor = new Color(0f, 0f, 0f, 0f);
            
            _trailRenderer.startColor = Color.Lerp(endColor, startColor, t1);
            _trailRenderer.endColor = Color.Lerp(endColor, startColor, t2);
        }
    }
}