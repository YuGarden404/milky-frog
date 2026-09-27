# Character frames

These transparent pixel frames were derived from `Naiwa.mp4` in
CHENGONGSHUO/Naiwa, sampled and processed by
`tools/generate_video_pixel_frog.py`.
The current set contains 33 idle frames at 20 fps and 171 laugh frames at
15 fps. Idle plays forward and backward for a seamless loop without duplicate
files. Each PNG is 192 x 240 pixels and uses nearest-neighbor scaling.

Source: https://github.com/CHENGONGSHUO/Naiwa
Repository license: GPL-3.0

The source repository does not document the origin or separate licensing of
the character design and video. Confirm those rights before publishing or
commercially distributing the frames. The video itself is not bundled.

To regenerate on Windows, install `tools/requirements-assets.txt` and run:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\GeneratePixelFrogAssets.ps1 -VideoPath "E:\path\to\Naiwa.mp4"
```
