using Facepunch;
using Network;
using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

namespace BLUNT
{
    public static class Utils
    {
        public static void SetFieldValue(this object instance, string name, object value)
        {
            instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.SetField).SetValue(instance, value);
        }

        public static T GetFieldValue<T>(this object instance, string name)
        {
            return (T)instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.SetField).GetValue(instance);
        }
    }

    public class Render
    {
        private static GUIStyle LabelStyle = new GUIStyle()
        {
            font = Font.CreateDynamicFontFromOSFont("Consolas", 10),
            fontSize = 12,
            normal =
            {
                textColor = Color.white
            }
        };

        private static GUIStyle None = new GUIStyle();
        private static Color TextureColor;
        private static Texture2D Texture = new Texture2D(1, 1);

        public static void Rectangle(float x, float y, float w, float h, float rounding, Color color)
        {
            if(TextureColor != color)
            {
                Texture.SetPixel(0, 0, color);
                Texture.Apply();
                TextureColor = color;
            }

            GUI.DrawTexture(new Rect(x, y, w, h), Texture, ScaleMode.StretchToFill, false, 1f, color, 0, rounding);
        }

        public static void Rectangle(float x, float y, float w, float h, Color color, float thickness = 1)
        {
            Rectangle(x, y, w, thickness, 0, color);
            Rectangle(x, y, thickness, h, 0, color);
            Rectangle(x + w - thickness, y, thickness, h, 0, color);
            Rectangle(x, y + h - thickness, w, thickness, 0, color);
        }

        public static void String(float x, float y, float w, float h, string text, Color color, int fontSize = 12, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            LabelStyle.alignment = alignment;
            LabelStyle.fontSize = fontSize;
            LabelStyle.normal.textColor = color;

            GUI.Label(new Rect(x, y, w, h), text, LabelStyle);
        }

        public static void String(float x, float y, string text, Color color, bool center = false, int fontSize = 12)
        {
            LabelStyle.alignment = TextAnchor.UpperLeft;
            LabelStyle.fontSize = fontSize;

            Vector2 size = LabelStyle.CalcSize(new GUIContent(text));
            if (center) x -= size.x / 2;

            String(x, y, size.x, size.y, text, color, fontSize, TextAnchor.UpperLeft);
        }

        public static bool IsHover(float x, float y, float w, float h)
        {
            float mouse_x = Input.mousePosition.x;
            float mouse_y = Screen.height - Input.mousePosition.y;

            bool hover_x = (mouse_x >= x && mouse_x <= x + w);
            bool hover_y = (mouse_y >= y && mouse_y <= y + h);

            return (hover_x && hover_y);
        }

        private static Color WindowColor = new Color32(115, 115, 115, 255);
        private static Color WindowOverlayColor = new Color32(105, 105, 115, 205);
        public static Rect Window(Rect rectangle, MethodInfo callback, int id = 1)
        {
            return GUI.Window(id, rectangle, new GUI.WindowFunction((id_) =>
            {
                Render.Rectangle(0, 0, rectangle.width, rectangle.height, 4, WindowColor);
                callback.Invoke(callback.DeclaringType.GetField("Instance", BindingFlags.Public | BindingFlags.Static).GetValue(null), null);
                GUI.DragWindow();
            }), "", None);
        }

        private static Color CheckboxActiveColor = new Color32(3, 173, 252, 255);
        private static Color CheckboxColor = new Color32(145, 145, 145, 255);

        public static bool Checkbox(float x, float y, float h, string text, bool state)
        {
            Render.Rectangle(x, y, h, h, 4, CheckboxColor);
            if(state) Render.Rectangle(x + 3, y + 3, h - 6, h - 6, 4, CheckboxActiveColor);
            Render.String(x + h + 8, y, 150, h, text, Color.white, 12, TextAnchor.MiddleLeft);
            if (GUI.Button(new Rect(x, y, 150 + h + 8, h), "", None)) state = !state;
            return state;
        }

        private static bool Binding = false;

        public static KeyCode Bind(float x, float y, float h, string text, KeyCode now)
        {
            Render.Rectangle(x, y, h * 3, h, 4, CheckboxColor);
            Render.String(x, y, h * 3, h, Binding ? "..." : now.ToString(), Color.white, 12, TextAnchor.MiddleCenter);
            Render.String(x + (h * 3) + 8, y, 150, h, text, Color.white, 12, TextAnchor.MiddleLeft);
            if(Binding)
            {
                Event keyEvent = Event.current;
                if (keyEvent.isKey)
                {
                    if (keyEvent.keyCode != KeyCode.Escape) now = keyEvent.keyCode;
                    Binding = false;
                }
            }
            if (GUI.Button(new Rect(x, y, 150 + h + 8, h), "", None)) Binding = !Binding;
            return now;
        }

        private static Color TabActiveColor = new Color32(3, 173, 252, 255);
        private static Color TabColor = new Color32(145, 145, 145, 255);

        public static int TabSelector(float x, float y, float w, float h, string[] tabs, int current)
        {
            Render.Rectangle(x, y, w, h, 4, TabColor);

            float tab_width = w / tabs.Length;
            for (int i = 0; i < tabs.Length; i++)
            {
                if (current == i) Render.Rectangle(x + (i * tab_width), y, tab_width, h, 4, TabActiveColor);
                Render.String(x + (i * tab_width), y, tab_width, h, tabs[i], Color.white);
                if (GUI.Button(new Rect(x + (i * tab_width), y, tab_width, h), "", None)) current = i;
            }
            return current;
        }

        private static Color SliderColor = new Color32(145, 145, 145, 255);
        private static Color SliderButtonColor = new Color32(3, 173, 252, 255);

        public static int Counter(float x, float y, float w, float h, string text, int current, int min, int max)
        {
            Render.Rectangle(x, y, w, h, 4, SliderColor);
            Render.Rectangle(x, y, h, h, 4, SliderButtonColor);
            Render.String(x, y, h, h, "-", Color.white, 10, TextAnchor.MiddleCenter);
            if (GUI.Button(new Rect(x, y, h, h), "", None) && current > min) current--;
            Render.Rectangle(x + w - h, y, h, h, 4, SliderButtonColor);
            Render.String(x + w - h, y, h, h, "+", Color.white, 10, TextAnchor.MiddleCenter);
            if (GUI.Button(new Rect(x + w - h, y, h, h), "", None) && current < max) current++;
            Render.String(x + h, y, w - (h * 2), h, $"{text}: {current}", Color.gray, 12, TextAnchor.MiddleCenter);
            return current;
        }

        public static Color ColorPicker(float x, float y, float w, float h, Color color)
        {
            Render.Rectangle(x, y, h, h, 4, color);
            Render.Rectangle(x + h + 8, y, w / 3, h, 4, SliderColor);
            Render.Rectangle(x + h + 10 + (w / 3), y, w / 3, h, 4, SliderColor);
            Render.Rectangle(x + h + 12 + ((w / 3) * 2), y, w / 3, h, 4, SliderColor);

            Render.String(x + h + 8, y, w / 3, h, $"{Math.Round(color.r, 2)}", Color.white);
            color.r = GUI.HorizontalSlider(new Rect(x + h + 8, y, w / 3, h), color.r, 0, 1, None, None);

            Render.String(x + h + 10 + (w / 3), y, w / 3, h, $"{Math.Round(color.g, 2)}", Color.white);
            color.g = GUI.HorizontalSlider(new Rect(x + h + 10 + (w / 3), y, w / 3, h), color.g, 0, 1, None, None);

            Render.String(x + h + 12 + ((w / 3) * 2), y, w / 3, h, $"{Math.Round(color.b, 2)}", Color.white);
            color.b = GUI.HorizontalSlider(new Rect(x + h + 12 + ((w / 3) * 2), y, w / 3, h), color.b, 0, 1, None, None);

            return color;
        }
    }

    public class Config
    {
        public static bool ESP_Players = false;
        public static Color ESP_Players_Color = Color.yellow;
        public static bool ESP_Players_Name = false;
        public static bool ESP_Players_Health = false;
        public static bool ESP_Players_Distance = false;
        public static bool ESP_Players_Weapon = false;
        public static bool ESP_Players_Box = false;
        public static bool ESP_Players_Sleeping = false;
        public static bool Crosshair = false;
        public static int Crosshair_Style = 0;
        public static string[] Crosshair_Style_Tabs = new string[]
        {
            "Dot",
            "Circle",
            "Circle + Dot"
        };
        public static int Crosshair_Size = 4;
        public static Color Crosshair_Color = Color.white;

        public static bool Aimbot = false;
        public static KeyCode Aimbot_Key = KeyCode.X;
        public static bool Aimbot_Silent = false;
        public static int Aimbot_Silent_Target = 1;
        public static int Aimbot_Silent_Style = 0;
        public static string[] Aimbot_Silent_Tabs = new string[]
        {
            "pSilent",
            "Silent",
            "Silent TP"
        };
        public static bool NoRecoil = false;
        public static bool Climbhack = false;
        public static bool AlwaysAttack = false;
        public static bool FastReload = false;
        public static bool FastHeal = false;
        public static bool Uberhatchet = false;
    }

    public class Menu : MonoBehaviour
    {
        public static Menu Instance;
        private MethodInfo OnRenderMenu;
        void Start()
        {
            Instance = this;
            OnRenderMenu = this.GetType().GetMethod("OnMenu", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private Color TitleBarColor = new Color32(3, 173, 252, 255);
        private Color RowColor = new Color32(125, 125, 125, 255);

        void OnMenu()
        {
            /* TITLE BAR */

            Render.Rectangle(0, 0, WindowRectangle.width, 32, 4, TitleBarColor);
            Render.String(0, 0, WindowRectangle.width, 32, "BLUNT SOFTWARE", Color.white);

            /* ROWS */

            Render.Rectangle(16, 48, WindowRectangle.width / 2 - 24, WindowRectangle.height - 64, 4, RowColor);
            Render.Rectangle(WindowRectangle.width / 2 + 8, 48, WindowRectangle.width / 2 - 24, WindowRectangle.height - 64, 4, RowColor);
            Render.String(28, 58, "Visuals >", Color.gray, false, 14);
            Render.String(WindowRectangle.width / 2 + 20, 58, "Other >", Color.gray, false, 14);

            /* ROW 1 */

            Config.ESP_Players = Render.Checkbox(28, 84, 16, "Players", Config.ESP_Players);
            Config.ESP_Players_Color = Render.ColorPicker(28, 104, WindowRectangle.width / 2 - 76, 16, Config.ESP_Players_Color);
            Config.ESP_Players_Name = Render.Checkbox(28, 124, 16, "Players Name", Config.ESP_Players_Name);
            Config.ESP_Players_Health = Render.Checkbox(28, 144, 16, "Players Health", Config.ESP_Players_Health);
            Config.ESP_Players_Distance = Render.Checkbox(28, 164, 16, "Players Distance", Config.ESP_Players_Distance);
            Config.ESP_Players_Weapon = Render.Checkbox(28, 184, 16, "Players Weapon", Config.ESP_Players_Weapon);
            Config.ESP_Players_Box = Render.Checkbox(28, 204, 16, "Players Box", Config.ESP_Players_Box);
            Config.ESP_Players_Sleeping = Render.Checkbox(28, 224, 16, "Players Sleeping", Config.ESP_Players_Sleeping);
            Config.Crosshair = Render.Checkbox(28, 244, 16, "Crosshair", Config.Crosshair);
            Config.Crosshair_Style = Render.TabSelector(28, 264, WindowRectangle.width / 2 - 48, 16, Config.Crosshair_Style_Tabs, Config.Crosshair_Style);
            Config.Crosshair_Size = Render.Counter(28, 284, WindowRectangle.width / 2 - 48, 16, "Crosshair Size", Config.Crosshair_Size, 1, 12);
            Config.Crosshair_Color = Render.ColorPicker(28, 304, WindowRectangle.width / 2 - 76, 16, Config.Crosshair_Color);

            /* ROW 2 */

            Config.Aimbot = Render.Checkbox(WindowRectangle.width / 2 + 20, 84, 16, "Aimbot", Config.Aimbot);
            Config.Aimbot_Key = Render.Bind(WindowRectangle.width / 2 + 20, 104, 16, "Aimbot Key", Config.Aimbot_Key);
            Config.Aimbot_Silent = Render.Checkbox(WindowRectangle.width / 2 + 20, 124, 16, "Aimbot Silent", Config.Aimbot_Silent);
            Config.Aimbot_Silent_Style = Render.TabSelector(WindowRectangle.width / 2 + 20, 144, WindowRectangle.width / 2 - 48, 16, Config.Aimbot_Silent_Tabs, Config.Aimbot_Silent_Style);
            Config.NoRecoil = Render.Checkbox(WindowRectangle.width / 2 + 20, 164, 16, "No Guns Recoil", Config.NoRecoil);
            Config.Climbhack = Render.Checkbox(WindowRectangle.width / 2 + 20, 184, 16, "Sticky Walls", Config.Climbhack);
            Config.AlwaysAttack = Render.Checkbox(WindowRectangle.width / 2 + 20, 204, 16, "Always Attack", Config.AlwaysAttack);
            Config.FastReload = Render.Checkbox(WindowRectangle.width / 2 + 20, 224, 16, "Fast Guns Reloading", Config.FastReload);
            Config.FastHeal = Render.Checkbox(WindowRectangle.width / 2 + 20, 244, 16, "Fast Healing", Config.FastHeal);
            Config.Uberhatchet = Render.Checkbox(WindowRectangle.width / 2 + 20, 264, 16, "Uber Hatchet", Config.Uberhatchet);
        }

        Color LogotypeColor = new Color32(3, 173, 252, 255);
        Rect WindowRectangle = new Rect(450, 250, 600, 350);
        bool visible = false;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Insert)) visible = !visible;
        }

        void OnGUI()
        {
            string logotype_text = "BLUNT SOFTWARE by vk.com/azp2033";
            Render.String(15, 15, logotype_text, LogotypeColor, false, 14);

            if (Config.Crosshair)
            {
                Color CrosshairColor = Config.Crosshair_Color;
                Color CrosshairCircleColor = new Color(CrosshairColor.r, CrosshairColor.g, CrosshairColor.b, .25f);
                switch (Config.Crosshair_Style)
                {
                    case 0:
                        Render.Rectangle(Screen.width / 2 - (Config.Crosshair_Size / 2), Screen.height / 2 - (Config.Crosshair_Size / 2), Config.Crosshair_Size, Config.Crosshair_Size, Config.Crosshair_Size / 2, CrosshairColor);
                        break;
                    case 1:
                        Render.Rectangle(Screen.width / 2 - ((Config.Crosshair_Size * 6) / 2), Screen.height / 2 - ((Config.Crosshair_Size * 6) / 2), Config.Crosshair_Size * 6, Config.Crosshair_Size * 6, (Config.Crosshair_Size * 6) / 2, CrosshairCircleColor);
                        break;
                    case 2:
                        Render.Rectangle(Screen.width / 2 - ((Config.Crosshair_Size * 6) / 2), Screen.height / 2 - ((Config.Crosshair_Size * 6) / 2), Config.Crosshair_Size * 6, Config.Crosshair_Size * 6, (Config.Crosshair_Size * 6) / 2, CrosshairCircleColor);
                        Render.Rectangle(Screen.width / 2 - 2, Screen.height / 2 - 2, 4, 4, Config.Crosshair_Size / 2, CrosshairColor);
                        break;
                }
            }
            if (visible) WindowRectangle = Render.Window(WindowRectangle, OnRenderMenu);
        }
    }

    public class Main : MonoBehaviour
    {
        private HookManager hookManager;
        void Start()
        {
            try
            {
                this.gameObject.AddComponent<Menu>();
            } catch { File.AppendAllText("error.log", System.Environment.NewLine + "[Error] while add menu component"); }
            try
            {
                StartCoroutine(SilentCycle());
            } catch { File.AppendAllText("error.log", System.Environment.NewLine + "[Error] while start silent coroutine component"); }
            try
            {
                StartCoroutine(HealingCycle());
            }
            catch { File.AppendAllText("error.log", System.Environment.NewLine + "[Error] while start heal coroutine component"); }
            try
            {
                StartCoroutine(TargetCycle());
            }
            catch { File.AppendAllText("error.log", System.Environment.NewLine + "[Error] while start heal coroutine component"); }
            try
            {
                hookManager = new HookManager();
            }
            catch { File.AppendAllText("error.log", System.Environment.NewLine + "[Error] while create instance of hook manager component"); }
            try
            {
                hookManager.Hook(typeof(BaseProjectile).GetMethod("CreateProjectile", BindingFlags.Instance | BindingFlags.NonPublic), typeof(Main).GetMethod("CreateProjectile"));
            }
            catch { File.AppendAllText("error.log", System.Environment.NewLine + "[Error] while hook createprojectile component"); }
            try
            {
                hookManager.Hook(typeof(BasePlayer).GetMethod("SendProjectileAttack", BindingFlags.Instance | BindingFlags.Public), typeof(Main).GetMethod("SendProjectileAttack"));
            }
            catch { File.AppendAllText("error.log", System.Environment.NewLine + "[Error] while hook sendprojectileattack component"); }
            try
            {
                hookManager.Hook(typeof(BasePlayer).GetMethod("GetSpeed", BindingFlags.Instance | BindingFlags.Public), typeof(Main).GetMethod("GetSpeed"));
            }
            catch { File.AppendAllText("error.log", System.Environment.NewLine + "[Error] while hook getspeed component"); }
            try
            {
                hookManager.Hook(typeof(BasePlayer).GetMethod("CanAttack", BindingFlags.Instance | BindingFlags.Public), typeof(Main).GetMethod("CanAttack"));

            }
            catch { File.AppendAllText("error.log", System.Environment.NewLine + "[Error] while hook canattack component"); }
            try
            {
                hookManager.Hook(typeof(Client).GetMethod("GetOSName", BindingFlags.Instance | BindingFlags.NonPublic), typeof(Main).GetMethod("GetOSName"));
            }
            catch { File.AppendAllText("error.log", System.Environment.NewLine + "[Error] while hook getosname component"); }
            try
            {
                hookManager.Hook(typeof(PlayerWalkMovement).GetMethod("CanSprint", BindingFlags.Instance | BindingFlags.NonPublic), typeof(Main).GetMethod("CanSprint"));
            }
            catch { File.AppendAllText("error.log", System.Environment.NewLine + "[Error] while hook cansprint component"); }
            try
            {
                hookManager.Hook(typeof(BasePlayer).GetMethod("SendClientTick", BindingFlags.Instance | BindingFlags.NonPublic), typeof(Main).GetMethod("SendClientTick"));
            }
            catch { File.AppendAllText("error.log", System.Environment.NewLine + "[Error] while hook sendclienttick component"); }
        }

        public static void SendClientTick(BasePlayer instance)
        {
            if(Time.realtimeSinceStartup - instance.GetFieldValue<float>("lastSentTickTime") > 0.5)
            {
                instance.SetFieldValue("lastSentTickTime", Time.realtimeSinceStartup);
            }
            using (PlayerTick playerTick = Pool.Get<PlayerTick>())
            {
                Item activeItem = instance.Belt.GetActiveItem();
                playerTick.activeItem = ((activeItem == null) ? 0U : activeItem.uid);
                playerTick.inputState = instance.input.state.current;
                playerTick.position = instance.transform.position;
                playerTick.eyePos = new Vector3(Random.Range(0, 360), 0, Random.Range(0, 360));
                if (playerTick.modelState == null)
                {
                    playerTick.modelState = Pool.Get<ModelState>();
                    playerTick.modelState.flying = false; 
                    playerTick.modelState.onground = true;
                }
                if (instance.modelState != null)
                {
                    instance.modelState.CopyTo(playerTick.modelState);
                    instance.modelState.flying = false;
                }
                if (Net.cl.write.Start())
                {
                    Net.cl.write.PacketID(Message.Type.Tick);
                    playerTick.WriteToStreamDelta(Net.cl.write, instance.GetFieldValue<PlayerTick>("lastSentTick"));
                    Net.cl.write.Send(new SendInfo(Net.cl.Connection)
                    {
                        priority = Priority.Immediate
                    });
                }
                if (Net.cl.IsRecording)
                {
                    byte[] array = playerTick.ToProtoBytes();
                    Net.cl.ManualRecordPacket(15, array, array.Length);
                }
                if (instance.GetFieldValue<PlayerTick>("lastSentTick") == null)
                {
                    instance.SetFieldValue("lastSentTick", Pool.Get<PlayerTick>());
                }
                playerTick.CopyTo(instance.GetFieldValue<PlayerTick>("lastSentTick"));
            }
        }


        public static bool CanSprint(PlayerWalkMovement instance)
        {
            return true;
        }

        public static string GetOSName(Client instance)
        {
            return "ГЕЙ ЕБАНЫЙ";
        }

        public static List<Projectile> CurrentProjectiles = new List<Projectile>();

        IEnumerator TargetCycle()
        {
            while (true)
            {
                try
                {
                    if (Config.Aimbot_Silent && LocalPlayer.Entity != null)
                    {
                        UpdateTarget();
                    }
                }
                catch { }
                yield return new WaitForSeconds(0.5f);
            }
        }

        IEnumerator SilentCycle()
        {
            while (true)
            {
                try
                {
                    if (Config.Aimbot_Silent && Config.Aimbot_Silent_Style == 2 && LocalPlayer.Entity != null)
                    {
                        if (silent_target != null)
                        {
                            for (int x = 0; x < CurrentProjectiles.Count; x++)
                            {
                                if(CurrentProjectiles[x] != null)
                                {
                                    float distance_a = Vector3.Distance(LocalPlayer.Entity.transform.position, silent_target.model.headBone.transform.position);
                                    float distance_b = Vector3.Distance(CurrentProjectiles[x].transform.position, LocalPlayer.Entity.transform.position);
                                    if (distance_b >= distance_a)
                                    {
                                        CurrentProjectiles[x].transform.position = silent_target.model.headBone.transform.position;
                                        CurrentProjectiles.RemoveAt(x);
                                    }
                                } else
                                {
                                    CurrentProjectiles.RemoveAt(x);
                                }
                            }
                        }
                    }
                }
                catch { }
                yield return new WaitForSeconds(0.5f);
            }
        }

        public static Projectile CreateProjectile(BaseProjectile instance, string prefabPath, Vector3 pos, Vector3 forward, Vector3 velocity)
        {
            GameObject gameObject = instance.gameManager.CreatePrefab(prefabPath, pos, Quaternion.LookRotation(forward), true);
            if (gameObject == null)
            {
                return null;
            }
            Projectile component = gameObject.GetComponent<Projectile>();
            component.InitializeVelocity(velocity);
            component.modifier = instance.GetProjectileModifier();
            try
            {
                if (Config.NoRecoil && instance.recoil)
                {
                    instance.recoil.timeToTakeMax = 0;
                    instance.recoil.timeToTakeMin = 0;
                    instance.recoil.recoilPitchMax = 0;
                    instance.recoil.recoilPitchMin = 0;
                    instance.recoil.recoilYawMax = 0;
                    instance.recoil.recoilYawMin = 0;
                    instance.recoil.ADSScale = 0;
                    instance.automatic = true;
                    instance.aimSway = 0;
                    instance.aimSwaySpeed = 0;
                    instance.aimCone = 0;
                }
                if(Config.Aimbot_Silent && Config.Aimbot_Silent_Style == 0)
                {
                    component.thickness = float.MaxValue;
                }
                if (Config.Aimbot_Silent && Config.Aimbot_Silent_Style == 2 && !CurrentProjectiles.Contains(component))
                    CurrentProjectiles.Add(component);
            }
            catch { }
            return component;
        }

        public static bool CanAttack(BasePlayer instance)
        {
            return true;
        }

        public static float GetSpeed(BasePlayer instance, float running, float ducking)
        {
            return 5.5f;
            //return Mathf.Lerp(Mathf.Lerp(2.8f, 5.5f, running), 1.7f, ducking);
        }

        public static void SendProjectileAttack(BasePlayer instance, PlayerProjectileAttack attack)
        {
            if(Config.Aimbot_Silent && Config.Aimbot_Silent_Style == 1)
            {
                if (silent_target != null)
                {
                    attack.playerAttack.attack.hitID = silent_target.net.ID;
                    attack.playerAttack.attack.hitBone = 698017942u;
                    attack.playerAttack.attack.hitPartID = 2173623152u;
                    attack.playerAttack.attack.hitPositionLocal = new Vector3(0.9f, -0.4f, 0.1f);
                    attack.playerAttack.attack.hitNormalLocal = new Vector3(0.9f, -0.4f, 0.1f);
                    LocalPlayer.Entity.ServerRPC<PlayerProjectileAttack>("OnProjectileAttack", attack);
                }
                return;
            }
            instance.ServerRPC<PlayerProjectileAttack>("OnProjectileAttack", attack);
        }

        

        IEnumerator HealingCycle()
        {
            float lastHealTick = Time.realtimeSinceStartup;
            while (true)
            {
                try
                {
                    if (Config.FastHeal && LocalPlayer.Entity != null)
                    {
                        HeldEntity heldEntity = LocalPlayer.Entity.GetHeldEntity();
                        if(heldEntity != null && (heldEntity is MedicalTool))
                        {
                            if (Time.realtimeSinceStartup - lastHealTick > 1)
                            {
                                heldEntity.ServerRPC("UseSelf");
                                lastHealTick = Time.realtimeSinceStartup;
                            }
                        }
                    }
                }
                catch { }
                yield return new WaitForSeconds(0.5f);
            }
        }

        static BasePlayer silent_target = null;

        static void UpdateTarget()
        {
            silent_target = null;
            BasePlayer target = null;
            float targetDist = 0;
            foreach (BasePlayer basePlayer in global::BasePlayer.VisiblePlayerList)
            {
                Vector3 vector = MainCamera.mainCamera.WorldToScreenPoint(basePlayer.model.headBone.transform.position);
                float num2 = Mathf.Abs(Vector2.Distance(new Vector2((float)(Screen.width / 2), (float)(Screen.height / 2)), new Vector2(vector.x, (float)Screen.height - vector.y)));
                if (basePlayer != null && !basePlayer.IsLocalPlayer() && num2 <= 20 && !basePlayer.IsDead())
                {
                    if (target != null)
                    {
                        if (targetDist > num2)
                        {
                            targetDist = num2;
                            target = basePlayer;
                        }
                    }
                    else
                    {
                        targetDist = num2;
                        target = basePlayer;
                    }
                }
            }
            if (target != null) silent_target = target;
        }

        void Update()
        {
            if(LocalPlayer.Entity != null)
            {
                try
                {
                    if (Config.Climbhack)
                    {
                        FieldInfo fieldInfo_ = typeof(PlayerWalkMovement).GetField("groundAngleNew", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);
                        fieldInfo_.SetValue(LocalPlayer.Entity.movement, 0);
                    }
                    if(Input.GetKeyDown(KeyCode.F))
                    {
                        UpdateTarget();
                    }

                }
                catch { }
            }
        }

        void OnGUI()
        {
            if(Config.ESP_Players)
            {
                if (LocalPlayer.Entity == null) return;
                foreach (BasePlayer player in BasePlayer.VisiblePlayerList)
                {
                    if (player == null || player.health < 1 || player.IsLocalPlayer()) continue;

                    Vector3 ScreenPosition = MainCamera.mainCamera.WorldToScreenPoint(player.transform.position);
                    if (ScreenPosition.z < 0) continue;

                    Vector3 HeadScreenPosition = MainCamera.mainCamera.WorldToScreenPoint(player.model.headBone.transform.position + new Vector3(0f, 0.3f, 0f));
                    float PlayerHeight = Mathf.Abs(ScreenPosition.y - HeadScreenPosition.y);

                    if(!player.IsSleeping())
                    {
                        List<string> text = new List<string>();
                        if (Config.ESP_Players_Name) text.Add(player.displayName);
                        if (Config.ESP_Players_Health) text.Add($"[{(int)player.health} HP]");
                        if (Config.ESP_Players_Distance) text.Add($"[{(int)Vector3.Distance(LocalPlayer.Entity.transform.position, player.transform.position)} M]");
                        if (Config.ESP_Players_Weapon)
                        {
                            Item ActiveItem = player.Belt.GetActiveItem();
                            if(ActiveItem != null) text.Add($"[{ActiveItem.info.displayName.english}]");
                        }
                        Render.String(ScreenPosition.x, (float)Screen.height - ScreenPosition.y, string.Join(" ", text.ToArray()), (silent_target == player) ? Color.red : Config.ESP_Players_Color, true, 12);
                        if (Config.ESP_Players_Box) Render.Rectangle(ScreenPosition.x - PlayerHeight / 4, (float)Screen.height - ScreenPosition.y - PlayerHeight, PlayerHeight / 2, PlayerHeight - 6, (silent_target == player) ? Color.red : Config.ESP_Players_Color, 1);
                    }
                }
            }
        }
    }
}


