namespace Zebble.IOS
{
    using Microsoft.Maui.ApplicationModel;
    using System.Threading.Tasks;
    using UIKit;

    public class PrimaryWindow : UIWindow
    {
        readonly UIViewController RootScreen;

        public PrimaryWindow(UIViewController rootScreen) : base(UIScreen.MainScreen.Bounds)
        {
            RootScreen = rootScreen;

            Platform.Init(() => rootScreen);

            Device.Screen.DarkMode = rootScreen.TraitCollection.UserInterfaceStyle == UIUserInterfaceStyle.Dark;
        }

        public async Task Configure()
        {
            await Setup.Start(RootScreen, this);

            MakeKeyAndVisible();

            Device.Screen.SafeAreaInsets.UpdateValues();
        }
    }
}