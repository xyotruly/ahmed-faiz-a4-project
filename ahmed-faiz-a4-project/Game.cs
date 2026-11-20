using System;
using System.Numerics;

namespace MohawkGame2D
{
    // Collectible key.
    public class DungeonKey
    {
        public Vector2 Position { get; private set; }
        public bool Collected { get; private set; }

        public DungeonKey(float x, float y)
        {
            Position = new Vector2(x, y);
            Collected = false;
        }

        public void Collect()
        {
            Collected = true;
        }

        public void DrawKey()
        {
            if (Collected)
                return;

            Draw.FillColor = Color.Yellow;

            // Key head and handle.
            Draw.Circle(Position.X, Position.Y - 5, 7);
            Draw.Rectangle(Position.X - 2, Position.Y, 4, 15);
            Draw.Rectangle(Position.X + 1, Position.Y + 9, 7, 4);
        }
    }

    // Moving enemy guard.
    public class DungeonGuard
    {
        public Vector2 Position { get; private set; }

        private Vector2 direction;
        private float speed;
        private float leftLimit;
        private float rightLimit;

        public DungeonGuard(float x, float y,
                            float moveSpeed, float range)
        {
            Position = new Vector2(x, y);
            direction = new Vector2(1, 0);
            speed = moveSpeed;

            leftLimit = x - range;
            rightLimit = x + range;
        }

        public void Move()
        {
            // Vector2 direction, normalization, and speed.
            Vector2 movement =
                Vector2.Normalize(direction) * speed;

            Position += movement;

            if (Position.X <= leftLimit)
            {
                Position = new Vector2(leftLimit, Position.Y);
                direction.X = 1;
            }

            if (Position.X >= rightLimit)
            {
                Position = new Vector2(rightLimit, Position.Y);
                direction.X = -1;
            }
        }

        public void DrawGuard()
        {
            // Orange body.
            Draw.FillColor = Color.Orange;
            Draw.Rectangle(Position.X - 10,
                           Position.Y - 4, 20, 20);

            // Yellow head.
            Draw.FillColor = Color.Yellow;
            Draw.Circle(Position.X, Position.Y - 11, 9);

            // Black helmet and feet.
            Draw.FillColor = Color.Black;
            Draw.Rectangle(Position.X - 10,
                           Position.Y - 19, 20, 5);
            Draw.Rectangle(Position.X - 9,
                           Position.Y + 16, 7, 5);
            Draw.Rectangle(Position.X + 2,
                           Position.Y + 16, 7, 5);
        }
    }

    // Solid maze wall.
    public class DungeonWall
    {
        private Vector2 position;
        private float width;
        private float height;

        public DungeonWall(float x, float y,
                           float wallWidth, float wallHeight)
        {
            position = new Vector2(x, y);
            width = wallWidth;
            height = wallHeight;
        }

        public void DrawWall()
        {
            Draw.FillColor = Color.Gray;
            Draw.Rectangle(position.X, position.Y, width, height);

            Draw.FillColor = Color.Black;
            Draw.Rectangle(position.X, position.Y, width, 3);
        }

        // Circle and rectangle collision detection.
        public bool Touches(Vector2 point, float radius)
        {
            float closestX = Math.Clamp(
                point.X, position.X, position.X + width);

            float closestY = Math.Clamp(
                point.Y, position.Y, position.Y + height);

            float dx = point.X - closestX;
            float dy = point.Y - closestY;

            return dx * dx + dy * dy < radius * radius;
        }
    }

    // Player controlled with arrow keys.
    public class DungeonPlayer
    {
        public Vector2 Position { get; private set; }

        // Adjust player movement speed for smoother maze navigation.
        private float speed = 3.5f;
        private const float Radius = 12f;

        public DungeonPlayer(float x, float y)
        {
            Position = new Vector2(x, y);
        }

        public void ResetPosition()
        {
            Position = new Vector2(60, 540);
        }

        public void Move(DungeonWall[] walls)
        {
            Vector2 direction = Vector2.Zero;

            if (Input.IsKeyboardKeyDown(KeyboardKey.Left))
                direction.X -= 1;

            if (Input.IsKeyboardKeyDown(KeyboardKey.Right))
                direction.X += 1;

            if (Input.IsKeyboardKeyDown(KeyboardKey.Up))
                direction.Y -= 1;

            if (Input.IsKeyboardKeyDown(KeyboardKey.Down))
                direction.Y += 1;

            // Keep movement speed consistent diagonally.
            if (direction.Length() > 0)
                direction = Vector2.Normalize(direction);

            Vector2 movement = direction * speed;

            // Horizontal wall collision.
            Vector2 nextX = Position +
                new Vector2(movement.X, 0);

            if (!TouchesWall(nextX, walls))
                Position = nextX;

            // Vertical wall collision.
            Vector2 nextY = Position +
                new Vector2(0, movement.Y);

            if (!TouchesWall(nextY, walls))
                Position = nextY;
        }

        private bool TouchesWall(Vector2 point,
                                 DungeonWall[] walls)
        {
            foreach (DungeonWall wall in walls)
            {
                if (wall.Touches(point, Radius))
                    return true;
            }

            return false;
        }

