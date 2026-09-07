using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using Rosemary.Common;
using Rosemary.Content.Misc;
using Rosemary.Core;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Liquid;
using Terraria.GameContent.Shaders;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.Localization;

namespace Rosemary.Content.Elk;

public static partial class ElkShimmerItemSets
{
    [OnLoad]
    private static void Load_ViolentShimmerReaction()
    {
        On_WorldItem.Shimmering += Shimmering_ViolentShimmerReaction;
        IL_WorldItem.MoveInWorld += MoveInWorld_ViolentShimmerReaction;

        // TODO: Move to separate system if this becomes relevant elsewhere
        On_Player.KillMe += KillMe_DisableDrops_ViolentShimmerReaction;
        On_Player.DropTombstone += DropTombstone_DisableDrops_ViolentShimmerReaction;

        On_LiquidRenderer.DrawShimmer += DrawShimmer_Mesmerizers;
    }

    private static readonly string[] death_keys_violent_shimmer_reaction =
    [
        Mods.Rosemary.Content.Elk.Shimmer.Ejection.DeathMessage.KEY,
        Mods.Rosemary.Content.Elk.Shimmer.Spikes.DeathMessage.KEY,
    ];

    private static bool skipPlayerDrops;

    private static void DrawShimmer_Mesmerizers(On_LiquidRenderer.orig_DrawShimmer orig, LiquidRenderer self, SpriteBatch sb, Vector2 drawOffset, bool isBackgroundDraw)
    {
        orig(self, sb, drawOffset, isBackgroundDraw);

        if (isBackgroundDraw)
        {
            return;
        }

        const float spike_count = 8;

        var texture = Assets.Elk.Particles.ExpandingCircle.Asset.Value;
        var origin = texture.Size() * 0.5f;

        using var _ = sb.Scope();

        var mesmerShaderizer = Assets.Elk.Shimmer.Mesmerizer.CreateMesmerizerShader();

        using var lease = ScreenspaceTargetProvider.Shared.Create(Main.graphics.GraphicsDevice, (_, _, targetWidth, targetHeight) => (targetWidth, targetHeight));

        using (lease.Scope(clearColor: Color.Transparent))
        {
            mesmerShaderizer.Parameters.SpikeCount = spike_count;
            mesmerShaderizer.Parameters.Time = (float)Main.timeForVisualEffects * 0.013f;

            mesmerShaderizer.Apply();

            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, mesmerShaderizer.Shader, Matrix.Identity);
            {
                DrawMesmerizers();
            }
            sb.End();
        }

        sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaMask, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Matrix.Identity);
        {
            var shimmerMesmerizer = Assets.Elk.Shimmer.MesmerizerShimmerColors.CreateMesmerizerShimmerColorsShader();

            shimmerMesmerizer.Parameters.Texture = new HlslSampler2D
            {
                Texture = lease.Target,
                Sampler = SamplerState.PointClamp,
            };

            shimmerMesmerizer.Parameters.Time = (float)Main.timeForVisualEffects;
            shimmerMesmerizer.Parameters.TargetPosition = Main.waterTarget.Position;

            shimmerMesmerizer.Apply();

            sb.Draw(lease.Target, Vector2.Zero, Color.White);
        }
        sb.End();

        sb.Begin(SpriteSortMode.Immediate, BlendState.InverseMultiplicative, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Matrix.Identity);
        {
            var inlineMesmerizer = Assets.Elk.Shimmer.MesmerizerInset.CreateMesmerizerInsetShader();

            inlineMesmerizer.Parameters.Texture = new HlslSampler2D
            {
                Texture = lease.Target,
                Sampler = SamplerState.PointClamp,
            };

            inlineMesmerizer.Parameters.TargetPosition = Main.waterTarget.Position;

            inlineMesmerizer.Apply();

            sb.Draw(lease.Target, Vector2.Zero, Color.White);
        }
        sb.End();

        return;

        void DrawMesmerizers()
        {
            foreach (var item in Main.ActiveItems)
            {
                if (!item.shimmerWet
                 || !MesmerizerInfo(item, out var scale, out var alpha))
                {
                    continue;
                }

                var curPosition = FindShimmerSurface(item, 16);

                var dist = curPosition == item.Bottom
                    ? 1f
                    : (1f - MathF.Pow(1f - MathF.Saturate(MathF.Abs(item.Center.Y - curPosition.Y) / 32f), 2f));

                Main.instance.DrawItem_GetBasics(item.inner, item.whoAmI, out var _, out var frame, out var _);

                var itemOrigin = frame.Size() * 0.5f;

                var topLeft = new Vector2((item.width * 0.5f) - itemOrigin.X, item.height - frame.Height);
                var center = item.position + itemOrigin + topLeft;

                center -= Main.waterTarget.Position;

                var time = Main.GlobalTimeWrappedHourly * 3f;

                var rotation = (1 - MathF.Cos(MathF.PI * (time % 1f))) * 0.5f + MathF.Floor(time);
                rotation *= MathF.Tau / (spike_count - 0.5f);

                var size = scale * dist * 0.15f;

                var color = Color.White * alpha;

                sb.Draw(texture, center, null, color, rotation, origin, size, SpriteEffects.None, 0f);
            }
        }

        static bool MesmerizerInfo(WorldItem item, out float scaleMultiplier, out float noiseMultiplier)
        {
            var reactant = ItemID.Sets.ViolentShimmerReaction[item.type] && item.ShimmerData is { SubSurfaceProgress: > 0f };

            scaleMultiplier = noiseMultiplier = 0f;

            if (!ItemID.Sets.ViolentShimmerReaction[item.type] || item.ShimmerData is not { SubSurfaceProgress: > 0f } data)
            {
                return false;
            }

            const float low_noise = 0.25f;

            scaleMultiplier = 1f - MathF.Pow(1f - data.SubSurfaceProgress, 12f);

            noiseMultiplier = scaleMultiplier;
            noiseMultiplier *= Utils.Remap(1f - MathF.Pow(data.SubSurfaceProgress, 23f), 0f, 1f, low_noise, 1f);

            scaleMultiplier *= Utils.Remap(1f - MathF.Pow(data.SubSurfaceProgress, 21f), 0f, 1f, low_noise, 1f);

            return reactant;
        }
    }

    private static void DropTombstone_DisableDrops_ViolentShimmerReaction(On_Player.orig_DropTombstone orig, Player self, long coinsOwned, NetworkText deathText, int hitDirection)
    {
        if (skipPlayerDrops)
        {
            return;
        }

        orig(self, coinsOwned, deathText, hitDirection);
    }

    private static void KillMe_DisableDrops_ViolentShimmerReaction(On_Player.orig_KillMe orig, Player self, PlayerDeathReason damageSource, double dmg, int hitDirection, bool pvp)
    {
        var priorNewItem = Item.newItemDisabled;

        if (damageSource.CustomReason._mode == NetworkText.Mode.LocalizationKey
         && death_keys_violent_shimmer_reaction.Contains(damageSource.CustomReason._text))
        {
            skipPlayerDrops = true;
            Item.newItemDisabled = true;
        }

        orig(self, damageSource, dmg, hitDirection, pvp);

        skipPlayerDrops = false;
        Item.newItemDisabled = priorNewItem;
    }

    private const float increment_violent_shimmer_reaction = 0.006f;

    private static void MoveInWorld_ViolentShimmerReaction(ILContext il)
    {
        var c = new ILCursor(il);

        var itemIndex = ParameterIndex.Invalid;

        c.GotoNext(
            MoveType.After,
            i => i.MatchCall(typeof(SoundEngine), nameof(SoundEngine.PlaySound)),
            i => i.MatchPop()
        );

        c.FindPrev(
            out _,
            i => i.MatchLdarg(out itemIndex),
            i => i.MatchLdflda<Entity>(nameof(Entity.position))
        );

        c.EmitLdarg(itemIndex);
        c.EmitDelegate(
            static (WorldItem item) =>
            {
                if (!ItemID.Sets.ViolentShimmerReaction[item.type])
                {
                    return;
                }

                item.ShimmerData ??= new ShimmerReactionData
                {
                    WaveProgress = 0f,
                    SubSurfaceProgress = 0f,
                    LoopingSound = false,
                };

                item.ShimmerData.WaveProgress = 0f;
                item.ShimmerData.SubSurfaceProgress = 0f;

                var modifier = new CallbackPunchCameraModifier(
                    item.Center,
                    new Vector2(1f, 0f),
                    4f,
                    7f,
                    (int)(1f / increment_violent_shimmer_reaction) + 20,
                    m =>
                    {
                        if (!ItemID.Sets.ViolentShimmerReaction[item.type] || !item.shimmerWet)
                        {
                            return false;
                        }

                        m._startPosition = item.Center;
                        m._framesLasted %= 60;

                        return false;
                    },
                    600f,
                    $"{nameof(Rosemary)}: SHIMMER_VIOLENT_WARNING"
                );
                Main.instance.CameraModifiers.Add(modifier);

                SoundEngine.PlaySound(
                    Assets.Elk.Shimmer.Burn.Asset with
                    {
                        PauseBehavior = PauseBehavior.PauseWithGame,
                        MaxInstances = 3,
                    },
                    item.Center,
                    SoundCallback,
                    3100f
                );

                SoundEngine.PlaySound(
                    Assets.Elk.Shimmer.BurnLoop.Asset with
                    {
                        PauseBehavior = PauseBehavior.PauseWithGame,
                        MaxInstances = 3,
                        IsLooped = true,
                        Volume = 0.5f,
                    },
                    item.Center,
                    SoundCallback
                );
                item.ShimmerData.LoopingSound = true;

                PlayScowl(item);

                return;

                bool SoundCallback(ActiveSound sound)
                {
                    if (!ItemID.Sets.ViolentShimmerReaction[item.type]
                     || !item.shimmerWet
                     || item.IsAir
                     || !item.active)
                    {
                        item.ShimmerData?.LoopingSound = false;
                        return false;
                    }

                    item.ShimmerData?.LoopingSound = true;
                    sound.Position = item.Center;

                    return item.shimmerWet;
                }
            }
        );
    }

    public static void PlayScowl(WorldItem item)
    {
        if (item.ExtendoGripData?.InClaw is not true)
        {
            SoundEngine.PlaySound(
                Assets.Elk.Shimmer.Scowl.Asset with
                {
                    PauseBehavior = PauseBehavior.PauseWithGame,
                    MaxInstances = 3,
                },
                item.Center,
                SoundCallback,
                3600f
            );
        }

        return;

        bool SoundCallback(ActiveSound sound)
        {
            if (!ItemID.Sets.ViolentShimmerReaction[item.type]
             || !item.shimmerWet
             || item.ExtendoGripData?.InClaw is true
             || item.IsAir
             || !item.active)
            {
                return false;
            }

            sound.Position = item.Center;

            return item.shimmerWet;
        }
    }

    private static void Shimmering_ViolentShimmerReaction(On_WorldItem.orig_Shimmering orig, WorldItem self)
    {
        if (!ItemID.Sets.ViolentShimmerReaction[self.type])
        {
            orig(self);

            return;
        }

        self.shimmerTime = 0;
        self.shimmered = false;

        self.ShimmerData ??= new ShimmerReactionData
        {
            WaveProgress = 0f,
            SubSurfaceProgress = 0f,
            LoopingSound = false,
        };

        self.noGrabDelay = 90;

        var data = self.ShimmerData;

        if (data.WaveProgress < 0f)
        {
            return;
        }

        if (!data.LoopingSound)
        {
            SoundEngine.PlaySound(
                Assets.Elk.Shimmer.BurnLoop.Asset with
                {
                    PauseBehavior = PauseBehavior.PauseWithGame,
                    MaxInstances = 3,
                    IsLooped = true,
                    Volume = 0.5f,
                },
                self.Center,
                SoundCallback
            );
            data.LoopingSound = true;
        }

        var curPosition = FindShimmerSurface(self, 32);

        // Find the bounds of the current pool
        var minRange = -16f;
        var maxRange = 16f;
        for (var i = 0; i < 32; ++i)
        {
            var leftPosition = curPosition.ToTileCoordinates();
            var rightPosition = leftPosition;
            leftPosition.X -= i;
            rightPosition.X += i;

            var leftShimmer = Main.tile[leftPosition].HasShimmer;
            var rightShimmer = Main.tile[rightPosition].HasShimmer;

            if (!leftShimmer && !rightShimmer)
            {
                break;
            }

            if (leftShimmer)
            {
                minRange -= 16f;
            }
            if (rightShimmer)
            {
                maxRange += 16f;
            }
        }

        var reactant = self.inner.ModItem as IViolentShimmerReactant;

        var progress = data.WaveProgress;

        var subSurface = false;

        var above = self.Center.ToTileCoordinates();
        above.Y -= 1;
        if (Main.tile[above].HasShimmer)
        {
            var dist = curPosition == self.Bottom
              ? 1f
              : MathF.Saturate(MathF.Abs(self.Center.Y - curPosition.Y) / 80f);

            self.velocity.Y = -22f * dist;
            subSurface = true;
        }
        else
        {
            self.velocity *= 0.5f;
        }

        var rippleOffset = new Vector2((1f - progress) * 700f, Rand.Next(-8f, 8f));

        if (data.SubSurfaceProgress > 1f)
        {
            if (reactant?.Ejection(self, subSurface) is true)
            {
                self.ClearOut();
            }
            else
            {
                data.SubSurfaceProgress = Rand.Next(0.5f, 0.8f);
            }
        }

        const float increment_smoke_result_shimmer_reaction = 0.001f;

        if (self.ExtendoGripData?.InClaw is not true)
        {
            data.WaveProgress += increment_violent_shimmer_reaction;

            if (data.SubSurfaceProgress > 0)
            {
                data.SubSurfaceProgress -= 0.005f;
            }

            data.SubSurfaceProgress = MathF.Min(data.SubSurfaceProgress, 0.5f);
        }

        data.SubSurfaceProgress += increment_smoke_result_shimmer_reaction;

        PassiveEffects();
        InteractWithPlayers();

        if (progress < 1f)
        {
            return;
        }

        var velocity = -self.velocity;
        velocity.Y = -20f;

        self.velocity = velocity;

        data.WaveProgress = -1f;

        EjectEffects();

        SoundEngine.PlaySound(
            Assets.Elk.Shimmer.Ejection.Asset with
            {
                PauseBehavior = PauseBehavior.PauseWithGame,
                MaxInstances = 3,
            },
            curPosition,
            attenuationDistance: 5500f
        );

        if (!subSurface
         && curPosition.Distance(Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f) > 3300f)
        {
            SoundEngine.PlaySound(
                Assets.Elk.Shimmer.EjectionFar.Asset with
                {
                    PauseBehavior = PauseBehavior.PauseWithGame,
                    MaxInstances = 3,
                },
                curPosition,
                attenuationDistance: 150000f
            );
        }

        if (reactant?.Ejection(self, subSurface) is true)
        {
            self.ClearOut();
        }

        if (subSurface)
        {
            return;
        }

        const int center_width = 80;
        const int center_height = 120;

        var centerHitbox = new Rectangle(
            (int)self.Center.X - center_width,
            (int)curPosition.Y - center_height,
            center_width * 2,
            center_height + 32
        );

        foreach (var player in Main.ActivePlayers)
        {
            if (centerHitbox.Intersects(player.Hitbox))
            {
                KillPlayer(player);
            }
        }

        return;

        bool SoundCallback(ActiveSound sound)
        {
            if (!ItemID.Sets.ViolentShimmerReaction[self.type]
             || !self.shimmerWet
             || self.IsAir
             || !self.active)
            {
                self.ShimmerData?.LoopingSound = false;
                return false;
            }

            self.ShimmerData?.LoopingSound = true;
            sound.Position = self.Center;

            return self.shimmerWet;
        }

        static void KillPlayer(Player player)
        {
            if (Main.myPlayer != player.whoAmI)
            {
                return;
            }

            player.KillMe(
                PlayerDeathReason.ByCustomReason(
                    NetworkText.FromKey(Mods.Rosemary.Content.Elk.Shimmer.Ejection.DeathMessage.KEY, player.name)
                ),
                int.MinValue,
                0
            );
        }

        void InteractWithPlayers()
        {
            if (rippleOffset.X < 85f || subSurface)
            {
                return;
            }

            const int offset = 16;
            const int height = 64;

            Rectangle? leftHitbox = null;
            if (-rippleOffset.X > minRange)
            {
                leftHitbox = new Rectangle(
                    (int)(curPosition.X - rippleOffset.X) - 8 - offset,
                    (int)curPosition.Y - height,
                    16,
                    height + 16
                );
            }

            Rectangle? rightHitbox = null;
            if (rippleOffset.X < maxRange)
            {
                rightHitbox = new Rectangle(
                    (int)(curPosition.X + rippleOffset.X) - 8 + offset,
                    (int)curPosition.Y - height,
                    16,
                    height + 16
                );
            }

            foreach (var player in Main.ActivePlayers)
            {
                if (leftHitbox?.Intersects(player.Hitbox) is true)
                {
                    DamagePlayer(player, 1);
                }
                else if (rightHitbox?.Intersects(player.Hitbox) is true)
                {
                    DamagePlayer(player, -1);
                }
            }

            return;

            static void DamagePlayer(Player player, int direction)
            {
                player.Hurt(
                    PlayerDeathReason.ByCustomReason(
                        NetworkText.FromKey(Mods.Rosemary.Content.Elk.Shimmer.Spikes.DeathMessage.KEY, player.name)
                    ),
                    120,
                    direction,
                    false,
                    true,
                    -1,
                    false,
                    0.4f,
                    0f,
                    7f
                );
            }
        }

        void PassiveEffects()
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }

            // Acid bubbles
            ElkShimmerParticles.Bubbles +=
                new ElkShimmerParticles.ShimmerBubble(
                    Rand.Next(self.Hitbox),
                    GetShimmerSplashColor(),
                    Rand.Next(-1f, 1f),
                    Rand.Next((byte)1, (byte)4),
                    0
                );

            var rippleStrength = self.ExtendoGripData?.InClaw is true ? Rand.Next(0.25f, 2.1f) : Rand.Next(0.15f, 0.85f);
            var rippleShape = self.ExtendoGripData?.InClaw is true ? RippleShape.Circle : RippleShape.Square;

            WaterShaderData.Instance.QueueRipple(Rand.Next(self.Hitbox), rippleStrength, rippleShape, MathF.PiOver4);

            var side = Rand.NextDirection();

            var dustOffX = MathF.Pow(1f - Rand.Next(0f, 1f), 3f);

            // Surface droplets
            var dustOffset = new Vector2(
                (dustOffX * (side >= 0 ? minRange : maxRange)),
                0f
            );

            var dust = Dust.NewDustPerfect(
                curPosition + dustOffset,
                DustID.ShimmerSplash,
                new Vector2(Rand.Next(-1f, 1f), Rand.Next(-13f, -5f)),
                0,
                GetShimmerSplashColor(),
                1.2f
            );

            dust.noGravity = true;

            if (subSurface
             || self.ExtendoGripData?.InClaw is true)
            {
                return;
            }

            // Dust moving inward
            dustOffset = Rand.NextUnitVector(Rand.Next(400f));
            dustOffset.Y = -MathF.Abs(dustOffset.Y);

            dust = Dust.NewDustPerfect(
                curPosition + dustOffset,
                DustID.ShimmerSplash,
                dustOffset.Normalized * -8f,
                0,
                GetShimmerSplashColor(),
                1.2f
            );

            dust.noGravity = true;

            // Ripples closing in
            var size = new Vector2(MathF.Max(50f * MathF.Pow(progress, 3), 6f));

            var strength = MathF.Max(MathF.Pow(progress, 2), 0.8f);

            WaterShaderData.Instance.QueueRipple(curPosition + rippleOffset, Rand.Next(0.75f, 1f) * strength, size, RippleShape.Square, MathF.PiOver4);
            WaterShaderData.Instance.QueueRipple(curPosition - rippleOffset, Rand.Next(0.75f, 1f) * strength, size, RippleShape.Square, MathF.PiOver4);

            // Inward spikes
            if (rippleOffset.X < 85f)
            {
                return;
            }

            SpawnSpike(rippleOffset.X, Rand.Next(32f, 64f), Rand.Next(0.04f, 0.07f));
            SpawnSpike(-rippleOffset.X, Rand.Next(32f, 64f), Rand.Next(0.04f, 0.07f));
        }

        void EjectEffects()
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }

            ElkShimmerParticles.Rings += new ElkShimmerParticles.ExpandingRing(self.Center, 0.1f, 0.02f, 0f, 0.04f);

            // Camera shake with lingering effect
            var strong = new PunchCameraModifier(curPosition, new Vector2(0f, -1f), 35f, 8f, 45, 4500f, $"{nameof(Rosemary)}: SHIMMER_VIOLENT_WRONG");
            var lingering = new PunchCameraModifier(curPosition, new Vector2(0f, -1f), 2f, 9f, 200, 7000f, $"{nameof(Rosemary)}: SHIMMER_VIOLENT_AFTERSHOCK");
            Main.instance.CameraModifiers.Add(strong);
            Main.instance.CameraModifiers.Add(lingering);

            if (subSurface)
            {
                return;
            }

            // Giant splash with a center bias
            for (var i = 0; i < 70; i++)
            {
                var dist = Rand.Next(-1f, 1f);

                var dustOffset = new Vector2((MathF.Pow(1f - MathF.Abs(dist), 3f)) * MathF.Sign(dist) * 120f, 0f);

                var dust = Dust.NewDustPerfect(
                    curPosition + dustOffset,
                    DustID.ShimmerSplash,
                    new Vector2(Rand.Next(-1f, 1f), Rand.Next(-50f * MathF.Abs(dist), -2f)),
                    0,
                    GetShimmerSplashColor(),
                    1.1f
                );

                dust.noGravity = true;
            }

            // curPosition.X -= 16f;

            // Hand-picked ejection values
            SpawnSpike(-86f, 48f, 0.08f);
            SpawnSpike(-70f, 64f, 0.025f);
            SpawnSpike(-48f, 16f, 0.04f);
            SpawnSpike(-16f, 128f, 0.03f);
            SpawnSpike(0f, 200f, 0.055f);
            SpawnSpike(16f, 128f, 0.03f);
            SpawnSpike(48f, 16f, 0.04f);
            SpawnSpike(70f, 64f, 0.025f);
            SpawnSpike(86f, 48f, 0.08f);
        }

        void SpawnSpike(float offset, float height, float speed)
        {
            var position = curPosition;

            position.X += offset;

            var tilePosition = position.ToTileCoordinates();

            if (!Main.tile[tilePosition].HasShimmer)
            {
                return;
            }

            var innerOffset = (int)(position.X % 16);

            if (innerOffset > 8)
            {
                tilePosition.X += 1;
                innerOffset -= 16;
            }

            var style = Rand.NextBoolean(7, 3) ? (byte)0 : Rand.Next((byte)3);

            ElkShimmerParticles.Spikes += new ElkShimmerParticles.ShimmerSpike(tilePosition, innerOffset, height, style, 0f, speed);
        }

        static Color GetShimmerSplashColor()
        {
            return Rand.Next(6) switch
            {
                0 => new Color(255, 255, 210),
                1 => new Color(190, 245, 255),
                2 => new Color(255, 150, 255),
                _ => new Color(190, 175, 255),
            };
        }
    }

    [GlobalNPCHooks.PreAI]
    private static bool PreAI_ViolentShimmerReaction_Faelings(NPC npc)
    {
        if (npc.aiStyle != NPCAIStyleID.Firefly
         || npc.type != NPCID.Shimmerfly)
        {
            return true;
        }

        var count = 0;
        var zero = Vector2.Zero;
        foreach (var item in Main.ActiveItems)
        {
            var diff = npc.Center - item.Center;

            if (!item.shimmerWet
             || item.ShimmerData is not { } data
             || (data.WaveProgress <= 0f && data.SubSurfaceProgress <= 0f)
             || diff.Length() >= 900f)
            {
                continue;
            }

            count++;
            zero += diff.Normalized;
        }

        if (zero == Vector2.Zero)
        {
            return true;
        }

        zero /= count;
        zero *= 2f;

        npc.velocity += zero;

        if (npc.velocity.Length() > 7f)
        {
            npc.velocity.Magnitude = 7f;
        }

        npc.localAI[0] = 10f;
        npc.netUpdate = true;

        return true;
    }

    private static Vector2 FindShimmerSurface(WorldItem item, int maxTiles)
    {
        var curPosition = item.Bottom;
        for (var j = 0; j < maxTiles; j++)
        {
            var position = item.Bottom.ToTileCoordinates();
            position.Y -= j + 1;

            if (Main.tile[position].HasShimmer)
            {
                continue;
            }

            position.Y += 1;

            var liquidLevel = (float)Main.tile[position].LiquidAmount / byte.MaxValue;
            liquidLevel = (1f - liquidLevel) * 16f;

            curPosition = position.ToWorldCoordinates(item.Bottom.X % 16f, liquidLevel);

            break;
        }

        return curPosition;
    }
}
