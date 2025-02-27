using LiteEntitySystem;
using Shooter.Scripts.Shared;
using UnityEngine;

namespace Shooter.Scripts.Client
{
    public class BasePlayerController : HumanControllerLogic<PlayerInputPacket, BasePlayer>
    {
        private readonly Camera _mainCamera;

        public BasePlayerController(EntityParams entityParams) : base(entityParams)
        {
            _mainCamera = Camera.main;
        }

        protected override void Update()
        {
            base.Update();
            if (ControlledEntity == null)
                return;
            const float maxPlayerDistance = 35f;
            foreach (var otherPlayer in EntityManager.GetEntities<BasePlayer>())
            {
                ChangeEntityDiffSync(otherPlayer, (otherPlayer.Position - ControlledEntity.Position).sqrMagnitude < maxPlayerDistance * maxPlayerDistance);
            }
        }

        protected override void OnEntityDiffSyncChanged(EntityLogic entity, bool enabled)
        {
            if (entity is BasePlayer bp)
                bp.SetActive(enabled);
        }

        protected override void VisualUpdate()
        {
            if (ControlledEntity == null)
                return;

            //input
            Vector2 velocity = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            Vector2 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dir = mousePos - ControlledEntity.Position;
            float rotation = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            ref var nextCommand = ref ModifyPendingInput();

            if (Input.GetAxis("Fire1") > 0f)
                nextCommand.Keys |= MovementKeys.FireSniper;
            if (Input.GetMouseButton(1))
                nextCommand.Keys |= MovementKeys.FireBullet;

            if (velocity.x < -0.5f)
                nextCommand.Keys |= MovementKeys.Left;
            if (velocity.x > 0.5f)
                nextCommand.Keys |= MovementKeys.Right;
            if (velocity.y < -0.5f)
                nextCommand.Keys |= MovementKeys.Up;
            if (velocity.y > 0.5f)
                nextCommand.Keys |= MovementKeys.Down;

            nextCommand.Rotation = rotation;
        }

        protected override void BeforeControlledUpdate()
        {
            base.BeforeControlledUpdate();
            var velocity = Vector2.zero;
            var input = CurrentInput;
            if (input.Keys.HasFlagFast(MovementKeys.Up))
                velocity.y = -1f;
            if (input.Keys.HasFlagFast(MovementKeys.Down))
                velocity.y = 1f;

            if (input.Keys.HasFlagFast(MovementKeys.Left))
                velocity.x = -1f;
            if (input.Keys.HasFlagFast(MovementKeys.Right))
                velocity.x = 1f;

            ControlledEntity?.SetInput(
                input.Keys.HasFlagFast(MovementKeys.FireSniper),
                input.Keys.HasFlagFast(MovementKeys.FireBullet),
                input.Rotation,
                velocity);
        }
    }
}