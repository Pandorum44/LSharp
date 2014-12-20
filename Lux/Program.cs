using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace Lux
{
    internal static class Program
    {

        private static Menu Config;
        private static Obj_AI_Hero Target;
        private static Obj_AI_Hero Player;
        private static Spell Q, W , E ,R;

        private static SpellSlot IgniteSlot;
        private static GameObject ESpell;
        private static HitChance hitChance;
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            // Player Declaration
            Player = ObjectManager.Player;

            if ( Player.ChampionName != "Kennen") return;

            Q = new Spell(SpellSlot.Q, 1175);
            W = new Spell(SpellSlot.W, 1075);
            E = new Spell(SpellSlot.E, 1100);
            R = new Spell(SpellSlot.R, 3340);
            IgniteSlot = Player.GetSpellSlot("SummonerDot");

            Q.SetSkillshot(0.25f, 80f, 1200f, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.25f, 150f, 1200f, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.15f, 275f, 1300f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(1.35f, 190f, float.MaxValue, false, SkillshotType.SkillshotLine);

            Config = new Menu("Lightning Lux", "Lightning Lux", true);
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            var orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalker"));

            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQ", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseW", "Use W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseE", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseR", "Use R if Killable").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseItems", "Use Items").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseIgnite", "Use Ignite").SetValue(true));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("Harass").AddItem(new MenuItem("HQ", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("HE", "Use E").SetValue(true));

            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("FarmActive", "Farm!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("Farm").AddItem(new MenuItem("JungSteal", "JungSteal!").SetValue(new KeyBind("J".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("Farm").AddItem(new MenuItem("FQ", "Use Q").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("FW", "Use W").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("FE", "Use E").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("FMP", "My MP %").SetValue(new Slider(15, 100, 0)));

            Config.AddSubMenu(new Menu("KillSteal", "KillSteal"));
            Config.SubMenu("KillSteal").AddItem(new MenuItem("KUseQ", "Use Q").SetValue(true));
            Config.SubMenu("KillSteal").AddItem(new MenuItem("KUseE", "Use E").SetValue(true));
            Config.SubMenu("KillSteal").AddItem(new MenuItem("KUseR", "Use R").SetValue(true));
            Config.SubMenu("KillSteal").AddItem(new MenuItem("KIgnite", "Use Ignite").SetValue(true));

            Config.AddSubMenu(new Menu("AutoShield", "AutoShield"));
            Config.SubMenu("AutoShield").AddItem(new MenuItem("WAllies", "Auto W for Allies").SetValue(true));
            Config.SubMenu("AutoShield").AddItem(new MenuItem("AutoW", "Auto W when Lux is targeted").SetValue(true));
            Config.SubMenu("AutoShield").AddItem(new MenuItem("HP", "Allies HP %").SetValue(new Slider(60, 100, 0)));
            Config.SubMenu("AutoShield").AddItem(new MenuItem("MP", "My MP %").SetValue(new Slider(30, 100, 0)));

            Config.AddSubMenu(new Menu("ExtraSettings", "ExtraSettings"));
            Config.SubMenu("ExtraSettings").AddItem(new MenuItem("UseQE", "Only E if Target trapped").SetValue(false));
            Config.SubMenu("ExtraSettings").AddItem(new MenuItem("AutoE2", "Auto pop E").SetValue(true));
            Config.SubMenu("ExtraSettings").AddItem(new MenuItem("UseQGap", "Q on GapCloser").SetValue(true));
            Config.SubMenu("ExtraSettings").AddItem(new MenuItem("HitChance", "HitChance").SetValue(new StringList(new[] { "Low", "Medium", "High", "Very High" }, 2)));
            Config.SubMenu("ExtraSettings").AddItem(new MenuItem("UsePacket", "Use Packet Cast").SetValue(true));

            Config.AddSubMenu(new Menu("UltSettings", "UltSettings"));
            Config.SubMenu("UltSettings").AddItem(new MenuItem("RHit", "Auto R if hit").SetValue(new StringList(new[] { "None", "2 Target", "3 Target", "4 Target", "5 Target" }, 1)));
            Config.SubMenu("UltSettings").AddItem(new MenuItem("RTrap", "Auto R if trapped").SetValue(false));

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("WRange", "W range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("ERange", "E range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
            Config.AddToMainMenu();

            Game.OnGameUpdate += Game_OnGameUpdate;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
            GameObject.OnCreate += OnCreateObject;
            GameObject.OnDelete += OnDeleteObject;

            Game.PrintChat("Pandorum | Lux - Loaded");

        }
        private static bool GetBool(string s)
        {
            return Config.Item(s).GetValue<bool>();
        }

        private static bool GetActive(string s)
        {
            return Config.Item(s).GetValue<KeyBind>().Active;
        }

        private static LeagueSharp.Common.Circle GetCircle(string s)
        {
            return Config.Item(s).GetValue<Circle>();
        }

        private static int GetSlider(string s)
        {
            return Config.Item(s).GetValue<Slider>().Value;
        }

        private static int GetSelected(string s)
        {
            return Config.Item(s).GetValue<StringList>().SelectedIndex;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            KillSteal();

            Target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);

            if (GetActive("ComboActive")) UseCombo();
            else if (GetActive("HarassActive")) Harass();
            else if (GetActive("FarmActive")) Farm();
            else if (GetActive("JungSteal")) JungSteal();

            if (GetBool("WAllies")) AutoShield();
            if (GetBool("AutoE2")) CastE2();
            if (GetBool("RTrap")) RTrapped();

            if (GetSelected("HitChance") == 0) hitChance = HitChance.Low;
            else if (GetSelected("HitChance") == 1) hitChance = HitChance.Medium;
            else if (GetSelected("HitChance") == 2) hitChance = HitChance.High;
            else if (GetSelected("HitChance") == 3) hitChance = HitChance.VeryHigh;

            if (GetSelected("RHit") == 0) RHit(0);
            else if (GetSelected("RHit") == 1) RHit(2);
            else if (GetSelected("RHit") == 2) RHit(3);
            else if (GetSelected("RHit") == 3) RHit(4);
            else if (GetSelected("RHit") == 4) RHit(5);
        }

        private static void OnCreateObject(GameObject sender, EventArgs args)
        {
            if (sender.Name.Contains("LuxLightstrike_tar_green"))
            {
                ESpell = sender;
                return;
            }
        }

        private static void OnDeleteObject(GameObject sender, EventArgs args)
        {
            if (sender.Name.Contains("LuxLightstrike_tar_green"))
            {
                ESpell = null;
                return;
            }
        }

        private static Obj_AI_Hero GrabAlly()
        {
            Obj_AI_Hero Ally = null;
            foreach (var hero in from hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(W.Range) && hero.IsAlly) let heroPercent = hero.Health / hero.MaxHealth * 100 let shieldPercent = GetSlider("HP") where heroPercent <= shieldPercent select hero)
            {
                Ally = hero;
                break;
            }
            return Ally;
        }

        private static void RHit(int x)
        {
            var rtarget = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Magical);
            R.CastIfWillHit(rtarget, x, GetBool("UsePacket"));
        }

        private static void RTrapped()
        {
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(R.Range) && !hero.IsDead && hero.HasBuff("LuxLightBindingMis")))
            {
                R.Cast(hero, GetBool("UsePacket"));
                return;
            }
        }

        private static bool IsOnTheLine(Vector3 point, Vector3 start, Vector3 end)
        {
            var obj = Geometry.ProjectOn(point.To2D(), start.To2D(), end.To2D());
            if (obj.IsOnSegment) return true;
            return false;
        }

        private static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsEnemy && sender.Type == GameObjectType.obj_AI_Minion)
            {
                if (GetBool("FW") && W.IsReady() && GetActive("FarmActive") && args.Target.Name == Player.Name && Player.Mana / Player.MaxMana * 100 >= GetSlider("MP"))
                {
                    if (GrabAlly() == null) W.Cast(sender, GetBool("UsePacket"));
                    else W.CastIfHitchanceEquals(GrabAlly(), hitChance, GetBool("UsePacket"));
                }

            }
            if (GetBool("AutoW") && W.IsReady() && sender.IsEnemy && (sender.Type == GameObjectType.obj_AI_Hero || sender.Type == GameObjectType.obj_AI_Turret))
            {
                if ((args.SData.Name != null && IsOnTheLine(Player.Position, args.Start, args.End)) || (args.Target == Player && Player.Distance(sender) <= 450) || args.Target == Player && Utility.UnderTurret(Player, true))
                {
                    if (GrabAlly() == null) W.Cast(sender, GetBool("UsePacket"));
                    else W.CastIfHitchanceEquals(GrabAlly(), hitChance, GetBool("UsePacket"));
                }
            }

        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (GetCircle("QRange").Active && !Player.IsDead)
            {
                Utility.DrawCircle(Player.Position, Q.Range, GetCircle("QRange").Color);
            }
            if (GetCircle("WRange").Active && !Player.IsDead)
            {
                Utility.DrawCircle(Player.Position, W.Range, GetCircle("WRange").Color);
            }
            if (GetCircle("ERange").Active && !Player.IsDead)
            {
                Utility.DrawCircle(Player.Position, E.Range, GetCircle("ERange").Color);
            }
            if (GetActive("JungSteal") && !Player.IsDead)
            {
                Utility.DrawCircle(Game.CursorPos, 900, Color.White);
            }
        }

        private static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (GetActive("ComboActive"))
                args.Process = (!Q.IsReady() || !E.IsReady() || Player.Distance(args.Target) >= 550);
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Player.HasBuff("Recall") || Player.IsWindingUp) return;
            if (GetBool("UseQGap") && Q.IsReady() && GetDistanceSqr(Player, gapcloser.Sender) <= Q.Range * Q.Range) Q.CastIfHitchanceEquals(gapcloser.Sender, hitChance, GetBool("UsePacket"));
        }

        private static void AutoShield()
        {
            if (Player.Mana / Player.MaxMana * 100 >= GetSlider("MP")) W.CastIfHitchanceEquals(GrabAlly(), hitChance, GetBool("UsePacket"));
        }

        private static bool IgniteKillable(Obj_AI_Base Target)
        {
            return Player.GetSummonerSpellDamage(Target, Damage.SummonerSpell.Ignite) >= Target.Health;
        }

        private static float GetDistanceSqr(Obj_AI_Hero source, Obj_AI_Base Target)
        {
            return Vector2.DistanceSquared(source.Position.To2D(), Target.ServerPosition.To2D());
        }

        private static double ComboDmg(Obj_AI_Hero target)
        {
            double damage = 0;
            if (Q.IsReady() && Q.InRange(target.Position))
                damage += Player.GetSpellDamage(target, SpellSlot.Q);
            if (E.IsReady() && E.InRange(target.Position))
                damage += Player.GetSpellDamage(target, SpellSlot.E);
            if (R.IsReady() && R.InRange(target.Position))
                damage += Player.GetSpellDamage(target, SpellSlot.R);
            if ((Items.HasItem(3128) && Items.CanUseItem(3128)) || (Items.HasItem(3188) && Items.CanUseItem(3188)) && GetDistanceSqr(Player, Target) <= 750 * 750)
                damage += damage * 1.2;
            if (CanIgnite() && Player.Distance(target) <= 600) damage += Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
            return damage;
        }

        private static bool CanIgnite()
        {
            return (IgniteSlot != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready);
        }

        private static void UseCombo()
        {
            if (Target == null) return;

            bool AllSkills = false;
            if (ComboDmg(Target) > Target.Health && Target.Distance(Player) <= 950) AllSkills = true;

            if (GetBool("UseItems") && AllSkills && GetDistanceSqr(Player, Target) <= 750 * 750)
            {
                if (Items.CanUseItem(3128)) Items.UseItem(3128, Target);
                if (Items.CanUseItem(3188)) Items.UseItem(3188, Target);
            }
            if (GetBool("UseQ") && Q.IsReady() && GetDistanceSqr(Player, Target) <= Q.Range * Q.Range)
            {
                Q.CastIfHitchanceEquals(Target, hitChance, GetBool("UsePacket"));
                if (Target.IsValidTarget(550) && Target.HasBuff("luxilluminatingfraulein"))
                    Player.IssueOrder(GameObjectOrder.AttackUnit, Target);
            }
            if (GetBool("UseW") && W.IsReady() && Utility.IsFacing(Player, Target, 450) && Player.Distance(Target) <= 450)
            {
                W.Cast(Target, GetBool("UsePacket"));
            }
            if (GetBool("UseE") && E.IsReady() && GetDistanceSqr(Player, Target) <= E.Range * E.Range)
            {
                if (GetBool("UseQE"))
                {
                    if (Target.HasBuff("LuxLightBindingMis"))
                    {
                        E.Cast(Target, GetBool("UsePacket"));
                        CastE2();
                    }
                }
                else
                {
                    E.CastIfHitchanceEquals(Target, hitChance, GetBool("UsePacket"));
                    CastE2();
                }
                if (Target.IsValidTarget(550) && Target.HasBuff("luxilluminatingfraulein"))
                    Player.IssueOrder(GameObjectOrder.AttackUnit, Target);
            }
            if (GetBool("UseR") && R.IsReady() && (R.IsKillable(Target) || AllSkills))
            {
                if (Target.Health <= Damage.GetAutoAttackDamage(Player, Target, true) && Player.Distance(Target) < 550)
                    Player.IssueOrder(GameObjectOrder.AttackUnit, Target);
                else R.CastIfHitchanceEquals(Target, HitChance.High, GetBool("UsePacket"));
            }
            if (GetBool("UseIgnite") && (IgniteKillable(Target) || AllSkills) && CanIgnite())
            {
                if (Player.Distance(Target) <= 600)
                    if (Target.Health <= Damage.GetAutoAttackDamage(Player, Target, true) && Player.Distance(Target) < 550)
                        Player.IssueOrder(GameObjectOrder.AttackUnit, Target);
                    else Player.Spellbook.CastSpell(IgniteSlot, Target);
            }
        }


        private static void Harass()
        {
            if (Target == null) return;
            if (GetBool("HQ") && Q.IsReady() && GetDistanceSqr(Player, Target) <= Q.Range * Q.Range)
            {
                Q.CastIfHitchanceEquals(Target, hitChance, GetBool("UsePacket"));
                if (Target.IsValidTarget(550) && Target.HasBuff("luxilluminatingfraulein"))
                    Player.IssueOrder(GameObjectOrder.AttackUnit, Target);
            }
            if (GetBool("HE") && E.IsReady() && GetDistanceSqr(Player, Target) <= E.Range * E.Range)
            {
                if (GetBool("UseQE"))
                {
                    if (Target.HasBuff("LuxLightBindingMis"))
                    {
                        E.CastIfHitchanceEquals(Target, hitChance, GetBool("UsePacket"));
                        CastE2();
                    }
                }
                else
                {
                    E.CastIfHitchanceEquals(Target, hitChance, GetBool("UsePacket"));
                    CastE2();
                }
                if (Target.IsValidTarget(550) && Target.HasBuff("luxilluminatingfraulein"))
                    Player.IssueOrder(GameObjectOrder.AttackUnit, Target);
            }
        }

        private static double CalculateDmg(Obj_AI_Base target)
        {
            double damage = 0;
            if (Q.IsReady() && Q.InRange(target.Position))
                damage += Player.GetSpellDamage(target, SpellSlot.Q);
            if (E.IsReady() && E.InRange(target.Position))
                damage += Player.GetSpellDamage(target, SpellSlot.E);
            if (R.IsReady() && R.InRange(target.Position))
                damage += Player.GetSpellDamage(target, SpellSlot.R);
            return damage;
        }

        private static void JungSteal()
        {
            var Minions = MinionManager.GetMinions(Game.CursorPos, 1000, MinionTypes.All, MinionTeam.Neutral);
            foreach (var minion in Minions.Where(minion => minion.IsVisible && !minion.IsDead))
            {
                if ((minion.SkinName == "SRU_Blue" || minion.SkinName == "SRU_Red" || minion.SkinName == "SRU_Baron" || minion.SkinName == "SRU_Dragon") &&
                    CalculateDmg(minion) > minion.Health)
                {
                    if (Q.IsReady() && GetDistanceSqr(Player, minion) <= Q.Range * Q.Range) Q.Cast(minion, GetBool("UsePacket"));
                    if (E.IsReady() && GetDistanceSqr(Player, minion) <= E.Range * E.Range)
                    {
                        E.Cast(minion, GetBool("UsePacket"));
                        while (ESpell != null)
                        {
                            E.Cast(GetBool("UsePacket"));
                            break;
                        }
                    }
                    if (R.IsReady() && minion.IsValidTarget(R.Range)) R.Cast(minion, GetBool("UsePacket"));
                }
            }
        }

        private static void KillSteal()
        {
            if (GetBool("KUseQ") || GetBool("KUseE") || GetBool("KUseR") || GetBool("KIgnite"))
            {
                foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(R.Range) && hero.IsEnemy && !hero.IsDead))
                {
                    if (GetBool("KUseQ") && Q.IsReady() && GetDistanceSqr(Player, hero) <= Q.Range * Q.Range && Q.IsKillable(hero))
                        Q.CastIfHitchanceEquals(hero, HitChance.High, GetBool("UsePacket"));
                    else if (GetBool("KUseE") && E.IsReady() && GetDistanceSqr(Player, hero) <= E.Range * E.Range && E.IsKillable(hero))
                    {
                        E.CastIfHitchanceEquals(hero, HitChance.High, GetBool("UsePacket"));
                        CastE2();
                    }
                    else if (GetBool("KUseR") && R.IsReady() && hero.IsValidTarget(R.Range) && R.IsKillable(hero))
                    {
                        if (hero.Health <= Damage.GetAutoAttackDamage(Player, hero, true) && Player.Distance(hero) < 550)
                            Player.IssueOrder(GameObjectOrder.AttackUnit, hero);
                        else R.CastIfHitchanceEquals(hero, HitChance.High, GetBool("UsePacket"));
                    }
                    else if (GetBool("KIgnite") && IgniteKillable(hero) && CanIgnite())
                    {
                        if (Player.Distance(hero) <= 600)
                        {
                            if (hero.Health <= Damage.GetAutoAttackDamage(Player, hero, true) && Player.Distance(hero) < 550)
                                Player.IssueOrder(GameObjectOrder.AttackUnit, hero);
                            else Player.Spellbook.CastSpell(IgniteSlot, hero);
                        }
                    }
                }
            }
        }


        private static void CastE2()
        {
            if (ESpell == null) return;
            foreach (Obj_AI_Hero current in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget() && enemy.IsEnemy &&
                                                                                   Vector3.Distance(ESpell.Position, enemy.ServerPosition) <= E.Width + 15))
            {
                E.Cast(GetBool("UsePacket"));
                return;
            }
            if (Vector3.Distance(Player.Position, ESpell.Position) > 800) E.Cast(GetBool("UsePacket"));
        }

        private static void Farm()
        {
            var Minions = MinionManager.GetMinions(Player.Position, E.Range, MinionTypes.All, MinionTeam.NotAlly);
            if (Minions.Count == 0) return;
            if (Player.Mana / Player.MaxMana * 100 >= GetSlider("MP"))
            {
                if (GetBool("FQ") && Q.IsReady())
                {
                    var castPostion = MinionManager.GetBestLineFarmLocation(Minions.Select(minion => minion.ServerPosition.To2D()).ToList(), Q.Width, Q.Range);
                    Q.Cast(castPostion.Position, GetBool("UsePacket"));
                }
                if (GetBool("FE") && E.IsReady())
                {
                    var castPostion = MinionManager.GetBestCircularFarmLocation(Minions.Select(minion => minion.ServerPosition.To2D()).ToList(), E.Width, E.Range);
                    E.Cast(castPostion.Position, GetBool("UsePacket"));
                    E.Cast(GetBool("UsePacket"));
                }
            }
        }
    }
}
    

