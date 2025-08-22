﻿using UnityEngine;

using NSMB.Entities.Player;

namespace NSMB.Entities.Enemies {
    //This is pretty much just the koopawalk script but it causes damage when you stand on it.
    public class Spiny : Koopa {

        //---IPlayerInteractable overrides
        public override void InteractWithPlayer(PlayerController player, PhysicsDataStruct.IContactStruct contact = null) {

            if (Holder) {
                return;
            }

            // Temporary invincibility, we dont want to spam the kick sound
            if (PreviousHolder == player && !ThrowInvincibility.ExpiredOrNotRunning(Runner)) {
                return;
            }

            Utils.Utils.UnwrapLocations(body.Position, player.body.Position, out Vector2 ourPos, out Vector2 theirPos);
            bool fromRight = ourPos.x < theirPos.x;
            Vector2 damageDirection = (theirPos - ourPos).normalized;
            bool attackedFromAbove = Vector2.Dot(damageDirection, Vector2.up) > 0f;

            // Do knockback to players in shells
            if (player.IsInShell && !player.IsStarmanInvincible && IsInShell && !IsStationary) {
                player.DoKnockback(!fromRight, 0, true, Object);
                SpecialKill(!fromRight, false, false, player.StarCombo++);
                return;
            }

            // Always damage exceptions
            if (player.InstakillsEnemies) {
                SpecialKill(!player.FacingRight, false, player.State == Enums.PowerupState.MegaMushroom, player.StarCombo++);
                return;
            }

            // Don't interact with players if we're being held.
            if (Holder) {
                return;
            }

            // Don't interact with crouched blue shell players
            if (!attackedFromAbove && player.IsCrouchedInShell) {
                FacingRight = !fromRight;
                return;
            }

            if (IsInShell) {
                // In shell.
                if (IsActuallyStationary) {
                    // We aren't moving. Check for kicks & pickups
                    if (player.CanPickupItem) {
                        // Pickup
                        Pickup(player);
                    } else {
                        // Kick
                        Kick(player, !fromRight, Mathf.Abs(player.body.Velocity.x) / player.RunningMaxSpeed, player.IsGroundpounding);
                    }
                    return;
                }

                // Moving, in shell. Check for stomps & damage.

                if (attackedFromAbove) {
                    // Stomped.
                    if (player.State == Enums.PowerupState.MiniMushroom) {
                        // Mini mario interactions
                        if (player.IsGroundpounding) {
                            // Mini mario is groundpounding, cancel their groundpound & stop moving
                            EnterShell(true, player);
                            player.IsGroundpounding = false;
                        }
                        player.DoEntityBounce = true;
                    } else {
                        // Normal mario interactions
                        if (player.IsGroundpounding) {
                            //normal mario is groundpounding, we get kick'd
                            Kick(player, !fromRight, Mathf.Abs(player.body.Velocity.x) / player.RunningMaxSpeed, player.IsGroundpounding);
                        } else {
                            // Normal mario isnt groundpounding, we get stopped
                            EnterShell(true, player);
                            player.DoEntityBounce = true;
                            FacingRight = fromRight;
                        }
                    }
                    return;
                }

                // Not being stomped on. just do damage.
                if (player.IsDamageable) {
                    player.Powerdown(false);
                    FacingRight = fromRight;
                }
            } else {
                // Not in shell, we can't be stomped on. Always damage.
                if (player.IsDamageable) {
                    player.Powerdown(false);
                    FacingRight = fromRight;
                    return;
                }
            }
        }

        public override void OnIsActiveChanged() {
            base.OnIsActiveChanged();

            if (IsActive) {
                animator.Play("walk");
            }
        }

        public override void OnIsDeadChanged() {
            base.OnIsDeadChanged();

            sRenderer.flipY = IsDead && IsInShell;
        }
    }
}
