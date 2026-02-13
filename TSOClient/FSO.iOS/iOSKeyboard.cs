using System;
using Foundation;
using UIKit;
using FSO.Common;
using FSO.Common.Rendering.Framework;

namespace FSO.iOS
{
    /// <summary>
    /// Manages a hidden UITextField to show/hide the iOS software keyboard
    /// and forward typed characters to the game's text input system.
    /// </summary>
    public static class iOSKeyboard
    {
        private static UITextField _hiddenField;
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            FSOEnvironment.RequestSoftKeyboard = OnKeyboardRequested;
        }

        private static void EnsureField()
        {
            if (_hiddenField != null) return;

            NSRunLoop.Main.BeginInvokeOnMainThread(() =>
            {
                var window = UIApplication.SharedApplication.KeyWindow
                    ?? UIApplication.SharedApplication.Windows[0];

                _hiddenField = new UITextField
                {
                    AutocorrectionType = UITextAutocorrectionType.No,
                    AutocapitalizationType = UITextAutocapitalizationType.None,
                    SpellCheckingType = UITextSpellCheckingType.No,
                    KeyboardType = UIKeyboardType.Default,
                    ReturnKeyType = UIReturnKeyType.Done,
                    Hidden = false,
                    // Place off-screen so it's not visible but still functional
                    Frame = new CoreGraphics.CGRect(-100, -100, 1, 1),
                    Text = " " // Need at least one char for backspace to work
                };

                _hiddenField.ShouldChangeCharacters = (textField, range, replacement) =>
                {
                    if (string.IsNullOrEmpty(replacement))
                    {
                        // Backspace
                        GameScreen.TextInput(null,
                            new Microsoft.Xna.Framework.TextInputEventArgs('\b', Microsoft.Xna.Framework.Input.Keys.Back));
                    }
                    else
                    {
                        foreach (var c in replacement)
                        {
                            GameScreen.TextInput(null,
                                new Microsoft.Xna.Framework.TextInputEventArgs(c));
                        }
                    }

                    // Reset text to a single space so backspace always works
                    textField.Text = " ";
                    return false;
                };

                _hiddenField.ShouldReturn = (textField) =>
                {
                    GameScreen.TextInput(null,
                        new Microsoft.Xna.Framework.TextInputEventArgs('\r', Microsoft.Xna.Framework.Input.Keys.Enter));
                    return false;
                };

                window.AddSubview(_hiddenField);
            });
        }

        private static void OnKeyboardRequested(bool show)
        {
            EnsureField();

            NSRunLoop.Main.BeginInvokeOnMainThread(() =>
            {
                if (_hiddenField == null) return;

                if (show)
                {
                    _hiddenField.Text = " ";
                    _hiddenField.BecomeFirstResponder();
                }
                else
                {
                    _hiddenField.ResignFirstResponder();
                }
            });
        }
    }
}
