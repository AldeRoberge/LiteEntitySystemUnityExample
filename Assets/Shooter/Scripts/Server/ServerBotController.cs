using LiteEntitySystem;
using Shooter.Scripts.Client;
using UnityEngine;

namespace Shooter.Scripts.Server
{
    public class ServerBotController : AiControllerLogic<BasePlayer>
    {
        private float _rotation;
        private float _rotationChangeTimer = 0.5f;

        public ServerBotController(EntityParams entityParams) : base(entityParams)
        {
            _rotation = Random.Range(0, 360);
        }

        protected override void BeforeControlledUpdate()
        {
            _rotationChangeTimer -= EntityManager.DeltaTimeF;
            if (_rotationChangeTimer < 0f)
            {
                _rotation += Random.Range(-30f, 30f);
                _rotationChangeTimer = Random.Range(0.5f, 3f);
            }

            bool sniperFire = Random.Range(0, 50) == 0;
            bool pistolFire = Random.Range(0, 100) == 0;
            ControlledEntity.SetInput(
                sniperFire,
                !sniperFire && pistolFire,
                _rotation,
                new Vector2(Mathf.Cos(_rotation * Mathf.Deg2Rad), Mathf.Sin(_rotation * Mathf.Deg2Rad) * 0.1f));
        }
    }
}