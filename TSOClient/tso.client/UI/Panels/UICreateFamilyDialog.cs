using System;
using System.Collections.Generic;
using System.Linq;
using FSO.Client.Model.FamilyCity;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Utils;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Panels
{
    /// <summary>
    /// Create a new family in a <see cref="FamilyCity"/> by adding members one at a time.
    /// Sequential per-member entry (the user's chosen flow): name/gender/skin per member,
    /// "Add" appends to the roster, "Create" calls the neighbour generator.
    ///
    /// Outfit / body type defaults to fit-medium-adult for now; real outfit picking is a
    /// later polish pass.
    /// </summary>
    public class UICreateFamilyDialog : UIDialog
    {
        private readonly FamilyCity City;

        private UITextBox FamilyNameInput;
        private UITextBox MemberNameInput;
        private UIButton MaleButton;
        private UIButton FemaleButton;
        private UIButton SkinLightButton;
        private UIButton SkinMediumButton;
        private UIButton SkinDarkButton;
        private UIButton AddMemberButton;
        private UIButton CreateButton;
        private UILabel RosterLabel;

        private readonly List<SimTemplateCreateInfo> Members = new List<SimTemplateCreateInfo>();

        private bool IsMale = true;     //'m'
        private string SkinCode = "lgt"; //'lgt' / 'med' / 'drk'

        /// <summary>Fires after the family is created with its new <see cref="FAMI"/>.</summary>
        public event Action<FAMI> OnFamilyCreated;

        public UICreateFamilyDialog(FamilyCity city) : base(UIDialogStyle.Close, true)
        {
            City = city;
            Caption = "New Family in " + city.Name;

            int y = 50;
            Add(new UILabel { Caption = "Family Name:", X = 25, Y = y });
            FamilyNameInput = new UITextBox { X = 130, Y = y, Size = new Vector2(150, 25) };
            FamilyNameInput.CurrentText = "Smith";
            Add(FamilyNameInput);

            y += 50;
            Add(new UILabel { Caption = "Member Name:", X = 25, Y = y });
            MemberNameInput = new UITextBox { X = 130, Y = y, Size = new Vector2(150, 25) };
            Add(MemberNameInput);

            y += 40;
            MaleButton = new UIButton { Caption = "Male", X = 25, Y = y, Width = 80, Selected = true };
            MaleButton.OnButtonClick += (b) => { IsMale = true; MaleButton.Selected = true; FemaleButton.Selected = false; };
            Add(MaleButton);
            FemaleButton = new UIButton { Caption = "Female", X = 115, Y = y, Width = 80 };
            FemaleButton.OnButtonClick += (b) => { IsMale = false; MaleButton.Selected = false; FemaleButton.Selected = true; };
            Add(FemaleButton);

            y += 40;
            SkinLightButton = new UIButton { Caption = "Light", X = 25, Y = y, Width = 80, Selected = true };
            SkinLightButton.OnButtonClick += (b) => SetSkin("lgt");
            Add(SkinLightButton);
            SkinMediumButton = new UIButton { Caption = "Medium", X = 115, Y = y, Width = 80 };
            SkinMediumButton.OnButtonClick += (b) => SetSkin("med");
            Add(SkinMediumButton);
            SkinDarkButton = new UIButton { Caption = "Dark", X = 205, Y = y, Width = 80 };
            SkinDarkButton.OnButtonClick += (b) => SetSkin("drk");
            Add(SkinDarkButton);

            y += 50;
            AddMemberButton = new UIButton { Caption = "Add Member", X = 25, Y = y, Width = 130 };
            AddMemberButton.OnButtonClick += AddMember_OnClick;
            Add(AddMemberButton);

            CreateButton = new UIButton { Caption = "Create Family", X = 165, Y = y, Width = 130, Disabled = true };
            CreateButton.OnButtonClick += CreateFamily_OnClick;
            Add(CreateButton);

            y += 50;
            RosterLabel = new UILabel { Caption = "Members: (none yet)", X = 25, Y = y };
            Add(RosterLabel);

            base.CloseButton.OnButtonClick += (btn) => UIScreen.RemoveDialog(this);

            SetSize(320, y + 60);
        }

        private void SetSkin(string code)
        {
            SkinCode = code;
            SkinLightButton.Selected = code == "lgt";
            SkinMediumButton.Selected = code == "med";
            SkinDarkButton.Selected = code == "drk";
        }

        private void AddMember_OnClick(UIElement btn)
        {
            var memberName = (MemberNameInput.CurrentText ?? "").Trim();
            if (string.IsNullOrEmpty(memberName))
            {
                UIScreen.GlobalShowAlert(new UIAlertOptions { Message = "Enter a member name first." }, true);
                return;
            }
            //code is gender + 'a' (adult) + 'fit' (default body type). See SimTemplateCreateInfo.
            var code = (IsMale ? "m" : "f") + "a" + "fit";
            var info = new SimTemplateCreateInfo(code, SkinCode)
            {
                Name = memberName,
                Bio = "",
            };
            // TSO-platform avatars read body & head from STR slots 1 and 2 as hex outfit IDs
            // (see VMAvatar.SetAvatarBodyStrings + VMOutfitReference.Parse). SimTemplateCreateInfo's
            // TS1 body strings target other slots and would leave the avatar invisible without TS1
            // content. Inject FreeSO default outfit IDs so the member renders out-of-the-box.
            info.BodyStringReplace[1] = "24C0000000D";   //default daywear body
            info.BodyStringReplace[2] = "3a00000000D";   //bob newbie head
            Members.Add(info);
            MemberNameInput.CurrentText = "";
            RefreshRoster();
        }

        private void RefreshRoster()
        {
            if (Members.Count == 0) RosterLabel.Caption = "Members: (none yet)";
            else RosterLabel.Caption = "Members: " + string.Join(", ", Members.Select(m => m.Name));
            CreateButton.Disabled = Members.Count == 0;
        }

        private void CreateFamily_OnClick(UIElement btn)
        {
            var familyName = (FamilyNameInput.CurrentText ?? "").Trim();
            if (string.IsNullOrEmpty(familyName))
            {
                UIScreen.GlobalShowAlert(new UIAlertOptions { Message = "Enter a family name first." }, true);
                return;
            }

            FAMI fami;
            try
            {
                fami = FamilyCityNeighbourGenerator.CreateFamily(City, familyName, Members.ToArray());
            }
            catch (Exception ex)
            {
                UIScreen.GlobalShowAlert(new UIAlertOptions { Message = "Could not create family: " + ex.Message }, true);
                return;
            }

            UIScreen.RemoveDialog(this);
            OnFamilyCreated?.Invoke(fami);
        }
    }
}
