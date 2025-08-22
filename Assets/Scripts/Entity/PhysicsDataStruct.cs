using System.Collections.Generic;
using UnityEngine;

using Fusion;
using NSMB.Utils;
using NSMB.Tiles;

[System.Serializable]
public struct PhysicsDataStruct : INetworkStruct {

    public byte Flags;
    public float FloorAngle;

    [Networked, Capacity(16)] public NetworkLinkedList<TileContact> TileContacts => default;
    [Networked, Capacity(8)] public NetworkLinkedList<ObjectContact> ObjectContacts => default;

    public IEnumerable<T> GetContactsFromDirection<T>(IEnumerable<T> list, InteractionDirection direction) where T : IContactStruct {
        foreach (T contact in list) {
            if ((contact.direction & direction) != 0) {
                yield return contact;
            }
        }
    }

    public void Reset() {
        Flags = 0;
        FloorAngle = 0;

        TileContacts.Clear();
        ObjectContacts.Clear();
    }

    public IEnumerable<TileContact> TilesStandingOn => GetContactsFromDirection(TileContacts, InteractionDirection.Down);
    public IEnumerable<TileContact> TilesHitSide => GetContactsFromDirection(TileContacts, InteractionDirection.Left | InteractionDirection.Right);
    public IEnumerable<TileContact> TilesHitRoof => GetContactsFromDirection(TileContacts, InteractionDirection.Up);
    public IEnumerable<ObjectContact> ObjectsStandingOn => GetContactsFromDirection(ObjectContacts, InteractionDirection.Down);

    public bool OnGround {
        get => Utils.BitTest(Flags, 0);
        set => Utils.BitSet(ref Flags, 0, value);
    }
    public bool CrushableGround {
        get => Utils.BitTest(Flags, 1);
        set => Utils.BitSet(ref Flags, 1, value);
    }
    public bool HitRoof {
        get => Utils.BitTest(Flags, 2);
        set => Utils.BitSet(ref Flags, 2, value);
    }
    public bool HitLeft {
        get => Utils.BitTest(Flags, 3);
        set => Utils.BitSet(ref Flags, 3, value);
    }
    public bool HitRight {
        get => Utils.BitTest(Flags, 4);
        set => Utils.BitSet(ref Flags, 4, value);
    }
    public bool OnMovingPlatform {
        get => Utils.BitTest(Flags, 5);
        set => Utils.BitSet(ref Flags, 5, value);
    }

    public override string ToString() {
        return $"FloorAngle: {FloorAngle} OnGround: {OnGround}, CrushableGround: {CrushableGround}, HitRoof: {HitRoof}, HitLeft: {HitLeft}, HitRight: {HitRight}, OnMovingPlatform: {OnMovingPlatform}";
    }

    public interface IContactStruct : INetworkStruct {
        public InteractionDirection direction { get; set; }
    }

    public struct TileContact : IContactStruct {
        public Vector2Int location;
        public InteractionDirection direction { get; set; }
    }

    public struct ObjectContact : IContactStruct {
        public NetworkId networkObjectId;
        public InteractionDirection direction { get; set; }

        public NetworkObject GetNetworkObject(NetworkRunner runner) {
            return runner.FindObject(networkObjectId);
        }
    }
}
