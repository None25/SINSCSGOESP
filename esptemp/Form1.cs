using ezOverLay;
using swed32;
using winformtemplate;
using System.Threading;

namespace esptemp
{
    public partial class Form1 : Form
    {
        const int localplayer = 0xDB55DC;
        const int entitylist = 0x4DD0E6C;
        const int viewmatrix = 0x4DC2784;
        const int xyz = 0x138; //vector origin
        const int team = 0xF4;
        const int dormant = 0xED;
        const int health = 0x100;

        Pen teampen = new Pen(Color.Blue, 3);
        Pen enemypen = new Pen(Color.Red, 3);

        swed swed = new swed();
        ez ez = new ez();
        entity player = new entity();
        List<entity> list = new List<entity>();


        IntPtr client;


        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;
            swed.GetProcess("csgo");

            // how to get module base address

            client = swed.GetModuleBase("client.dll");

            ez.SetInvi(this);
            ez.DoStuff("Counter-Strike: Global Offensive - Direct3D 9", this);

            Thread thread = new Thread(main) { IsBackground = true };
            thread.Start();

        }


        void main()
        {
            while (true)
            {
                updatelocal();
                updateentities();
                panel1.Refresh();
                Thread.Sleep(13);
            }
        }

        void updatelocal()
        {
            var buffer = swed.ReadPointer(client, localplayer);

            player.team = BitConverter.ToInt32(swed.ReadBytes(buffer, team,4), 0);
        }


        void updateentities()
        {
            list.Clear();

            for(int i = 0; i <32; i++)
            {
                var buffer = swed.ReadPointer(client, entitylist + i * 0x10);

                var tm = BitConverter.ToInt32(swed.ReadBytes(buffer, team, 4), 0);
                var dorm = BitConverter.ToInt32(swed.ReadBytes(buffer, dormant, 4), 0);
                var hp = BitConverter.ToInt32(swed.ReadBytes(buffer, health, 4), 0);

                if (hp < 2 || dorm != 0)
                    continue;

                var coords = swed.ReadBytes(buffer, xyz, 12);


                var ent = new entity
                {
                    x = BitConverter.ToSingle(coords, 0),
                    y = BitConverter.ToSingle(coords, 4),
                    z = BitConverter.ToSingle(coords, 8),
                    team = tm,
                    health = hp

                };

                ent.bot = WorldToScreen(readmatrix(), ent.x, ent.y, ent.z, Width, Height);
                
                ent.top = WorldToScreen(readmatrix(), ent.x, ent.y, ent.z+58, Width, Height);




                list.Add(ent);



            }

        }

        viewmatrix readmatrix()
        {
            var matrix = new viewmatrix();

            var buffer = new byte[16 * 4];

            buffer = swed.ReadBytes(client, viewmatrix, buffer.Length);



            matrix.m11 = BitConverter.ToSingle(buffer, 0*4);
            matrix.m12 = BitConverter.ToSingle(buffer, 1 * 4);
            matrix.m13 = BitConverter.ToSingle(buffer, 2 * 4);
            matrix.m14 = BitConverter.ToSingle(buffer, 3 * 4);

            matrix.m21 = BitConverter.ToSingle(buffer, 4 * 4);
            matrix.m22 = BitConverter.ToSingle(buffer, 5 * 4);
            matrix.m23 = BitConverter.ToSingle(buffer, 6 * 4);
            matrix.m24 = BitConverter.ToSingle(buffer, 7 * 4);

            matrix.m31 = BitConverter.ToSingle(buffer, 8 * 4);
            matrix.m32 = BitConverter.ToSingle(buffer, 9 * 4);
            matrix.m33 = BitConverter.ToSingle(buffer, 10 * 4);
            matrix.m34 = BitConverter.ToSingle(buffer, 11 * 4);

            matrix.m41 = BitConverter.ToSingle(buffer, 12 * 4);
            matrix.m42 = BitConverter.ToSingle(buffer, 13 * 4);
            matrix.m43 = BitConverter.ToSingle(buffer, 14 * 4);
            matrix.m44 = BitConverter.ToSingle(buffer, 15 * 4);

            return matrix;

        }


        Point WorldToScreen(viewmatrix mtx, float x, float y, float z, int width, int height)
        {
            var twoD = new Point();

            float screenW = (mtx.m41 * x) + (mtx.m42 * y) + (mtx.m43 * z) + mtx.m44;

            if (screenW > 0.001f)
            {
                float screenX = (mtx.m11*x) + (mtx.m12 * y) + (mtx.m13 * z) + mtx.m14;

                float screenY = (mtx.m21 * x) + (mtx.m22 * y) + (mtx.m23 * z) + mtx.m24;

                float camX = width / 2f;
                float camY = height / 2f;

                float X = camX + (camX * screenX / screenW);

                float Y = camY - (camY * screenY / screenW);

                twoD.X = (int)X;
                twoD.Y = (int)Y;

                return twoD;

            }

            else
            {
                return new Point(-99, -99);
            }

        }


        private void panel1_Paint(object sender, PaintEventArgs e)
        {




            var g = e.Graphics;

            if (list.Count > 0)
            {
                try
                {
                    foreach( var ent in list)
                    {

                        if (ent.team == player.team && ent.bot.X > 0 && ent.bot.X < Width && ent.bot.Y > 0 && ent.bot.Y < Height)
                        {
                            g.DrawRectangle(teampen, ent.rect());
                            g.DrawLine(teampen, Width / 2, Height, ent.bot.X, ent.bot.Y);
                        }

                        else if (ent.team != player.team && ent.bot.X > 0 && ent.bot.X < Width && ent.bot.Y > 0 && ent.bot.Y < Height)
                        {
                            g.DrawRectangle(enemypen, ent.rect());
                        }



                    }
                }
                catch { }
            }
        }
    }
}