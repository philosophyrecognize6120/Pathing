﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TmfLib;
using TmfLib.Prototype;

namespace BhModule.Community.Pathing.Utility {
    public static class AttributeParsingUtil {

        private const char ATTRIBUTEVALUE_DELIMITER = ',';

        private static readonly CultureInfo _invariantCulture = CultureInfo.InvariantCulture;

        private static IEnumerable<string> SplitAttributeValue(this IAttribute attribute) => attribute.Value.Split(ATTRIBUTEVALUE_DELIMITER);

        private static T InternalGetValueAsEnum<T>(string attributeValue) where T : struct {
            return Enum.TryParse(attributeValue, true, out T value) ? value : default;
        }

        private static int InternalGetValueAsInt(string attributeValue, int @default = default) {
            return int.TryParse(attributeValue, NumberStyles.Any, _invariantCulture, out int value) ? value : @default;
        }

        private static float InternalGetValueAsFloat(string attributeValue, float @default = default) {
            return float.TryParse(attributeValue, NumberStyles.Any, _invariantCulture, out float value) ? value : @default;
        }

        private static Guid InternalGetValueAsGuid(string attributeValue) {
            byte[] rawGuid = Convert.FromBase64String(attributeValue);

            return rawGuid.Length == 16 ? new Guid(rawGuid) : new Guid();
        }

        private static bool InternalGetValueAsBool(string attributeValue) {
            return InternalGetValueAsInt(attributeValue) > 0;
        }

        public static string GetValueAsString(this IAttribute attribute) {
            return attribute.Value;
        }

        public static IEnumerable<string> GetValueAsStrings(this IAttribute attribute) {
            return SplitAttributeValue(attribute);
        }

        public static int GetValueAsInt(this IAttribute attribute, int @default = default) {
            return InternalGetValueAsInt(attribute.Value, @default);
        }

        public static float GetValueAsFloat(this IAttribute attribute, float @default = default) {
            return InternalGetValueAsFloat(attribute.Value, @default);
        }

        public static bool GetValueAsBool(this IAttribute attribute) {
            return InternalGetValueAsBool(attribute.Value);
        }

        public static Guid GetValueAsGuid(this IAttribute attribute) {
            return InternalGetValueAsGuid(attribute.Value);
        }

        public static IEnumerable<int> GetValueAsInts(this IAttribute attribute) {
            return SplitAttributeValue(attribute).Select(attr => InternalGetValueAsInt(attr));
        }

        public static IEnumerable<float> GetValueAsFloats(this IAttribute attribute) {
            return SplitAttributeValue(attribute).Select(attr => InternalGetValueAsFloat(attr));
        }

        public static IEnumerable<Guid> GetValueAsGuids(this IAttribute attribute) {
            return SplitAttributeValue(attribute).Select(InternalGetValueAsGuid);
        }

        public static IEnumerable<bool> GetValueAsBools(this IAttribute attribute) {
            return SplitAttributeValue(attribute).Select(InternalGetValueAsBool);
        }

        public static Texture2D GetValueAsTexture(this IAttribute attribute, IPackResourceManager resourceManager) {
            //var texture = new AsyncTexture2D(ContentService.Textures.Error);

            string texturePath = attribute.GetValueAsString();

            byte[] texture = resourceManager.LoadResource(texturePath);

            return texture != null
                       ? TextureUtil.FromStreamPremultiplied(GameService.Graphics.GraphicsDevice, new MemoryStream(texture))
                       : null;

            //resourceManager.LoadResourceAsync(texturePath).ContinueWith((loadedTexture) => {
            //    if (loadedTexture.Result == null) return;

            //    texture.SwapTexture(TextureUtil.FromStreamPremultiplied(GameService.Graphics.GraphicsDevice, new MemoryStream(loadedTexture.Result)));
            //});

            //return texture;
        }

        public static async Task<Texture2D> GetValueAsTextureAsync(this IAttribute attribute, IPackResourceManager resourceManager) {
            string texturePath = attribute.GetValueAsString();

            byte[] textureData = await resourceManager.LoadResourceAsync(texturePath);

            return textureData != null
                       ? TextureUtil.FromStreamPremultiplied(GameService.Graphics.GraphicsDevice, new MemoryStream(textureData))
                       : null;
        }

        public static Color GetValueAsColor(this IAttribute attribute, Color @default = default) {
            string attrValue = attribute.GetValueAsString().ToLowerInvariant();

            return attrValue switch {
                "white" => Color.White,
                "yellow" => Color.FromNonPremultiplied(255, 255, 0,  255),
                "red" => Color.FromNonPremultiplied(242,    13,  19, 255),
                "green" => Color.FromNonPremultiplied(85,   221, 85, 255),
                _ => ColorUtil.TryParseHex(attrValue, out var color) ? color : @default
            };
        }

        public static T GetValueAsEnum<T>(this IAttribute attribute) where T : struct {
            return InternalGetValueAsEnum<T>(attribute.Value);
        }

        public static IEnumerable<T> GetValueAsEnums<T>(this IAttribute attribute) where T : struct {
            return SplitAttributeValue(attribute).Select(InternalGetValueAsEnum<T>);
        }

    }
}
