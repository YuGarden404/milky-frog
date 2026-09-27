"""Create a transparent Windows icon from a white-background reference image."""

import argparse
from pathlib import Path

import numpy as np
from PIL import Image


def remove_white_background(source: Image.Image) -> Image.Image:
    rgb = np.asarray(source.convert("RGB"), dtype=np.int16)
    minimum = rgb.min(axis=2)
    spread = rgb.max(axis=2) - minimum

    # White paper/background is bright and nearly neutral. Keep colored
    # pixels and dark details fully opaque, with a soft alpha on antialiased
    # edges so the icon does not retain a white halo.
    alpha = np.maximum((245 - minimum) * 18, spread * 3)
    alpha = np.clip(alpha, 0, 255).astype(np.uint8)
    alpha[(minimum >= 180) & (spread <= 80)] = 0

    rgb = rgb.clip(0, 255).astype(np.uint8)
    rgb[alpha == 0] = 0
    rgba = np.dstack((rgb, alpha))
    image = Image.fromarray(rgba, "RGBA")
    bbox = image.getchannel("A").getbbox()
    if bbox is None:
        raise RuntimeError("The source image contains no visible artwork")

    # Keep a small transparent margin around the artwork for tray rendering.
    left, top, right, bottom = bbox
    margin = max(2, round(max(right - left, bottom - top) * 0.035))
    left = max(0, left - margin)
    top = max(0, top - margin)
    right = min(image.width, right + margin)
    bottom = min(image.height, bottom + margin)
    return image.crop((left, top, right, bottom))


def resize_premultiplied(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    rgba = np.asarray(image.convert("RGBA"), dtype=np.float32)
    alpha = rgba[:, :, 3:4] / 255.0
    premultiplied = rgba[:, :, :3] * alpha

    rgb_image = Image.fromarray(np.uint8(np.round(premultiplied)), "RGB")
    alpha_image = image.getchannel("A")
    resized_rgb = np.asarray(
        rgb_image.resize(size, Image.Resampling.LANCZOS), dtype=np.float32
    )
    resized_alpha = np.asarray(
        alpha_image.resize(size, Image.Resampling.LANCZOS), dtype=np.float32
    )
    alpha_factor = np.maximum(resized_alpha[:, :, None] / 255.0, 1 / 255.0)
    restored_rgb = np.clip(resized_rgb / alpha_factor, 0, 255)
    return Image.fromarray(
        np.dstack((np.uint8(np.round(restored_rgb)), np.uint8(resized_alpha))),
        "RGBA",
    )


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", type=Path, required=True)
    parser.add_argument("--png", type=Path, required=True)
    parser.add_argument("--ico", type=Path, required=True)
    args = parser.parse_args()

    artwork = remove_white_background(Image.open(args.input))
    side = max(artwork.width, artwork.height)
    canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    canvas.alpha_composite(
        artwork,
        ((side - artwork.width) // 2, (side - artwork.height) // 2),
    )

    args.png.parent.mkdir(parents=True, exist_ok=True)
    args.ico.parent.mkdir(parents=True, exist_ok=True)
    resized = resize_premultiplied(canvas, (256, 256))
    resized_pixels = np.asarray(resized).copy()
    resized_rgb = resized_pixels[:, :, :3].astype(np.int16)
    resized_minimum = resized_rgb.min(axis=2)
    resized_spread = resized_rgb.max(axis=2) - resized_minimum
    resized_pixels[(resized_minimum >= 180) & (resized_spread <= 80)] = (0, 0, 0, 0)
    resized = Image.fromarray(resized_pixels, "RGBA")
    resized.save(
        args.png, "PNG", optimize=True
    )

    icon_sizes = (16, 20, 24, 32, 40, 48, 64, 128, 256)
    resized.save(
        args.ico,
        "ICO",
        sizes=[(size, size) for size in icon_sizes],
    )
    print(f"Created {args.png} and {args.ico}")


if __name__ == "__main__":
    main()
