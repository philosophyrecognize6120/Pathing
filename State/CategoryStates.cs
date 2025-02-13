﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BhModule.Community.Pathing.Utility;
using Blish_HUD;
using Microsoft.Xna.Framework;
using TmfLib.Pathable;

// ReSharper disable InconsistentlySynchronizedField

namespace BhModule.Community.Pathing.State {
    public class CategoryStates : ManagedState {

        private static readonly Logger Logger = Logger.GetLogger<CategoryStates>();

        private const string STATE_FILE = "categories.txt";

        private const double INTERVAL_SAVESTATE                = 5000; // 5.0 seconds
        private const double INTERVAL_UPDATEINACTIVECATEGORIES = 100;  // 0.1 seconds

        private HashSet<string> _inactiveCategories = new(StringComparer.OrdinalIgnoreCase);

        private readonly List<PathingCategory> _rawInactiveCategories   = new();

        private double _lastSaveState                     = 0;
        private double _lastInactiveCategoriesCalculation = 0;

        private bool _stateDirty       = false;
        private bool _calculationDirty = false;

        public CategoryStates(IRootPackState packState) : base(packState, 100) { /* NOOP */ }

        private async Task LoadState() {
            string categoryStatesPath = Path.Combine(DataDirUtil.GetSafeDataDir(DataDirUtil.COMMON_STATE), STATE_FILE);

            if (!File.Exists(categoryStatesPath)) return;

            string recordedCategories = "";

            try {
                using var categoryReader = File.OpenText(categoryStatesPath);
                recordedCategories = await categoryReader.ReadToEndAsync();
            } catch (Exception e) {
                Logger.Error(e, $"Failed to read {STATE_FILE} ({categoryStatesPath}).");
            }

            lock (_rawInactiveCategories) {
                _rawInactiveCategories.Clear();

                foreach (string categoryNamespace in recordedCategories.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)) {
                    // TODO: Consider the case where a category no longer exists - this will create it.
                    // It should not display, though because it will not have a displayname.
                    _rawInactiveCategories.Add(_rootPackState.RootCategory.GetOrAddCategoryFromNamespace(categoryNamespace));
                }
            }

            _calculationDirty = true;
        }

        private void SaveState(GameTime gameTime) {
            if (!_stateDirty) return;

            PathingCategory[] inactiveCategories;

            lock (_rawInactiveCategories) inactiveCategories = _rawInactiveCategories.ToArray();

            string categoryStatesPath = Path.Combine(DataDirUtil.GetSafeDataDir(DataDirUtil.COMMON_STATE), STATE_FILE);

            try {
                File.WriteAllLines(categoryStatesPath, inactiveCategories.Select(c => c.GetNamespace()));
            } catch (Exception e) {
                Logger.Error(e, $"Failed to write {STATE_FILE} ({categoryStatesPath}).");
            }
            
            _stateDirty = false;
        }

        private void CalculateOptimizedCategoryStates(GameTime gameTime) {
            if (!_calculationDirty) return;

            PathingCategory[] inactiveCategories;

            lock (_rawInactiveCategories) inactiveCategories = _rawInactiveCategories.ToArray();

            var preCalcInactiveCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var inactiveCategory in inactiveCategories) { 
                AddAllSubCategories(preCalcInactiveCategories, inactiveCategory);
            }

            lock (_inactiveCategories) _inactiveCategories = preCalcInactiveCategories;

            _calculationDirty = false;
        }

        private void AddAllSubCategories(HashSet<string> categories, PathingCategory currentCategory) {
            categories.Add(currentCategory.GetNamespace());

            foreach (var subCategory in currentCategory) {
                AddAllSubCategories(categories, subCategory);
            }
        }

        protected override async Task<bool> Initialize() {
            await LoadState();

            return true;
        }

        public override async Task Reload() {
            lock (_inactiveCategories)
            lock (_rawInactiveCategories) {
                _inactiveCategories.Clear();
                _rawInactiveCategories.Clear();
            }

            await LoadState();
        }

        protected override void Update(GameTime gameTime) {
            UpdateCadenceUtil.UpdateWithCadence(CalculateOptimizedCategoryStates, gameTime, INTERVAL_UPDATEINACTIVECATEGORIES, ref _lastInactiveCategoriesCalculation);
            UpdateCadenceUtil.UpdateWithCadence(SaveState,                        gameTime, INTERVAL_SAVESTATE,                ref _lastSaveState);
        }

        protected override void Unload() {
            SaveState(null);
        }

        public bool GetNamespaceInactive(string categoryNamespace) {
            return _inactiveCategories.Contains(categoryNamespace);
        }

        public bool GetCategoryInactive(PathingCategory category) {
            return _rawInactiveCategories.Contains(category);
        }

        public void SetInactive(PathingCategory category, bool isInactive) {
            lock (_rawInactiveCategories) {
                _rawInactiveCategories.Remove(category);

                if (isInactive) {
                    _rawInactiveCategories.Add(category);
                }
            }

            _stateDirty       = true;
            _calculationDirty = true;
        }

    }
}
