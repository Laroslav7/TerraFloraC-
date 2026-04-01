using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;

namespace TerraFlora;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;

    private Texture2D _backgroundTexture = null!;
    private Texture2D _logoTexture = null!;
    private Texture2D _grassTexture = null!;
    private Texture2D _dirtTexture = null!;
    private Texture2D _woodTexture = null!;
    private Texture2D _playerTexture = null!;
    private Texture2D _pickaxeTexture = null!;
    private Texture2D _swordTexture = null!;
    private Texture2D _hotbarTexture = null!;
    private Texture2D _pixelTexture = null!;

    private readonly GameManager _gameManager = new();
    private readonly Camera2D _camera = new();
    private readonly World _world = new(220, 70, 32);
    private readonly Inventory _inventory = new(4);
    private readonly Player _player = new();
    private InputHandler _inputHandler = null!;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
    }

    protected override void Initialize()
    {
        _world.GenerateSimpleTerrain();
        _inputHandler = new InputHandler();

        _player.Initialize(new Vector2(400f, 300f));

        _inventory.SetSlot(0, new ItemStack(ItemType.Pickaxe, 1));
        _inventory.SetSlot(1, new ItemStack(ItemType.Sword, 1));

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        _backgroundTexture = LoadTexture("FieldBackground.png");
        _logoTexture = LoadTexture("TerraFlora.png");
        _grassTexture = LoadTexture("pieceofgrass.png");
        _dirtTexture = LoadTexture("GrassFloar.png");
        _woodTexture = LoadTexture("TreeOak.png");
        _playerTexture = LoadTexture("character0.png");
        _pickaxeTexture = LoadTexture("TerraPickax.png");
        _swordTexture = LoadTexture("TerraSword.png");
        _hotbarTexture = LoadTexture("FirstHotBar.png");
    }

    protected override void Update(GameTime gameTime)
    {
        _inputHandler.Update();

        if (_inputHandler.IsExitPressed())
        {
            Exit();
            return;
        }

        switch (_gameManager.State)
        {
            case GameState.MainMenu:
                UpdateMainMenu();
                break;
            case GameState.Playing:
                UpdateGameplay(gameTime);
                break;
        }

        base.Update(gameTime);
    }

    private void UpdateMainMenu()
    {
        if (_inputHandler.WasStartPressed())
        {
            _gameManager.State = GameState.Playing;
        }
    }

    private void UpdateGameplay(GameTime gameTime)
    {
        _inventory.SelectSlot(_inputHandler.GetHotbarSelectionDelta(), _inputHandler.GetDirectSlotSelection());

        _player.Update(gameTime, _inputHandler, _world);

        if (_inputHandler.WasBreakTilePressed())
        {
            Point tileCoords = _camera.ScreenToWorldTile(_inputHandler.MousePosition, _world.TileSize);
            if (_player.CanReachTile(tileCoords, _world.TileSize) && _world.TryBreakTile(tileCoords.X, tileCoords.Y, out var droppedItem))
            {
                _inventory.TryAddItem(droppedItem);
            }
        }

        var playerCenter = _player.GetFeetAnchor();
        _camera.Update(playerCenter, GraphicsDevice.Viewport);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        if (_gameManager.State == GameState.MainMenu)
        {
            DrawMainMenu();
        }
        else
        {
            DrawGameplay();
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawMainMenu()
    {
        _spriteBatch.Draw(_backgroundTexture, GraphicsDevice.Viewport.Bounds, Color.White);

        var logoScale = 0.45f;
        var logoPos = new Vector2(
            GraphicsDevice.Viewport.Width * 0.5f - (_logoTexture.Width * logoScale) * 0.5f,
            60f);
        _spriteBatch.Draw(_logoTexture, logoPos, null, Color.White, 0f, Vector2.Zero, logoScale, SpriteEffects.None, 0f);

        Rectangle playButton = new(GraphicsDevice.Viewport.Width / 2 - 140, 420, 280, 80);
        _spriteBatch.Draw(_pixelTexture, playButton, new Color(70, 120, 80, 220));
        _spriteBatch.Draw(_pixelTexture, new Rectangle(playButton.X - 3, playButton.Y - 3, playButton.Width + 6, 3), Color.White);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(playButton.X - 3, playButton.Bottom, playButton.Width + 6, 3), Color.White);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(playButton.X - 3, playButton.Y, 3, playButton.Height), Color.White);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(playButton.Right, playButton.Y, 3, playButton.Height), Color.White);
    }

    private void DrawGameplay()
    {
        _spriteBatch.Draw(_backgroundTexture, GraphicsDevice.Viewport.Bounds, Color.White);

        _world.Draw(_spriteBatch, _camera, _grassTexture, _dirtTexture, _woodTexture);
        _player.Draw(_spriteBatch, _camera, _playerTexture, GetItemTexture(_inventory.GetSelectedItem().ItemType));

        DrawHotbar();
    }

    private void DrawHotbar()
    {
        int slotSize = 64;
        int spacing = 8;
        int totalWidth = (slotSize * 4) + (spacing * 3);
        int x = GraphicsDevice.Viewport.Width / 2 - totalWidth / 2;
        int y = GraphicsDevice.Viewport.Height - 100;

        Rectangle barBounds = new(x - 20, y - 12, totalWidth + 40, slotSize + 24);
        _spriteBatch.Draw(_hotbarTexture, barBounds, Color.White);

        for (int i = 0; i < 4; i++)
        {
            Rectangle slotRect = new(x + i * (slotSize + spacing), y, slotSize, slotSize);
            bool selected = i == _inventory.SelectedSlot;

            _spriteBatch.Draw(_pixelTexture, slotRect, selected ? new Color(240, 220, 120, 120) : new Color(20, 20, 20, 100));

            var stack = _inventory.GetSlot(i);
            var tex = GetItemTexture(stack.ItemType);
            if (tex != null)
            {
                Rectangle itemRect = FitRect(tex.Width, tex.Height, slotRect, 0.72f);
                _spriteBatch.Draw(tex, itemRect, Color.White);

                if (stack.Count > 1)
                {
                    int barHeight = Math.Clamp(stack.Count, 2, slotSize - 4);
                    Rectangle qtyBar = new(slotRect.Right - 7, slotRect.Bottom - 2 - barHeight, 4, barHeight);
                    _spriteBatch.Draw(_pixelTexture, qtyBar, new Color(80, 220, 120));
                }
            }

            Rectangle numberRect = new(slotRect.X + 2, slotRect.Y + 2, 10, 3);
            _spriteBatch.Draw(_pixelTexture, numberRect, Color.White * 0.75f);
        }
    }

    private Texture2D? GetItemTexture(ItemType type) => type switch
    {
        ItemType.Pickaxe => _pickaxeTexture,
        ItemType.Sword => _swordTexture,
        ItemType.Dirt => _dirtTexture,
        ItemType.Wood => _woodTexture,
        _ => null
    };

    private Texture2D LoadTexture(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Content", fileName);
        using var stream = TitleContainer.OpenStream(path);
        return Texture2D.FromStream(GraphicsDevice, stream);
    }

    private static Rectangle FitRect(int sourceWidth, int sourceHeight, Rectangle target, float fill)
    {
        float scale = MathF.Min(target.Width / (float)sourceWidth, target.Height / (float)sourceHeight) * fill;
        int w = (int)(sourceWidth * scale);
        int h = (int)(sourceHeight * scale);
        return new Rectangle(target.Center.X - w / 2, target.Center.Y - h / 2, w, h);
    }
}

