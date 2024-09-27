using LiteEntitySystem.Internal;

namespace LiteEntitySystem
{
    /// <summary>
    /// Base class for singletons entity that can exists in only one instance
    /// </summary>
    public abstract class SingletonEntityLogic : InternalEntity
    {
        protected SingletonEntityLogic(EntityParams entityParams) : base(entityParams)
        {
            InternalOwnerId.Value = EntityManager.ServerPlayerId;
        }
    }

    /// <summary>
    /// Base class for local-only singletons without synchronization 
    /// </summary>
    public abstract class LocalSingletonEntityLogic : InternalEntity
    {
        protected LocalSingletonEntityLogic(EntityParams entityParams) : base(entityParams)
        {
            InternalOwnerId.Value = EntityManager.InternalPlayerId;
        }
    }
}