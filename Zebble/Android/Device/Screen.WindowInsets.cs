﻿using AndroidX.Core.View;
using System;
using System.Threading.Tasks;

namespace Zebble.Device
{
    partial class Screen
    {
        static Android.Views.View CurrentView;

        static WindowInsetsCompat CurrentInsets;
        static int _KeyboardHeight;
        internal static Action<int> OnKeyboardHeightChanged { get; set; }
        internal static int KeyboardHeight;

        class ApplyWindowInstetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
        {
            public WindowInsetsCompat OnApplyWindowInsets(Android.Views.View view, WindowInsetsCompat insets)
            {
                CurrentView = view;
                CurrentInsets = insets;

                var windowInsetsCompat = UpdateLayoutInsets();

                HeightProvider = OnHeightProvider;

                //On some devices not working so wee need to delay a frame to fix the problem
                Thread.UI.Post(() => Thread.Pool.Run(async () =>
                {
                    await Task.Delay(Animation.OneFrame);
                    OnKeyboardHeightChanged?.Invoke(KeyboardHeight);
                    UpdateLayout();
                }));

                //Fixed issue 145722 on Google_Pixel_3 and OnePlus_7T
                Thread.UI.Post(() => Thread.Pool.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(Animation.OneFrame.Milliseconds * 5));
                    UpdateLayout();
                }));

                return windowInsetsCompat;
            }

            float OnHeightProvider()
            {
                var navigationBar = CurrentInsets.GetInsets(WindowInsetsCompat.Type.NavigationBars()).Bottom;
                var totalBottom = KeyboardHeight == 0 ? navigationBar : KeyboardHeight;

                //In some cases the navigation bar height value is zero.
                //So, by doing this we can ensure that contain a correct value.
                if (totalBottom == 0)
                    totalBottom = DisplaySetting.InWindowNavbarHeight > 0 ? DisplaySetting.InWindowNavbarHeight : DisplaySetting.OutOfWindowNavbarHeight;

                DisplaySetting.InWindowNavbarHeight = navigationBar;

                if (KeyboardHeight > 0)
                {
                    Keyboard.SoftKeyboardHeight = Scale.ToZebble(KeyboardHeight);
                    Keyboard.RaiseShown();
                }
                else
                {
                    Keyboard.SoftKeyboardHeight = 0;
                    Keyboard.RaiseHidden();
                }

                return Scale.ToZebble(DisplaySetting.RealHeight - totalBottom) - StatusBar.Height;
            }

            WindowInsetsCompat UpdateLayoutInsets()
            {
                var statusBar = CurrentInsets.GetInsets(WindowInsetsCompat.Type.StatusBars()).Top;
                KeyboardHeight = CurrentInsets.GetInsets(WindowInsetsCompat.Type.Ime()).Bottom;

                DisplaySetting.InWindowStatusBarHeight = statusBar;
                DisplaySetting.Ime = KeyboardHeight;

                var windowInsetsCompat = new WindowInsetsCompat.Builder()
                    .SetInsets(WindowInsetsCompat.Type.SystemBars(), AndroidX.Core.Graphics.Insets.Of(0, statusBar, 0, KeyboardHeight))
                    .Build();

                Xamarin.Essentials.MainThread.BeginInvokeOnMainThread(() => ViewCompat.OnApplyWindowInsets(CurrentView, windowInsetsCompat));
                SafeAreaInsets.UpdateValues();

                return windowInsetsCompat;
            }
        }
    }
}
