#region

using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

#endregion

namespace Irelia
{
    internal static class Program
    {
        private const string championName = "Irelia";

        private static Menu config;

        private static Spell Q;
        private static Spell W;
        private static Spell E;
        private static Spell R;
        private static Obj_AI_Hero Player;

        public static Spell[] spellUp = { Q, E, W, Q, Q, R, Q, E, Q, E, R, E, W, E, W, R, W, W };

        private static bool toFire;
        private static int toCharges;
        private static bool toPacketCast;

        public static Items.Item kingSword, ravenousHydra, Tiamat, bilgewaterCutlass, ghostBlade;
        private static SpellSlot IgniteSlot;
        public static Orbwalking.Orbwalker Orbwalker { get; set; }

        /// SPACE
        /// SPACE
        /// SPACE
        /// SPACE

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
            AppDomain.CurrentDomain.UnhandledException +=
                delegate (object sender, UnhandledExceptionEventArgs eventArgs)
                {
                    var exception = eventArgs.ExceptionObject as Exception;
                    if (exception != null) Console.WriteLine(exception.Message);
                };
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName != championName)
                return;
            if (Player.IsDead) return;

            Q = new Spell(SpellSlot.Q, 650);
            W = new Spell(SpellSlot.W, Orbwalking.GetRealAutoAttackRange(ObjectManager.Player)); // Thanks ChewyMoon
            E = new Spell(SpellSlot.E, 325);
            R = new Spell(SpellSlot.R, 1000);

            Q.SetSkillshot(0.25f, 75f, 1500f, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.15f, 75f, 1500f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.15f, 80f, 1500f, false, SkillshotType.SkillshotLine);

            SetupMenu();

            // Items

            Player = ObjectManager.Player;

            kingSword = new Items.Item(3153, 450f);
            ravenousHydra = new Items.Item(3074, 450f);
            Tiamat = new Items.Item(3077, 420f);
            bilgewaterCutlass = new Items.Item(3144, 450f);

            // Summoner Spells

            IgniteSlot = Player.GetSpellSlot("SummonerDot");

            Game.OnGameUpdate += Game_OnGameUpdate;
            Interrupter.OnPossibleToInterrupt += InterrupterOnOnPossibleToInterrupt;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloserOnOnEnemyGapcloser;
            Drawing.OnDraw += Drawing_OnDraw;
            Utility.HpBarDamageIndicator.DamageToUnit += DamageToUnit;

