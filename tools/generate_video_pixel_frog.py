"""Create transparent pixel animation frames from a supplied reference video.

Requires opencv-python and Pillow. The video stays outside the app package.
"""

import argparse
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageEnhance, ImageFilter


CANVAS = (192, 240)
IDLE_FPS = 20
IDLE_FRAME_COUNT = 33
IDLE_TIMES = np.arange(IDLE_FRAME_COUNT) / IDLE_FPS
LAUGH_FPS = 15
LAUGH_FRAME_COUNT = 171
LAUGH_TIMES = 2.00 + np.arange(LAUGH_FRAME_COUNT) / LAUGH_FPS


def frame_at(capture: cv2.VideoCapture, seconds: float) -> np.ndarray:
    capture.set(cv2.CAP_PROP_POS_MSEC, float(seconds) * 1000)
    ok, frame = capture.read()
    if not ok:
        raise RuntimeError(f"Could not read video at {seconds:.2f}s")
    return cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)


def character_mask(frame: np.ndarray) -> np.ndarray:
    height, width = frame.shape[:2]
    preview_width = 540
    preview_height = round(height * preview_width / width)
    small = cv2.resize(
        frame, (preview_width, preview_height), interpolation=cv2.INTER_AREA
    )
    hsv = cv2.cvtColor(small, cv2.COLOR_RGB2HSV)

    # The reference is filmed on white. Its yellow body, dark face and
    # warm cream belly are colored; the gray floor shadow is not.
    saturated = (hsv[:, :, 1] > 22) & (hsv[:, :, 2] < 251)
    dark = hsv[:, :, 2] < 132
    candidate = np.uint8(saturated | dark) * 255
    candidate[:35, :] = 0
    candidate[:, :28] = 0
    candidate[:, -28:] = 0
    candidate[-12:, :] = 0

    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (9, 9))
    candidate = cv2.morphologyEx(candidate, cv2.MORPH_CLOSE, kernel)
    contours, _ = cv2.findContours(
        candidate, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE
    )
    if not contours:
        raise RuntimeError("Could not isolate the character")

    largest = max(contours, key=cv2.contourArea)
    if cv2.contourArea(largest) < 12000:
        raise RuntimeError("Character segmentation is unexpectedly small")

    silhouette = np.zeros(candidate.shape, dtype=np.uint8)
    cv2.drawContours(silhouette, [largest], -1, 255, cv2.FILLED)

    # Refine the contour against the light backdrop. Interior eye and
    # tooth highlights remain foreground because they lie inside it.
    eroded = cv2.erode(silhouette, np.ones((5, 5), np.uint8))
    expanded = cv2.dilate(silhouette, np.ones((19, 19), np.uint8))
    grab_mask = np.full(candidate.shape, cv2.GC_BGD, dtype=np.uint8)
    grab_mask[expanded > 0] = cv2.GC_PR_FGD
    grab_mask[eroded > 0] = cv2.GC_FGD
    white = (hsv[:, :, 1] < 9) & (hsv[:, :, 2] > 247)
    grab_mask[white & (silhouette == 0)] = cv2.GC_BGD

    cv2.grabCut(
        small,
        grab_mask,
        None,
        np.zeros((1, 65), np.float64),
        np.zeros((1, 65), np.float64),
        3,
        cv2.GC_INIT_WITH_MASK,
    )
    matte = np.uint8(
        (grab_mask == cv2.GC_FGD) | (grab_mask == cv2.GC_PR_FGD)
    ) * 255

    # Remove detached floor-shadow fragments and keep enclosed details.
    count, labels, statistics, _ = cv2.connectedComponentsWithStats(matte)
    if count > 1:
        largest_component = 1 + np.argmax(statistics[1:, cv2.CC_STAT_AREA])
        matte = np.uint8(labels == largest_component) * 255

    background_like = np.uint8(
        (hsv[:, :, 1] < 42) & (hsv[:, :, 2] > 158)
    )
    component_count, background_labels = cv2.connectedComponents(
        background_like
    )
    border_labels = np.unique(
        np.concatenate(
            (
                background_labels[0, :],
                background_labels[-1, :],
                background_labels[:, 0],
                background_labels[:, -1],
            )
        )
    )
    border_labels = border_labels[border_labels != 0]
    if component_count > 1 and border_labels.size:
        matte[np.isin(background_labels, border_labels)] = 0

    floor_start = round(preview_height * 0.82)
    floor_like = (
        (hsv[floor_start:, :, 1] < 60)
        & (hsv[floor_start:, :, 2] > 140)
    )
    matte[floor_start:, :][floor_like] = 0

    matte = cv2.resize(matte, (width, height), interpolation=cv2.INTER_LINEAR)
    return matte


