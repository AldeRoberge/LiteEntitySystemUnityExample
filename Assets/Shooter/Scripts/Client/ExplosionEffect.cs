using System;
using Shooter.Scripts.Shared;
using UnityEngine;

namespace Shooter.Scripts.Client
{
    public class ExplosionEffect : MonoBehaviour
    {
        [SerializeField] private AudioSource _source;
        [SerializeField] private AudioClip[] _hitClips;

        private Action<ExplosionEffect> _onDeathCallback;

        public void Init(Action<ExplosionEffect> onDeathCallback)
        {
            _onDeathCallback = onDeathCallback;
            gameObject.SetActive(false);
        }

        public void Spawn(Vector2 from)
        {
            transform.position = from;
            gameObject.SetActive(true);
            _source.PlayOneShot(_hitClips.GetRandomElement());
        }

        private void Update()
        {
            if (!_source.isPlaying)
            {
                gameObject.SetActive(false);
                _onDeathCallback?.Invoke(this);
            }
        }
    }
}