public enum GameState { MainMenu, Playing }

public class GameManager
{
    public GameState State { get; set; } = GameState.MainMenu;
}

public enum TileType { Air, Grass, Dirt, Wood }
public enum ItemType { None, Dirt, Wood, Pickaxe, Sword }

public readonly record struct ItemStack(ItemType ItemType, int Count);

public class Inventory
{
    private readonly ItemStack[] _slots;
    public int SelectedSlot { get; private set; }

    public Inventory(int size)
    {
        _slots = new ItemStack[size];
    }

    public void SetSlot(int index, ItemStack stack) => _slots[index] = stack;
    public ItemStack GetSlot(int index) => _slots[index];
    public ItemStack GetSelectedItem() => _slots[SelectedSlot];

    public void SelectSlot(int scrollDelta, int? direct)
    {
        if (direct.HasValue)
        {
            SelectedSlot = Math.Clamp(direct.Value, 0, _slots.Length - 1);
            return;
        }

        if (scrollDelta != 0)
        {
            SelectedSlot = (SelectedSlot + _slots.Length + scrollDelta) % _slots.Length;
        }
    }

    public void TryAddItem(ItemType type)
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i].ItemType == type)
            {
                _slots[i] = _slots[i] with { Count = _slots[i].Count + 1 };
                return;
            }
        }

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i].ItemType == ItemType.None)
            {
                _slots[i] = new ItemStack(type, 1);
                return;
            }
        }
    }
}

