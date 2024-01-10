using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace LiteEntitySystem.Internal
{
    public abstract class InternalEntity : IComparable<InternalEntity>
    {
        /// <summary>
        /// Entity class id
        /// </summary>
        public readonly ushort ClassId;
        
        /// <summary>
        /// Entity instance id
        /// </summary>
        public readonly ushort Id;
        
        /// <summary>
        /// Entity manager
        /// </summary>
        public readonly EntityManager EntityManager;
        
        internal readonly byte Version;
        
        [SyncVarFlags(SyncFlags.NeverRollBack)]
        private SyncVar<bool> _isDestroyed;
        
        /// <summary>
        /// Is entity is destroyed
        /// </summary>
        public bool IsDestroyed => _isDestroyed;

        /// <summary>
        /// Is entity local controlled
        /// </summary>
        public bool IsLocalControlled => IsControlledBy(EntityManager.InternalPlayerId);

        /// <summary>
        /// Is entity remote controlled
        /// </summary>
        public bool IsRemoteControlled => IsControlledBy(EntityManager.InternalPlayerId) == false;
        
        /// <summary>
        /// Is entity is controlled by server
        /// </summary>
        public bool IsServerControlled => IsControlledBy(EntityManager.ServerPlayerId);
        
        /// <summary>
        /// ClientEntityManager that available only on client. Will throw exception if called on server
        /// </summary>
        public ClientEntityManager ClientManager => (ClientEntityManager)EntityManager;
        
        /// <summary>
        /// ServerEntityManager that available only on server. Will throw exception if called on client
        /// </summary>
        public ServerEntityManager ServerManager => (ServerEntityManager)EntityManager;

        internal abstract bool IsControlledBy(byte playerId);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref EntityClassData GetClassData()
        {
            return ref EntityManager.ClassDataDict[ClassId];
        }

        /// <summary>
        /// Is locally created entity
        /// </summary>
        public bool IsLocal => Id >= EntityManager.MaxSyncedEntityCount;

        /// <summary>
        /// Destroy entity
        /// </summary>
        public void Destroy()
        {
            if ((EntityManager.IsClient && !IsLocal) || _isDestroyed)
                return;
            DestroyInternal();
        }
        
        private void OnDestroyChange(bool prevValue)
        {
            if (!prevValue && _isDestroyed)
            {
                _isDestroyed = false;
                DestroyInternal();
            }
        }

        /// <summary>
        /// Event called on entity destroy
        /// </summary>
        protected virtual void OnDestroy()
        {

        }

        internal virtual void DestroyInternal()
        {
            if (_isDestroyed)
                return;
            _isDestroyed = true;
            OnDestroy();
            EntityManager.RemoveEntity(this);
        }

        /// <summary>
        /// Fixed update. Called if entity has attribute <see cref="UpdateableEntity"/>
        /// </summary>
        protected internal virtual void Update()
        {
        }

        /// <summary>
        /// Called only on <see cref="ClientEntityManager.Update"/> and if entity has attribute <see cref="UpdateableEntity"/>
        /// </summary>
        protected internal virtual void VisualUpdate()
        {
            
        }

        /// <summary>
        /// Called when entity constructed
        /// </summary>
        protected internal virtual void OnConstructed()
        {
        }

        internal void RegisterRpcInternal()
        {
            ref var classData = ref GetClassData();
            List<RpcFieldInfo> rpcCahce = null;
            if(classData.RemoteCallsClient == null)
            {
                //setup field ids for BindOnChange
                for (int i = 0; i < classData.FieldsCount; i++)
                {
                    ref var field = ref classData.Fields[i];
                    if (field.FieldType == FieldType.SyncVar)
                        RefMagic.RefFieldValue<byte>(this, field.Offset + field.IntSize) = (byte)i;
                }
                rpcCahce = new List<RpcFieldInfo>();
                var rpcRegistrator = new RPCRegistrator(rpcCahce);
                RegisterRPC(ref rpcRegistrator);
                //Logger.Log($"RegisterRPCs for class: {classData.ClassId}");
            }
            //setup id for later sync calls
            for (int i = 0; i < classData.SyncableFields.Length; i++)
            {
                ref var syncFieldInfo = ref classData.SyncableFields[i];
                var syncField = RefMagic.RefFieldValue<SyncableField>(this, syncFieldInfo.Offset);
                syncField.ParentEntity = this;
                if (syncFieldInfo.Flags.HasFlagFast(SyncFlags.OnlyForOwner))
                    syncField.Flags = ExecuteFlags.SendToOwner;
                else if (syncFieldInfo.Flags.HasFlagFast(SyncFlags.OnlyForOtherPlayers))
                    syncField.Flags = ExecuteFlags.SendToOther;
                else
                    syncField.Flags = ExecuteFlags.SendToAll;
                if (classData.RemoteCallsClient != null)
                {
                    syncField.RPCOffset = syncFieldInfo.RPCOffset;
                }
                else
                {
                    syncField.RPCOffset = (ushort)rpcCahce.Count;
                    syncFieldInfo.RPCOffset = syncField.RPCOffset;
                    var syncablesRegistrator = new SyncableRPCRegistrator(syncFieldInfo.Offset, rpcCahce);
                    syncField.RegisterRPC(ref syncablesRegistrator);
                }
            }
            classData.RemoteCallsClient ??= rpcCahce.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ExecuteRPC(in RemoteCall rpc)
        {
            ((Action<InternalEntity>)rpc.CachedAction)(this);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ExecuteRPC<T>(in RemoteCall<T> rpc, T value) where T : unmanaged
        {
            ((Action<InternalEntity, T>)rpc.CachedAction)(this, value);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ExecuteRPC<T>(in RemoteCallSpan<T> rpc, ReadOnlySpan<T> value) where T : unmanaged
        {
            ((SpanAction<InternalEntity, T>)rpc.CachedAction)(this, value);
        }

        /// <summary>
        /// Method for registering RPCs and OnChange notifications
        /// </summary>
        /// <param name="r"></param>
        protected virtual void RegisterRPC(ref RPCRegistrator r)
        {
            r.BindOnChange(this, ref _isDestroyed, OnDestroyChange);
        }

        protected InternalEntity(EntityParams entityParams)
        {
            EntityManager = entityParams.EntityManager;
            Id = entityParams.Id;
            ClassId = entityParams.ClassId;
            Version = entityParams.Version;
        }

        int IComparable<InternalEntity>.CompareTo(InternalEntity other)
        {
            //local first because mostly this is unity physics or something similar
            return (Id >= EntityManager.MaxSyncedEntityCount ? Id - ushort.MaxValue : Id) -
                   (other.Id >= EntityManager.MaxSyncedEntityCount ? other.Id - ushort.MaxValue : other.Id);
        }

        public override int GetHashCode()
        {
            return Id + Version * ushort.MaxValue;
        }

        public override string ToString()
        {
            return $"Entity. Id: {Id}, ClassId: {ClassId}, Version: {Version}";
        }
    }
}