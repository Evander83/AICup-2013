using System;
using System.Collections;
using Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeTroopers2013.DevKit.CSharpCgdk
{
    public sealed class MyStrategy : IStrategy
    {
        private readonly Random random = new Random(); 
        public abstract class MyUnit
        {
            private readonly long id;
            private int x;
            private int y;

            protected MyUnit(long id, int x, int y)
            {
                this.id = id;
                this.x = x;
                this.y = y;
            }
            public long Id
            {
                get { return id; }
            }
            public int X
            {
                get { return x; }
                set { x = value; }
            }
            public int Y
            {
                get { return y; }
                set { y = value; }
            }
            public double GetDistanceTo(int x, int y)
            {
                int xRange = x - this.x;
                int yRange = y - this.y;
                return Math.Sqrt(xRange * xRange + yRange * yRange);
            }
            public double GetDistanceTo(Unit unit)
            {
                return GetDistanceTo(unit.X, unit.Y);
            }
        }
        public sealed class MyTrooper : MyUnit
        {
            private readonly long playerId;
            private readonly int teammateIndex;
            private readonly bool isTeammate;

            private readonly TrooperType type;
            private TrooperStance stance;

            private int hitpoints;
            private readonly int maximalHitpoints;

            private int actionPoints;
            private readonly int initialActionPoints;

            private readonly double visionRange;
            private readonly double shootingRange;

            private readonly int shootCost;
            private readonly int standingDamage;
            private readonly int kneelingDamage;
            private readonly int proneDamage;
            private readonly int damage;

            private bool isHoldingGrenade;
            private bool isHoldingMedikit;
            private bool isHoldingFieldRation;

            public MyTrooper(Trooper trooper)
                : base(trooper.Id, trooper.X, trooper.Y)
            {
                this.playerId = trooper.PlayerId;
                this.teammateIndex = trooper.TeammateIndex;
                this.isTeammate = trooper.IsTeammate;
                this.type = trooper.Type;
                this.stance = trooper.Stance;
                this.hitpoints = trooper.Hitpoints;
                this.maximalHitpoints = trooper.MaximalHitpoints;
                this.actionPoints = trooper.ActionPoints;
                this.initialActionPoints = trooper.InitialActionPoints;
                this.visionRange = trooper.VisionRange;
                this.shootingRange = trooper.ShootingRange;
                this.shootCost = trooper.ShootCost;
                this.standingDamage = trooper.StandingDamage;
                this.kneelingDamage = trooper.KneelingDamage;
                this.proneDamage = trooper.ProneDamage;
                this.damage = trooper.Damage;
                this.isHoldingGrenade = trooper.IsHoldingGrenade;
                this.isHoldingMedikit = trooper.IsHoldingMedikit;
                this.isHoldingFieldRation = trooper.IsHoldingFieldRation;
            }

            public long PlayerId
            {
                get { return playerId; }
            }
            public int TeammateIndex
            {
                get { return teammateIndex; }
            }
            public bool IsTeammate
            {
                get { return isTeammate; }
            }
            public TrooperType Type
            {
                get { return type; }
            }
            public TrooperStance Stance
            {
                get { return stance; }
                set { stance = value; }
            }
            public int Hitpoints
            {
                get { return hitpoints; }
                set { hitpoints = value; }
            }
            public int MaximalHitpoints
            {
                get { return maximalHitpoints; }
            }
            public int ActionPoints
            {
                get { return actionPoints; }
                set { actionPoints = value; }
            }
            public int InitialActionPoints
            {
                get { return initialActionPoints; }
            }
            public double VisionRange
            {
                get { return visionRange; }
            }
            public double ShootingRange
            {
                get { return shootingRange; }
            }
            public int ShootCost
            {
                get { return shootCost; }
            }
            public int StandingDamage
            {
                get { return standingDamage; }
            }
            public int KneelingDamage
            {
                get { return kneelingDamage; }
            }
            public int ProneDamage
            {
                get { return proneDamage; }
            }
            public int Damage
            {
                get { return GetDamage(stance); }
            }
            public bool IsHoldingGrenade
            {
                get { return isHoldingGrenade; }
                set { isHoldingGrenade = value; }
            }
            public bool IsHoldingMedikit
            {
                get { return isHoldingMedikit; }
                set { isHoldingMedikit = value; }
            }
            public bool IsHoldingFieldRation
            {
                get { return isHoldingFieldRation; }
                set { isHoldingFieldRation = value; }
            }
            public int GetDamage(TrooperStance stance)
            {
                switch (stance)
                {
                    case TrooperStance.Prone:
                        return proneDamage;
                    case TrooperStance.Kneeling:
                        return kneelingDamage;
                    case TrooperStance.Standing:
                        return standingDamage;
                    default:
                        throw new ArgumentException("Unsupported stance: " + stance + '.');
                }
            }
        }

        Trooper m_self;
        Game m_game;
        World m_world;
        CellType[][] m_cells;
        int[][] m_cellDangerous = null;

        // Стоимость хода
        private int MoveCost(MyTrooper trooper)
        {
            if (trooper.Stance == TrooperStance.Prone) return m_game.ProneMoveCost;
            if (trooper.Stance == TrooperStance.Kneeling) return m_game.KneelingMoveCost;
            return m_game.StandingMoveCost;
        }
        // Опасность клеток
        private void CellDangerous()
        {
            m_cellDangerous = new int[m_world.Width][];
            for (int x = 0; x < m_world.Width; x++)
            {
                m_cellDangerous[x] = new int[m_world.Height];
                for (int y = 0; y < m_world.Height; y++)
                {
                    m_cellDangerous[x][y] = 0;
                    for (int i = 0; i < m_world.Width; i++)
                        for (int j = 0; j < m_world.Height; j++)
                        {
                            if (m_world.IsVisible(10, i, j, TrooperStance.Standing, x, y, TrooperStance.Standing)) m_cellDangerous[x][y]++;
                            if (m_world.IsVisible(10, i, j, TrooperStance.Standing, x, y, TrooperStance.Kneeling)) m_cellDangerous[x][y]++;
                            if (m_world.IsVisible(10, i, j, TrooperStance.Standing, x, y, TrooperStance.Prone)) m_cellDangerous[x][y]++;
                            if (m_world.IsVisible(10, x, y, TrooperStance.Standing, i, j, TrooperStance.Standing)) m_cellDangerous[x][y]--;
                            if (m_world.IsVisible(10, x, y, TrooperStance.Standing, i, j, TrooperStance.Kneeling)) m_cellDangerous[x][y]--;
                            if (m_world.IsVisible(10, x, y, TrooperStance.Standing, i, j, TrooperStance.Prone)) m_cellDangerous[x][y]--;
                        }
                }
            }
        }

        // Поиск оптимального пути
        struct DirDist
        {
            public Direction dir;
            public int dist;
        }
        private DirDist FindNextStep(int srcX, int srcY, int tagX, int tagY, int MaxStep)
        {          
            DirDist dd;
            dd.dist = (tagX - srcX) * (tagX - srcX) + (tagY - srcY) * (tagY - srcY);
            dd.dir = Direction.CurrentPoint;

            if (MaxStep == 0) return dd;

            // Отмечаем текущую точку занятой, чтобы не возвращаться при поиске, ищем дальнейший путь
            m_cells[srcX][srcY] = CellType.HighCover;
            for (int i = 0; i < MaxStep && dd.dist > 0; i++)
            {
                // Пробуем идти на восток EAST
                if (srcX < m_world.Width - 1)
                    if (m_cells[srcX + 1][srcY] == CellType.Free)
                    {
                        DirDist next = FindNextStep(srcX + 1, srcY, tagX, tagY, i);
                        if (next.dist < dd.dist)
                        {
                            dd.dir = Direction.East;
                            dd.dist = next.dist;
                        }
                    }

                // Пробуем идти на запад WEST
                if (srcX > 0)
                    if (m_cells[srcX - 1][srcY] == CellType.Free)
                    {
                        DirDist next = FindNextStep(srcX - 1, srcY, tagX, tagY, i);
                        if (next.dist < dd.dist)
                        {
                            dd.dir = Direction.West;
                            dd.dist = next.dist;
                        }
                    }

                // Пробуем идти на север NORTH
                if (srcY > 0)
                    if (m_cells[srcX][srcY - 1] == CellType.Free)
                    {
                        DirDist next = FindNextStep(srcX, srcY - 1, tagX, tagY, i);
                        if (next.dist < dd.dist)
                        {
                            dd.dir = Direction.North;
                            dd.dist = next.dist;
                        }
                    }

                // Пробуем идти на юг SOUTH
                if (srcY < m_world.Height - 1)
                    if (m_cells[srcX][srcY + 1] == CellType.Free)
                    {
                        DirDist next = FindNextStep(srcX, srcY + 1, tagX, tagY, i);
                        if (next.dist < dd.dist)
                        {
                            dd.dir = Direction.South;
                            dd.dist = next.dist;
                        }
                    }
            }

            // Освобождаем занятую клетку
            m_cells[srcX][srcY] = CellType.Free;
            return dd;
        }
        
        // Цель движения и номер хода с которого отсутствуют враги
        static int moveWithoutEnemy = 0;
        static int targetX = 15;
        static int targetY = 10;

        static ArrayList m_troopers = new ArrayList();

        // Анализ позиции
        private double GetPositionValue(MyTrooper self, ArrayList players, ArrayList troopers, ArrayList bonuses)
        {
            double value = 0.0;

            double enemyDeath = 75.0;
            double enemyHitpoints = 1.0;
            double teamHitpoints = 2.5;

            double isHoldingFieldRation = 25.0;
            double isHoldingGrenade = 25.0;
            double isHoldingMedikit = 25.0;

            double canShootEnemy = 0;
            double canSeeEnemy = 0;
            double canBeShootingByEnemy = 20.0;
            double canBeSeeningByEnemy = 30.0;

            double distance = 0.75;
            double distanceTeam = 0.2;
            double danger = 0.001;

            // Бонусы солдата
            value += self.IsHoldingFieldRation ? isHoldingFieldRation : 0;
            value += self.IsHoldingGrenade ? isHoldingGrenade : 0;
            value += self.IsHoldingMedikit ? isHoldingMedikit : 0;

            // Взаиморасположение с другими солдатами
            foreach (MyTrooper trooper in troopers)
            {
                if (trooper.IsTeammate == false)
                {
                    // Смерть врага
                    value += trooper.Hitpoints <= 0 ? enemyDeath : 0;

                    // Видимость и возможность выстрела во врага
                    value += m_world.IsVisible(self.ShootingRange, self.X, self.Y, self.Stance, trooper.X, trooper.Y, trooper.Stance) ? 
                        canShootEnemy : 0;
                    value += m_world.IsVisible(self.VisionRange, self.X, self.Y, self.Stance, trooper.X, trooper.Y, trooper.Stance) ? 
                        canSeeEnemy : 0;

                    // Здоровье врагов
                    value -= trooper.Hitpoints * enemyHitpoints;

                    // Видимость и возможность выстрела врагом в солдата
                    value -= m_world.IsVisible(trooper.ShootingRange, trooper.X, trooper.Y, trooper.Stance, self.X, self.Y, self.Stance) ?
                        canBeShootingByEnemy * (2 - self.Hitpoints / self.MaximalHitpoints) : 0;
                    value -= m_world.IsVisible(trooper.VisionRange, trooper.X, trooper.Y, trooper.Stance, self.X, self.Y, self.Stance) ?
                        canBeSeeningByEnemy * (2 - self.Hitpoints / self.MaximalHitpoints) : 0;
                }
                if (trooper.IsTeammate == true)
                {
                    // Здоровье своих
                    value += trooper.Hitpoints * teamHitpoints;

                    // Опасная клетка
                    value -= m_cellDangerous[self.X][self.Y] * danger;

                    // Удаленность от своих
                    value -= distanceTeam * self.GetDistanceTo(trooper.X, trooper.Y);
                    value -= distanceTeam * FindNextStep(self.X, self.Y, trooper.X, trooper.Y, 5).dist;
                }
            }

            // Удаленность солдата от цели
            value -= distance * self.GetDistanceTo(targetX, targetY);
            value -= distance * FindNextStep(self.X, self.Y, targetX, targetY, 5).dist;

            return value;
        }

        // Поиск оптимального действия солдата
        struct MoveVal
        {
            public Move move;
            public double val;
        }
        static int cnt_invoke = 0;
        private MoveVal FindNextMove(MyTrooper self, ArrayList players, ArrayList troopers, ArrayList bonuses)
        {
            MoveVal mv;
            mv.val = (self.ActionPoints <= MoveCost(self)) ? GetPositionValue(self, players, troopers, bonuses) : -1000;
            mv.move = new Move();
            mv.move.Action = ActionType.EndTurn;

            if (cnt_invoke++ > 16000) return mv;

            int[] nearX = { 0, 0, 0, 1, -1 };
            int[] nearY = { 0, 1, -1, 0, 0 };
            Direction[] nearDir = { Direction.CurrentPoint, Direction.South, Direction.North, Direction.East, Direction.West };

            // 1. Eat Field Ration
            if (self.IsHoldingFieldRation &&
                self.ActionPoints + m_game.FieldRationBonusActionPoints <= self.InitialActionPoints &&
                self.ActionPoints >= m_game.FieldRationEatCost)
            {
                self.ActionPoints -= m_game.FieldRationEatCost - m_game.FieldRationBonusActionPoints;
                self.IsHoldingFieldRation = false;

                MoveVal next = FindNextMove(self, players, troopers, bonuses);
                if (mv.val < next.val)
                {
                    mv.move.Action = ActionType.EatFieldRation;
                    mv.move.Direction = Direction.CurrentPoint;
                    mv.val = next.val;
                }

                self.IsHoldingFieldRation = true;
                self.ActionPoints += m_game.FieldRationEatCost - m_game.FieldRationBonusActionPoints;
            }

            // 2. Throw Grenade
            if (self.IsHoldingGrenade)
                foreach (MyTrooper enemy in troopers)
                {
                    if (enemy.IsTeammate == true) continue;
                    for (int i = 0; i < 5; i++)
                        if (enemy.X + nearX[i] < m_world.Width &&
                            enemy.Y + nearY[i] < m_world.Height &&
                            enemy.X + nearX[i] >= 0 &&
                            enemy.Y + nearY[i] >= 0 &&
                            self.GetDistanceTo(enemy.X + nearX[i], enemy.Y + nearY[i]) <= m_game.GrenadeThrowRange &&
                            self.ActionPoints >= m_game.GrenadeThrowCost)
                        {
                            self.ActionPoints -= m_game.FieldMedicHealCost;
                            self.IsHoldingGrenade = false;
                            foreach (MyTrooper et in troopers)
                            {
                                if (et.GetDistanceTo(enemy.X + nearX[i], enemy.Y + nearY[i]) == 1)
                                    et.Hitpoints -= m_game.GrenadeCollateralDamage;
                                if (et.GetDistanceTo(enemy.X + nearX[i], enemy.Y + nearY[i]) == 0)
                                    et.Hitpoints -= m_game.GrenadeDirectDamage;
                            }

                            MoveVal next = FindNextMove(self, players, troopers, bonuses);
                            if (mv.val < next.val)
                            {
                                mv.move.Action = ActionType.ThrowGrenade;
                                mv.move.Direction = null;
                                mv.move.X = enemy.X + nearX[i];
                                mv.move.Y = enemy.Y + nearY[i];
                                mv.val = next.val;
                            }

                            self.IsHoldingGrenade = true;
                            self.ActionPoints += m_game.FieldMedicHealCost;
                            foreach (MyTrooper et in troopers)
                            {
                                if (et.GetDistanceTo(enemy.X + nearX[i], enemy.Y + nearY[i]) == 1)
                                    et.Hitpoints += m_game.GrenadeCollateralDamage;
                                if (et.GetDistanceTo(enemy.X + nearX[i], enemy.Y + nearY[i]) == 0)
                                    et.Hitpoints += m_game.GrenadeDirectDamage;
                            }
                        }
                }

            // 3. Medikit
            if (self.IsHoldingMedikit)
                for (int i = 0; i < 5; i++)
                    if (self.X + nearX[i] < m_world.Width &&
                        self.Y + nearY[i] < m_world.Height &&
                        self.X + nearX[i] >= 0 &&
                        self.Y + nearY[i] >= 0 &&
                        self.ActionPoints >= m_game.MedikitUseCost)
                        foreach (MyTrooper mt in troopers)
                            if (mt.IsTeammate &&
                                mt.X == self.X + nearX[i] &&
                                mt.Y == self.Y + nearY[i] &&
                                mt.MaximalHitpoints - mt.Hitpoints >= (i == 0 ? m_game.MedikitHealSelfBonusHitpoints : m_game.MedikitBonusHitpoints))
                            {
                                self.ActionPoints -= m_game.MedikitUseCost;
                                mt.Hitpoints += (i == 0 ? m_game.MedikitHealSelfBonusHitpoints : m_game.MedikitBonusHitpoints);
                                self.IsHoldingMedikit = false;

                                MoveVal next = FindNextMove(self, players, troopers, bonuses);
                                if (mv.val < next.val)
                                {
                                    mv.move.Action = ActionType.UseMedikit;
                                    mv.move.Direction = nearDir[i];
                                    mv.val = next.val;
                                }

                                self.IsHoldingMedikit = true;
                                self.ActionPoints += m_game.MedikitUseCost;
                                mt.Hitpoints -= (i == 0 ? m_game.MedikitHealSelfBonusHitpoints : m_game.MedikitBonusHitpoints);
                            }

            // 4. Move
            int moveCost = MoveCost(self);
            for (int i = 1; i < 5; i++)
                if (self.X + nearX[i] < m_world.Width &&
                    self.Y + nearY[i] < m_world.Height &&
                    self.X + nearX[i] >= 0 &&
                    self.Y + nearY[i] >= 0 &&
                    self.ActionPoints >= moveCost &&
                    m_cells[self.X + nearX[i]][self.Y + nearY[i]] == CellType.Free)
                {
                    bool isTrooper = false;
                    foreach (MyTrooper trooper in m_troopers)
                        if (self.X + nearX[i] == trooper.X &&
                            self.Y + nearY[i] == trooper.Y) isTrooper = true;
                    if (isTrooper) continue;

                    self.ActionPoints -= moveCost;
                    self.X += nearX[i];
                    self.Y += nearY[i];

                    Bonus bonus = null;
                    foreach (Bonus b in bonuses)
                        if (b != null &&
                            b.X == self.X &&
                            b.Y == self.Y &&
                            (!self.IsHoldingFieldRation && b.Type == BonusType.FieldRation ||
                             !self.IsHoldingGrenade && b.Type == BonusType.Grenade ||
                             !self.IsHoldingMedikit && b.Type == BonusType.Medikit))
                            bonus = b;
                    if (bonus != null)
                    {
                        bonuses.Remove(bonus);
                        if (bonus.Type == BonusType.FieldRation) self.IsHoldingFieldRation = true;
                        if (bonus.Type == BonusType.Grenade) self.IsHoldingGrenade = true;
                        if (bonus.Type == BonusType.Medikit) self.IsHoldingMedikit = true;
                    }

                    MoveVal next = FindNextMove(self, players, troopers, bonuses);
                    if (mv.val < next.val)
                    {
                        mv.move.Action = ActionType.Move;
                        mv.move.Direction = nearDir[i];
                        mv.val = next.val;
                    }

                    if (bonus != null)
                    {
                        bonuses.Add(bonus);
                        if (bonus.Type == BonusType.FieldRation) self.IsHoldingFieldRation = false;
                        if (bonus.Type == BonusType.Grenade) self.IsHoldingGrenade = false;
                        if (bonus.Type == BonusType.Medikit) self.IsHoldingMedikit = false;
                    }
                    self.X -= nearX[i];
                    self.Y -= nearY[i];
                    self.ActionPoints += MoveCost(self);
                }

            // 5. Lower Stance
            if (self.Stance != TrooperStance.Prone &&
                self.ActionPoints >= m_game.StanceChangeCost)
            {
                self.ActionPoints -= m_game.StanceChangeCost;
                if (self.Stance == TrooperStance.Standing) self.Stance = TrooperStance.Kneeling;
                else
                    if (self.Stance == TrooperStance.Kneeling) self.Stance = TrooperStance.Prone;

                MoveVal next = FindNextMove(self, players, troopers, bonuses);
                if (mv.val < next.val)
                {
                    mv.move.Action = ActionType.LowerStance;
                    mv.move.Direction = Direction.CurrentPoint;
                    mv.val = next.val;
                }

                self.ActionPoints += m_game.StanceChangeCost;
                if (self.Stance == TrooperStance.Kneeling) self.Stance = TrooperStance.Standing;
                else
                    if (self.Stance == TrooperStance.Prone) self.Stance = TrooperStance.Kneeling;
            }

            // 6. Raise Stance
            if (self.Stance != TrooperStance.Standing &&
                self.ActionPoints >= m_game.StanceChangeCost)
            {
                self.ActionPoints -= m_game.StanceChangeCost;
                if (self.Stance == TrooperStance.Kneeling) self.Stance = TrooperStance.Standing;
                else
                    if (self.Stance == TrooperStance.Prone) self.Stance = TrooperStance.Kneeling;

                MoveVal next = FindNextMove(self, players, troopers, bonuses);
                if (mv.val < next.val)
                {
                    mv.move.Action = ActionType.RaiseStance;
                    mv.move.Direction = Direction.CurrentPoint;
                    mv.val = next.val;
                }

                self.ActionPoints += m_game.StanceChangeCost;
                if (self.Stance == TrooperStance.Standing) self.Stance = TrooperStance.Kneeling;
                else
                    if (self.Stance == TrooperStance.Kneeling) self.Stance = TrooperStance.Prone;
            }

            // 7. Heal
            for (int i = 0; i < 5; i++)
                if (self.X + nearX[i] < m_world.Width &&
                    self.Y + nearY[i] < m_world.Height &&
                    self.X + nearX[i] >= 0 &&
                    self.Y + nearY[i] >= 0 &&
                    self.Type == TrooperType.FieldMedic &&
                    self.ActionPoints >= m_game.FieldMedicHealCost)
                    foreach (MyTrooper mt in troopers)
                        if (mt.IsTeammate == true &&
                            mt.X == self.X + nearX[i] &&
                            mt.Y == self.Y + nearY[i] &&
                            mt.MaximalHitpoints - mt.Hitpoints >= (i == 0 ? m_game.FieldMedicHealSelfBonusHitpoints : m_game.FieldMedicHealBonusHitpoints))
                        {
                            self.ActionPoints -= m_game.FieldMedicHealCost;
                            mt.Hitpoints += (i == 0 ? m_game.FieldMedicHealSelfBonusHitpoints : m_game.FieldMedicHealBonusHitpoints);

                            MoveVal next = FindNextMove(self, players, troopers, bonuses);
                            if (mv.val < next.val)
                            {
                                mv.move.Action = ActionType.Heal;
                                mv.move.Direction = nearDir[i];
                                mv.val = next.val;
                            }

                            self.ActionPoints += m_game.FieldMedicHealCost;
                            mt.Hitpoints -= (i == 0 ? m_game.FieldMedicHealSelfBonusHitpoints : m_game.FieldMedicHealBonusHitpoints);
                        }

            // 8. Shoot
            foreach (MyTrooper enemy in troopers)
            {
                if (enemy.IsTeammate == true) continue;
                if (self.ActionPoints >= self.ShootCost &&
                    m_world.IsVisible(self.ShootingRange, self.X, self.Y, self.Stance, enemy.X, enemy.Y, enemy.Stance))
                {
                    self.ActionPoints -= self.ShootCost;
                    enemy.Hitpoints -= self.Damage;

                    MoveVal next = FindNextMove(self, players, troopers, bonuses);
                    if (mv.val < next.val)
                    {
                        mv.move.Action = ActionType.Shoot;
                        mv.move.Direction = null;
                        mv.move.X = enemy.X;
                        mv.move.Y = enemy.Y;
                        mv.val = next.val;
                    }

                    enemy.Hitpoints += self.Damage;
                    self.ActionPoints += self.ShootCost;
                }
            }

            return mv;
        }

        // Основной метод
        public void Move(Trooper self, World world, Game game, Move move)
        {
            if (self.ActionPoints == 0) return;
            cnt_invoke = 0;

            m_self = self;
            m_game = game;
            m_world = world;
            m_cells = world.Cells;

            // Определение игрока-цели
            Player nearToCentr = null;
            foreach (Player p in world.Players)
                if (p.Id != self.PlayerId &&
                    p.ApproximateX >= 0 &&
                    p.ApproximateY >= 0)
                    if (nearToCentr == null ||
                        FindNextStep(self.X, self.Y, nearToCentr.ApproximateX, nearToCentr.ApproximateY, 3).dist >
                        FindNextStep(self.X, self.Y, p.ApproximateX, p.ApproximateY, 3).dist
                    ) nearToCentr = p;
            if (nearToCentr != null)
            {
                targetX = nearToCentr.ApproximateX;
                targetY = nearToCentr.ApproximateY;
            }

            if (m_cellDangerous == null) CellDangerous();
            if (targetX == -1 && targetY == -1)
            {
                targetX = self.X;
                targetY = self.Y;
            }

            // Запрос цели
            const double stageDist = 2.5;
            if (self.GetDistanceTo(targetX, targetY) <= stageDist ||
                world.MoveIndex - moveWithoutEnemy >= 4)
            {
                bool CommanderAlive = false;
                foreach (Trooper tr in world.Troopers)
                    if (tr.IsTeammate && tr.Type == TrooperType.Commander)
                        CommanderAlive = true;
                if (!CommanderAlive)
                {
                    targetX = random.Next(world.Width);
                    targetY = random.Next(world.Height);
                    moveWithoutEnemy = world.MoveIndex;
                }
                else
                    if (self.Type == TrooperType.Commander &&
                        self.ActionPoints >= game.CommanderRequestEnemyDispositionCost)
                    {
                        move.Action = ActionType.RequestEnemyDisposition;
                        moveWithoutEnemy = world.MoveIndex;
                        return;
                    }
            }

            // Заполнение списка солдат, определение отсутствия врагов
            if (self.ActionPoints >= self.InitialActionPoints)
                m_troopers = new ArrayList();
            foreach (Trooper trooper in m_world.Troopers)
            {
                if (trooper.IsTeammate == false)
                    moveWithoutEnemy = m_world.MoveIndex;

                bool isExist = false;
                for (int i = 0; i < m_troopers.Count; i++)
                {
                    MyTrooper mt = (MyTrooper)m_troopers[i];
                    if (mt.Id == trooper.Id && mt.PlayerId == trooper.PlayerId)
                    {
                        isExist = true;
                        m_troopers[i] = new MyTrooper(trooper);
                        break;
                    }
                }
                if (!isExist) m_troopers.Add(new MyTrooper(trooper));
            }

            // Выбор оптимального действия
            Move m = FindNextMove(new MyTrooper(self), new ArrayList(m_world.Players), m_troopers, new ArrayList(m_world.Bonuses)).move;

            move.Action = m.Action;
            move.Direction = m.Direction;
            move.X = m.X;
            move.Y = m.Y;

            return;
        }
    }
}