public class World
{
    private readonly TileType[,] _tiles;
    public int Width { get; }
    public int Height { get; }
    public int TileSize { get; }

    public World(int width, int height, int tileSize)
    {
        Width = width;
        Height = height;
        TileSize = tileSize;
        _tiles = new TileType[width, height];
    }

    public void GenerateSimpleTerrain()
    {
        int surface = Height / 2 + 6;

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                _tiles[x, y] = y < surface ? TileType.Air : (y == surface ? TileType.Grass : TileType.Dirt);
            }
        }

        for (int tx = 12; tx < Width; tx += 33)
        {
            int baseY = surface - 1;
            for (int i = 0; i < 3; i++)
            {
                if (baseY - i >= 0)
                    _tiles[tx, baseY - i] = TileType.Wood;
            }
        }
    }

    public bool IsSolid(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return true;

        return _tiles[x, y] is TileType.Grass or TileType.Dirt or TileType.Wood;
    }

    public bool TryBreakTile(int x, int y, out ItemType dropped)
    {
        dropped = ItemType.None;
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return false;

        var tile = _tiles[x, y];
        if (tile == TileType.Air)
            return false;

        dropped = tile == TileType.Wood ? ItemType.Wood : ItemType.Dirt;
        _tiles[x, y] = TileType.Air;
        return true;
    }

    public void Draw(SpriteBatch spriteBatch, Camera2D camera, Texture2D grass, Texture2D dirt, Texture2D wood)
    {
        var viewWorld = camera.GetWorldBounds();
        int minX = Math.Max(0, viewWorld.Left / TileSize - 1);
        int maxX = Math.Min(Width - 1, viewWorld.Right / TileSize + 1);
        int minY = Math.Max(0, viewWorld.Top / TileSize - 1);
        int maxY = Math.Min(Height - 1, viewWorld.Bottom / TileSize + 1);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                var tile = _tiles[x, y];
                if (tile == TileType.Air)
                    continue;

                Texture2D tex = tile switch
                {
                    TileType.Grass => grass,
                    TileType.Wood => wood,
                    _ => dirt
                };

                var worldRect = new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize);
                var screenRect = camera.WorldToScreen(worldRect);
                spriteBatch.Draw(tex, screenRect, Color.White);
            }
        }
    }
}

public class Player
{
    private Vector2 _position;
    private Vector2 _velocity;
    private const float Speed = 210f;
    private const float JumpVelocity = -430f;
    private const float Gravity = 1150f;
    private const float Scale = 1.3f;

    private Rectangle _collisionBox;

    public void Initialize(Vector2 startPos)
    {
        _position = startPos;
        _velocity = Vector2.Zero;
        _collisionBox = new Rectangle((int)_position.X, (int)_position.Y, 44, 78);
    }

    public void Update(GameTime gameTime, InputHandler input, World world)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        int move = input.GetHorizontalDirection();

        _velocity.X = move * Speed;
        _velocity.Y += Gravity * dt;

        if (input.WasJumpPressed() && IsGrounded(world))
        {
            _velocity.Y = JumpVelocity;
        }