def clear_arm_gap(image: Image.Image) -> Image.Image:
    pixels = np.asarray(image).copy()
    hsv = cv2.cvtColor(pixels[:, :, :3], cv2.COLOR_RGB2HSV)
    candidate = np.uint8(
        (pixels[:, :, 3] > 0)
        & (hsv[:, :, 1] < 75)
        & (hsv[:, :, 2] > 150)
    )
    candidate[:48, :] = 0
    candidate[87:, :] = 0
    candidate[:, :44] = 0
    candidate[:, 74:] = 0

    count, labels, stats, _ = cv2.connectedComponentsWithStats(candidate)
    for label in range(1, count):
        x, y, width, height, area = stats[label]
        if area < 8 or x + width >= 74 or y + height >= 87:
            continue

        gap = np.uint8(labels == label)
        fringe = cv2.dilate(gap, np.ones((3, 3), np.uint8)) > 0
        pale = (hsv[:, :, 1] < 100) & (hsv[:, :, 2] > 175)
        pixels[(gap > 0) | (fringe & pale)] = (0, 0, 0, 0)

    return Image.fromarray(pixels, "RGBA")


def pixel_frame(frame: np.ndarray, mask: np.ndarray) -> Image.Image:
    height, width = frame.shape[:2]
    # Keep the same camera-space framing throughout the video so the
    # character's movements do not cause scale or position jumps.
    crop = (round(width * 0.065), round(height * 0.055),
            round(width * 0.935), round(height * 0.975))
    rgba = Image.fromarray(np.dstack((frame, mask)), "RGBA").crop(crop)
    rgba.thumbnail(CANVAS, Image.Resampling.LANCZOS)

    low = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    low.alpha_composite(
        rgba, ((CANVAS[0] - rgba.width) // 2, CANVAS[1] - rgba.height)
    )

    alpha = np.asarray(low.getchannel("A")).copy()
    alpha = np.where(alpha >= 115, 255, 0).astype(np.uint8)

    # White backdrop can be enclosed between the legs, so flood-filling
    # from the image border does not always remove it. The lower body has
    # no white details; keep the cream belly and teeth above this cutoff.
    hsv = cv2.cvtColor(np.asarray(low.convert("RGB")), cv2.COLOR_RGB2HSV)
    lower_background = (
        (np.arange(CANVAS[1])[:, None] >= round(CANVAS[1] * 0.67))
        & (hsv[:, :, 1] < 30)
        & (hsv[:, :, 2] > 230)
    )
    alpha[lower_background] = 0
    alpha = Image.fromarray(alpha, "L")
    opaque = Image.new("RGB", CANVAS, "#fff7e7")
    opaque.paste(low.convert("RGB"), mask=alpha)
    opaque = ImageEnhance.Contrast(opaque).enhance(1.08)
    opaque = opaque.filter(ImageFilter.UnsharpMask(radius=0.8, percent=90, threshold=2))
    colors = opaque.quantize(colors=128, method=Image.Quantize.MEDIANCUT)
    result = colors.convert("RGBA")
    result.putalpha(alpha)
    result.paste((0, 0, 0, 0), mask=alpha.point(lambda value: 255 - value))
    pixels = np.asarray(result).copy()
    quantized_hsv = cv2.cvtColor(pixels[:, :, :3], cv2.COLOR_RGB2HSV)
    pixels[
        (np.arange(CANVAS[1])[:, None] >= round(CANVAS[1] * 0.67))
        & (quantized_hsv[:, :, 1] < 30)
        & (quantized_hsv[:, :, 2] > 230)
    ] = (0, 0, 0, 0)
    result = Image.fromarray(pixels, "RGBA")
    return result


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--video", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()

    capture = cv2.VideoCapture(str(args.video))
    if not capture.isOpened():
        raise RuntimeError(f"Could not open {args.video}")

    try:
        for state, times in (("Idle", IDLE_TIMES), ("Laugh", LAUGH_TIMES)):
            target = args.output / state
            target.mkdir(parents=True, exist_ok=True)
            stem = "idle" if state == "Idle" else "laugh"
            for index, seconds in enumerate(times):
                frame = frame_at(capture, float(seconds))
                mask = character_mask(frame)
                result = pixel_frame(frame, mask)
                if state == "Laugh" and 76 <= index <= 96:
                    result = clear_arm_gap(result)
                result.save(target / f"{stem}_{index:03d}.png", optimize=True)
    finally:
        capture.release()

    print(
        f"Generated {len(IDLE_TIMES)} idle and "
        f"{len(LAUGH_TIMES)} laugh frames in {args.output}"
    )


if __name__ == "__main__":
    main()
