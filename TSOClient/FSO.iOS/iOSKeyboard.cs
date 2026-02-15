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
        private static HiddenFieldDelegate _fieldDelegate;
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            FSOEnvironment.RequestSoftKeyboard = OnKeyboardRequested;
        }

        /// <summary>
        /// UITextFieldDelegate subclass that intercepts keystrokes and forwards
        /// them to GameScreen.TextInput. Using a delegate class is more reliable
        /// than property-based delegates on iOS.
        /// </summary>
        private class HiddenFieldDelegate : UITextFieldDelegate
        {
            public override bool ShouldChangeCharacters(UITextField textField, NSRange range, string replacementString)
            {
                if (string.IsNullOrEmpty(replacementString))
                {
                    // Backspace
                    GameScreen.TextInput(null,
                        new Microsoft.Xna.Framework.TextInputEventArgs('\b', Microsoft.Xna.Framework.Input.Keys.Back));
                }
                else
                {
                    foreach (var c in replacementString)
                    {
                        GameScreen.TextInput(null,
                            new Microsoft.Xna.Framework.TextInputEventArgs(c));
                    }
                }

                // Defer text reset - modifying text inside this callback can break
                // the delegate chain on newer iOS versions.
                // Return false so iOS doesn't apply the change itself.
                NSRunLoop.Main.BeginInvokeOnMainThread(() =>
                {
                    textField.Text = " ";
                });
                return false;
            }

            public override bool ShouldReturn(UITextField textField)
            {
                GameScreen.TextInput(null,
                    new Microsoft.Xna.Framework.TextInputEventArgs('\r', Microsoft.Xna.Framework.Input.Keys.Enter));
                return false;
            }
        }

        private static void EnsureField()
        {
            if (_hiddenField != null) return;

            NSRunLoop.Main.BeginInvokeOnMainThread(() =>
            {
                if (_hiddenField != null) return; // Double-check after dispatch

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

                _fieldDelegate = new HiddenFieldDelegate();
                _hiddenField.Delegate = _fieldDelegate;

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