        MoveHorizontal(world, _velocity.X * dt);
        MoveVertical(world, _velocity.Y * dt);
    }

    public bool CanReachTile(Point tileCoords, int tileSize)
    {
        Vector2 center = new(_collisionBox.Center.X, _collisionBox.Center.Y);
        Vector2 tileCenter = new((tileCoords.X + 0.5f) * tileSize, (tileCoords.Y + 0.5f) * tileSize);
        return Vector2.Distance(center, tileCenter) < tileSize * 3.2f;
    }

    public Vector2 GetFeetAnchor() => new(_collisionBox.Center.X, _collisionBox.Bottom - 12);

    public void Draw(SpriteBatch spriteBatch, Camera2D camera, Texture2D playerTexture, Texture2D? heldItem)
    {
        var drawRect = new Rectangle(
            _collisionBox.Center.X - (int)(playerTexture.Width * Scale * 0.5f),
            _collisionBox.Bottom - (int)(playerTexture.Height * Scale),
            (int)(playerTexture.Width * Scale),
            (int)(playerTexture.Height * Scale));

        spriteBatch.Draw(playerTexture, camera.WorldToScreen(drawRect), Color.White);

        if (heldItem != null)
        {
            Rectangle handRect = new(_collisionBox.Right - 2, _collisionBox.Top + 30, 30, 30);
            spriteBatch.Draw(heldItem, camera.WorldToScreen(handRect), Color.White);
        }
    }

    private bool IsGrounded(World world)
    {
        Rectangle feetProbe = new(_collisionBox.X + 4, _collisionBox.Bottom + 1, _collisionBox.Width - 8, 2);
        return IntersectsSolid(world, feetProbe);
    }

    private void MoveHorizontal(World world, float amount)
    {
        if (amount == 0f)
            return;

        _position.X += amount;
        UpdateCollisionBox();

        if (!IntersectsSolid(world, _collisionBox))
            return;

        int step = Math.Sign(amount);
        while (IntersectsSolid(world, _collisionBox))
        {
            _position.X -= step;
            UpdateCollisionBox();
        }

        _velocity.X = 0f;
    }

    private void MoveVertical(World world, float amount)
    {
        if (amount == 0f)
            return;

        _position.Y += amount;
        UpdateCollisionBox();

        if (!IntersectsSolid(world, _collisionBox))
            return;

        int step = Math.Sign(amount);
        while (IntersectsSolid(world, _collisionBox))
        {
            _position.Y -= step;
            UpdateCollisionBox();
        }

        _velocity.Y = 0f;
    }

    private bool IntersectsSolid(World world, Rectangle rect)
    {
        int tileSize = world.TileSize;
        int minX = rect.Left / tileSize;
        int maxX = (rect.Right - 1) / tileSize;
        int minY = rect.Top / tileSize;
        int maxY = (rect.Bottom - 1) / tileSize;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (world.IsSolid(x, y))
                    return true;
            }
        }

        return false;
    }

    private void UpdateCollisionBox()
    {
        _collisionBox.X = (int)_position.X;
        _collisionBox.Y = (int)_position.Y;
    }
}

public class Camera2D
{
    private Vector2 _position;

    public void Update(Vector2 target, Viewport viewport)
    {
        Vector2 desired = new(target.X - viewport.Width / 2f, target.Y - viewport.Height / 2f);
        _position = Vector2.Lerp(_position, desired, 0.16f);
    }

    public Rectangle WorldToScreen(Rectangle worldRect)
    {
        return new Rectangle(
            (int)MathF.Round(worldRect.X - _position.X),
            (int)MathF.Round(worldRect.Y - _position.Y),
            worldRect.Width,
            worldRect.Height);
    }

    public Point ScreenToWorldTile(Point screen, int tileSize)
    {
        int wx = (int)(screen.X + _position.X);
        int wy = (int)(screen.Y + _position.Y);
        return new Point(wx / tileSize, wy / tileSize);
    }

    public Rectangle GetWorldBounds() => new((int)_position.X, (int)_position.Y, 1280, 720);
}

public class InputHandler
{
    private KeyboardState _currentKeys;
    private KeyboardState _previousKeys;
    private MouseState _currentMouse;
    private MouseState _previousMouse;

    public Point MousePosition => _currentMouse.Position;

    public void Update()
    {
        _previousKeys = _currentKeys;
        _previousMouse = _currentMouse;
        _currentKeys = Keyboard.GetState();
        _currentMouse = Mouse.GetState();
    }

    public bool IsExitPressed() => _currentKeys.IsKeyDown(Keys.Escape) || GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed;
    public bool WasStartPressed() => IsNewKeyPress(Keys.Enter) || IsNewKeyPress(Keys.Space);
    public bool WasJumpPressed() => IsNewKeyPress(Keys.Space);
    public bool WasBreakTilePressed() => _currentMouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;

    public int GetHorizontalDirection()
    {
        int dir = 0;
        if (_currentKeys.IsKeyDown(Keys.A)) dir--;
        if (_currentKeys.IsKeyDown(Keys.D)) dir++;
        return dir;
    }

    public int GetHotbarSelectionDelta()
    {
        int delta = _currentMouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;
        if (delta == 0) return 0;
        return delta > 0 ? -1 : 1;
    }

    public int? GetDirectSlotSelection()
    {
        if (IsNewKeyPress(Keys.D1)) return 0;
        if (IsNewKeyPress(Keys.D2)) return 1;
        if (IsNewKeyPress(Keys.D3)) return 2;
        if (IsNewKeyPress(Keys.D4)) return 3;
        return null;
    }

    private bool IsNewKeyPress(Keys key) => _currentKeys.IsKeyDown(key) && !_previousKeys.IsKeyDown(key);
}
