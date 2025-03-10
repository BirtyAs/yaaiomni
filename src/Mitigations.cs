﻿using TerrariaApi.Server;

namespace Chireiden.TShock.Omni;

public partial class Plugin : TerrariaPlugin
{
    internal static class Mitigations
    {
        internal static bool HandleInventorySlotPE(byte player, Span<byte> data)
        {
            if (data.Length != 8)
            {
                return true;
            }

            if (data[0] != player)
            {
                return true;
            }

            var slot = BitConverter.ToInt16(data.Slice(1, 2));
            var stack = BitConverter.ToInt16(data.Slice(3, 2));
            var prefix = data[5];
            var type = BitConverter.ToInt16(data.Slice(6, 2));

            var p = Terraria.Main.player[player];
            var existingItem = slot switch
            {
                short when Terraria.ID.PlayerItemSlotID.Loadout3_Dye_0 + 10 > slot && slot >= Terraria.ID.PlayerItemSlotID.Loadout3_Dye_0
                    => p.Loadouts[2].Dye[slot - Terraria.ID.PlayerItemSlotID.Loadout3_Dye_0],
                short when Terraria.ID.PlayerItemSlotID.Loadout3_Dye_0 > slot && slot >= Terraria.ID.PlayerItemSlotID.Loadout3_Armor_0
                    => p.Loadouts[2].Armor[slot - Terraria.ID.PlayerItemSlotID.Loadout3_Armor_0],
                short when Terraria.ID.PlayerItemSlotID.Loadout3_Armor_0 > slot && slot >= Terraria.ID.PlayerItemSlotID.Loadout2_Dye_0
                    => p.Loadouts[1].Dye[slot - Terraria.ID.PlayerItemSlotID.Loadout2_Dye_0],
                short when Terraria.ID.PlayerItemSlotID.Loadout2_Dye_0 > slot && slot >= Terraria.ID.PlayerItemSlotID.Loadout2_Armor_0
                    => p.Loadouts[1].Armor[slot - Terraria.ID.PlayerItemSlotID.Loadout2_Armor_0],
                short when Terraria.ID.PlayerItemSlotID.Loadout2_Armor_0 > slot && slot >= Terraria.ID.PlayerItemSlotID.Loadout1_Dye_0
                    => p.Loadouts[0].Dye[slot - Terraria.ID.PlayerItemSlotID.Loadout1_Dye_0],
                short when Terraria.ID.PlayerItemSlotID.Loadout1_Dye_0 > slot && slot >= Terraria.ID.PlayerItemSlotID.Loadout1_Armor_0
                    => p.Loadouts[0].Armor[slot - Terraria.ID.PlayerItemSlotID.Loadout1_Armor_0],
                short when Terraria.ID.PlayerItemSlotID.Loadout1_Armor_0 > slot && slot >= Terraria.ID.PlayerItemSlotID.Bank4_0
                    => p.bank4.item[slot - Terraria.ID.PlayerItemSlotID.Bank4_0],
                short when Terraria.ID.PlayerItemSlotID.Bank4_0 > slot && slot >= Terraria.ID.PlayerItemSlotID.Bank3_0
                    => p.bank3.item[slot - Terraria.ID.PlayerItemSlotID.Bank3_0],
                short when Terraria.ID.PlayerItemSlotID.Bank3_0 > slot && slot >= Terraria.ID.PlayerItemSlotID.TrashItem
                    => p.trashItem,
                short when Terraria.ID.PlayerItemSlotID.TrashItem > slot && slot >= Terraria.ID.PlayerItemSlotID.Bank2_0
                    => p.bank2.item[slot - Terraria.ID.PlayerItemSlotID.Bank2_0],
                short when Terraria.ID.PlayerItemSlotID.Bank2_0 > slot && slot >= Terraria.ID.PlayerItemSlotID.Bank1_0
                    => p.bank.item[slot - Terraria.ID.PlayerItemSlotID.Bank1_0],
                short when Terraria.ID.PlayerItemSlotID.Bank1_0 > slot && slot >= Terraria.ID.PlayerItemSlotID.MiscDye0
                    => p.miscDyes[slot - Terraria.ID.PlayerItemSlotID.MiscDye0],
                short when Terraria.ID.PlayerItemSlotID.MiscDye0 > slot && slot >= Terraria.ID.PlayerItemSlotID.Misc0
                    => p.miscEquips[slot - Terraria.ID.PlayerItemSlotID.Misc0],
                short when Terraria.ID.PlayerItemSlotID.Misc0 > slot && slot >= Terraria.ID.PlayerItemSlotID.Dye0
                    => p.dye[slot - Terraria.ID.PlayerItemSlotID.Dye0],
                short when Terraria.ID.PlayerItemSlotID.Dye0 > slot && slot >= Terraria.ID.PlayerItemSlotID.Armor0
                    => p.armor[slot - Terraria.ID.PlayerItemSlotID.Armor0],
                short when Terraria.ID.PlayerItemSlotID.Armor0 > slot && slot >= Terraria.ID.PlayerItemSlotID.Inventory0
                    => p.inventory[slot - Terraria.ID.PlayerItemSlotID.Inventory0],
                _ => throw new System.Runtime.CompilerServices.SwitchExpressionException($"Unexpected slot: {slot}")
            };

            if (existingItem != null)
            {
                if ((existingItem.netID == 0 || existingItem.stack == 0) && (type == 0 || stack == 0))
                {
                    return true;
                }
                if (existingItem.netID == type && existingItem.stack == stack && existingItem.prefix == prefix)
                {
                    return true;
                }
            }
            return false;
        }
    }

