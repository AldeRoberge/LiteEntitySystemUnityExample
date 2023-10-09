using Code.Client;
using LiteEntitySystem;
using UnityEngine;

namespace Code.Shared
{
    [UpdateableEntity]
    public class SimpleProjectile : EntityLogic
    {
        [SyncVarFlags(SyncFlags.Interpolated)]
        public SyncVar<Vector2> Position;
        public SyncVar<Vector2> ShooterPos;
        public SyncVar<Vector2> Speed;
        public SyncVar<byte> ShooterPlayerId;
        
        private Rigidbody2D _rigidbody;
        private BoxCollider2D _collider;
        private UnityPhysicsManager _unityPhys;
        public GameObject UnityObject;
        private float _lifeTime = 2f;

        protected override void OnConstructed()
        {
            _unityPhys = EntityManager.GetSingleton<UnityPhysicsManager>();
            var prefab = Resources.Load<GameObject>(EntityManager.IsClient ? "ProjectileClient" : "ProjectileServer");
            UnityObject = Object.Instantiate(prefab, ShooterPos.Value, Quaternion.identity, _unityPhys.Root);
            UnityObject.name = $"Projectile_{Id}";
            UnityObject.GetComponent<SimpleProjectileView>().Attached = this;
        }

        protected override void OnDestroy()
        {
            if (!IsLocal && EntityManager.IsClient)
                ClientLogic.Instance.SpawnHit(Position);
            Object.Destroy(UnityObject);
            _rigidbody = null;
            _collider = null;
        }

        public SimpleProjectile(EntityParams entityParams) : base(entityParams)
        {
        }

        public void Init(byte playerId, Vector2 position, Vector2 speed)
        {
            Position = position;
            Speed = speed;
            ShooterPos = position;
            ShooterPlayerId = playerId;
        }

        public override void Update()
        {
            Position += Speed.Value * EntityManager.DeltaTimeF;
            _lifeTime -= EntityManager.DeltaTimeF;
            if (_lifeTime <= 0f)
            {
                Destroy();
            }
            //_rigidbody.position = Position;
        }
    }
}