using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;


using StardewValley.Menus;

namespace Screenshot
{
    internal sealed class ModEntry : Mod
    {
        public override void Entry(IModHelper helper)
        {
            helper.Events.Input.ButtonReleased += this.OnButtonReleased;
        }

        private bool working = false;
        private void OnButtonReleased(object? sender, ButtonReleasedEventArgs e)
        {
            if (!Context.IsWorldReady)return;

            if (e.Button == SButton.F1 || e.Button == SButton.Escape)
            {
                if(e.Button != SButton.Escape || working){
                    working = !working;
                }else{
                    return;
                }

                if(working)
                {
                    Game1.activeClickableMenu = new OverlayMenu();
                }else{
                    Game1.exitActiveMenu(); // 关闭菜单
                }
            }

        }
    }




public class OverlayMenu : IClickableMenu
{
    private Texture2D? texture = null;

    public OverlayMenu(): base(0, 0, Game1.viewport.Width, Game1.viewport.Height) // 覆盖整个屏幕
    {
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
        // 绘制鼠标
        base.drawMouse(b);
    }

    ~OverlayMenu(){
        texture?.Dispose();
        Game1.isTimePaused = false;
    }
}
}