    private void Hook_Mitigation_GetData(object? sender, OTAPI.Hooks.MessageBuffer.GetDataEventArgs args)
    {
        if (args.Result == OTAPI.HookResult.Cancel)
        {
            return;
        }

        var mitigation = this.config.Mitigation;
        if (!mitigation.Enabled)
        {
            return;
        }

        switch (args.PacketId)
        {
            case (int) PacketTypes.PlayerSlot:
            {
                if (!mitigation.InventorySlotPE)
                {
                    break;
                }
                var index = args.Instance.whoAmI;
                if (Mitigations.HandleInventorySlotPE((byte) index, args.Instance.readBuffer.AsSpan(args.ReadOffset, args.Length - 1)))
                {
                    args.Result = OTAPI.HookResult.Cancel;
                    this.Statistics.MitigationSlotPE++;
                    var player = TShockAPI.TShock.Players[index];
                    if (player == null)
                    {
                        return;
                    }
                    var value = player.GetData<int>(Consts.DataKey.DetectPE);
                    player.SetData<int>(Consts.DataKey.DetectPE, value + 1);
                    if (value % 500 == 0)
                    {
                        var currentLoadoutIndex = Terraria.Main.player[index].CurrentLoadoutIndex;
                        Terraria.NetMessage.TrySendData((int) PacketTypes.SyncLoadout, -1, -1, null, index, (currentLoadoutIndex + 1) % 3);
                        Terraria.NetMessage.TrySendData((int) PacketTypes.SyncLoadout, -1, -1, null, index, currentLoadoutIndex);
                        player.SetData<bool>(Consts.DataKey.IsPE, true);
                    }
                }
                else
                {
                    this.Statistics.MitigationSlotPEAllowed++;
                }
                break;
            }
            case (int) PacketTypes.EffectHeal:
            {
                if (!mitigation.PotionSicknessPE)
                {
                    break;
                }
                var index = args.Instance.whoAmI;
                if (args.Instance.readBuffer[args.ReadOffset] != index)
                {
                    args.Result = OTAPI.HookResult.Cancel;
                    break;
                }
                if (Terraria.Main.player[index].inventory[Terraria.Main.player[index].selectedItem].potion)
                {
                    var amount = BitConverter.ToInt16(args.Instance.readBuffer.AsSpan(args.ReadOffset + 1, 2));
                    TShockAPI.TShock.Players[index]?.SetData<int>(Consts.DataKey.PendingRevertHeal, amount);
                }
                break;
            }
            case (int) PacketTypes.PlayerBuff:
            {
                if (!mitigation.PotionSicknessPE)
                {
                    break;
                }
                var index = args.Instance.whoAmI;
                if (args.Instance.readBuffer[args.ReadOffset] != index)
                {
                    args.Result = OTAPI.HookResult.Cancel;
                    break;
                }
                var buffcount = (args.Length - 1) / 2;
                for (var i = 0; i < buffcount; i++)
                {
                    var buff = BitConverter.ToInt16(args.Instance.readBuffer.AsSpan(args.ReadOffset + 1 + i * 2, 2));
                    if (buff == Terraria.ID.BuffID.PotionSickness)
                    {
                        TShockAPI.TShock.Players[index]?.SetData<int>(Consts.DataKey.PendingRevertHeal, 0);
                    }
                }
                break;
            }
            case (int) PacketTypes.ClientSyncedInventory:
            {
                if (!mitigation.PotionSicknessPE)
                {
                    break;
                }
                var index = args.Instance.whoAmI;
                var pending = TShockAPI.TShock.Players[index]?.GetData<int>(Consts.DataKey.PendingRevertHeal) ?? 0;
                if (pending > 0)
                {
                    TShockAPI.TShock.Players[index]?.SetData<int>(Consts.DataKey.PendingRevertHeal, 0);
                    Terraria.Main.player[index].statLife -= pending;
                    Terraria.NetMessage.TrySendData((int) PacketTypes.PlayerHp, -1, -1, null, index);
                }
                break;
            }
            case (int) PacketTypes.PlayerUpdate:
            {
                if (!mitigation.SwapWhileUsePE)
                {
                    break;
                }
                var index = args.Instance.whoAmI;
                if (args.Instance.readBuffer[args.ReadOffset] != index)
                {
                    args.Result = OTAPI.HookResult.Cancel;
                    break;
                }
                Terraria.BitsByte control = args.Instance.readBuffer[args.ReadOffset + 1];
                var selectedItem = args.Instance.readBuffer[args.ReadOffset + 5];
                if (Terraria.Main.player[index].controlUseItem && control[5] && selectedItem != Terraria.Main.player[index].selectedItem)
                {
                    args.Result = OTAPI.HookResult.Cancel;
                    Terraria.Main.player[index].controlUseItem = false;
                    Terraria.NetMessage.TrySendData((int) PacketTypes.PlayerUpdate, -1, -1, null, index);
                    break;
                }
                break;
            }
            default:
                break;
        }
    }
}
