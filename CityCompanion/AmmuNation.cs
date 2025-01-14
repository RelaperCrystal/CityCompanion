﻿// Copyright (c) RelaperCrystal.
// Licensed under GNU GPL version 3 or any later version.

using System;
using System.Collections.Generic;
using System.IO;
using CityCompanion.Util;
using GTA;
using GTA.Math;
using LemonUI.Menus;
using Newtonsoft.Json;
using PlayerCompanion;

namespace CityCompanion
{
    public class AmmuNation : Script
    {
        private static readonly Vector3[] AmmuNationLocations = { new Vector3(18.18945f, -1120.384f, 28.91654f), new Vector3(-325.6184f, 6072.246f, 31.21228f) };

        private readonly NativeMenu menu = new NativeMenu("Ammu-Nation", "Ammu-Nation");
        private readonly List<NativeItem> purchases = new List<NativeItem>();
        private readonly List<Blip> blips = new List<Blip>();
#pragma warning disable S1450 // Private fields only used as local variables in methods should become local variables
        private readonly Dictionary<WeaponHash, WeaponDefinition> weaponDefinitions;
#pragma warning restore S1450 // Private fields only used as local variables in methods should become local variables
        private int delay;
        private readonly AmmuNationDrawing drawing;

        public AmmuNation()
        {
            var shopItems = JsonConvert.DeserializeObject<AmmuNationShopItems>(File.ReadAllText(@"scripts\CityCompanion\weaponshop_entries.json"));
            weaponDefinitions = JsonConvert.DeserializeObject<Dictionary<WeaponHash, WeaponDefinition>>(File.ReadAllText(@"scripts\CityCompanion\weapon_definition.json"));
            foreach (var pos in AmmuNationLocations)
            {
                var blip = World.CreateBlip(pos);
                blip.Sprite = BlipSprite.AmmuNation;
                blip.Scale = 0.75f;
                blip.Color = BlipColor.Pink;
                blip.IsShortRange = true;
                blip.ShowsDollarSign = true;
                blips.Add(blip);
            }
            foreach (var item in shopItems.Products)
            {
                var weapon = weaponDefinitions[item.Key];
                var shopEntry = item.Value;
                var purchase = new NativeItem(Game.GetLocalizedString(weapon.NameLocalizationKey), Game.GetLocalizedString(weapon.DescriptionLocalizationKey), "$" + shopEntry.Price.ToString());
                purchase.Activated += (sender, e) =>
                {
                    if (Companion.Wallet.Money < shopEntry.Price)
                    {
                        GTA.UI.Screen.ShowSubtitle("~r~You don't have enough money to purchase.");
                        return;
                    }

                    if (Game.Player.Character.Weapons.HasWeapon(item.Key))
                    {
                        if (!shopEntry.AllowExisting)
                        {
                            purchase.Description = "You are already had this weapon.";
                            return;
                        }

                        Game.Player.Character.Weapons[item.Key].Ammo += shopEntry.AmmoAmount;
                    }
                    else
                    {
                        Game.Player.Character.Weapons.Give(item.Key, shopEntry.AmmoAmount, false, true);
                    }

                    Companion.Wallet.Money -= shopEntry.Price;
                    Audio.PlaySoundFrontend("HACKING_CLICK");
                    purchase.Description = "The purchase was successful.";
                    menu.SelectedItem = purchase;
                };
#pragma warning disable RCS1163 // Unused parameter.
                purchase.Selected += (sender, e) =>
#pragma warning restore RCS1163 // Unused parameter.
                {
                    purchase.Description = Game.GetLocalizedString(weapon.DescriptionLocalizationKey);
                };
                purchases.Add(purchase);
                menu.Add(purchase);
            }

            Tick += AmmuNation_Tick;
            Aborted += AmmuNation_Aborted;

            drawing = InstantiateScript<AmmuNationDrawing>();
            drawing.Pool.Add(menu);
        }

        private void AmmuNation_Aborted(object sender, EventArgs e)
        {
            foreach (var item in blips)
            {
                if (item?.Exists() == true)
                {
                    item.Delete();
                }
            }
            drawing.Abort();
        }

        private void AmmuNation_Tick(object sender, EventArgs e)
        {
            foreach (var ammu in AmmuNationLocations)
            {
                if (ammu.DistanceTo(Game.Player.Character.Position) < 5f)
                {
                    if (!menu.Visible)
                    {
                        GTA.UI.Screen.ShowHelpTextThisFrame("Press ~INPUT_CONTEXT~ to open Ammu-Nation menu.");
                    }

                    Game.DisableControlThisFrame(Control.Context);
                    if (Game.IsControlJustPressed(Control.Context) && delay == 0)
                    {
                        menu.Visible = !menu.Visible;
                        delay = 2;
                    }
                }
            }

            if (delay > 0)
            {
                delay--;
            }

            if (delay < 0)
            {
                delay = 0;
            }
        }
    }
}