        public void DrawPlayer()
        {
            // Yellow Pac-Man-style player.
            Draw.FillColor = Color.Yellow;
            Draw.Circle(Position.X, Position.Y, 12);

            // Mouth.
            Draw.FillColor = Color.Black;
            Draw.Rectangle(Position.X + 3,
                           Position.Y - 4, 12, 8);

            // Eye.
            Draw.Circle(Position.X - 2,
                        Position.Y - 5, 2);
        }
    }

    public class Game
    {
        private DungeonPlayer player;

        // Two arrays of custom class objects.
        private DungeonKey[] keys = new DungeonKey[4];
        private DungeonGuard[] guards = new DungeonGuard[4];

        private DungeonWall[] walls = new DungeonWall[10];

        private Vector2 exitPosition = new Vector2(730, 55);

        private int keysCollected;
        private int hits;
        private int hitCooldown;

        private bool won;
        private bool lost;
        private bool restartWasDown;

        public void Setup()
        {
            Window.SetSize(800, 600);
            ResetGame();
        }

        private void ResetGame()
        {
            player = new DungeonPlayer(60, 540);

            keysCollected = 0;
            hits = 0;
            hitCooldown = 0;

            won = false;
            lost = false;

            // Four collectible keys.
            keys[0] = new DungeonKey(180, 510);
            keys[1] = new DungeonKey(400, 390);
            keys[2] = new DungeonKey(620, 270);
            keys[3] = new DungeonKey(650, 390);

            // Give each guard a different patrol speed.
            guards[0] = new DungeonGuard(300, 510, 2, 65);
            guards[1] = new DungeonGuard(480, 390, 3, 60);
            guards[2] = new DungeonGuard(320, 270, 2.5f, 55);
            guards[3] = new DungeonGuard(560, 90, 3, 50);

            // Outer walls.
            walls[0] = new DungeonWall(0, 0, 800, 15);
            walls[1] = new DungeonWall(0, 585, 800, 15);
            walls[2] = new DungeonWall(0, 0, 15, 600);
            walls[3] = new DungeonWall(785, 0, 15, 600);

            // Maze walls.
            walls[4] = new DungeonWall(0, 450, 600, 15);
            walls[5] = new DungeonWall(200, 330, 600, 15);
            walls[6] = new DungeonWall(0, 210, 600, 15);
            walls[7] = new DungeonWall(200, 105, 600, 15);
            walls[8] = new DungeonWall(380, 465, 15, 65);
            walls[9] = new DungeonWall(450, 225, 15, 65);
        }

        public void Update()
        {
            bool restartDown =
                Input.IsKeyboardKeyDown(KeyboardKey.R);

            if (restartDown && !restartWasDown)
                ResetGame();

            restartWasDown = restartDown;

            if (!won && !lost)
            {
                player.Move(walls);

                foreach (DungeonGuard guard in guards)
                    guard.Move();

                CheckKeys();
                CheckGuards();
                CheckExit();

                if (hitCooldown > 0)
                    hitCooldown--;
            }

            DrawDungeon();
        }

        private void CheckKeys()
        {
            foreach (DungeonKey key in keys)
            {
                float distance = Vector2.Distance(
                    player.Position, key.Position);

                // Allow the player to collect nearby keys.
                if (!key.Collected && distance < 26)
                {
                    key.Collect();
                    keysCollected++;
                }
            }
        }

        private void CheckGuards()
        {
            if (hitCooldown > 0)
                return;

            foreach (DungeonGuard guard in guards)
            {
                float distance = Vector2.Distance(
                    player.Position, guard.Position);

                if (distance < 26)
                {
                    hits++;
                    hitCooldown = 60;
                    player.ResetPosition();

                    if (hits >= 3)
                        lost = true;

                    break;
                }
            }
        }

        private void CheckExit()
        {
            float distance = Vector2.Distance(
                player.Position, exitPosition);

            if (keysCollected == 4 && distance < 30)
                won = true;
        }

        private void DrawDungeon()
        {
            // Black floor.
            Draw.FillColor = Color.Black;
            Draw.Rectangle(0, 0, 800, 600);

            // Gray maze walls.
            foreach (DungeonWall wall in walls)
                wall.DrawWall();

            // Exit door.
            if (keysCollected == 4)
                Draw.FillColor = Color.Green;
            else
                Draw.FillColor = Color.Red;

            Draw.Rectangle(700, 25, 60, 60);

            // Door handle.
            Draw.FillColor = Color.Yellow;
            Draw.Circle(748, 55, 5);

            // Keys and guards.
            foreach (DungeonKey key in keys)
                key.DrawKey();

            foreach (DungeonGuard guard in guards)
                guard.DrawGuard();

            player.DrawPlayer();

            // Collected key indicators.
            for (int i = 0; i < keysCollected; i++)
            {
                Draw.FillColor = Color.Yellow;
                Draw.Circle(25 + i * 25, 25, 7);
            }

            // Damage indicators.
            for (int i = 0; i < hits; i++)
            {
                Draw.FillColor = Color.Red;
                Draw.Circle(25 + i * 25, 50, 7);
            }

            // Victory.
            if (won)
            {
                Draw.FillColor = Color.Green;
                Draw.Rectangle(250, 240, 300, 120);

                Draw.FillColor = Color.Yellow;
                Draw.Circle(400, 300, 35);
            }

            // Game over.
            if (lost)
            {
                Draw.FillColor = Color.Red;
                Draw.Rectangle(250, 240, 300, 120);

                Draw.FillColor = Color.Black;
                Draw.Circle(400, 300, 35);
            }
        }
    }
}
