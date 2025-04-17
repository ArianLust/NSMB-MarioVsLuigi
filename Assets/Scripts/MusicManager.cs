using NSMB.Extensions;
using NSMB.Loading;
using NSMB.UI.Game;
using Quantum;
using UnityEngine;

namespace NSMB.Sound {
    public class MusicManager : MonoBehaviour {

        //---Serialized Variables
        [SerializeField] private LoopingMusicPlayer musicPlayer;

        //---Private Variables
        private VersusStageData stage;

        public void OnValidate() {
            this.SetIfNull(ref musicPlayer);
        }

        public unsafe void Start() {
            QuantumCallback.Subscribe<CallbackUpdateView>(this, OnUpdateView);
            QuantumCallback.Subscribe<CallbackGameResynced>(this, OnGameResynced);
            QuantumEvent.Subscribe<EventMarioPlayerDied>(this, OnMarioPlayerDied);
            QuantumEvent.Subscribe<EventMarioPlayerRespawned>(this, OnMarioPlayerRespawned);
            QuantumEvent.Subscribe<EventGameEnded>(this, OnGameEnded);

            stage = (VersusStageData) QuantumUnityDB.GetGlobalAsset(FindObjectOfType<QuantumMapData>().Asset.UserAsset);
            LoadingCanvas.OnLoadingEnded += OnLoadingEnded;

            QuantumGame game = NetworkHandler.Game;
            if (game != null) {
                GameState state = game.Frames.Predicted.Global->GameState;
                if (state == GameState.Starting || state == GameState.Playing) {
                    // Already in a game
                    HandleMusic(game, true);
                }
            }
        }

        public void OnDestroy() {
            LoadingCanvas.OnLoadingEnded -= OnLoadingEnded;
        }

        public unsafe void OnUpdateView(CallbackUpdateView e) {
            if (e.Game.Frames.Predicted.Global->GameState == GameState.Playing) {
                HandleMusic(e.Game, false);
            }
        }

        private void OnLoadingEnded(bool validPlayer) {
            if (!validPlayer) {
                HandleMusic(QuantumRunner.DefaultGame, true);
            }
        }

        private unsafe void HandleMusic(QuantumGame game, bool force) {
            Frame f = game.Frames.Predicted;
            var rules = f.Global->Rules;

            if (!force && !musicPlayer.IsPlaying) {
                return;
            }

            bool invincible = false;
            bool mega = false;
            bool speedup = false;

            var allPlayers = f.Filter<MarioPlayer>();
            allPlayers.UseCulling = false;

            int playersWithOneLife = 0;
            while (allPlayers.NextUnsafe(out EntityRef entity, out MarioPlayer* mario)) {
                if (rules.IsLivesEnabled && mario->Lives == 0) {
                    continue;
                }
                if (rules.IsLivesEnabled && mario->Lives == 1) {
                    playersWithOneLife++;
                }

                bool isSpectateTarget = false;
                foreach (var playerElement in PlayerElements.AllPlayerElements) {
                    if (playerElement.Entity == entity) {
                        isSpectateTarget = true;
                        break;
                    }
                }

                if (!game.PlayerIsLocal(mario->PlayerRef) && !isSpectateTarget) {
                    continue;
                }

                mega |= Settings.Instance.audioSpecialPowerupMusic.HasFlag(Enums.SpecialPowerupMusic.MegaMushroom) && mario->MegaMushroomFrames > 0;
                invincible |= Settings.Instance.audioSpecialPowerupMusic.HasFlag(Enums.SpecialPowerupMusic.Starman) && mario->IsStarmanInvincible;
            }

            speedup |= rules.IsTimerEnabled && f.Global->Timer <= 60;
            speedup |= QuantumUtils.GetFirstPlaceStars(f) >= rules.StarsToWin - 1;

            if (!speedup && rules.IsLivesEnabled) {
                // Also speed up the music if:
                // A: two players left, at least one has one life
                // B: three+ players left, all have one life
                speedup |= (f.Global->RealPlayers <= 2 && playersWithOneLife > 0) || (playersWithOneLife >= f.Global->RealPlayers);
            }

            if (mega) {
                musicPlayer.Play(f.FindAsset(stage.MegaMushroomMusic));
            } else if (invincible) {
                musicPlayer.Play(f.FindAsset(stage.InvincibleMusic));
            } else {
                musicPlayer.Play(f.FindAsset(stage.MainMusic[f.Global->TotalGamesPlayed % stage.MainMusic.Length]));
            }

            musicPlayer.FastMusic = speedup;
        }

        private void OnGameEnded(EventGameEnded e) {
            musicPlayer.Stop();
        }

        private void OnMarioPlayerRespawned(EventMarioPlayerRespawned e) {
            if (Utils.Utils.IsMarioLocal(e.Entity) && !musicPlayer.IsPlaying) {
                HandleMusic(e.Game, true);
            }
        }

        private void OnMarioPlayerDied(EventMarioPlayerDied e) {
            if (Utils.Utils.IsMarioLocal(e.Entity) && Settings.Instance.audioRestartMusicOnDeath) {
                musicPlayer.Stop();
            }
        }

        private unsafe void OnGameResynced(CallbackGameResynced e) {
            if (e.Game.Frames.Predicted.Global->GameState == GameState.Playing) {
                HandleMusic(e.Game, true);
            }
        }
    }
}