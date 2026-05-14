The Sims 2 UI pack for FreeSO
=============================

This is a loose-folder UI pack. Drop PNG files into this directory named
by their 16-character hex asset id (e.g. 000003a300000001.png).

The id is the (FileID << 32) | TypeID from FreeSO's UIFileIDs enum
(see TSOClient/FSO.UI/UIFileIDs.cs). Path within the pack folder is
ignored — only the basename matters. Subfolders are allowed for your
own organisation.

Quick id reference for testing (most visible on startup):

    setup            (loading screen background)   000003a300000001.png
    maxislogo        (the big maxis logo)          0000039f00000001.png
    eagames          (EA Games splash logo)        0000087700000001.png
    creditscreen_background                        000008ac00000001.png
    creditscreen_maxisbtn                          000008ae00000001.png

Seasonal setup variants (the game picks these by date):
    setup_halloween     0000cdd0856ddbac.png
    setup_thanksgiving  0000cde0856ddbac.png
    setup_xmas          0002000000000018.png
    setup_valentine     0001000000000018.png
    setup_paddys_day    0004000000000018.png

To activate this pack, set FSOEnvironment.ActiveUIPack = "sims2-theme"
before Content.Init() runs, or call:
    Content.Get().UIPacks.SetActive("sims2-theme");
at any time after init.

PNGs use real alpha — no magenta-keying required.
The pack only needs to ship the assets it overrides; everything else
falls through to the stock FAR3 graphics.