            Game.PrintChat("<font color='#70DBDB'>Pandorum: Irelia</font> <font color='#FFFFFF'>Loaded!</font>");
        }

        private static float DamageToUnit(Obj_AI_Hero hero)
        {
            double dmg = 0;

            if (kingSword.IsReady())
                dmg += ObjectManager.Player.GetItemDamage(hero, Damage.DamageItems.Botrk);

            if (ravenousHydra.IsReady())
                dmg += ObjectManager.Player.GetItemDamage(hero, Damage.DamageItems.Hydra);

            if (Tiamat.IsReady())
                dmg += ObjectManager.Player.GetItemDamage(hero, Damage.DamageItems.Tiamat);

            if (bilgewaterCutlass.IsReady())
                dmg += ObjectManager.Player.GetItemDamage(hero, Damage.DamageItems.Bilgewater);

            if (Q.IsReady())
                dmg += ObjectManager.Player.GetSpellDamage(hero, SpellSlot.Q);

            if (W.IsReady())
                dmg += ObjectManager.Player.GetSpellDamage(hero, SpellSlot.W);

            if (E.IsReady())
                dmg += ObjectManager.Player.GetSpellDamage(hero, SpellSlot.E);

            // Thanks ChewyMoon
            if (R.IsReady())
                dmg += ObjectManager.Player.GetSpellDamage(hero, SpellSlot.R) * 4;

            return (float)dmg;
        }

        private static void AntiGapcloserOnOnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!config.Item("eOnGapclose").GetValue<bool>()) return;
            if (!E.IsReady()) return;

            E.Cast(gapcloser.Sender, toPacketCast);
        }

        private static void InterrupterOnOnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!config.Item("interruptUlts").GetValue<bool>()) return;
            if (spell.DangerLevel != InterruptableDangerLevel.High || !CanStunTarget(unit)) return;

            var range = unit.Distance(ObjectManager.Player);
            if (range <= E.Range)
            {
                if (E.IsReady())
                {
                    E.Cast(unit, toPacketCast);
                }
            }
            else if (range <= Q.Range)
            {
                if (!Q.IsReady() || !E.IsReady()) return;
                Q.Cast(unit, toPacketCast);
                E.Cast(unit, toPacketCast);
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var drawQ = config.Item("QDraw").GetValue<bool>();
            var drawE = config.Item("EDraw").GetValue<bool>();
            var drawR = config.Item("RDraw").GetValue<bool>();

            var position = ObjectManager.Player.Position;

            if (drawQ)
                Utility.DrawCircle(position, Q.Range, Q.IsReady() ? Color.Aqua : Color.Red);

            if (drawE)
                Utility.DrawCircle(position, E.Range, E.IsReady() ? Color.Aqua : Color.Red);

            if (drawR)
                Utility.DrawCircle(position, R.Range, R.IsReady() ? Color.Aqua : Color.Red);
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            toPacketCast = config.Item("packetCast").GetValue<bool>();

            FireCharges();

            if (!Orbwalking.CanMove(100)) return;

            // Orbwalker WaveClear
            if (config.Item("waveClear").GetValue<KeyBind>().Active && !ObjectManager.Player.IsDead)
            {
                WaveClear();
            }

                // Orbwalker Combo
                if (config.Item("comboActive").GetValue<KeyBind>().Active && !ObjectManager.Player.IsDead)
                {
                Combo();
                }

                    // Farm with Q ChewyMoon's Logic
                    if (config.Item("QLastHit").GetValue<KeyBind>().Active && config.Item("QLastHitEnable").GetValue<bool>() &&
                    !ObjectManager.Player.IsDead)
                    {
                    LastHitWithQ();
                    }
        }

        private static void WaveClear()
        {
            var useQ = config.Item("useQWC").GetValue<bool>();
            var useW = config.Item("useWWC").GetValue<bool>();
            var useR = config.Item("useRWC").GetValue<bool>();

            var minions = MinionManager.GetMinions(ObjectManager.Player.Position, 650);
            foreach (var minion in minions)
            {
                if (useQ)
                {
                    if (config.Item("useQWCKillable").GetValue<bool>())
                    {
                        var damage = ObjectManager.Player.GetSpellDamage(minion, SpellSlot.Q);

                        if (damage > minion.Health && Q.IsReady())
                            Q.Cast(minion, toPacketCast);
                    }
                    else
                    {
                        Q.Cast(minion, toPacketCast);
                    }
                }

                if (useW && W.IsReady()) W.Cast();
                if (useR && R.IsReady()) R.Cast(minion, toPacketCast);
            }
        }

        private static void LastHitWithQ()
        {
            var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range);
            foreach (
                var minion in
                    minions.Where(minion => ObjectManager.Player.GetSpellDamage(minion, SpellSlot.Q) > minion.Health))
            {
                var noFarmDangerous = config.Item("QNoFarmTower").GetValue<bool>();
                
                if (noFarmDangerous)
                {
                    if (minion.UnderTurret()) continue;
                    if (Q.IsReady())
                        Q.Cast(minion, toPacketCast);
                }
                else
                {
                    if (Q.IsReady())
                        Q.Cast(minion, toPacketCast);
                }
            }
        }

        private static void FireCharges()
        {
            if (!toFire) return;

            R.Cast(SimpleTs.GetTarget(1000, SimpleTs.DamageType.Physical), toPacketCast);
            toCharges -= 1;
            toFire = toCharges != 0;
        }

        private static void Combo()
        {
            var useQ = config.Item("useQ").GetValue<bool>();
            var useW = config.Item("useW").GetValue<bool>();
            var useE = config.Item("useE").GetValue<bool>();
            var useR = config.Item("useR").GetValue<bool>();
            var useEStun = config.Item("useEStun").GetValue<bool>();
            var target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);

            if (target == null)
            {
                GapCloseCombo();
            }
            if (target == null || !target.IsValid) return;

            var isUnderTower = target.UnderTurret();
            var diveTower = config.Item("diveTower").GetValue<bool>();
            var doNotCombo = false;

            if (isUnderTower && !diveTower)
            {
                
                var percent = (int)target.Health / target.MaxHealth * 100;
                var overridePercent = config.Item("diveTowerPercent").GetValue<Slider>().Value;

                if (percent > overridePercent) doNotCombo = true;
            }

            if (doNotCombo) return;

            if (useW && W.IsReady())
            {
                W.Cast();
            }

            if (useQ && Q.IsReady())
            {
                if (config.Item("dontQ").GetValue<bool>())
                {
                    var distance = ObjectManager.Player.Distance(target);

                    if (distance > config.Item("dontQRange").GetValue<Slider>().Value)
                    {
                        Q.Cast(target, toPacketCast);
                    }
                }
                else
                {
                    Q.Cast(target, toPacketCast);
                }
            }

            if (kingSword.IsReady())
                kingSword.Cast(target);

            if (ravenousHydra.IsReady())
                ravenousHydra.Cast(target);

            if (Tiamat.IsReady())
                Tiamat.Cast(target);

            if (bilgewaterCutlass.IsReady())
                bilgewaterCutlass.Cast(target);

            if (useE && E.IsReady())
            {
                if (useEStun)
                {
                    if (CanStunTarget(target))
                    {
                        E.Cast(target, toPacketCast);
                    }
                }
                else
                {
                    E.Cast(target, toPacketCast);
                }
            }

            if (!useR || !R.IsReady() || toFire) return;
            toFire = true;
            toCharges = 4;
        }

        // KillSteal Option [ NEWWW! ]
        private static void KillSteal(Obj_AI_Hero target)
        {
            var dmgIgnite = Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
            var dmgQ = Player.GetSpellDamage(target, SpellSlot.Q);
            var dmgE = Player.GetSpellDamage(target, SpellSlot.E);
            var dmgR = Player.GetSpellDamage(target, SpellSlot.R);

            if (target.IsValidTarget())
            {
                if (config.Item("useIgnite").GetValue<bool>() && Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                {
                    if (dmgIgnite > target.Health && Player.Distance(target) < 600)
                    {
                        Player.SummonerSpellbook.CastSpell(IgniteSlot, target);
                    }
                }
            }

            if (config.Item("UseQKs").GetValue<bool>())
            {
                if (dmgQ > target.Health && Player.Distance(target) <= Q.Range)
                {
                    Q.Cast(target);
                }
            }

            if (config.Item("UseEKs").GetValue<bool>())
            {
                if (dmgE > target.Health && Player.Distance(target) <= E.Range)
                {
                    E.Cast(target);
                }
            }

            if (config.Item("UseRKs").GetValue<bool>())
            {
                if (dmgR > target.Health && Player.Distance(target) <= R.Range)
                {
                    R.Cast(target);
                }
            }
        }

        private static void GapCloseCombo()
        {
            if (!config.Item("useMinionGapclose").GetValue<bool>()) return;

            var target = SimpleTs.GetTarget(Q.Range * 3, SimpleTs.DamageType.Physical);
            if (!target.IsValidTarget() || target == null) return;

            foreach (
                var minion in
                    MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range)
                        .Where(minion => ObjectManager.Player.GetSpellDamage(minion, SpellSlot.Q) > minion.Health &&
                                         minion.ServerPosition.Distance(target.ServerPosition) < Q.Range)
                        .Where(minion => minion.IsValidTarget(Q.Range * 3))
                        .Where(minion => Q.IsReady()))
            {
                Q.Cast(minion, toPacketCast);
                break;
            }
        }

        private static bool CanStunTarget(AttackableUnit target)
        {
            var enemyHealthPercent = target.Health / target.MaxHealth * 100;
            var myHealthPercent = ObjectManager.Player.Health / ObjectManager.Player.MaxHealth * 100;

            return enemyHealthPercent > myHealthPercent;
        }

        private static void SetupMenu()
        {
            config = new Menu("Irelia", "Irelia", true);

            // Target Selector
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
           config.AddSubMenu(targetSelectorMenu);

            // Orbwalker
            var orbwalkerMenu = new Menu("Orbwalker", "Orbwalker");
            Orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            config.AddSubMenu(orbwalkerMenu);

            // Combo
            var comboMenu = new Menu("Combo", "Combo");
            comboMenu.AddItem(new MenuItem("useQ", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem("useW", "Use W").SetValue(true));
            comboMenu.AddItem(new MenuItem("useE", "Use E").SetValue(true));
            comboMenu.AddItem(new MenuItem("useEStun", "Use e only if target can be stunned").SetValue(false));
            comboMenu.AddItem(new MenuItem("useR", "Use R").SetValue(true));
            comboMenu.AddItem(new MenuItem("useMinionGapclose", "Q minion gap closer").SetValue(true));
            config.AddSubMenu(comboMenu);

            // Farm Options
            var farmingMenu = new Menu("Farm", "Farm");
            farmingMenu.AddItem(new MenuItem("QLasthitEnable", "Last hitting with Q").SetValue(false));
            farmingMenu.AddItem(new MenuItem("QLastHit", "Last hit with Q").SetValue(new KeyBind(88, KeyBindType.Press)));
            farmingMenu.AddItem(new MenuItem("QNoFarmTower", "Don't Q minions under tower").SetValue(false));

            var waveClearMenu = new Menu("Wave Clear", "Wave Clear");
            waveClearMenu.AddItem(new MenuItem("useQWC", "Use Q").SetValue(true));
            waveClearMenu.AddItem(new MenuItem("useWWC", "Use W").SetValue(true));
            waveClearMenu.AddItem(new MenuItem("useRWC", "Use R").SetValue(false));
            waveClearMenu.AddItem(new MenuItem("useQWCKillable", "Only Q killable minions").SetValue(true));
            waveClearMenu.AddItem(new MenuItem("waveClear", "Wave Clear!").SetValue(new KeyBind(86, KeyBindType.Press)));
            farmingMenu.AddSubMenu(waveClearMenu);
            config.AddSubMenu(farmingMenu);

            var KillSteal = new Menu("KSMenu", "KillSteal");
            KillSteal.AddItem(new MenuItem("useIgnite", "Use Ignite").SetValue(true));
            KillSteal.AddItem(new MenuItem("UseQKs", "Use Q").SetValue(true));
            KillSteal.AddItem(new MenuItem("UseEKs", "Use E").SetValue(true));
            KillSteal.AddItem(new MenuItem("UseRKs", "Use R").SetValue(true));

            var drawingMenu = new Menu("Drawings", "Drawings");
            drawingMenu.AddItem(new MenuItem("QDraw", "Draw Q [Range]").SetValue(true));
            drawingMenu.AddItem(new MenuItem("EDraw", "Draw E [Range]").SetValue(false));
            drawingMenu.AddItem(new MenuItem("RDraw", "Draw R [Range]").SetValue(true));
            drawingMenu.AddItem(new MenuItem("comboDraw", "Draw Combo Damage").SetValue(true));
            drawingMenu.Item("comboDraw").ValueChanged +=
                (sender, args) => Utility.HpBarDamageIndicator.Enabled = args.GetNewValue<bool>();
            config.AddSubMenu(drawingMenu);

            var miscMenu = new Menu("Misc", "Misc");
            miscMenu.AddItem(new MenuItem("interruptUlts", "Interrupt ults with E").SetValue(true));
            miscMenu.AddItem(new MenuItem("interruptQE", "Q + E to interrupt if not in range").SetValue(true));
            // Packets RIP
            // miscMenu.AddItem(new MenuItem("packetCast", "Use packets to cast spells").SetValue(false)); 
            miscMenu.AddItem(new MenuItem("diveTower", "Dive tower when combo'ing").SetValue(false));
            miscMenu.AddItem(new MenuItem("diveTowerPercent", "Override dive tower").SetValue(new Slider(10)));
            miscMenu.AddItem(new MenuItem("dontQ", "Dont Q if range is small").SetValue(false));
            miscMenu.AddItem(new MenuItem("dontQRange", "Q Range").SetValue(new Slider(200, 0, 650)));
            miscMenu.AddItem(new MenuItem("eOnGapclose", "E on Gapcloser").SetValue(true));
            config.AddSubMenu(miscMenu);

            config.AddItem(new MenuItem("comboActive", "Combo").SetValue(new KeyBind(32, KeyBindType.Press)));

            config.AddToMainMenu();
        }
    }
}