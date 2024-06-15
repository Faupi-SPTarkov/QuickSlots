using BepInEx;
using HarmonyLib;
using Aki.Reflection.Patching;
using System.Reflection;
using EFT.UI.DragAndDrop;
using UnityEngine;
using UnityEngine.UI;
using System;
using EFT.InventoryLogic;
using Comfort.Common;
using ItemSize = GStruct24;
using ItemTemplate = GClass2888;
using System.Linq;

namespace QuickSlots
{

    [BepInPlugin("com.faupi.quickslots", "QuickSlots", "0.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        void Awake()
        {
            new UpdateScalePatch().Enable();
        }
    }

    public class UpdateScalePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(QuickSlotItemView).GetMethod("UpdateScale", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(QuickSlotItemView __instance)
        {
            var __mainImage = (Image)AccessTools.Field(typeof(ItemView), "MainImage").GetValue(__instance);
            bool mainImageInactive = !__mainImage.gameObject.activeSelf; // Image isn't visible
            bool isWeapon = __instance.Item is Weapon; // Do not handle weapons, those generally should stay rotated
            if (mainImageInactive || isWeapon)
            {
                // Skip handling (go to base method)
                return true;
            }

            Quaternion quatRotation;
            Vector3 localScale;
            if (__instance.IconScale != null)
            {
                float rotation = 0f;
                ItemSize itemSize = __instance.Item.CalculateCellSize();
                bool itemIsWide = itemSize.X >= itemSize.Y * 1.5;
                bool itemIsTall = itemSize.Y >= itemSize.X * 2.5;  // 1x2 is fine upright but anything bigger should rotate the other way

                bool itemIsStim = false;
                try
                {
                    ItemTemplate itemTemplate = Singleton<HandbookClass>.Instance.StructuredItems[__instance.Item.TemplateId];
                    itemIsStim = itemTemplate.Category.Contains("5b47574386f77428ca22b33a");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Could not get item template: {ex.Message}");
                }

                // Only rotate items if they're meaningfully bigger horizontally
                if (itemIsWide)
                {
                    rotation = 45f;
                }
                else if (itemIsTall)
                {
                    rotation = -45f;
                }
                // Stims are already -45f by default - we want to turn it to the other side
                else if (itemIsStim)
                {
                    rotation = -90f;
                }

                // From here on it's basically the vanilla handling
                quatRotation = Quaternion.Euler(0f, 0f, rotation);
                Vector3 vector = quatRotation * __mainImage.rectTransform.rect.size;
                float scaleX = __instance.IconScale.Value.x / Mathf.Abs(vector.x);
                float scaleY = __instance.IconScale.Value.y / Mathf.Abs(vector.y);
                localScale = Vector3.one * Math.Min(scaleX, scaleY);
            }
            else
            {
                localScale = Vector3.one;
                quatRotation = Quaternion.identity;
            }
            Transform transform = __mainImage.transform;
            transform.localRotation = quatRotation;
            transform.localScale = localScale;

            // We did all we wanted with the customs, don't do anything else.
            return false;
        }
    }
}