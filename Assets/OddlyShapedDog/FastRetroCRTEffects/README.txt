Fast retro CRT effect for Unity
===============================

Description
-----------

This is an image effect you can apply to any Unity camera to lower your game's resolution and/or mimic the look of a CRT screen. It's the perfect cherry on top of a games with retro graphics (SNES/N64/PS1).

Instructions
------------

1. Import the asset pack into your Unity project
2. Add the "RetroImageEffect" script to your camera
3. In the script properties, choose a material:

- To emulate a specific resolution regardless of the actual screen size or aspect ratio, use the "FastDownsampleEffect​" material.
- To emulate a specific resolution and mimic the look of pixels in a CRT screen, pick "FastCRTEffectWithOverlays​".
- For all of the above, plus rounded borders and a fake light reflection, pick "FastCRTEffectWithOverlays​".

​---

Q: What makes this different from other CRT effects on the Unity store?

This one is cheap, simple, and fast. It doesn't have computationally expensive operations like distortion, bloom, and chromatic aberration. It aims to achieve the CRT look with textures and blending modes alone.

Also, If you're technically minded, you'll also find these shaders easy to pick apart and customize. You can tweak the strength of various effects and swap out the textures. And the shaders are organized into independent, self-contained shader passes that you can reference in your own shaders.​

---

Q: ​Can I use this shader to make an in-game TV screen?

A: Yes! You can use this shader in a spatial material. Just make sure _MainTex is set to the image you want to display on the TV screen.

---

Shameless plug

If you're working on a 3D retro game, check out my vertex-lit/vertex-colored shader pack here: LINK TBD