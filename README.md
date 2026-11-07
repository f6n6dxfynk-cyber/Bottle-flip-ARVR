# Bottle Flip in VR

A Unity bottle-flip project with an editable scene, assisted landings, scoring, checkpoints and replay. The demonstrated version runs on a desktop; headset interaction is future work.

## Included folders

- `BottleFlipVR_Final/`: Unity source project, macOS demo, gameplay video, presentation and validation evidence.
- `BottleFlipVRBuild/`: the separately supplied macOS application build.

The folders are kept separate as supplied. Generated working files and macOS metadata are omitted.

## Play

Download or clone this repository, then open `BottleFlipVR_Final/Demo/BottleFlipVR.app` on a Mac. Press and release **Space**, or click outside the instruction panel, to flip. Wait for **Ready** before the next throw. Three successful transfers earn **300 points**. Press **R** or choose **Replay** to restart.

The separate build is at `BottleFlipVRBuild/BottleFlipVR.app`.

## Edit in Unity

1. Use Unity **6000.5.7f1**.
2. Add `BottleFlipVR_Final/UnityProject/BottleFlipVR` in Unity Hub and open it in the Editor.
3. Open `Assets/Scenes/BottleFlipVR.unity` and press Play.
4. Stop Play before making changes you want to save. Select **Bottle** to edit assistance and timing in the Inspector, or select a platform to adjust the course.

## Presentation and evidence

The 12-slide PowerPoint is in `BottleFlipVR_Final/Presentation/`. Its seventh slide contains a recorded gameplay demo. A standalone recording is at `BottleFlipVR_Final/Demo/Gameplay_Demo.mp4`.

The supplied evidence records 48 passing automated checks for the default desktop configuration. These results do not validate a headset version or every future scene change.

The current supplied folder does not contain the `Report/` directory mentioned in its original `START_HERE.txt`; that original instruction file is retained unchanged.

## Team

Harsh Gajera, Rudra Pancholi and Swayam Samel, with equal contribution credit.
