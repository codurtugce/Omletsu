using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace DesktopCat
{
    public partial class MainWindow : Window
    {
        // =========================
        // TIMERLAR
        // =========================

        private readonly DispatcherTimer movementTimer;
        private readonly DispatcherTimer animationTimer;
        private readonly DispatcherTimer behaviorTimer;
        private readonly DispatcherTimer idleTimer;


        // =========================
        // GENEL AYARLAR
        // =========================

        private readonly Random random = new Random();

        private double speed = 2;

        private double targetX;
        private double targetY;

        private bool goingRight = true;

        private int walkFrame = 1;
        private int tailFrame = 1;
        private int lickFrame = 1;

        private PetState currentState = PetState.Walking;

        private bool petVisible = false;
        private bool automaticMode = true;


        // TEST İÇİN 10 SANİYE
        // Sonra 120 yaparsan 2 dakika olur.
        private double idleThresholdSeconds = 120;


        // =========================
        // WINDOWS IDLE ALGILAMA
        // =========================

        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(
            ref LASTINPUTINFO plii
        );


        // =========================
        // CONSTRUCTOR
        // =========================

        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;

            CatImage.RenderTransformOrigin =
                new System.Windows.Point(0.5, 0.5);


            // -------------------------
            // Hareket Timer
            // -------------------------

            movementTimer = new DispatcherTimer();

            movementTimer.Interval =
                TimeSpan.FromMilliseconds(20);

            movementTimer.Tick +=
                MovementTimer_Tick;


            // -------------------------
            // Animasyon Timer
            // -------------------------

            animationTimer = new DispatcherTimer();

            animationTimer.Interval =
                TimeSpan.FromMilliseconds(250);

            animationTimer.Tick +=
                AnimationTimer_Tick;


            // -------------------------
            // Davranış Timer
            // -------------------------

            behaviorTimer = new DispatcherTimer();

            behaviorTimer.Interval =
                TimeSpan.FromSeconds(4);

            behaviorTimer.Tick +=
                BehaviorTimer_Tick;


            // -------------------------
            // Bilgisayar boşta mı?
            // -------------------------

            idleTimer = new DispatcherTimer();

            idleTimer.Interval =
                TimeSpan.FromSeconds(1);

            idleTimer.Tick +=
                IdleTimer_Tick;


            // Bilgisayar uyku / uyanma takibi
            SystemEvents.PowerModeChanged +=
                SystemEvents_PowerModeChanged;
        }


        // =========================
        // PROGRAM AÇILDIĞINDA
        // =========================

        private void MainWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            Left = 100;

            Top =
                SystemParameters.WorkArea.Bottom
                - Height;


            CatImage.RenderTransform =
                new ScaleTransform(1, 1);


            ChooseNewTarget();

            ShowWalkFrame();


            movementTimer.Start();
            animationTimer.Start();
            behaviorTimer.Start();
            idleTimer.Start();


            // İlk açıldığında gizlensin.
            // Kullanıcı bilgisayarı bırakınca ortaya çıkacak.
            Hide();

            petVisible = false;
        }


        // =========================
        // IDLE KONTROLÜ
        // =========================

        public void ShowPetManually()
        {
            automaticMode = false;
            ShowPet();
        }

        public void HidePetManually()
        {
            automaticMode = false;
            HidePet();
        }

        public void EnableAutomaticMode()
        {
            automaticMode = true;

            // Otomatik moda geçince yeniden
            // idle kontrolüne bırakalım.
            HidePet();
        }
        public void SetIdleThreshold(double seconds)
        {
            idleThresholdSeconds = seconds;
        }
        private void IdleTimer_Tick(
            object? sender,
            EventArgs e)
        {

            if (!automaticMode)
                return;

            double idleSeconds =
                GetIdleSeconds();


            // Kullanıcı bilgisayarı bıraktı
            if (idleSeconds >= idleThresholdSeconds)
            {
                if (!petVisible)
                {
                    ShowPet();
                }
            }

            // Kullanıcı tekrar mouse / klavye kullandı
            else
            {
                if (petVisible)
                {
                    HidePet();
                }
            }
        }


        private double GetIdleSeconds()
        {
            LASTINPUTINFO info =
                new LASTINPUTINFO();

            info.cbSize =
                (uint)Marshal.SizeOf(info);


            if (!GetLastInputInfo(ref info))
                return 0;


            uint idleMilliseconds =
                unchecked(
                    (uint)Environment.TickCount
                    - info.dwTime
                );


            return idleMilliseconds / 1000.0;
        }


        // =========================
        // OMETSU'YU GÖSTER
        // =========================

        private void ShowPet()
        {
            petVisible = true;

            Show();

            Topmost = true;


            currentState =
                PetState.Walking;

            walkFrame = 1;


            ChooseNewTarget();

            ShowWalkFrame();
        }


        // =========================
        // OMETSU'YU GİZLE
        // =========================

        private void HidePet()
        {
            petVisible = false;

            Hide();
        }


        // =========================
        // HAREKET
        // =========================

        private void MovementTimer_Tick(
            object? sender,
            EventArgs e)
        {
            if (!petVisible)
                return;


            if (currentState != PetState.Walking)
                return;


            double diffX =
                targetX - Left;

            double diffY =
                targetY - Top;


            double distance =
                Math.Sqrt(
                    diffX * diffX +
                    diffY * diffY
                );


            if (distance < speed)
            {
                Left = targetX;
                Top = targetY;

                ChooseNewTarget();

                return;
            }


            Left +=
                (diffX / distance)
                * speed;

            Top +=
                (diffY / distance)
                * speed;


            // -------------------------
            // Kedinin baktığı yön
            // -------------------------

            if (diffX > 0 && !goingRight)
            {
                goingRight = true;

                CatImage.RenderTransform =
                    new ScaleTransform(1, 1);
            }

            else if (
                diffX < 0 &&
                goingRight
            )
            {
                goingRight = false;

                CatImage.RenderTransform =
                    new ScaleTransform(-1, 1);
            }
        }


        // =========================
        // ANİMASYON
        // =========================

        private void AnimationTimer_Tick(
            object? sender,
            EventArgs e)
        {
            if (!petVisible)
                return;


            // -------------------------
            // Yürüme
            // -------------------------

            if (
                currentState ==
                PetState.Walking
            )
            {
                walkFrame++;

                if (walkFrame > 4)
                    walkFrame = 1;


                ShowWalkFrame();
            }


            // -------------------------
            // Kuyruk sallama
            // -------------------------

            else if (
                currentState ==
                PetState.TailWagging
            )
            {
                tailFrame++;

                if (tailFrame > 2)
                    tailFrame = 1;


                ShowTailFrame();
            }


            // -------------------------
            // Yalanma
            // -------------------------

            else if (
                currentState ==
                PetState.Licking
            )
            {
                lickFrame++;

                if (lickFrame > 3)
                    lickFrame = 1;


                ShowLickFrame();
            }
        }


        // =========================
        // RASTGELE DAVRANIŞ
        // =========================

        private async void BehaviorTimer_Tick(
            object? sender,
            EventArgs e)
        {
            if (!petVisible)
                return;


            if (
                currentState !=
                PetState.Walking
            )
                return;


            // %40 ihtimalle duracak
            if (random.Next(100) >= 40)
                return;


            // -------------------------
            // Önce otursun
            // -------------------------

            currentState =
                PetState.Sitting;

            ShowSit();


            await Task.Delay(
                TimeSpan.FromSeconds(
                    random.Next(1, 3)
                )
            );


            if (!petVisible)
                return;


            // 0 = normal otur
            // 1 = kuyruk salla
            // 2 = yalan

            int behavior =
                random.Next(3);


            // =========================
            // NORMAL OTURMA
            // =========================

            if (behavior == 0)
            {
                currentState =
                    PetState.Sitting;

                ShowSit();


                await Task.Delay(
                    TimeSpan.FromSeconds(
                        random.Next(2, 5)
                    )
                );
            }


            // =========================
            // KUYRUK SALLAMA
            // =========================

            else if (behavior == 1)
            {
                currentState =
                    PetState.TailWagging;

                tailFrame = 1;

                ShowTailFrame();


                await Task.Delay(
                    TimeSpan.FromSeconds(
                        random.Next(2, 5)
                    )
                );
            }


            // =========================
            // YALANMA
            // =========================

            else
            {
                currentState =
                    PetState.Licking;

                lickFrame = 1;

                ShowLickFrame();


                await Task.Delay(
                    TimeSpan.FromSeconds(
                        random.Next(2, 5)
                    )
                );
            }


            if (!petVisible)
                return;


            // =========================
            // TEKRAR YÜRÜSÜN
            // =========================

            currentState =
                PetState.Walking;

            walkFrame = 1;


            ChooseNewTarget();

            ShowWalkFrame();
        }


        // =========================
        // RASTGELE HEDEF
        // =========================

        private void ChooseNewTarget()
        {
            double minX =
                SystemParameters.WorkArea.Left;

            double maxX =
                SystemParameters.WorkArea.Right
                - Width;


            double minY =
                SystemParameters.WorkArea.Top;

            double maxY =
                SystemParameters.WorkArea.Bottom
                - Height;


            targetX =
                minX +
                random.NextDouble()
                * (maxX - minX);


            targetY =
                minY +
                random.NextDouble()
                * (maxY - minY);
        }


        // =========================
        // GÖRSELLER
        // =========================
        private void ShowWalkFrame()
        {
            CatImage.Width = 440;
            CatImage.Height = 440;

            SetImage($"cat{walkFrame}.png");
        }

        private void ShowSit()
        {
            CatImage.Width = 360;
            CatImage.Height = 360;

            SetImage("sit.png");
        }

        private void ShowTailFrame()
        {
            CatImage.Width = 360;
            CatImage.Height = 360;

            SetImage($"tail{tailFrame}.png");
        }

        private void ShowLickFrame()
        {
            CatImage.Width = 360;
            CatImage.Height = 360;

            SetImage($"lick{lickFrame}.png");
        }
        private void SetImage(
            string fileName)
        {
            CatImage.Source =
                new BitmapImage(
                    new Uri(
                        $"/DesktopCat;component/{fileName}",
                        UriKind.Relative
                    )
                );
        }


        // =========================
        // UYKU MODUNDAN UYANMA
        // =========================

        private void SystemEvents_PowerModeChanged(
            object sender,
            PowerModeChangedEventArgs e)
        {
            if (
                e.Mode ==
                PowerModes.Resume
            )
            {
                Dispatcher.Invoke(() =>
                {
                    HidePet();

                    currentState =
                        PetState.Walking;

                    walkFrame = 1;
                });
            }
        }


        // =========================
        // PROGRAM KAPANIRKEN
        // =========================

        private void MainWindow_Closed(
            object? sender,
            EventArgs e)
        {
            SystemEvents.PowerModeChanged -=
                SystemEvents_PowerModeChanged;
        }
    }


    // =============================
    // OMETSU'NUN DURUMLARI
    // =============================

    public enum PetState
    {
        Walking,
        Sitting,
        TailWagging,
        Licking
    }
}