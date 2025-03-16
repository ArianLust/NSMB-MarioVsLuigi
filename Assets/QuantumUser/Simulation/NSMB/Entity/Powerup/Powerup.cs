using Photon.Deterministic;

namespace Quantum {
    public unsafe partial struct Powerup {

        public void Initialize(Frame f, EntityRef thisEntity, byte spawnAnimationLength, PowerupSpawnReason spawnReason) {
            SpawnReason = spawnReason;
            SpawnAnimationFrames = spawnAnimationLength;
            Lifetime += spawnAnimationLength;

            f.Unsafe.GetPointer<PhysicsObject>(thisEntity)->DisableCollision = true;
            f.Unsafe.GetPointer<Interactable>(thisEntity)->ColliderDisabled = true;
        }

        public void Initialize(Frame f, EntityRef thisEntity, byte spawnAnimationLength, PowerupSpawnReason spawnReason, FPVector2 spawnOrigin, FPVector2 spawnDestination, bool launch = false) {
            Initialize(f, thisEntity, spawnAnimationLength, spawnReason);

            LaunchSpawn = launch;
            BlockSpawn = !launch;
            BlockSpawnOrigin = spawnOrigin;
            BlockSpawnDestination = spawnDestination;
            BlockSpawnAnimationLength = spawnAnimationLength;
            f.Unsafe.GetPointer<Transform2D>(thisEntity)->Position = spawnOrigin;

            var physicsObject = f.Unsafe.GetPointer<PhysicsObject>(thisEntity);

            if (launch) {
                // TODO magic number
                physicsObject->Velocity = new FPVector2(2, 9);
            } else {
                physicsObject->IsFrozen = true;
            }
        }

        public void ParentToPlayer(Frame f, EntityRef thisEntity, EntityRef playerToFollow) {
            Initialize(f, thisEntity, 60, PowerupSpawnReason.Coins);
            ParentMarioPlayer = playerToFollow;

            var marioTransform = f.Unsafe.GetPointer<Transform2D>(playerToFollow);
            var marioCamera = f.Unsafe.GetPointer<CameraController>(playerToFollow);

            // TODO magic value
            f.Unsafe.GetPointer<Transform2D>(thisEntity)->Position = new FPVector2(marioTransform->Position.X, marioCamera->CurrentPosition.Y + PowerupSystem.CameraYOffset);
            f.Unsafe.GetPointer<PhysicsObject>(thisEntity)->IsFrozen = true;
        }
    }
}