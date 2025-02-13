﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BhModule.Community.Pathing.Entity;
using BhModule.Community.Pathing.Entity.Effects;
using BhModule.Community.Pathing.State;
using Blish_HUD;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TmfLib;
using TmfLib.Pathable;

namespace BhModule.Community.Pathing {
    public class SharedPackState : IRootPackState {
        
        public ModuleSettings UserConfiguration { get; }

        public int CurrentMapId { get; set; }

        public PathingCategory RootCategory { get; private set; }

        public MarkerEffect SharedMarkerEffect { get; private set; }
        public TrailEffect  SharedTrailEffect  { get; private set; }

        public BehaviorStates     BehaviorStates     { get; private set; }
        public CategoryStates     CategoryStates     { get; private set; }
        public MapStates          MapStates          { get; private set; }
        public UserResourceStates UserResourceStates { get; private set; }
        public UiStates           UiStates           { get; private set; }

        private readonly SynchronizedCollection<IPathingEntity> _entities = new();
        public IEnumerable<IPathingEntity> Entities => _entities;

        private bool _initialized = false;

        public SharedPackState(ModuleSettings moduleSettings) {
            this.UserConfiguration = moduleSettings;

            InitShaders();

            Blish_HUD.Common.Gw2.KeyBindings.Interact.Activated += OnInteractPressed;
        }

        private async Task ReloadStates() {
            await this.CategoryStates.Reload();
            await this.BehaviorStates.Reload();
            await this.MapStates.Reload();
            await this.UserResourceStates.Reload();
            await this.UiStates.Reload();
        }

        private async Task InitStates() {
            await InitCategoryStateManagement();
            await InitBehaviorStateManagement();
            await InitMapStateManagement();
            await InitUserResourceStateManagement();
            await InitUiStateManagement();

            _initialized = true;
        }

        private async Task InitBehaviorStateManagement() {
            this.BehaviorStates = new BehaviorStates(this);
            await this.BehaviorStates.Start();
        }

        private async Task InitCategoryStateManagement() {
            this.CategoryStates = new CategoryStates(this);
            await this.CategoryStates.Start();
        }

        private async Task InitMapStateManagement() {
            this.MapStates = new MapStates(this);
            await this.MapStates.Start();
        }

        private async Task InitUserResourceStateManagement() {
            this.UserResourceStates = new UserResourceStates(this);
            await this.UserResourceStates.Start();
        }

        private async Task InitUiStateManagement() {
            this.UiStates = new UiStates(this);
            await this.UiStates.Start();
        }

        public void UnloadPacks() {
            lock (_entities.SyncRoot) {
                foreach (var pathingEntity in _entities) {
                    lock (pathingEntity.Behaviors.SyncRoot) pathingEntity.Behaviors.Clear();
                }

                GameService.Graphics.World.RemoveEntities(_entities);

                _entities.Clear();
            }

            this.RootCategory = null;
        }

        private IPathingEntity BuildEntity(IPointOfInterest pointOfInterest) {
            return pointOfInterest.Type switch {
                PointOfInterestType.Marker => new StandardMarker(this, pointOfInterest),
                PointOfInterestType.Trail => new StandardTrail(this, pointOfInterest as ITrail),
                PointOfInterestType.Route => throw new NotImplementedException("Routes have not been implemented."),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public async Task LoadPackCollection(IPackCollection collection) {
            this.RootCategory = collection.Categories;

            if (!_initialized) {
                await InitStates();
            } else {
                await ReloadStates();
            }

            await InitPointsOfInterest(collection.PointsOfInterest);
        }

        private async Task InitPointsOfInterest(IEnumerable<PointOfInterest> pois) {
            // TODO: Resolve load / unload deadlock
            //lock (_entities.SyncRoot) {
            pois.AsParallel()
                .Select(BuildEntity)
                .ForAll(newEntity => {
                            _entities.Add(newEntity);
                            GameService.Graphics.World.AddEntity(newEntity);
                            newEntity.FadeIn(); });
            //}

            await Task.CompletedTask;
        }

        private Effect GetEffect(string effectPath) {
            byte[] compiledShader = Utility.TwoMGFX.ShaderCompilerUtil.CompileShader(effectPath);

            System.IO.File.WriteAllBytes(@"C:\Users\dade\source\repos\Pathing\ref\hlsl\marker.mgfx2", compiledShader);

            return new Effect(GameService.Graphics.GraphicsDevice, compiledShader, 0, compiledShader.Length);
        }

        private void InitShaders() {
            //this.SharedMarkerEffect = new MarkerEffect(GetEffect(@"C:\Users\dade\source\repos\Pathing\ref\hlsl\marker.hlsl"));
            //this.SharedTrailEffect  = new TrailEffect(GetEffect(@"C:\Users\dade\source\repos\Pathing\ref\hlsl\trail.hlsl"));
            this.SharedMarkerEffect = new MarkerEffect(PathingModule.Instance.ContentsManager.GetEffect(@"hlsl\marker.mgfx"));
            this.SharedTrailEffect  = new TrailEffect(PathingModule.Instance.ContentsManager.GetEffect(@"hlsl\trail.mgfx"));
        }

        private void OnInteractPressed(object sender, EventArgs e) {
            lock (_entities.SyncRoot) {
                foreach (var entity in _entities) {
                    if (entity.DistanceToPlayer <= entity.TriggerRange) {
                        entity.Interact(false);
                    }
                }
            }
        }

        public void Update(GameTime gameTime) {
            lock (_entities.SyncRoot) {
                foreach (var entity in this._entities) {
                    if (entity.DistanceToPlayer <= entity.TriggerRange) {
                        entity.Focus();
                    } else {
                        entity.Unfocus();
                    }
                }
            }
        }

    }
}
