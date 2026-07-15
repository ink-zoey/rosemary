using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Text;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Rosemary.Content.Dev;

public sealed class SiffrinHover : ModItem
{
    public override string Texture => Assets.Elk.TestItem.KEY;

    public override void SetDefaults()
    {
        Item.mountType = ModContent.MountType<SiffrinHoverMount>();
    }
}

public sealed class SiffrinHoverBuff : ModBuff
{
    public override string Texture => Assets.Elk.TestItem.KEY;

    public override void SetStaticDefaults()
    {
        BuffID.Sets.MountType[Type] = ModContent.MountType<SiffrinHoverMount>();
    }
}

public sealed class SiffrinHoverMount : ModMount
{
    public override string Texture => string.Empty;

    public override void SetStaticDefaults()
    {
        MountID.Sets.IgnoresHoverFatigue[Type] = true;
        MountID.Sets.CanUseHooks[Type] = true;

        MountData.buff = ModContent.BuffType<SiffrinHoverBuff>();

        MountData.heightBoost = 0;

        MountData.flightTimeMax = 1;
        MountData.fatigueMax = 1;

        MountData.fallDamage = 0f;

        MountData.usesHover = true;

        MountData.runSpeed = 9f;
        MountData.dashSpeed = 9f;
        MountData.acceleration = 0.36f;
        MountData.jumpHeight = 10;
        MountData.jumpSpeed = 4f;

        MountData.blockExtraJumps = true;
        // MountData.xOffset = -2;
        MountData.bodyFrame = 0;
        // MountData.yOffset = 8;
        MountData.playerHeadOffset = 0;
        MountData.playerYOffsets = [0];

        MountData.totalFrames = 1;

        MountData.standingFrameCount = 1;
        MountData.standingFrameDelay = 0;
        MountData.standingFrameStart = 0;

        MountData.runningFrameCount = 1;
        MountData.runningFrameDelay = 0;
        MountData.runningFrameStart = 0;

        MountData.flyingFrameCount = 1;
        MountData.flyingFrameDelay = 0;
        MountData.flyingFrameStart = 0;

        MountData.inAirFrameCount = 6;
        MountData.inAirFrameDelay = 8;
        MountData.inAirFrameStart = 0;

        MountData.idleFrameCount = 0;
        MountData.idleFrameDelay = 0;
        MountData.idleFrameStart = 0;
        MountData.idleFrameLoop = true;

        MountData.swimFrameCount = 0;
        MountData.swimFrameDelay = 0;
        MountData.swimFrameStart = 0;
    }
}
