using UnityEngine;

namespace Shooter.Scripts.Client
{
    public class BasePlayerView : MonoBehaviour
    {
        public BasePlayer AttachedPlayer;

        private void Update()
        {
            transform.position = AttachedPlayer.Position;
            transform.rotation = Quaternion.Euler(0f, 0f, AttachedPlayer.Rotation);
        }
    }
}