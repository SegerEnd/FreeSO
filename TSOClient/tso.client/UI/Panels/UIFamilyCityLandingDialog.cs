using System;
using System.Linq;
using FSO.Client.Model.FamilyCity;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Files.Formats.IFF.Chunks;

namespace FSO.Client.UI.Panels
{
    /// <summary>
    /// Minimal Phase 2 entry point for Family City: pick or create a city by name and
    /// enter it. Phase 4 will add a real list with thumbnails, family rosters, lot
    /// browser, etc.
    /// </summary>
    public class UIFamilyCityLandingDialog : UIDialog
    {
        private UITextBox NameInput;

        public UIFamilyCityLandingDialog() : base(UIDialogStyle.Close, true)
        {
            Caption = "Family City";

            var existing = FamilyCityManager.ListCityNames().ToList();
            var label = new UILabel
            {
                Caption = existing.Count > 0
                    ? "Existing cities: " + string.Join(", ", existing)
                    : "No cities yet. Enter a name to create one.",
                X = 25,
                Y = 50
            };
            Add(label);

            NameInput = new UITextBox
            {
                X = 25,
                Y = 90,
                Size = new Microsoft.Xna.Framework.Vector2(250, 25)
            };
            NameInput.CurrentText = existing.FirstOrDefault() ?? "MyCity";
            Add(NameInput);

            var enterButton = new UIButton
            {
                Caption = "Enter",
                X = 75,
                Y = 130,
                Width = 150
            };
            enterButton.OnButtonClick += EnterButton_OnClick;
            Add(enterButton);

            base.CloseButton.OnButtonClick += (btn) => UIScreen.RemoveDialog(this);

            SetSize(300, 200);
        }

        private void EnterButton_OnClick(UIElement btn)
        {
            var name = (NameInput.CurrentText ?? "").Trim();
            if (string.IsNullOrEmpty(name))
            {
                UIScreen.GlobalShowAlert(new UIAlertOptions { Message = "Please enter a city name." }, true);
                return;
            }

            FamilyCity city;
            try
            {
                city = FamilyCityManager.Exists(name)
                    ? FamilyCityManager.Open(name)
                    : FamilyCityManager.Create(name);
                city.LoadCharacters();
            }
            catch (Exception ex)
            {
                UIScreen.GlobalShowAlert(new UIAlertOptions { Message = "Could not open city: " + ex.Message }, true);
                return;
            }

            UIScreen.RemoveDialog(this);

            // If the city already has at least one family, drop straight into the first one's lot.
            // Otherwise open the family-creation dialog and proceed once the user has built a family.
            var first = city.Families.Families.FirstOrDefault();
            if (first != null)
            {
                FSOFacade.Controller.EnterFamilyCity(city, first.ChunkID, 1);
                return;
            }

            var createDialog = new UICreateFamilyDialog(city);
            createDialog.OnFamilyCreated += (fami) =>
            {
                FSOFacade.Controller.EnterFamilyCity(city, fami.ChunkID, 1);
            };
            UIScreen.GlobalShowDialog(createDialog, true);
        }
    }
}
