﻿using System.Collections.Generic;
using BhModule.Community.Pathing.Entity;
using BhModule.Community.Pathing.Entity.Effects;
using TmfLib.Pathable;

namespace BhModule.Community.Pathing.State {
    public interface IPackState {

        ModuleSettings UserConfiguration { get; }

        int CurrentMapId { get; }

        PathingCategory RootCategory { get; }

        MarkerEffect SharedMarkerEffect { get; }
        TrailEffect SharedTrailEffect { get; }

        BehaviorStates     BehaviorStates     { get; }
        CategoryStates     CategoryStates     { get; }
        MapStates          MapStates          { get; }
        UserResourceStates UserResourceStates { get; }
        UiStates           UiStates           { get; }

        IEnumerable<IPathingEntity> Entities { get; }

        // TODO: Needs unload

    }
}
