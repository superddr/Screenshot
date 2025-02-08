using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley.Menus;

namespace Screenshot
{
    public class ModConfig
    {
        public bool AutoScreenshotOnWarp { get; set; } = true;
        public string ScreenshotHotkey { get; set; } = "F1";
        public string ToggleAutoScreenshotHotkey { get; set; } = "F2";
        public string[] AvailableKeys { get; set; } = Enum.GetValues(typeof(SButton)).Cast<SButton>().Select(b => b.ToString()).ToArray();
        
    }

    internal sealed class ModEntry : Mod
    {
        private ModConfig Config;
        
        public override void Entry(IModHelper helper)
        {
            // 加载配置
            Config = helper.ReadConfig<ModConfig>();

            helper.Events.Input.ButtonReleased += this.OnButtonReleased;
            helper.Events.Player.Warped += this.OnWarped;
        }

        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            
            // 检查配置是否启用了自动截图
            if (Config.AutoScreenshotOnWarp && e.Player.IsLocalPlayer)
            {
                working = true;
                Game1.activeClickableMenu = new OverlayMenu("Auto screenshot, Press " + Config.ToggleAutoScreenshotHotkey + " to disable");
            }
        }

        private bool working = false;
        private void OnButtonReleased(object? sender, ButtonReleasedEventArgs e)
        {
            if (!Context.IsWorldReady)return;
        
            if (e.Button.ToString() == Config.ScreenshotHotkey || e.Button == SButton.Escape)
            {
                if(e.Button != SButton.Escape || working){
                    working = !working;
                }else{ // Esc and not working
                    return;
                }

                if(working)
                {
                    Game1.activeClickableMenu = new OverlayMenu("Screenshot. Press " + Config.ScreenshotHotkey + " to close");
                }else{
                    Game1.exitActiveMenu(); // 关闭菜单
                }
            }else if(e.Button.ToString() == Config.ToggleAutoScreenshotHotkey){
                Config.AutoScreenshotOnWarp = !Config.AutoScreenshotOnWarp;
                // 在屏幕上显示提示
                Game1.addHUDMessage(new HUDMessage("Auto screenshot on warp: " + (Config.AutoScreenshotOnWarp ? "Enabled" : "Disabled"), 2));
            }

        }
    }




public class OverlayMenu : IClickableMenu
{
    private Texture2D? texture = null;
    private string title;
    public OverlayMenu(string title): base(0, 0, Game1.viewport.Width, Game1.viewport.Height) // 覆盖整个屏幕
    {
        this.title = title;
        Game1.isTimePaused = true;
        Game1.game1.takeMapScreenshot(1, "screenshot",null);
    }


    private Vector2 offset;
    private float scalemin,scale,scroll;
    private Vector2? oldMousePos;

    // 绘制遮罩层和内容
    public override void draw(SpriteBatch b)
    {
        var mouseState = Game1.input.GetMouseState();
        if (texture == null)
        {
            var path = Game1.game1.GetScreenshotFolder() + "/screenshot.png"; 
            try{
                texture = Texture2D.FromStream(Game1.graphics.GraphicsDevice, System.IO.File.OpenRead(path));
            }catch{
                return;
            }

            // 计算缩放比例
            Vector2 viewport = new( Game1.game1.GraphicsDevice.Viewport.Width, Game1.game1.GraphicsDevice.Viewport.Height);
            Vector2 mapsize = new ( texture.Width   , texture.Height );

            scale = Math.Min(viewport.X / mapsize.X, viewport.Y / mapsize.Y); //缩放比例
            scalemin = scale / 2; //最小可以缩小适合尺寸的一半

            // 计算绘制位置，使纹理居中
            offset = (viewport - mapsize * scale) / 2;
            scroll = mouseState.ScrollWheelValue;
        }
        
        var pos = mouseState.Position.ToVector2();

        //滚轮缩放
        var newscroll = mouseState.ScrollWheelValue;
        if(scroll!=newscroll){
            float newscale = Math.Max(scalemin, Math.Min(2, scale + (newscroll-scroll) / 2000f));

            // 根据鼠标位置缩放
            offset = (offset - pos) * newscale / scale + pos;
            scale = newscale;
            scroll = newscroll;
        }

        //鼠标左键拖动
        if(mouseState.LeftButton == ButtonState.Pressed){
            if(oldMousePos == null){oldMousePos = pos;}

            offset += pos - oldMousePos.Value;

            oldMousePos = pos;
        }else{
            oldMousePos = null;
        }

        //先把Game1.spriteBatch全部涂黑
        Game1.spriteBatch.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height), Color.Black);
        // 绘制缩放后的纹理
        Game1.spriteBatch.Draw(texture, offset, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        // 绘制标题
        Game1.spriteBatch.DrawString(Game1.dialogueFont, title, new Vector2(10, 10), Color.White);
        // 绘制鼠标
        base.drawMouse(b);
    }
    
    ~OverlayMenu(){
        texture?.Dispose();
        Game1.isTimePaused = false;
    }
}
}