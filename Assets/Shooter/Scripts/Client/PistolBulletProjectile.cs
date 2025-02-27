using LiteEntitySystem;
using UnityEngine;

namespace Shooter.Scripts.Client
{
    public struct ProjectileInitParams
    {
        public EntitySharedReference Player;
        public Vector2               Position;
        public Vector2               Speed;
        public void Init(PistolBulletProjectile e) => e.Init(Player, Position, Speed);
    }

    [EntityFlags(EntityFlags.Updateable)]
    public class PistolBulletProjectile : EntityLogic
    {
        private static readonly RaycastHit2D[] RaycastHits = new RaycastHit2D[10];

        [SyncVarFlags(SyncFlags.Interpolated)]
        public SyncVar<Vector2> Position;

        public SyncVar<Vector2>               Speed;
        public SyncVar<EntitySharedReference> ShooterPlayer;
        public SyncVar<bool>                  HitSomething;

        private UnityPhysicsManager _physicsManager;
        public  GameObject          GameObject;

        private float _lifeTime = 2f;

        protected override void OnConstructed()
        {
            _physicsManager = EntityManager.GetSingleton<UnityPhysicsManager>();
            if (IsClient)
            {
                var prefab = Resources.Load<GameObject>("ProjectileClient");
                GameObject = Object.Instantiate(prefab, Position.Value, Quaternion.identity, _physicsManager.Root);
                GameObject.name = $"Projectile_{Id}";
            }
        }

        protected override void OnDestroy()
        {
            if (IsClient && !IsLocal && !HitSomething)
                ClientLogic.Instance.SpawnHit(Position);
            if (GameObject != null)
                Object.Destroy(GameObject);
        }

        public PistolBulletProjectile(EntityParams entityParams) : base(entityParams)
        {
        }

        public void Init(EntitySharedReference player, Vector2 position, Vector2 speed)
        {
            Position.Value = position;
            Speed.Value = speed;
            ShooterPlayer.Value = player;
        }

        protected override void Update()
        {
            //skip IsRemoteControlled because EntityFlags.UpdateOnClient
            //but EntityFlags.UpdateOnClient needed for VisualUpdate
            if (HitSomething || IsRemoteControlled)
                return;

            EnableLagCompensationForOwner();
            int hitsCount = _physicsManager.PhysicsScene.Raycast(
                Position,
                Speed.Value.normalized,
                Speed.Value.magnitude * EntityManager.DeltaTimeF,
                RaycastHits);
            DisableLagCompensationForOwner();

            for (var i = 0; i < hitsCount; i++)
            {
                ref var hit = ref RaycastHits[i];

                if (hit.transform.TryGetComponent<BasePlayerView>(out var playerProxy) && playerProxy.AttachedPlayer.SharedReference != ShooterPlayer)
                {
                    playerProxy.AttachedPlayer.Damage(25);
                    if (EntityManager.IsClient && EntityManager.InNormalState)
                        ClientLogic.Instance.SpawnHit(Position);
                    
                    GameObject?.SetActive(false);
                    HitSomething.Value = true;
                    break;
                }
            }

            Position.Value += Speed.Value * EntityManager.DeltaTimeF;
            _lifeTime -= EntityManager.DeltaTimeF;

            if (HitSomething || (EntityManager.IsServer && _lifeTime <= 0f))
                Destroy();
        }

        protected override void VisualUpdate()
        {
            GameObject.transform.position = Position.Value;
        }
    }
}